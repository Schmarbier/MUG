# Phase 0 — Research: PersonalFinance

**Feature**: 001-personal-finance-viewer · **Fecha**: 2026-07-18
**Entrada**: [spec.md](./spec.md) · [constitution.md](../../.specify/memory/constitution.md) · AGENTS.md

Este documento resuelve las incógnitas técnicas del plan. Cada decisión se justifica contra un
requisito de la spec o un principio de la constitución. Las alternativas descartadas quedan
registradas para no volver a discutirlas.

---

## R1 — Almacenamiento de montos y tipos de cambio en SQLite

**Incógnita**: FR-038 exige 2 decimales exactos en montos y FR-039 lo mismo en tipos de cambio.
SQLite no tiene tipo decimal nativo: solo INTEGER, REAL y TEXT. EF Core mapea `decimal` a TEXT por
defecto y advierte explícitamente que las comparaciones y agregaciones en SQL sobre esa columna no
son confiables. FR-015a ordena por monto y FR-013 suma: si el almacenamiento no es ordenable ni
sumable de forma exacta, ambos requisitos quedan comprometidos.

**Decisión**: almacenar montos y tipos de cambio como **INTEGER en unidades menores** (centavos y
centésimos respectivamente), mediante un `ValueConverter` de EF Core que expone `decimal` al
dominio y persiste `long`. Toda la aritmética de conversión y suma se hace en `decimal` de .NET,
que es de base 10 y exacto para este dominio.

**Rationale**:
- Exactitud absoluta: no hay representación binaria de fracciones, que es la falla clásica de usar
  REAL para dinero.
- El orden numérico de FR-015a y las sumas son correctos incluso si algún día se empujan a SQL.
- El dominio nunca ve `long`: el converter mantiene la ubicuidad del lenguaje (`decimal Monto`).
- 2 decimales fijos (FR-038/FR-039) hacen que el factor de escala sea constante y trivial: ×100.

**Alternativas consideradas**:
- `decimal` → TEXT (default de EF Core): descartado. Ordenar por una columna TEXT ordena
  lexicográficamente ("$9" > "$10"), lo que rompe FR-015a de forma silenciosa.
- `double`/REAL: descartado de plano. Coma flotante binaria sobre dinero viola FR-038 y el
  Principio III (un centavo fabricado sigue siendo un dato fabricado).
- Guardar `decimal` como TEXT y agregar siempre en memoria: viable a esta escala, pero deja una
  trampa latente para el día que alguien escriba un `OrderBy` que sí se traduzca a SQL.

---

## R2 — Cálculo y redondeo del resumen

**Incógnita**: FR-040 exige precisión completa hasta la suma y un único redondeo al mostrar, con
empate hacia arriba. Hay que decidir dónde ocurre la agregación.

**Decisión**: la agregación del resumen mensual se resuelve **en memoria**, cargando los
movimientos del mes y agrupando con LINQ to Objects. El redondeo se aplica una sola vez, en el
borde de presentación, con `decimal.Round(valor, 2, MidpointRounding.AwayFromZero)`.

**Rationale**:
- `MidpointRounding.AwayFromZero` es exactamente "al más cercano, empate hacia arriba" de FR-040.
  El default de .NET es `ToEven` (redondeo bancario), que daría $2.930,12 donde la spec exige
  $2.930,11 — el escenario US2 AC-3.b falla si se usa el default.
- Agregar en memoria garantiza que la conversión por movimiento (FR-013, suma de equivalentes con
  tipo de cambio individual) se calcule en `decimal` y no en el motor SQL.
- El volumen lo permite holgadamente (ver R7).

**Alternativas consideradas**:
- Agregación en SQL con `GROUP BY`: descartada. Obligaría a llevar la lógica de conversión por
  tipo de cambio histórico al motor, que no puede expresarla sin desnormalizar.
- Vista materializada / tabla de resumen: descartada. La spec declara el resumen como vista
  derivada no persistida (Key Entities), y persistirlo abriría un problema de invalidación tras
  cada corrección manual (FR-018 a FR-023) sin ningún beneficio a esta escala.

---

## R3 — Aislamiento e invocación del modelo (Principio II)

**Incógnita**: cómo estructurar la llamada a Ollama para que la lógica de negocio sea testeable
sin modelo real, como exige el Principio II, y cómo obtener una respuesta parseable de forma
confiable.

**Decisión**: definir un **puerto** en el dominio que exprese la operación en términos del negocio
(recibe el texto del mensaje y el catálogo de categorías y monedas activas; devuelve una
clasificación propuesta o una falla tipada). El adaptador que habla con Ollama vía OllamaSharp
vive en infraestructura y es la única pieza que conoce prompts, JSON y timeouts. Se solicita
salida en formato JSON con esquema explícito y temperatura baja, y toda respuesta que no valide
contra el esquema se traduce a falla, nunca a un valor asumido.

**Rationale**:
- El Principio II exige literalmente que prompts, parseo y manejo de errores del modelo vivan
  aislados del negocio. El puerto es esa frontera.
- Los tests de clasificación usan un doble del puerto: deterministas, sin Ollama levantado, lo que
  hace viable el ciclo rojo-verde-refactor del Principio I sobre reglas de negocio.
- Que una respuesta inválida se convierta en falla y no en un valor por defecto es la
  materialización del Principio III y de FR-011.

**Alternativas consideradas**:
- Llamar a OllamaSharp desde el servicio de clasificación: viola el Principio II de forma directa.
- Parsear la respuesta con expresiones regulares sobre texto libre: descartado. Es frágil y
  empuja a "interpretar" salidas ambiguas, que es justo lo que el Principio III prohíbe.

---

## R4 — Ingesta, ciclo y reintentos

**Incógnita**: FR-005a exige clasificar en el mismo ciclo de ingesta, y FR-010a exige reintentar
un mensaje pendiente en el ciclo siguiente hasta 3 veces. Con ingesta reactiva a mensajes nuevos,
un mensaje pendiente podría no reintentarse nunca si no llegan mensajes nuevos.

**Decisión**: el proceso Bot corre **dos responsabilidades en un mismo servicio alojado**:
1. Recepción de actualizaciones de Telegram por long polling, que persiste los mensajes del chat
   autorizado y dispara la clasificación de pendientes.
2. Un **barrido periódico cada 60 segundos** que clasifica los mensajes pendientes que existan,
   haya o no tráfico nuevo.

Un "ciclo de ingesta" en los términos de FR-005a y FR-010a es cualquiera de los dos disparadores.

**Rationale**:
- Sin el barrido, FR-010a es incumplible en el caso justamente más probable: Ollama caído durante
  un período sin mensajes nuevos. El mensaje quedaría pendiente para siempre y nunca alcanzaría el
  tope de 3 intentos que FR-010b necesita para hacerlo visible.
- 60 segundos es holgado frente a SC-002 (clasificación < 5 s p90) y no compite con la ingesta.
- La deduplicación por identificador de mensaje del canal (FR-004, y Restricciones Técnicas de la
  constitución) se resuelve con un **índice único** en la columna del identificador, además de la
  verificación previa. El índice es la garantía real: la verificación sola tiene una condición de
  carrera entre los dos disparadores.

**Alternativas consideradas**:
- Solo long polling: descartado por lo anterior.
- Solo barrido periódico: descartado. Retrasa la ingesta hasta un minuto sin necesidad.
- Reintento con espera exponencial: descartado por ahora. Con tope de 3 intentos y ciclo de 60 s,
  agrega complejidad sin cambiar el resultado observable.

---

## R5 — Zona horaria y asignación al mes (resuelve CHK034)

**Incógnita**: las Assumptions dicen "zona horaria local del dueño" sin fijar cuál, y de eso
depende a qué mes pertenece un movimiento.

**Decisión**: fijar `America/Argentina/Buenos_Aires` como zona horaria de la aplicación,
resuelta con `TimeZoneInfo.FindSystemTimeZoneById`. Las marcas temporales se persisten en UTC y se
convierten a esa zona únicamente para decidir la fecha del movimiento y el mes del resumen.

**Rationale**:
- .NET 6+ acepta identificadores IANA en Windows y en Linux, así que el mismo literal funciona en
  ambos sin condicionales.
- Persistir en UTC y convertir en el borde evita que un cambio de huso reescriba el pasado.
- Es una decisión de configuración, no de dominio: queda expuesta como opción para no volver a
  hardcodearla si el dueño viaja.

---

## R6 — Superficie del visor y modo de render

**Incógnita**: FR-036 exige pantallas de administración, pero AGENTS.md fija Static SSR por
defecto para el visor.

**Decisión**: el resumen mensual (US2) se sirve en **Static SSR**. Las pantallas de administración
—categorías, monedas, bandeja de errores y edición de movimientos— habilitan interactividad
**por componente** con `@rendermode InteractiveServer`. No se adopta WebAssembly.

**Rationale**:
- Es exactamente el camino que AGENTS.md dejó previsto, y evita reescribir el visor.
- El resumen es de solo lectura y se beneficia del render en servidor: sin circuito, sin costo de
  conexión, y ayuda a SC-003.
- La edición requiere estado y validación interactiva; forzarla a formularios con post completo
  complicaría FR-023, que necesita una confirmación intermedia del usuario.

**Alternativas consideradas**:
- Todo InteractiveServer: descartado. Paga circuito SignalR en la pantalla más visitada, que no lo
  necesita.
- Todo Static SSR con formularios clásicos: viable pero incómodo para FR-023.

---

## R7 — Volumen de referencia para los criterios de rendimiento (resuelve CHK037)

**Incógnita**: SC-003 exige el resumen en menos de 1 segundo p95 sin declarar sobre qué volumen,
lo que lo hacía no verificable.

**Decisión**: el volumen de referencia para medir SC-003 es **24 meses de historia, 300
movimientos por mes (7.200 movimientos), 20 categorías y 3 monedas**. El resumen se mide sobre el
mes en curso dentro de ese conjunto.

**Rationale**:
- Es un techo generoso para finanzas personales mono-usuario: 10 movimientos por día durante dos
  años sin interrupción.
- Fija el orden de magnitud que justifica la agregación en memoria de R2. A este volumen la
  operación es de milisegundos.

---

## R8 — Definición de acierto de la clasificación (resuelve CHK038)

**Incógnita**: SC-001 exige 80% de acierto sin definir qué cuenta como acierto.

**Decisión**: un mensaje se considera **acertado solo si los cuatro atributos coinciden** con la
etiqueta esperada: categoría, tipo, monto y moneda. Un acierto parcial cuenta como error.

**Rationale**:
- Es el criterio honesto: un movimiento con la categoría correcta y el monto equivocado ensucia el
  resumen igual que uno completamente mal.
- Alineado con el Principio III: no se premia una salida parcialmente inventada.
- Hace el umbral objetivamente verificable sobre el conjunto etiquetado de SC-001.

---

## R9 — Estrategia de pruebas (Principio I)

**Decisión**: xUnit, con la pirámide siguiente:
- **Unitarias de dominio**: reglas de clasificación, cálculo del resumen, redondeo, orden y
  paginación, ciclo de vida de categorías y monedas. Sin base de datos ni modelo.
- **Integración de persistencia**: contra SQLite en archivo temporal por test, no in-memory, para
  ejercitar el converter de R1, el índice único de R4 y las migraciones reales.
- **Integración del adaptador de IA**: contra un servidor simulado, verificando el contrato de
  R3 —incluido el camino de respuesta inválida→ falla— sin depender de Ollama.

**Rationale**: el Principio I es no negociable y exige que el test exista y falle antes del
código. Esta división permite que la mayoría de los requisitos de la spec se cubran con pruebas
rápidas y deterministas, que es la única forma de que el ciclo rojo-verde-refactor sea sostenible.

**Nota de dependencias**: verificar la licencia de toda librería de aserciones antes de sumarla;
varias del ecosistema .NET cambiaron a licencia comercial en versiones recientes. Las aserciones
nativas de xUnit alcanzan para todo lo anterior.

---

## R10 — Configuración y secretos (Principio IV)

**Decisión**: `TelegramBotToken`, `TelegramChatAutorizado` y `OLLAMA_MODEL` se leen vía
`IConfiguration` desde User Secrets en desarrollo o variables de entorno. `appsettings.json`
documenta las claves con valor vacío o `0`. No se usan archivos `.env`.

La base SQLite vive en la ruta absoluta `%LOCALAPPDATA%\PersonalFinance\personalfinance.db`,
compartida por Bot y Web, con override por cadena de conexión.

**Rationale**: Principio IV y Restricciones Técnicas de la constitución, literal. La ruta absoluta
no es una preferencia: una ruta relativa produce archivos divergentes porque cada proceso corre
con su propio directorio de trabajo, y la constitución lo prohíbe explícitamente.

---

## Incógnitas restantes

Ninguna bloquea el diseño. Quedan abiertos, y documentados en el checklist de integridad
financiera, los ítems de totales de bloque (CHK010–CHK015) y propagación de tipo de cambio
(CHK016–CHK023). Se resuelven en `/speckit-tasks` o en una segunda pasada de `/speckit-clarify`;
ninguno cambia el modelo de datos ni la estructura de proyectos definidos aquí.

# VERIFY — FEAT-001a

**Ticket:** FEAT-001a — Ingesta de mensajes de Telegram y clasificación en movimientos vía Ollama
**Tier:** FEATURE · **Fecha:** 2026-08-02 · **Ronda:** 1
**PRD:** `docs/daw/prd/prd-FEAT-001a.md` · **Spec:** `docs/daw/specs/spec-FEAT-001a.md`
**Veredicto:** ✅ **PASSED** — 36 PASS · 11 WARN · **0 FAIL**

La verificación cruzada la hizo el agente `daw-module-verifier`, que no escribió esta
implementación. Los números de cobertura los midió el orquestador por separado. Dos de los
hallazgos (W7 y W8) los encontraron ambos de forma independiente.

---

## F-VER-01 — Trazabilidad AC → código → test

Los **14 AC** del PRD tienen implementación localizable y un test que verifica comportamiento real
(no sólo "no explota"). Cada AC está nombrado explícitamente en el título o en un comentario de al
menos un test, como exige AGENTS.md.

| AC | Código | Test | |
|---|---|---|---|
| AC-01 | `IngestarMensajes.cs:79-85` | `IngestarMensajesTests.cs:21` — tupla completa | ✅ |
| AC-02 | `IngestarMensajes.cs:96-99` (`EsAceptable`) | `IngestarMensajesTests.cs:36` | ✅ |
| AC-03 | `IngestarMensajes.cs:74` + `MensajeConfiguration.cs:30` (índice UNIQUE) | `IngestarMensajesTests.cs:48`, `RepositorioMensajesTests.cs:14/28/41` | ✅ |
| AC-04 | `SeedCategorias.cs:52` | `SeedCategoriasTests.cs:16` | ✅ |
| AC-05 | `SeedCategorias.cs:63-67` | `SeedCategoriasTests.cs:31/47` | ✅ |
| AC-06 | `ClasificarMensajesPendientes.cs:83-94` | `ClasificarMensajesPendientesTests.cs:37` | ⚠️ W1 |
| AC-07 | ídem | `ClasificarMensajesPendientesTests.cs:53` + dataset #1 | ✅ |
| AC-08 | `ClasificadorOllama.cs:207-209` (`ResolverCategoria`) | `ClasificadorOllamaTests.cs:74` | ✅ |
| AC-09 | `ClasificadorOllama.cs:155-158` | `ClasificadorOllamaTests.cs:137/151` | ✅ |
| AC-10 | `ClasificadorOllama.cs:53-60,73` | `ClasificadorOllamaTests.cs:168` | ✅ |
| AC-11 | `ClasificadorOllama.cs:195-202` (`LeerTipo`) | `ClasificadorOllamaTests.cs:121` | ✅ |
| AC-12 | `ClasificarMensajesPendientes.cs:75-81` | `ClasificarMensajesPendientesTests.cs:109` | ✅ |
| AC-13 | `AccuracyClasificadorTests.cs:69` | 94.0% (47/50) ≥ 80% | ✅ |
| AC-14 | `LatenciaClasificadorTests.cs:56` | p90 < 5 s | ✅ |

## F-VER-02 / F-VER-06 — Tareas y tests comprometidos

Los **57 tests** que el spec prometió (6+11+13+13+9+5) existen con el nombre exacto declarado y
pasan. Los 6 bloques están implementados con todos los archivos de su sección *Files*. ✅

## F-VER-03 — Cobertura

Medida sobre `src/`, fusionando la corrida unitaria y la de integración, **excluyendo código
generado y accesores triviales**.

| | Total `src/` | Domain | Infrastructure |
|---|---|---|---|
| Líneas | **92.4%** (681/737) ✅ | 96.1% ✅ | 90.9% ✅ |
| Ramas | **81.9%** (118/144) ✅ | 97.4% ✅ | 76.4% ⚠️ |
| Métodos | **86.7%** (39/45) ✅ | 100% ✅ | 84.6% ✅ |

Las tres métricas superan el 80% exigido. ✅

**Nota metodológica.** La medición cruda daba 68.2% de métodos en Domain. No era un agujero de
tests: eran constructores de copia de `record`, accesores `get_`/`set_` y `MoveNext()` de máquinas
de estado async — miembros que sintetiza el compilador y que ningún test invoca a propósito.
Filtrados, Domain queda en 100%. Un gate que dispara por un artefacto de medición entrena a la
gente a ignorar el gate.

Las ramas de Infrastructure al 76.4% son en su mayoría los `if (OperatingSystem.IsWindows())` de
`AgregarPersistenciaExtensions` (ACL de Windows vs `UnixFileMode`): en una sola máquina es
imposible cubrir las dos ramas. No es deuda.

## F-VER-04 — Sad paths

Toda superficie de entrada tiene al menos un sad path: `IngestarMensajes` (6),
`ClasificarMensajesPendientes` (5), `ClasificadorOllama` (7), `FuenteMensajesTelegram` (4), las
tres extensiones de DI, `SeedCategorias` (2), repositorios (violación de constraint única) y
`UnitOfWork` (atomicidad real contra SQLite). Ningún componente con sólo happy path. ✅

## F-VER-05 — Lint / type checker

`dotnet build PersonalFinance.sln`: 0 errores, 0 advertencias, con `TreatWarningsAsErrors=true` y
`EnforceCodeStyleInBuild=true`. ✅

## Convenciones de arquitectura (AGENTS.md)

Todas limpias: `Domain.csproj` sin una sola referencia; sin `using` de EF Core / Telegram.Bot /
OllamaSharp / `Microsoft.Extensions.*` en Domain; sin `DateTime.UtcNow` en Domain (entra por
`IReloj`); entidades sin atributos de EF (mapeo íntegro en `Persistencia/Configuraciones/*`);
`DbContext` confinado a Infrastructure; `Domain.Tests` sin referencia a Infrastructure ni a SQLite;
`IConfiguration` con 0 apariciones bajo `src/PersonalFinance.Infrastructure/`; errores esperados
como valor de retorno (`ResultadoClasificacion`, tipo cerrado con constructor base privado).

Las 6 mitigaciones del threat model (M-01…M-06) tienen test propio.

---

## WARNINGS (11) — ninguno bloquea

### Cobertura y tests

- **W7 — El tope de 8 KB de la respuesta del modelo no tiene test.** `ClasificadorOllama.cs:20,110`
  implementa la regla que el spec declara en *Input validation* del Bloque 4 (*"Máx. 8 KB de
  respuesta"*), y es la validación sobre el input **menos confiable del sistema**: lo que contesta
  el LLM. Con `HandlerFalso` se cubre en tres líneas. En la misma bolsa, sin test: `ClasificarAsync`
  con `texto` vacío o > 4096 (`:42-43`) y `LeerAsync` con `maximo < 1`
  (`FuenteMensajesTelegram.cs:37`). **Es el warning de mayor prioridad.**
- **W8 — El filtro `Where(c => c.Activa)` de FR-08 no tiene test.** `RepositorioCategoriasEfCore.cs:24`
  tiene **0% de cobertura**: es el único adaptador de puerto entregado sin una sola prueba. El test
  que suena a que lo cubre, `PromptClasificacionTests.cs:19`
  (`..._IncluyeLasCategoriasActivasYNoLasDesactivadas`), tiene la segunda mitad vacía: assertea
  `DoesNotContain("Ocio")` sobre un prompt al que nunca se le pasó `Ocio`. Verifica que la función
  imprime lo que recibe, no que alguien filtró.
- **W1 — AC-06 es el único AC cuyo enunciado literal no se ejerce.** El AC nombra el texto
  `"$10.000 sueldo de julio"`; el test de dominio le pasa la clasificación ya hecha
  (`ClasificadorFalso`) y el dataset de accuracy no contiene ese string (lo más cercano es #33
  `"Me depositaron el sueldo de julio 1.200.000"`). Nadie verifica que el modelo real parsee
  `"$10.000"` con separador de miles. Se cierra agregando una entrada al dataset; no es hueco de
  implementación.
- **W12 — `CadenaPorDefecto()` sin test.** Compone
  `%LOCALAPPDATA%\PersonalFinance\personalfinance.db`, que es **la ruta que usa la corrida real**.
  Todos los tests pasan una cadena de conexión explícita, así que el único camino que no se ejerce
  es el que corre en producción.
- **W11 — Rama sin test y sin spec.** `ClasificadorOllama.cs:167-171` devuelve `NoDisponible`
  cuando el modelo contesta una categoría desconocida **y** `Otros` no está activa. La decisión es
  razonable (no destruir un mensaje recuperable) pero no figura en la tabla de *Error handling*.

### Test frágil

- **W9 — `Accuracy_OllamaNoDisponible_FallaConMensajeExplicito` depende de la máquina.**
  `AccuracyClasificadorTests.cs:52` abre un `TcpClient` contra `127.0.0.1:11435` y da verde
  *porque en esta máquina no hay nada escuchando en ese puerto*. No está marcado
  `[Trait("Categoria","Integracion")]`, así que corre en `dotnet test` a secas. El día que alguien
  levante cualquier cosa en 11435, el test se cae sin que el producto haya cambiado. Se cierra
  tomando un puerto efímero y liberándolo, o apuntando a un host reservado.

### Spec desincronizado del código

El código está bien en los tres casos; el documento quedó diciendo otra cosa.

- **W2 — Bloque 5, *Error handling*.** El spec dice *"Falla al persistir el movimiento → se
  continúa con el siguiente mensaje"*. El código **corta la corrida**
  (`ClasificarMensajesPendientes.cs:107-115`, `Abortada: true`). El razonamiento es correcto —la
  unidad de trabajo queda con cambios pendientes que la próxima confirmación arrastraría, rompiendo
  el "o los dos, o ninguno"— y el test lo assertea así (`:142`).
- **W3 — Bloque 4, contrato del schema.** El spec declara
  `{ "monto", "tipo": "ingreso"|"egreso", "categoria" }`. El código emite `entro`/`salio` y
  reordena a `categoria, monto, tipo` (`EsquemaClasificacion.cs:23-25,42-64`). Es el cambio que
  llevó la accuracy de 72% a 94%.
- **W6 — Bloque 6, *completion criterion*.** El spec manda
  `dotnet test --filter Categoria=Integracion`, comando que **no funciona**: `Directory.Build.props`
  impone `tests.runsettings` con `TestCaseFilter=Categoria!=Integracion` y VSTest combina ambos con
  AND. Se resolvió con `integracion.runsettings`; el spec no se actualizó.
- **W4 / W5 — Dos *completion criteria* incumplidos tal como están escritos.**
  *"`FuenteMensajesTelegram` es el único tipo que importa `Telegram.Bot`"* → también lo importa
  `AgregarTelegramExtensions.cs:5`. *"`ClasificadorOllama` es el único que importa `OllamaSharp`"* →
  también `PromptClasificacion.cs:2` y `AgregarClasificadorExtensions.cs:2`. Inofensivo (todo dentro
  de `Infrastructure`), pero el criterio literal no se cumple.

### Código

- **W10 — `OpcionesTelegram.ChatAutorizado` no lo consume nadie en producción.**
  `OpcionesTelegram.cs:6`. `FuenteMensajesTelegram` sólo usa `.Token`; el filtro de chat lo hace
  `IngestarMensajes` con el primitivo que le inyecta `AgregarTelegram`. La propiedad sobrevive por
  su propio `ToString()` y por los tests. O se usa, o se saca.

---

## W-VER-01 / W-VER-03 — Código muerto y tests frágiles

Revisión archivo por archivo: **cero usings sin usar, cero código comentado, cero métodos
huérfanos**. Sin dependencias de orden, sin estado global, sin timestamps del sistema (`RelojFijo`),
sin IDs hardcodeados. Bases SQLite in-memory con nombre GUID por test (`BaseDePruebas.CrearAsync`);
los tests de filesystem usan `Path.GetTempPath()` + GUID con limpieza en `finally`. Única excepción:
W9.

Dos detalles que vale registrar como patrón a repetir: `Identidad.ConId` resuelve por reflexión
**en el borde de los dobles** la necesidad de Ids en tests de dominio, en vez de abrirle un setter
público a la entidad; y `RepositorioMensajesEnMemoria.ObtenerPendientesAsync` replica el filtro real
(`!Procesado && !Error`) en vez de devolver la lista cruda — sin eso,
`EjecutarAsync_MensajeYaProcesado_NoLoVuelveAClasificar` sería una mentira.

---

## Resultados

```
F-VER-01 trazabilidad AC → test    ✅ 14/14
F-VER-02 tareas del spec           ✅ 6/6 bloques
F-VER-03 cobertura                 ✅ líneas 92.4% · ramas 81.9% · métodos 86.7%
F-VER-04 sad paths                 ✅ toda superficie de entrada
F-VER-05 lint / type checker       ✅ 0 errores, 0 warnings
F-VER-06 tests comprometidos       ✅ 57/57

Tests: 111 (109 unitarios + 2 integración), 0 fallidos
SAST (fase CODE): PASSED — docs/daw/security/sast-FEAT-001a.md

Total: 36 PASS · 11 WARN · 0 FAIL
Resultado: PASSED → gates.verify = true
```

## Pendientes recomendados, por prioridad

1. **W7** — testear el tope de 8 KB. Única regla de *Input validation* del spec sin cobertura, y
   sobre la entrada menos confiable del sistema.
2. **W8** — cubrir `RepositorioCategoriasEfCore` y arreglar el test de prompt que promete más de lo
   que verifica.
3. **W1** — agregar `"$10.000 sueldo de julio"` al dataset etiquetado.
4. **W2 / W3 / W6** — decidir qué hacer con el spec desincronizado: actualizarlo por bucle
   correctivo a PLAN, o dejar el desvío asentado por escrito.
5. **W9, W10, W11, W12** — test acoplado al puerto 11435, propiedad muerta, rama sin documentar,
   ruta por defecto sin test.

---

# Ronda 2 — tras el bucle correctivo (2026-08-02)

**Veredicto:** ✅ **PASSED** — W7 y W8 cerrados y verificados. 0 FAIL.

La ronda 1 ya había dado PASSED; el bucle a CODE lo pidió el usuario para cerrar los dos huecos de
cobertura antes de release, no por un gate en rojo. Esto queda asentado porque explica por qué hay
una ronda 2 sobre una verificación que no había fallado.

## Qué cambió

Sólo tests. `git diff -- src/` vacío entre la ronda 1 y la 2: el código productivo es byte a byte
el mismo. 9 casos nuevos + 1 reescrito; suite 111 → 120.

| Test | Cubre |
|---|---|
| `ClasificarAsync_RespuestaMayorAOchoKb_DevuelveNoDisponible` | W7 — el tope |
| `ClasificarAsync_RespuestaJustoPorDebajoDeOchoKb_DevuelveClasificado` | W7 — el otro lado del borde |
| `ClasificarAsync_TextoVacioOEnBlanco_LanzaArgumentException` (×2) | W7 — precondición |
| `ClasificarAsync_TextoMayorAlMaximo_LanzaArgumentOutOfRangeException` | W7 — precondición |
| `LeerAsync_MaximoMenorAUno_LanzaArgumentOutOfRangeException` (×2) | W7 — precondición |
| `ObtenerActivasAsync_ConActivasYDesactivadas_DevuelveSoloLasActivas` | W8a — FR-08 |
| `ObtenerActivasAsync_TodasDesactivadas_DevuelveListaVacia` | W8a — sad path |
| `ConstruirSystemPrompt_CategoriasRecibidas_EnumeraExactamenteEsasConSuDescripcion` | W8b — reescrito |

## Verificación de que los huecos se cerraron

No basta con que suba el promedio: se verificó archivo por archivo.

| Archivo | Ronda 1 | Ronda 2 |
|---|---|---|
| `RepositorioCategoriasEfCore.cs` | **0%** (0/9) | **100%** (9/9) |
| `ClasificadorOllama.cs` | 93.7% (104/111) | **95.5%** (106/111) |
| `RepositorioMensajesEfCore.cs` | 100% | 100% |
| `RepositorioMovimientosEfCore.cs` | 100% | 100% |

La rama del tope de 8 KB (`ClasificadorOllama.cs:110`) figuraba como `L110(1/2)` en la lista de
ramas incompletas de la ronda 1 y **ya no aparece**. Los tres adaptadores EF de repositorio quedan
en 100%.

## F-VER-03 — Cobertura, ronda 2

| | Ronda 1 | Ronda 2 | |
|---|---|---|---|
| Líneas | 92.4% | **93.9%** (692/737) | ✅ |
| Ramas | 81.9% | **82.6%** (119/144) | ✅ |
| Métodos | 86.7% | **86.7%** (39/45) | ✅ |

Infrastructure: líneas 90.9% → 93.0%, ramas 76.4% → 77.4%. Las ramas de Infrastructure siguen
debajo del 80% por el mismo motivo de la ronda 1 —los `if (OperatingSystem.IsWindows())` de las
ACL, incubrables desde una sola máquina— y el agregado sobre `src/` cumple el umbral.

## Gates re-ganados

```
/daw-test         120 tests (51 Domain + 67 Infrastructure + 2 integración), 0 fallidos
                  accuracy 94.0 % (47/50) — idéntica a la ronda 1: la medición es reproducible
                  latencia p90 0.66 s (p99 7.39 s, outlier de modelo frío; el gate es p90)
type checker      0 errores, 0 advertencias
/daw-security-sast ronda 2: 0 vulnerabilidades (ver docs/daw/security/sast-FEAT-001a.md)
```

## Calidad de la evidencia

Los tests nuevos se validaron por **mutación deliberada** del código productivo, revertida en el
acto y verificada de forma independiente por el orquestador (`git diff --stat -- src/` vacío):

- Sacar `.Where(c => c.Activa)` → los 2 tests de FR-08 fallan.
- `MaximoRespuesta = int.MaxValue` y quitar las guardas → 6 tests fallan.
- `MaximoRespuesta = 0` → falla el test del borde inferior.
- Agregar una categoría inventada a `ConstruirSystemPrompt` → **el test viejo seguía en verde**;
  el reescrito falla. Ésa es la prueba de que W8b no era una objeción de estilo.

## Hallazgo nuevo, diferido a ticket propio

**El spec declara un log que no está implementado.** Bloque 4 → *Error handling*: *"Respuesta que no
es JSON parseable → `NoDisponible`. Se loguea el cuerpo truncado, sin el texto del mensaje."*
`ClasificadorOllama` no recibe `ILogger` y no loguea nada. La ronda 1 no lo detectó: verificó que
devolviera `NoDisponible` —que lo hace— y el "se loguea" pasó de largo. Lo encontró el implementador
del bucle correctivo, mirando el mismo código desde la obligación de escribir un test.

Consecuencia operativa: hoy una respuesta cortada por el tope de 8 KB es **indistinguible de Ollama
caído** desde afuera; las dos terminan en `NoDisponible` y en silencio.

Decisión del usuario: **no se arregla en este ticket.** Es observabilidad, no corrección —nada se
clasifica mal por su ausencia— pero es código productivo que cambia el constructor de
`ClasificadorOllama`, toca `AgregarClasificador` y tiene implicancias de M-03 (que el log no filtre
el texto del mensaje). Va como ticket propio después de cerrar FEAT-001a.

Otros dos hallazgos del mismo origen, menores: el tope de 8 KB compara **caracteres, no bytes**
(en UTF-8 con acentos deja pasar hasta ~16 KB reales, mientras el spec dice "8 KB"), y `Categoria`
no expone método de desactivación, así que FR-08 filtra por un flag que ningún camino del dominio
puede poner en `false` (coherente con el alcance: RF-16/RF-28 son de otro sub-ticket).

## Warnings que siguen abiertos

W1, W2, W3, W4, W5, W6, W9, W10, W11, W12 — sin cambios respecto de la ronda 1. Ninguno bloquea.
Los tres del spec desincronizado (W2, W3, W6) siguen siendo la deuda de mayor valor: el código está
bien, el documento miente, y arreglarlo exige un bucle a PLAN.

```
Ronda 2 — Resultado: PASSED → gates.verify = true
```

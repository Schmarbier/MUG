# Implementation Plan: PersonalFinance — visor de finanzas personales

**Branch**: `modulo-4` | **Date**: 2026-07-18 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-personal-finance-viewer/spec.md`

## Summary

Visor de finanzas personales mono-usuario. Un bot lee los mensajes del chat autorizado del dueño,
un modelo local los clasifica como movimientos (monto, tipo, categoría, moneda) y un visor web
muestra el resumen mensual agrupado por categoría y moneda, más las pantallas de administración
de categorías, monedas, errores y correcciones.

El enfoque técnico se apoya en tres decisiones que salen de los principios de la constitución y
del trabajo de clarificación previo:

1. **La IA vive detrás de un puerto** (Principio II). El dominio recibe una clasificación o una
   falla tipada, nunca un JSON ni un cliente HTTP. Todas las reglas de negocio se testean sin
   Ollama levantado.
2. **El dinero se almacena en unidades menores** como INTEGER y se opera en `decimal`. FR-038 a
   FR-041 exigen exactitud de centavos, y SQLite no tiene tipo decimal; guardar dinero como TEXT
   rompe el orden de FR-015a y como REAL rompe la precisión.
3. **La incertidumbre se deriva a revisión humana, nunca se rellena** (Principio III). Toda
   respuesta del modelo que no valide contra el esquema se convierte en un mensaje con error
   visible en la bandeja, con la única excepción que FR-008 autoriza expresamente: moneda ausente
   asume la base ARS.

## Technical Context

**Language/Version**: C# / .NET 10 (SDK 10.0.302 verificado en el entorno)

**Primary Dependencies**: EF Core 10 + proveedor SQLite · Telegram.Bot (canal de mensajes) ·
OllamaSharp (cliente del modelo) · Blazor Web App con render mode Static SSR

**Storage**: SQLite en ruta absoluta `%LOCALAPPDATA%\PersonalFinance\personalfinance.db`,
compartida por ambos procesos. Montos y tipos de cambio en INTEGER (unidades menores) vía
`ValueConverter`.

**Testing**: xUnit. Unitarias de dominio sin infraestructura; integración de persistencia contra
SQLite en archivo temporal; integración del adaptador de IA contra servidor simulado.

**Target Platform**: Escritorio Windows para el desarrollo del dueño; ambos procesos son
multiplataforma. Ollama corre aparte, local.

**Project Type**: Dos procesos .NET —un servicio alojado de ingesta y una aplicación web— sobre
un dominio compartido.

**Performance Goals**: Clasificación de un mensaje < 5 s p90 (SC-002). Resumen mensual < 1 s p95
(SC-003), medido sobre el volumen de referencia de R7. Acierto de clasificación ≥ 80% sobre el
conjunto etiquetado, con el criterio estricto de R8 (SC-001).

**Constraints**: Mono-usuario, sin autenticación. El bot no responde por Telegram. Ruta de base de
datos absoluta, obligatoria por constitución. Deduplicación por identificador de mensaje del canal
antes de clasificar o persistir. Sin integración con fuentes externas de cotización.

**Scale/Scope**: Volumen de referencia 24 meses × 300 movimientos/mes = 7.200 movimientos, 20
categorías, 3 monedas (R7). 6 historias de usuario, 57 requisitos funcionales, 5 pantallas.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Gate | Estado pre-Phase 0 | Estado post-Phase 1 |
|---|---|---|---|
| **I. Test-First (TDD)** | Cada tarea de implementación tiene su test escrito antes, que falla primero. Test projects existen desde la primera tarea. | ✅ Planificado | ✅ R9 define la pirámide; el orden test→código se hace explícito en `/speckit-tasks` |
| **II. Aislamiento de la lógica de IA** | Prompts, parseo, cliente y timeouts viven en un módulo dedicado. El negocio se testea sin modelo real. | ✅ Planificado | ✅ Puerto definido en `contracts/clasificador.md`; el catálogo se pasa como entrada para que el adaptador no toque persistencia |
| **III. Fidelidad a la fuente de verdad** | Ninguna salida del modelo se persiste sin validar. Ante ambigüedad, se deriva a revisión humana. | ✅ Planificado | ✅ El puerto devuelve `Clasificacion` o `Falla`, sin tercer camino. Única excepción autorizada: FR-008 (moneda ausente → ARS) |
| **IV. Gestión de secretos** | Sin secretos en código ni repo. Todo por `IConfiguration`. Sin `.env`. | ✅ Planificado | ✅ R10 |

**Restricciones técnicas adicionales**

| Restricción | Cómo se cumple |
|---|---|
| Ruta de persistencia absoluta y estable | R10: ruta bajo `%LOCALAPPDATA%`, compartida, con override por cadena de conexión |
| Deduplicación por identificador de mensaje | Índice único sobre `IdentificadorCanal`, además de la verificación previa. El índice es la garantía real frente a la carrera entre los dos disparadores de ingesta (R4) |

**Resultado del gate**: ✅ **Sin violaciones.** La sección Complexity Tracking queda vacía a
propósito.

## Project Structure

### Documentation (this feature)

```text
specs/001-personal-finance-viewer/
├── plan.md              # Este archivo
├── research.md          # Phase 0 — 10 decisiones técnicas con alternativas descartadas
├── data-model.md        # Phase 1 — entidades, validaciones, transiciones de estado, índices
├── quickstart.md        # Phase 1 — guía de validación end-to-end
├── contracts/
│   ├── clasificador.md  # Puerto de IA + esquema de respuesta del modelo
│   └── visor.md         # Páginas, render modes y contrato de cada operación
├── checklists/
│   ├── requirements.md          # Calidad general de la spec (16/16)
│   └── integridad-financiera.md # Compuerta financiera (43/43 resueltos)
└── tasks.md             # Phase 2 — lo genera /speckit-tasks, NO este comando
```

### Source Code (repository root)

```text
src/
├── PersonalFinance.Domain/          # Entidades, puertos, servicios de negocio puros
│   ├── Entidades/                   # Mensaje, Movimiento, Categoria, Moneda
│   ├── Puertos/                     # IClasificadorDeMensajes, repositorios
│   └── Servicios/                   # Clasificación, resumen mensual, ciclo de vida
├── PersonalFinance.Infrastructure/  # Adaptadores: EF Core + SQLite, cliente Ollama
│   ├── Persistencia/                # DbContext, configuraciones, converters, migraciones
│   └── IA/                          # Adaptador OllamaSharp: prompts, esquema, timeouts
├── PersonalFinance.Bot/             # Servicio alojado: ingesta Telegram + barrido periódico
└── PersonalFinance.Web/             # Blazor Web App (Static SSR + componentes interactivos)

tests/
├── PersonalFinance.Domain.Tests/          # Unitarias, sin infraestructura
├── PersonalFinance.Infrastructure.Tests/  # Persistencia contra SQLite temporal; adaptador de IA contra servidor simulado
└── PersonalFinance.Web.Tests/             # Componentes del visor y contrato de las páginas
```

**Structure Decision**: cuatro proyectos de producción con el dominio en el centro, sin
dependencias salientes hacia infraestructura.

Esta separación no es decoración arquitectónica: la exige el **Principio II**. Para que la lógica
de negocio se pueda testear sin invocar un modelo real, el negocio tiene que depender de una
abstracción y no del cliente de Ollama. Eso obliga a que el puerto viva donde vive el negocio
(`Domain`) y su implementación afuera (`Infrastructure`). Lo mismo aplica a la persistencia, que
además debe ser compartida por dos procesos que corren por separado.

`Bot` y `Web` son dos procesos independientes porque cumplen ciclos de vida distintos —uno corre
permanentemente consumiendo el canal, el otro atiende pedidos del navegador— y AGENTS.md ya los
define así. Comparten el archivo SQLite, nunca memoria.

## Phase 0 — Research

Completa. Ver [research.md](./research.md). Diez decisiones, todas con alternativas descartadas y
su motivo:

| # | Decisión |
|---|---|
| R1 | Montos y tipos de cambio en INTEGER (unidades menores) con `ValueConverter` |
| R2 | Agregación del resumen en memoria; redondeo único con empate hacia arriba |
| R3 | Puerto de clasificación en el dominio; prompts y JSON solo en el adaptador |
| R4 | Long polling + barrido periódico de 60 s; índice único para deduplicar |
| R5 | Zona horaria `America/Argentina/Buenos_Aires`; se persiste UTC (resuelve CHK034) |
| R6 | Static SSR para el resumen; interactividad por componente en administración |
| R7 | Volumen de referencia para SC-003: 7.200 movimientos (resuelve CHK037) |
| R8 | Acierto de SC-001 = los cuatro atributos correctos (resuelve CHK038) |
| R9 | Pirámide de pruebas xUnit en tres niveles |
| R10 | Secretos por `IConfiguration`; ruta absoluta de SQLite |

Dos decisiones merecen atención especial porque corrigen defaults que fallarían en silencio:

- **R2**: el redondeo por defecto de .NET es bancario (`ToEven`). Con él, el escenario US2 AC-3.b
  da $2.930,12 donde la spec exige $2.930,11. Hay que pedir `AwayFromZero` explícitamente.
- **R4**: sin el barrido periódico, FR-010a es incumplible justo en el escenario más probable —el
  clasificador caído sin tráfico nuevo—: el mensaje nunca alcanzaría los 3 intentos que FR-010b
  necesita para hacerlo visible en la bandeja.

## Phase 1 — Design & Contracts

Completa.

- **[data-model.md](./data-model.md)**: cuatro entidades persistidas más la vista derivada del
  resumen. Incluye reglas de validación trazadas al requisito que las origina, transiciones de
  estado de `Mensaje`, `Categoria` y `Moneda`, y los índices con su motivo.
- **[contracts/clasificador.md](./contracts/clasificador.md)**: el puerto de IA, el esquema JSON
  que debe devolver el modelo, el mapeo de cada falla al motivo de error persistido, y los casos
  que el adaptador debe cubrir en tests.
- **[contracts/visor.md](./contracts/visor.md)**: cinco páginas con su render mode, el contrato de
  datos del resumen con sus invariantes, y qué rechaza cada operación de administración.
- **[quickstart.md](./quickstart.md)**: cómo validar la feature de punta a punta.

**Re-evaluación del Constitution Check post-diseño**: ✅ sin violaciones. Ver la tabla de la
sección Constitution Check, columna post-Phase 1.

## Complexity Tracking

> Fill ONLY if Constitution Check has violations that must be justified

Sin violaciones que justificar. No hay entradas.

## Riesgos y trabajo pendiente

| Riesgo | Mitigación |
|---|---|
| Calidad de clasificación por debajo del 80% de SC-001 | La descripción de cada categoría viaja en el prompt como señal discriminante; toda baja confianza deriva a la bandeja en vez de ensuciar el resumen (FR-011) |
| Ollama no disponible de forma prolongada | Reintento acotado y degradación a error visible (FR-010a/b). Ningún mensaje queda en limbo invisible, que es lo que SC-006 exige |
| Divergencia de la base entre procesos | Ruta absoluta obligatoria por constitución; una ruta relativa produciría dos archivos distintos |
| Precisión monetaria | R1 y R2, con US2 AC-3.b como test que falla ruidosamente si el redondeo se hace en el lugar equivocado |

**Checklist de integridad financiera**: 43/43 ítems resueltos (ver
[checklists/integridad-financiera.md](./checklists/integridad-financiera.md)). Este plan resolvió
CHK034, CHK037 y CHK038 vía R5, R7 y R8; el resto se cerró en pasadas posteriores de
`/speckit-clarify` sobre la spec, sin que ninguna alterara el modelo de datos ni la estructura de
proyectos definidos acá — `data-model.md` y `contracts/visor.md` ya referencian los FR resultantes
(FR-012a, FR-015a, FR-017a, FR-018a, FR-020a, FR-021a, FR-023a, FR-035a–f).

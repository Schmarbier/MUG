# Tasks: PersonalFinance — visor de finanzas personales

**Input**: Design documents from `/specs/001-personal-finance-viewer/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/clasificador.md](./contracts/clasificador.md), [contracts/visor.md](./contracts/visor.md)

**Tests**: La constitución del proyecto exige Test-First (Principio I, NO NEGOCIABLE). Las tareas de test de abajo son OBLIGATORIAS, no opcionales, y deben completarse — y confirmarse en rojo — antes de su tarea de implementación correspondiente.

**Organization**: Las tareas se agrupan por historia de usuario (spec.md) para permitir implementación y prueba independiente de cada una.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo (archivos distintos, sin dependencias pendientes)
- **[Story]**: A qué historia de usuario pertenece la tarea (US1..US6)
- Cada descripción incluye la ruta exacta de archivo

## Path Conventions

Cuatro proyectos de producción + tres de test, según `plan.md` § Project Structure:

```text
src/PersonalFinance.Domain/{Entidades,Puertos,Servicios}/
src/PersonalFinance.Infrastructure/{Persistencia,IA}/
src/PersonalFinance.Bot/
src/PersonalFinance.Web/Components/Pages/
tests/PersonalFinance.Domain.Tests/
tests/PersonalFinance.Infrastructure.Tests/
tests/PersonalFinance.Web.Tests/
```

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Inicialización de la solución y los proyectos

- [X] T001 Crear `PersonalFinance.sln` y los cuatro proyectos de producción (`src/PersonalFinance.Domain`, `src/PersonalFinance.Infrastructure`, `src/PersonalFinance.Bot`, `src/PersonalFinance.Web`) con las referencias de proyecto del plan: Domain sin dependencias salientes; Infrastructure → Domain; Bot → Domain + Infrastructure; Web → Domain + Infrastructure
- [X] T002 Crear los tres proyectos de test xUnit (`tests/PersonalFinance.Domain.Tests` → Domain; `tests/PersonalFinance.Infrastructure.Tests` → Infrastructure; `tests/PersonalFinance.Web.Tests` → Web + Domain) y agregar los siete proyectos a `PersonalFinance.sln`
- [X] T003 [P] Agregar el paquete NuGet `Microsoft.EntityFrameworkCore.Sqlite` a `src/PersonalFinance.Infrastructure`
- [X] T004 [P] Agregar el paquete NuGet `Telegram.Bot` a `src/PersonalFinance.Bot`
- [X] T005 [P] Agregar el paquete NuGet `OllamaSharp` a `src/PersonalFinance.Infrastructure`
- [X] T006 [P] Agregar el paquete NuGet `bunit` a `tests/PersonalFinance.Web.Tests` para testear componentes Razor (verificar licencia antes de sumarlo, per R9)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Entidades, persistencia compartida y configuración que TODAS las historias necesitan

**⚠️ CRITICAL**: Ninguna historia de usuario puede empezar hasta que esta fase esté completa

### Tests Foundational (MANDATORY — Test-First) ⚠️

> Escribir estos tests PRIMERO, confirmar que fallan antes de implementar

- [X] T007 [P] Test de integración: `PersonalFinanceDbContext` crea el esquema con los índices únicos de `data-model.md` § Índices (`Categoria.Titulo`, `Moneda.Codigo`, `Mensaje.IdentificadorCanal`) — un insert duplicado sobre cualquiera de los tres lanza excepción — en `tests/PersonalFinance.Infrastructure.Tests/Persistencia/EsquemaTests.cs`
- [X] T008 [P] Test de integración: el `ValueConverter` de `Monto`/`TipoDeCambio` hace round-trip decimal↔INTEGER sin pérdida para valores con 2 decimales (R1) — en `tests/PersonalFinance.Infrastructure.Tests/Persistencia/ConvertersTests.cs`
- [X] T009 Test de integración: la migración inicial deja sembrada la moneda ARS con `EsBase = true`, `Activa = true` y `TipoDeCambio = null` (FR-032) — en `tests/PersonalFinance.Infrastructure.Tests/Persistencia/SeedTests.cs`
- [X] T010 [P] Test de integración: el resolutor de cadena de conexión arma la ruta absoluta `%LOCALAPPDATA%\PersonalFinance\personalfinance.db` y respeta un override explícito (R10, restricción de la constitución) — en `tests/PersonalFinance.Infrastructure.Tests/Persistencia/ConexionSqliteTests.cs`

### Implementación Foundational

- [X] T011 [P] Crear entidad `Categoria` (Id, Titulo, Descripcion, Activa) en `src/PersonalFinance.Domain/Entidades/Categoria.cs`
- [X] T012 [P] Crear entidad `Moneda` (Id, Codigo, EsBase, Activa, TipoDeCambio) en `src/PersonalFinance.Domain/Entidades/Moneda.cs`
- [X] T013 [P] Crear entidad `Mensaje` (Id, IdentificadorCanal, Texto, FechaRecepcionUtc, Procesado, IntentosClasificacion, TieneError, MotivoError) en `src/PersonalFinance.Domain/Entidades/Mensaje.cs`
- [X] T014 [P] Crear enum `TipoMovimiento` y entidad `Movimiento` (Id, MensajeId, CategoriaId, MonedaId, Monto, Tipo, Fecha, TipoDeCambioHistorico) en `src/PersonalFinance.Domain/Entidades/TipoMovimiento.cs` y `src/PersonalFinance.Domain/Entidades/Movimiento.cs`
- [X] T015 Definir los puertos de repositorio `ICategoriaRepositorio`, `IMonedaRepositorio`, `IMensajeRepositorio`, `IMovimientoRepositorio` en `src/PersonalFinance.Domain/Puertos/` (depende de T011-T014)
- [X] T016 Crear `PersonalFinanceDbContext` con los cuatro `DbSet` en `src/PersonalFinance.Infrastructure/Persistencia/PersonalFinanceDbContext.cs` (depende de T011-T014)
- [X] T017 [P] Configurar las entidades vía Fluent API con los índices únicos y no únicos de `data-model.md` § Índices en `src/PersonalFinance.Infrastructure/Persistencia/Configuraciones/` (depende de T016; hace pasar T007)
- [X] T018 [P] Implementar `MontoValueConverter` y `TipoDeCambioValueConverter` (decimal↔long, factor ×100) en `src/PersonalFinance.Infrastructure/Persistencia/Converters/` (hace pasar T008)
- [X] T019 Generar la migración inicial de EF Core con el seed de ARS en `src/PersonalFinance.Infrastructure/Persistencia/Migraciones/` (depende de T016-T018; hace pasar T009)
- [X] T020 Implementar el resolutor de cadena de conexión absoluta con override en `src/PersonalFinance.Infrastructure/Persistencia/ConexionSqlite.cs` (hace pasar T010)
- [X] T021 [P] Implementar los adaptadores EF Core de los cuatro puertos de repositorio en `src/PersonalFinance.Infrastructure/Persistencia/Repositorios/` (depende de T015, T017, T019)
- [X] T022 [P] Configurar `TelegramBotToken`, `TelegramChatAutorizado` y `OLLAMA_MODEL` vía `IConfiguration` con placeholders vacíos/`0` en `src/PersonalFinance.Bot/appsettings.json` y `src/PersonalFinance.Web/appsettings.json` (Principio IV)
- [X] T023 [P] Implementar el helper de zona horaria `America/Argentina/Buenos_Aires` (persistir UTC, convertir a local al derivar la fecha del movimiento) en `src/PersonalFinance.Domain/Servicios/ZonaHorariaLocal.cs` (R5)

**Checkpoint**: Fundación lista — las historias de usuario pueden empezar, incluso en paralelo

---

## Phase 3: User Story 1 - Registro automático de movimientos desde mensajes (Priority: P1) 🎯 MVP

**Goal**: El bot lee los mensajes del chat autorizado, los guarda una sola vez y los clasifica automáticamente en el mismo ciclo, sin intervención del dueño.

**Independent Test**: Enviar mensajes desde el chat autorizado y verificar que quedan movimientos creados con los atributos correctos, que un mensaje repetido no genera un segundo registro y que un mensaje de otro chat se descarta.

### Tests for User Story 1 (MANDATORY — Test-First) ⚠️

> Escribir estos tests PRIMERO, confirmar que fallan antes de implementar

- [ ] T024 [P] [US1] Test de dominio: un mensaje de un chat distinto al autorizado se descarta sin guardarse (FR-002, AC-2) en `tests/PersonalFinance.Domain.Tests/Servicios/IngestaServicioTests.cs`
- [ ] T025 [P] [US1] Test de dominio: una `Clasificacion` exitosa del puerto crea un `Movimiento` con categoría, tipo, monto y moneda del resultado (FR-005, FR-006, FR-007, AC-4, AC-5, AC-6) en `tests/PersonalFinance.Domain.Tests/Servicios/ClasificacionServicioTests.cs`
- [ ] T026 [P] [US1] Test de dominio: un mensaje sin moneda explícita asigna ARS y sin tipo de cambio histórico (FR-008, AC-4, AC-5) en `tests/PersonalFinance.Domain.Tests/Servicios/ClasificacionServicioTests.cs`
- [ ] T027 [P] [US1] Test de dominio: una `Falla` de motivo `ClasificadorNoDisponible` incrementa `IntentosClasificacion` y mantiene el mensaje pendiente mientras el contador sea menor a 3 (FR-010a) en `tests/PersonalFinance.Domain.Tests/Servicios/ClasificacionServicioTests.cs`
- [ ] T028 [P] [US1] Test de dominio: al tercer intento fallido el mensaje pasa a error "clasificador no disponible" y queda visible para reproceso (FR-010b, AC-4.a) en `tests/PersonalFinance.Domain.Tests/Servicios/ClasificacionServicioTests.cs`
- [ ] T028a [P] [US1] Test de dominio: una `Falla` de motivo `SinConfianza` marca el mensaje con error y motivo "no se pudo determinar la categoría con confianza" de inmediato, sin incrementar el contador de intentos ni reintentar (a diferencia de `ClasificadorNoDisponible`) (FR-011) en `tests/PersonalFinance.Domain.Tests/Servicios/ClasificacionServicioTests.cs`
- [ ] T029 [P] [US1] Test de dominio: un mensaje del que se creó un movimiento queda marcado como procesado (FR-009, AC-7) en `tests/PersonalFinance.Domain.Tests/Servicios/ClasificacionServicioTests.cs`
- [ ] T030 [P] [US1] Test de dominio: sin ninguna categoría activa, el mensaje va a error sin invocar el puerto de clasificación (Edge Case) en `tests/PersonalFinance.Domain.Tests/Servicios/ClasificacionServicioTests.cs`
- [ ] T030a [P] [US1] Test de dominio: una categoría desactivada no se incluye en `CategoriasActivas` pasado al clasificador y nunca se asigna a un mensaje nuevo, aunque sí siga apareciendo en los movimientos ya creados (FR-031, AC-8 US1) en `tests/PersonalFinance.Domain.Tests/Servicios/ClasificacionServicioTests.cs`
- [ ] T031 [P] [US1] Test de integración: el adaptador Ollama traduce cada caso —respuesta válida, JSON malformado, campo faltante, categoría inexistente, confianza bajo el umbral, timeout, servidor caído— a la `Falla` correspondiente, nunca a una clasificación parcial, contra un servidor simulado (contracts/clasificador.md § Verificación) en `tests/PersonalFinance.Infrastructure.Tests/IA/OllamaClasificadorAdapterTests.cs`
- [ ] T032 [P] [US1] Test de integración: reingerir el mismo `IdentificadorCanal` no duplica el mensaje ni cambia la cantidad almacenada (FR-004, AC-3, SC-007) en `tests/PersonalFinance.Infrastructure.Tests/Persistencia/IngestaServicioTests.cs`

### Implementation for User Story 1

- [ ] T033 [P] [US1] Definir el puerto `IClasificadorDeMensajes` y los tipos `Clasificacion`/`Falla`/`MotivoFalla` de `contracts/clasificador.md` en `src/PersonalFinance.Domain/Puertos/IClasificadorDeMensajes.cs`
- [ ] T034 [US1] Implementar `ClasificacionServicio` (categorías/monedas activas como entrada, default ARS, mapeo de fallas a motivo persistido, contador de intentos) en `src/PersonalFinance.Domain/Servicios/ClasificacionServicio.cs` (depende de T033; hace pasar T025-T030, T030a, T028a)
- [ ] T035 [US1] Implementar `OllamaClasificadorAdapter` (prompt JSON estricto, esquema, validación, timeout acotado, temperatura baja) en `src/PersonalFinance.Infrastructure/IA/OllamaClasificadorAdapter.cs` (depende de T033; hace pasar T031)
- [ ] T036 [US1] Implementar `IngestaServicio` (filtra por `TelegramChatAutorizado`, guarda con deduplicación) en `src/PersonalFinance.Bot/Ingesta/IngestaServicio.cs` (depende de T021, T022; hace pasar T024, T032)
- [ ] T037 [US1] Implementar `IngestaTelegramBackgroundService` (long polling de Telegram.Bot, guarda y dispara clasificación en el mismo ciclo — FR-005a) en `src/PersonalFinance.Bot/IngestaTelegramBackgroundService.cs` (depende de T034, T036)
- [ ] T038 [US1] Implementar `BarridoClasificacionBackgroundService` (barrido cada 60 s que clasifica pendientes existan o no mensajes nuevos — R4) en `src/PersonalFinance.Bot/BarridoClasificacionBackgroundService.cs` (depende de T034)
- [ ] T039 [US1] Conectar ambos `BackgroundService` y sus dependencias (DbContext, repositorios, adaptador Ollama, configuración) en `src/PersonalFinance.Bot/Program.cs`

**Checkpoint**: User Story 1 funcional de punta a punta — validar con quickstart.md § V1, V2, V3, V9

---

## Phase 4: User Story 2 - Resumen mensual de ingresos y egresos (Priority: P1) 🎯 MVP

**Goal**: El dueño abre el visor y ve el resumen del mes, agrupado por categoría y moneda, en dos bloques separados que paginan de forma independiente.

**Independent Test**: Cargar un conjunto conocido de movimientos del mes y verificar los totales por categoría y moneda, la separación entre ingresos y egresos, y la paginación de cada bloque.

### Tests for User Story 2 (MANDATORY — Test-First) ⚠️

> Escribir estos tests PRIMERO, confirmar que fallan antes de implementar

- [ ] T040 [P] [US2] Test de dominio: tres egresos ARS de la categoría "Hogar" agrupan en una fila con el total sumado (AC-1) en `tests/PersonalFinance.Domain.Tests/Servicios/ResumenMensualServicioTests.cs`
- [ ] T041 [P] [US2] Test de dominio: un egreso y un ingreso de igual monto y categoría no se netean entre bloques (FR-014, AC-4) en `tests/PersonalFinance.Domain.Tests/Servicios/ResumenMensualServicioTests.cs`
- [ ] T042 [P] [US2] Test de dominio: el equivalente de una fila en moneda extranjera es la suma de los equivalentes individuales, cada uno con su propio tipo de cambio histórico, no un tipo de cambio único aplicado al total (FR-013, AC-3.a) en `tests/PersonalFinance.Domain.Tests/Servicios/ResumenMensualServicioTests.cs`
- [ ] T043 [P] [US2] Test de dominio: sumar 1465,0555 + 1465,0555 y redondear una sola vez con empate hacia arriba da $2.930,11, no $2.930,12 (FR-040, AC-3.b) en `tests/PersonalFinance.Domain.Tests/Servicios/ResumenMensualServicioTests.cs`
- [ ] T044 [P] [US2] Test de dominio: las filas de un bloque se ordenan descendente por equivalente en base, con desempate alfabético por categoría y luego moneda, paginadas de a 4, y la secuencia es idéntica entre dos consultas consecutivas (FR-015, FR-015a, AC-2, AC-2.a, AC-2.b) en `tests/PersonalFinance.Domain.Tests/Servicios/ResumenMensualServicioTests.cs`
- [ ] T044a [P] [US2] Test de dominio: el total general de un bloque es la suma de los equivalentes en moneda base de TODAS las filas del mes, y no varía al cambiar de página (FR-012a, AC-5) en `tests/PersonalFinance.Domain.Tests/Servicios/ResumenMensualServicioTests.cs`
- [ ] T045 [P] [US2] Test de dominio: un mes sin movimientos muestra ambos bloques presentes con totales en cero (Edge Case) en `tests/PersonalFinance.Domain.Tests/Servicios/ResumenMensualServicioTests.cs`
- [ ] T046 [P] [US2] Test de dominio: un bloque con menos de 4 filas produce una única página sin controles de navegación activos (Edge Case) en `tests/PersonalFinance.Domain.Tests/Servicios/ResumenMensualServicioTests.cs`
- [ ] T047 [P] [US2] Test de componente: la página `/` renderiza los dos bloques con paginación independiente, el total general de cada bloque, y respeta las invariantes de `contracts/visor.md` § Resumen mensual en `tests/PersonalFinance.Web.Tests/Paginas/ResumenMensualPaginaTests.cs`

### Implementation for User Story 2

- [ ] T048 [US2] Implementar `ResumenMensualServicio` (agregación en memoria, `decimal.Round(..., MidpointRounding.AwayFromZero)` una sola vez, orden y desempate, paginación de 4 filas, total general por bloque sobre todas las filas del mes) en `src/PersonalFinance.Domain/Servicios/ResumenMensualServicio.cs` (depende de T021; hace pasar T040-T046, T044a)
- [ ] T049 [US2] Implementar la página `/` en modo Static SSR consumiendo `ResumenMensualServicio` con número de página por bloque y el total general de cada bloque en `src/PersonalFinance.Web/Components/Pages/ResumenMensual.razor` (depende de T048; hace pasar T047)

**Checkpoint**: User Stories 1 y 2 = MVP completo — validar con quickstart.md § V4, V5, V6, V7

---

## Phase 5: User Story 3 - Gestión del ciclo de vida de categorías (Priority: P2)

**Goal**: El dueño crea, lista, edita, elimina, desactiva y reactiva categorías desde el visor.

**Independent Test**: Crear, editar, eliminar, desactivar y reactivar categorías desde la pantalla `/categorias`, sin depender de la ingesta de mensajes.

### Tests for User Story 3 (MANDATORY — Test-First) ⚠️

> Escribir estos tests PRIMERO, confirmar que fallan antes de implementar

- [ ] T050 [P] [US3] Test de dominio: crear una categoría con título único la deja activa con ese título y descripción (AC-1) en `tests/PersonalFinance.Domain.Tests/Servicios/CategoriaServicioTests.cs`
- [ ] T051 [P] [US3] Test de dominio: crear o editar con un título ya existente se rechaza con error, también contra categorías desactivadas (FR-024, FR-026, AC-2, AC-4) en `tests/PersonalFinance.Domain.Tests/Servicios/CategoriaServicioTests.cs`
- [ ] T052 [P] [US3] Test de dominio: eliminar una categoría sin movimientos la borra; con movimientos la desactiva en lugar de borrarla (FR-028, FR-029, AC-6, AC-7) en `tests/PersonalFinance.Domain.Tests/Servicios/CategoriaServicioTests.cs`
- [ ] T053 [P] [US3] Test de dominio: editar el título o la descripción de una categoría desactivada conserva su estado "desactivada" (FR-026, AC-9) en `tests/PersonalFinance.Domain.Tests/Servicios/CategoriaServicioTests.cs`
- [ ] T054 [P] [US3] Test de dominio: reactivar una categoría desactivada la vuelve disponible para clasificar nuevos movimientos (FR-030, FR-031, AC-10) en `tests/PersonalFinance.Domain.Tests/Servicios/CategoriaServicioTests.cs`
- [ ] T055 [P] [US3] Test de componente: la página `/categorias` permite crear, editar, eliminar, desactivar y reactivar, mostrando el estado de cada una en `tests/PersonalFinance.Web.Tests/Paginas/CategoriasPaginaTests.cs`

### Implementation for User Story 3

- [ ] T056 [US3] Implementar `CategoriaServicio` (crear, editar, eliminar/desactivar, reactivar, unicidad de título) en `src/PersonalFinance.Domain/Servicios/CategoriaServicio.cs` (depende de T021; hace pasar T050-T054)
- [ ] T057 [US3] Implementar la página InteractiveServer `/categorias` en `src/PersonalFinance.Web/Components/Pages/Categorias.razor` (depende de T056; hace pasar T055)

**Checkpoint**: gestión de categorías operativa de punta a punta — validar con quickstart.md § V10

---

## Phase 6: User Story 4 - Bandeja de mensajes con error y reproceso (Priority: P2)

**Goal**: El dueño ve los mensajes que no se pudieron convertir en movimiento, con su motivo, y los reprocesa tras corregir la causa.

**Independent Test**: Forzar mensajes con error conocido (sin monto, sin descripción, con moneda no cargada), verificar que aparecen en el listado con su motivo y que al corregir y reprocesar quedan procesados.

### Tests for User Story 4 (MANDATORY — Test-First) ⚠️

> Escribir estos tests PRIMERO, confirmar que fallan antes de implementar

- [ ] T058 [P] [US4] Test de dominio: mensajes sin monto, sin descripción o con moneda no soportada quedan con el motivo de error correspondiente (FR-010, AC-1, AC-2, AC-3) en `tests/PersonalFinance.Domain.Tests/Servicios/BandejaErroresServicioTests.cs`
- [ ] T059 [P] [US4] Test de dominio: reprocesar un mensaje en error tras corregir la causa lo deja procesado con su movimiento, sin duplicar uno ya creado (FR-017, AC-5, Edge Case de reproceso) en `tests/PersonalFinance.Domain.Tests/Servicios/BandejaErroresServicioTests.cs`
- [ ] T060 [P] [US4] Test de componente: la página `/errores` lista los mensajes con error y su motivo, y permite reprocesarlos en `tests/PersonalFinance.Web.Tests/Paginas/ErroresPaginaTests.cs`

### Implementation for User Story 4

- [ ] T061 [US4] Implementar `BandejaErroresServicio` (listar mensajes en error, reprocesar reinvocando `ClasificacionServicio`) en `src/PersonalFinance.Domain/Servicios/BandejaErroresServicio.cs` (depende de T034; hace pasar T058-T059)
- [ ] T062 [US4] Implementar la página InteractiveServer `/errores` en `src/PersonalFinance.Web/Components/Pages/Errores.razor` (depende de T061; hace pasar T060)

**Checkpoint**: bandeja de errores operativa de punta a punta — validar con quickstart.md § V8

---

## Phase 7: User Story 5 - Corrección manual de movimientos (Priority: P2)

**Goal**: El dueño corrige a mano la categoría, el tipo, el monto o la moneda de un movimiento existente.

**Independent Test**: Sobre movimientos ya existentes, editar cada atributo y verificar el resultado y el impacto en el resumen.

### Tests for User Story 5 (MANDATORY — Test-First) ⚠️

> Escribir estos tests PRIMERO, confirmar que fallan antes de implementar

- [ ] T063 [P] [US5] Test de dominio: editar la categoría o el monto de un movimiento existente lo actualiza sin afectar los demás campos (FR-018, FR-019, AC-1, AC-2) en `tests/PersonalFinance.Domain.Tests/Servicios/MovimientoServicioTests.cs`
- [ ] T063a [P] [US5] Test de dominio: editar el monto de un movimiento a un valor menor o igual a cero se rechaza con error, sin modificar el movimiento (FR-019, FR-038) en `tests/PersonalFinance.Domain.Tests/Servicios/MovimientoServicioTests.cs`
- [ ] T064 [P] [US5] Test de dominio: editar la moneda de un movimiento registra el tipo de cambio vigente de la nueva moneda (FR-020, FR-021, AC-3) en `tests/PersonalFinance.Domain.Tests/Servicios/MovimientoServicioTests.cs`
- [ ] T065 [P] [US5] Test de dominio: editar el tipo de un movimiento lo mueve al otro bloque del resumen sin alterar monto, moneda ni tipo de cambio histórico (FR-018a, AC-4, AC-5) en `tests/PersonalFinance.Domain.Tests/Servicios/MovimientoServicioTests.cs`
- [ ] T066 [P] [US5] Test de componente: `/movimientos/{id}/editar` permite editar categoría, tipo, monto y moneda, y el resumen refleja el cambio sin recálculo manual (SC-008) en `tests/PersonalFinance.Web.Tests/Paginas/EditarMovimientoPaginaTests.cs`

### Implementation for User Story 5

- [ ] T067 [US5] Implementar `MovimientoServicio` (editar categoría, tipo, monto y moneda, registrando el tipo de cambio vigente al cambiar de moneda, rechazando monto ≤ 0) en `src/PersonalFinance.Domain/Servicios/MovimientoServicio.cs` (depende de T021; hace pasar T063-T065, T063a)
- [ ] T068 [US5] Implementar la página InteractiveServer `/movimientos/{id}/editar` en `src/PersonalFinance.Web/Components/Pages/EditarMovimiento.razor` (depende de T067; hace pasar T066)

**Checkpoint**: corrección manual operativa de punta a punta — validar con quickstart.md § V12 (primera mitad)

---

## Phase 8: User Story 6 - Monedas y tipo de cambio histórico (Priority: P3)

**Goal**: El dueño da de alta, edita, elimina/desactiva y reactiva monedas, y corrige a mano el tipo de cambio histórico de un movimiento con propagación opcional.

**Independent Test**: Dar de alta una moneda, crear movimientos con ella, editar su cotización y verificar que los movimientos previos conservan su tipo de cambio histórico.

### Tests for User Story 6 (MANDATORY — Test-First) ⚠️

> Escribir estos tests PRIMERO, confirmar que fallan antes de implementar

- [ ] T069 [P] [US6] Test de dominio: agregar una moneda con código único y tipo de cambio > 0 la deja disponible; un código duplicado o un tipo de cambio ≤ 0 se rechazan (FR-033, FR-039, AC-1, AC-2) en `tests/PersonalFinance.Domain.Tests/Servicios/MonedaServicioTests.cs`
- [ ] T070 [P] [US6] Test de dominio: editar la cotización de una moneda no modifica el tipo de cambio histórico de movimientos ya creados (FR-034, FR-035, AC-3) en `tests/PersonalFinance.Domain.Tests/Servicios/MonedaServicioTests.cs`
- [ ] T070a [P] [US6] Test de dominio: editar la cotización de una moneda a un valor menor o igual a cero se rechaza con error, sin modificar la cotización vigente (FR-034, FR-039) en `tests/PersonalFinance.Domain.Tests/Servicios/MonedaServicioTests.cs`
- [ ] T071 [P] [US6] Test de dominio: eliminar una moneda sin movimientos la borra; con movimientos la desactiva preservando el tipo de cambio histórico; ARS nunca se elimina ni se desactiva (FR-035b, FR-035c, FR-035f, AC-8, AC-9, AC-12) en `tests/PersonalFinance.Domain.Tests/Servicios/MonedaServicioTests.cs`
- [ ] T072 [P] [US6] Test de dominio: una moneda desactivada se trata como "moneda no soportada" al clasificar, y reactivarla la vuelve disponible con su tipo de cambio vigente (FR-035e, AC-10, AC-11) en `tests/PersonalFinance.Domain.Tests/Servicios/MonedaServicioTests.cs`
- [ ] T073 [P] [US6] Test de dominio: editar el tipo de cambio histórico de un movimiento con confirmación de propagación lo aplica a los demás movimientos de igual moneda y fecha; sin confirmar, solo al editado (FR-022, FR-023, AC-6, AC-7) en `tests/PersonalFinance.Domain.Tests/Servicios/MovimientoServicioTests.cs`
- [ ] T073a [P] [US6] Test de dominio: editar el tipo de cambio histórico de un movimiento a un valor menor o igual a cero se rechaza con error, sin modificar el movimiento ni disparar la confirmación de propagación (FR-022, FR-039) en `tests/PersonalFinance.Domain.Tests/Servicios/MovimientoServicioTests.cs`
- [ ] T074 [P] [US6] Test de componente: `/monedas` permite alta, listado, edición de cotización, baja y reactivación en `tests/PersonalFinance.Web.Tests/Paginas/MonedasPaginaTests.cs`

### Implementation for User Story 6

- [ ] T075 [US6] Implementar `MonedaServicio` (crear, editar cotización con rechazo de valores ≤ 0, eliminar/desactivar con excepción de ARS, reactivar) en `src/PersonalFinance.Domain/Servicios/MonedaServicio.cs` (depende de T021; hace pasar T069-T072, T070a)
- [ ] T076 [US6] Extender `MovimientoServicio` con edición del tipo de cambio histórico (rechazando valores ≤ 0) y propagación opcional a movimientos de igual moneda y fecha en `src/PersonalFinance.Domain/Servicios/MovimientoServicio.cs` (depende de T067; hace pasar T073, T073a)
- [ ] T077 [US6] Implementar la página InteractiveServer `/monedas` en `src/PersonalFinance.Web/Components/Pages/Monedas.razor` (depende de T075; hace pasar T074)
- [ ] T078 [US6] Agregar la confirmación de propagación a `/movimientos/{id}/editar` (depende de T068, T076)

**Checkpoint**: todas las historias de usuario son funcionales de forma independiente — validar con quickstart.md § V11, V12 (segunda mitad)

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Validación end-to-end y cierre de criterios medibles que atraviesan todas las historias

- [ ] T079 [P] Ejecutar los doce escenarios de `quickstart.md` § Escenarios de validación (V1–V12) y documentar el resultado de cada uno
- [ ] T080 [P] Verificar SC-001 (≥ 80% de acierto, los cuatro atributos correctos per R8) sobre un conjunto etiquetado de al menos 50 mensajes que cubra todas las categorías
- [ ] T081 [P] Verificar SC-002 (clasificación < 5 s p90) y SC-003 (resumen < 1 s p95) sobre el volumen de referencia de R7 (7.200 movimientos, 20 categorías, 3 monedas)
- [ ] T082 Revisar `checklists/integridad-financiera.md` y cerrar los ítems cubiertos por las tareas anteriores (totales de bloque, propagación de tipo de cambio, corrección de datos, alcance temporal, medibilidad, trazabilidad)
- [ ] T082a [P] Verificar que `src/PersonalFinance.Bot` no registra comandos ni envía respuestas al chat de Telegram bajo ninguna circunstancia —el canal es solo entrada de ingesta— (FR-037)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sin dependencias — puede arrancar de inmediato
- **Foundational (Phase 2)**: depende de Setup — BLOQUEA todas las historias de usuario
- **User Stories (Phase 3-8)**: todas dependen de Foundational; entre ellas:
  - **US1 y US2 (P1)** son independientes entre sí y forman el MVP
  - **US3 (P2)** es independiente de todas las demás
  - **US4 (P2)** reutiliza `ClasificacionServicio` de US1 (T061 depende de T034) — requiere US1 implementada, no solo Foundational
  - **US5 (P2)** es independiente de todas las demás
  - **US6 (P3)** extiende `MovimientoServicio` de US5 (T076 depende de T067) — requiere US5 implementada
- **Polish (Phase 9)**: depende de las historias que se quieran validar; T079-T081 requieren el sistema completo corriendo

### User Story Dependencies

- **US1 (P1)**: después de Foundational — sin dependencias de otras historias
- **US2 (P1)**: después de Foundational — sin dependencias de otras historias (independiente de US1 per spec.md, se prueba cargando movimientos conocidos directamente)
- **US3 (P2)**: después de Foundational — sin dependencias de otras historias
- **US4 (P2)**: después de Foundational **y** de la implementación de US1 (reutiliza `ClasificacionServicio`)
- **US5 (P2)**: después de Foundational — sin dependencias de otras historias
- **US6 (P3)**: después de Foundational **y** de la implementación de US5 (extiende `MovimientoServicio`)

### Within Each User Story

- Los tests se escriben y deben fallar antes de la implementación
- Entidades/puertos antes de servicios
- Servicios antes de páginas
- Historia completa y validada antes de pasar a la siguiente prioridad

### Parallel Opportunities

- Todas las tareas [P] de Setup pueden correr en paralelo
- T007-T010 (tests Foundational) en paralelo entre sí; T011-T014 (entidades) en paralelo entre sí
- Completada Foundational: **US1, US2, US3 y US5** pueden avanzar en paralelo (no dependen entre sí)
- **US4** solo puede empezar su implementación (no sus tests) después de que T034 (US1) exista
- **US6** solo puede empezar su implementación (no sus tests) después de que T067 (US5) exista
- Dentro de cada historia, todos los tests marcados [P] corren en paralelo (archivos distintos)

---

## Parallel Example: User Story 1

```bash
# Lanzar todos los tests de dominio de US1 juntos (deben fallar primero):
Task: "Test de dominio: mensaje de chat no autorizado se descarta en tests/PersonalFinance.Domain.Tests/Servicios/IngestaServicioTests.cs"
Task: "Test de dominio: clasificación exitosa crea Movimiento en tests/PersonalFinance.Domain.Tests/Servicios/ClasificacionServicioTests.cs"
Task: "Test de dominio: moneda ausente asigna ARS en tests/PersonalFinance.Domain.Tests/Servicios/ClasificacionServicioTests.cs"
Task: "Test de dominio: fallo del clasificador reintenta hasta 3 veces en tests/PersonalFinance.Domain.Tests/Servicios/ClasificacionServicioTests.cs"

# Lanzar el test de integración del adaptador de IA y el de deduplicación juntos:
Task: "Test de integración: adaptador Ollama mapea cada fallo a su Falla en tests/PersonalFinance.Infrastructure.Tests/IA/OllamaClasificadorAdapterTests.cs"
Task: "Test de integración: índice único evita duplicar mensajes en tests/PersonalFinance.Infrastructure.Tests/Persistencia/IngestaServicioTests.cs"
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2)

1. Completar Phase 1: Setup
2. Completar Phase 2: Foundational (CRÍTICO — bloquea todas las historias)
3. Completar Phase 3: User Story 1 (ingesta + clasificación automática)
4. Completar Phase 4: User Story 2 (resumen mensual)
5. **DETENER Y VALIDAR**: correr quickstart.md § V1-V7, V9
6. Este es el MVP: el dueño registra gastos por mensaje y ve el resumen

### Incremental Delivery

1. Setup + Foundational → fundación lista
2. US1 + US2 → MVP → validar → demo
3. US3 (categorías) y US5 (corrección manual) → pueden agregarse en paralelo, cada una añade valor sin romper el MVP
4. US4 (bandeja de errores) → requiere US1 completa
5. US6 (monedas) → requiere US5 completa
6. Phase 9 (Polish) → validación end-to-end de los doce escenarios y de los criterios de éxito medibles

### Parallel Team Strategy

Con más de un desarrollador, después de Foundational:

- Developer A: US1 → luego US4 (depende de US1)
- Developer B: US2
- Developer C: US5 → luego US6 (depende de US5)
- Developer D: US3

---

## Notes

- [P] = archivos distintos, sin dependencias pendientes
- [Story] mapea cada tarea a su historia de usuario para trazabilidad
- Cada historia debe quedar completable y testeable de forma independiente, salvo las dependencias explícitas US4→US1 y US6→US5 documentadas arriba
- Verificar que los tests fallan antes de implementar (rojo antes de verde, Principio I)
- Detenerse en cada checkpoint para validar la historia de forma independiente contra quickstart.md

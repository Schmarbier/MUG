# Spec FEAT-001a: Ingesta de mensajes de Telegram y clasificación en movimientos vía Ollama

| Field | Value |
|-------|-------|
| Ticket | FEAT-001a |
| PRD | docs/daw/prd/prd-FEAT-001a.md |
| Tier | FEATURE |
| Date | 2026-07-31 |
| Spec loops | 4 |

## Summary

Se construye desde cero una solución .NET 10 con arquitectura hexagonal en 3 proyectos de
producción (`Domain`, `Infrastructure`, `Bot`) y 2 de test. El dominio define las entidades
`Mensaje`, `Movimiento` y `Categoria`, los puertos que necesita, y dos casos de uso:
`IngestarMensajes` (lee Telegram, filtra por chat autorizado, deduplica por `message_id`) y
`ClasificarMensajesPendientes` (toma los mensajes sin procesar y los convierte en movimientos
usando el clasificador). La infraestructura aporta los adaptadores: EF Core/SQLite para
persistencia, `Telegram.Bot` para la fuente de mensajes y `OllamaSharp` para el clasificador. El
proyecto `Bot` es el único composition root: arma la DI y ejecuta seed → ingesta → clasificación.

El clasificador usa **structured output con JSON schema** para restringir al modelo a las 5
categorías del seed y a los 2 valores de `tipo`. El fallback a `Otros` (FR-09) y el error
`"tipo no reconocido"` (FR-11) se implementan igual, como red por si el schema no alcanza.

## Coverage: PRD → blocks

| Requirement | Covered by |
|---|---|
| FR-01 | Block 3 |
| FR-02 | Block 3 |
| FR-03 | Block 3 |
| FR-04 | Block 3 |
| FR-05 | Block 2 |
| FR-06 | Block 5 |
| FR-07 | Block 5 |
| FR-08 | Block 5 |
| FR-09 | Block 4 |
| FR-10 | Block 5 |
| FR-11 | Block 5 |
| FR-12 | Block 5 |
| NFR-01 | Strategy: el prompt del Bloque 4 enumera las 5 categorías con su descripción y fuerza la salida por JSON schema, de modo que el modelo no puede responder fuera del conjunto válido. El Bloque 6 mide la accuracy sobre un dataset de 50 mensajes etiquetados y falla el test si baja de 80%. |
| NFR-02 | Strategy: una sola llamada al modelo por mensaje, sin reintentos (decisión de DEFINE). El `HttpClient.Timeout` se fija en **15 s, deliberadamente por encima del umbral de 5 s del NFR**: si el timeout fuera 5 s, toda respuesta lenta se convertiría en `NoDisponible` y saldría de la muestra, y el p90 no podría fallar nunca. Con 15 s, un mensaje que tarda 12 s se clasifica correctamente **y hace fallar el test de latencia**, que es el comportamiento buscado. El Bloque 6 mide el p90 sobre los 50 mensajes y falla si alcanza o supera 5 s. |

## Dependencies between blocks

Orden de ejecución estricto: **1 → 2 → 3 → 4 → 5 → 6**.

- **Block 1** no depende de nada. Define entidades y puertos que todos los demás consumen.
- **Block 2** depende de 1 (implementa `IRepositorio*` e `IReloj`).
- **Block 3** depende de 1 (implementa `IFuenteMensajes`) y de 2 (persiste mensajes).
- **Block 4** depende de 1 (implementa `IClasificador`). No depende de 2 ni de 3.
- **Block 5** depende de 1, 2 y 4. El composition root además necesita 3.
- **Block 6** depende de 4 (mide el adaptador real contra Ollama).

**Prerequisito del Block 1 (hallazgo del impact scan):** el disco conserva 754 archivos de `bin/` y
`obj/` de 7 proyectos de la implementación anterior, más
`src/PersonalFinance.Web/PersonalFinance.Web.csproj.user`. Están gitignoreados y no contaminan git,
pero `dotnet build` los reutiliza y da falsa sensación de continuidad. Antes de crear nada:
eliminar `src/*/bin`, `src/*/obj`, `tests/*/bin`, `tests/*/obj` y el `.csproj.user`.

---

## Block 1 — Esqueleto de solución, dominio y puertos

**Files**

- `PersonalFinance.sln` (new) — solución con los 5 proyectos.
- `Directory.Build.props` (new) — `TargetFramework=net10.0`, `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`. Impone transversalmente la convención de `AGENTS.md` → Code conventions, sin depender de que cada `.csproj` la repita.
- `Directory.Packages.props` (new) — gestión centralizada de paquetes con las versiones confirmadas por el impact scan: EF Core `10.0.10`, `Telegram.Bot 22.10.2`, `OllamaSharp 5.4.27`, xUnit. **M-05 (threat model):** pinea explícitamente `SQLitePCLRaw.bundle_e_sqlite3` en **≥ 3.0.3** para cerrar el **CVE-2025-6965**, en vez de heredar la versión que arrastre `Microsoft.EntityFrameworkCore.Sqlite`.
- `src/PersonalFinance.Domain/PersonalFinance.Domain.csproj` (new) — sin `PackageReference` a infraestructura. Solo BCL.
- `src/PersonalFinance.Domain/Entidades/Mensaje.cs` (new) — entidad con estado `Procesado` / `Error` / `Motivo`.
- `src/PersonalFinance.Domain/Entidades/Movimiento.cs` (new)
- `src/PersonalFinance.Domain/Entidades/Categoria.cs` (new)
- `src/PersonalFinance.Domain/Entidades/TipoMovimiento.cs` (new) — `enum { Ingreso, Egreso }`.
- `src/PersonalFinance.Domain/Puertos/IRepositorioMensajes.cs` (new)
- `src/PersonalFinance.Domain/Puertos/IRepositorioMovimientos.cs` (new)
- `src/PersonalFinance.Domain/Puertos/IRepositorioCategorias.cs` (new)
- `src/PersonalFinance.Domain/Puertos/IFuenteMensajes.cs` (new)
- `src/PersonalFinance.Domain/Puertos/IClasificador.cs` (new)
- `src/PersonalFinance.Domain/Puertos/IReloj.cs` (new) — `DateTime UtcNow { get; }`. Existe porque `AGENTS.md` prohíbe llamar al reloj del sistema desde `Domain`.
- `src/PersonalFinance.Domain/Puertos/IUnitOfWork.cs` (new) — `ConfirmarAsync(ct)`. El Bloque 5 exige atomicidad entre `IRepositorioMovimientos` e `IRepositorioMensajes` ("o se guardan los dos, o ninguno"). Esa es una necesidad del **dominio**, así que se declara como puerto: sin él, la garantía quedaría librada a que los repositorios compartan `DbContext` por casualidad de implementación.
- `src/PersonalFinance.Domain/Clasificacion/ResultadoClasificacion.cs` (new) — tipo de retorno que modela los caminos de error del PRD sin excepciones.
- `tests/PersonalFinance.Domain.Tests/PersonalFinance.Domain.Tests.csproj` (new) — referencia únicamente a `Domain`.
- `tests/PersonalFinance.Domain.Tests/MensajeTests.cs` (new)
- `tests/PersonalFinance.Domain.Tests/MovimientoTests.cs` (new)

**Logic**

Las entidades exponen su estado con `private set` y se mutan por métodos con nombre de negocio,
según `AGENTS.md` → Code conventions:

- `Mensaje.MarcarProcesado()` — pone `Procesado = true`. Rechaza si el mensaje ya está en error.
- `Mensaje.MarcarError(string motivo)` — pone `Error = true` y `Motivo`. Rechaza motivo vacío.
- `Movimiento.Crear(mensajeId, categoriaId, monto, tipo, fecha)` — factory que valida invariantes.

`ResultadoClasificacion` es un tipo cerrado con **cinco** casos, uno por camino del PRD:
`Clasificado(monto, tipo, categoria)`, `SinMonto`, `SinDescripcion`, `TipoNoReconocido`,
`NoDisponible`. Ningún camino de error esperado se propaga como excepción.

**Data model**

| Entidad | Campo | Tipo | Constraints |
|---|---|---|---|
| `Mensaje` | `Id` | `long` | PK, autoincrement |
| | `MessageId` | `long` | NOT NULL, **UNIQUE** (clave de deduplicación, FR-04) |
| | `Texto` | `string` | NOT NULL, máx. 4096 |
| | `FechaRecepcion` | `DateTime` | NOT NULL, UTC |
| | `Procesado` | `bool` | NOT NULL, default `false` |
| | `Error` | `bool` | NOT NULL, default `false` |
| | `Motivo` | `string?` | NULL permitido; obligatorio cuando `Error = true`, máx. 200 |
| `Categoria` | `Id` | `int` | PK, autoincrement |
| | `Titulo` | `string` | NOT NULL, **UNIQUE**, máx. 60 |
| | `Descripcion` | `string` | NOT NULL, máx. 200 |
| | `Activa` | `bool` | NOT NULL, default `true` |
| `Movimiento` | `Id` | `long` | PK, autoincrement |
| | `MensajeId` | `long` | NOT NULL, FK → `Mensaje.Id`, **UNIQUE** (un Mensaje produce como máximo un Movimiento) |
| | `CategoriaId` | `int` | NOT NULL, FK → `Categoria.Id` |
| | `Monto` | `decimal(18,2)` | NOT NULL, > 0 |
| | `Tipo` | `int` | NOT NULL, valores del enum `TipoMovimiento` |
| | `FechaCreacion` | `DateTime` | NOT NULL, UTC |

**No existe campo `moneda`.** Es una decisión explícita del PRD (sección Out of Scope) y su deuda
está registrada en `docs/daw/prd/prd-FEAT-001.md`.

**Input validation**

Este bloque no recibe input externo; valida invariantes de dominio:

- `Motivo`: no vacío ni sólo espacios cuando se marca error. Máx. 200 caracteres.
- `Monto`: `decimal` estrictamente mayor que 0.
- `Tipo`: valor definido del enum. Un `int` fuera de rango se rechaza.
- `Texto`: no vacío, máx. 4096 caracteres.

**Error handling**

| Error | Manejo |
|---|---|
| `MarcarError` con motivo vacío o en blanco | `ArgumentException`. Es un error de programación, no un camino del PRD. |
| `MarcarProcesado` sobre un mensaje ya marcado con error | `InvalidOperationException`. Los dos estados son excluyentes. |
| `Movimiento.Crear` con monto ≤ 0 | `ArgumentOutOfRangeException`. |
| `Movimiento.Crear` con `TipoMovimiento` fuera del enum | `ArgumentOutOfRangeException`. |

**Required tests**

- [ ] `MarcarProcesado_MensajeNuevo_QuedaProcesadoTrue` — sustenta AC-06
- [ ] `MarcarError_ConMotivo_QuedaErrorTrueYConMotivo` — sustenta AC-09, AC-10, AC-11
- [ ] `MarcarError_MotivoVacio_LanzaArgumentException` — sad path
- [ ] `MarcarProcesado_MensajeYaConError_LanzaInvalidOperationException` — sad path
- [ ] `Crear_MontoCeroONegativo_LanzaArgumentOutOfRangeException` — sad path
- [ ] `Crear_TipoFueraDelEnum_LanzaArgumentOutOfRangeException` — sad path

**Completion criterion**

`dotnet build` compila los 5 proyectos con `TreatWarningsAsErrors=true` y 0 warnings.
`dotnet test` ejecuta los 6 tests de `PersonalFinance.Domain.Tests` en verde.
`PersonalFinance.Domain.csproj` no contiene ningún `PackageReference` ni `ProjectReference`.

---

## Block 2 — Persistencia EF Core/SQLite y seed de categorías

**Files**

- `src/PersonalFinance.Infrastructure/PersonalFinance.Infrastructure.csproj` (new) — referencia a `Domain`, paquetes EF Core.
- `src/PersonalFinance.Infrastructure/Persistencia/PersonalFinanceDbContext.cs` (new)
- `src/PersonalFinance.Infrastructure/Persistencia/Configuraciones/MensajeConfiguration.cs` (new) — `IEntityTypeConfiguration<Mensaje>`.
- `src/PersonalFinance.Infrastructure/Persistencia/Configuraciones/CategoriaConfiguration.cs` (new)
- `src/PersonalFinance.Infrastructure/Persistencia/Configuraciones/MovimientoConfiguration.cs` (new)
- `src/PersonalFinance.Infrastructure/Persistencia/RepositorioMensajesEfCore.cs` (new)
- `src/PersonalFinance.Infrastructure/Persistencia/RepositorioCategoriasEfCore.cs` (new)
- `src/PersonalFinance.Infrastructure/Persistencia/RepositorioMovimientosEfCore.cs` (new)
- `src/PersonalFinance.Infrastructure/Persistencia/SeedCategorias.cs` (new) — FR-05.
- `src/PersonalFinance.Infrastructure/Reloj/RelojSistema.cs` (new) — implementa `IReloj`.
- `src/PersonalFinance.Infrastructure/AgregarPersistenciaExtensions.cs` (new) — `AgregarPersistencia(this IServiceCollection, string? cadenaConexion = null)`.
- `src/PersonalFinance.Infrastructure/Persistencia/UnitOfWorkEfCore.cs` (new) — implementa `IUnitOfWork` compartiendo el mismo `DbContext` con los tres repositorios dentro del scope de la corrida.
- `tests/PersonalFinance.Infrastructure.Tests/PersonalFinance.Infrastructure.Tests.csproj` (new)
- `tests/PersonalFinance.Infrastructure.Tests/SeedCategoriasTests.cs` (new)
- `tests/PersonalFinance.Infrastructure.Tests/RepositorioMensajesTests.cs` (new)
- `tests/PersonalFinance.Infrastructure.Tests/AgregarPersistenciaExtensionsTests.cs` (new) — tests de la extensión de DI: cadena vacía, directorio inexistente, archivo no accesible, ACL.
- `tests/PersonalFinance.Infrastructure.Tests/UnitOfWorkEfCoreTests.cs` (new)

**Logic**

El mapeo va **exclusivamente** por `IEntityTypeConfiguration<T>`: ninguna entidad de `Domain` lleva
atributos de EF Core, según `AGENTS.md` → Architecture conventions.

`AgregarPersistencia` registra el `DbContext` contra SQLite. La cadena por defecto apunta a
`%LOCALAPPDATA%\PersonalFinance\personalfinance.db` (ruta absoluta, según `AGENTS.md` →
Configuración), creando el directorio si no existe. Acepta override por parámetro para los tests.

**M-06 (threat model):** el directorio y el archivo se crean con ACL restringida al usuario actual.
Es el control compensatorio del riesgo aceptado R-01 (base sin cifrar): si los permisos del sistema
de archivos son la única defensa del historial financiero, tienen que estar puestos a propósito y no
heredados de lo que el sistema tenga por default.

`SeedCategorias.EjecutarAsync` inserta las 5 categorías del seed —`Hogar`, `Ocio`, `Servicios`,
`Sueldo`, `Otros`— con `Activa = true`, **sólo las que no existan** (comparación por `Titulo`). Es
idempotente por diseño: correrlo N veces deja exactamente 5 categorías.

`RepositorioMensajes.ExisteAsync(messageId)` resuelve la deduplicación de FR-04 con una consulta
sobre el índice único de `MessageId`.

**Data model**

Se materializa el modelo declarado en el Block 1. Índices explícitos:

- `IX_Mensaje_MessageId` — UNIQUE, sobre `Mensaje.MessageId`.
- `IX_Mensaje_Procesado_Error` — no único, sobre (`Procesado`, `Error`). Es la consulta que ejecuta
  `ClasificarMensajesPendientes` en cada corrida.
- `IX_Categoria_Titulo` — UNIQUE, sobre `Categoria.Titulo`.
- `IX_Movimiento_MensajeId` — UNIQUE, sobre `Movimiento.MensajeId`.

El esquema se crea con `EnsureCreatedAsync` (no se usan migraciones: es la primera versión del
esquema y el sub-ticket de monedas —FEAT-001f— introducirá las migraciones cuando agregue el campo
`moneda`).

**Input validation**

- `cadenaConexion`: si viene provista, no puede ser vacía ni sólo espacios.
- `Titulo` de categoría en el seed: máx. 60 caracteres, no vacío (validado por la constraint).

**Error handling**

| Error | Manejo |
|---|---|
| El directorio `%LOCALAPPDATA%\PersonalFinance\` no existe | Se crea antes de abrir la conexión. No es un error terminal. |
| El archivo SQLite está bloqueado o no es accesible | `SqliteException` se propaga al composition root, que la loguea y aborta la corrida. Es una falla de infraestructura, no un mensaje inválido. |
| El seed corre con las 5 categorías ya existentes | No inserta nada. No es un error: es el caso normal a partir de la segunda corrida. |
| El seed corre con el seed parcialmente presente | Inserta sólo las faltantes. |
| Violación de la constraint UNIQUE de `Titulo` por carrera entre procesos | `DbUpdateException` capturada dentro del seed; se reintenta la lectura y se continúa. |

**Required tests**

- [ ] `EjecutarAsync_BaseVacia_CreaLasCincoCategoriasActivas` — valida AC-04
- [ ] `EjecutarAsync_SeedYaExistente_DejaLaCantidadEnCinco` — valida AC-05
- [ ] `EjecutarAsync_SeedParcial_InsertaSoloLasFaltantes` — sad path
- [ ] `EjecutarAsync_TituloDuplicadoPorCarrera_NoPropagaDbUpdateException` — sad path del error documentado
- [ ] `AgregarPersistencia_CadenaVacia_LanzaArgumentException` — sad path
- [ ] `ExisteAsync_MessageIdYaGuardado_DevuelveTrue` — sustenta AC-03
- [ ] `Guardar_MessageIdDuplicado_ViolaConstraintUnique` — sad path del índice único
- [ ] `AgregarPersistencia_DirectorioInexistente_LoCreaYAbreLaConexion` — sad path del error documentado
- [ ] `AgregarPersistencia_ArchivoNoAccesible_PropagaSqliteException` — sad path del error documentado
- [ ] `AgregarPersistencia_CreaElArchivoConAclRestringidaAlUsuario` — valida M-06
- [ ] `ConfirmarAsync_FallaAlGuardarElMovimiento_NoPersisteElCambioDeEstadoDelMensaje` — valida la atomicidad que el Bloque 5 exige

**Completion criterion**

Los 11 tests de `PersonalFinance.Infrastructure.Tests` pasan sobre SQLite in-memory (los tres de
ruta usan un directorio temporal descartable, no `%LOCALAPPDATA%`). Los tres repositorios y
`UnitOfWorkEfCore` comparten el mismo `DbContext` dentro del scope de la corrida.
`PersonalFinance.Domain.Tests.csproj` sigue sin referenciar `Infrastructure`.
Ninguna entidad de `Domain` contiene atributos `[Key]`, `[Table]` ni `[Column]`.

---

## Block 3 — Ingesta de mensajes de Telegram

**Files**

- `src/PersonalFinance.Infrastructure/Telegram/FuenteMensajesTelegram.cs` (new) — implementa `IFuenteMensajes` con `Telegram.Bot`.
- `src/PersonalFinance.Infrastructure/Telegram/OpcionesTelegram.cs` (new) — `Token`, `ChatAutorizado`.
- `src/PersonalFinance.Infrastructure/Telegram/AgregarTelegramExtensions.cs` (new) — `AgregarTelegram(this IServiceCollection, string token, long chatAutorizado)`. **Recibe primitivos, NO `IConfiguration`**: `AGENTS.md` → Architecture conventions establece que `Bot` y `Web` son los únicos que leen configuración. Misma forma que `AgregarPersistencia`. Valida el token acá y lanza `ArgumentException` si viene vacío o con formato inválido — es el punto donde el arranque falla si falta el secreto.
- `src/PersonalFinance.Domain/CasosDeUso/IngestarMensajes.cs` (new) — FR-01 a FR-04.
- `tests/PersonalFinance.Domain.Tests/IngestarMensajesTests.cs` (new)
- `tests/PersonalFinance.Infrastructure.Tests/OpcionesTelegramTests.cs` (new)
- `tests/PersonalFinance.Infrastructure.Tests/FuenteMensajesTelegramTests.cs` (new) — tests del adaptador: 401, enmascarado del token en excepciones, updates sin texto, truncado a 4096.
- `tests/PersonalFinance.Infrastructure.Tests/AgregarTelegramExtensionsTests.cs` (new) — tests de la extensión de DI.

**Logic**

`FuenteMensajesTelegram.LeerAsync` usa `GetUpdatesAsync` con offset guardado en un **campo de
instancia** del adaptador, registrado como **singleton** en la DI. No es un `static`: `AGENTS.md` →
Code conventions prohíbe los estáticos con estado, y un offset mutable en un `static` es
exactamente eso. Como singleton, el estado tiene ciclo de vida gestionado y es reemplazable en test.
Devuelve únicamente los updates de tipo mensaje de texto.

`IngestarMensajes.EjecutarAsync` es el caso de uso, y vive en `Domain`:

1. Pide los mensajes a `IFuenteMensajes` (FR-01).
2. Descarta los que no vienen del `ChatAutorizado` **sin guardarlos** (FR-02).
3. Para cada uno restante, consulta `IRepositorioMensajes.ExisteAsync(messageId)`; si ya existe, lo
   descarta (FR-04).
4. Guarda los nuevos con `Procesado = false`, `Error = false`, `Motivo = null` y
   `FechaRecepcion = IReloj.UtcNow` (FR-03).

El bot **no responde** al usuario por Telegram: `AGENTS.md` → Qué NO hacer. Sólo lee.

**M-04 (threat model):** `IngestarMensajes` procesa como máximo **100 mensajes por corrida**. El
resto queda en Telegram para la corrida siguiente (los updates viven 24 h). Sin el límite, una tanda
grande dispara N llamadas al modelo de hasta 15 s cada una y la corrida se vuelve interminable.

**M-03 (threat model):** prohibido loguear el `TelegramBotToken` y el `Mensaje.Texto`. Los logs
identifican mensajes por `MessageId`. `OpcionesTelegram.ToString()` se sobrescribe para enmascarar
el token, y las excepciones de `Telegram.Bot` se re-lanzan con el token removido del mensaje —
`Telegram.Bot` incluye la URL en el texto de sus excepciones, y esa URL lleva el token adentro.

**Input validation**

Todo lo que entra viene de la API de Telegram y es input no confiable:

- `chatId`: debe ser exactamente igual a `ChatAutorizado`. Cualquier otro valor se descarta.
- `ChatAutorizado` configurado en `0`: el bot no ingiere nada (placeholder, según `AGENTS.md`).
- `messageId`: entero positivo. `<= 0` se descarta.
- `Texto`: no nulo, no vacío, máx. 4096 caracteres (límite de Telegram). Se trunca a 4096 si excede.
- Updates que no son mensaje de texto (foto, sticker, audio, edición): se descartan sin guardar.
- `Token`: no vacío. Formato `<digitos>:<alfanumérico>`.

**Error handling**

| Error | Manejo |
|---|---|
| `TelegramBotToken` ausente o vacío | Falla al arrancar con mensaje explícito. No se ingiere nada. |
| `TelegramChatAutorizado` ausente o `0` | El bot arranca, loguea la advertencia y no ingiere ningún mensaje. |
| La API de Telegram no responde (timeout, red, 5xx) | La corrida de ingesta aborta sin guardar nada. Los mensajes siguen en Telegram y se leen en la próxima corrida. No marca error en ningún mensaje. |
| La API devuelve 401 (token inválido) | Falla con mensaje explícito. No reintenta. |
| Update sin texto (foto, sticker) | Se descarta silenciosamente. No es un error. |
| Texto mayor a 4096 caracteres | Se trunca a 4096 y se guarda. |

**Required tests**

- [ ] `EjecutarAsync_MensajeNuevoDelChatAutorizado_LoGuardaNoProcesadoSinError` — valida AC-01
- [ ] `EjecutarAsync_MensajeDeOtroChat_NoLoGuardaNiCreaMovimiento` — valida AC-02
- [ ] `EjecutarAsync_MessageIdYaGuardado_NoDuplicaYMantieneLaCantidad` — valida AC-03
- [ ] `EjecutarAsync_ChatAutorizadoEnCero_NoIngiereNada` — sad path del error documentado
- [ ] `EjecutarAsync_FuenteLanzaExcepcion_NoGuardaNadaYNoMarcaError` — sad path del error documentado
- [ ] `EjecutarAsync_UpdateSinTexto_LoDescartaSinGuardar` — sad path del error documentado
- [ ] `EjecutarAsync_TextoMayorA4096_LoTruncaYLoGuarda` — sad path del error documentado
- [ ] `EjecutarAsync_MessageIdNoPositivo_LoDescarta` — sad path
- [ ] `AgregarTelegram_TokenVacio_LanzaArgumentException` — sad path del error documentado. Vive en `AgregarTelegramExtensionsTests.cs`: la validación del token está en la extensión de DI, no en el record de opciones.
- [ ] `LeerAsync_ApiDevuelve401_FallaConMensajeExplicitoYNoReintenta` — sad path del error documentado
- [ ] `EjecutarAsync_MasDeCienMensajes_ProcesaCienYDejaElRestoParaLaProximaCorrida` — valida M-04
- [ ] `OpcionesTelegram_ToString_EnmascaraElToken` — valida M-03
- [ ] `LeerAsync_ExcepcionDeTelegram_SeRelanzaSinElTokenEnElMensaje` — valida M-03

**Completion criterion**

Los 13 tests pasan. `IngestarMensajesTests` corre en `Domain.Tests` con un doble de
`IFuenteMensajes` — sin red, sin SQLite. `FuenteMensajesTelegram` es el único tipo del repo que
importa `Telegram.Bot`.

---

## Block 4 — Adaptador del clasificador Ollama

**Files**

- `src/PersonalFinance.Infrastructure/Ollama/ClasificadorOllama.cs` (new) — implementa `IClasificador` con `OllamaSharp`.
- `src/PersonalFinance.Infrastructure/Ollama/OpcionesOllama.cs` (new) — `Uri`, `Modelo` (default `llama3.1`, override por `OLLAMA_MODEL`), `Timeout` (**15 s**, ver la estrategia de NFR-02).
- `src/PersonalFinance.Infrastructure/Ollama/EsquemaClasificacion.cs` (new) — JSON schema de la respuesta.
- `src/PersonalFinance.Infrastructure/Ollama/PromptClasificacion.cs` (new) — arma el prompt con las categorías activas.
- `src/PersonalFinance.Infrastructure/Ollama/AgregarClasificadorExtensions.cs` (new) — `AgregarClasificador(this IServiceCollection, Uri uri, string modelo, bool permitirOllamaRemoto = false)`. Primitivos, **no `IConfiguration`**, igual que las otras dos extensiones.
- `tests/PersonalFinance.Infrastructure.Tests/ClasificadorOllamaTests.cs` (new)
- `tests/PersonalFinance.Infrastructure.Tests/PromptClasificacionTests.cs` (new)
- `tests/PersonalFinance.Infrastructure.Tests/AgregarClasificadorExtensionsTests.cs` (new)

**Logic**

`ClasificadorOllama.ClasificarAsync(texto, categoriasActivas, ct)` hace **una sola llamada** al
modelo (sin reintentos — decisión de DEFINE, para no comprometer NFR-02) con `format` seteado al
JSON schema de `EsquemaClasificacion`:

```json
{ "monto": number, "tipo": "ingreso" | "egreso", "categoria": "<enum de las categorías activas>" }
```

El schema restringe `tipo` y `categoria` a valores del conjunto válido, con lo cual el modelo no
puede alucinar fuera de él. Aun así se valida la respuesta al parsearla, porque un schema es una
instrucción al modelo, no una garantía del runtime.

**M-01 (threat model) — anti prompt injection:** el system prompt, con las 5 categorías y sus
descripciones, es **fijo y no admite interpolación**. El texto del mensaje viaja como mensaje de rol
`user`, delimitado, **nunca concatenado dentro del system prompt**. Sumado al JSON schema, un
mensaje del estilo `"ignorá las instrucciones anteriores"` no puede reescribir las reglas ni sacar
la respuesta del conjunto válido.

**M-02 (threat model) — el endpoint no sale de loopback:** `OpcionesOllama.Uri` tiene default
`http://127.0.0.1:11434`. Si se configura un host que **no** es loopback, `AgregarClasificador`
falla al arrancar con mensaje explícito, salvo que se active el flag de opt-in
`PermitirOllamaRemoto`, que además exige una URI `https`. El texto de los mensajes es PII financiera
y viaja en HTTP plano: mientras no salga de la máquina eso es aceptable; apuntando a `0.0.0.0` deja
de serlo, y contra un endpoint que además no tiene autenticación.

Mapeo de la respuesta a `ResultadoClasificacion`:

- Respuesta válida → `Clasificado(monto, tipo, categoria)`.
- `categoria` fuera de las activas → `Clasificado` con **`Otros`** (FR-09).
- `tipo` fuera de `{ingreso, egreso}` → `TipoNoReconocido`.
- `monto` ausente, nulo o ≤ 0 → `SinMonto`.
- Texto sin descripción utilizable → `SinDescripcion`.
- Ollama no responde / timeout / JSON no parseable → `NoDisponible`.

`NoDisponible` es distinto de los demás a propósito: los otros son datos malos del usuario, éste es
una falla de infraestructura, y el Block 5 los trata distinto (FR-12).

**Input validation**

- `texto`: no nulo ni vacío. Máx. 4096.
- `categoriasActivas`: colección no vacía. Si viene vacía se lanza `ArgumentException` — clasificar
  sin categorías es un error de programación, no un caso de negocio.
- Respuesta del modelo: JSON parseable, `monto` numérico positivo, `tipo` y `categoria` dentro del
  conjunto enviado. Máx. 8 KB de respuesta.

**Error handling**

| Error | Manejo |
|---|---|
| Ollama no responde / conexión rechazada | `ResultadoClasificacion.NoDisponible`. No lanza. |
| Timeout de 15 s superado | `NoDisponible`. El umbral está por encima de los 5 s de NFR-02 a propósito: una respuesta lenta debe entrar en la muestra de latencia y hacer fallar AC-14, no desaparecer de ella. |
| Respuesta que no es JSON parseable | `NoDisponible`. Se loguea el cuerpo truncado, sin el texto del mensaje. |
| `categoria` devuelta fuera de las activas | `Clasificado` con `Otros` (FR-09). |
| `tipo` devuelto fuera del enum | `TipoNoReconocido`. |
| `monto` ausente, nulo, no numérico o ≤ 0 | `SinMonto`. |
| `categoriasActivas` vacía | `ArgumentException`. Error de programación. |
| `Uri` configurada a un host no-loopback sin el flag `PermitirOllamaRemoto` | `AgregarClasificador` falla al arrancar con mensaje explícito. M-02. |
| `Uri` no-loopback con opt-in pero esquema `http` | Falla al arrancar: el opt-in exige `https`. M-02. |

**Required tests**

- [ ] `ClasificarAsync_CategoriaFueraDeLasActivas_DevuelveOtros` — valida AC-08
- [ ] `ClasificarAsync_OllamaNoResponde_DevuelveNoDisponible` — sad path del error documentado
- [ ] `ClasificarAsync_TimeoutSuperado_DevuelveNoDisponible` — sad path del error documentado
- [ ] `ClasificarAsync_RespuestaNoParseable_DevuelveNoDisponible` — sad path del error documentado
- [ ] `ClasificarAsync_TipoFueraDelEnum_DevuelveTipoNoReconocido` — sad path del error documentado
- [ ] `ClasificarAsync_MontoAusenteONegativo_DevuelveSinMonto` — sad path del error documentado
- [ ] `ClasificarAsync_SinDescripcion_DevuelveSinDescripcion` — sad path
- [ ] `ClasificarAsync_CategoriasActivasVacia_LanzaArgumentException` — sad path del error documentado
- [ ] `PromptClasificacion_IncluyeLasCategoriasActivasYNoLasDesactivadas` — regresión
- [ ] `PromptClasificacion_TextoDelMensaje_VaComoRolUserYNoEnElSystemPrompt` — valida M-01
- [ ] `PromptClasificacion_TextoConIntentoDeInjection_NoAlteraElSystemPrompt` — sad path de M-01
- [ ] `AgregarClasificador_UriNoLoopbackSinOptIn_FallaAlArrancar` — valida M-02
- [ ] `AgregarClasificador_UriNoLoopbackConOptInYHttp_FallaPorNoSerHttps` — sad path de M-02

**Completion criterion**

Los 13 tests pasan con un `HttpMessageHandler` de doble — no requieren Ollama levantado.
`ClasificadorOllama` es el único tipo del repo que importa `OllamaSharp`.
`Domain` no contiene ninguna referencia a `OllamaSharp` ni a `Microsoft.Extensions.AI`.

---

## Block 5 — Caso de uso de clasificación y composition root

**Files**

- `src/PersonalFinance.Domain/CasosDeUso/ClasificarMensajesPendientes.cs` (new) — FR-06 a FR-08, FR-10 a FR-12.
- `src/PersonalFinance.Bot/PersonalFinance.Bot.csproj` (new)
- `src/PersonalFinance.Bot/Program.cs` (new) — composition root.
- `src/PersonalFinance.Bot/appsettings.json` (new) — `TelegramBotToken` vacío, `TelegramChatAutorizado` en `0`, `OllamaModelo` en `llama3.1`.
- `tests/PersonalFinance.Domain.Tests/ClasificarMensajesPendientesTests.cs` (new)

**Logic**

`ClasificarMensajesPendientes.EjecutarAsync`:

1. Trae los mensajes con `Procesado = false` y `Error = false`.
2. Trae las categorías activas.
3. Por cada mensaje llama a `IClasificador.ClasificarAsync`.
4. Según el `ResultadoClasificacion`:
   - `Clasificado` → crea el `Movimiento` (FR-06, FR-07, FR-08) y llama a `MarcarProcesado()` (FR-10).
   - `SinMonto` → `MarcarError("no contiene monto")` (FR-11).
   - `SinDescripcion` → `MarcarError("no contiene descripcion")` (FR-11).
   - `TipoNoReconocido` → `MarcarError("tipo no reconocido")` (FR-11).
   - `NoDisponible` → **no toca el mensaje** (FR-12). Queda `Procesado = false`, `Error = false` y lo
     levanta la próxima corrida.
5. Confirma con `IUnitOfWork.ConfirmarAsync` **una vez por mensaje**: movimiento y estado del
   mensaje se guardan juntos, o no se guarda ninguno. La garantía es del puerto, no de que los
   repositorios compartan `DbContext` por casualidad.

`Program.cs` arma la DI llamando **sólo** a las extensiones (`AgregarPersistencia`,
`AgregarTelegram`, `AgregarClasificador`), según `AGENTS.md` → Code conventions, y ejecuta en orden:
seed → ingesta → clasificación. Lee la configuración de `IConfiguration` (user-secrets o variables
de entorno). No hardcodea ningún secreto.

**M-03 (threat model):** el resumen de corrida que se loguea por consola informa cantidades y
`MessageId`, nunca el texto de los mensajes ni ningún valor de configuración. El "Mensajes
guardados" que menciona `AGENTS.md` es exactamente eso: un conteo, no un volcado.

**Input validation**

- Los mensajes vienen de la propia base, ya validados en el Block 3.
- Si no hay categorías activas, el caso de uso aborta sin tocar ningún mensaje y loguea la causa: sin
  categorías no hay clasificación posible, y marcar error en todos sería destruir datos recuperables.

**Error handling**

| Error | Manejo |
|---|---|
| `NoDisponible` del clasificador | El mensaje queda intacto (`Procesado = false`, `Error = false`). FR-12. |
| `SinMonto` | `MarcarError("no contiene monto")`, sin movimiento. |
| `SinDescripcion` | `MarcarError("no contiene descripcion")`, sin movimiento. |
| `TipoNoReconocido` | `MarcarError("tipo no reconocido")`, sin movimiento. |
| No hay categorías activas | Aborta la corrida sin tocar mensajes. |
| Falla al persistir el movimiento | `IUnitOfWork.ConfirmarAsync` no confirma: no queda ni movimiento ni estado cambiado. Se continúa con el siguiente mensaje. |

**Required tests**

- [ ] `EjecutarAsync_MensajeDeSueldo_CreaMovimientoIngresoEnSueldoYMarcaProcesado` — valida AC-06
- [ ] `EjecutarAsync_MensajeDeComida_CreaMovimientoEgresoEnHogar` — valida AC-07
- [ ] `EjecutarAsync_SinMonto_MarcaErrorNoContieneMontoYNoCreaMovimiento` — valida AC-09
- [ ] `EjecutarAsync_SinDescripcion_MarcaErrorNoContieneDescripcionYNoCreaMovimiento` — valida AC-10
- [ ] `EjecutarAsync_TipoNoReconocido_MarcaErrorTipoNoReconocidoYNoCreaMovimiento` — valida AC-11
- [ ] `EjecutarAsync_ClasificadorNoDisponible_DejaElMensajeIntacto` — valida AC-12
- [ ] `EjecutarAsync_SinCategoriasActivas_AbortaSinTocarMensajes` — sad path del error documentado
- [ ] `EjecutarAsync_FallaAlPersistirElMovimiento_NoDejaMensajeMarcado` — sad path del error documentado
- [ ] `EjecutarAsync_MensajeYaProcesado_NoLoVuelveAClasificar` — regresión de FR-10

> **Dónde quedó el token ausente.** La primera versión de este spec ponía acá un test
> `Program_TelegramBotTokenAusente_...`, sin proyecto donde vivir: la solución no tiene
> `Bot.Tests` y `Domain.Tests` no puede referenciar el composition root. Con la corrección de la
> firma de `AgregarTelegram` (Bloque 3), la validación del token ocurre **en esa extensión**, y su
> test vive en `AgregarTelegramExtensionsTests`. `Program.cs` sólo lee `IConfiguration` y pasa
> primitivos: no le queda lógica propia que testear.

**Completion criterion**

Los 9 tests pasan con dobles de `IClasificador`, `IUnitOfWork` y los repositorios — sin Ollama, sin
red. `Program.cs` no contiene lógica más allá de leer configuración y encadenar las tres
extensiones.
`dotnet run --project src/PersonalFinance.Bot` arranca, ejecuta seed → ingesta → clasificación y
termina sin excepciones con `TelegramChatAutorizado = 0` (no ingiere nada, que es el comportamiento
documentado).

---

## Block 6 — Accuracy del clasificador y latencia

**Files**

- `tests/PersonalFinance.Infrastructure.Tests/Datos/mensajes-etiquetados.json` (new) — 50 mensajes con su categoría y tipo esperados, cubriendo las 5 categorías del seed.
- `tests/PersonalFinance.Infrastructure.Tests/AccuracyClasificadorTests.cs` (new) — NFR-01.
- `tests/PersonalFinance.Infrastructure.Tests/LatenciaClasificadorTests.cs` (new) — NFR-02.

**Logic**

Estos dos tests corren contra **Ollama real** — son los únicos del spec que lo requieren. Se marcan
con `[Trait("Categoria", "Integracion")]` para poder excluirlos de la corrida por defecto y
ejecutarlos con `dotnet test --filter Categoria=Integracion` con Ollama levantado.

El dataset cubre las 5 categorías con al menos 8 mensajes cada una, e incluye mensajes con la
redacción real del usuario (`"$2.000 comida casa"`, `"Saqué $800 de ahorros"`), no frases de
laboratorio. Un mensaje que cae en `Otros` sin que su etiqueta sea `Otros` **cuenta como error** —
es la degradación que el PRD anticipó en su sección de riesgos.

La latencia se mide por mensaje, se ordena la muestra y se toma el percentil 90. **Los 50 mensajes
entran en la muestra**, incluidos los lentos: el timeout de 15 s del Bloque 4 está por encima del
umbral de 5 s justamente para que una respuesta lenta se mida en vez de convertirse en
`NoDisponible` y desaparecer. Si algún mensaje llegara igualmente a `NoDisponible`, el test falla:
una muestra incompleta no puede sostener una afirmación sobre el p90.

**Input validation**

- El dataset debe tener exactamente 50 entradas, sin `messageId` repetidos, con `categoria` dentro
  del seed y `tipo` dentro del enum. Un test de forma lo verifica antes de medir.

**Error handling**

| Error | Manejo |
|---|---|
| Ollama no está levantado al correr los tests de integración | El test falla con mensaje explícito ("Ollama no responde en {uri} — levantalo con `ollama serve`"), no con un timeout opaco. |
| El dataset no tiene 50 entradas o tiene etiquetas inválidas | El test de forma falla antes de llamar al modelo. |
| Accuracy < 80% | El test falla e informa la matriz de confusión por categoría, para saber cuál falla. |
| p90 ≥ 5 s | El test falla e informa el p50, p90 y p99 medidos. |
| Algún mensaje del dataset devuelve `NoDisponible` | El test de latencia falla: la muestra quedó incompleta y el p90 sobre 49 de 50 no es el p90 pedido. |

**Required tests**

- [ ] `Dataset_TieneCincuentaEntradasValidas_CubriendoLasCincoCategorias` — sad path de forma. Vive en `AccuracyClasificadorTests.cs`, y `LatenciaClasificadorTests.cs` reutiliza el mismo cargador del dataset.
- [ ] `Accuracy_SobreDatasetEtiquetado_EsMayorOIgualA80Porciento` — valida AC-13
- [ ] `Latencia_SobreDatasetEtiquetado_P90MenorA5Segundos` — valida AC-14
- [ ] `Accuracy_OllamaNoDisponible_FallaConMensajeExplicito` — sad path del error documentado
- [ ] `Latencia_AlgunMensajeDevuelveNoDisponible_FallaPorMuestraIncompleta` — sad path del error documentado

**Completion criterion**

`dotnet test --filter Categoria=Integracion` con Ollama levantado y `llama3.1` descargado: los 2
tests de integración (`Accuracy_SobreDatasetEtiquetado` y `Latencia_SobreDatasetEtiquetado`) pasan.
Los otros 3 tests del bloque usan dobles y corren en la suite normal. `dotnet test` sin filtro
(Ollama apagado) sigue en verde porque excluye los de integración.

---

## Rollback

Es la primera versión del esquema y no hay datos productivos que preservar, así que el rollback es
trivial y no requiere migración inversa:

1. Revertir los commits de la rama `feat/FEAT-001a-telegram-ingesta-clasificacion`.
2. Borrar el archivo `%LOCALAPPDATA%\PersonalFinance\personalfinance.db`.

**Indicador para aplicarlo:** que la accuracy medida en el Block 6 quede tan por debajo de 80% que
el clasificador no sea usable, y se decida rediseñar el prompt o cambiar de modelo antes de
persistir movimientos mal categorizados.

## Final verification

- Los 12 FR del PRD tienen bloque asignado y los 14 AC tienen al menos un test que los nombra.
- `dotnet build` compila con `TreatWarningsAsErrors=true` y 0 warnings.
- `dotnet test` (sin filtro) pasa: **55 tests unitarios** (6 + 11 + 13 + 13 + 9 + 3), sin red ni Ollama.
- **Cada test listado tiene un archivo declarado en la sección Files de su bloque.** Un test prometido sin archivo donde vivir es un test que no se va a escribir.
- Las tres extensiones de DI (`AgregarPersistencia`, `AgregarTelegram`, `AgregarClasificador`) reciben **primitivos**, no `IConfiguration`. `grep` de `IConfiguration` bajo `src/PersonalFinance.Infrastructure/` no devuelve resultados.
- `dotnet test --filter Categoria=Integracion` pasa con Ollama levantado: **2 tests** — accuracy ≥ 80%, p90 < 5 s.
- Todo error documentado en la tabla "Error handling" de un bloque tiene su test en ese mismo bloque
  (F-SPEC-16), incluidos los cuatro que faltaban en el primer loop: directorio inexistente y archivo
  bloqueado (Bloque 2), 401 de Telegram (Bloque 3) y token ausente al arrancar (Bloque 3, en
  `AgregarTelegram` — se movió del Bloque 5 al corregir la firma de la extensión).
- `PersonalFinance.Domain.csproj` no tiene `PackageReference` ni `ProjectReference` a nada.
- Ninguna entidad de `Domain` tiene atributos de EF Core; el mapeo está en
  `IEntityTypeConfiguration<T>`.
- `grep` de `Microsoft.EntityFrameworkCore`, `Telegram.Bot` y `OllamaSharp` bajo
  `src/PersonalFinance.Domain/` no devuelve resultados.
- No existe campo `moneda` en `Movimiento` — la deuda está registrada en `prd-FEAT-001.md` a nombre
  de FEAT-001f.
- Las 6 mitigaciones del threat model (`docs/daw/security/threat-FEAT-001a.md`) están incorporadas y
  cada una tiene test: M-01 y M-02 en el Bloque 4, M-03 en los Bloques 3 y 5, M-04 en el Bloque 3,
  M-05 en el Bloque 1, M-06 en el Bloque 2.
- `grep` de `SQLitePCLRaw` en `Directory.Packages.props` devuelve una versión ≥ 3.0.3 (CVE-2025-6965).

## Deuda que este spec deja registrada

- `AGENTS.md` → "Cómo correr" documenta `dotnet run --project src/PersonalFinance.Web`. Este ticket
  no crea ese proyecto (está fuera del alcance del PRD), así que ese comando no va a funcionar hasta
  FEAT-001b. Corresponde actualizarlo en el DEFINE de ese sub-ticket.
- El esquema se crea con `EnsureCreatedAsync`, sin migraciones. FEAT-001f, que agrega el campo
  `moneda`, deberá introducir EF Core Migrations y una migración inicial que refleje este esquema.
- **`AGENTS.md` → Code conventions define dos categorías de error** (caminos del PRD → valor de
  retorno; fallas de infraestructura → excepción) y este spec usa una tercera que el documento no
  contempla: **precondiciones de API** (`ArgumentException` por motivo vacío, monto ≤ 0, tipo fuera
  del enum, `categoriasActivas` vacía). Es el idiom estándar de .NET para invariantes y está
  documentado caso por caso, pero conviene asentarlo por escrito en `AGENTS.md` para que no quede
  como zona gris en los próximos sub-tickets. No se puede escribir desde PLAN (ruta prohibida):
  corresponde al DEFINE de FEAT-001b.

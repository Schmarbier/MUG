# AGENTS.md — PersonalFinance

## Propósito
Visor de finanzas personales mono-usuario: un bot lee mensajes de Telegram y un agente los
clasifica como movimientos (ingreso/egreso) por categoría, con resumen mensual.

## Stack
- .NET 10 — dos procesos: `PersonalFinance.Bot` (ingesta Telegram + agente clasificador) y `PersonalFinance.Web` (visor del resumen mensual).
- `PersonalFinance.Web` es una **Blazor Web App con render mode Static SSR** por defecto. El resumen se renderiza en el server (sin circuito ni WASM); las futuras pantallas interactivas (recategorizar/editar movimientos — RF-16, RF-28, RF-32) se habilitan por componente con `@rendermode InteractiveServer`, sin reescribir.
- EF Core 10 + SQLite (persistencia).
- Telegram.Bot (canal de mensajes) · OllamaSharp (cliente del modelo).
- Ollama corre aparte: modelo `llama3.1` local, configurable vía env `OLLAMA_MODEL`.

## Architecture conventions

Arquitectura hexagonal (puertos y adaptadores) sobre 4 proyectos. La regla que manda es
la **dirección de las dependencias**: todo apunta al dominio, el dominio no apunta a nada.

### Proyectos y dirección de dependencias
- `PersonalFinance.Domain` — entidades, value objects, reglas de negocio y **puertos**
  (interfaces). **No referencia ningún otro proyecto de la solución ni ningún paquete de
  infraestructura.** Solo BCL.
- `PersonalFinance.Infrastructure` — **adaptadores**: EF Core/SQLite, cliente de Telegram,
  cliente de Ollama. Referencia a `Domain`. `Domain` nunca referencia a `Infrastructure`.
- `PersonalFinance.Bot` y `PersonalFinance.Web` — *composition roots*. Referencian a `Domain`
  e `Infrastructure`, y son los **únicos** que arman el contenedor de DI y leen `IConfiguration`.

### Puertos y adaptadores
- Los puertos son interfaces **definidas en `Domain`**. El dominio declara qué necesita;
  la infraestructura decide cómo. Nunca al revés.
- Un adaptador por tecnología, en `Infrastructure`, implementando su puerto.
- Los casos de uso viven en `Domain` como servicios de dominio. No hay proyecto
  `Application` separado: la app es mono-usuario y el caso de uso no tiene orquestación
  que justifique una capa propia.

### Prohibiciones (auditables leyendo los `using`)
- `Domain` **no debe** contener `using Microsoft.EntityFrameworkCore`, `using Telegram.Bot`,
  `using OllamaSharp` ni `using Microsoft.Extensions.*`.
- Ninguna entidad de dominio lleva atributos de EF Core (`[Key]`, `[Table]`, `[Column]`).
  El mapeo va por `IEntityTypeConfiguration<T>` en `Infrastructure`.
- `DbContext` no existe fuera de `Infrastructure`.
- `Domain` no hace I/O: ni HTTP, ni disco, ni reloj del sistema directo (el tiempo entra
  por un puerto).

### Vocabulario
El dominio se nombra en castellano, igual que el PRD: `Mensaje`, `Movimiento`, `Categoria`,
`procesado`, `motivo`. Los tipos y miembros de framework quedan en inglés.

### Tests
- `Domain.Tests` **no debe** referenciar `Infrastructure` ni levantar SQLite. Si un test de
  dominio necesita una base, el diseño está mal.
- `Infrastructure.Tests` puede usar SQLite in-memory.

## Configuración / secretos
El token del bot de Telegram **no se commitea**. Se lee de `IConfiguration` con la clave
`TelegramBotToken`, así que puede venir de User Secrets (dev local) o de variable de entorno.
No usar `.env`.

Dev local (una sola vez, desde el proyecto del bot):
```
dotnet user-secrets set "TelegramBotToken" "TU_TOKEN"
```
El valor queda fuera del repo (en `%APPDATA%\Microsoft\UserSecrets\` en Windows, o
`~/.microsoft/usersecrets/` en Linux/Mac). En `appsettings.json` la clave
queda vacía, solo como documentación de que existe. Alternativa: exportar `TelegramBotToken`
como variable de entorno. Ídem `OLLAMA_MODEL`.

Además, el bot solo ingiere mensajes del **chat autorizado del dueño** (RF-02). El id de ese
chat se lee de la clave `TelegramChatAutorizado` (mismo mecanismo: user-secrets o variable de
entorno). En `appsettings.json` queda en `0` como placeholder; con `0` el bot no ingiere nada.

Ambos procesos (`Bot` y `Web`) comparten el archivo SQLite en una ruta **absoluta y estable**:
`%LOCALAPPDATA%\PersonalFinance\personalfinance.db`. No es relativa a propósito: `dotnet run
--project X` usa el directorio del proyecto como working directory, así que una ruta relativa
haría que cada proceso escribiera su propio archivo. Se puede override pasando otra cadena de
conexión a `AgregarPersistencia`.

## Cómo correr
Prerequisito — Ollama levantado con el modelo:
```
ollama serve
ollama pull llama3.1
```
Instalar / restaurar:
```
dotnet restore
```
Levantar (una terminal por proceso):
```
dotnet run --project src/PersonalFinance.Bot   # bot + clasificador
dotnet run --project src/PersonalFinance.Web   # Blazor (Static SSR) — resumen mensual
```
Tests:
```
dotnet test
```

## Qué NO hacer
- El bot NO responde al usuario por Telegram. Solo lee; el "Mensajes guardados" (AC-11) es
  log interno/consola, no un reply. (Fuera de alcance)
- NO agregar sistema de usuarios ni login: la app es mono-usuario. (Fuera de alcance)
- NO re-guardar mensajes ya ingeridos: deduplicar por `message_id` de Telegram y marcar
  `procesado`; el canal es Telegram, no WhatsApp. (Riesgo / Dependencias)

## Domain glossary

- **Mensaje** — lo que llega por Telegram y se guarda tal cual. Lleva `message_id` (clave de
  deduplicación), el texto original, y su estado: `procesado`, `error` y `motivo`.
- **Movimiento** — el registro estructurado que sale de clasificar un Mensaje. Tiene monto,
  `tipo` y Categoria. Un Mensaje produce como máximo un Movimiento.
- **Categoria** — agrupador de Movimientos. Tiene título único y estado (`activa` /
  `desactivada`). Sólo las activas participan de la clasificación.
- **tipo** — `ingreso` o `egreso`. No hay un tercer valor.
- **seed** — las 5 Categorias que el sistema crea al inicializarse: `Hogar`, `Ocio`,
  `Servicios`, `Sueldo` y `Otros`.
- **Otros** — Categoria de descarte: recibe los Movimientos cuya categoría el clasificador no
  supo resolver dentro del seed.
- **clasificador** — el agente que corre sobre Ollama (`llama3.1`) y traduce el texto de un
  Mensaje a `tipo` + Categoria + monto.
## Code conventions

- **Idioma:** el dominio se nombra en castellano (`Mensaje`, `Movimiento`, `procesado`); los
  tipos y miembros de framework, en inglés. Comentarios y mensajes de error, en castellano.
- **Async:** todo método que hace I/O es `async` y termina en `Async`. Prohibido `.Result` y
  `.Wait()`. `CancellationToken` como último parámetro en todos los puertos.
- **Nulabilidad:** `<Nullable>enable</Nullable>` en todos los `.csproj`. Prohibido el operador
  `!` (null-forgiving) para callar al compilador.
- **Inmutabilidad:** los value objects son `record` o `readonly record struct`. Las entidades
  exponen su estado con `private set` y se mutan por métodos con nombre de negocio
  (`MarcarProcesado()`, `MarcarError(motivo)`), nunca por setters públicos.
- **Errores esperados vs excepciones:** los caminos de error del PRD (sin monto, sin
  descripción, tipo no reconocido) se modelan como **valor de retorno**, no como excepción.
  Las excepciones quedan para fallas de infraestructura.
- **Inyección de dependencias:** un método de extensión de `IServiceCollection` por proyecto
  (`AgregarPersistencia`, `AgregarTelegram`, `AgregarClasificador`). El composition root solo
  llama a esas extensiones; no registra servicios sueltos.
- **Sin estáticos con estado.** `DateTime.UtcNow` no se llama directo desde `Domain`: entra
  por puerto.
- **Un tipo público por archivo**, y el archivo se llama como el tipo.

### Tests
- xUnit. Nombre del test: `Metodo_Escenario_ResultadoEsperado`.
- Cada test nombra en su título o en un comentario el **AC del PRD** que cubre. Sin eso,
  VERIFY no puede trazar AC → test (F-VER-01).
- Arrange / Act / Assert separados por línea en blanco. Una aserción conceptual por test.
- Se mockean **puertos**, nunca entidades de dominio.
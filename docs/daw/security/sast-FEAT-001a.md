# SAST — FEAT-001a

**Ticket:** FEAT-001a — Ingesta de mensajes de Telegram y clasificación en movimientos vía Ollama
**Fecha:** 2026-08-02
**Fase:** CODE (closeout)
**Alcance:** diff `main...HEAD` — 100 archivos, 8868 inserciones. Código productivo bajo `src/`
(Domain, Infrastructure, Bot) y tests bajo `tests/`.
**Resultado:** ✅ **PASSED** — 0 Critical, 0 High, 0 Medium.

---

## 1. Secretos hardcodeados (F-SAST-01 — Critical)

✅ **Limpio.**

| Verificación | Resultado |
|---|---|
| `TelegramBotToken` en `appsettings.json` | Vacío (`""`), documentado como placeholder |
| `TelegramChatAutorizado` en `appsettings.json` | `0` — con ese valor el bot no ingiere nada |
| Carga del secreto | `AddUserSecrets` + variables de entorno (`Program.cs:26,29`) |
| `.env` en `.gitignore` | Sí (`.gitignore:7`) — el proyecto además no usa `.env` |
| `*.db` en `.gitignore` | Sí (`.gitignore:485-487`, incluye `-shm` y `-wal`) |
| Archivos sensibles trackeados (`.db`, `.env`, `.pfx`, `.key`) | Ninguno |

**Hallazgo informativo (🟢 no bloquea, no requiere supresión):**
`tests/…/AgregarTelegramExtensionsTests.cs:11` y `tests/…/FuenteMensajesTelegramTests.cs:12`
declaran `const string Token = "123456789:AAHscodigosecretodelbot1234567890abc"`. Es un fixture
sintético cuyo único propósito es satisfacer el regex de formato `^\d+:[A-Za-z0-9_-]+$`; el
literal se autodescribe como falso y no corresponde a ningún bot real. Falso positivo de patrón.

## 2. Inyección

| Regla | Verificación | Resultado |
|---|---|---|
| F-SAST-02 (SQL, Critical) | Todo el acceso a datos va por EF Core con LINQ. Único `CommandText` del repo: `tests/…/SeedCategoriasTests.cs:113`, parametrizado con `AddWithValue("$titulo", …)` | ✅ |
| F-SAST-03 (comandos, Critical) | Sin `Process.Start`, `exec`, `system` en `src/` | ✅ |
| F-SAST-05 (path traversal, High) | La ruta de la base no acepta input de usuario: se compone con `Environment.SpecialFolder.LocalApplicationData` + constantes y se normaliza con `Path.GetFullPath` (`AgregarPersistenciaExtensions.cs:65-75, 95`). El override `CadenaConexion` es configuración del dueño, no input externo | ✅ |

## 3. XSS (F-SAST-06 — High)

✅ **No aplica.** El módulo no tiene superficie HTML: `PersonalFinance.Web` no existe todavía en
este alcance. Sin `innerHTML`, sin render de markup.

## 4. Funciones inseguras y criptografía (F-SAST-04, F-SAST-08, F-SAST-17)

✅ **Limpio.** Sin `BinaryFormatter`, `Assembly.Load`, `Activator.CreateInstance` ni
deserialización insegura. La única deserialización es `JsonDocument.Parse` sobre la respuesta del
modelo (`ClasificadorOllama.cs:139`), envuelta en `try/catch (JsonException)` y acotada a 8 KB
antes de parsear (`MaximoRespuesta`, línea 20/110). No se usa criptografía propia.

## 5. SSRF (F-SAST-07 — High)

✅ **Mitigado explícitamente (M-02 del threat model).**
`AgregarClasificadorExtensions.ValidarEndpoint` (líneas 52-75): el endpoint de Ollama por defecto
es loopback; salir de loopback exige opt-in `PermitirOllamaRemoto` **y** esquema `https`. La URI no
se construye con input de usuario: viene de configuración del dueño.

## 6. Debug en producción (F-SAST-09 — Medium)

✅ **Limpio.** Sin `DeveloperExceptionPage` ni flags de debug. El logging de EF Core está bajado a
`Warning` a propósito (`appsettings.json`), lo que además evita volcar sentencias SQL con datos.

## 7. Logging de datos sensibles (F-SAST-10 — High)

✅ **Mitigado explícitamente (M-03 del threat model).** Las 5 llamadas a log del repo están todas
en `Program.cs` y sólo emiten **conteos**, nunca el texto de los mensajes ni el token:

- `Program.cs:73` — `"Mensajes guardados: {Guardados} (leídos: {Leidos})"`
- `Program.cs:87` — contadores de clasificación
- `Program.cs:63,95` — advertencias operativas sin datos

Dos defensas adicionales, ambas deliberadas:
- `FuenteMensajesTelegram.Sanitizada` (líneas 94-100) reemplaza el token por `***` en el mensaje de
  la excepción y **descarta la inner exception**, porque `Telegram.Bot` mete la URL de la request
  —con el token adentro— en su `Message`. Se paga el stack trace a cambio de que el secreto no
  llegue a un log.
- `AgregarTelegramExtensions:42-45` rechaza un token mal formado **sin incluir el valor recibido**
  en el mensaje de error.

## 8. Upload sin restricción (F-SAST-11) y CSRF (F-SAST-12)

✅ **No aplican.** No hay endpoints HTTP entrantes ni formularios: el bot es un proceso de consola
que hace polling saliente contra Telegram.

## 9. Validación de entrada incompleta (F-SAST-14 — Medium)

✅ **Limpio.**

- Texto entrante truncado a `Mensaje.TextoMaximo` en el borde del adaptador
  (`FuenteMensajesTelegram.Truncar`, línea 102) — el límite no se delega al dominio.
- Updates que no son mensajes de texto se descartan sin guardar (línea 50).
- Respuesta del modelo acotada a 8 KB y validada campo por campo (`ValueKind`, `TryGetDecimal`,
  `TryParse` con `InvariantCulture`) antes de convertirse en dominio.
- Categoría devuelta por el modelo resuelta **contra la lista de activas**, con caída a `Otros`:
  el modelo no puede inventar una categoría que entre al sistema.
- Regex del token (`^\d+:[A-Za-z0-9_-]+$`) sin cuantificadores anidados → sin riesgo de ReDoS.

## 10. Manejo de errores que filtra internals (F-SAST-15 — Medium)

✅ **Limpio.** `Program.cs:81` loguea `excepcion.Message`, no el stack trace, y el `Message` que
puede llegar desde el adaptador de Telegram ya viene sanitizado por `Sanitizada`. Los errores
esperados del PRD se modelan como valor de retorno (`ResultadoClasificacion.*`), no como excepción.

## 11. Dependencias (F-SAST-13 / F-SAST-16)

✅ **Limpio.** `dotnet list package --vulnerable --include-transitive` sobre los 5 proyectos:
sin paquetes vulnerables.

Vale registrar la mitigación **M-05**: `SQLitePCLRaw.bundle_e_sqlite3` está pineado a `3.0.3` por
CVE-2025-6965, y `CentralPackageTransitivePinningEnabled` es lo que hace que ese pin gane sobre la
versión que arrastra `Microsoft.EntityFrameworkCore.Sqlite` (`Directory.Packages.props`).

---

## Riesgo aceptado (arrastrado del threat model)

**R-01 — la base SQLite no está cifrada.** Aceptado en PLAN. Mitigación compensatoria implementada
(**M-06**): `PrepararAlmacenamiento` crea directorio y archivo con ACL restringida al usuario
actual, con herencia deshabilitada en Windows (`SetAccessRuleProtection`) y `0600`/`0700` en Unix
(`AgregarPersistenciaExtensions.cs:83-202`). No es un hallazgo nuevo de este escaneo.

---

## Resumen

```
Total: 11 categorías limpias, 0 vulnerabilidades (0 Critical, 0 High, 0 Medium)
Supresiones: 0
Informativos: 1 (tokens sintéticos en fixtures de test — falso positivo de patrón)
Gate: PASSED → gates.sast = true
```

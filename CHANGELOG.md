# Changelog

Todos los cambios relevantes de PersonalFinance se anotan acá.

El formato sigue [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/) y el versionado,
[Semantic Versioning](https://semver.org/lang/es/). Cada entrada nombra el ticket que la produjo,
así se puede ir de una línea del changelog a su PRD, su spec y su reporte de verificación bajo
`docs/daw/`.

## [Unreleased]

### Added

- **[FEAT-001a]** Ingesta de mensajes de Telegram y clasificación en movimientos vía Ollama.
  Cubre RF-01, RF-02, RF-03, RF-04, RF-06, RF-07, RF-08, RF-10, RF-11, RNF-01 y RNF-02 del PRD
  padre FEAT-001.
  - Ingesta de mensajes del chat autorizado del dueño, con deduplicación por `message_id` de
    Telegram. Los updates que no son mensajes de texto se descartan sin guardar.
  - Clasificación de los mensajes pendientes en movimientos (`monto`, `tipo`, `Categoria`) con el
    modelo `llama3.1` corriendo en Ollama local, usando structured output.
  - Seed de las 5 categorías iniciales —`Hogar`, `Ocio`, `Servicios`, `Sueldo` y `Otros`—
    idempotente: sólo crea las que faltan.
  - Persistencia en SQLite vía EF Core, en una ruta absoluta y estable bajo
    `%LOCALAPPDATA%\PersonalFinance\`, compartida entre los procesos `Bot` y `Web`.
  - Arquitectura hexagonal sobre 4 proyectos: `Domain` no referencia ningún otro proyecto ni
    ningún paquete de infraestructura.

### Security

- **[FEAT-001a]** Mitigaciones del threat model, todas con test propio:
  - El endpoint de Ollama es loopback por defecto; apuntar a un host remoto exige opt-in explícito
    y esquema `https`, porque el texto de los mensajes es información financiera (M-02).
  - Las excepciones de Telegram se re-lanzan sin el token y sin excepción interna: la biblioteca
    incluye la URL de la request —con el token adentro— en el texto de sus excepciones (M-03).
  - Los logs informan cantidades, nunca el texto de los mensajes ni configuración (M-03).
  - `SQLitePCLRaw.bundle_e_sqlite3` pineado en 3.0.3 por CVE-2025-6965, con
    `CentralPackageTransitivePinningEnabled` para que el pin gane sobre la versión transitiva de
    EF Core (M-05).
  - El archivo y el directorio de la base se crean con permisos restringidos al usuario actual —ACL
    sin herencia en Windows, `0600`/`0700` en Unix— porque la base no está cifrada: riesgo R-01
    aceptado, y esos permisos son su única defensa (M-06).

### Known issues

- El spec de FEAT-001a declara que `ClasificadorOllama` loguea el cuerpo truncado ante una respuesta
  no parseable. No está implementado: el adaptador no recibe `ILogger`. Hoy una respuesta cortada
  por el tope de 8 KB es indistinguible de Ollama caído desde afuera —las dos terminan en
  `NoDisponible` y en silencio—. Diferido a ticket propio.
- El spec quedó desincronizado del código en tres puntos, detallados en
  `docs/daw/reports/verify-FEAT-001a.md` (W2, W3 y W6): el manejo de la falla al persistir, el
  vocabulario del schema del modelo, y el comando para correr los tests de integración. El código
  es correcto en los tres casos; el documento es el que quedó viejo.

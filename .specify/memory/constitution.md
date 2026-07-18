<!--
Sync Impact Report
Version change: [TEMPLATE] → 1.0.0 (initial ratification)
Modified principles: n/a (first concrete fill of the template)
Added sections:
  - Core Principles: I. Test-First (TDD) — NON-NEGOTIABLE, II. Aislamiento de la Lógica de IA,
    III. Fidelidad a la Fuente de Verdad (No Fabricar Datos), IV. Gestión de Secretos
  - Restricciones Técnicas Adicionales
  - Flujo de Desarrollo y Calidad
  - Governance
Removed sections:
  - Principle 5 slot (template offered 5; project only requires 4 — no 5th principle defined)
Templates requiring updates:
  - .specify/templates/plan-template.md: ✅ no change needed (Constitution Check gate is generic/dynamic by design)
  - .specify/templates/spec-template.md: ✅ no change needed (no test-optionality or conflicting language)
  - .specify/templates/tasks-template.md: ✅ updated — removed "Tests are OPTIONAL" language, test tasks
    are now mandatory and ordered before implementation tasks per Principle I (Test-First, NON-NEGOTIABLE)
  - .claude/skills/speckit-*/SKILL.md: ✅ reviewed — no agent-specific (CLAUDE-only) stale references found
    requiring generic-guidance fixes
Follow-up TODOs: none
-->

# PersonalFinance Constitution

## Core Principles

### I. Test-First (TDD) — NON-NEGOTIABLE

Los tests se escriben antes que la implementación. Ningún código de producción se escribe sin
un test que lo cubra y que haya fallado primero. El ciclo rojo-verde-refactor es obligatorio:
(1) escribir un test que exprese el comportamiento esperado y confirmar que falla (rojo),
(2) escribir el código mínimo necesario para que pase (verde), (3) refactorizar manteniendo
todos los tests en verde. Un cambio que agrega o modifica lógica de negocio sin tests que la
cubran no puede mergearse.

**Rationale**: Garantiza que el comportamiento quede especificado por tests antes de existir,
evita regresiones silenciosas y mantiene el diseño guiado por casos de uso reales en vez de
suposiciones.

### II. Aislamiento de la Lógica de IA

Toda la lógica de invocación a modelos de IA (prompts, parseo de respuestas del modelo, llamadas
al cliente OllamaSharp, manejo de errores/timeouts del modelo) vive en un módulo dedicado y
aislado, nunca mezclada con la lógica de negocio (clasificación de movimientos, cálculo de
resúmenes, reglas de persistencia). La lógica de negocio debe poder testearse por completo sin
invocar un modelo real, a través de una abstracción del módulo de IA.

**Rationale**: Separar la IA del negocio permite testear las reglas de clasificación de forma
determinística, cambiar de proveedor o modelo sin tocar el dominio, y evita que un cambio de
prompt rompa lógica que no tiene relación con el modelo.

### III. Fidelidad a la Fuente de Verdad (No Fabricar Datos)

El sistema nunca inventa datos que no estén presentes en su fuente de verdad (los mensajes de
Telegram ingeridos y lo efectivamente persistido en SQLite). Ante ambigüedad, información
incompleta o confianza insuficiente para clasificar un movimiento, el sistema deriva el caso a
revisión humana en lugar de asumir una categoría, un monto o una fecha. Ninguna salida del
modelo de IA se persiste como hecho verificado sin pasar por esta regla.

**Rationale**: Es una app financiera mono-usuario; un dato inventado (monto o categoría
incorrecta) corrompe el resumen mensual sin que el usuario lo note. Ser honesto sobre la
incertidumbre vale más que la automatización completa.

### IV. Gestión de Secretos

Ningún secreto (token de Telegram, id de chat autorizado, connection strings sensibles, claves
de API) se hardcodea en el código fuente ni se commitea al repositorio. Los secretos se leen
vía `IConfiguration`, desde User Secrets en desarrollo local o variables de entorno en otros
entornos; `appsettings.json` únicamente documenta la existencia de la clave con un valor vacío
o placeholder neutro, nunca un valor real. No se usan archivos `.env`.

**Rationale**: Evita fugas de credenciales en el historial de git y mantiene el mecanismo de
configuración estándar de .NET como única fuente de secretos, sin introducir un sistema paralelo.

## Restricciones Técnicas Adicionales

- Stack de referencia: .NET, EF Core + SQLite para persistencia, Telegram.Bot para el canal de
  mensajes, OllamaSharp como cliente del modelo (ver AGENTS.md para versiones y detalle de
  procesos).
- La persistencia compartida entre procesos usa siempre una ruta absoluta y estable; una ruta
  relativa al working directory de cada proceso queda prohibida porque produce archivos SQLite
  divergentes entre el bot y el visor.
- El bot no re-procesa mensajes ya ingeridos: la deduplicación por identificador de mensaje del
  canal (Telegram `message_id`) es obligatoria antes de clasificar o persistir.

## Flujo de Desarrollo y Calidad

- Ninguna tarea de implementación (`tasks.md`) puede marcarse completa si sus tests asociados no
  existían antes del código y no pasaron por rojo-verde-refactor (Principio I).
- Toda revisión de código o PR debe verificar explícitamente el cumplimiento de los cuatro
  principios; una violación no justificada bloquea el merge.
- Si una tarea introduce complejidad que viola un principio (por ejemplo, lógica de IA mezclada
  con lógica de negocio, o un dato asumido sin marcarlo para revisión humana), la justificación
  debe quedar registrada en la sección "Complexity Tracking" del plan correspondiente antes de
  implementar.

## Governance

Esta constitución prevalece sobre cualquier otra práctica, convención informal o preferencia
individual dentro del proyecto. Toda enmienda requiere: (1) una propuesta escrita del cambio y
su motivación, (2) la actualización de esta versión siguiendo versionado semántico — MAJOR para
eliminar o redefinir un principio de forma incompatible, MINOR para agregar o expandir
materialmente una sección o principio, PATCH para aclaraciones o correcciones de redacción —, y
(3) la propagación de los cambios a las plantillas dependientes (`plan-template.md`,
`spec-template.md`, `tasks-template.md`) cuando corresponda. Toda revisión de PR debe verificar
cumplimiento de estos principios; la complejidad no justificada debe rechazarse. Usar AGENTS.md
para guía operativa de desarrollo (cómo correr el proyecto, qué está fuera de alcance).

**Version**: 1.0.0 | **Ratified**: 2026-07-18 | **Last Amended**: 2026-07-18

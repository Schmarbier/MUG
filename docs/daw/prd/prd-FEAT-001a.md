# PRD FEAT-001a: Ingesta de mensajes de Telegram y clasificación en movimientos vía Ollama

| Field | Value |
|-------|-------|
| Ticket | FEAT-001a |
| Tracker | none |
| Date | 2026-07-31 |
| PRD loops | 0 |

> Sub-ticket de **PRD-001 — PersonalFinance** (`docs/daw/prd/PRD.md`). Cada FR de este documento
> traza contra el RF del padre indicado entre paréntesis. Los FR sin referencia al padre son
> nuevos, acordados en DEFINE, y deben incorporarse al PRD padre.
>
> **Decisión de scope (DEFINE):** el scope check marcó 14 AC contra una heurística de 5-7 y propuso
> partir en ingesta (FR-01..FR-04) y clasificación (FR-05..FR-12). Se decidió **mantenerlo como un
> solo ticket**: la mitad de ingesta no produce comportamiento observable sin la de clasificación
> (este sub-ticket no incluye pantallas), ambas mitades comparten el mismo modelo de datos, y 8 de
> los 14 AC son caminos de error de un mismo componente. La granularidad se resuelve en PLAN,
> partiendo el spec en bloques dentro de un único pipeline.

## Context and Problem

Hoy los gastos no se registran en ningún lado. Pagar en efectivo, crédito, débito o desde los
ahorros no deja rastro consultable, y registrar cada movimiento a mano es exactamente lo que hace
que nadie lo sostenga más de una semana.

El canal donde el gasto ya se anota naturalmente es Telegram: el dueño se manda un mensaje a sí
mismo del estilo `"$2.000 comida casa"` en el momento en que gasta. Ese mensaje existe, pero es
texto suelto en un chat — no es un dato consultable.

Este sub-ticket cubre la primera mitad del producto: **convertir esos mensajes en movimientos
estructurados**. Leerlos, guardarlos sin duplicar, y que un modelo local los clasifique en tipo
(ingreso/egreso) y categoría. Sin esto no hay nada que resumir, que recategorizar ni que corregir:
es la base sobre la que apoyan todos los demás sub-tickets.

## Goals

- Que un mensaje escrito en Telegram quede convertido en un movimiento con monto, tipo y categoría,
  sin intervención manual.
- Que el bot pueda correrse repetidamente sin duplicar mensajes ni movimientos.
- Que una falla del clasificador no ensucie el estado de los datos ni obligue a recuperar nada a mano.
- Que los mensajes que no pueden convertirse queden marcados con un motivo legible, para que los
  sub-tickets de listado y reproceso tengan de dónde agarrarse.

## Functional Requirements

- FR-01: El sistema debe leer los mensajes enviados al bot de Telegram. *(padre RF-01)*
- FR-02: El sistema debe ingerir únicamente los mensajes provenientes del chat autorizado del dueño, y debe descartar sin guardar los mensajes de cualquier otro chat. *(padre RF-02)*
- FR-03: El sistema debe guardar cada mensaje ingerido con estado inicial `procesado = false` y `error = false`. *(padre RF-03)*
- FR-04: El sistema no debe guardar dos veces el mismo mensaje, identificándolo por el `message_id` de Telegram. *(padre RF-04)*
- FR-05: El sistema debe crear, al inicializarse, las categorías activas `Hogar`, `Ocio`, `Servicios`, `Sueldo` y `Otros`, y no debe duplicarlas en inicializaciones posteriores. *(nuevo — reemplaza RF-05/RF-17 en este sub-ticket)*
- FR-06: El sistema debe crear un movimiento a partir de cada mensaje guardado con `procesado = false` y `error = false`. *(padre RF-06)*
- FR-07: El sistema debe determinar, para cada movimiento, si es de tipo `ingreso` o `egreso` a partir del contenido del mensaje. *(padre RF-07)*
- FR-08: El sistema debe clasificar cada movimiento en una de las categorías activas. *(padre RF-08)*
- FR-09: El sistema debe asignar la categoría `Otros` al movimiento cuando el clasificador devuelve una categoría que no está entre las activas. *(nuevo)*
- FR-10: El sistema debe marcar con `procesado = true` cada mensaje del que se creó un movimiento. *(padre RF-10)*
- FR-11: El sistema debe marcar con `error = true` y un motivo cada mensaje que no puede convertirse en un movimiento, y no debe crear el movimiento en ese caso. *(padre RF-11)*
- FR-12: El sistema debe dejar el mensaje con `procesado = false` y `error = false` cuando el clasificador no responde, para que la siguiente corrida vuelva a tomarlo. *(nuevo)*

## Non-Functional Requirements

- NFR-01: El clasificador debe alcanzar una accuracy mayor o igual a 80% sobre un conjunto de 50 mensajes de prueba etiquetados que cubra las 5 categorías del seed. *(padre RNF-01)*
- NFR-02: El clasificador debe responder en menos de 5 segundos por mensaje, medido en el percentil 90. *(padre RNF-02)*

## Acceptance Criteria

- AC-01: WHEN llega un mensaje nuevo desde el chat autorizado, THE sistema SHALL guardarlo con `procesado = false` y `error = false`. *(FR-01, FR-03)*
- AC-02: IF un mensaje proviene de un chat distinto al autorizado, THEN THE sistema SHALL descartarlo sin guardarlo y sin crear ningún movimiento. *(FR-02)*
- AC-03: IF el sistema recibe un `message_id` que ya está almacenado, THEN THE sistema SHALL no guardar un segundo registro y SHALL mantener sin cambios la cantidad de mensajes almacenados. *(FR-04)*
- AC-04: WHEN el sistema se inicializa sobre una base sin categorías, THE sistema SHALL crear exactamente 5 categorías con estado `activa`: `Hogar`, `Ocio`, `Servicios`, `Sueldo` y `Otros`. *(FR-05)*
- AC-05: IF las 5 categorías del seed ya existen, THEN THE sistema SHALL dejar la cantidad de categorías en 5 al volver a inicializarse. *(FR-05)*
- AC-06: WHEN el clasificador procesa el mensaje `"$10.000 sueldo de julio"`, THE sistema SHALL crear un movimiento con `monto = 10000`, `tipo = ingreso` y `categoria = "Sueldo"`, y SHALL marcar el mensaje con `procesado = true`. *(FR-06, FR-07, FR-08, FR-10)*
- AC-07: WHEN el clasificador procesa el mensaje `"$2.000 comida casa"`, THE sistema SHALL crear un movimiento con `monto = 2000`, `tipo = egreso` y `categoria = "Hogar"`. *(FR-06, FR-07, FR-08)*
- AC-08: IF el clasificador devuelve una categoría que no está entre las activas (por ejemplo `"Transporte"` para el mensaje `"$3.500 nafta"`), THEN THE sistema SHALL crear el movimiento con `categoria = "Otros"`. *(FR-09)*
- AC-09: IF el mensaje no contiene un monto, THEN THE sistema SHALL marcarlo con `error = true` y `motivo = "no contiene monto"`, y SHALL no crear movimiento. *(FR-11)*
- AC-10: IF el mensaje no contiene una descripción, THEN THE sistema SHALL marcarlo con `error = true` y `motivo = "no contiene descripcion"`, y SHALL no crear movimiento. *(FR-11)*
- AC-11: IF el clasificador devuelve un tipo distinto de `ingreso` o `egreso`, THEN THE sistema SHALL marcar el mensaje con `error = true` y `motivo = "tipo no reconocido"`, y SHALL no crear movimiento. *(FR-11)*
- AC-12: IF el clasificador no responde por caída, timeout o error de red, THEN THE sistema SHALL dejar el mensaje con `procesado = false` y `error = false`. *(FR-12)*
- AC-13: WHEN el clasificador procesa un conjunto de 50 mensajes de prueba etiquetados que cubre las 5 categorías del seed, THE sistema SHALL alcanzar una accuracy mayor o igual a 80%. *(NFR-01)*
- AC-14: WHEN el clasificador procesa un mensaje, THE sistema SHALL responder en menos de 5 segundos medido en el percentil 90. *(NFR-02)*

## Out of Scope

- **Categorías**: crear, listar, editar, eliminar, desactivar y reactivar categorías (padre RF-05, RF-17 a RF-23). En este sub-ticket las categorías se crean por seed y no se administran.
- **Monedas y tipo de cambio**: el movimiento **no tiene campo `moneda`** (padre RF-09, RF-24 a RF-32). Todo monto se guarda sin denominación.
- **Resumen mensual**: agrupación por categoría, paginación y su NFR de carga (padre RF-12, RF-13, RNF-03).
- **Listado y reproceso de errores** (padre RF-14, RF-15). Los mensajes con `error = true` quedan almacenados pero no hay pantalla ni mecanismo para verlos o reprocesarlos en este sub-ticket.
- **Recategorización y edición de movimientos** (padre RF-16, RF-28 a RF-31).
- **Interfaz web**: este sub-ticket no agrega ni modifica pantallas de `PersonalFinance.Web`.
- El bot no responde al usuario por Telegram: solo lee.

## Risks and Mitigations

- **Riesgo**: un mensaje con moneda extranjera (`"100 EUR viaje"`) genera un movimiento con el monto pelado (`100`) y queda marcado `procesado = true`, por lo que el mecanismo de reproceso del padre (RF-14, RF-15) nunca lo verá. → **Mitigación**: FR-03 conserva el texto original del mensaje, de modo que el sub-ticket de monedas puede identificarlos buscando menciones de moneda en el texto y corregirlos en una pasada dedicada. **Riesgo asumido y decidido explícitamente en DEFINE.**
- **Riesgo**: el fallback a `Otros` (FR-09) evita el error pero cuenta como clasificación incorrecta al medir NFR-01, y puede acumular movimientos mal categorizados sin que nadie se entere. → **Mitigación**: `Otros` es una categoría visible como cualquier otra; el sub-ticket de recategorización (padre RF-16) permite corregirlos. El conjunto de prueba de AC-13 expone la degradación antes de producción.
- **Riesgo**: `llama3.1` es un modelo local de tamaño acotado clasificando texto libre en castellano rioplatense sobre 5 opciones; puede no alcanzar el 80% de NFR-01. → **Mitigación**: el prompt enumera las 5 categorías con su descripción y restringe el tipo a `ingreso`/`egreso`. Si NFR-01 no se alcanza, la palanca es el prompt, no el scope.
- **Riesgo**: sin RF-15 en scope, los mensajes que quedan con `error = true` (AC-09, AC-10, AC-11) no tienen forma de recuperarse dentro de este sub-ticket. → **Mitigación**: quedan persistidos con su motivo, que es exactamente el insumo que consume el sub-ticket de listado y reproceso.
- **Riesgo**: Telegram descarta los updates no leídos a las 24 horas; si el bot no se levanta en ese plazo, los mensajes se pierden en origen. → **Mitigación**: fuera del control del sistema; se documenta en la operativa de arranque.

## Dependencies

- **API de Telegram** vía la librería `Telegram.Bot`. Requiere `TelegramBotToken` y `TelegramChatAutorizado` provistos por `IConfiguration` (user-secrets o variable de entorno), según la sección "Configuración / secretos" de `AGENTS.md`.
- **Ollama** corriendo localmente con el modelo `llama3.1` (configurable vía `OLLAMA_MODEL`), consumido a través de `OllamaSharp`. FR-12 depende de poder detectar su indisponibilidad.
- **SQLite + EF Core 10** para la persistencia de mensajes, categorías y movimientos, en la ruta absoluta `%LOCALAPPDATA%\PersonalFinance\personalfinance.db` compartida con `PersonalFinance.Web`.
- **PRD-001** (`docs/daw/prd/PRD.md`) como PRD padre: los sub-tickets de categorías, monedas, resumen mensual y reproceso dependen de que este sub-ticket deje persistidos los mensajes, sus estados y sus motivos de error.

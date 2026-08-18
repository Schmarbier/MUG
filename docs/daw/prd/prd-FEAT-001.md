# Parent PRD FEAT-001: PersonalFinance — visor de finanzas personales

| Field | Value |
|-------|-------|
| Ticket | FEAT-001 |
| Tracker | none |
| Date | 2026-07-31 |
| Status | Split |

> **Este archivo es el índice de sub-tickets, no el catálogo de requisitos.** El catálogo completo
> del producto —RF-01 a RF-32, RNF-01 a RNF-03 y AC-01 a AC-41— vive en
> [`PRD.md`](./PRD.md) y es la fuente contra la que traza cada sub-PRD. Este documento sólo dice
> **qué se implementa, en qué orden y en qué estado está**.

## Sub-tickets

| Sub-ticket | Título | PRD | RF del padre que cubre | Dependencias | Estado |
|---|---|---|---|---|---|
| FEAT-001a | Ingesta de mensajes de Telegram y clasificación en movimientos vía Ollama | [prd-FEAT-001a.md](./prd-FEAT-001a.md) | RF-01, RF-02, RF-03, RF-04, RF-06, RF-07, RF-08, RF-10, RF-11, RNF-01, RNF-02 | ninguna | **active** |
| FEAT-001b | Resumen mensual por categoría con paginación | pendiente | RF-12, RF-13, RNF-03 | depende de a | pending |
| FEAT-001c | ABM de categorías | pendiente | RF-05, RF-17, RF-18, RF-19, RF-20, RF-21, RF-22, RF-23 | depende de a | pending |
| FEAT-001d | Listado y reproceso de mensajes con error | pendiente | RF-14, RF-15 | depende de a | pending |
| FEAT-001e | Recategorización de movimientos | pendiente | RF-16 | depende de a, c | pending |
| FEAT-001f | Monedas y tipo de cambio | pendiente | RF-09, RF-24, RF-25, RF-26, RF-27 | depende de a | pending |
| FEAT-001g | Edición de movimientos | pendiente | RF-28, RF-29, RF-30, RF-31, RF-32 | depende de f | pending |

> **La columna `Estado` se mantiene, no es decorativa.** El cierre en RELEASE mueve el sub-ticket
> terminado a `done` —anotando dónde quedó su rama— y pone el siguiente en `active`.

## Suggested implementation order

```
a → b → c → d → e → f → g
```

**a** primero porque sin movimientos no hay nada que mostrar ni corregir. **b** segundo porque es el
objetivo declarado del producto —"poder ver en cualquier momento cuánto gasté y cuánto ahorré"— y
todo lo demás es corrección o administración sobre datos que ya se ven. **f** y **g** al final
porque las monedas son el bloque más grande y el que menos usa un usuario que opera en ARS.

## Deuda conocida que dejó FEAT-001a

Decisiones tomadas en el DEFINE de FEAT-001a que **generan trabajo explícito** en sub-tickets
posteriores. No son omisiones: están decididas y documentadas.

| Deuda | Origen | Quién la paga |
|---|---|---|
| El movimiento **no tiene campo `moneda`**. RF-09 quedó fuera de a. Un mensaje con moneda extranjera genera movimiento con el monto pelado y queda `procesado = true`, por lo que RF-15 nunca lo verá. | Decisión de scope en DEFINE de FEAT-001a | **FEAT-001f** debe agregar el campo y hacer una pasada de corrección buscando menciones de moneda en el texto original de los mensajes ya procesados. AC-30 del padre se cubre recién ahí. |
| Las categorías se crean por **seed fijo** (`Hogar`, `Ocio`, `Servicios`, `Sueldo`, `Otros`) y no se administran. | Reemplaza RF-05/RF-17 en a | **FEAT-001c** reemplaza el seed por el ABM completo. El seed queda como estado inicial. |
| La categoría **`Otros`** es fallback cuando el clasificador devuelve algo fuera del seed (FR-09 de a). Acumula movimientos mal categorizados de forma silenciosa. | Decisión de scope en DEFINE de FEAT-001a | **FEAT-001e** (recategorización) es la herramienta para vaciarla. |
| Los mensajes con `error = true` quedan persistidos con su motivo pero **no hay forma de verlos ni reprocesarlos**. | RF-14/RF-15 fuera de a | **FEAT-001d**. |
| AC-05 y AC-06 del padre afirman `moneda = "ARS"`. FEAT-001a los cubre **parcialmente**: monto, tipo y categoría sí; la aserción de moneda no. | Sin campo `moneda` en a | **FEAT-001f** cierra la parte de moneda de esos dos AC. |

## Original context

Los gastos no se registran en ningún lado. Se paga en efectivo, crédito, débito o desde los ahorros
y no queda nada consultable, y registrar cada movimiento a mano es lo que hace que el hábito no dure.

El canal donde el gasto ya se anota naturalmente es Telegram: el dueño se manda un mensaje a sí mismo
del estilo `"$2.000 comida casa"` en el momento en que gasta. El producto convierte esos mensajes en
movimientos estructurados mediante un modelo local (Ollama / `llama3.1`) y los muestra en un resumen
mensual agrupado por categoría y moneda.

Es mono-usuario, sin login, sin exportación y sin API externa de cotización: el tipo de cambio se
carga y se edita a mano.

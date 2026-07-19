# Contrato — Superficie del visor

**Feature**: 001-personal-finance-viewer · **Fecha**: 2026-07-18
**Origen**: [FR-036, FR-037](../spec.md) · [research.md R6](../research.md)

El visor es la **única** interfaz de administración. El canal de mensajería no ofrece comandos ni
respuestas para ninguna de estas operaciones. [FR-037]

---

## Páginas

| Ruta | Propósito | Render | Requisitos |
|---|---|---|---|
| `/` | Resumen mensual del mes en curso | Static SSR | FR-012 a FR-015a |
| `/categorias` | Alta, listado, edición, baja y reactivación de categorías | InteractiveServer | FR-024 a FR-031 |
| `/monedas` | Alta, listado, edición de cotización, baja y reactivación de monedas | InteractiveServer | FR-032 a FR-035f |
| `/errores` | Bandeja de mensajes con error y reproceso | InteractiveServer | FR-016, FR-017 |
| `/movimientos/{id}/editar` | Corrección manual de un movimiento | InteractiveServer | FR-018 a FR-023 |

El resumen es la única pantalla de solo lectura y la más visitada: se sirve sin circuito
interactivo, lo que sostiene SC-003. [R6]

---

## Resumen mensual — contrato de datos

**Entrada**: mes en curso (implícito), número de página por bloque (independiente entre bloques).

**Salida**: dos bloques, cada uno con sus filas paginadas.

| Elemento | Contenido |
|---|---|
| Bloque | Tipo (ingresos \| egresos), página actual, cantidad total de páginas, **total general del bloque** |
| Fila | Categoría, moneda, total en la moneda de la fila, equivalente en moneda base |

**Invariantes que la página debe respetar**
- El total general de cada bloque es la suma de los equivalentes en moneda base de **todas** las
  filas del mes, no solo las de la página visible; no cambia al paginar. [FR-012a]
- Los bloques nunca se netean entre sí. [FR-014]
- Las filas se ordenan por equivalente en base descendente, con desempate alfabético por categoría
  y luego por código de moneda. [FR-015a]
- Se muestran 4 filas por página; cada bloque pagina de forma independiente. [FR-015]
- El equivalente se redondea a 2 decimales una sola vez, al mostrar. [FR-040]
- La fila en moneda base no exhibe equivalente. [FR-013]
- Un mes sin movimientos muestra ambos bloques presentes, con totales en cero, no un error.
  [Edge Cases]
- Un bloque con menos de 4 filas muestra una única página sin controles de navegación activos.
  [Edge Cases]

---

## Operaciones de administración

Cada operación declara qué rechaza. Los rechazos son parte del contrato, no un detalle de
implementación: son requisitos con escenario de aceptación propio.

### Categorías

| Operación | Rechaza cuando | Requisito |
|---|---|---|
| Crear | El título ya existe | FR-024 |
| Editar título | El nuevo título ya existe | FR-026 |
| Editar descripción | — | FR-027 |
| Eliminar | Tiene movimientos asociados → se desactiva en lugar de eliminar | FR-028, FR-029 |
| Reactivar | — | FR-030 |

Editar una categoría desactivada es válido y no altera su estado. [FR-026]

### Monedas

| Operación | Rechaza cuando | Requisito |
|---|---|---|
| Agregar | El código ya existe, o el tipo de cambio es ≤ 0 | FR-033, FR-039 |
| Editar cotización | El tipo de cambio es ≤ 0 | FR-034, FR-039 |
| Eliminar | Tiene movimientos asociados → se desactiva; es la moneda base → se rechaza | FR-035b, FR-035c, FR-035f |
| Desactivar | Es la moneda base | FR-035f |
| Reactivar | — | FR-035d |

Editar la cotización **no** modifica el tipo de cambio histórico de los movimientos existentes.
[FR-035]

### Bandeja de errores

| Operación | Contrato | Requisito |
|---|---|---|
| Listar | Muestra cada mensaje con su motivo de error | FR-016 |
| Reprocesar | Solo aplica a mensajes en estado Error; no duplica un movimiento ya creado | FR-017, Edge Cases |
| Reprocesar todos | Reprocesa el lote completo; el fallo de uno no corta el resto; informa resueltos sobre total y deja en la bandeja los no resueltos | FR-017b |

### Edición de movimiento

| Campo editable | Efecto | Requisito |
|---|---|---|
| Categoría | — | FR-018 |
| Tipo | Cambia de bloque en el resumen; no altera monto, moneda ni tipo de cambio histórico | FR-018a |
| Monto | Debe ser > 0 con 2 decimales | FR-019, FR-038 |
| Moneda | Registra el tipo de cambio vigente de la nueva moneda | FR-020, FR-021 |
| Tipo de cambio histórico | Debe ser > 0; dispara la confirmación de propagación | FR-022, FR-023, FR-039 |

**Confirmación de propagación**: al editar el tipo de cambio histórico, la pantalla debe preguntar
si aplicar el nuevo valor a los demás movimientos de la misma moneda y fecha, y aplicarlo
únicamente si el dueño confirma. Sin confirmación, solo cambia el movimiento editado. [FR-023]

Toda corrección se refleja en el resumen sin ningún paso adicional de recálculo por parte del
usuario. [SC-008]

# Phase 1 — Modelo de Datos: PersonalFinance

**Feature**: 001-personal-finance-viewer · **Fecha**: 2026-07-18
**Entrada**: [spec.md](./spec.md) · [research.md](./research.md)

Las reglas de validación de este documento derivan de los requisitos funcionales de la spec. Cada
una cita su origen. Los tipos de almacenamiento siguen la decisión R1 (unidades menores en
INTEGER, `decimal` expuesto al dominio vía `ValueConverter`).

---

## Categoria

Agrupador de movimientos definido por el dueño.

| Campo | Tipo dominio | Almacenamiento | Notas |
|---|---|---|---|
| `Id` | `int` | INTEGER PK | Identidad técnica |
| `Titulo` | `string` | TEXT, índice único | Único entre todas las categorías |
| `Descripcion` | `string` | TEXT | |
| `Activa` | `bool` | INTEGER | Estado activa / desactivada |

**Reglas de validación**
- El título es único; un alta o edición que lo duplique se rechaza con error. [FR-024, FR-026]
- La unicidad aplica también contra categorías desactivadas: una desactivada sigue ocupando su
  título. [FR-026]
- Editar el título o la descripción no altera el estado. [FR-026]

**Transiciones de estado**

```text
        crear                    desactivar (al intentar eliminar con movimientos)
  ( ) ────────► Activa ◄──────────────────────────────────► Desactivada
                  │                    reactivar                  │
                  │ eliminar (solo sin movimientos)               │
                  ▼                                               │
                 ( )  ◄───────────────────────────────────────────┘
                          eliminar (solo sin movimientos)
```

- Eliminar solo procede si no tiene movimientos asociados. [FR-028]
- Con movimientos asociados, el intento de eliminar la desactiva en lugar de borrarla. [FR-029]
- Una categoría desactivada se excluye de la clasificación automática, pero los movimientos ya
  creados conservan su categoría. [FR-031]

---

## Moneda

Unidad en la que se expresa un movimiento.

| Campo | Tipo dominio | Almacenamiento | Notas |
|---|---|---|---|
| `Id` | `int` | INTEGER PK | |
| `Codigo` | `string` | TEXT, índice único | p. ej. ARS, USD |
| `EsBase` | `bool` | INTEGER | Verdadero solo para ARS |
| `Activa` | `bool` | INTEGER | Estado activa / desactivada |
| `TipoDeCambio` | `decimal?` | INTEGER (centésimos), nulo | Nulo en la moneda base |

**Reglas de validación**
- El código es único; un alta duplicada se rechaza con error. [FR-033]
- El tipo de cambio admite hasta 2 decimales y debe ser estrictamente mayor a cero, tanto al
  agregar como al editar. [FR-039]
- La moneda base no lleva tipo de cambio. [FR-032, FR-035]
- Existe exactamente una moneda base, ARS, preexistente sin carga del usuario. [FR-032]
- ARS no puede eliminarse ni desactivarse bajo ninguna condición. [FR-035f]

**Transiciones de estado**

Idénticas a Categoria —eliminar sin movimientos, desactivar con movimientos, reactivar— con la
excepción de que la moneda base está exenta de ambas operaciones. [FR-035b, FR-035c, FR-035d,
FR-035f]

- Desactivar preserva el tipo de cambio histórico de los movimientos existentes. [FR-035c]
- Una moneda desactivada se trata en la clasificación igual que una inexistente: el mensaje queda
  con error "moneda no soportada". [FR-035e]

---

## Mensaje

Texto recibido en el chat autorizado. Es la fuente de verdad de la que deriva todo movimiento.

| Campo | Tipo dominio | Almacenamiento | Notas |
|---|---|---|---|
| `Id` | `int` | INTEGER PK | |
| `IdentificadorCanal` | `long` | INTEGER, **índice único** | `message_id` de Telegram |
| `Texto` | `string` | TEXT | |
| `FechaRecepcionUtc` | `DateTimeOffset` | TEXT (ISO-8601) | Se convierte a zona local para derivar la fecha del movimiento (R5) |
| `Procesado` | `bool` | INTEGER | |
| `IntentosClasificacion` | `int` | INTEGER | Contador para el tope de FR-010a |
| `TieneError` | `bool` | INTEGER | |
| `MotivoError` | `string?` | TEXT, nulo | |

**Reglas de validación**
- Solo se persisten mensajes del chat autorizado; los demás se descartan sin guardarse. [FR-002]
- El identificador del canal es único: el índice único es la garantía de FR-004 frente a la
  condición de carrera entre los dos disparadores de ingesta (R4).
- Los motivos de error cubren al menos: "no contiene monto", "no contiene descripción", "moneda no
  soportada" y "clasificador no disponible". [FR-010]

**Transiciones de estado**

```text
                    clasificación exitosa
  ( ) ──► Pendiente ──────────────────────► Procesado
   guardar  │  ▲                              (terminal)
            │  │ fallo del clasificador
            │  │ (intentos < 3)
            │  └──────────┘
            │
            │ fallo de validación, o fallo del clasificador con intentos = 3
            ▼
          Error ──────────────────────────► Procesado
              reproceso manual exitoso
```

- Pendiente equivale a `Procesado = false` y `TieneError = false`.
- Un fallo del clasificador incrementa `IntentosClasificacion` y mantiene el mensaje Pendiente
  mientras el contador sea menor a 3. [FR-010a]
- Alcanzados los 3 intentos, pasa a Error con motivo "clasificador no disponible". [FR-010b]
- Solo se reprocesan mensajes en estado Error; un mensaje Procesado no vuelve a generar
  movimientos. [Assumptions, Edge Cases]
- Un mensaje origina cero o un Movimiento. [Key Entities]

---

## Movimiento

Registro económico derivado de un Mensaje.

| Campo | Tipo dominio | Almacenamiento | Notas |
|---|---|---|---|
| `Id` | `int` | INTEGER PK | |
| `MensajeId` | `int` | INTEGER FK → Mensaje | Origen |
| `CategoriaId` | `int` | INTEGER FK → Categoria | |
| `MonedaId` | `int` | INTEGER FK → Moneda | |
| `Monto` | `decimal` | INTEGER (centavos) | 2 decimales exactos |
| `Tipo` | `TipoMovimiento` | INTEGER | Ingreso / Egreso |
| `Fecha` | `DateOnly` | TEXT | Fecha local derivada del mensaje (R5) |
| `TipoDeCambioHistorico` | `decimal?` | INTEGER (centésimos), nulo | Nulo en moneda base |

**Reglas de validación**
- El monto tiene exactamente 2 decimales y es estrictamente mayor a cero; el sentido económico lo
  aporta `Tipo`, nunca el signo. [FR-038]
- El tipo de cambio histórico admite hasta 2 decimales y debe ser mayor a cero cuando aplica.
  [FR-039]
- Es nulo si y solo si la moneda del movimiento es la base. [FR-035]
- Se registra al crearse con el tipo de cambio vigente de su moneda, y no se modifica cuando se
  actualiza la cotización de esa moneda. [FR-035]
- Al editar la moneda de un movimiento se registra el tipo de cambio vigente en ese momento.
  [FR-021]
- La fecha se deriva de la fecha del mensaje origen convertida a la zona local, y determina a qué
  mes pertenece en el resumen. [Assumptions, R5]

**Ediciones admitidas**

| Atributo | Requisito | Efecto sobre otros campos |
|---|---|---|
| Categoría | FR-018 | Ninguno |
| Tipo | FR-018a | Cambia de bloque en el resumen; no altera monto, moneda ni tipo de cambio histórico |
| Monto | FR-019 | Ninguno |
| Moneda | FR-020, FR-021 | Registra el tipo de cambio vigente de la nueva moneda |
| Tipo de cambio histórico | FR-022, FR-023 | Puede propagarse a movimientos de igual moneda y fecha, previa confirmación |

---

## ResumenMensual (vista derivada, no persistida)

Proyección de solo lectura sobre los Movimientos de un mes. No tiene tabla. [Key Entities]

**Composición**
- Dos bloques independientes: ingresos y egresos, que nunca se netean entre sí. [FR-014]
- Cada bloque contiene **filas**, donde una fila es una combinación de categoría y moneda.
  [FR-012, FR-015]
- Cada bloque expone un **total general**, suma de los equivalentes en moneda base de todas sus
  filas del mes en curso —independiente de la paginación—, con el mismo redondeo único de las
  filas. [FR-012a, FR-040]

**Fila**

| Campo | Origen |
|---|---|
| Categoría | Agrupación |
| Moneda | Agrupación |
| Total en moneda de la fila | Suma de los montos de los movimientos agrupados |
| Equivalente en moneda base | Suma de los equivalentes individuales, cada movimiento convertido con su propio tipo de cambio histórico [FR-013] |

**Reglas de cálculo**
- El equivalente de la fila se calcula con precisión completa y se redondea una sola vez, a 2
  decimales, con empate hacia arriba. [FR-040, R2]
- Las filas de cada bloque se ordenan de forma descendente por su equivalente en moneda base, con
  desempate alfabético por título de categoría y luego por código de moneda. El orden es
  determinístico. [FR-015a]
- Cada bloque se pagina de forma independiente, de a 4 filas por página. [FR-015]
- Incluye los movimientos cuya categoría o moneda fue desactivada después de su creación.
  [FR-031, FR-035e]
- El alcance es el mes calendario en curso. [Assumptions]

---

## Relaciones

```text
Mensaje  1 ──── 0..1  Movimiento  ──── * 1  Categoria
                          │
                          └──────────── * 1  Moneda

ResumenMensual  ──derivado de──►  Movimiento  (sin persistencia)
```

## Índices

| Tabla | Índice | Motivo |
|---|---|---|
| Mensaje | único sobre `IdentificadorCanal` | FR-004 y Restricciones Técnicas de la constitución |
| Categoria | único sobre `Titulo` | FR-024, FR-026 |
| Moneda | único sobre `Codigo` | FR-033 |
| Movimiento | sobre `Fecha` | Consulta del resumen por mes |
| Movimiento | sobre (`MonedaId`, `Fecha`) | Propagación del tipo de cambio de FR-023 |

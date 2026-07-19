# Contrato — Clasificador de mensajes

**Feature**: 001-personal-finance-viewer · **Fecha**: 2026-07-18
**Origen**: [research.md R3](../research.md) · Principios II y III de la constitución

Este es el contrato de la frontera entre la lógica de negocio y el modelo de IA. Es el punto donde
el Principio II se hace verificable: **todo lo que está del lado del modelo —prompt, JSON,
timeouts, reintentos de red— es invisible para el negocio**, que solo ve este contrato.

---

## Puerto (lo que ve el dominio)

**Operación**: clasificar el texto de un mensaje contra el catálogo vigente.

**Entrada**
| Campo | Tipo | Notas |
|---|---|---|
| `Texto` | `string` | Texto crudo del mensaje, sin normalizar |
| `CategoriasActivas` | lista de `{ Titulo, Descripcion }` | Solo activas [FR-007, FR-031] |
| `MonedasActivas` | lista de `{ Codigo, EsBase }` | Solo activas [FR-035e] |

El catálogo se pasa como entrada en lugar de que el adaptador lo consulte: mantiene al adaptador
sin acceso a persistencia y hace los tests del dominio triviales de armar.

**Salida**: exactamente uno de dos resultados. No hay tercer camino, y ninguno de los dos admite
valores asumidos.

| Resultado | Contenido |
|---|---|
| `Clasificacion` | `Monto` (decimal > 0), `Tipo` (Ingreso \| Egreso), `TituloCategoria`, `CodigoMoneda` |
| `Falla` | `Motivo` ∈ { SinMonto, SinDescripcion, MonedaNoSoportada, SinConfianza, ClasificadorNoDisponible } |

**Mapeo de fallas a motivos de error del Mensaje** [FR-010]

| `Motivo` | Motivo persistido | Requisito |
|---|---|---|
| `SinMonto` | "no contiene monto" | FR-010, FR-041 |
| `SinDescripcion` | "no contiene descripción" | FR-010 |
| `MonedaNoSoportada` | "moneda no soportada" | FR-010, FR-035e |
| `SinConfianza` | "no se pudo determinar la categoría con confianza" | FR-011 |
| `ClasificadorNoDisponible` | "clasificador no disponible" | FR-010b |

`ClasificadorNoDisponible` es la única falla que **no** marca el mensaje con error de inmediato:
incrementa el contador de intentos y deja el mensaje pendiente hasta alcanzar 3. [FR-010a]

---

## Esquema de respuesta del modelo (lado del adaptador)

El adaptador solicita salida JSON estricta. Toda respuesta que no valide contra este esquema se
traduce a `Falla`, nunca a una clasificación parcial.

```json
{
  "monto": 2000.00,
  "tipo": "egreso",
  "categoria": "Hogar",
  "moneda": "ARS",
  "confianza": 0.92
}
```

| Campo | Tipo | Validación |
|---|---|---|
| `monto` | número | > 0, máximo 2 decimales. Ausente o no interpretable → `SinMonto` [FR-038, FR-041] |
| `tipo` | string | `"ingreso"` o `"egreso"`. Otro valor → `SinConfianza` [FR-006] |
| `categoria` | string | Debe coincidir con una categoría activa recibida en la entrada. Si no coincide → `SinConfianza` [FR-007] |
| `moneda` | string | Debe coincidir con una moneda activa. Ausente → se asume la base ARS [FR-008]. Presente y no reconocida → `MonedaNoSoportada` [FR-035e] |
| `confianza` | número | 0 a 1. Por debajo del umbral configurado → `SinConfianza` [FR-011] |

**La moneda ausente es la única omisión que admite un valor por defecto**, y solo porque FR-008 lo
manda explícitamente. Cualquier otra ausencia es una falla. Esto no es rigidez: es el Principio
III: un monto o una categoría inventados corrompen el resumen sin que el dueño lo note.

---

## Reglas de invocación

| Regla | Valor | Motivo |
|---|---|---|
| Formato solicitado | JSON estricto con el esquema anterior | Evita parseo de texto libre, frágil por definición |
| Temperatura | Baja | La clasificación no es una tarea creativa; la variabilidad solo introduce ruido |
| Timeout | Acotado, por debajo del presupuesto de SC-002 | La clasificación debe completarse en < 5 s p90 |
| Categorías en el prompt | Solo las activas, con su descripción | La descripción es la señal que distingue categorías de nombre parecido |
| Modelo | Configurable vía `OLLAMA_MODEL` | AGENTS.md; no se hardcodea |

**Ante ausencia total de categorías activas** el adaptador no se invoca: el mensaje va
directamente a error indicando que no hay categorías disponibles para clasificar. [Edge Cases]

---

## Verificación del contrato

- El dominio se testea íntegramente contra un doble de este puerto, sin Ollama levantado. Es la
  prueba de que el Principio II se cumple: si algún test de negocio necesita el modelo real, el
  aislamiento está roto. [R9]
- El adaptador se testea contra un servidor simulado, cubriendo al menos: respuesta válida,
  JSON malformado, campo faltante, categoría inexistente en el catálogo, confianza bajo el umbral,
  timeout y servidor caído. Cada caso debe producir la `Falla` correspondiente y **jamás** una
  clasificación con valores rellenados.

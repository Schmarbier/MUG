# Quickstart — Validación de PersonalFinance

**Feature**: 001-personal-finance-viewer · **Fecha**: 2026-07-18
**Entrada**: [plan.md](./plan.md) · [contracts/](./contracts/)

Guía para levantar la feature y comprobar que funciona de punta a punta. No contiene código de
implementación: los detalles de cada entidad están en [data-model.md](./data-model.md) y los de
cada interfaz en [contracts/](./contracts/).

---

## Prerequisitos

| Requisito | Verificación |
|---|---|
| .NET 10 SDK | `dotnet --version` → 10.x |
| Ollama corriendo con el modelo | `ollama serve` y `ollama pull llama3.1` |
| Bot de Telegram creado | Token obtenido de BotFather |
| Chat autorizado identificado | Id numérico del chat privado del dueño |

## Configuración

Los secretos no se commitean (Principio IV). Desde el proyecto del bot, una sola vez:

```
dotnet user-secrets set "TelegramBotToken" "TU_TOKEN"
dotnet user-secrets set "TelegramChatAutorizado" "TU_CHAT_ID"
```

Alternativa: exportarlos como variables de entorno. Ídem `OLLAMA_MODEL` si se usa un modelo
distinto del predeterminado.

Con `TelegramChatAutorizado` en `0` el bot no ingiere nada — es el placeholder de
`appsettings.json`, no un valor válido.

## Puesta en marcha

```
dotnet restore
dotnet test
```

Una terminal por proceso:

```
dotnet run --project src/PersonalFinance.Bot
dotnet run --project src/PersonalFinance.Web
```

Ambos escriben y leen el mismo archivo SQLite en `%LOCALAPPDATA%\PersonalFinance\`. Si cada
proceso terminara con su propia base, la ruta dejó de ser absoluta: es la falla que la constitución
prohíbe explícitamente.

---

## Escenarios de validación

Cada escenario cita el requisito que prueba. Ejecutarlos en orden: los primeros construyen el
estado que los últimos necesitan.

### V1 — Ingesta y clasificación automática

1. Desde el chat autorizado, enviar `$2.000 comida casa`.
2. **Esperado**: en menos de un ciclo, el mensaje queda guardado y clasificado sin ninguna acción
   adicional del dueño. Se crea un movimiento de egreso, $2.000, moneda ARS, en una categoría
   activa coherente. [FR-005a, FR-008, US1 AC-5, AC-7.a]

### V2 — Aislamiento del chat

1. Enviar un mensaje al bot desde una cuenta distinta.
2. **Esperado**: no se guarda ni genera movimiento alguno. [FR-002, US1 AC-2]

### V3 — Deduplicación

1. Detener el bot, volver a levantarlo, dejar que reprocese actualizaciones.
2. **Esperado**: la cantidad de mensajes y de movimientos almacenados no cambia. [FR-004, SC-007]

### V4 — Resumen mensual, agrupación y no neteo

1. Registrar en el mes un egreso y un ingreso de $800, ambos en la misma categoría y en ARS.
2. Abrir `/`.
3. **Esperado**: el bloque de egresos muestra una fila con $800 y el de ingresos otra fila con
   $800. No se netean. [FR-014, US2 AC-4]

### V5 — Moneda extranjera y suma de equivalentes

1. Dar de alta USD con cotización, registrar dos movimientos en USD con **cotizaciones distintas**
   (editar la cotización entre uno y otro).
2. Abrir `/`.
3. **Esperado**: una única fila para esa categoría en USD, cuyo equivalente en ARS es la suma de
   los equivalentes individuales —cada movimiento con su propio tipo de cambio histórico—, no el
   resultado de aplicar una cotización única al total. [FR-013, US2 AC-3.a]

### V6 — Redondeo

1. Registrar dos movimientos de U$S 1,01 con tipo de cambio histórico 1450,55.
2. **Esperado**: el equivalente de la fila es **$2.930,11**, no $2.930,12. [FR-040, US2 AC-3.b]

> Este es el escenario que delata un redondeo mal ubicado o el modo de redondeo por defecto de
> .NET. Si da $2.930,12, revisar R2 antes de seguir.

### V7 — Orden y paginación

1. Tener un bloque con más de 4 filas, con montos equivalentes distintos.
2. **Esperado**: las filas aparecen ordenadas por equivalente en base descendente, 4 por página,
   cada bloque paginando por su cuenta. Recargar la página produce exactamente la misma
   secuencia. [FR-015, FR-015a, US2 AC-2, AC-2.a, AC-2.b]

### V8 — Errores y reproceso

1. Enviar `100 EUR viaje` sin tener EUR cargada.
2. **Esperado**: el mensaje queda con error "moneda no soportada" y aparece en `/errores`.
   [FR-010, US4 AC-3, AC-4]
3. Dar de alta EUR y reprocesar el mensaje desde la bandeja.
4. **Esperado**: queda procesado y se crea su movimiento en EUR. [FR-017, US4 AC-5]
5. Dejar dos mensajes con error, corregir la causa de uno solo y usar "Reprocesar todos".
6. **Esperado**: el corregido queda procesado, la pantalla informa "1 de 2 reprocesados
   correctamente" y el no resuelto sigue listado con su motivo. [FR-017b, US4 AC-5.a]

### V9 — Clasificador caído

1. Detener Ollama. Enviar un mensaje válido.
2. **Esperado**: el mensaje queda pendiente, sin error, y se reintenta en los ciclos siguientes.
   [FR-010a, US4 AC-4.b]
3. Dejarlo caído hasta agotar los 3 intentos.
4. **Esperado**: el mensaje pasa a error "clasificador no disponible" y aparece en `/errores`.
   Ningún mensaje queda invisible. [FR-010b, SC-006, US4 AC-4.a]

### V10 — Ciclo de vida de categorías

1. Crear una categoría; intentar crear otra con el mismo título.
2. **Esperado**: la segunda se rechaza con error. [FR-024, US3 AC-2]
3. Intentar eliminar una categoría con movimientos asociados.
4. **Esperado**: queda desactivada, no eliminada, y deja de usarse para clasificar mensajes nuevos
   sin afectar a los movimientos ya creados. [FR-029, FR-031, US3 AC-7]

### V11 — Ciclo de vida de monedas

1. Intentar eliminar una moneda con movimientos asociados.
2. **Esperado**: queda desactivada; los movimientos conservan su tipo de cambio histórico y siguen
   apareciendo en el resumen. [FR-035c, US6 AC-9, AC-13]
3. Intentar eliminar o desactivar ARS.
4. **Esperado**: se rechaza con error. [FR-035f, US6 AC-12]

### V12 — Corrección manual

1. Editar el **tipo** de un movimiento de egreso a ingreso.
2. **Esperado**: el movimiento cambia de bloque en el resumen conservando monto, moneda y tipo de
   cambio histórico, sin ningún paso de recálculo manual. [FR-018a, SC-008, US5 AC-4, AC-5]
3. Editar el tipo de cambio histórico de un movimiento que comparte moneda y fecha con otros.
4. **Esperado**: se pregunta si propagar. Confirmando, cambian todos; sin confirmar, solo el
   editado. [FR-023, US6 AC-6, AC-7]

---

## Criterios de aceptación de la validación

La feature se considera validada cuando los doce escenarios pasan y además:

- **SC-006**: ningún mensaje del chat autorizado quedó fuera de las dos únicas salidas posibles —
  procesado con su movimiento, o visible en `/errores` con motivo.
- **SC-002 / SC-003**: la clasificación se completa en menos de 5 s (p90) y el resumen se muestra
  en menos de 1 s (p95) sobre el volumen de referencia de R7.
- **SC-001**: sobre un conjunto etiquetado de al menos 50 mensajes que cubra todas las categorías,
  el acierto es ≥ 80%, contando acierto solo cuando categoría, tipo, monto y moneda son todos
  correctos (R8).

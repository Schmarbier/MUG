# PRD-001: PersonalFinance — visor de finanzas personales

## Contexto y Problema
Personalmente los gastos no los manejo con nada, puedo pagar efectivo, credito, debito, ahorros y no tengo algo sencillo para ver los gastos/ahorros que voy teniendo en el mes.

## Objetivos
Poder ver en cualquier momento cuanto gaste y cuanto ahorre en el mes, agrupado por categoria, sin tener que registrar manualmente cada movimiento.

## Requerimientos Funcionales
- RF-01: El sistema debe leer los mensajes enviados por Telegram.
- RF-02: El sistema debe ingerir únicamente los mensajes provenientes del chat autorizado del dueño; los mensajes de cualquier otro chat se descartan sin guardarse.
- RF-03: El sistema debe guardar los mensajes leídos.
- RF-04: El sistema no debe guardar dos veces el mismo mensaje, identificándolo por el `message_id` de Telegram.
- RF-05: El sistema debe permitir crear una categoria con Titulo único y Descripcion.
- RF-06: El sistema debe crear un movimiento a partir de cada mensaje.
- RF-07: El sistema debe determinar, para cada movimiento, si es de tipo ingreso o egreso a partir del contenido del mensaje.
- RF-08: El sistema debe clasificar cada movimiento en una de las categorias activas.
- RF-09: El sistema debe asignar la moneda ARS al movimiento cuando el mensaje no indica una moneda explícita.
- RF-10: El sistema debe marcar como procesado cada mensaje del que se creó un movimiento.
- RF-11: El sistema debe marcar con error y motivo cada mensaje que no pueda convertirse en un movimiento.
- RF-12: El sistema debe mostrar un resumen mensual dividido en dos bloques —ingresos y egresos—; dentro de cada bloque debe agrupar los movimientos por categoria y moneda.
- RF-13: El sistema debe paginar cada bloque del resumen mensual de forma independiente, de a 4 categorias por página.
- RF-14: El sistema debe listar los mensajes que quedaron con error.
- RF-15: El sistema debe permitir volver a procesar los mensajes con error.
- RF-16: El sistema debe permitir recategorizar un movimiento.
- RF-17: El sistema debe listar las categorias existentes.
- RF-18: El sistema debe permitir editar el Titulo de una categoria existente.
- RF-19: El sistema debe permitir editar la Descripcion de una categoria existente.
- RF-20: El sistema debe permitir eliminar una categoria que no tiene movimientos asociados.
- RF-21: El sistema debe permitir desactivar una categoria que tiene movimientos asociados, en lugar de eliminarla.
- RF-22: El sistema debe permitir reactivar una categoria desactivada.
- RF-23: El sistema debe excluir las categorias desactivadas de la clasificacion de nuevos movimientos.
- RF-24: El sistema debe tener la moneda ARS preexistente como moneda base, sin que el usuario deba cargarla. La moneda base no requiere tipo de cambio, porque los montos ya se expresan en ella.
- RF-25: El sistema debe permitir agregar una moneda nueva con su tipo de cambio respecto a la moneda base.
- RF-26: El sistema debe permitir editar el tipo de cambio de una moneda existente.
- RF-27: El sistema debe registrar en cada movimiento el tipo de cambio historico de su moneda al momento de crearse.
- RF-28: El sistema debe permitir editar el monto de un movimiento existente.
- RF-29: El sistema debe permitir editar la moneda de un movimiento existente.
- RF-30: El sistema debe registrar el tipo de cambio vigente de la moneda al momento de editar el movimiento a una moneda distinta de la que tenía.
- RF-31: El sistema debe permitir editar manualmente el tipo de cambio historico registrado en un movimiento.
- RF-32: El sistema debe preguntar si aplicar el tipo de cambio editado a los demás movimientos de la misma moneda y fecha, y aplicarlo solo a esos movimientos si el usuario confirma.

## Requerimientos No Funcionales
- RNF-01: El modelo debe alcanzar una accuracy de al menos 80% en la clasificacion.
- RNF-02: El agente clasificador debe responder en menos de 5 s p90.
- RNF-03: El resumen mensual debe cargar en menos de 1 s p95.

## Criterios de Aceptación
- AC-01 (RF-01, RF-03): Dado un mensaje nuevo del chat autorizado, cuando el sistema lo lee y lo guarda, entonces el mensaje queda almacenado con estado "no procesado".
- AC-02 (RF-02): Dado un mensaje enviado al bot desde un chat distinto al del dueño, cuando el sistema lee los mensajes, entonces el mensaje no se guarda y no genera ningún movimiento.
- AC-03 (RF-04): Dado un mensaje con `message_id` = 123 ya guardado, cuando el sistema vuelve a leer los mensajes y recibe nuevamente el `message_id` = 123, entonces no se guarda un segundo registro y la cantidad de mensajes almacenados no cambia.
- AC-04 (RF-05, RF-17): Dada una categoria con Titulo = "Hogar" y Descripcion = "gastos del hogar", cuando se crea la categoria, entonces la categoria aparece en el listado de categorias con Titulo = "Hogar", Descripcion = "gastos del hogar" y estado = "activa".
- AC-05 (RF-06, RF-07, RF-08, RF-09): Dado que existe la categoria "sueldo" y el mensaje "$10.000 ingreso" (sin moneda explícita), cuando el agente procesa los mensajes, entonces se crea un movimiento con categoria = "sueldo", monto = "$10.000", tipo = "ingreso", moneda = "ARS" (por defecto), tipo de cambio historico = no aplica.
- AC-06 (RF-06, RF-07, RF-08, RF-09): Dado que existe la categoria "hogar" y el mensaje "$2.000 comida casa" (sin moneda explícita), cuando el agente procesa los mensajes, entonces se crea un movimiento con categoria = "hogar", monto = "$2.000", tipo = "egreso", moneda = "ARS" (por defecto), tipo de cambio historico = no aplica.
- AC-07 (RF-11): Dado un mensaje que no contiene un monto, cuando el agente procesa los mensajes, entonces el mensaje queda con error = true y motivo = "no contiene monto".
- AC-08 (RF-11): Dado un mensaje que contiene monto pero no descripcion, cuando el agente procesa los mensajes, entonces el mensaje queda con error = true y motivo = "no contiene descripcion".
- AC-09 (RF-10): Dado un mensaje del que se creó un movimiento con categoria asignada, cuando el agente termina de procesarlo, entonces el mensaje queda con procesado = true.
- AC-10 (RF-12): Dado un listado de movimientos del mes que incluye 3 movimientos en ARS de la categoria "Hogar" por $2.000, $3.000 y $1.500, cuando se muestra el resumen mensual, entonces en el bloque de egresos aparece una fila "Hogar — ARS" con monto = $6.500.
- AC-11 (RF-13): Dado el bloque de egresos del resumen mensual con 10 categorias, cuando se muestra, entonces ese bloque se pagina de forma independiente de a 4 categorias por pagina, resultando en 3 paginas (2 paginas completas de 4 categorias y 1 pagina final con 2 categorias).
- AC-12 (RF-14): Dado un mensaje con error = true, cuando se listan los mensajes con error, entonces el mensaje aparece en el listado de errores.
- AC-13 (RF-15): Dado el mensaje "100 EUR viaje" que quedó con error = "moneda no soportada", cuando el usuario agrega la moneda EUR y vuelve a procesar el mensaje, entonces el mensaje queda con procesado = true y se crea su movimiento en EUR.
- AC-14 (RF-16): Dado un movimiento con categoria = "Hogar", cuando el usuario edita su categoria a "Ocio", entonces el movimiento queda con categoria = "Ocio".
- AC-15 (RNF-01): Dado un conjunto de al menos 50 mensajes de prueba etiquetados que cubre todas las categorias existentes, cuando el agente los clasifica, entonces la accuracy es mayor o igual a 80%.
- AC-16 (RNF-02): Dado un mensaje, cuando el agente lo clasifica, entonces responde en menos de 5 s medido en el p90.
- AC-17 (RF-05): Dado que ya existe una categoria con Titulo = "Hogar", cuando se intenta crear otra categoria con Titulo = "Hogar", entonces el sistema rechaza la creacion con error.
- AC-18 (RF-18): Dada una categoria con Titulo = "Hogar", cuando se edita su Titulo a "Casa", entonces la categoria queda con Titulo = "Casa".
- AC-19 (RF-18): Dado que ya existe una categoria con Titulo = "Ocio", cuando se intenta editar otra categoria para que su Titulo sea "Ocio", entonces el sistema rechaza la edicion con error.
- AC-20 (RF-19): Dada una categoria con Descripcion = "gastos del hogar", cuando se edita su Descripcion a "gastos de la casa", entonces la categoria queda con Descripcion = "gastos de la casa".
- AC-21 (RF-20): Dada una categoria sin movimientos asociados, cuando se elimina, entonces la categoria deja de aparecer en el listado de categorias.
- AC-22 (RF-21): Dada una categoria con al menos un movimiento asociado, cuando se intenta eliminar, entonces la categoria queda desactivada (no eliminada).
- AC-23 (RF-22, RF-23): Dada una categoria desactivada, cuando se reactiva, entonces vuelve a estar disponible para clasificar nuevos movimientos.
- AC-24 (RF-08, RF-23): Dado que la categoria "Hogar" esta desactivada, cuando el agente clasifica un mensaje nuevo, entonces no asigna la categoria "Hogar" al movimiento.
- AC-25 (RF-24): Dado un sistema recién inicializado sin monedas cargadas por el usuario, cuando se consultan las monedas disponibles, entonces existe la moneda ARS como moneda base.
- AC-26 (RF-25): Dado que no existe la moneda "USD", cuando se agrega la moneda "USD" con tipo de cambio = 1450, entonces la moneda "USD" queda disponible con tipo de cambio = 1450.
- AC-27 (RF-26): Dada la moneda "USD" con tipo de cambio = 1450 y un movimiento ya creado en USD con tipo de cambio historico = 1450, cuando se edita el tipo de cambio de "USD" a 1500, entonces el movimiento ya creado conserva tipo de cambio historico = 1450 y los movimientos nuevos en USD usan 1500.
- AC-28 (RF-06, RF-07, RF-27): Dado que existe la moneda "USD" con tipo de cambio = 1450, la categoria "ahorro", y el mensaje "800 USD ahorro", cuando el agente procesa el mensaje, entonces se crea un movimiento con categoria = "ahorro", monto = 800, moneda = "USD", tipo = egreso, tipo de cambio historico = 1450.
- AC-29 (RF-07, RF-08): Dado que existe la categoria "Ahorro" y el mensaje "Saqué $800 de ahorros", cuando el agente procesa el mensaje, entonces se crea un movimiento con categoria = "Ahorro" y tipo = ingreso.
- AC-30 (RF-11): Dado un mensaje que menciona una moneda no cargada en el sistema (ej. "100 EUR viaje" sin que exista la moneda "EUR"), cuando el agente procesa el mensaje, entonces el mensaje queda con error = true y motivo = "moneda no soportada".
- AC-31 (RF-12): Dado un movimiento de la categoria "Ahorro" en USD por monto = 800 con tipo de cambio historico = 1450, cuando se muestra el resumen mensual, entonces en el bloque de egresos aparece una fila "Ahorro — USD" con monto = U$S 800 y equivalente historico = $1.160.000 ARS, separada de las filas en ARS de esa categoria.
- AC-32 (RNF-03): Dado un mes con movimientos ya cargados, cuando se muestra el resumen mensual, entonces responde en menos de 1 s medido en p95.
- AC-33 (RF-17, RF-21): Dada una categoria desactivada con Titulo = "Ocio", cuando se lista, entonces aparece en el listado de categorias con estado = "desactivada".
- AC-34 (RF-25): Dado que ya existe la moneda "USD", cuando se intenta agregar otra moneda "USD", entonces el sistema rechaza la creacion con error.
- AC-35 (RF-18): Dada una categoria desactivada con Titulo = "Ocio", cuando se edita su Titulo a "Entretenimiento", entonces la categoria queda con Titulo = "Entretenimiento" y estado = "desactivada".
- AC-36 (RF-28): Dado un movimiento con monto = $2.000, cuando se edita su monto a $2.500, entonces el movimiento queda con monto = $2.500.
- AC-37 (RF-29, RF-30): Dado un movimiento con moneda = "ARS" y la moneda "USD" con tipo de cambio = 1500, cuando se edita la moneda del movimiento a "USD", entonces el movimiento queda con moneda = "USD" y tipo de cambio historico = 1500.
- AC-38 (RF-31): Dado un movimiento en USD con tipo de cambio historico = 1500, cuando el usuario edita manualmente ese tipo de cambio a 1450, entonces el movimiento queda con tipo de cambio historico = 1450.
- AC-39 (RF-32): Dado que existen 3 movimientos en USD con fecha = "2026-07-10" y tipo de cambio historico = 1500, cuando se edita el tipo de cambio historico de uno de ellos a 1450 y se confirma aplicar a los demás, entonces los 3 movimientos quedan con tipo de cambio historico = 1450.
- AC-40 (RF-32): Dado que existen 3 movimientos en USD con fecha = "2026-07-10" y tipo de cambio historico = 1500, cuando se edita el tipo de cambio historico de uno de ellos a 1450 y el usuario NO confirma aplicar a los demás, entonces solo ese movimiento queda con tipo de cambio historico = 1450 y los otros dos conservan 1500.
- AC-41 (RF-12): Dado un mes con un movimiento de egreso en la categoria "Ahorro" por $800 y un movimiento de ingreso en la categoria "Ahorro" por $800, cuando se muestra el resumen mensual, entonces el bloque de egresos incluye una fila "Ahorro — ARS" con monto = $800 y el bloque de ingresos incluye una fila "Ahorro — ARS" con monto = $800, sin netearse entre sí.

## Fuera de Alcance
- No tiene sistema de usuarios.
- No incluye canal via WhatsApp.
- No responde el bot al mensaje enviado por el usuario.
- No permite exportar datos (ej. CSV, Excel, PDF).
- No integra ninguna API externa de cotizacion de moneda: el tipo de cambio se carga y edita manualmente por el usuario.

## Riesgos y Dependencias
- Riesgo: La IA puede categorizar mal el movimiento -> mitigación: (RF-16, RNF-01) Si el usuario mira que el mensaje "$100 compra super" fue categorizado como "hobby" o ve que un movimiento tiene bajo accuracy, puede editarlo por la categoria correspondiente.
- Riesgo: El Bot que lee los mensajes puede guardar los mismos mensajes una y otra vez, dando asi, mensajes duplicados por cada corrida -> mitigacion: (RF-04, RF-10) el sistema deduplica por `message_id` de Telegram para no re-guardar el mismo mensaje, y marca procesado = true los mensajes que ya generaron un movimiento.
- Riesgo: El usuario puede desactivar por error una categoria con movimientos, dejandola no disponible para clasificar mensajes nuevos -> mitigacion: (RF-22) puede reactivarla en cualquier momento sin perder el historial de movimientos ya creados.
- Riesgo: El tipo de cambio cargado a mano puede quedar desactualizado si el usuario no lo edita -> mitigacion: (RF-26, RF-27) el usuario puede editarlo cuando quiera; los movimientos ya creados conservan su tipo de cambio historico y no se ven afectados por la actualizacion.
- Dependencia: Modelo de IA via API Ollama local, instrucciones .md al agente categorizador, API Telegram, SQLite. El tipo de cambio de moneda es un dato manual cargado por el usuario, sin dependencia de ninguna API externa de cotizacion.

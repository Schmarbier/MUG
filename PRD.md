# PRD-001: PersonalFinance — visor de finanzas personales

## Contexto y Problema
Personalmente los gastos no los manejo con nada, puedo pagar efectivo, credito, debito, ahorros y no tengo algo sencillo para ver los gastos/ahorros que voy teniendo en el mes.

## Objetivos
A partir de un mensaje de Telegram, un Bot debe obtener ese mensaje y clasificar el gasto como un movimiento de una categoria.

## Requerimientos Funcionales
- RF-01: El sistema debe leer los mensajes enviados por Telegram.
- RF-02: El sistema debe guardar los mensajes leídos.
- RF-03: El sistema debe permitir crear una categoria con Titulo y Descripcion.
- RF-04: El sistema debe crear un movimiento a partir de cada mensaje.
- RF-05: El sistema debe clasificar cada movimiento en una de las categorias existentes.
- RF-06: El sistema debe marcar como procesado cada mensaje del que se creó un movimiento.
- RF-07: El sistema debe marcar con error y motivo cada mensaje que no pueda convertirse en un movimiento.
- RF-08: El sistema debe mostrar un resumen mensual por categoria.
- RF-09: El sistema debe paginar el resumen mensual de a 4 categorias por página.
- RF-10: El sistema debe listar los mensajes que quedaron con error.
- RF-11: El sistema debe permitir volver a procesar los mensajes con error.
- RF-12: El sistema debe permitir recategorizar un movimiento.

## Requerimientos No Funcionales
- RNF-01: El modelo debe alcanzar una accuracy de al menos 80% en la clasificacion.
- RNF-02: El agente clasificador debe responder en menos de 5 s p90.

## Criterios de Aceptación
- AC-01 (RF-01, RF-02): Dado un mensaje nuevo, cuando el sistema lo lee y lo guarda, entonces el mensaje queda almacenado con estado "no procesado".
- AC-02 (RF-03): Dada una categoria con Titulo = "Hogar" y Descripcion = "gastos del hogar", cuando se crea la categoria, entonces la categoria aparece en el listado de categorias con Titulo = "Hogar" y Descripcion = "gastos del hogar".
- AC-03 (RF-04, RF-05): Dado que existe la categoria "sueldo" y el mensaje "$10.000 ingreso", cuando el agente procesa los mensajes, entonces se crea un movimiento con categoria = "sueldo", monto = "$10.000", tipo = "ingreso".
- AC-04 (RF-04, RF-05): Dado que existe la categoria "hogar" y el mensaje "$2.000 comida casa", cuando el agente procesa los mensajes, entonces se crea un movimiento con categoria = "hogar", monto = "$2.000", tipo = "egreso".
- AC-05 (RF-07): Dado un mensaje que no contiene un monto, cuando el agente procesa los mensajes, entonces el mensaje queda con error = true y motivo = "no contiene monto".
- AC-06 (RF-07): Dado un mensaje que contiene monto pero no descripcion, cuando el agente procesa los mensajes, entonces el mensaje queda con error = true y motivo = "no contiene descripcion".
- AC-07 (RF-06): Dado un mensaje clasificado correctamente, cuando el agente termina de procesarlo, entonces el mensaje queda con procesado = true.
- AC-08 (RF-08): Dado un listado de 20 movimientos de un mes, cuando se muestra el resumen mensual, entonces el resumen muestra una fila por categoria con la suma de los montos de esa categoria.
- AC-09 (RF-09): Dado un resumen mensual de 50 movimientos que representan 10 categorias, cuando se muestra, entonces se pagina de a 4 categorias por pagina.
- AC-10 (RF-10): Dado un mensaje con error = true, cuando se listan los mensajes con error, entonces el mensaje aparece en el listado de errores.
- AC-11 (RF-11): Dado un mensaje con error que ahora es valido, cuando el usuario lo vuelve a procesar, entonces el mensaje queda con procesado = true y se crea su movimiento.
- AC-12 (RF-12): Dado un movimiento con categoria = "Hogar", cuando el usuario edita su categoria a "Ocio", entonces el movimiento queda con categoria = "Ocio".
- AC-13 (RNF-01): Dado un conjunto de mensajes de prueba etiquetados, cuando el agente los clasifica, entonces la accuracy es mayor o igual a 80%.
- AC-14 (RNF-02): Dado un mensaje, cuando el agente lo clasifica, entonces responde en menos de 5 s medido en el p90.

## Fuera de Alcance
- No tiene sistema de usuarios.
- No incluye canal via WhatsApp.
- No responde el bot al mensaje enviado por el usuario.

## Riesgos y Dependencias
- Riesgo: La IA puede categorizar mal el movimiento -> mitigación: (RF-12, RNF-01) Si el usuario mira que el mensaje "$100 compra super" fue categorizado como "hobby" o ve que un movimiento tiene bajo accuracy, puede editarlo por la categoria correspondiente.
- Riesgo: El Bot que lee los mensajes puede guardar los mismos mensajes una y otra vez, dando asi, mensajes duplicados por cada corrida -> mitigacion: (RF-06) Por cada mensaje que se haya creado correctamente el movimiento se lo debe guardar como procesado = true.
- Dependencia: Modelo de IA via API Ollama local, instrucciones .md al agente categorizador, API Telegram, SQLite.

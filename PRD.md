# PRD-001: PersonalFinance — visor de financias personales

## Contexto y Problema
Personalmente los gastos no los manejo con nada, puedo pagar efectivo, credito, debito, ahorros y no tengo algo sencillo para ver los gastos/ahorros que voy teniendo en el mes.

## Objetivos
A partir de un mensaje de WhatsApp/Telegram, un Bot debe obtener ese mensaje y clasificar el gasto como un movimiento de una categoria.

## Requerimientos Funcionales
- RF-01: El sistema debe poder leer mensajes enviados por WPP/WhatsApp y guardarlos.
- RF-02: Se debe poder crear una categoria, poniendole Titulo + Descripcion
- RF-03: Cada mensaje debe ser leído por un Agente y este lo marque como procesado y clasifique como un movimiento a partir de las categorias hechas.
- RF-04: Se debe poder mostrar un resumen mensual por categoria.
- RF-05: Los mensajes que no pudieron ser procesados debido a un error, quedan en un listado.
- RF-06: Los mensajes procesados con error se pueden volver a procesar.
- RF-07: Un movimiento se puede recategorizar.

## Requerimientos No Funcionales
- RNF-01: El modelo debe devolver un % de accuracy en la clasificacion.
- RNF-02: El agente clasificador debe responder en menos de 5 s p90

## Criterios de Aceptación
- AC-01 (RF-01): Dados mensaje nuevo, cuando se leen los mensajes, entonces el mensaje queda como "no procesado"
- AC-02 (RF-03): Dado mensaje "$10.000 ingreso", cuando se procesan los mensajes, entonces debe quedar como categoria = "sueldo", monto = "$10.000", tipo = "ingreso"
- AC-03 (RF-03): Dado mensaje "$2.000 comida casa", cuando se procesan los mensajes, entonces debe quedar como categoria = "hogar", monto = "$2.000", tipo = "egreso"
- AC-04 (RF-04): Dado un listado de 20 movimientos, cuando se listan los movimientos mensuales, entonces se debe ver agrupados los movimientos por categoria.
- AC-05 (RF-04): Dado un listado de 50 movimientos que representan 10 categorias, cuando se listan los movimientos mensuales, entonces se debe poder paginar de a 4 categorias.
- AC-06 (RF-02, RF-03): Dada categoria, cuando crea una categoria, entonces debe poder ser clasificada por el agente.
- AC-07 (RF-03): Dado mensaje que no contenga un monto, cuando se procesan los mensajes, entonces el mensaje queda como error = true, motivo = no contiene monto.
- AC-08 (RF-03): Dado mensaje que no tenga descripcion pero si monto, cuando se procesan los mensajes, entonces el mensaje queda como error = true, motivo = no contiene descripcion.
- AC-09 (RF-05, RF-06): Dado un mensaje o mas con error, cuando se listan los mensajes con errores, entonces se pueden volver a procesar.
- AC-10 (RF-07, RF-02): Dado un movimiento con categoria = Hogar, cuando el usuario entra al movimiento, entonces se puede editar la categoria el movimiento.
- AC-11 (RF-01): Dados mensajes nuevos enviados por el usuario, cuando el Bot lee los mensajes, entonces el bot al terminar de leer los nuevos mensajes debe devolver "Mensajes guardados".

## Fuera de Alcance
- No tiene sistema de usuarios, Canal via WhatsApp, Respuesta de bot al mensaje enviado por el usuario. 

## Riesgos y Dependencias
- Riesgo: La IA puede categorizar mal el movimiento -> mitigación: (RNF-07) Si el usuario mira que el mensaje "$100 compra super" fue categorizado como "hobby" o ve que un movimiento tiene bajo accuracy, puede editarlo por la categoria correspondiente.
- Riesgo: El Bot que lee los mensajes puede guardar los mismos mensajes una y otra vez, dando asi, mensajes duplicados por cada corrida -> mitigacion: (RF-03) Por cada mensaje que se haya creado correctamente el movimiento se lo debe guardar como procesado = true.
- Dependencia: Modelo de IA via API Ollama local, instrucciones .md al agente categorizador, API Telegram, SQLite.
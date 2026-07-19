# Feature Specification: PersonalFinance — visor de finanzas personales

**Feature Branch**: `modulo-4`

**Created**: 2026-07-18

**Status**: Draft

**Input**: User description: "@PRD.md — PRD-001: PersonalFinance — visor de finanzas personales"

## Clarifications

### Session 2026-07-18

- Q: ¿Cuál es la unidad de paginación del resumen mensual: categorías o filas (categoría + moneda)? → A: Fila (categoría + moneda); 4 filas por página, las filas de una misma categoría pueden quedar partidas entre páginas.
- Q: ¿Cómo se calcula el equivalente en moneda base de una fila que agrupa movimientos con tipos de cambio históricos distintos? → A: Suma de equivalentes — cada movimiento se convierte con su propio TC histórico y se suman los resultados; no se usa un TC único ni promediado.
- Q: ¿Qué dispara la clasificación de los mensajes guardados como "no procesados"? → A: Automática en el mismo ciclo de ingesta — cada corrida guarda los mensajes nuevos y a continuación clasifica todos los pendientes, sin intervención del dueño.
- Q: ¿Desde qué superficie administra el dueño las categorías, monedas, errores y edición de movimientos? → A: Todas en el visor, como pantallas propias; el canal de mensajería queda exclusivamente como entrada de ingesta.
- Q: ¿Qué pasa con un mensaje que no se pudo clasificar porque el clasificador no respondió? → A: Reintento automático en cada ciclo hasta 3 intentos; superado el tope queda con error "clasificador no disponible" y aparece en la bandeja, reprocesable a mano.
- Q: ¿Puede el dueño corregir el tipo (ingreso/egreso) de un movimiento mal clasificado? → A: Sí — el tipo es editable desde la pantalla de edición del movimiento, igual que categoría, monto y moneda (CHK029).
- Q: Los edge cases mencionan monedas "desactivadas" pero ningún requisito define ese estado. ¿Se agrega el ciclo de vida o se quita la mención? → A: Se agrega, en espejo con el de Categoría — eliminar si no tiene movimientos, desactivar si los tiene, reactivar, y excluir las desactivadas de la clasificación; ARS exenta (CHK024).
- Q: ¿En qué orden se muestran las filas dentro de cada bloque del resumen? Sin orden definido la paginación es indeterminada. → A: Monto descendente por su equivalente en moneda base, con desempate alfabético por título de categoría y luego por código de moneda (CHK009).
- Q: ¿Cuántos decimales admite el monto de un movimiento? → A: 2 decimales fijos para toda moneda; el monto debe ser estrictamente mayor a cero (CHK001, CHK005).
- Q: ¿Qué precisión admite un tipo de cambio? → A: Hasta 2 decimales, misma regla que el monto; estrictamente mayor a cero (CHK002, CHK007).
- Q: ¿Cuándo se redondea el equivalente en moneda base? → A: Solo al mostrar — la conversión y la suma se calculan con precisión completa y el redondeo a 2 decimales se aplica una sola vez sobre el total de la fila (CHK003, CHK004).
- Q: ¿Cómo se interpretan los separadores numéricos de un mensaje? → A: Convención es-AR — el punto separa miles y la coma separa decimales (CHK006).
- Q: ¿Puede el dueño eliminar un movimiento creado por error? → A: Sí, eliminación definitiva; el movimiento deja de existir y de computar en el resumen (CHK031).
- Q: ¿Puede el dueño corregir la fecha de un movimiento? → A: Sí, es editable como cualquier otro atributo; el movimiento se reasigna al mes correspondiente a la nueva fecha (CHK030).
- Q: ¿Qué pasa con el tipo de cambio histórico cuando un movimiento se edita desde una moneda no base hacia la moneda base? → A: Se anula — el movimiento en moneda base queda sin tipo de cambio histórico, igual que cualquier otro movimiento en esa moneda (CHK016).
- Q: ¿La propagación del tipo de cambio editado alcanza a todos los movimientos de esa moneda y fecha, o solo a los que compartían el tipo de cambio anterior al editado? → A: A todos los de esa moneda y fecha, sin importar el tipo de cambio que tuvieran antes (CHK018).
- Q: ¿Qué zona horaria concreta determina a qué mes pertenece un movimiento? → A: Fija en `America/Argentina/Buenos_Aires` (UTC-3) para esta entrega, sin pantalla de configuración (CHK034).
- Q: ¿Qué tipo de cambio histórico recibe un movimiento creado al reprocesar un mensaje días después de ingerido? → A: El vigente al momento del reproceso; el sistema no mantiene un historial de cotizaciones por fecha, solo el valor vigente de cada moneda, así que es el único dato disponible (CHK017).
- Q: ¿La propagación del tipo de cambio alcanza movimientos de un mes distinto al que se está visualizando? → A: Es una consulta global por moneda y fecha, sin acotar al mes en pantalla; como misma fecha implica mismo mes, esto no cambia el resultado pero evita una query mal acotada (CHK020).
- Q: ¿Está especificado el formato y la validación del código de moneda? → A: ISO 4217 de 3 letras, normalizado a mayúsculas al guardar; la unicidad de FR-033 se evalúa case-insensitive (CHK027).
- Q: ¿Qué cuenta como "acierto" en el 80% de SC-001: solo la categoría, o también monto/tipo/moneda? → A: Categoría y tipo (ingreso/egreso) — lo que el modelo de IA decide con incertidumbre real; monto y moneda se parsean de forma determinística y sus errores son bugs de parsing, no de clasificación (CHK038).
- Q: ¿Qué valida el sistema al editar la categoría o la moneda de un movimiento hacia un valor inexistente? → A: Se rechaza con error, igual que el resto de las validaciones referenciales de la spec (CHK032).
- Q: ¿Puede reasignarse un movimiento a una categoría desactivada durante una corrección manual? → A: No, se rechaza con error — la desactivación bloquea toda asignación nueva, automática o manual (CHK033).
- Q: ¿Qué volumen de datos hace verificable el criterio de rendimiento del resumen (SC-003)? → A: Hasta 1.000 movimientos en el mes en curso, con hasta 20 categorías y 5 monedas activas (CHK037).
- Q: ¿El criterio de idempotencia de la ingesta (SC-007) debe verificar también los valores de los registros, no solo su cantidad? → A: Sí — ningún campo de un mensaje o movimiento existente cambia de valor tras una re-ingesta (CHK039).
- Q: ¿En qué momento debe reflejarse en el resumen una corrección manual, de forma verificable (SC-008)? → A: En la siguiente carga de la página del resumen, sin ventana de espera ni job asíncrono — es una vista derivada calculada on-demand (CHK040).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Registro automático de movimientos desde mensajes (Priority: P1)

El dueño escribe en su chat privado mensajes cortos y coloquiales con lo que gastó o cobró
("$2.000 comida casa", "$10.000 ingreso"). El sistema lee esos mensajes, los guarda una sola
vez, y a partir de cada uno crea un movimiento con monto, tipo (ingreso o egreso), categoría y
moneda, sin que el dueño tenga que completar ningún formulario.

**Why this priority**: Es el corazón de la propuesta de valor: eliminar el registro manual. Sin
esta captura no hay datos y ninguna otra historia tiene sentido.

**Independent Test**: Se puede probar de punta a punta enviando mensajes desde el chat
autorizado y verificando que quedan movimientos creados con los atributos correctos, que un
mensaje repetido no genera un segundo registro y que un mensaje de otro chat se descarta.

**Acceptance Scenarios**:

1. **Given** un mensaje nuevo del chat autorizado, **When** el sistema lo lee y lo guarda,
   **Then** el mensaje queda almacenado con estado "no procesado".
2. **Given** un mensaje enviado desde un chat distinto al del dueño, **When** el sistema lee los
   mensajes, **Then** el mensaje no se guarda y no genera ningún movimiento.
3. **Given** un mensaje con identificador 123 ya guardado, **When** el sistema vuelve a leer los
   mensajes y recibe nuevamente el identificador 123, **Then** no se guarda un segundo registro
   y la cantidad de mensajes almacenados no cambia.
4. **Given** que existe la categoría "sueldo" y el mensaje "$10.000 ingreso" sin moneda
   explícita, **When** se procesan los mensajes, **Then** se crea un movimiento con categoría
   "sueldo", monto $10.000, tipo ingreso, moneda ARS y sin tipo de cambio histórico.
5. **Given** que existe la categoría "hogar" y el mensaje "$2.000 comida casa" sin moneda
   explícita, **When** se procesan los mensajes, **Then** se crea un movimiento con categoría
   "hogar", monto $2.000, tipo egreso, moneda ARS y sin tipo de cambio histórico.
6. **Given** que existe la categoría "Ahorro" y el mensaje "Saqué $800 de ahorros", **When** se
   procesa el mensaje, **Then** se crea un movimiento con categoría "Ahorro" y tipo ingreso.
7. **Given** un mensaje del que se creó un movimiento con categoría asignada, **When** termina
   su procesamiento, **Then** el mensaje queda marcado como procesado.
7.a. **Given** un mensaje nuevo en el chat autorizado, **When** corre un ciclo de ingesta,
   **Then** el mensaje queda guardado y clasificado en esa misma corrida, sin que el dueño realice
   ninguna acción adicional.
8. **Given** que la categoría "Hogar" está desactivada, **When** se clasifica un mensaje nuevo,
   **Then** no se asigna la categoría "Hogar" al movimiento.
9. **Given** un sistema recién inicializado sin monedas cargadas por el usuario, **When** se
   consultan las monedas disponibles, **Then** existe la moneda ARS como moneda base.

---

### User Story 2 - Resumen mensual de ingresos y egresos (Priority: P1)

En cualquier momento del mes el dueño abre el visor y ve, de un vistazo, cuánto gastó y cuánto
ingresó, agrupado por categoría y por moneda, en dos bloques separados que se paginan de forma
independiente.

**Why this priority**: Es el objetivo declarado del producto. Junto con la Historia 1 forma el
MVP: capturar sin esfuerzo y ver el resultado.

**Independent Test**: Se puede probar cargando un conjunto conocido de movimientos del mes y
verificando los totales por categoría y moneda, la separación entre ingresos y egresos, y la
paginación de cada bloque.

**Acceptance Scenarios**:

1. **Given** 3 movimientos de egreso del mes en ARS de la categoría "Hogar" por $2.000, $3.000 y
   $1.500, **When** se muestra el resumen mensual, **Then** en el bloque de egresos aparece una
   fila "Hogar — ARS" con monto $6.500.
2. **Given** el bloque de egresos con 10 filas (combinaciones de categoría y moneda), **When** se
   muestra, **Then** ese bloque se pagina de forma independiente de a 4 filas por página,
   resultando en 3 páginas (dos de 4 filas y una final de 2).
2.a. **Given** un bloque de egresos con las filas "Ahorro — USD" (equivalente $1.160.000),
   "Hogar — ARS" ($50.000), "Ahorro — ARS" ($30.000), "Ocio — ARS" ($20.000) y "Viajes — ARS"
   ($20.000), **When** se muestra, **Then** la primera página contiene, en ese orden,
   "Ahorro — USD", "Hogar — ARS", "Ahorro — ARS" y "Ocio — ARS" —las dos filas de "Ahorro" cuentan
   por separado y no quedan contiguas—, y la segunda página contiene "Viajes — ARS", que pierde el
   desempate con "Ocio — ARS" por orden alfabético.
2.b. **Given** el mismo conjunto de movimientos del mes, **When** se muestra el resumen dos veces
   consecutivas, **Then** ambas veces las filas aparecen en la misma secuencia y en las mismas
   páginas.
3. **Given** un movimiento de la categoría "Ahorro" en USD por 800 con tipo de cambio histórico
   1450, **When** se muestra el resumen mensual, **Then** en el bloque de egresos aparece una
   fila "Ahorro — USD" con monto U$S 800 y equivalente histórico $1.160.000 ARS, separada de las
   filas en ARS de esa categoría.
3.a. **Given** dos movimientos de egreso de la categoría "Ahorro" en USD, uno de 800 con tipo de
   cambio histórico 1450 y otro de 200 con tipo de cambio histórico 1500, **When** se muestra el
   resumen mensual, **Then** la fila "Ahorro — USD" muestra monto U$S 1.000 y equivalente
   histórico $1.460.000 ARS (800×1450 + 200×1500), y no el resultado de aplicar un único tipo de
   cambio a los U$S 1.000.
3.b. **Given** dos movimientos de egreso de la categoría "Viajes" en USD, cada uno de U$S 1,01 con
   tipo de cambio histórico 1450,55, **When** se muestra el resumen mensual, **Then** la fila
   "Viajes — USD" muestra equivalente $2.930,11 —resultado de sumar 1465,0555 + 1465,0555 y
   redondear una sola vez— y no $2.930,12, que sería el resultado de redondear cada movimiento
   antes de sumarlos.
4. **Given** un mes con un egreso de $800 y un ingreso de $800, ambos en la categoría "Ahorro" y
   en ARS, **When** se muestra el resumen mensual, **Then** el bloque de egresos incluye una fila
   "Ahorro — ARS" con monto $800 y el bloque de ingresos incluye otra fila "Ahorro — ARS" con
   monto $800, sin netearse entre sí.
5. **Given** el bloque de egresos con las filas "Ahorro — USD" (equivalente $1.160.000),
   "Hogar — ARS" ($50.000), "Ahorro — ARS" ($30.000), "Ocio — ARS" ($20.000) y "Viajes — ARS"
   ($20.000), **When** se muestra la primera página (que solo exhibe las primeras 4 filas),
   **Then** el total general del bloque de egresos es $1.280.000 —la suma de las cinco filas del
   mes— y no $1.260.000, que sería la suma de solo las cuatro filas visibles en esa página.

---

### User Story 3 - Gestión del ciclo de vida de categorías (Priority: P2)

El dueño define y mantiene las categorías con las que se clasifican sus movimientos: las crea,
las lista, edita su título y descripción, elimina las que no se usaron y desactiva (en lugar de
borrar) las que ya tienen historial, pudiendo reactivarlas después.

**Why this priority**: Sin categorías propias la clasificación no refleja cómo el dueño piensa
sus gastos, pero el sistema puede arrancar con un set inicial mínimo, por eso va después del MVP.

**Independent Test**: Se puede probar completamente desde la pantalla de categorías creando,
editando, eliminando, desactivando y reactivando, sin depender de la ingesta de mensajes.

**Acceptance Scenarios**:

1. **Given** una categoría con título "Hogar" y descripción "gastos del hogar", **When** se
   crea, **Then** aparece en el listado con ese título, esa descripción y estado "activa".
2. **Given** que ya existe una categoría con título "Hogar", **When** se intenta crear otra con
   título "Hogar", **Then** el sistema rechaza la creación con error.
3. **Given** una categoría con título "Hogar", **When** se edita su título a "Casa", **Then** la
   categoría queda con título "Casa".
4. **Given** que ya existe una categoría con título "Ocio", **When** se intenta editar otra para
   que su título sea "Ocio", **Then** el sistema rechaza la edición con error.
5. **Given** una categoría con descripción "gastos del hogar", **When** se edita a "gastos de la
   casa", **Then** la categoría queda con esa descripción.
6. **Given** una categoría sin movimientos asociados, **When** se elimina, **Then** deja de
   aparecer en el listado de categorías.
7. **Given** una categoría con al menos un movimiento asociado, **When** se intenta eliminar,
   **Then** la categoría queda desactivada y no eliminada.
8. **Given** una categoría desactivada con título "Ocio", **When** se lista, **Then** aparece en
   el listado con estado "desactivada".
9. **Given** una categoría desactivada con título "Ocio", **When** se edita su título a
   "Entretenimiento", **Then** queda con ese título y conserva el estado "desactivada".
10. **Given** una categoría desactivada, **When** se reactiva, **Then** vuelve a estar disponible
    para clasificar nuevos movimientos.

---

### User Story 4 - Bandeja de mensajes con error y reproceso (Priority: P2)

Cuando un mensaje no puede convertirse en movimiento, el dueño lo ve en un listado con el motivo
del problema, corrige la causa (por ejemplo, da de alta la moneda faltante) y vuelve a
procesarlo, sin perder el gasto.

**Why this priority**: Evita pérdida silenciosa de datos y sostiene la confianza en el resumen,
pero el sistema es usable sin esta pantalla mientras los mensajes se procesen bien.

**Independent Test**: Se puede probar forzando mensajes con error conocido (sin monto, sin
descripción, con moneda no cargada), verificando que aparecen en el listado con su motivo y que
al corregir y reprocesar quedan procesados.

**Acceptance Scenarios**:

1. **Given** un mensaje que no contiene un monto, **When** se procesa, **Then** queda con error y
   motivo "no contiene monto".
2. **Given** un mensaje que contiene monto pero no descripción, **When** se procesa, **Then**
   queda con error y motivo "no contiene descripción".
3. **Given** un mensaje que menciona una moneda no cargada en el sistema (por ejemplo "100 EUR
   viaje" sin que exista la moneda EUR), **When** se procesa, **Then** queda con error y motivo
   "moneda no soportada".
4. **Given** un mensaje con error, **When** se listan los mensajes con error, **Then** el mensaje
   aparece en el listado.
4.a. **Given** un mensaje cuya clasificación falló porque el clasificador no respondió, **When**
   corren dos ciclos de ingesta más y el clasificador sigue sin responder, **Then** tras el tercer
   intento el mensaje queda con error y motivo "clasificador no disponible" y aparece en el
   listado de errores.
4.b. **Given** un mensaje cuyo primer intento de clasificación falló por falta de respuesta del
   clasificador, **When** corre el siguiente ciclo de ingesta y el clasificador responde, **Then**
   el mensaje se procesa normalmente, se crea su movimiento y no queda con error.
5. **Given** el mensaje "100 EUR viaje" que quedó con error "moneda no soportada", **When** el
   dueño agrega la moneda EUR con tipo de cambio 1000 y vuelve a procesar el mensaje, **Then** el
   mensaje queda procesado y se crea su movimiento en EUR con tipo de cambio histórico 1000 —el
   vigente al momento del reproceso, no uno vigente al momento del mensaje original, que no
   existe porque la moneda EUR no estaba dada de alta cuando llegó el mensaje.
5.a. **Given** tres mensajes con error, de los cuales dos ya tienen su causa corregida y uno no,
   **When** el dueño reprocesa todos los mensajes de la bandeja en una sola acción, **Then** los
   dos corregidos quedan procesados con su movimiento, el sistema informa "2 de 3 reprocesados
   correctamente" y el mensaje no resuelto sigue apareciendo en la bandeja con su motivo de error.

---

### User Story 5 - Corrección manual de movimientos (Priority: P2)

Cuando la clasificación automática se equivoca o el monto quedó mal, el dueño corrige el
movimiento a mano: cambia su categoría, su tipo (ingreso o egreso), su monto o su moneda.

**Why this priority**: Es la mitigación directa del riesgo de mala categorización automática.
Necesaria para que el resumen sea confiable, pero posterior a tener resumen y captura.

**Independent Test**: Se puede probar sobre movimientos ya existentes, editando cada atributo y
verificando el resultado y el impacto en el resumen.

**Acceptance Scenarios**:

1. **Given** un movimiento con categoría "Hogar", **When** el dueño edita su categoría a "Ocio",
   **Then** el movimiento queda con categoría "Ocio".
2. **Given** un movimiento con monto $2.000, **When** se edita su monto a $2.500, **Then** el
   movimiento queda con monto $2.500.
3. **Given** un movimiento en ARS y la moneda USD con tipo de cambio 1500, **When** se edita la
   moneda del movimiento a USD, **Then** el movimiento queda en USD con tipo de cambio histórico
   1500.
4. **Given** un movimiento de $10.000 en ARS clasificado como egreso en la categoría "Sueldo",
   **When** el dueño edita su tipo a ingreso, **Then** el movimiento queda como ingreso con monto
   $10.000, y en el resumen mensual la fila "Sueldo — ARS" desaparece del bloque de egresos y
   aparece en el bloque de ingresos por $10.000.
5. **Given** un movimiento en USD con tipo de cambio histórico 1450, **When** el dueño edita
   únicamente su tipo, **Then** el movimiento conserva su monto, su moneda USD y su tipo de cambio
   histórico 1450.
6. **Given** un movimiento creado por error a partir de un mensaje mal interpretado, **When** el
   dueño lo elimina, **Then** el movimiento deja de existir, deja de aparecer en el resumen
   mensual y el Mensaje de origen no se ve afectado.
7. **Given** un movimiento con fecha 2026-07-05, **When** el dueño edita su fecha a 2026-06-30,
   **Then** el movimiento deja de aparecer en el resumen mensual de julio y aparece en el de
   junio, conservando su monto, tipo, moneda y tipo de cambio histórico.
8. **Given** un movimiento en USD con tipo de cambio histórico 1450, **When** el dueño edita su
   moneda a ARS (la moneda base), **Then** el movimiento queda en ARS sin tipo de cambio
   histórico.
9. **Given** la categoría "Ocio" desactivada, **When** el dueño intenta recategorizar un
   movimiento hacia "Ocio", **Then** el sistema rechaza la edición con error y el movimiento
   conserva su categoría anterior.

---

### User Story 6 - Monedas y tipo de cambio histórico (Priority: P3)

El dueño da de alta monedas distintas de ARS con su tipo de cambio, lo actualiza cuando cambia,
da de baja las que ya no usa —eliminándolas si nunca se usaron o desactivándolas si tienen
historial, con opción de reactivarlas—, y puede corregir a mano el tipo de cambio histórico
registrado en un movimiento, decidiendo si esa corrección se propaga a los demás movimientos de la
misma moneda y fecha.

**Why this priority**: Enriquece el resumen para quien ahorra en moneda extranjera, pero la app
entrega valor completo operando solo en ARS.

**Independent Test**: Se puede probar dando de alta una moneda, creando movimientos con ella,
editando su cotización y verificando que los movimientos previos conservan su tipo de cambio
histórico.

**Acceptance Scenarios**:

1. **Given** que no existe la moneda USD, **When** se agrega USD con tipo de cambio 1450,
   **Then** la moneda USD queda disponible con tipo de cambio 1450.
2. **Given** que ya existe la moneda USD, **When** se intenta agregar otra moneda USD, **Then**
   el sistema rechaza la creación con error.
3. **Given** la moneda USD con tipo de cambio 1450 y un movimiento en USD con tipo de cambio
   histórico 1450, **When** se edita el tipo de cambio de USD a 1500, **Then** el movimiento
   existente conserva 1450 y los movimientos nuevos en USD usan 1500.
4. **Given** que existe la moneda USD con tipo de cambio 1450 y la categoría "ahorro", **When**
   se procesa el mensaje "800 USD ahorro", **Then** se crea un movimiento con categoría "ahorro",
   monto 800, moneda USD, tipo egreso y tipo de cambio histórico 1450.
5. **Given** un movimiento en USD con tipo de cambio histórico 1500, **When** el dueño lo edita
   manualmente a 1450, **Then** el movimiento queda con tipo de cambio histórico 1450.
6. **Given** 3 movimientos en USD con fecha 2026-07-10 y tipo de cambio histórico 1500, **When**
   se edita el de uno de ellos a 1450 y se confirma aplicar a los demás, **Then** los 3
   movimientos quedan con tipo de cambio histórico 1450.
7. **Given** 3 movimientos en USD con fecha 2026-07-10 y tipo de cambio histórico 1500, **When**
   se edita el de uno de ellos a 1450 y el dueño NO confirma aplicar a los demás, **Then** solo
   ese movimiento queda con 1450 y los otros dos conservan 1500.
7.a. **Given** dos movimientos en USD con fecha 2026-07-10, uno con tipo de cambio histórico 1500
   y otro ya editado antes a 1480, **When** se edita el primero a 1450 y se confirma aplicar a los
   demás, **Then** el segundo también queda con tipo de cambio histórico 1450, sin importar que su
   valor previo (1480) fuera distinto al del movimiento editado.
8. **Given** la moneda EUR sin movimientos asociados, **When** se elimina, **Then** deja de
   aparecer en el listado de monedas.
9. **Given** la moneda USD con al menos un movimiento asociado, **When** se intenta eliminar,
   **Then** la moneda queda desactivada y no eliminada, y los movimientos en USD conservan su
   tipo de cambio histórico.
10. **Given** la moneda USD desactivada, **When** se procesa el mensaje "800 USD ahorro",
    **Then** el mensaje queda con error y motivo "moneda no soportada".
11. **Given** la moneda USD desactivada, **When** se reactiva, **Then** vuelve a estar disponible
    para clasificar nuevos movimientos con su tipo de cambio vigente.
12. **Given** la moneda base ARS con movimientos asociados, **When** se intenta eliminarla o
    desactivarla, **Then** el sistema rechaza la operación con error y ARS permanece activa.
13. **Given** un movimiento en USD creado antes de que USD se desactivara, **When** se muestra el
    resumen mensual, **Then** el movimiento sigue apareciendo en su fila "categoría — USD" con su
    equivalente en moneda base.

---

### Edge Cases

- **Ningún mensaje del mes**: el resumen mensual se muestra vacío, con ambos bloques presentes y
  totales en cero, no un error.
- **Mensaje ambiguo o sin categoría adecuada**: si no hay confianza suficiente para asignar una
  categoría activa, el mensaje queda con error y motivo, y se deriva a la bandeja de errores en
  lugar de asumir una categoría.
- **No existe ninguna categoría activa**: todo mensaje nuevo queda con error indicando que no hay
  categorías disponibles para clasificar.
- **Bloque de resumen con menos de 4 filas**: se muestra una única página, sin controles de
  navegación activos.
- **Un bloque vacío y el otro con datos**: cada bloque pagina de forma independiente; el vacío no
  afecta la paginación del otro.
- **Eliminar o desactivar la moneda base ARS**: no se permite; ARS es preexistente y no requiere
  tipo de cambio.
- **Mensaje que menciona una moneda desactivada o inexistente**: se trata como "moneda no
  soportada" y va a la bandeja de errores.
- **Reproceso de un mensaje que ya generó movimiento**: no se duplica el movimiento; solo se
  reprocesan mensajes en estado de error.
- **Movimiento cuya categoría fue desactivada después de crearse**: el movimiento conserva su
  categoría y sigue apareciendo en el resumen; la categoría desactivada solo se excluye de
  clasificaciones nuevas.
- **Mensaje que llega mientras el clasificador no responde**: el mensaje queda guardado y no
  procesado, para ser clasificado en la siguiente corrida. Si el clasificador sigue sin responder
  y se agotan los 3 intentos, el mensaje pasa a error "clasificador no disponible" y aparece en la
  bandeja, en lugar de quedar invisible.
- **Mensaje recibido en el límite entre dos meses**: la fecha del movimiento es la fecha del
  mensaje (día calendario en la zona horaria fija de la Assumption "Fecha del movimiento"), que
  determina el mes sin ambigüedad; no requiere tratamiento especial más allá de esa regla.
- **Reproceso de un mensaje ingerido en un mes anterior**: el movimiento creado toma la fecha
  original del mensaje y se asigna al mes de esa fecha, no al mes del reproceso; en cambio, su
  tipo de cambio histórico sí es el vigente al momento del reproceso (FR-017a). Fecha de
  asignación al mes y tipo de cambio se resuelven de forma independiente.

## Requirements *(mandatory)*

### Functional Requirements

**Ingesta de mensajes**

- **FR-001**: El sistema MUST leer los mensajes enviados por el canal de mensajería del dueño.
- **FR-002**: El sistema MUST ingerir únicamente los mensajes provenientes del chat autorizado
  del dueño; los mensajes de cualquier otro chat se descartan sin guardarse.
- **FR-003**: El sistema MUST guardar los mensajes leídos.
- **FR-004**: El sistema MUST NOT guardar dos veces el mismo mensaje, identificándolo por el
  identificador único que le asigna el canal de mensajería.

**Clasificación en movimientos**

- **FR-005**: El sistema MUST crear un movimiento a partir de cada mensaje válido.
- **FR-005a**: El sistema MUST ejecutar la clasificación automáticamente dentro del mismo ciclo de
  ingesta, inmediatamente después de guardar los mensajes nuevos, procesando todos los mensajes en
  estado "no procesado". La clasificación MUST NOT requerir ninguna acción del dueño.
- **FR-006**: El sistema MUST determinar, para cada movimiento, si es de tipo ingreso o egreso a
  partir del contenido del mensaje.
- **FR-007**: El sistema MUST clasificar cada movimiento en una de las categorías activas.
- **FR-008**: El sistema MUST asignar la moneda base ARS al movimiento cuando el mensaje no
  indica una moneda explícita.
- **FR-009**: El sistema MUST marcar como procesado cada mensaje del que se creó un movimiento.
- **FR-010**: El sistema MUST marcar con error y motivo cada mensaje que no pueda convertirse en
  un movimiento, cubriendo al menos los motivos "no contiene monto", "no contiene descripción",
  "moneda no soportada" y "clasificador no disponible".
- **FR-010a**: Ante un fallo del clasificador (sin respuesta, timeout o error del modelo), el
  sistema MUST dejar el mensaje como no procesado, registrar el intento y reintentarlo en el
  siguiente ciclo de ingesta, hasta un máximo de 3 intentos.
- **FR-010b**: Superados los 3 intentos, el sistema MUST marcar el mensaje con error y motivo
  "clasificador no disponible", de modo que quede visible en la bandeja de errores y reprocesable
  manualmente. Ningún mensaje ingerido MUST permanecer indefinidamente en estado no procesado sin
  ser visible para el dueño.
- **FR-011**: El sistema MUST derivar a error, en lugar de asumir un valor, todo mensaje para el
  cual no haya confianza suficiente en la categoría, el monto o el tipo del movimiento.

**Resumen mensual**

- **FR-012**: El sistema MUST mostrar un resumen mensual dividido en dos bloques —ingresos y
  egresos— agrupando dentro de cada bloque los movimientos por categoría y moneda.
- **FR-012a**: El sistema MUST mostrar, para cada bloque, un total general correspondiente a la
  totalidad de las filas del bloque en el mes en curso —no solo a las de la página visible—,
  calculado como la suma de los equivalentes en moneda base de esas filas, con el mismo criterio
  de precisión y redondeo único de FR-040.
- **FR-013**: El sistema MUST mostrar, para cada fila del resumen expresada en una moneda
  distinta de la base, su equivalente en moneda base calculado como la suma de los equivalentes
  individuales: cada movimiento agrupado se convierte con su propio tipo de cambio histórico y los
  resultados se suman. El sistema MUST NOT aplicar un tipo de cambio único, promediado o más
  reciente al total de la fila. La fila MUST NOT exponer un tipo de cambio propio como dato
  visible: solo se muestran el total en la moneda de la fila y su equivalente ya sumado en moneda
  base, sin una columna de tipo de cambio.
- **FR-014**: El sistema MUST NOT netear ingresos contra egresos de una misma categoría: cada
  bloque totaliza por separado.
- **FR-015**: El sistema MUST paginar cada bloque del resumen mensual de forma independiente, de
  a 4 filas por página, donde una fila es una combinación de categoría y moneda. Las filas de una
  misma categoría en distintas monedas cuentan por separado y pueden quedar en páginas distintas.
- **FR-015a**: El sistema MUST ordenar las filas de cada bloque de forma descendente por el monto
  de la fila expresado en moneda base, de modo que la primera página contenga las filas de mayor
  peso económico. Ante montos equivalentes iguales, el desempate MUST ser alfabético por título de
  categoría y, si persiste, por código de moneda. El orden resultante MUST ser determinístico: dos
  consultas consecutivas sobre los mismos datos producen la misma secuencia de filas.

**Errores y reproceso**

- **FR-016**: El sistema MUST listar los mensajes que quedaron con error, mostrando su motivo.
- **FR-017**: Los usuarios MUST poder volver a procesar los mensajes con error.
- **FR-017a**: El sistema MUST registrar, en el movimiento creado por un reproceso, el tipo de
  cambio vigente de la moneda al momento del reproceso —no el vigente al momento del mensaje
  original—, dado que el sistema no mantiene un historial de cotizaciones por fecha.
- **FR-017b**: Los usuarios MUST poder reprocesar en una sola acción todos los mensajes que están
  con error. El fallo al reprocesar un mensaje MUST NOT impedir el reproceso de los restantes: el
  sistema MUST continuar con el lote e informar al final cuántos mensajes se resolvieron sobre el
  total intentado. Todo mensaje que no se pudo resolver MUST seguir visible en la bandeja de
  errores con su motivo, de modo que ningún mensaje desaparezca de la bandeja sin haberse
  clasificado.

**Movimientos**

- **FR-018**: Los usuarios MUST poder recategorizar un movimiento. El sistema MUST rechazar con
  error una categoría que no existe en el sistema o que está desactivada: la desactivación
  bloquea toda asignación nueva, tanto de la clasificación automática (FR-031) como de la
  corrección manual.
- **FR-018a**: Los usuarios MUST poder cambiar el tipo de un movimiento existente entre ingreso y
  egreso. Al hacerlo, el movimiento MUST dejar de computar en el bloque del tipo anterior y pasar
  a computar en el del nuevo tipo, sin alterar su monto, moneda ni tipo de cambio histórico.
- **FR-019**: Los usuarios MUST poder editar el monto de un movimiento existente, sin que esa
  edición modifique su tipo de cambio histórico.
- **FR-020**: Los usuarios MUST poder editar la moneda de un movimiento existente. El sistema
  MUST rechazar con error una moneda que no existe en el sistema.
- **FR-020a**: Los usuarios MUST poder editar la fecha de un movimiento existente. Al hacerlo, el
  sistema MUST reasignar el movimiento al mes correspondiente a la nueva fecha en el resumen
  mensual, sin alterar su monto, tipo, moneda ni tipo de cambio histórico.
- **FR-021**: El sistema MUST registrar el tipo de cambio vigente de la moneda al momento de
  editar un movimiento a una moneda distinta de la que tenía.
- **FR-021a**: Cuando la moneda de un movimiento se edita hacia la moneda base, el sistema MUST
  anular su tipo de cambio histórico, de modo que un movimiento en moneda base nunca conserve un
  valor residual de una moneda anterior.
- **FR-022**: Los usuarios MUST poder editar manualmente el tipo de cambio histórico registrado
  en un movimiento.
- **FR-023**: El sistema MUST preguntar si aplicar el tipo de cambio editado a los demás
  movimientos de la misma moneda y fecha, y aplicarlo solo a esos movimientos si el usuario
  confirma. El criterio de coincidencia MUST ser exclusivamente moneda y fecha del movimiento
  editado: alcanza a todos los movimientos que compartan ambos valores, sin importar el tipo de
  cambio histórico que tuvieran antes de la propagación ni si ese valor previo se había fijado de
  forma automática o mediante una edición manual anterior. "Misma fecha" MUST interpretarse como
  el mismo día calendario en la zona horaria fija de la Assumption "Fecha del movimiento". La
  búsqueda de movimientos alcanzados MUST ser global por moneda y fecha, sin acotarse al mes que
  el dueño esté visualizando en el resumen.
- **FR-023a**: Los usuarios MUST poder eliminar un movimiento existente. La eliminación MUST ser
  definitiva: el movimiento deja de existir y deja de computar en el resumen mensual, sin dejar
  rastro ni afectar al Mensaje de origen ni a otros movimientos.

**Categorías**

- **FR-024**: Los usuarios MUST poder crear una categoría con título único y descripción; un
  título duplicado se rechaza con error.
- **FR-025**: El sistema MUST listar las categorías existentes con su estado (activa o
  desactivada).
- **FR-026**: Los usuarios MUST poder editar el título de una categoría existente, incluida una
  desactivada, manteniendo la unicidad del título y sin alterar su estado.
- **FR-027**: Los usuarios MUST poder editar la descripción de una categoría existente.
- **FR-028**: Los usuarios MUST poder eliminar una categoría que no tiene movimientos asociados.
- **FR-029**: El sistema MUST desactivar, en lugar de eliminar, una categoría que tiene
  movimientos asociados.
- **FR-030**: Los usuarios MUST poder reactivar una categoría desactivada.
- **FR-031**: El sistema MUST excluir las categorías desactivadas de la clasificación de nuevos
  movimientos, conservando la categoría de los movimientos ya creados.

**Monedas**

- **FR-032**: El sistema MUST tener la moneda ARS preexistente como moneda base, sin que el
  usuario deba cargarla y sin requerir tipo de cambio.
- **FR-033**: Los usuarios MUST poder agregar una moneda nueva con su tipo de cambio respecto a
  la moneda base; un código de moneda duplicado se rechaza con error. El código MUST cumplir el
  formato ISO 4217 (3 letras) y MUST normalizarse a mayúsculas al guardar, de modo que la
  unicidad se evalúe sin distinguir mayúsculas de minúsculas.
- **FR-034**: Los usuarios MUST poder editar el tipo de cambio de una moneda existente.
- **FR-035**: El sistema MUST registrar en cada movimiento el tipo de cambio histórico de su
  moneda al momento de crearse, y ese valor MUST NOT modificarse cuando se actualiza el tipo de
  cambio de la moneda. Esta inmutabilidad rige frente a la actualización automática de la
  cotización de la moneda; la única excepción es la edición manual y explícita del dueño prevista
  en FR-022 y FR-023, que es la vía deliberada para corregir un tipo de cambio histórico mal
  registrado.
- **FR-035a**: El sistema MUST listar las monedas existentes con su estado (activa o desactivada)
  y su tipo de cambio vigente.
- **FR-035b**: Los usuarios MUST poder eliminar una moneda que no tiene movimientos asociados.
- **FR-035c**: El sistema MUST desactivar, en lugar de eliminar, una moneda que tiene movimientos
  asociados, preservando el tipo de cambio histórico de esos movimientos.
- **FR-035d**: Los usuarios MUST poder reactivar una moneda desactivada.
- **FR-035e**: El sistema MUST excluir las monedas desactivadas de la clasificación de nuevos
  movimientos, tratando el mensaje que menciona una moneda desactivada con el mismo motivo de
  error que una moneda inexistente ("moneda no soportada"), y MUST conservar la moneda de los
  movimientos ya creados.
- **FR-035f**: El sistema MUST NOT permitir eliminar ni desactivar la moneda base ARS,
  independientemente de que tenga o no movimientos asociados.

**Superficie de interacción**

- **FR-036**: El sistema MUST exponer en el visor las pantallas desde las cuales el dueño realiza
  todas las operaciones manuales especificadas: gestión de categorías (FR-024 a FR-031), gestión
  de monedas (FR-032 a FR-035f), bandeja de mensajes con error y reproceso (FR-016, FR-017) y
  edición de movimientos (FR-018 a FR-023).
- **FR-037**: El canal de mensajería MUST usarse únicamente como entrada de ingesta; el sistema
  MUST NOT ofrecer comandos ni respuestas por ese canal para administrar categorías, monedas,
  movimientos ni reprocesos.

**Precisión y representación numérica**

- **FR-038**: El sistema MUST almacenar y mostrar el monto de todo movimiento con exactamente 2
  decimales, cualquiera sea su moneda. El monto MUST ser estrictamente mayor a cero: el sentido
  económico lo aporta el tipo (ingreso o egreso), nunca el signo del monto.
- **FR-039**: El sistema MUST admitir tipos de cambio con hasta 2 decimales y MUST rechazar con
  error todo tipo de cambio menor o igual a cero, tanto al agregar una moneda como al editar su
  cotización o el tipo de cambio histórico de un movimiento.
- **FR-040**: El sistema MUST calcular la conversión de cada movimiento a moneda base y la suma de
  esos equivalentes con precisión completa, sin redondeos intermedios, y MUST aplicar el redondeo
  a 2 decimales una sola vez, sobre el valor que se muestra. El redondeo MUST ser al más cercano,
  resolviendo el empate hacia arriba.
- **FR-041**: El sistema MUST interpretar los montos escritos en los mensajes según la convención
  local del dueño: el punto separa miles y la coma separa decimales ("$2.000" son dos mil,
  "$2.000,50" son dos mil con cincuenta centavos). Un monto que no pueda interpretarse sin
  ambigüedad bajo esa convención MUST derivarse a error con motivo "no contiene monto", en lugar
  de asumir un valor.

### Key Entities

- **Mensaje**: texto recibido en el chat autorizado. Atributos: identificador único del canal,
  texto, fecha de recepción, estado de procesamiento (procesado / no procesado), cantidad de
  intentos de clasificación, indicador de error y motivo del error. Origen de cero o un Movimiento.
- **Movimiento**: registro económico derivado de un Mensaje. Atributos: monto, tipo (ingreso o
  egreso), fecha, moneda, tipo de cambio histórico (no aplica en moneda base). Relacionado con
  una Categoría y con un Mensaje de origen.
- **Categoría**: agrupador de movimientos definido por el dueño. Atributos: título único,
  descripción, estado (activa / desactivada). Relacionada con cero o más Movimientos.
- **Moneda**: unidad en la que se expresa un movimiento. Atributos: código, indicador de moneda
  base, estado (activa / desactivada), tipo de cambio vigente respecto de la moneda base (no
  aplica en la base). Relacionada con cero o más Movimientos.
- **Resumen mensual**: vista derivada, no persistida. Agrupa los Movimientos de un mes por tipo,
  categoría y moneda, con su total y su equivalente en moneda base.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Sobre un conjunto de al menos 50 mensajes de prueba etiquetados que cubre todas las
  categorías existentes, la clasificación automática acierta en al menos el 80% de los casos. Un
  caso cuenta como acierto cuando coinciden tanto la categoría como el tipo (ingreso/egreso)
  asignados; monto y moneda quedan fuera de esta métrica por derivarse de un parseo determinístico
  del texto (FR-041, FR-008), no de la clasificación por IA.
- **SC-002**: La clasificación de un mensaje se completa en menos de 5 segundos, medido en el
  percentil 90.
- **SC-003**: El resumen mensual se muestra en menos de 1 segundo, medido en el percentil 95,
  sobre un volumen de referencia de hasta 1.000 movimientos en el mes en curso, 20 categorías y
  5 monedas activas.
- **SC-004**: El dueño registra un gasto sin abrir la aplicación: escribir un mensaje de una
  línea es la única acción requerida, con cero campos de formulario a completar.
- **SC-005**: El dueño consulta cuánto gastó y cuánto ahorró en el mes, agrupado por categoría,
  en una sola pantalla y sin pasos intermedios.
- **SC-006**: Ningún mensaje del chat autorizado se pierde: todo mensaje ingerido queda o bien
  procesado con su movimiento, o bien visible en el listado de errores con un motivo.
- **SC-007**: Ejecutar la ingesta varias veces sobre los mismos mensajes no altera la cantidad de
  mensajes ni de movimientos almacenados, ni el valor de ninguno de sus campos existentes.
- **SC-008**: Corregir la categoría de un movimiento mal clasificado se refleja en el resumen
  mensual sin pasos adicionales de recálculo por parte del usuario: la siguiente carga de la
  página del resumen ya muestra el cambio, sin ventana de espera ni proceso asíncrono
  intermedio.

## Assumptions

- **Mono-usuario**: la aplicación tiene un único dueño; no hay registro, login ni permisos. Todo
  lo que se muestra pertenece a esa única persona.
- **Alcance del resumen**: el resumen mensual corresponde al mes calendario en curso. La
  navegación a meses anteriores no está especificada en el PRD y se considera fuera de esta
  entrega.
- **Fecha del movimiento**: se toma la fecha del mensaje que lo originó, en la zona horaria fija
  `America/Argentina/Buenos_Aires` (UTC-3, sin horario de verano); esa fecha determina a qué mes
  pertenece en el resumen. No es configurable en esta entrega: la app es mono-usuario y opera en
  ARS.
- **Categoría no determinable**: si la clasificación no puede asignar una categoría activa con
  confianza suficiente, el mensaje queda con error y se deriva al listado de errores, en lugar de
  asignar una categoría por defecto (Principio III de la constitución: no fabricar datos).
- **Reproceso**: solo se reprocesan mensajes en estado de error; un mensaje ya procesado no
  vuelve a generar movimientos.
- **Moneda base fija**: ARS es la moneda base y no es configurable ni eliminable en esta entrega.
- **Tipo de cambio manual**: no existe integración con ninguna fuente externa de cotización; el
  valor lo carga y actualiza el dueño.
- **Propagación de tipo de cambio**: la propagación de un tipo de cambio editado alcanza
  únicamente a los movimientos de la misma moneda y la misma fecha del movimiento editado.
- **Ingesta periódica**: el sistema consulta el canal de mensajería de forma recurrente; no se
  exige entrega en tiempo real, y un mensaje puede tardar un ciclo de ingesta en aparecer.
  Cada ciclo guarda y clasifica en la misma corrida (FR-005a): un mensaje que quedó no procesado
  porque el clasificador no respondió se reintenta en el ciclo siguiente, sin acción del dueño.
- **Sin límite superior de monto**: al ser una aplicación mono-usuario de finanzas personales, no
  se impone un tope al monto de un movimiento más allá de la precisión de almacenamiento definida
  en FR-038. No es un vacío: es una decisión explícita.
- **Notación en los escenarios**: los ejemplos de esta especificación usan "$" para montos en la
  moneda base ARS y "U$S" para montos en USD, con la convención de separadores de FR-041.
- **Superficie única**: el visor es la única interfaz de administración (FR-036). El resumen
  mensual es una vista de solo lectura; las pantallas de categorías, monedas, errores y edición de
  movimientos requieren interacción del dueño.
- **Fuera de alcance**: canal de WhatsApp, respuesta del bot al usuario, exportación de datos
  (CSV, Excel, PDF) e integración con APIs externas de cotización.

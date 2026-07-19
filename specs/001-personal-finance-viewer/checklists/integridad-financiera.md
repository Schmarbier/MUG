# Integridad Financiera y Cálculo — Checklist de Calidad de Requisitos

**Purpose**: Compuerta formal previa a `/speckit-plan`. Valida que los requisitos que gobiernan
montos, monedas, tipos de cambio, agregación y totales estén completos, cuantificados y sin
conflictos. No verifica implementación: audita **cómo están escritos los requisitos**.
**Created**: 2026-07-18
**Feature**: [spec.md](../spec.md)

**Depth**: Profundo (compuerta de release del plan) · **Audiencia**: autor, pre-plan
**Alcance**: FR-005a, FR-008, FR-012 a FR-015, FR-018 a FR-023, FR-032 a FR-035, SC-001, SC-003,
SC-007, SC-008 y las Assumptions de moneda/fecha.

---

## Representación y Precisión Monetaria

- [x] CHK001 - ¿Está especificada la cantidad de decimales admitida para el monto de un movimiento? [Gap]
  - **Resuelto 2026-07-18**: FR-038 — 2 decimales fijos para toda moneda.
- [x] CHK002 - ¿Está definida la precisión y la cantidad de decimales admitida para un tipo de cambio? Los ejemplos usan enteros (1450, 1500) sin declarar si se admiten fracciones. [Gap, Spec §FR-033]
  - **Resuelto 2026-07-18**: FR-039 — hasta 2 decimales, misma regla que el monto.
- [x] CHK003 - ¿Está especificada la regla de redondeo del equivalente en moneda base (a cuántos decimales y con qué criterio)? [Gap, Spec §FR-013]
  - **Resuelto 2026-07-18**: FR-040 — 2 decimales, al más cercano, empate hacia arriba.
- [x] CHK004 - ¿Define la spec si el redondeo se aplica por movimiento antes de sumar, o una sola vez sobre el total de la fila? Ambas producen resultados distintos. [Ambiguity, Spec §FR-013]
  - **Resuelto 2026-07-18**: FR-040 — precisión completa hasta la suma, redondeo único al mostrar; US2 AC-3.b lo verifica con un caso donde ambas reglas difieren en un centavo.
- [x] CHK005 - ¿Están definidos los límites del monto (mínimo, máximo, si se admite cero o negativo)? [Gap]
  - **Resuelto 2026-07-18**: FR-038 exige monto estrictamente mayor a cero (el sentido lo aporta el tipo); la ausencia de tope superior queda documentada como decisión explícita en Assumptions.
- [x] CHK006 - ¿Está definido el criterio de interpretación numérica de los montos escritos en lenguaje coloquial? La spec escribe "$2.000" como dos mil, pero nunca declara la convención de separadores. [Ambiguity, Spec §US1 AC-5]
  - **Resuelto 2026-07-18**: FR-041 — punto=miles, coma=decimales; monto ambiguo deriva a error "no contiene monto".
- [x] CHK007 - ¿Está especificado si un tipo de cambio puede ser cero o negativo, y qué ocurre si se intenta cargar uno? [Gap, Spec §FR-033]
  - **Resuelto 2026-07-18**: FR-039 — se rechaza con error todo TC menor o igual a cero, en alta, edición de cotización y edición del TC histórico.
- [x] CHK008 - ¿Es consistente la representación del monto entre el enunciado de los escenarios y los requisitos funcionales (símbolos, moneda implícita)? [Consistency, Spec §US2]
  - **Resuelto 2026-07-18**: convención de notación de los escenarios ("$" para ARS, "U$S" para USD) declarada explícitamente en Assumptions.

## Completitud del Cálculo de Agregación

- [x] CHK009 - ¿Está definido el criterio de ordenamiento de las filas dentro de cada bloque del resumen? Sin orden especificado, qué fila cae en qué página es indeterminado y el escenario de paginación no es verificable. [Gap, Conflict, Spec §FR-015 / §US2 AC-2.a]
  - **Resuelto 2026-07-18**: agregado FR-015a (monto descendente por equivalente en base, desempate alfabético categoría → código de moneda, orden determinístico); US2 AC-2.a reescrito con montos concretos y AC-2.b verifica estabilidad entre consultas.
- [x] CHK010 - ¿Existe un requisito que defina el total general de cada bloque (ingresos y egresos), o solo se especifican los totales por fila? SC-005 promete "cuánto gastó" pero ningún FR define ese número. [Gap, Spec §FR-012 / §SC-005]
  - **Resuelto 2026-07-18**: agregado FR-012a — total general por bloque, suma de equivalentes en moneda base de todas las filas del mes.
- [x] CHK011 - Si existe un total de bloque, ¿está definido si corresponde a la página visible o a todas las filas del mes? [Gap, Spec §FR-015]
  - **Resuelto 2026-07-18**: FR-012a — a todas las filas del mes, independiente de la paginación; US2 AC-5 lo verifica con un caso donde ambas cifras difieren.
- [x] CHK012 - Si existe un total de bloque que abarca varias monedas, ¿está definido cómo se consolida (suma de equivalentes en base, o totales separados por moneda)? [Gap, Spec §FR-013]
  - **Resuelto 2026-07-18**: FR-012a — mismo criterio que FR-013, suma de equivalentes en moneda base.
- [x] CHK013 - ¿Está especificado qué se muestra en la columna de tipo de cambio de una fila que agrupa movimientos con tipos de cambio históricos distintos? La regla de suma de equivalentes deja a la fila sin un TC único que exhibir. [Gap, Spec §FR-013]
  - **Resuelto 2026-07-18**: no aplica — el contrato de `contracts/visor.md` nunca definió una columna de TC por fila (solo total + equivalente); FR-013 ahora lo explicita (`/speckit-clarify`, segunda vuelta).
- [x] CHK014 - ¿Está definido qué se muestra en la posición del equivalente para las filas expresadas en la moneda base? [Gap, Spec §FR-013]
  - **Resuelto** (ya estaba en `contracts/visor.md`, sin marcar en este checklist hasta el análisis `/speckit-analyze`): "La fila en moneda base no exhibe equivalente."
- [x] CHK015 - ¿Está especificado si el resumen incluye movimientos cuya categoría fue desactivada, de forma consistente entre el requisito y el edge case? [Consistency, Spec §FR-031 / §Edge Cases]
  - **Resuelto** (ya estaba en spec.md §Edge Cases y `data-model.md`, sin marcar en este checklist hasta el análisis `/speckit-analyze`): el movimiento conserva su categoría y sigue apareciendo en el resumen; la exclusión solo aplica a clasificaciones nuevas.

## Tipo de Cambio Histórico — Claridad y Cobertura

- [x] CHK016 - ¿Está definido qué tipo de cambio histórico se registra cuando un movimiento se edita **desde** una moneda no base **hacia** la moneda base? ¿Se conserva, se anula, o queda sin valor? [Gap, Spec §FR-020 / §FR-021 / §FR-035]
  - **Resuelto 2026-07-18**: FR-021a — se anula; el movimiento en moneda base queda sin TC histórico (`/speckit-clarify`, sesión post-checklist).
- [x] CHK017 - ¿Está definido el tipo de cambio a registrar cuando un mensaje se reprocesa días después de haber sido ingerido: el vigente al momento del mensaje o el vigente al momento del reproceso? [Gap, Spec §FR-017 / §FR-035]
  - **Resuelto 2026-07-18**: FR-017a — el vigente al momento del reproceso, ya que el sistema no mantiene historial de cotizaciones por fecha; US4 AC-5 lo verifica (`/speckit-clarify`, segunda vuelta).
- [x] CHK018 - ¿Está definido con precisión el criterio de coincidencia para la propagación: alcanza a todos los movimientos de la misma moneda y fecha, o solo a los que compartían el tipo de cambio anterior al editado? [Ambiguity, Spec §FR-023 / §US6 AC-6]
  - **Resuelto 2026-07-18**: FR-023 aclarado — alcanza a todos los de esa moneda y fecha, sin importar el TC previo; US6 AC-7.a lo verifica (`/speckit-clarify`).
- [x] CHK019 - ¿Está definido si "misma fecha" en la propagación significa día calendario, y en qué zona horaria se evalúa? [Ambiguity, Spec §FR-023 / §Assumptions]
  - **Resuelto 2026-07-18**: consecuencia directa de CHK034 — FR-023 ahora referencia la zona horaria fija de Assumptions (`/speckit-clarify`, segunda vuelta).
- [x] CHK020 - ¿Está definido si la propagación alcanza movimientos de un mes distinto al que se está visualizando? [Coverage, Gap, Spec §FR-023]
  - **Resuelto 2026-07-18**: FR-023 aclarado — búsqueda global por moneda y fecha, sin acotar al mes visualizado (`/speckit-clarify`, segunda vuelta).
- [x] CHK021 - ¿Está especificado si la propagación del tipo de cambio se aplica también a movimientos ya editados manualmente por el dueño? [Edge Case, Gap, Spec §FR-022 / §FR-023]
  - **Resuelto 2026-07-18**: consecuencia directa de CHK018 — FR-023 explicita que alcanza a todos sin importar si el valor previo vino de una edición manual anterior (`/speckit-clarify`, segunda vuelta).
- [x] CHK022 - ¿Es consistente el requisito de inmutabilidad del tipo de cambio histórico (FR-035) con la edición manual que sí lo modifica (FR-022)? ¿Está explicitado que la excepción es deliberada y solo por acción del dueño? [Conflict, Spec §FR-022 / §FR-035]
  - **Resuelto 2026-07-18**: FR-035 ahora aclara que la inmutabilidad rige ante actualización automática de la cotización, y que FR-022/FR-023 son la única vía deliberada de corrección manual (análisis `/speckit-analyze`, hallazgo U1).
- [x] CHK023 - ¿Está definido qué ocurre con el tipo de cambio histórico de un movimiento cuando se edita únicamente su monto? [Coverage, Spec §FR-019]
  - **Resuelto 2026-07-18**: FR-019 aclarado — editar el monto no modifica el TC histórico (`/speckit-clarify`, segunda vuelta).

## Ciclo de Vida de Monedas — Conflictos y Vacíos

- [x] CHK024 - Los edge cases refieren a una "moneda desactivada", pero ningún requisito funcional define el estado activa/desactivada de una moneda ni la operación de desactivarla. ¿Es un requisito faltante o una referencia inválida? [Conflict, Spec §Edge Cases / §FR-032–FR-035]
  - **Resuelto 2026-07-18**: agregados FR-035a a FR-035f (listar con estado, eliminar sin uso, desactivar con historial, reactivar, excluir de clasificación, ARS exenta) + escenarios US6 AC-8 a AC-13.
- [x] CHK025 - ¿Incluye la entidad Moneda un atributo de estado, de forma consistente con lo que describen los edge cases? [Consistency, Spec §Key Entities]
  - **Resuelto 2026-07-18**: entidad Moneda ahora incluye el atributo estado (activa / desactivada).
- [x] CHK026 - ¿Está definido qué ocurre con los movimientos existentes de una moneda si esa moneda se desactiva o se elimina? [Gap, Coverage]
  - **Resuelto 2026-07-18**: FR-035c preserva el TC histórico al desactivar y FR-035b impide eliminar una moneda con movimientos; US6 AC-13 lo verifica en el resumen.
- [x] CHK027 - ¿Está especificado el formato y la validación del código de moneda que determina cuándo dos monedas son duplicadas? [Clarity, Spec §FR-033]
  - **Resuelto 2026-07-18**: FR-033 — ISO 4217, 3 letras, normalizado a mayúsculas, unicidad case-insensitive (`/speckit-clarify`, segunda vuelta).
- [x] CHK028 - ¿Está definido si la moneda base es identificable por un atributo propio o por su código, y si ese atributo es único en el sistema? [Clarity, Spec §FR-032 / §Key Entities]
  - **Resuelto** (ya estaba en Key Entities → Moneda: atributo "indicador de moneda base", sin marcar hasta esta pasada; `/speckit-clarify`, segunda vuelta).

## Corrección de Datos — Cobertura de Escenarios

- [x] CHK029 - ¿Existe un requisito que permita corregir el **tipo** (ingreso/egreso) de un movimiento mal clasificado? Se puede corregir categoría, monto y moneda, pero no el tipo, y una inversión de signo distorsiona ambos bloques del resumen. [Gap, Spec §FR-018–FR-020 / §FR-006]
  - **Resuelto 2026-07-18**: agregado FR-018a (edición del tipo, sin alterar monto/moneda/TC histórico) + escenarios US5 AC-4 y AC-5.
- [x] CHK030 - ¿Existe un requisito que permita corregir la **fecha** de un movimiento? Sin él, un movimiento asignado al mes equivocado no es corregible. [Gap, Spec §Assumptions]
  - **Resuelto 2026-07-18**: FR-020a — fecha editable, reasigna el movimiento al mes de la nueva fecha; US5 AC-7 (`/speckit-clarify`).
- [x] CHK031 - ¿Existe un requisito que permita eliminar un movimiento creado por error? Sin él, un movimiento espurio permanece en el resumen de forma permanente. [Gap, Coverage]
  - **Resuelto 2026-07-18**: FR-023a — eliminación definitiva; US5 AC-6 (`/speckit-clarify`).
- [x] CHK032 - ¿Están definidos los requisitos de validación al editar un movimiento (rechazo de monto inválido, de categoría desactivada, de moneda inexistente)? [Gap, Spec §FR-018–FR-020]
  - **Resuelto 2026-07-18**: FR-018 y FR-020 rechazan categoría/moneda inexistente; monto y TC ya cubiertos por FR-038/FR-039 (`/speckit-clarify`, tercera vuelta).
- [x] CHK033 - ¿Está definido si un movimiento puede reasignarse a una categoría desactivada durante una corrección manual, de forma consistente con la exclusión de categorías desactivadas en la clasificación automática? [Consistency, Spec §FR-018 / §FR-031]
  - **Resuelto 2026-07-18**: FR-018 rechaza categoría desactivada también en edición manual; US5 AC-9 lo verifica (`/speckit-clarify`, tercera vuelta).

## Alcance Temporal y Asignación al Mes

- [x] CHK034 - ¿Está identificada la zona horaria concreta que determina a qué mes pertenece un movimiento, o queda como referencia no resuelta ("zona horaria local del dueño")? [Ambiguity, Spec §Assumptions]
  - **Resuelto 2026-07-18**: Assumptions — fija en `America/Argentina/Buenos_Aires` (UTC-3), sin configuración (`/speckit-clarify`).
- [x] CHK035 - ¿Están definidos los requisitos para un mensaje recibido en el límite entre dos meses? [Edge Case, Gap]
  - **Resuelto 2026-07-18**: nuevo Edge Case — la fecha del mensaje en la TZ fija determina el mes sin ambigüedad, consecuencia directa de CHK034 (`/speckit-clarify`, tercera vuelta).
- [x] CHK036 - ¿Está definido a qué mes se asigna un movimiento creado por el reproceso de un mensaje ingerido en un mes anterior: al mes del mensaje o al del reproceso? [Ambiguity, Spec §FR-017 / §Assumptions]
  - **Resuelto 2026-07-18**: ya estaba en Assumptions ("Fecha del movimiento" = fecha del mensaje); nuevo Edge Case lo hace explícito y lo distingue del TC (FR-017a, que sí usa el momento del reproceso) (`/speckit-clarify`, tercera vuelta).

## Medibilidad de los Criterios de Éxito

- [x] CHK037 - ¿Es medible el criterio de rendimiento del resumen sin un volumen de datos declarado (cantidad de movimientos, categorías y monedas de referencia)? [Measurability, Spec §SC-003]
  - **Resuelto 2026-07-18**: SC-003 — hasta 1.000 movimientos/mes, 20 categorías, 5 monedas activas (`/speckit-clarify`, tercera vuelta).
- [x] CHK038 - ¿Está definido qué cuenta como "acierto" de la clasificación: solo la categoría, o también el monto, el tipo y la moneda? El umbral del 80% no es verificable sin esa definición. [Measurability, Ambiguity, Spec §SC-001]
  - **Resuelto 2026-07-18**: SC-001 — acierto = categoría + tipo coinciden; monto/moneda excluidos por ser parseo determinístico (`/speckit-clarify`, segunda vuelta).
- [x] CHK039 - ¿Es verificable el criterio de idempotencia sobre los montos, y no solo sobre la cantidad de registros? Repetir la ingesta podría preservar la cantidad y alterar valores. [Measurability, Spec §SC-007]
  - **Resuelto 2026-07-18**: SC-007 — extendido a "ni el valor de ninguno de sus campos existentes" (`/speckit-clarify`, tercera vuelta).
- [x] CHK040 - ¿Está definida la consistencia esperada entre el resumen y el detalle de movimientos tras una corrección manual, de forma objetivamente verificable? [Measurability, Spec §SC-008]
  - **Resuelto 2026-07-18**: SC-008 — se refleja en la siguiente carga de página, sin ventana de espera (coherente con Static SSR, vista derivada on-demand) (`/speckit-clarify`, tercera vuelta).

## Trazabilidad y Consistencia Documental

- [x] CHK041 - ¿Cuenta cada requisito de cálculo con al menos un escenario de aceptación que lo ejercite con valores concretos? [Traceability, Spec §FR-012–FR-015]
  - **Resuelto 2026-07-18** (auditoría, tercera vuelta): FR-012→US2 AC-1, FR-012a→AC-5, FR-013→AC-3/3.a/3.b, FR-014→AC-4, FR-015→AC-2, FR-015a→AC-2.a/2.b. Sin huecos.
- [x] CHK042 - ¿Es consistente el esquema de numeración de requisitos tras la incorporación de los sufijos FR-005a, FR-010a y FR-010b? [Traceability, Spec §Requirements]
  - **Resuelto 2026-07-18** (auditoría, tercera vuelta): patrón base+letra consistente en todos los sufijos agregados (FR-012a, FR-015a, FR-017a, FR-018a, FR-020a, FR-021a, FR-023a, FR-035a–f), insertados junto a su FR padre.
- [x] CHK043 - ¿Están las decisiones de la sesión de clarificación reflejadas sin contradicción en los requisitos, escenarios y edge cases que tocan? [Consistency, Spec §Clarifications]
  - **Resuelto 2026-07-18** (auditoría, tercera vuelta): las 12 respuestas de las tres vueltas de `/speckit-clarify` están propagadas a FR, escenarios y edge cases correspondientes, sin texto contradictorio remanente.

---

## Notes

- Marcá los ítems resueltos con `[x]` y anotá inline la decisión tomada.
- Un ítem sin resolver no bloquea por sí solo: bloquea si su ambigüedad puede producir un monto
  incorrecto en pantalla. Priorizar en ese orden.
- Ítems de mayor riesgo detectados en esta pasada: **CHK009** (orden de filas indeterminado hace
  no verificable la paginación), **CHK024** (los edge cases referencian un estado de moneda que
  ningún requisito define) y **CHK029** (no hay forma de corregir el tipo ingreso/egreso).
  **Los tres quedaron resueltos el 2026-07-18**, y con CHK024 cayeron también CHK025 y CHK026.
  **Tanda de precisión monetaria (CHK001–CHK008) cerrada el 2026-07-18** con FR-038 a FR-041.
  **CHK010–CHK012 (total de bloque) cerrados el 2026-07-18** con FR-012a (análisis
  `/speckit-analyze`, hallazgo G1). **CHK022 (conflicto FR-022/FR-035) cerrado el 2026-07-18**
  (hallazgo U1). **CHK014–CHK015 marcados el 2026-07-18**: ya estaban resueltos en
  `contracts/visor.md` y `data-model.md`/Edge Cases, solo faltaba marcarlos.
  **Sesión `/speckit-clarify` 2026-07-18 (primera vuelta)** cerró CHK016, CHK018, CHK030, CHK031
  y CHK034 (5 preguntas, tope de la sesión) — ver `## Clarifications` en spec.md.
  **Segunda vuelta 2026-07-18** cerró CHK013 (no aplica — el contrato no tiene columna de TC),
  CHK017, CHK019, CHK020, CHK021, CHK023, CHK027, CHK028 y CHK038 (5 preguntas + 4 resueltas por
  inferencia directa sin gastar cupo).
  **Tercera vuelta 2026-07-18** cerró CHK032, CHK033, CHK037, CHK039, CHK040 (5 preguntas) y
  CHK035, CHK036, CHK041, CHK042, CHK043 (5 resueltos por auditoría directa sin gastar cupo).
  **Estado final: 43 de 43 resueltos.** Checklist cerrado — sin bloqueos pendientes para
  `/speckit-plan`. Los artefactos de planning ya existentes (`plan.md`, `data-model.md`,
  `contracts/`, `tasks.md`) quedaron desactualizados frente a los FR nuevos de estas tres
  vueltas (FR-017a, FR-018 y FR-020 con validación referencial, FR-020a, FR-021a, FR-023a,
  FR-023 con criterio de propagación explícito, FR-033 con formato ISO 4217) — conviene
  regenerarlos antes de `/speckit-implement`.
- Este checklist audita la calidad de los requisitos, no la implementación. Un ítem se resuelve
  editando `spec.md`, no escribiendo código.

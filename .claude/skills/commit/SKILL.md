---
name: commit
description: Arma commits siguiendo Conventional Commits adaptados a PersonalFinance — mensaje en español, scopes del repo, commits atómicos con confirmación del usuario, detección de BREAKING CHANGE y referencias a RF/AC en el body. Se usa cuando el usuario pide commitear, armar un commit o preparar un mensaje de commit.
---

# Commit

Nunca commiteás en one-shot: primero inspeccionás el diff, decidís si es UN cambio
o VARIOS, proponés la separación, ESPERÁS que el usuario confirme, y recién ahí
armás el mensaje. El commit se hace solo con el visto bueno del usuario.

## Formato (Conventional Commits)

```
tipo(scope): subject en español, imperativo

body opcional en español, explicando el porqué

footer opcional (BREAKING CHANGE, refs)
```

## Tipos permitidos

`feat` · `fix` · `docs` · `refactor` · `test` · `chore` · `perf` · `build` · `ci` · `style`

## Scopes de ESTE repo (catálogo cerrado)

No inventes scopes libres. Usá uno de estos según qué tocó el cambio:

| Scope      | Cuándo |
|------------|--------|
| `bot`      | Proceso `PersonalFinance.Bot` (ingesta, ciclo de vida del bot) |
| `web`      | Proceso `PersonalFinance.Web` (API / resumen mensual) |
| `agent`    | El clasificador / prompts / cliente Ollama |
| `telegram` | Integración `Telegram.Bot`, lectura de mensajes, dedupe |
| `db`       | EF Core, migraciones, esquema, SQLite |
| `config`   | `appsettings`, secretos, env, `AGENTS.md`, tooling |

Si el cambio no encaja en ninguno, avisale al usuario y proponé cuál usar antes de commitear;
no fuerces uno.

## Paso 1 — Inspeccionar el diff

Antes de escribir nada, mirá qué hay realmente:

```bash
git status
git diff --staged   # si ya hay algo en stage
git diff            # cambios sin stage
```

## Paso 2 — Atomicidad (mostrar y confirmar)

Un commit = un cambio lógico. Si en el diff ves DOS o más cambios independientes
(ej: un fix del bot + un refactor de la web), NO los juntes:

1. Listá al usuario los grupos de cambios que detectaste, diciendo qué archivos
   entran en cada uno y qué tipo/scope le corresponde.
2. ESPERÁ que el usuario confirme la separación (o la corrija).
3. Recién con la confirmación, armás y hacés los commits uno por uno.

Si es un solo cambio lógico, seguí de largo pero igual mostrale el mensaje antes de commitear.

## Paso 3 — Armar el mensaje

**Subject:**
- En español, modo imperativo ("agregar", "corregir", "eliminar" — NO "agregado" ni "agrega").
- Minúscula inicial, sin punto final.
- Máximo ~50 caracteres (límite duro 72).

**Body (obligatorio si el diff es grande):**
- Si el cambio toca más de ~3 archivos o ~50 líneas, el body es OBLIGATORIO.
- Explica el PORQUÉ del cambio, no el qué (el qué ya está en el diff).
- En español, líneas de ~72 caracteres.

**Referencias en el body (RF / AC):**
- Si el cambio implementa o afecta un requerimiento o criterio del PRD / `AGENTS.md`,
  referencialo en el body: `Implementa RF-03` / `Cubre AC-11`.
- Van en el BODY, nunca en el subject.

## Paso 4 — BREAKING CHANGE

Detectá y marcá ruptura cuando el cambio toca:
- Migraciones EF o el esquema de SQLite de forma incompatible.
- Contratos de la API de `PersonalFinance.Web`.
- Claves de configuración / secretos que rompen el arranque.

Marcalo con `!` después del scope y un footer explicando qué se rompe y cómo migrar:

```
feat(db)!: renombrar tabla Movimientos a Transacciones

BREAKING CHANGE: la tabla Movimientos pasa a Transacciones.
Correr la migración AddTransaccionesTable antes de levantar.
```

## Paso 5 — Confirmar y commitear

Mostrale el mensaje final completo y esperá el OK. Recién ahí:

```bash
git commit -m "..." -m "..."
```

## Reglas duras (siempre)

- PROHIBIDO `Co-Authored-By` o cualquier atribución a IA en el commit.
- Mensaje SIEMPRE en español.
- Solo Conventional Commits: sin este formato, no hay commit.
- Nunca commitees sin mostrar el mensaje y tener el OK del usuario.
- No agregues a stage archivos que el usuario no pidió; ante la duda, preguntá.

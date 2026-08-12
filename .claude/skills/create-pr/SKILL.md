---
name: create-pr
description: Crea o actualiza el Pull Request de la rama actual hacia main (u otra rama base), generando un título descriptivo y una descripción basada en los commits reales de la rama. Úsalo después de hacer commit cuando el usuario quiera abrir o refrescar un PR sin escribir el título/descripción a mano. Dispara con frases como "crea el PR", "sube el PR", "abre el pull request", "actualiza el PR".
user-invocable: true
allowed-tools:
  - Bash(git *)
  - Bash(gh *)
---

# /create-pr — Crear o actualizar el Pull Request de la rama actual

Argumentos opcionales: `$ARGUMENTS` — rama base del PR. Si no se indica, usa `main`.

## Qué hace

1. Detecta la rama actual y la rama base.
2. Sube a `origin` los commits locales que falten (nunca con `--force`).
3. Reúne los commits de la rama que no están en la base.
4. Redacta un título descriptivo y una descripción basados en esos commits (no en una plantilla genérica).
5. Si ya existe un PR abierto de esta rama hacia la base, actualiza título y descripción. Si no existe, lo crea.
6. Entrega al usuario la URL final del PR.

## Pasos

1. **Rama actual y base**: `git rev-parse --abbrev-ref HEAD`. La base es el primer valor de `$ARGUMENTS`, o `main` si no viene nada. Si la rama actual es igual a la base, detente y avísale al usuario — no tiene sentido un PR de una rama contra sí misma.

2. **Cambios sin commitear**: corre `git status --short`. Si hay cambios sin commitear, avisa al usuario que quedarán fuera del PR (no los commitees automáticamente salvo que el usuario lo pida explícitamente en este mismo turno).

3. **Sincroniza con el remoto**:
   - `git fetch origin` para tener referencias actualizadas de la rama y de la base.
   - Si la rama actual no tiene upstream (`git rev-parse --abbrev-ref --symbolic-full-name @{u}` falla), haz `git push -u origin <rama>`.
   - Si tiene upstream pero hay commits locales no subidos (`git log origin/<rama>..<rama>` no vacío), haz `git push`.
   - Nunca uses `--force` ni reescribas historia. Si el push normal es rechazado, detente y pregúntale al usuario cómo proceder — no asumas que hay que forzar.

4. **Commits exclusivos de la rama**: `git log origin/<base>..HEAD --pretty=format:"%h %s"`. Si viene vacío, dile al usuario que no hay commits nuevos para proponer y detente ahí (no crees un PR vacío).

5. **Redacta título y descripción** a partir de esos commits (y, si el alcance no queda claro, revisa también `git diff origin/<base>...HEAD --stat`):
   - **Título**: corto, imperativo, sin punto final, máx. ~70 caracteres. Resume el cambio principal de la rama — si hay varios commits con propósitos distintos, sintetiza, no copies literal el asunto de un solo commit.
   - **Descripción**, en el idioma en que el usuario te habla, con esta forma:
     ```markdown
     ## Resumen
     - punto 1 (un punto por cambio significativo, no necesariamente uno por commit)
     - punto 2

     ## Commits incluidos
     - <hash corto> <asunto del commit>
     - ...

     ## Plan de pruebas
     - [ ] ítems relevantes según lo que cambió (build, tests, verificación manual)
     ```

6. **Busca un PR existente** para esta rama: `gh pr list --head <rama> --state open --json number,url,title`.
   - Si existe: `gh pr edit <numero> --title "<título>" --body "<descripción>"` (pasa el body con heredoc para preservar formato).
   - Si no existe: `gh pr create --base <base> --head <rama> --title "<título>" --body "<descripción>"`.

7. **Si `gh` falla por autenticación** (401, "Bad credentials", "not logged in"): no reintentes en bucle. Explícale al usuario que necesita correr `gh auth login` (puede usar `! gh auth login` en la sesión) y detente ahí — no hay forma de crear el PR sin sesión válida.

8. Al terminar, muéstrale al usuario la URL del PR (creado o actualizado) y un resumen breve de qué cambió respecto a la versión anterior del PR si era una actualización.

## Reglas

- Nunca `git push --force` ni reescribas historia de la rama.
- Nunca cambies la rama base a algo distinto de `main` salvo que el usuario lo pida explícitamente vía `$ARGUMENTS`.
- Si no hay commits nuevos desde el último PR/actualización, dilo claramente en vez de crear un PR vacío o duplicado.
- Título y descripción deben reflejar el contenido real de los commits de la rama, no texto genérico de relleno.

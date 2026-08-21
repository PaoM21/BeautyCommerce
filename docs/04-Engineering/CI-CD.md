# CI/CD — BeautyCommerce (Haldy&Co)

Fotografía técnica completa de qué existe hoy en integración y despliegue
continuo — no cómo debería ser, sino qué es verificable ahora mismo. Todo
lo marcado **HECHO** se verificó leyendo `.github/workflows/backend-ci.yml`,
consultando la API de GitHub (`gh api`, `gh run list`, `gh secret list`)
y `docs/DEPLOYMENT.md`. No se modificó ningún workflow, configuración de
GitHub, ni se ejecutó ningún despliegue para escribir este documento.

## Resumen ejecutivo

**HECHO.** Existe **un solo workflow de GitHub Actions** en todo el
repositorio (`backend-ci.yml`) — no hay CI para el frontend, no hay
workflow de despliegue, no hay protección de rama, no hay entornos de
GitHub configurados, no hay secretos de GitHub Actions definidos. El
despliegue real a producción (Render + Cloudflare Pages) ocurre **por
completo fuera de GitHub Actions**, disparado directamente por los propios
proveedores al detectar un push — sin que ningún check de CI lo condicione.

---

## Workflows existentes

### `backend-ci.yml` — el único workflow

**HECHO**, contenido completo verificado línea por línea:

| Aspecto | Valor real |
|---|---|
| Triggers | `push` a `main` y `pull_request` hacia `main`, ambos **filtrados por path** (`backend/**`, `.github/workflows/backend-ci.yml`) — un cambio que solo toca `frontend/` o `docs/` no dispara este workflow |
| Runner | `ubuntu-latest`, para ambos jobs |
| Jobs | Dos: `build-and-test` y `docker` (este último depende de que el primero termine, `needs: build-and-test`) |
| Servicios/dependencias declaradas | **Ninguna** — no hay bloque `services:` para PostgreSQL, Redis, ni nada — ver "Defecto confirmado" abajo |
| Pasos de `build-and-test` | Checkout → instalar .NET 9.0.x → `dotnet restore BeautyCommerce.sln` → `dotnet build --configuration Release --no-restore` → `dotnet test --configuration Release --no-build --collect:"XPlat Code Coverage"` → subir artifact `test-results` |
| Filtro de tests | **Ninguno.** El comando es `dotnet test BeautyCommerce.sln --configuration Release --no-build --collect:"XPlat Code Coverage"` — sin `--filter`, así que intenta correr **los 61 archivos de test**, incluidos los `[Trait("Category","Integration")]` que requieren PostgreSQL real |
| Artifacts | `test-results` (carpeta `backend/**/TestResults`, cobertura de código en formato "XPlat Code Coverage") — se sube siempre, incluso si el job falla (comportamiento por defecto de `actions/upload-artifact` cuando no se especifica `if:`) |
| Build de frontend | **No existe en absoluto** — ningún job compila ni tipa el frontend |
| Job `docker` | Construye la imagen (`docker build -f backend/Dockerfile -t beautycommerce-api:latest backend`) **y nunca la publica a ningún registro** — es un smoke test de que el `Dockerfile` compila, no un paso de despliegue |
| Deployment | **No hay ningún paso de despliegue en este workflow** — ver la sección de Deployment más abajo, que ocurre por un camino completamente distinto |

### Frontend — sin workflow

**HECHO, por ausencia confirmada.** `find .github/workflows` devuelve
únicamente `backend-ci.yml`. Ningún cambio de `frontend/web/` dispara
verificación automática de ningún tipo — ni `tsc -b`, ni `vite build`, ni
lint.

---

## PostgreSQL en CI

**HECHO — el hallazgo ya elevado a `docs/README.md` y
`04-Engineering/TESTING-STRATEGY.md`, con el detalle técnico completo aquí:**

- El runner `ubuntu-latest` no tiene PostgreSQL preinstalado, y el
  workflow no declara ningún `services: postgres: ...` para levantarlo
  como contenedor efímero (el mecanismo estándar de GitHub Actions para
  esto).
- Los tests con `[Trait("Category","Integration")]` usan una cadena de
  conexión hardcodeada (`Host=localhost;Port=5432;Database=BeautyCommerceDb;
  Username=postgres;`) que asume una instancia local ya poblada con datos
  base (marca/categoría semilla) — no solo un servidor Postgres vacío
  arrancado por el CI.
- El propio código de test ya documentaba la intención de excluirlos de
  este entorno (`Concurrency/InventoryConcurrencyTests.cs:16-19`, citado en
  `TESTING-STRATEGY.md`), pero el workflow nunca aplicó
  `--filter Category!=Integration`.

**Tests ejecutados vs. excluidos, dicho con precisión:** el workflow
**intenta ejecutar los 61**, no excluye ninguno deliberadamente — la
"exclusión" que ocurre hoy es un efecto secundario de que esos tests
fallan por no poder conectar, no una decisión de configuración.

---

## Historial real de ejecuciones

**HECHO, verificado empíricamente vía `gh run list`, no inferido del YAML
solamente:**

| Métrica | Valor |
|---|---|
| Últimas 26 ejecuciones | 22 fallidas, 4 exitosas |
| Últimas 5 ejecuciones | 5/5 fallidas |
| Causa confirmada en la ejecución más reciente | `Npgsql.NpgsqlException: Failed to connect to 127.0.0.1:5432` / `SocketException: Connection refused`, en múltiples tests de `Concurrency/` |

**Sobre las 4 ejecuciones exitosas dentro de las últimas 26: no se
investigó a fondo en esta pasada** por qué pasaron — es plausible que
correspondan a un estado anterior del repositorio con menos tests
`Integration` o con cambios que no tocaban `backend/**` (y por tanto no
dispararon el job de tests en absoluto, contando como "exitoso" por
ausencia de ejecución real) — **marcado explícitamente como NO VERIFICADO**,
no como una explicación confirmada.

---

## Branch protection y merge policy

**HECHO**, verificado vía `gh api repos/PaoM21/BeautyCommerce/branches/main/protection`
→ `404 Branch not protected`:

- **No hay ninguna regla de protección sobre `main`.**
- Consecuencia directa: no hay revisión de PR obligatoria, no hay ningún
  check de CI requerido antes de mergear, no hay restricción de quién
  puede hacer push directo a `main`, no hay protección contra force-push.
- No existe archivo `CODEOWNERS`, ni plantilla de Pull Request
  (`.github/PULL_REQUEST_TEMPLATE.md`) — confirmado por ausencia junto al
  único archivo real bajo `.github/`.

**Consecuencia práctica, ya que `backend-ci.yml` falla la mayoría de las
veces:** si existiera una regla que exigiera "CI debe pasar para mergear",
**bloquearía casi todos los merges hoy** — la ausencia de esa regla no es
solo un hueco de gobernanza, es lo que permite que el equipo siga
trabajando pese a que CI esté roto. Este es un matiz importante para la
recomendación al final del documento: no se puede simplemente "activar"
branch protection sin antes reparar CI.

---

## Secrets y environments de GitHub

**HECHO**, verificado vía `gh secret list` y `gh api .../environments`:

- **Cero secretos de GitHub Actions configurados.** `backend-ci.yml` no
  necesita ninguno (solo construye y prueba, no despliega ni publica una
  imagen), consistente con esa ausencia.
- **Cero entornos de GitHub configurados** (`environments: []`) — no hay
  `production`/`staging` con reglas de protección ni secretos propios a
  nivel de GitHub.
- Esto es coherente con que **todos los secretos reales del proyecto viven
  en otro lugar**: `dotnet user-secrets` en desarrollo local, y variables
  de entorno marcadas `sync: false` directamente en el dashboard de Render
  (ver `01-Architecture/SECURITY-ARCHITECTURE.md` → Secrets & configuration)
  — GitHub Actions nunca maneja un secreto de producción en este proyecto.

---

## Deployment — ocurre completamente fuera de GitHub Actions

**HECHO**, verificado contra `docs/DEPLOYMENT.md`:

```
Push a main / a la rama que Render y Cloudflare Pages observan
        │
        ├──────────────────────────────┐
        ▼                               ▼
  Render (Blueprint,               Cloudflare Pages
  detecta render.yaml)              (build: npm run build,
  build: Docker (backend/Dockerfile) output: dist/)
        │                               │
        ▼                               ▼
  beautycommerce-api.onrender.com   beautycommerce-web.pages.dev

  Ninguno de los dos pasos anteriores depende de que
  .github/workflows/backend-ci.yml haya corrido, y mucho
  menos de que haya pasado.
```

- **Backend**: Render detecta `render.yaml` como "Blueprint" y construye
  la imagen **usando su propio pipeline de build**, no la imagen que
  `backend-ci.yml` construye y descarta en el job `docker`. Son dos builds
  Docker completamente independientes del mismo `Dockerfile`.
- **Frontend**: Cloudflare Pages tiene su propia integración Git directa
  (`Root directory: frontend/web`, `Build command: npm run build`),
  totalmente al margen de GitHub Actions.
- **Migraciones de base de datos**: se aplican automáticamente al arrancar
  el contenedor en Render (`dbContext.Database.Migrate()` fuera de
  `Development`, ya citado en `01-Architecture/SYSTEM-ARCHITECTURE.md`) —
  no hay un paso de migración explícito en ningún pipeline, es parte del
  arranque de la aplicación misma.

**Consecuencia arquitectónica central de este hallazgo:** el pipeline de
CI (`backend-ci.yml`) y el pipeline de despliegue (Render/Cloudflare) **no
están conectados entre sí en absoluto**. Un push a `main` con tests
rotos (algo que, dado el estado actual, es casi garantizado) **se despliega
igual** a producción si el push llega a la rama que Render/Cloudflare
observan. CI hoy es, en la práctica, un reporte informativo que nadie
puede usar como gate — no porque alguien lo haya decidido así, sino porque
nunca se conectó.

---

## Clasificación, con la disciplina de siempre

### HECHO — lo que existe hoy
- Un workflow de backend (`build-and-test` + `docker` smoke build).
- Cobertura de código recolectada y subida como artifact (no publicada a
  ningún dashboard, solo descargable manualmente desde la ejecución).
- Despliegue automático a Render y Cloudflare Pages, disparado
  directamente por los proveedores al detectar push — independiente de
  GitHub Actions.
- Gestión de secretos de producción correcta y fuera de GitHub (Render
  `sync: false`, `dotnet user-secrets` en desarrollo).

### 🔴 DEFECTO confirmado
- **CI de backend roto**: 22/26 ejecuciones recientes fallidas por falta
  de servicio de PostgreSQL — ya elevado a `docs/README.md`.

### 🟠 RIESGO
- **CI y Deployment desconectados**: nada impide que código con tests
  rotos (o que ni siquiera compila, en el caso extremo) se despliegue a
  producción, porque el despliegue no depende de que CI pase.
- **Sin protección de rama en `main`**: sin revisión obligatoria, sin
  check requerido, sin protección contra force-push.
- **Sin CI de frontend**: ningún cambio de frontend se verifica
  automáticamente antes de desplegarse a Cloudflare Pages.
- **Cobertura de código recolectada pero no consumida**: se sube como
  artifact pero no hay ningún umbral ni gate que la use — es evidencia
  archivada, no una señal accionable.
- **Job `docker` es un smoke test aislado**, no representativo del build
  real que Render ejecuta — dos pipelines de build Docker independientes
  del mismo `Dockerfile` podrían divergir sin que nadie lo note.

### 🟡 RECOMENDACIÓN — cómo debería evolucionar (no implementado aquí)
1. Agregar un servicio `postgres` al job `build-and-test` (mecanismo
   estándar de GitHub Actions, `services:` con la imagen oficial de
   Postgres), o separar los tests `Integration` a un job propio con ese
   servicio, dejando el resto en un job rápido sin esa dependencia.
2. Solo **después** de que CI pase de forma confiable, evaluar activar
   protección de rama en `main` exigiendo ese check — activarla antes
   bloquearía prácticamente todo merge, dado el estado actual.
3. Agregar un workflow mínimo de frontend (`tsc -b && vite build`) con el
   mismo filtro de `paths: frontend/**`.
4. Decidir conscientemente si se quiere conectar CI y Deployment (por
   ejemplo, que Render/Cloudflare solo desplieguen ramas donde
   `backend-ci.yml`/el futuro workflow de frontend hayan pasado) — esto es
   una decisión de arquitectura de release, no un detalle de
   configuración, y merece su propio ADR cuando se decida abordar.
5. Si se decide usar la cobertura de código como gate, definir un umbral
   explícito y hacer que el workflow falle por debajo de él — hoy se
   recolecta pero no se usa para nada.

Ninguna de estas cinco recomendaciones se implementa en este documento —
quedan como INFERENCIA/RECOMENDACIÓN para una decisión futura explícita,
consistente con la regla de no convertir automáticamente un hallazgo en
implementación.

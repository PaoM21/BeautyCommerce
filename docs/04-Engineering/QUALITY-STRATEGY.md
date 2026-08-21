# Quality Strategy — BeautyCommerce (Haldy&Co)

`04-Engineering/TESTING-STRATEGY.md` responde **cómo verificamos** que el
sistema funciona. Este documento responde una pregunta un nivel más
arriba: **qué significa "calidad" para este producto específico, y cómo
demostramos que la tenemos** — no solo a nivel de código, sino conectando
negocio, arquitectura, seguridad, experiencia y operación en una sola
cadena de evidencia. Todo lo marcado **HECHO** se verificó contra los
documentos y el código ya citados a lo largo de esta documentación; nada
se cambió de código para escribir este documento.

## Qué significa "calidad" aquí — no solo "los tests pasan"

**DECISIÓN**, derivada directamente de `00-Governance/PROJECT-CHARTER.md`:
la propuesta de valor de Haldy&Co es una experiencia boutique/premium, no
solo una transacción correcta. Eso significa que "calidad" en este
producto tiene que cubrir **cuatro dimensiones**, no una:

1. **Correctitud** — el sistema hace lo que dice que hace (dominio de
   `04-Engineering/TESTING-STRATEGY.md`).
2. **Confiabilidad operativa** — el sistema sigue haciéndolo bien bajo
   carga real, fallos parciales, y a lo largo del tiempo sin que nadie lo
   vigile manualmente (CI, observabilidad).
3. **Seguridad** — el sistema protege los datos y las operaciones de
   quienes no deberían tener acceso (`01-Architecture/SECURITY-ARCHITECTURE.md`).
4. **Experiencia** — el sistema se siente boutique, no genérico; esto
   todavía **no tiene documento propio** (`03-UX-UI/` está pendiente) —
   se señala aquí como una dimensión de calidad real del producto, no
   solo "algo que UX decide aparte".

Un release que pasa todos los tests pero tiene una UX rota, o que es
rápido pero inseguro, **no cumple el estándar de calidad de este
producto** — aunque "los tests estén verdes".

---

## La cadena de evidencia: Business → Architecture → Code → Tests → CI → Security → UX → Observability → Release Readiness

**HECHO**, cada eslabón mapeado al documento real que lo sostiene hoy, con
su estado de madurez honesto — no aspiracional:

```
Business                00-Governance/PROJECT-CHARTER.md, SCOPE.md
   │                    🟢 Escrito y verificado contra el código real
   ▼
Architecture             01-Architecture/SYSTEM-ARCHITECTURE.md,
   │                     DOMAIN-MODEL.md, DATA-ARCHITECTURE.md
   │                     🟢 Escrito, con RIESGOs explícitos documentados
   ▼
Code                     El código fuente en sí — backend + frontend
   │                     🟢 Compila limpio (0 warnings/0 errores,
   │                     verificado en esta sesión de documentación)
   ▼
Tests                    04-Engineering/TESTING-STRATEGY.md
   │                     🟢 Suite rica (61 archivos), pero ver el
   │                     salto al siguiente eslabón
   ▼
CI                       .github/workflows/backend-ci.yml
   │                     🔴 ROTO — 22/26 corridas recientes fallidas
   │                     (ver alerta en docs/README.md). Este es el
   │                     eslabón que rompe la cadena: toda la evidencia
   │                     de los eslabones anteriores deja de propagarse
   │                     automáticamente a partir de aquí.
   ▼
Security                 01-Architecture/SECURITY-ARCHITECTURE.md
   │                     🟡 Modelado, con preguntas abiertas sin
   │                     verificar (ownership de GET /orders/{id}) y
   │                     RIESGOs documentados (sin rate limiting, JWT
   │                     sin lifecycle completo)
   ▼
UX                       03-UX-UI/ — 🔴 no existe todavía como
   │                     documento; la "experiencia boutique" de la
   │                     propuesta de valor no tiene forma de
   │                     verificarse sistemáticamente hoy
   ▼
Observability             01-Architecture/SYSTEM-ARCHITECTURE.md /
   │                      API-ARCHITECTURE.md → sección Observabilidad
   │                      🟡 Logging estructurado existe (Serilog);
   │                      sin correlation ID, sin métricas, sin tracing
   ▼
Release Readiness         00-Governance/PROJECT-CHARTER.md → tabla
                           🟡/🔴 mixta — ver Quality Gate abajo
```

**La conclusión que esta cadena obliga a decir sin rodeos:** los primeros
cuatro eslabones (Business, Architecture, Code, Tests) están en un estado
sólido, con evidencia real y documentada. **El eslabón de CI es donde la
cadena se rompe** — no porque el trabajo de los eslabones anteriores esté
mal hecho, sino porque nada garantiza hoy, de forma automática y continua,
que ese trabajo siga siendo cierto en cada cambio nuevo. Todo lo que viene
después en la cadena (Security, UX, Observability, Release Readiness)
hereda esa misma incertidumbre.

---

## Quality Gate formal

**DECISIÓN — este es el primer Quality Gate formal del proyecto.** No
existía antes de este documento. Se define ahora porque, después de cerrar
Governance, Architecture y Testing Strategy, hay evidencia real suficiente
para que cada criterio signifique algo concreto y verificable, no una
aspiración genérica.

**Cómo se lee esta tabla:** cada fila es un criterio independiente. La
columna "Enforcement" distingue lo que un mecanismo automatizado verifica
hoy de lo que depende de que una persona lo haga a mano — esa distinción
es, en sí misma, el hallazgo más importante de este Gate.

| # | Criterio | Estado actual | Enforcement | Evidencia |
|---|---|---|---|---|
| 1 | **Build** | 🟢 | Automatizado (CI, para el backend) + manual verificado (frontend) | `dotnet build` 0 warnings/0 errores; `tsc -b && vite build` limpio (verificado en esta sesión) |
| 2 | **Unit/integration tests** | 🟡 | **Parcial** — corre en CI, pero ver #6 | 61 archivos de test, ver `TESTING-STRATEGY.md` |
| 3 | **PostgreSQL integration** | 🔴 | **No automatizado** — solo corre si alguien lo ejecuta localmente contra su propio Postgres | Los tests `[Trait("Category","Integration")]` fallan en CI por falta de servicio Postgres |
| 4 | **E2E crítico** | 🟡 | Manual/local — existen tests tipo E2E (`CheckoutTransactionIntegrityTests`) pero corren contra el handler en memoria, no contra el servidor HTTP real, y comparten el mismo problema de CI que #3 | `TESTING-STRATEGY.md` → Test levels |
| 5 | **Security** | 🟡 | Manual — sin scanner automatizado, sin gate de CI que bloquee por hallazgo de seguridad | `SECURITY-ARCHITECTURE.md`, con preguntas abiertas sin cerrar (ownership de `GET /orders/{id}`) |
| 6 | **API contract** | 🟡 | **No automatizado** — no hay test de contrato (ej. Pact, snapshot de OpenAPI) que falle si un endpoint cambia de forma incompatible | `API-ARCHITECTURE.md`, con inconsistencias ya documentadas (envelope de respuesta, ausencia de versionado) |
| 7 | **Frontend build** | 🟢 (local) / 🔴 (CI) | **No hay ningún workflow de CI para el frontend** — confirmado por ausencia: `.github/workflows/` solo tiene `backend-ci.yml` | Verificado manualmente en esta sesión (`npm run build`), nunca en CI |
| 8 | **UX critical flows** | 🔴 | No automatizado, sin checklist formal — la única verificación existente es ad hoc (ej. la sesión de Playwright de esta misma documentación, no repetible) | No existe `03-UX-UI/USER-FLOWS.md` todavía |
| 9 | **No residuos QA** | 🟡 | **Disciplina manual**, no un script — cada test de integración limpia lo que siembra (`IAsyncLifetime`), pero no hay un chequeo automatizado post-suite que confirme que la base quedó limpia | `TESTING-STRATEGY.md` → Test data/fixtures |
| 10 | **Git/diff control** | 🔴 | **No hay protección de rama en `main`** — confirmado vía API de GitHub (`Branch not protected`, 404). No hay revisión obligatoria de PR ni check de CI requerido antes de mergear | Verificado en esta sesión: `gh api repos/.../branches/main/protection` → 404 |
| 11 | **CI health** | 🔴 | — | 22/26 corridas recientes de `backend-ci.yml` fallidas (ver alerta en `docs/README.md`) |
| 12 | **Known risks** | 🟢 | Documentado — cada documento de esta serie mantiene su propia sección de RIESGO/BACKLOG | Ver `00-Governance/SCOPE.md`, `SECURITY-ARCHITECTURE.md`, `TESTING-STRATEGY.md` |
| 13 | **Release blockers** | 🔴 | Documentado, no bloqueado por tooling — nada impide técnicamente un deploy con el pago simulado activo | `PROJECT-CHARTER.md` → Release Readiness: pago real es P0 |

**El patrón que esta tabla revela, dicho una sola vez para no repetirlo en
cada fila:** de los 13 criterios, **solo 2 tienen enforcement realmente
automatizado hoy** (Build parcialmente, y de forma indirecta la porción de
tests que sí corre en CI). El resto vive como **evidencia documentada +
disciplina manual**, no como un gate que técnicamente impida mergear o
desplegar código que no la cumple. Eso no significa que la calidad no
exista — significa que hoy depende de que alguien la revise a mano, no de
que el sistema la exija.

---

## Qué significa "apto para avanzar al siguiente nivel de release"

**DECISIÓN.** Con este Gate, una versión no se declara "apta" con una
frase genérica ("los tests están verdes") — se declara apta citando
**cuáles** de los 13 criterios están en verde, cuáles en amarillo con
riesgo aceptado conscientemente, y cuáles en rojo bloquean explícitamente
ese release específico. Ejemplo aplicado al estado real de hoy:

- **¿Apto para un pilot interno con datos de prueba?** Razonable — los
  criterios en rojo (CI health, PostgreSQL integration en CI, UX flows,
  git/diff control) son aceptables para un entorno controlado donde el
  equipo mismo verifica manualmente antes de cada cambio importante.
- **¿Apto para procesar ventas reales?** No — el criterio #13 (Release
  blockers) lo bloquea explícitamente por sí solo: `PaymentService` es un
  simulador (ADR-007). Ningún otro criterio en verde compensa ese
  bloqueador.

Esta forma de decidir — criterio por criterio, no un veredicto binario — es
la que convierte "creemos que está listo" en "podemos citar exactamente
por qué".

---

## Relación con `TESTING-STRATEGY.md`

**DECISIÓN**, para que quede explícita la frontera entre ambos documentos
y no se dupliquen: `TESTING-STRATEGY.md` es la fuente de verdad de **cómo**
se genera evidencia técnica (qué tipo de test, contra qué motor de datos,
con qué patrón de limpieza). Este documento consume esa evidencia y la
**agrega** con la de otras disciplinas (seguridad, UX, CI, control de
cambios) en una sola vista de decisión de release. Ningún hallazgo técnico
nuevo de testing debería agregarse aquí sin también agregarse allá — este
documento no reemplaza esa fuente, la usa.

---

## Quality risks / backlog

Misma disciplina que el resto de la documentación — sin ascender
automáticamente una ausencia a defecto:

### 🔴 DEFECTO demostrado
- CI de backend roto (ya elevado a `docs/README.md`, criterio #11 del Gate).

### 🟠 RIESGO arquitectónico
- **Ausencia total de protección de rama en `main`** — cualquier persona
  con acceso de escritura puede mergear sin revisión ni check de CI
  aprobado. Confirmado por API de GitHub, no inferido.
- **Ausencia total de CI para el frontend** — ningún cambio de frontend se
  verifica automáticamente, ni siquiera a nivel de compilación.
- **Ningún criterio de seguridad ni de contrato de API está automatizado**
  — dependen de que alguien relea `SECURITY-ARCHITECTURE.md`/
  `API-ARCHITECTURE.md` a mano antes de cada release.

### 🟡 MEJORA recomendada
- Agregar un servicio de PostgreSQL al workflow de CI (o separar un job de
  Integration tests que lo tenga) — **no implementado en este documento**,
  es la corrección que el criterio #3/#11 del Gate necesita.
- Agregar un workflow de CI para el frontend (`tsc -b && vite build` como
  mínimo).
- Configurar protección básica de rama en `main` (requerir que el check de
  CI pase antes de mergear, una vez que CI esté reparado — hacerlo antes
  bloquearía todo merge, dado que CI hoy falla la mayoría de las veces).

### 🔵 REQUIREMENT DE PRODUCCIÓN
- Ninguna versión debería declararse apta para procesar pagos reales
  mientras el criterio #13 (Release blockers → ADR-007) siga en rojo.
- Antes de un lanzamiento público, los criterios #3, #10 y #11 (integración
  con Postgres en CI, control de Git, salud de CI) deberían pasar de
  "disciplina manual" a "enforcement automatizado" — un producto que
  cobra dinero real no debería depender de que alguien recuerde revisar
  esto a mano.

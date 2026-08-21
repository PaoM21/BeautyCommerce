# Testing Strategy — BeautyCommerce (Haldy&Co)

## Propósito y alcance

Este documento no responde "¿qué frameworks usamos?" — responde **cómo
sabemos que el sistema funciona**, y qué tipo de evidencia exige cada clase
de comportamiento. La lección central que atraviesa todo el documento, ya
demostrada varias veces en este proyecto:

> **Un test en verde no demuestra automáticamente que el comportamiento
> esté funcionando.** El Escenario J tenía tests de concurrencia y aun así
> el contrato HTTP del perdedor de la carrera estaba roto. El Escenario I
> tenía tests de lockout y aun así el pipeline real de producción hacía
> rollback del efecto. El Escenario E tenía cobertura indirecta de
> inventario y aun así cancelar una orden destruía stock silenciosamente.

Y una lección nueva, encontrada al escribir este mismo documento, que
extiende la anterior: **un test en verde localmente tampoco demuestra que
el pipeline de integración continua lo esté verificando** — ver
"Regression testing policy" más abajo. Todo lo marcado **HECHO** en este
documento se verificó leyendo los 61 archivos de test reales, el workflow
de CI, y el historial real de ejecuciones de CI en GitHub — no se describe
ningún test que no exista. No se cambió ni se ejecutó ningún test para
escribir este documento (más allá de leerlos), y no se modificó el
workflow de CI.

---

## Testing pyramid actual

**HECHO**, no aspiracional — así es como se distribuye la evidencia real
hoy, no cómo "debería" verse una pirámide de testing de manual:

```
                    ▲
                   ╱ ╲     Manual / navegador real
                  ╱   ╲    Smoke tests puntuales con Playwright,
                 ╱     ╲   ejecutados ad hoc por un agente o el
                ╱───────╲  equipo — NO automatizados, NO en CI
               ╱         ╲
              ╱  E2E API   ╲  Handler completo + pipeline de
             ╱  (algunos)   ╲ MediatR real (Transaction/Validation)
            ╱────────────────╲ contra SQLite o Postgres reales
           ╱                  ╲
          ╱   Integration       ╲  La capa más grande y más rica:
         ╱   (SQLite + Postgres)  ╲ handlers reales contra un motor
        ╱──────────────────────────╲ relacional real, no mocks
       ╱                            ╲
      ╱   Unit (mocks / InMemory)     ╲  Lógica aislada, sin motor
     ╱──────────────────────────────────╲ relacional real
    ╱                                    ╲
   ╱   Frontend                           ╲  tsc -b + npm run build
  ╱─────────────────────────────────────────╲ (0 tests automatizados)
```

**Hallazgo que vale la pena decir sin rodeos:** este proyecto invirtió su
pirámide respecto a la forma "de libro de texto". La capa más grande y con
más inversión de ingeniería no es "muchos unit tests baratos" — es
**integración contra un motor relacional real** (SQLite o PostgreSQL). Esto
es una **DECISIÓN** consistente con el patrón repetido en toda la
arquitectura (ver ADR-001 y `01-Architecture/DATA-ARCHITECTURE.md`): la
base de datos es la autoridad final de los invariantes, así que los tests
que importan de verdad tienen que ejecutarse contra una base de datos real,
no contra un doble de prueba que no modela sus garantías.

---

## Mapa de tests existente

**HECHO** — 61 archivos de test reales (más 7 helpers de infraestructura de
test, no tests en sí) en `backend/tests/BeautyCommerce.Tests/`, organizados
en 12 carpetas:

| Carpeta | Archivos | Qué cubre | Motor de datos predominante |
|---|---|---|---|
| `Behaviors/` | 3 | Comportamiento de los pipeline behaviors de MediatR (logging, opt-out transaccional) | Mocks / SQLite |
| `Cache/` | 5 | Invalidación de cache, aislamiento por usuario, concurrencia del propio `MemoryCacheService` | SQLite / Postgres real (1 archivo) |
| `Commands/` | 12 | Handlers de escritura (checkout, productos, marcas, carrito, auth, envío, media) | Mocks / InMemory / SQLite |
| `Concurrency/` | 5 | Carreras reales contra Postgres (carrito, checkout, creación de producto, wishlist, inventario) | **Postgres real, siempre** |
| `Configurations/` | 1 | Normalización de connection string | Ninguno (lógica pura) |
| `Features/` | 1 | Dashboard admin | InMemory |
| `Integrity/` | 21 | La carpeta más grande — reproducción de defectos históricos, invariantes de negocio, constraints reales | Mixto: SQLite para la mayoría, Postgres real para 9 de los 21 |
| `Middlewares/` | 1 | Mapeo de excepciones a códigos HTTP | Ninguno |
| `Queries/` | 5 | Handlers de lectura (carrito, órdenes admin, catálogo) | Mixto: InMemory y Postgres real |
| `Security/` | 4 | Autorización por rol, CORS, validación de arranque del `Jwt:Key` | Ninguno (reflexión / host real en memoria) |
| `Services/` | 5 | `InventoryService`, `OutboxProcessor`, generador de SKU/Barcode, cliente de Resend, calculadora de envío | InMemory / SQLite / mocks |
| `Validators/` | 2 | Reglas de FluentValidation aisladas | Ninguno |

**Qué NO cubre esta suite, dicho explícitamente:** no hay tests de carga
(cuántas requests concurrentes soporta el sistema, no solo si dos
transacciones concurrentes resuelven correctamente), no hay tests de
contrato de API automatizados fuera de proceso (ningún test lanza el
servidor real y le pega por HTTP — todo corre en memoria contra el handler
o un `WebApplication` de prueba), y no hay ningún test de frontend
automatizado (ver "Frontend testing/build strategy").

---

## Test levels vs. responsabilidades

**HECHO/DECISIÓN**, consolidado de la lectura de los 61 archivos:

| Tipo | Debe demostrar | Motor de datos | Ejemplo real |
|---|---|---|---|
| **Unit** | Lógica aislada, sin dependencias externas reales | Mocks o InMemory | `ShippingCostCalculatorTests`, `Validators/*` |
| **Integration (SQLite)** | Interacción Application ↔ Infrastructure ↔ un motor relacional real, cuando **no** se necesita concurrencia real ni el dialecto exacto de PostgreSQL | SQLite en memoria | `CheckoutIdempotencyReproductionTests.SequentialRetry`, la mayoría de `Integrity/` |
| **Integration (Postgres real)** | Lo mismo, pero cuando el comportamiento depende de **concurrencia real**, **constraints/índices específicos de PostgreSQL**, o **aislamiento de transacciones real** | PostgreSQL real (`BeautyCommerceDb` local) | Toda `Concurrency/`, `CheckConstraintTests`, `SkuBarcodeUniquenessTests`, `EmailUniquenessConcurrencyTests` |
| **API/contrato HTTP** | Middleware + mapeo de excepciones + autorización — sin llegar a levantar el servidor completo por HTTP real | Ninguno / host en memoria | `GlobalExceptionHandlerTests`, `CorsConfigurationTests`, `JwtKeyStartupValidationTests` |
| **E2E de negocio** | El comportamiento completo de un flujo (checkout, cancelación) atravesando el pipeline real de MediatR, no solo el handler aislado | SQLite o Postgres real | `CheckoutTransactionIntegrityTests` (usa el `TransactionBehavior` real, no un mock del comportamiento transaccional) |
| **DB** | Constraints, concurrencia, atomicidad — la base de datos como autoridad final | Postgres real, siempre | `CheckConstraintTests`, `CascadeDeleteTests` |
| **Frontend** | Que el código compile y tipe correctamente — **no** comportamiento de UI/estado (no existe esa capa hoy) | N/A | `tsc -b && vite build` |
| **Manual / navegador** | Que la UI real renderice y se comporte correctamente para un flujo específico | N/A | Verificación puntual con Playwright, no repetible automáticamente (ver sesión de esta misma documentación, verificación de lazy-loading del panel admin) |

---

## Testing de invariantes críticos

**HECHO**, cada invariante con su evidencia real, no aspiracional:

| Invariante | Evidencia (archivo : método) |
|---|---|
| Stock nunca negativo | `Concurrency/CheckoutConcurrencyTests.cs`, `Concurrency/InventoryConcurrencyTests.cs` (ambos métodos — ver Concurrencia abajo), `Services/InventoryServiceTests.cs`, `Integrity/CheckConstraintTests.cs` (el constraint mismo) |
| Precio histórico de `OrderItem` inmutable | `Commands/Orders/UpdateOrderStatusCancellationRestockTests.cs :: Cancelling_Paid_Order_Does_Not_Alter_Historical_Price_Or_Award_Loyalty_Points` — probado en contraste directo con `Integrity/CartPriceRefreshReproductionTests.cs`, que prueba la política **opuesta** para el carrito (`ShoppingCartItem.UnitPrice` sí se refresca) — la pareja de tests deja explícita la distinción documentada en `01-Architecture/DATA-ARCHITECTURE.md` → Modelo de precios |
| Idempotencia de checkout | `Integrity/CheckoutIdempotencyReproductionTests.cs` — los 3 métodos, ver Regression testing policy |
| Concurrencia | Ver sección dedicada abajo |
| Ownership/aislamiento por usuario | `Cache/UserIsolationTests.cs`, `Behaviors/CachingBehaviorTests.cs :: Should_Scope_Cache_Key_By_Current_User`, `Concurrency/AddCartItemConcurrencyTests.cs` |
| Autorización por rol | `Security/CatalogAuthorizationTests.cs`, `Security/CatalogPublicReadReproductionTests.cs` |
| Soft delete | `Integrity/CatalogSoftDeleteIntegrityTests.cs`, `Integrity/CascadeDeleteTests.cs` (prueba el caso contrario: hard-delete real sí cascada, a diferencia del soft-delete — consistente con el hallazgo de `01-Architecture/DATA-ARCHITECTURE.md`) |
| Cancelación + restitución de stock | `Commands/Orders/UpdateOrderStatusCancellationRestockTests.cs` — 6 métodos |
| Lockout de login | `Integrity/LoginLockoutReproductionTests.cs`, `Behaviors/TransactionBehaviorOptOutTests.cs` (el mecanismo que lo habilita) |
| Unicidad de email en registro | `Integrity/EmailUniquenessConcurrencyTests.cs` — 4 métodos |

---

## Testing de concurrencia: la distinción que más importa

**HECHO — esta es la sección donde el aprendizaje de B/C/E/I/J se vuelve
disciplina explícita.** El código de la propia suite ya distingue entre dos
categorías que un lector apurado podría confundir:

### Concurrencia real (dos+ contextos/conexiones lanzados con `Task.WhenAll`, sin esperar uno antes de iniciar el otro, contra Postgres real)

| Archivo : método | Qué carrera prueba | Qué la resuelve de verdad |
|---|---|---|
| `Concurrency/AddCartItemConcurrencyTests.cs :: Two_Concurrent_AddCartItem_Requests_For_A_Brand_New_User_Resolve_To_The_Same_Cart` | Dos requests creando el primer carrito de un usuario nuevo | Índice único `UNIQUE(UserId)` en `ShoppingCarts` + recuperación por savepoint en el handler |
| `Concurrency/CheckoutConcurrencyTests.cs :: Concurrent_Checkouts_Never_Both_Succeed` | Dos usuarios comprando las últimas 7 unidades | `ExecuteUpdateAsync` atómico con `WHERE Stock >= quantity` |
| `Concurrency/CreateProductConcurrencyTests.cs :: Concurrent_Product_Creations_Never_Produce_Colliding_Identifiers` | Dos productos creados al mismo instante | Verificación de existencia en DB + aislamiento real de transacción, probado bajo scheduling real, no asumido |
| `Concurrency/DbConstraintExceptionReproductionTests.cs :: Two_Concurrent_AddWishlist_For_The_Same_Product...` | Dos altas de wishlist para el mismo par usuario/producto | Constraint único + manejo idempotente de la violación |
| `Concurrency/InventoryConcurrencyTests.cs :: Concurrent_RegisterExitAsync_Calls_Never_Both_Succeed` | Dos salidas de stock reales compitiendo | `ExecuteUpdateAsync` atómico |
| `Integrity/CheckoutIdempotencyReproductionTests.cs :: ConcurrentDoubleClick.Two_Near_Simultaneous_Checkouts_...` | Doble clic del mismo usuario en el mismo carrito | El "reclamo temprano del carrito" dentro de la transacción (ver `01-Architecture/API-ARCHITECTURE.md`) |
| `Integrity/EmailUniquenessConcurrencyTests.cs :: Concurrent_Registrations_With_The_Same_Email` | Dos registros con el mismo email | Índice único `EmailIndex` de ASP.NET Identity sobre `NormalizedEmail` |

### Secuencial, deliberadamente NO concurrencia — y por qué eso está bien

**El ejemplo más importante de todo el documento:**
`Concurrency/InventoryConcurrencyTests.cs :: Deterministic_Interleaved_Exits_Cause_A_Lost_Update`
— el propio comentario del test lo dice sin ambigüedad: *"Manually
reproduces, step by step... two 'requests' deliberately interleaved... This
is deterministic (not dependent on OS/network scheduling)."* Dos contextos
leen el stock, y luego se escriben **en secuencia controlada por el propio
test** (A escribe, después B escribe basado en su lectura ya obsoleta).
**Esto no prueba que el sistema resista una carrera — prueba el mecanismo
exacto del bug que la corrección atómica (`ExecuteUpdateAsync`) previene.**
Es una reproducción controlada de la falla, no un stress test de la
solución — y el archivo tiene, en el mismo namespace, el test genuinamente
concurrente (`Concurrent_RegisterExitAsync_Calls_Never_Both_Succeed`) que sí
prueba la solución. La pareja de tests, uno al lado del otro, es la mejor
evidencia de que el equipo ya distingue estas dos categorías conscientemente.

Segundo ejemplo, mismo patrón: `Integrity/CheckoutIdempotencyReproductionTests.cs`
tiene **ambas** categorías en el mismo archivo — `SequentialRetry` (dos
llamadas de handler, una completamente esperada antes de que empiece la
otra — simula un reintento del cliente, no una carrera) junto a
`ConcurrentDoubleClick` (`Task.WhenAll` real). Son escenarios de negocio
distintos (reintento vs. doble clic simultáneo) y **ambos son necesarios**
— no es que uno sea "mejor" que el otro, es que prueban cosas diferentes.

**Regla que se puede extraer de la evidencia, no inventada:** un test
"de concurrencia" que solo llama al handler dos veces de forma secuencial
(por ejemplo, `Concurrency/DbConstraintExceptionReproductionTests.cs ::
AddCartItem_With_Zero_Quantity_Is_Rejected_By_Validation_Before_Reaching_The_Database`,
que pese a vivir en la carpeta `Concurrency/` es una sola llamada de
handler sin concurrencia real) **no demuestra nada sobre condiciones de
carrera** — demuestra otra cosa (en ese caso, qué capa rechaza una
cantidad inválida). Vivir en la carpeta `Concurrency/` no basta para que un
test *sea* un test de concurrencia.

### Principio de la estrategia, extraído de este patrón

**DECISIÓN.** El par `Deterministic_Interleaved_Exits_Cause_A_Lost_Update`
/ `Concurrent_RegisterExitAsync_Calls_Never_Both_Succeed` se adopta como
regla general de esta estrategia, no como una curiosidad de un solo
archivo:

> **Cuando el riesgo identificado es de concurrencia, la evidencia de
> regresión debe ejercitar concurrencia real (`Task.WhenAll` u
> equivalente, contra Postgres real) siempre que sea técnicamente
> posible.** Una reproducción secuencial del mecanismo de la falla es
> válida y útil como documentación del bug — pero no sustituye, y no se
> debe confundir con, la prueba de que la solución resiste una carrera
> real. Esto es una extensión directa de la lección de Escenario J: el
> perdedor de una carrera real puede comportarse distinto (a nivel de
> contrato HTTP, de excepción, de estado) de lo que un test secuencial
> asumiría.

---

## Testing contra PostgreSQL real vs. SQLite — cuándo cada uno es obligatorio

**HECHO/DECISIÓN**, con la razón técnica explícita citada del propio código
(`SqliteDbContextHelper.cs`, comentario referenciando "6.2.2"):

- **SQLite en memoria es suficiente cuando** el test necesita un motor
  relacional real (para que `SaveChanges` dispare validaciones/constraints
  reales que el proveedor `InMemory` de EF Core no modela), pero **no**
  necesita concurrencia real ni el dialecto exacto de PostgreSQL. Ejemplo:
  la mayoría de `Integrity/`.
- **PostgreSQL real es obligatorio cuando:**
  1. El comportamiento depende de que `ExecuteUpdateAsync`/
     `ExecuteDeleteAsync` se traduzcan a SQL real y se ejecuten
     atómicamente — el proveedor `InMemory` de EF Core **no soporta**
     estas operaciones en absoluto (razón técnica citada literalmente en
     `SqliteDbContextHelper.cs`).
  2. El test necesita **concurrencia genuina** — dos conexiones reales
     compitiendo — porque ni `InMemory` ni SQLite en memoria (que además,
     en la configuración usada aquí, comparte una sola conexión) modelan
     row-level locking ni aislamiento de transacciones de forma realista.
  3. El comportamiento depende de un **constraint o índice específico de
     PostgreSQL** (`CHECK`, índice único compuesto, `SQLSTATE 23514`) —
     probarlo contra SQLite probaría la sintaxis de SQLite, no la garantía
     real que protege producción.

**Consecuencia práctica de esta distinción, ya verificada:** el 9 de los
21 archivos de `Integrity/`, los 5 de `Concurrency/`, y algunos de `Cache/`
y `Queries/` **requieren una instancia local de PostgreSQL corriendo y
alcanzable** para pasar — no son opcionales para esos escenarios
específicos. Esto tiene una consecuencia directa sobre CI, ver la sección
siguiente.

---

## API/E2E evidence standard: reproduce → observe → change → verify → cleanup

**HECHO**, patrón repetido consistentemente en los tests etiquetados
`[Trait("Category", "Integration")]` que implementan `IAsyncLifetime`:

1. **Reproduce**: `InitializeAsync` siembra datos con prefijo `"QA "` en el
   nombre (`"QA Rated Product"`, `"QA Idempotency Race Product"`, etc.) —
   nunca datos de catálogo real.
2. **Observe**: el test ejecuta el handler/comando real contra esos datos
   sembrados.
3. **Change**: el test invoca la acción bajo prueba (checkout, cancelación,
   registro concurrente).
4. **Verify**: se abre una **conexión/contexto nuevo** para leer el estado
   final (`AsNoTracking()`), evitando que el resultado esté contaminado por
   el tracking de EF Core de la misma sesión que hizo el cambio — un
   patrón que aparece en prácticamente todos los tests de `Concurrency/` e
   `Integrity/`.
5. **Cleanup**: `DisposeAsync` borra exactamente lo que `InitializeAsync`
   sembró (por IDs trackeados, ej. `_usedUserIds`, `_createdProductIds`),
   no un `TRUNCATE` global.

**Cuándo un test unitario no es suficiente, con evidencia:** cuando el
comportamiento depende de qué hace *la base de datos*, no qué hace el
código C# — el ejemplo más claro es `Integrity/CheckConstraintTests.cs`,
que ejecuta SQL crudo para confirmar que un `INSERT` inválido es rechazado
con `SQLSTATE 23514`, algo que ningún test unitario contra un mock podría
demostrar.

---

## Regression testing policy

**HECHO, con los tres casos reales más ricos del proyecto como evidencia
directa** de que la disciplina reproducción → fix → test de regresión →
verificación E2E se aplicó de verdad, no solo se declaró:

| Caso | Reproducción | Fix | Regresión |
|---|---|---|---|
| **Escenario I** (lockout revertido) | `LoginLockoutReproductionTests.cs` demuestra que 5 intentos fallidos no bloqueaban la cuenta porque el rollback de `TransactionBehavior` deshacía el contador de Identity | `INotTransactional` en `LoginCommand` (ADR-004) | `TransactionBehavior/TransactionBehaviorOptOutTests.cs` prueba **ambas mitades**: el opt-out funciona Y los comandos ordinarios siguen haciendo rollback |
| **Escenario E** (cancelación no restituía stock) | Cobertura indirecta de inventario no capturaba que cancelar una orden `Paid` no llamaba a `IInventoryService` | Restitución explícita en `UpdateOrderStatusCommandHandler` (`Paid→Cancelled`) | `Commands/Orders/UpdateOrderStatusCancellationRestockTests.cs`, 6 métodos, incluyendo que **no** se reescribe el precio histórico ni se otorgan puntos de loyalty por una cancelación |
| **pre-7.5.3** (checkout concurrente sin manejar) | Dos checkouts casi simultáneos del mismo usuario podían producir una `DbUpdateConcurrencyException` sin capturar, llegando como 500 crudo al cliente | Captura explícita de `DbUpdateConcurrencyException` → `BadRequestException` en `CheckoutCommandHandler` | `Integrity/CheckoutIdempotencyReproductionTests.cs :: ConcurrentDoubleClick` prueba explícitamente `unhandledConcurrencyFailures.Should().Be(0)` |

**Otros marcadores históricos encontrados**, cada uno con su propio test de
reproducción/regresión, que confirman que este patrón no fue excepcional
sino la norma de trabajo del proyecto (numeración interna del propio
equipo, citada tal como aparece en el código):

| Marcador | Qué demuestra su test |
|---|---|
| 6.2 / 6.2.2 / 6.2.3 | Protección atómica contra sobreventa de stock, probada primero de forma determinista (el mecanismo de la falla) y luego bajo concurrencia real repetida (10 rondas) |
| 6.3 / 6.3.7 / 6.3.8 | Huecos de invalidación de cache en Brands/Categories/Reviews/Users cerrados; aislamiento por usuario en cache confirmado; un riesgo teórico de `CancellationTokenSource` bajo stress **no se reprodujo** (el test lo intentó y el código quedó sin cambios) |
| 6.4.2 / 6.4.4 | Ausencia total de `CHECK` constraints detectada por auditoría; migración `AddDomainCheckConstraints` la cerró con 12 constraints reales |
| 6.4.5 | 5 variantes reales con SKU/Barcode en blanco encontradas en auditoría; `IProductVariantIdentifierGenerator` + migración de unicidad lo resolvió |
| 6.5-B.2 | Auditoría encontró que la API no devolvía Color/Size ya existentes en 79 variantes reales del catálogo |
| 7.8.2 / 7.8.3 / 7.8.4 | `ImageUrl` aceptaba cualquier string sin validar; se diseñó y luego se implementó una regla de FluentValidation exigiendo URL http/https absoluta |
| 8.3 / 8.3.1 / 8.3.2 / 8.3.4 | Cantidad no positiva llegaba a la base de datos sin validar; carrera de creación de carrito para usuario nuevo; índice único que ahora protege contra un segundo carrito por usuario |
| 8.4.3 ("Policy C") | Política explícita: el precio del carrito se refresca con el precio vigente de catálogo mientras el ítem sigue en el carrito — contraste directo con el precio de `OrderItem`, que nunca se reescribe |

**Lo que esta tabla demuestra en conjunto:** este proyecto tiene una
historia de auditoría interna mucho más extensa que los 4 defectos
originalmente reconstruidos en `00-Governance/PROJECT-CHARTER.md`/`SCOPE.md`
en fases anteriores de esta documentación. La numeración `6.x`/`7.x`/`8.x`
sugiere una serie de fases de hardening sistemáticas — este hallazgo se
traslada como insumo directo para `06-Quality/AUDIT-PUNTOS-1-5.md`, que
debería ampliarse más allá de solo Escenario I/E/J.

### Regresión de CI, no solo de producto — hallazgo nuevo de esta pasada

**HECHO, verificado empíricamente contra el historial real de GitHub
Actions, no inferido de la lectura del YAML únicamente:**
`.github/workflows/backend-ci.yml` ejecuta `dotnet test BeautyCommerce.sln`
**sin ningún filtro** (`--filter Category!=Integration` no está presente),
sobre un runner que **no tiene ningún servicio de PostgreSQL configurado**.
El propio código de test ya lo anticipa —
`Concurrency/InventoryConcurrencyTests.cs:16-19` dice literalmente:

> *"Tagged 'Integration' so it can be excluded from the fast default run
> (`dotnet test --filter Category!=Integration`) and from CI, which has no
> Postgres service configured today."*

Es decir: **el propio equipo ya documentó, en un comentario, que estos
tests debían excluirse de CI** — pero el workflow real nunca aplicó ese
filtro. Verificado contra el historial real de ejecuciones
(`gh run list --workflow=backend-ci.yml`): de las últimas 26 ejecuciones,
**22 fallaron**; la corrida más reciente falla exactamente por
`Npgsql.NpgsqlException : Failed to connect to 127.0.0.1:5432` /
`Connection refused`, en múltiples tests de `Concurrency/`.

**Clasificación explícita:** esto es un **DEFECTO demostrado del pipeline
de CI** (no del producto — el código de aplicación funciona correctamente,
verificado localmente contra Postgres real) — no se corrige en este
documento, tal como se pidió no hacer cambios de código. Su consecuencia
para el resto de este documento es directa: **toda la evidencia de
concurrencia/constraints/idempotencia citada arriba solo se verifica hoy
cuando alguien corre los tests localmente contra su propio PostgreSQL** —
no hay confirmación continua automatizada de que siga siendo cierta en
cada cambio nuevo al repositorio. Ver "Release confidence levels" para
cómo esto afecta la lectura del resto del documento.

---

## Frontend testing/build strategy

**HECHO, por ausencia confirmada.** No existe ningún archivo `*.test.*` ni
`*.spec.*` en `frontend/web/src`, y `package.json` no tiene Vitest, Jest,
Playwright ni ningún script `test` configurado. La única verificación
automatizada del frontend es:

```
tsc -b && vite build
```

(el script `build` de `package.json`) — TypeScript debe tipar sin errores
y Vite debe empaquetar sin fallos. **Esto verifica corrección de tipos y
que el bundle se construya, no comportamiento de UI ni de estado.**

**Límite de cobertura actual, dicho sin rodeos:** ningún cambio de frontend
tiene una red de seguridad automatizada más allá del chequeo de tipos. La
única verificación de comportamiento real que existe hoy es manual —
navegación real o, en esta sesión de documentación, una verificación
puntual con Playwright dirigida por un agente (ver el trabajo de
lazy-loading del panel admin en este mismo proyecto) — **no repetible
automáticamente**, no forma parte de ningún pipeline.

---

## Test data / fixtures

**HECHO**, convención consistente en toda la suite:

- **Naming**: todo dato sembrado para un test de integración lleva el
  prefijo `"QA "` en campos visibles (`Name`, `Description`) —
  `"QA Brand"`, `"QA Rated Product"`, `"QA Idempotency Race Product"` — y
  frecuentemente un comentario explícito `"safe to delete"`.
- **Aislamiento**: cada clase de test que toca Postgres real implementa
  `IAsyncLifetime` con su propio `InitializeAsync`/`DisposeAsync`, o usa
  SQLite en memoria con una conexión descartable por test
  (`SqliteConnectionHolder`, `using var _ = connection`).
- **Cleanup**: `DisposeAsync` borra por ID trackeado (`_usedUserIds`,
  `_createdProductIds`, listas específicas), nunca un `TRUNCATE`/`DELETE`
  sin filtrar contra la base compartida.
- **Prohibición implícita, nunca escrita como regla explícita en ningún
  lado del código pero respetada consistentemente en los 61 archivos
  leídos:** ningún test muta datos de catálogo real preexistente — todo
  dato de prueba se crea y se destruye dentro del propio test. **MEJORA
  recomendada**: esta convención vive únicamente en la práctica repetida,
  no en un documento — vale la pena promoverla a regla explícita ahora que
  existe este documento.

---

## Flakiness policy

**HECHO — dos hallazgos distintos, clasificados por separado para no
mezclarlos:**

### 1. El defecto de CI (ya descrito arriba)

No es "flakiness" en sentido estricto — es una **falla determinística**
(falta un servicio de Postgres, así que **siempre** falla en ese entorno,
no intermitentemente). Se documenta en "Regression testing policy", no
aquí, para no confundir las dos categorías.

### 2. Riesgo estructural de flakiness real, no confirmado como incidente

**HECHO el mecanismo, RIESGO la manifestación** (no se encontró evidencia
de que esto haya fallado realmente en una corrida específica — se
documenta porque el mecanismo existe y es demostrable leyendo el código,
no porque se haya observado fallar):
`Integrity/CheckoutTransactionIntegrityTests.cs:298-301` define
`CountOutboxMessagesAsync()`, que ejecuta `context.OutboxMessages.CountAsync()`
**sin filtrar** contra la base de datos Postgres compartida
(`BeautyCommerceDb`) — usado como una cuenta antes/después
(`outboxCountBefore`/`outboxCountAfter`) alrededor de un intento de
checkout que debe fallar. Como xUnit no tiene ninguna configuración que
desactive la paralelización (confirmado por ausencia: sin
`xunit.runner.json`, sin `[assembly: CollectionBehavior]` en todo el
proyecto de tests), las clases de test corren en paralelo por defecto —
así que si **otra** clase de test de `Concurrency/`/`Integrity/` etiquetada
`Integration` inserta un `OutboxMessage` real en la misma base compartida
mientras esta ventana está abierta, la aserción de delta podría fallar
por una causa completamente ajena al comportamiento bajo prueba.

**Cómo se reconocería si ocurriera:** una falla intermitente y no
reproducible de
`CheckoutTransactionIntegrityTests.Failed_Payment_After_Stock_Reservation_Rolls_Back_Everything`
específicamente en la aserción de `outboxCountAfter`, que desaparece al
volver a correr la suite sola (sin el resto de tests de Integration en
paralelo).

**Por qué no se debe ocultar desactivando el paralelismo simplemente para
obtener verde:** desactivar la paralelización de xUnit globalmente
ocultaría el síntoma sin corregir la causa (una aserción que depende de
una tabla compartida sin aislamiento), y además ralentizaría toda la
suite — el fix correcto, si algún día se decide abordar esto, es que el
test cuente **solo** los mensajes relacionados a su propia orden/usuario
generado, igual que ya hace `EmailUniquenessConcurrencyTests.cs:283`
(`CountAsync` filtrado por `NormalizedEmail`, no global) — pero **eso no
se implementa en este documento**, queda como RIESGO documentado.

**Cuándo se considera preexistente:** si esto llegara a manifestarse, sería
correcto tratarlo como preexistente (no una regresión de un cambio
reciente) — el mecanismo que lo permitiría existe en el código desde que
se escribió `CheckoutTransactionIntegrityTests.cs`, no desde ningún cambio
posterior.

---

## Definition of Done

### Para un defecto

**HECHO, patrón consistente extraído de Escenario I/E/pre-7.5.3, no
inventado para este documento:**

1. Reproducción real con evidencia (test que falla mostrando el
   comportamiento incorrecto, o observación directa como el historial de
   CI).
2. Autorización explícita antes de tocar código (no se corrige nada "de
   paso").
3. Fix mínimo, acotado al defecto demostrado.
4. Test de regresión que prueba **ambas mitades**: que el fix funciona Y
   que no rompió el comportamiento anterior para los casos que no debían
   cambiar (ver `TransactionBehaviorOptOutTests.cs` como el ejemplo
   canónico).
5. `dotnet build` con 0 warnings/0 errores.
6. `dotnet test` en dos corridas consecutivas limpias (para distinguir un
   defecto real de un test flaky).
7. Verificación de que no queda dato de prueba residual en la base de
   datos.

### Para una feature

**DECISIÓN**, consistente con la regla de secuencia establecida en
`docs/README.md` (ninguna capacidad nueva no trivial se implementa sin su
documento de arquitectura primero):

1. Documento de arquitectura de la feature en `07-Features/` antes de
   escribir código.
2. Tests de integración para cada invariante de negocio nuevo que
   introduzca, siguiendo la matriz de "Test levels vs. responsabilidades"
   de este documento (¿necesita Postgres real o alcanza con SQLite?).
3. Si la feature toca un invariante ya cubierto en otra parte de la suite
   (precio, stock, ownership), un test de regresión que confirme que el
   invariante existente sigue sosteniéndose con la feature nueva presente.
4. Mismo cierre que un defecto: build limpio, dos corridas de test
   consecutivas, sin residuo en base de datos.

---

## Release confidence levels

**HECHO/DECISIÓN**, cruzando lo demostrado en este documento contra la
tabla de Release Readiness de `00-Governance/PROJECT-CHARTER.md` — con el
matiz nuevo de que "verde localmente" y "verificado en CI" **no son lo
mismo** en este proyecto hoy:

| Capacidad | Evidencia de test | Verificado en CI | Nivel |
|---|---|---|---|
| Stock / inventario | Concurrencia real + constraints + restitución en cancelación | ❌ (tests de Integration) | 🟡 — evidencia fuerte, pero no continua |
| Checkout / idempotencia | 3 escenarios reales incl. concurrencia genuina | ❌ | 🟡 |
| Precio histórico vs. precio de catálogo | Par de tests directamente contrastados | ❌ (mixto SQLite/Postgres) | 🟡 |
| Autenticación / lockout | Reproducción + regresión de ambas mitades | ❌ (usa SQLite en memoria — **no depende de Postgres real**, ver nota) | 🟢 — este caso específico probablemente sí corre en CI |
| Registro concurrente | Constraint real + captura de excepción | ❌ | 🟡 |
| Autorización por rol | Reflexión + host real en memoria | ✅ probablemente (no depende de Postgres) | 🟢 |
| Ownership (ver `01-Architecture/SECURITY-ARCHITECTURE.md`) | Parcial — `GetOrderById` no verificado línea por línea | N/A | 🔴 comportamiento no demostrado (pregunta abierta ya documentada en Security) |
| Pago real | No existe (simulador, ADR-007) | N/A | 🔴 |
| Frontend (UI/estado) | Cero tests automatizados | N/A | 🔴 comportamiento no demostrado más allá de tipado |
| Pipeline de CI en sí mismo | — | — | 🔴 **roto**, 22/26 corridas recientes fallidas |

**Nota sobre las filas marcadas 🟢 "probablemente":** no se verificó
ejecutando esos tests específicos de forma aislada en esta pasada para
confirmar con certeza que no dependen indirectamente de Postgres —
marcado como **INFERENCIA razonable**, no como hecho confirmado al 100%.

---

## Testing risks / backlog

Clasificación estricta, sin ascender automáticamente una ausencia a
defecto:

### 🔴 DEFECTO demostrado
- **Pipeline de CI no ejecuta correctamente los tests de Integration** —
  22/26 corridas recientes fallidas por falta de servicio de PostgreSQL en
  el runner, pese a que el propio código de test ya documentaba la
  necesidad de excluirlos. Ver "Regression testing policy".

### 🟠 RIESGO arquitectónico
- Riesgo estructural de flakiness en `CheckoutTransactionIntegrityTests`
  por conteo global no filtrado contra una tabla compartida (ver
  "Flakiness policy").
- Ownership de `GET /api/orders/{id}` no verificado (ya documentado en
  `01-Architecture/SECURITY-ARCHITECTURE.md`, repetido aquí porque afecta
  directamente el nivel de confianza de release de ese endpoint).
- Sin tests de carga/rendimiento — no se sabe cuántas requests concurrentes
  reales soporta el sistema, solo que las carreras de a dos se resuelven
  correctamente.

### 🟡 MEJORA recomendada
- Promover la convención de datos `"QA "` + `IAsyncLifetime` a regla
  explícita documentada (hoy vive solo en la práctica repetida).
- Ampliar `06-Quality/AUDIT-PUNTOS-1-5.md` con los marcadores 6.x/7.x/8.x
  encontrados en esta pasada, más allá de Escenario I/E/J.
- Considerar un test de contrato HTTP real (fuera de proceso) para al
  menos los flujos críticos (checkout, login) — hoy todo corre en memoria
  contra el handler, nunca contra el servidor real por HTTP.

### 🔵 REQUIREMENT DE PRODUCCIÓN
- Antes de cualquier lanzamiento real, decidir conscientemente si el
  pipeline de CI roto es aceptable o bloqueante — hoy la única red de
  seguridad continua real es el subconjunto de tests que no necesitan
  Postgres (autorización, validadores, algunos de `Commands/`), no la
  suite completa.

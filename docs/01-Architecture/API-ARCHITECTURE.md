# API Architecture — BeautyCommerce (Haldy&Co)

La pregunta que este documento responde no es "¿qué endpoints existen?" —
es **cómo fluye una intención del usuario desde HTTP hasta el dato
persistido y cómo vuelve la respuesta**, incluyendo seguridad, validación,
transacción, concurrencia, caché y errores. El mapa de 45 endpoints está
abajo, pero es evidencia de apoyo, no el objeto del documento.

Todo lo marcado **HECHO** se verificó leyendo los 15 controllers, el
`GlobalExceptionHandler`, `Program.cs`, y los tests de integridad de
checkout — no se documentan endpoints "ideales", solo los que existen. No
se cambió ningún código para escribir este documento.

## El recorrido real de una request

**HECHO**, integrando lo ya verificado en
`01-Architecture/SYSTEM-ARCHITECTURE.md` con lo específico de la capa HTTP:

```
Cliente (HTTP + JWT en header Authorization)
   │
   ▼
CORS ("DefaultCorsPolicy") → Authentication (JwtBearer) → Authorization
   │                                                          │
   │                                          [Authorize]/[AllowAnonymous]
   │                                          por controller/acción — sin
   │                                          FallbackPolicy global (ver
   │                                          "Autenticación y autorización")
   ▼
ASP.NET Core model binding + FluentValidation auto-validación
   │        (rechaza con 400 vía ModelState ANTES de llegar a MediatR,
   │         para el camino estándar — ver ADR-002 y la sección de abajo)
   ▼
IMediator.Send(command/query)
   │
   ▼
LoggingBehavior → PerformanceBehavior → TransactionBehavior →
CachingBehavior → ValidationBehavior → Handler
   │                                        │
   │                          (llama a servicios: IInventoryService,
   │                           IPaymentService, ICacheService, etc.)
   ▼
EF Core → PostgreSQL (constraints como autoridad final — ver
                       01-Architecture/DATA-ARCHITECTURE.md)
   │
   ▼
Controller: Ok/NoContent/CreatedAtAction/NotFound
   │                    (o, en excepción, GlobalExceptionHandler → ProblemDetails)
   ▼
Cliente: frontend/web/src/services/*Service.ts → getApiErrorMessage()
```

Este es el **comportamiento observado**, no una arquitectura deseada
dibujada aparte — cada flecha de este diagrama corresponde a código leído,
citado en las secciones siguientes.

---

## Mapa completo de la API

**HECHO**, 45 acciones verificadas en los 15 controllers, ninguna inferida.

### Autenticación (pública)

| Verbo + ruta | Autorización | Comando/Query | Éxito |
|---|---|---|---|
| `POST /api/auth/register` | ninguna (clase sin atributo) | `RegisterCommand` | `200 Ok` |
| `POST /api/auth/login` | ninguna | `LoginCommand` | `200 Ok` |
| `POST /api/auth/forgot-password` | ninguna | `ForgotPasswordCommand` | `200 Ok` |
| `POST /api/auth/reset-password` | ninguna | `ResetPasswordCommand` | `200 Ok` |

**RIESGO menor, no defecto:** `AuthController` no tiene `[Authorize]` ni
`[AllowAnonymous]` en clase ni acciones. Es públicamente accesible porque no
existe una `FallbackPolicy` global (ver "Autenticación y autorización") —
funciona hoy, pero depende de que nadie agregue una política global
restrictiva sin recordar excluir explícitamente este controller.

### Catálogo (público para lectura, Admin para escritura)

| Verbo + ruta | Autorización | Comando/Query | Éxito |
|---|---|---|---|
| `GET /api/products` | `[AllowAnonymous]` | `GetProductsQuery` | `200 Ok` |
| `GET /api/products/best-sellers` | `[AllowAnonymous]` | `GetBestSellersQuery` | `200 Ok` |
| `GET /api/products/{id}` | `[AllowAnonymous]` | `GetProductByIdQuery` | `200 Ok` (**sin manejo explícito de "no encontrado" en la acción**) |
| `POST /api/products` | `[Authorize(Roles="Admin")]` | `CreateProductCommand` | `201 CreatedAtAction` |
| `PUT /api/products/{id}` | `[Authorize(Roles="Admin")]` | `UpdateProductCommand` | `204 NoContent` |
| `DELETE /api/products/{id}` | `[Authorize(Roles="Admin")]` | `DeleteProductCommand` (soft delete) | `200 Ok` / `404` si `!result` |
| `GET /api/brands`, `/{id}` | `[AllowAnonymous]` | `GetAllBrandsQuery`/`GetBrandByIdQuery` | `200 Ok` / `404` |
| `POST/PUT/DELETE /api/brands...` | `[Authorize(Roles="Admin")]` | `Create/Update/DeleteBrandCommand` | `200 Ok` / `404` |
| `GET /api/categories`, `/{id}` | `[AllowAnonymous]` | `GetAllCategoriesQuery`/`GetCategoryByIdQuery` | `200 Ok` / `404` |
| `POST/PUT/DELETE /api/categories...` | `[Authorize(Roles="Admin")]` | `Create/Update/DeleteCategoryCommand` | `200 Ok` / `404` |
| `GET /api/reviews/featured` | `[AllowAnonymous]` | `GetFeaturedReviewsQuery` | `200 Ok` |
| `GET /api/reviews/product/{id}` | `[AllowAnonymous]` | `GetProductReviewsQuery` | `200 Ok` |
| `POST /api/reviews` | `[Authorize]` (clase) | `CreateReviewCommand` | `200 Ok` |

### Cliente autenticado (Customer)

| Verbo + ruta | Autorización | Comando/Query | Éxito | Ownership |
|---|---|---|---|---|
| `POST/GET/PUT/DELETE /api/cart...` | `[Authorize]` (clase) | `Add/Get/Update/RemoveCartItemCommand`, `ClearCartCommand` | `200/204` | La **acción misma** lee `UserId` de `ClaimTypes.NameIdentifier` — no viene de la ruta ni del body |
| `POST /api/orders/checkout` | `[Authorize]` | `CheckoutCommand` | `200 Ok` | En el handler, vía `ICurrentUserService` |
| `GET /api/orders`, `/{id}` | `[Authorize]` | `GetMyOrdersQuery`/`GetOrderByIdQuery` | `200 Ok` / `404` | En el handler |
| `POST/GET/DELETE /api/wishlist...` | `[Authorize]` | `Add/Get/RemoveWishlistCommand` | `200/204` | En el handler |
| `GET /api/loyalty`, `/history` | `[Authorize]` | `GetMyLoyaltyQuery`/`GetLoyaltyHistoryQuery` | `200 Ok` | En el handler |
| `POST /api/newsletter/subscribe` | `[AllowAnonymous]` (clase) | `SubscribeNewsletterCommand` | `200 Ok` | N/A |

### Administración (rol `Admin` exclusivamente)

| Verbo + ruta | Autorización | Comando/Query | Éxito |
|---|---|---|---|
| `GET /api/admin/dashboard` | `[Authorize(Roles="Admin")]` (clase) | `GetDashboardQuery` | `200 Ok` |
| `GET /api/inventory`, `/movements` | `[Authorize(Roles="Admin")]` (clase) | `GetInventoryQuery`/`GetInventoryMovementsQuery` | `200 Ok` |
| `PUT /api/inventory/stock` | `[Authorize(Roles="Admin")]` | `UpdateStockCommand` | `204 NoContent` |
| `POST /api/media/sync-drive-images` | `[Authorize(Roles="Admin")]` (clase) | `SyncImagesFromDriveCommand` | `200 Ok` |
| `GET /api/admin/orders`, `/{id}` | `[Authorize(Roles="Admin")]` (clase) | `GetAllOrdersQuery`/`GetAdminOrderByIdQuery` | `200 Ok` / `404` |
| `PUT /api/admin/orders/status`, `/shipping` | `[Authorize(Roles="Admin")]` | `UpdateOrderStatusCommand`/`UpdateOrderShippingCommand` | `200 Ok` / `404` |
| `GET /api/users`, `/customers` | `[Authorize(Roles="Admin")]` (clase) | `GetUsersQuery`/`GetCustomersQuery` | `200 Ok` |

**HECHO — inconsistencia de response envelope, no defecto:** conviven tres
formas de éxito en el cuerpo de la respuesta: `ApiResponse<T>` (una clase
real, `Common/Models/ApiResponse.cs`, con `Success`/`Message`/`Data`/
`Errors`) usada en `Brands`/`Categories`/algunas de `Products`; objetos
anónimos que imitan esa forma a mano (`new { Success, Message }` en `Auth`,
`Newsletter`, `Admin/Orders`); y el resultado crudo del query/command sin
envolver en ningún lado (`Cart`, `Orders`, `Wishlist`, `Loyalty`,
`Inventory`, `Dashboard`, `Users`, la mayoría de `Products`). No hay un
contrato de éxito único — el frontend maneja esto porque cada
`*Service.ts` conoce la forma específica de su propio endpoint, pero no
existe una convención de respuesta a nivel de API.

---

## Convenciones HTTP — cuándo se usa cada código realmente

**HECHO**, mapeo exacto de `GlobalExceptionHandler.cs:71-88`:

| Código | Se dispara por | Ejemplo real |
|---|---|---|
| `200 Ok` | Éxito de la mayoría de acciones (incluso creaciones en algunos controllers — ver inconsistencia abajo) | Casi todo el catálogo de lectura, login, checkout |
| `201 Created` | Solo `POST /api/products` usa `CreatedAtAction` | El resto de creaciones (`Brand`, `Category`, `Review`) devuelve `200 Ok`, no `201` |
| `204 NoContent` | Actualizaciones/eliminaciones que no devuelven cuerpo | `PUT /api/products/{id}`, operaciones de carrito/wishlist |
| `400 BadRequest` | `BadRequestException`, `FluentValidation.ValidationException`, `ArgumentException`, `InvalidOperationException` | Carrito vacío, stock insuficiente, transición de estado inválida |
| `401 Unauthorized` | `UnauthorizedException`, `UnauthorizedAccessException`, o falta de token JWT válido | Login con credenciales incorrectas; request sin token a un endpoint `[Authorize]` |
| `403 Forbidden` | Falla de `[Authorize(Roles=...)]` — **no pasa por `GlobalExceptionHandler`** (ver Contrato de errores) | Un `Customer` intentando `GET /api/admin/dashboard` |
| `404 NotFound` | `NotFoundException`, `KeyNotFoundException`, o `NotFound()` explícito cuando un handler devuelve `false`/`null` | Producto/orden/categoría inexistente |
| `409 Conflict` | `ConflictException` | Registro concurrente con email duplicado (ver ADR-004 y la sección de Concurrencia) |
| `500 Internal Server Error` | Cualquier excepción no mapeada explícitamente | Fallback genérico — nunca debería ser la respuesta esperada de un flujo conocido |

**HECHO — inconsistencia real, no defecto crítico:** `201 Created` casi no
se usa (solo un endpoint de 45), pese a que varias acciones de creación
existen. No hay evidencia de que esto rompa al frontend (que no depende del
código de estado para decidir su lógica, solo del cuerpo), pero es una
desviación de la convención HTTP estándar (`POST` que crea un recurso
"debería" devolver `201` con `Location`).

---

## Contrato de errores

### El camino normal: `ProblemDetails`

**HECHO**, `GlobalExceptionHandler.cs:43-51,62-66`: toda excepción no
capturada llega a `IExceptionHandler.TryHandleAsync`, se mapea a
`(statusCode, title)`, y se serializa como
`Microsoft.AspNetCore.Mvc.ProblemDetails`:

```json
{
  "status": 400,
  "title": "Solicitud inválida.",
  "detail": "El carrito está vacío.",
  "instance": "/api/orders/checkout"
}
```

`Detail` es el mensaje real de la excepción **excepto para 500**, donde se
reemplaza por un mensaje genérico ("Ocurrió un error inesperado...") para
no filtrar detalles internos al cliente — una decisión de seguridad
correcta, verificada línea por línea (`:47-49`). Para
`FluentValidation.ValidationException`, además se agrega
`Extensions["errors"]` con los errores agrupados por campo (`:53-60`).

### El camino roto: 403 sin cuerpo

**HECHO, mecanismo confirmado por lectura de código; comportamiento en
ejecución no verificado empíricamente en esta pasada** (no se levantó el
servidor para probarlo en vivo, por la instrucción de no tocar/ejecutar
cambios durante este documento). Un fallo de `[Authorize(Roles="Admin")]`
**no lanza una excepción de aplicación** — lo resuelve el middleware de
autorización de ASP.NET Core directamente, llamando a `ForbidAsync()` sobre
el esquema de autenticación por defecto. Como `Program.cs` configura
`DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme`
(`:126-127`) sin `DefaultForbidScheme` explícito, y `JwtBearerEvents` solo
tiene `OnAuthenticationFailed` configurado (`:143-149`, sin `OnForbidden`),
el comportamiento por defecto de `JwtBearerHandler` para un forbid es
escribir únicamente el código de estado `403` — **sin cuerpo, sin
`ProblemDetails`, sin pasar por `GlobalExceptionHandler` en absoluto**. Esto
es consistente con lo que el equipo ya había identificado como "el 403
vacío" durante el hardening previo.

Adicionalmente, `Program.cs:78-103` configura eventos de la cookie de
Identity (`OnRedirectToLogin`/`OnRedirectToAccessDenied`) para devolver
401/403 en vez de redirigir cuando la ruta empieza con `/api` — pero dado
que JwtBearer (no la cookie) es el esquema por defecto, **es INFERENCIA, no
un hecho confirmado, que este código de la cookie realmente se ejecute para
requests de API** en la práctica; puede ser código muerto heredado de
cuando se configuró `AddIdentity`. No se verificó con un request real en
esta pasada — queda marcado como pregunta abierta para
`01-Architecture/SECURITY-ARCHITECTURE.md`.

**Consecuencia real para el frontend:** `getApiErrorMessage.ts:1-13` lee
`error.response.data.detail` y cae a `data.message` si no existe; para un
403 sin cuerpo, ninguno de los dos existe, así que **siempre** se muestra
el mensaje de `fallback` que el desarrollador haya puesto a mano en ese
punto de llamada — nunca un mensaje específico del backend. No es un
defecto (el frontend no se rompe, siempre hay algún mensaje), pero es la
única familia de errores de todo el sistema donde el backend no puede
comunicar *por qué* falló — 400/401/404/409/500 sí lo hacen.

### `getApiErrorMessage` como consumidor único

**HECHO.** Es la única función central de manejo de error del frontend
(`frontend/web/src/services/apiError.ts`); cada componente que llama a un
servicio la usa con su propio `fallback` string. No hay un interceptor
global de Axios que centralice el manejo de 401 (por ejemplo, redirigir a
`/login` automáticamente en cualquier 401) — **no verificado en esta
pasada si existe tal interceptor**; se marca como pregunta abierta, no como
ausencia confirmada.

---

## Autenticación y autorización

**HECHO**, `Program.cs:116-150`: JWT vía `AddJwtBearer`, con
`ValidateIssuer`, `ValidateAudience`, `ValidateIssuerSigningKey` todos en
`true`, clave HMAC validada al arranque (`ValidateOnStart()`, mínimo 128
bits). Los roles se emiten como claim `ClaimTypes.Role` (ver
`01-Architecture/SYSTEM-ARCHITECTURE.md` → `JwtTokenGenerator`).

**HECHO — sin política global:** no existe ningún `AddAuthorization(options
=> ...)` con `FallbackPolicy`/`DefaultPolicy` en `Program.cs`. La
autorización es 100% declarativa por controller/acción
(`[Authorize]`/`[Authorize(Roles=...)]`/`[AllowAnonymous]`). Esto significa
que un controller nuevo sin ningún atributo **es público por defecto** — el
mismo patrón que ya deja a `AuthController` sin protección explícita
(correcto para ese caso, porque debe ser público, pero el mecanismo que lo
permite es "ausencia de atributo", no una decisión explícita de "esto es
público").

**Ownership (aislamiento por usuario):** dos patrones distintos,
verificados:

1. **En la acción del controller** (`CartController`): lee
   `ClaimTypes.NameIdentifier` directamente y lo pasa al comando/query — el
   patrón más explícito de los dos.
2. **En el handler**, vía `ICurrentUserService.UserId`
   (`Infrastructure/Identity/CurrentUserService.cs:16-27`), que también lee
   de `ClaimTypes.NameIdentifier` del `HttpContext.User` ya autenticado —
   **no es un valor que el cliente pueda spoofear** (no viene de ruta, body
   ni header custom), es derivado del JWT ya validado por el middleware.
   Este es el patrón usado por `Orders`, `Wishlist`, `Loyalty`, `Cart`
   (para las acciones que no lo hacen en el controller), `Reviews`.

**RIESGO ya documentado en `01-Architecture/DATA-ARCHITECTURE.md`,
repetido aquí por ser relevante a la capa de API:** ambos patrones son
seguros en el sentido de que no dependen de input del cliente, pero
**ninguno tiene respaldo de base de datos** — si un handler nuevo olvida
filtrar por `UserId`, no hay un query filter global (como sí existe para
soft delete) que lo detecte.

**Qué ve cada rol, por diseño de atributos (no una tabla aparte —
completa en "Matriz de seguridad" más abajo):** Anónimo ve catálogo
público, se registra, inicia sesión. Customer, además, opera su propio
carrito/wishlist/pedidos/loyalty. Admin, además, administra todo el
catálogo y ve/opera sobre todos los pedidos, inventario, clientes y
dashboard. No existe un rol intermedio (ej. "editor de catálogo sin acceso
a pedidos") — es binario Customer/Admin.

---

## Commands vs Queries

**HECHO**, consistente con `01-Architecture/SYSTEM-ARCHITECTURE.md`:
nomenclatura por sufijo de tipo (`...Command` muta estado y pasa por
`TransactionBehavior`; `...Query` lee y pasa por `CachingBehavior`, salvo
las exclusiones ya documentadas en ADR-006). Cada acción de controller
despacha exactamente un command o query — no hay acciones que orquesten
varios `Send()` en secuencia (**HECHO por inspección del inventario de 45
endpoints**: ninguna acción del inventario llama a `_mediator.Send()` más
de una vez).

**Excepción de nomenclatura encontrada:** ninguna — los 45 endpoints
inventariados respetan el sufijo `Command`/`Query` sin excepción.

---

## Idempotencia y concurrencia

Esta es la sección con más evidencia empírica real de todo el documento —
respaldada por
`backend/tests/BeautyCommerce.Tests/Integrity/CheckoutIdempotencyReproductionTests.cs`,
que reproduce tres escenarios reales contra SQLite/PostgreSQL, no
solamente los describe.

### El patrón "reclamo temprano del carrito"

**HECHO**, `CheckoutCommandHandler.cs:67-79`: lo primero que hace el
handler, antes de validar disponibilidad o cobrar, es borrar los ítems del
carrito y hacer `SaveChangesAsync` inmediatamente — capturando
explícitamente `DbUpdateConcurrencyException` para convertirla en
`BadRequestException("Este carrito ya fue procesado.")`. Esto **reclama**
el carrito atómicamente antes de continuar.

**Por qué esto es seguro, no un atajo peligroso:** todo el handler corre
dentro de la transacción ambiental de `TransactionBehavior` (es un
`Command`). Si algo falla después del reclamo (pago rechazado, stock
insuficiente), `TransactionBehavior` hace rollback de **toda** la
transacción, incluyendo el borrado del carrito — confirmado por el test
`Failed_Payment_After_Claiming_The_Cart_Rolls_Back_The_Claim_And_A_Retry_Can_Succeed`,
que verifica que tras un pago fallido el carrito **sigue teniendo su ítem**
y el stock **no se movió**, y que un reintento posterior sí puede
completarse con éxito.

### Qué pasa en cada escenario, con evidencia de test

| Escenario | Test | Resultado verificado |
|---|---|---|
| Reintento secuencial después de un checkout ya exitoso | `Retrying_Checkout_After_It_Already_Succeeded_Finds_An_Empty_Cart_And_Is_Rejected` | El reintento encuentra el carrito vacío (una nueva conexión/contexto lo confirma) y es rechazado con `400` antes de llegar al pago — **nunca se llama `IPaymentService` dos veces**, cero riesgo de doble cobro. Una sola `Order` persiste. |
| Pago rechazado tras reclamar el carrito | `Failed_Payment_After_Claiming_The_Cart_Rolls_Back_The_Claim_And_A_Retry_Can_Succeed` | Rollback completo (carrito, stock); un reintento posterior sí puede tener éxito. Ninguna `Order` fantasma queda creada. |
| Dos checkouts casi simultáneos del mismo usuario/carrito (concurrencia real, `Task.WhenAll`) | `Two_Near_Simultaneous_Checkouts_By_The_Same_User_For_The_Same_Cart` | Exactamente 1 gana, el otro es rechazado **limpiamente** (400, no un 500 de `DbUpdateConcurrencyException` sin manejar) **antes** de tocar stock o pago. El comentario del test cita explícitamente que este era "el modo de falla pre-7.5.3" — es decir, **hubo una versión anterior de este código donde la excepción de concurrencia SÍ llegaba sin manejar al cliente como 500**, y se corrigió. |

**Otros mecanismos de concurrencia ya documentados y aplicables aquí:**

- **Stock**: `ExecuteUpdateAsync` con `WHERE Stock >= quantity` en una sola
  sentencia SQL (ver `01-Architecture/DATA-ARCHITECTURE.md` → Inventario) —
  compare-and-swap atómico, no lectura-luego-escritura.
- **Registro de usuario**: índice único de Identity + captura de
  `PostgresException` con `SqlState == UniqueViolation` → `ConflictException`
  → `409` (ver ADR y `06-Quality/AUDIT-PUNTOS-1-5.md` cuando se escriba) —
  mismo principio: la base de datos, no un pre-check de aplicación, es la
  autoridad final ante una carrera.
- **Login/lockout**: ver ADR-004 — no es un problema de concurrencia entre
  requests, sino de una transacción que revertía su propio efecto
  secundario; se resolvió con `INotTransactional`, no con locking.

**Patrón consistente en las tres soluciones:** ninguna usa locks
explícitos, semáforos ni colas — todas se apoyan en que **la operación
final contra PostgreSQL sea atómica y falle de forma detectable**
(`ExecuteUpdateAsync` con condición, índice único, `DbUpdateConcurrencyException`
capturada), y el código de aplicación solo traduce ese fallo a una
respuesta HTTP correcta.

---

## Caching

**HECHO**, ya documentado en detalle en
`01-Architecture/SYSTEM-ARCHITECTURE.md` (ADR-006) — resumen orientado a
API: toda `Query` se cachea 5 minutos en memoria salvo las de los
namespaces `ShoppingCart`, `Dashboard`, `Wishlist`, `Loyalty` (exclusión
deliberada). Esto significa, a nivel de endpoint: `GET /api/products`,
`/api/brands`, `/api/categories`, `/api/reviews/*`, `/api/admin/orders*`,
`/api/inventory*`, `/api/users*` **se cachean**; `GET /api/cart`,
`/api/admin/dashboard`, `/api/wishlist`, `/api/loyalty*` **no**.

**Invalidación:** no es automática por invalidación de clave — varios
handlers de escritura llaman explícitamente `_cache.InvalidateTagAsync(...)`
(`"Products"`, `"Inventory"`, `"Orders"`) después de mutar. Esto significa
que la invalidación depende de que cada comando nuevo recuerde invalidar el
tag correcto — no hay una relación automática entre "esta entidad cambió" y
"invalida esta clave de cache".

**Riesgo ya documentado, repetido aquí por relevancia directa a la API:**
las claves de TanStack Query del frontend (`"admin-product"`,
`"admin-product-edit"`, `"admin-products"`) están duplicadas como literales
en tres archivos (ver `00-Governance/SCOPE.md` → Backlog) — es un riesgo de
**cache del cliente**, distinto y no relacionado directamente al
`CachingBehavior` del backend, pero comparten la misma familia de problema:
invalidación de cache dependiente de que un humano recuerde todas las
claves relacionadas.

---

## API pública vs. administrativa

**HECHO**, derivado directamente del mapa de endpoints:

- **Pública (sin autenticación):** catálogo de lectura (productos, marcas,
  categorías, reseñas destacadas/por producto), autenticación, newsletter.
- **Customer (autenticado, sin rol especial):** carrito, checkout, mis
  pedidos, wishlist, loyalty, crear reseña.
- **Admin exclusivo:** todo lo demás — CRUD de catálogo, dashboard,
  inventario, gestión de pedidos de cualquier usuario, gestión de clientes,
  sincronización de imágenes.

No existe una superficie de API "solo lectura para partners" ni un tercer
nivel de rol — la matriz completa está abajo.

---

## Matriz de seguridad por endpoint

**HECHO**, derivada exclusivamente de los atributos reales encontrados —
ninguna celda se completó por suposición del propósito del endpoint.

| Recurso | Anónimo | Customer | Admin | Ownership |
|---|---|---|---|---|
| Catálogo (productos/marcas/categorías) — lectura | ✅ | ✅ | ✅ | N/A |
| Catálogo — escritura (crear/editar/eliminar) | ❌ | ❌ | ✅ | N/A |
| Reseñas — lectura | ✅ | ✅ | ✅ | N/A |
| Reseñas — crear | ❌ | ✅ | ✅ (hereda `[Authorize]` de clase, sin exclusión de rol) | Enforced en handler (no verificado línea por línea en esta pasada) |
| Auth (registro/login/recuperar contraseña) | ✅ | ✅ | ✅ | N/A |
| Newsletter — suscribirse | ✅ | ✅ | ✅ | N/A |
| Carrito (propio) | ❌ | ✅ | ✅ (mismo atributo `[Authorize]`, sin distinción de rol) | Propio — `UserId` de claims en la acción |
| Checkout | ❌ | ✅ | ✅ | Propio — vía `ICurrentUserService` en handler |
| Mis pedidos | ❌ | ✅ | ✅ | Propio — vía handler |
| Wishlist (propia) | ❌ | ✅ | ✅ | Propio — vía handler |
| Loyalty (propia) | ❌ | ✅ | ✅ | Propio — vía handler |
| Dashboard admin | ❌ | ❌ | ✅ | N/A |
| Pedidos — todos (admin) | ❌ | ❌ | ✅ | N/A |
| Pedidos — cambiar estado/envío | ❌ | ❌ | ✅ | N/A |
| Inventario (consulta/ajuste) | ❌ | ❌ | ✅ | N/A |
| Clientes (listado admin) | ❌ | ❌ | ✅ | N/A |
| Sincronización de imágenes (Drive→Cloudinary) | ❌ | ❌ | ✅ | N/A |

**Nota metodológica importante:** las filas de "Carrito", "Checkout", "Mis
pedidos", "Wishlist", "Loyalty" y "Reseñas — crear" marcan ✅ para Admin
porque el atributo real es `[Authorize]` sin restricción de rol — **un
usuario con rol Admin también puede operar como cliente normal** (tener su
propio carrito, hacer checkout, etc.). No hay evidencia de que esto sea
indeseado; se documenta porque la matriz debe reflejar el atributo real, no
una suposición de que "Admin" implica exclusividad en todo.

---

## Evolución / versionado

**HECHO, por ausencia confirmada.** No existe `AddApiVersioning`, ningún
atributo `[ApiVersion]`, ni prefijo `/v1/` en ninguna ruta real — todas las
rutas son `api/[controller]` planas. El único lugar donde aparece "v1" es
la etiqueta del documento de Swagger (`SwaggerDoc("v1", ...)`), que es
metadata de documentación, no una estrategia de versionado real.
**Consecuencia:** cualquier cambio incompatible a un contrato de request/
response hoy es, por definición, un breaking change para el único
consumidor actual (el frontend propio) sin ningún mecanismo de convivencia
entre versiones. Aceptable mientras backend y frontend se desplieguen
siempre juntos desde el mismo repositorio (**SUPUESTO** — no confirmado que
ese acoplamiento de despliegue sea garantizado; ver
`01-Architecture/SYSTEM-ARCHITECTURE.md` sobre despliegues independientes
en Render/Cloudflare Pages, que en principio permite que frontend y backend
avancen desacoplados).

---

## Observabilidad

**HECHO:**

- **Logging:** Serilog, con `UseSerilogRequestLogging()` en el middleware
  (`01-Architecture/SYSTEM-ARCHITECTURE.md`), más los logs explícitos de
  `LoggingBehavior`/`PerformanceBehavior` (Information/Warning) y
  `GlobalExceptionHandler` (Warning para errores de negocio, Error para
  500).
- **Correlation/Request ID:** **no existe.** No se encontró ningún
  middleware ni configuración de `TraceIdentifier` personalizado ni header
  `X-Correlation-Id` en `Program.cs`. ASP.NET Core genera un
  `HttpContext.TraceIdentifier` por defecto, pero no hay evidencia de que
  se propague al log estructurado ni se devuelva al cliente — si un
  usuario reporta un error, no hay un ID que correlacione su request
  específico con una línea de log concreta más allá de buscar por
  timestamp/mensaje.
- **Auditoría de datos:** `CreatedAt`/`UpdatedAt`/`DeletedAt` +
  `CreatedBy`/`UpdatedBy`/`DeletedBy` automáticos vía `BaseEntity` (ver
  `01-Architecture/DATA-ARCHITECTURE.md`). `InventoryMovement` es un
  historial de auditoría específico de dominio. `OutboxMessage` deja rastro
  de qué eventos asíncronos se procesaron o fallaron (`Error` field).
- **Qué no existe:** métricas (no hay integración de OpenTelemetry,
  Application Insights, ni Prometheus encontrada), tracing distribuido,
  dashboards de error rate/latencia más allá de lo que Render capture de
  la salida por consola (ver `docs/DEPLOYMENT.md`).

---

## Limitaciones conocidas

- **`PaymentService` es un simulador** (ver ADR-007) — ningún endpoint de
  esta API mueve dinero real todavía.
- **No existe ningún mecanismo, vía API o configuración, para forzar un
  fallo de pago desde fuera del proceso.** `IPaymentService.CreatePaymentAsync`
  solo puede fallar hoy si `amount <= 0` (`PaymentService.cs:28-33`) —
  no hay un endpoint de test, header especial, ni modo de configuración
  que permita a un tester o a un pipeline de CI provocar un rechazo de pago
  de forma determinista contra el sistema real (los tests que sí lo hacen,
  como `CheckoutIdempotencyReproductionTests`, lo logran mockeando
  `IPaymentService` directamente en memoria, no a través de la API HTTP).
- **El 403 sin cuerpo** (ver Contrato de errores) es la única familia de
  error sin mensaje específico para el cliente.
- **Riesgo de resolución histórica de productos eliminados:** ya
  documentado en detalle en `01-Architecture/DATA-ARCHITECTURE.md` — un
  pedido histórico puede dejar de mostrar nombre/imagen de producto si ese
  producto se soft-elimina después, porque `GetOrderById`/`GetMyOrders` no
  usan `IgnoreQueryFilters()`. Se repite aquí porque es, en última
  instancia, un problema de contrato de API: el cliente puede recibir un
  campo `null`/vacío para un pedido que antes lo mostraba correctamente,
  sin ningún cambio explícito de versión que lo anuncie.
- **Sin endpoint para consultar el historial de `InventoryMovement` de un
  producto específico desde una UI** — el dato existe (`GET
  /api/inventory/movements` existe a nivel de API), pero no hay consumo
  desde el frontend (ver `00-Governance/SCOPE.md` → Backlog).
- **Respuesta de éxito sin contrato único** (`ApiResponse<T>` vs. objetos
  anónimos vs. resultado crudo) — ver la nota en "Mapa completo de la API".

---

## Qué queda para `SECURITY-ARCHITECTURE.md`

Este documento estableció el mecanismo de autenticación/autorización y la
matriz de qué endpoint requiere qué rol. Quedan fuera de este documento, a
propósito, para tratarse con profundidad en
`01-Architecture/SECURITY-ARCHITECTURE.md`: modelo de amenazas, trust
boundaries, gestión de secretos (`Jwt:Key`, credenciales de Cloudinary/
Google Drive/Resend), qué datos se consideran sensibles, y controles
preventivos vs. detectivos.

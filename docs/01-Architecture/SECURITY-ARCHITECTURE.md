# Security Architecture — BeautyCommerce (Haldy&Co)

Este no es un checklist genérico de OWASP — es el modelo de seguridad de
**este** sistema, construido sobre la evidencia ya reunida en
`01-Architecture/SYSTEM-ARCHITECTURE.md`, `DOMAIN-MODEL.md`,
`DATA-ARCHITECTURE.md` y `API-ARCHITECTURE.md`. Todo lo marcado **HECHO**
se verificó leyendo código real; ninguna ausencia se asciende a
"vulnerabilidad" sin evidencia de explotabilidad — se clasifica como
RIESGO, MEJORA o REQUIREMENT DE PRODUCCIÓN según corresponda. No se cambió
ningún código para escribir este documento.

## Trust boundaries

```
┌─────────────────────────┐   No confiable — cualquier dato que
│  Browser / Frontend      │   entra desde aquí (body, query,
│  (Cloudflare Pages)       │   headers, incluso el JWT) se trata
└───────────┬──────────────┘   como potencialmente hostil.
            │ HTTPS + JWT en header Authorization
            ▼
┌─────────────────────────┐   Primer punto de confianza condicional:
│  API (ASP.NET Core)       │   el JWT se vuelve "confiable" recién
│  CORS · AuthN · AuthZ      │   DESPUÉS de que JwtBearerHandler
└───────────┬──────────────┘   valida firma+issuer+audience+expiración.
            │
            ▼
┌─────────────────────────┐   Confiable: todo lo que llega aquí ya
│  Application (MediatR)    │   pasó autenticación/autorización de
│  Handlers, behaviors        │   ASP.NET Core — pero el ownership
└───────────┬──────────────┘   (¿es SU pedido?) se resuelve aquí, no antes.
            │
            ▼
┌─────────────────────────┐   Confiable — última autoridad para
│  Infrastructure/EF Core    │   invariantes (constraints, índices
│  PostgreSQL                │   únicos) según DATA-ARCHITECTURE.md.
└───────────┬──────────────┘
            │
            ▼
┌─────────────────────────┐   Externos: Cloudinary, Google Drive,
│  Servicios externos        │   Resend (email). Solo alcanzables
│  (Cloudinary/Drive/Resend) │   desde Admin (Media) o desde el
└─────────────────────────┘   sistema mismo (OutboxProcessor), nunca
                                directamente desde el browser.
```

**HECHO, el límite más importante de todos:** el JWT es el único artefacto
que cruza la frontera Browser→API, y **no se vuelve confiable por el mero
hecho de estar presente** — solo después de que `JwtBearerHandler` valida
`ValidateIssuer`, `ValidateAudience`, `ValidateIssuerSigningKey` contra la
clave HMAC (`Program.cs:133-141`, ya citado en
`01-Architecture/API-ARCHITECTURE.md`). Todo dato *dentro* del JWT (el
`UserId` del claim `NameIdentifier`, el rol) se trata como confiable
únicamente después de esa validación — es la base de por qué
`ICurrentUserService.UserId` es seguro de usar para ownership (ver
Autorización).

---

## Threat model

### Actores

**HECHO/INFERENCIA razonada** a partir de los roles y endpoints reales
(no hay un documento de threat modeling previo — esta es la primera vez
que se enumeran estos actores para este sistema):

| Actor | Qué puede hacer hoy |
|---|---|
| Visitante anónimo | Navegar catálogo, registrarse, iniciar sesión, suscribirse a newsletter |
| Customer | Todo lo anterior + carrito, checkout, sus propios pedidos/wishlist/loyalty, crear reseñas |
| **Customer malicioso** | Mismo acceso de API que un Customer legítimo — la pregunta de threat model es qué puede hacer *con* ese acceso (ver Amenazas) |
| Admin | Todo lo de Customer (ver la nota de `API-ARCHITECTURE.md`: Admin no está excluido de las rutas de Customer) + administración completa |
| Atacante externo sin cuenta | Superficie pública: registro, login, catálogo — vector principal: fuerza bruta de login/registro, manipulación de requests |
| Proceso/servicio interno comprometido | No aplica hoy de forma diferenciada — no hay separación de identidad entre "la API como llamante de PostgreSQL" y ningún otro proceso; un solo connection string con los mismos privilegios para toda operación |

### Amenazas relevantes al negocio, con su control real (no hipotético)

| Amenaza | ¿Existe un control? | Evidencia |
|---|---|---|
| Acceso a pedidos de otro usuario | Sí — ownership en Application | `GetMyOrdersQueryHandler.cs:34`, `.Where(x => x.UserId == _currentUser.UserId.Value)` |
| Manipulación de precios (pagar menos de lo que corresponde) | Parcial — ver RIESGO ya documentado en `DATA-ARCHITECTURE.md` | El precio final (`OrderItem.UnitPrice`) viene de `ShoppingCartItem.UnitPrice`, que el servidor fija desde `ProductVariant.Price` en `AddCartItemCommandHandler` — **el cliente nunca envía un precio que el servidor confíe directamente**; no se encontró ningún endpoint que acepte un precio desde el request |
| Manipulación de stock | Sí — atómico a nivel de SQL | `ExecuteUpdateAsync` con condición `WHERE Stock >= quantity` (`InventoryService.cs`, ver `DATA-ARCHITECTURE.md`) |
| Checkout duplicado / doble cobro | Sí — reclamo temprano + transacción | `CheckoutIdempotencyReproductionTests.cs`, ver `API-ARCHITECTURE.md` → Idempotencia |
| Registro concurrente (mismo email) | Sí — constraint único + captura de excepción | `IdentityService.RegisterAsync`, `PostgresException.SqlState == UniqueViolation` → `ConflictException` → 409 |
| Abuso de login (fuerza bruta) | Parcial — lockout de cuenta, sin rate limiting de IP | Ver Authentication y Abuse/Rate limiting abajo |
| Escalamiento de privilegios (Customer → Admin) | Sí, a nivel de asignación de rol — **no verificado si existe algún endpoint que permita a un usuario auto-asignarse el rol Admin** | `RegisterCommandHandler`/`IdentityService.RegisterAsync` asigna el rol `"Customer"` de forma fija (`IdentityService.cs:88`, ya citado en sesiones previas); no se encontró ningún parámetro de request que acepte un rol al registrarse |
| Exposición de información administrativa (dashboard, inventario) | Sí — `[Authorize(Roles="Admin")]` en cada controller/acción relevante | Matriz completa en `API-ARCHITECTURE.md` |
| Manipulación de identificadores (IDOR — cambiar un GUID en la URL para ver el recurso de otro usuario) | Sí para pedidos/carrito/wishlist propios (ownership en handler); **no evaluado para todos los 45 endpoints individualmente en esta pasada** — ver Matriz de controles para lo que sí se verificó | `GetOrderByIdQueryHandler` (no se leyó línea por línea en esta pasada si valida ownership además de existencia — **RIESGO a confirmar**, ver abajo) |
| Replay de operaciones (reenviar el mismo request firmado) | Parcial — el JWT no tiene protección anti-replay (no hay nonce/jti trackeado); la idempotencia de checkout protege ese caso específico por diseño de dominio, no por un mecanismo genérico anti-replay | Ver JWT lifecycle |

**RIESGO no confirmado, marcado explícitamente para no fingir certeza que
no se verificó:** no se leyó `GetOrderByIdQueryHandler.cs` línea por línea
en esta pasada para confirmar si, además de buscar la orden por `Id`,
también filtra por `UserId` del usuario actual (protección IDOR real) o
solo por `Id` (lo cual permitiría a un Customer autenticado ver el pedido
de **cualquier** usuario cambiando el GUID en la URL, ya que
`GET /api/orders/{id}` solo requiere `[Authorize]`, sin rol). Esto es una
pregunta de seguridad de alta importancia que quedó fuera del alcance
verificado de `API-ARCHITECTURE.md` (que solo confirmó el atributo de
autorización, no la lógica interna del handler) y debe verificarse antes de
cerrar este documento como definitivo — se dejará como acción de
seguimiento explícita en la sección de Riesgos.

---

## Authentication

**HECHO**, `Program.cs:63-76,116-150` y `JwtTokenGenerator.cs` (ya citado en
`01-Architecture/SYSTEM-ARCHITECTURE.md`):

- **Password policy** (ASP.NET Identity, `Program.cs:66-72`): longitud
  mínima 8, requiere dígito, mayúscula, minúscula; **no requiere carácter
  no alfanumérico** (`RequireNonAlphanumeric = false`); `RequireUniqueEmail
  = true`.
- **Lockout**: **no hay ninguna configuración explícita de
  `options.Lockout`** en `Program.cs` — se confirmó por ausencia. Esto
  significa que rigen los valores **por defecto de ASP.NET Identity**:
  `MaxFailedAccessAttempts = 5`, `DefaultLockoutTimeSpan = 5 minutos`,
  `AllowedForNewUsers = true`. Es HECHO por comportamiento documentado del
  framework, no una lectura literal de un valor configurado en este
  código — si alguien cambia la versión de Identity o sus defaults, este
  comportamiento cambiaría silenciosamente.
- **Qué pasa después de 5 intentos fallidos:** la cuenta queda bloqueada
  por `LockoutEnd` (ver ADR-004) — el mecanismo que preserva ese efecto
  incluso ante un login fallido está descrito en detalle en ADR-004 y
  probado en `LoginLockoutReproductionTests.cs`.
- **JWT — claims emitidos** (`JwtTokenGenerator.cs`, ya citado):
  `ClaimTypes.NameIdentifier` (UserId), `ClaimTypes.Email`,
  `ClaimTypes.Name` (nombre completo), y uno o más `ClaimTypes.Role`.
  **No se incluye ningún claim de expiración de sesión "deslizante" ni
  `jti` (JWT ID) rastreado** — no se encontró código que registre o
  verifique `jti` contra una lista de tokens revocados.
- **Expiración**: `Jwt:DurationInMinutes = 120` (`appsettings.json`) — 2
  horas fijas desde la emisión, sin renovación automática (ver JWT
  lifecycle).
- **Diferencia autenticación/autorización, aplicada a este sistema:**
  autenticación = "¿este JWT es válido y quién dice ser?" (resuelto por
  `JwtBearerHandler` antes de que la request llegue a cualquier
  controller); autorización = dos preguntas separadas, "¿tiene el rol
  requerido?" (`[Authorize(Roles=...)]`, resuelto por el middleware de
  autorización) y "¿es dueño de este recurso específico?" (resuelto **más
  adentro**, en el handler de Application, no en el middleware — ver
  siguiente sección).

---

## Authorization: dos mecanismos que no deben confundirse

**HECHO, distinción arquitectónica central de este documento:**

### 1. Role-based authorization

`[Authorize(Roles = "Admin")]` — resuelto por el middleware de
autorización de ASP.NET Core, **antes** de que cualquier código de
Application se ejecute. Es un control de **capa API**, binario, sin
matices: o el JWT trae el claim de rol requerido, o no.

### 2. Resource ownership

`Order.UserId == _currentUser.UserId` — resuelto **dentro del handler**,
en la capa de **Application**, usando `ICurrentUserService.UserId` (que a
su vez lee un claim ya validado, no un parámetro del cliente). **Esto NO
es un mecanismo de seguridad de PostgreSQL** — no hay row-level security,
no hay una política de base de datos que lo respalde (ya documentado como
RIESGO en `DATA-ARCHITECTURE.md`, repetido aquí porque es exactamente el
tipo de hallazgo que pertenece a un documento de seguridad).

**Por qué la distinción importa:** un desarrollador nuevo podría asumir
que, como el JWT ya fue validado y el rol ya fue autorizado, la seguridad
"ya está resuelta" para ese endpoint — pero para cualquier endpoint que
opera sobre un recurso identificado por `{id}` en la ruta (`GET
/api/orders/{id}`, por ejemplo), la autorización de rol por sí sola
**no** es suficiente: falta la pregunta de ownership, que vive en un lugar
completamente distinto del código (el handler, no el atributo del
controller) y que — como se señaló en Threat Model — no se verificó de
forma exhaustiva en esta pasada para todos los endpoints.

---

## Matriz de controles

**HECHO — solo controles realmente demostrados con evidencia citable**, no
aspiracionales:

| Amenaza | Control actual | Capa | Evidencia | Riesgo residual |
|---|---|---|---|---|
| Customer accede a pedido ajeno vía "mis pedidos" | Filtro de ownership por `UserId` | Application | `GetMyOrdersQueryHandler.cs:34` | Bajo |
| Customer accede a pedido ajeno vía `GET /orders/{id}` con GUID ajeno | **No verificado en esta pasada** | — | — | **Desconocido — ver Threat Model** |
| Customer/anónimo accede a Inventory/Dashboard/Admin orders | Rol `Admin` requerido | API (middleware) | Matriz de `API-ARCHITECTURE.md` | Bajo |
| Checkout duplicado / doble cobro | Reclamo temprano de carrito + transacción ambiental + captura de `DbUpdateConcurrencyException` | Application + DB | `CheckoutIdempotencyReproductionTests.cs` (3 tests, incluyendo concurrencia real con `Task.WhenAll`) | Bajo |
| Registro concurrente con el mismo email | Índice único de Identity + captura de `PostgresException` (`UniqueViolation`) → `ConflictException` | DB + Application | Ver ADR (Puntos 1–5) y `EmailUniquenessConcurrencyTests` | Bajo |
| Login por fuerza bruta | Lockout de cuenta tras 5 intentos (default de Identity) | Identity | `LoginLockoutReproductionTests.cs`, ADR-004 | **Medio** — el lockout es por cuenta, no hay limitación por IP/dispositivo (ver Abuse/Rate limiting) |
| Manipulación de stock (vender más de lo disponible) | `ExecuteUpdateAsync` atómico condicionado | DB | `InventoryService.cs`, `DATA-ARCHITECTURE.md` | Bajo |
| Manipulación de precio enviado por el cliente | El servidor nunca lee un precio del request — lo fija desde `ProductVariant.Price` | Application | `AddCartItemCommandHandler.cs:101,110` | Bajo, condicionado al RIESGO ya documentado de que el precio del carrito puede desincronizarse del catálogo (ver `DATA-ARCHITECTURE.md`) |
| Fuga de stack trace / detalle interno en error 500 | `GlobalExceptionHandler` reemplaza el detalle por un mensaje genérico solo para 500 | API | `GlobalExceptionHandler.cs:47-49` | Bajo |
| Un endpoint nuevo queda público sin que nadie lo decida a propósito | **Ninguno** — ver sección final de este documento | — | Ausencia confirmada de `FallbackPolicy` | **Alto, ver cierre del documento — no se corrige aquí** |

---

## Seguridad de datos — clasificación

**HECHO/INFERENCIA razonada**, sin un documento de clasificación de datos
previo — esta es la primera vez que se declara explícitamente:

| Dato | Sensibilidad | ¿Aparece en logs/errores/respuestas indebidamente? |
|---|---|---|
| Contraseña (texto plano en el request) | Crítica | No se encontró — `LoggingBehavior` solo loguea `typeof(TRequest).Name`, nunca el payload (`LoggingBehavior.cs:21,25`); ASP.NET Identity hashea antes de persistir |
| JWT | Crítica (equivale a la sesión completa) | No se loguea explícitamente; viaja solo en el header `Authorization`, nunca en query string (no se encontró ningún endpoint que lo acepte por query) |
| Email | Media (PII) | Aparece en logs de `IdentityService`/`AuthController` en mensajes de auditoría de intentos (`_logger?.LogWarning("Login failed for {Email}...")`, ya citado en sesiones previas) — **DECISIÓN aceptable para logs internos de servidor, pero confirma que el email SÍ queda en los logs de aplicación**, algo a tener en cuenta si esos logs se exportan a un tercero |
| Información de pedidos (dirección de envío, teléfono) | Media-Alta (PII) | Expuesta solo a su dueño (ownership) y a Admin — sin hallazgo de fuga |
| Precios/stock | Baja (información de negocio, no de identidad) | Pública por diseño (catálogo) |
| Información administrativa (dashboard, ventas totales) | Alta (confidencialidad de negocio) | Protegida por rol `Admin` |
| `TransactionId` de pago (`"SIM-..."` hoy) | **Se volverá crítica el día que exista una pasarela real** | Hoy inocuo porque es un simulador (ADR-007); expuesto en `OrderDto` a su dueño y a Admin — **REQUIREMENT DE PRODUCCIÓN**: cuando se integre un proveedor real, revisar si el ID de transacción del proveedor debe ocultarse parcialmente en las respuestas al cliente |

**Qué debería quedar fuera de logs/respuestas/errores, ya verificado que
cumple:** contraseñas y JWT no aparecen en ningún log encontrado; el 500
nunca expone detalle interno. **Qué no cumple del todo:** el email
aparece en logs de aplicación (aceptable para logs internos, pero es una
decisión que vale la pena que quede explícita en vez de implícita).

---

## Secrets & configuration

**HECHO, verificado con evidencia concreta de qué está y qué no está
versionado:**

- `backend/src/BeautyCommerce.API/appsettings.json` (committed): `Jwt:Key`
  es **string vacío** — no hay secreto real versionado ahí. Igual para
  `GoogleDrive:ServiceAccountJson`, `Cloudinary:ApiKey`/`ApiSecret`,
  `Email:ApiKey` (todos vacíos, confirmado en sesiones previas de este
  mismo proyecto de documentación).
- `appsettings.Development.json` (committed): **no tiene sección `Jwt` en
  absoluto**, y el connection string no lleva contraseña
  (`Username=postgres;` sin `Password=`) — consistente con autenticación
  local `trust`/`peer` de PostgreSQL en desarrollo, no una credencial
  filtrada.
- El proyecto tiene `<UserSecretsId>` configurado en
  `BeautyCommerce.API.csproj` — el mecanismo real para el `Jwt:Key` (y
  cualquier otro secreto) en desarrollo local es `dotnet user-secrets`,
  que almacena fuera del repositorio (`%APPDATA%\Microsoft\UserSecrets\`
  en Windows). **Esto es una DECISIÓN correcta de higiene de secretos**,
  no un hallazgo negativo.
- Para producción (Render): ya documentado en `docs/DEPLOYMENT.md` y
  `render.yaml` — todas las variables sensibles (`Jwt__Key`,
  `GoogleDrive__ServiceAccountJson`, `Cloudinary__*`, `Email__ApiKey`)
  están marcadas `sync: false`, el mecanismo de Render para "debe
  ingresarse manualmente en el dashboard, nunca vivir en el repositorio".

**No existe una estrategia de secrets management más allá de
variables de entorno** — no hay Key Vault, AWS Secrets Manager, ni
rotación automática de claves. **No se asume que deba existir** solo
porque no está — para el tamaño y etapa actual del proyecto (ver
`00-Governance/PROJECT-CHARTER.md`), variables de entorno + `sync: false`
en Render es una posición razonable. Se marca como **MEJORA**, no como
RIESGO ni DEFECTO.

**RIESGO real, no cosmético:** `Jwt:Key` no tiene una estrategia de
rotación documentada. Si la clave se filtrara, invalidar todos los tokens
emitidos requeriría cambiar la clave (lo cual invalida *todas* las
sesiones activas de golpe, sin gracia) — no hay un mecanismo de rotación
con periodo de convivencia entre clave vieja/nueva.

---

## Abuse / rate limiting

**HECHO, por ausencia confirmada — no se inventa una mitigación que no
existe.** No se encontró `AddRateLimiter`, `UseRateLimiter`, ni ninguna
otra forma de throttling (ni a nivel de ASP.NET Core, ni de un proxy
inverso documentado) en todo `backend/src`. **No hay protección específica
de rate limiting para `/api/auth/login`, `/api/auth/register`, ni
`/api/orders/checkout`.**

**Clasificación honesta:**

- **Login:** el lockout de cuenta (ver Authentication) mitiga la fuerza
  bruta contra **una cuenta específica ya existente**, pero no limita
  cuántos intentos de login puede hacer una IP contra **muchas** cuentas
  distintas (credential stuffing) — cada cuenta tiene su propio contador
  independiente. **RIESGO, no defecto** — no hay evidencia de que se haya
  explotado.
- **Registro:** ningún límite de cuántas cuentas puede crear la misma
  IP/dispositivo en un período — el único control es el índice único de
  email (evita duplicados, no evita spam de cuentas con emails distintos).
  **RIESGO.**
- **Checkout:** protegido contra duplicación por el mismo usuario
  (idempotencia ya documentada), pero no hay límite de cuántos intentos de
  checkout puede iniciar una cuenta en un período corto — no es lo mismo
  que abuso de fuerza bruta, pero podría usarse para saturar el sistema de
  pago simulado (o, el día de mañana, generar cargos de intento contra una
  pasarela real de pago que cobre por intento). **RIESGO, más relevante
  cuando ADR-007 se resuelva.**

---

## JWT lifecycle

**HECHO, distinguiendo explícitamente "el JWT funciona" de "existe un
lifecycle de sesión completo" — no son lo mismo:**

| Etapa | Estado |
|---|---|
| Emisión | ✅ `JwtTokenGenerator`, al login exitoso |
| Validación | ✅ `JwtBearerHandler`, en cada request autenticado |
| Expiración | ✅ 120 minutos fijos (`Jwt:DurationInMinutes`), sin renovación deslizante |
| Almacenamiento en frontend | `localStorage` (`authStore.ts:50`, `localStorage.setItem("beauty_token", token)`) — **RIESGO conocido de la industria**: un token en `localStorage` es accesible por cualquier script que corra en la página (superficie de XSS), a diferencia de una cookie `httpOnly` |
| Logout | Solo del lado del cliente — `authStore.logout()` únicamente hace `localStorage.removeItem("beauty_token")`; **no hay ninguna llamada al backend para invalidar el token** |
| Revocación | **No existe.** Un JWT robado o "cerrado sesión" en el cliente sigue siendo válido en el backend hasta que expira por tiempo — no hay lista de revocación, no hay `jti` trackeado |
| Refresh token | **Existió y fue eliminado.** Migración `20260812122556_RemoveLegacyUserRoleAndRefreshToken.cs` confirma que hubo una tabla `RefreshTokens` con FK a usuarios, eliminada deliberadamente. **DECISIÓN histórica, no accidente** — consistente con las fases de limpieza de código muerto de este proyecto (ver commits de "Barrido final de backend"), aunque la razón específica de por qué se decidió no tener refresh tokens no quedó documentada en ningún ADR — **BACKLOG de documentación**, no de código: si alguien reintroduce refresh tokens, debería quedar como ADR nuevo, no como resurrección silenciosa de la tabla eliminada |

**Conclusión explícita que el equipo pidió no omitir:** el JWT **funciona**
correctamente como mecanismo de autenticación stateless. Pero **no existe
un lifecycle de sesión completo** — sin logout server-side, sin revocación,
sin refresh, la única forma de terminar una sesión comprometida antes de
que expire por sí sola es rotar `Jwt:Key` globalmente (lo cual cierra la
sesión de *todo el mundo*, no solo la comprometida). Para la etapa actual
del proyecto (ver Release Readiness en `00-Governance/PROJECT-CHARTER.md`)
esto es un **RIESGO documentado**, no necesariamente un bloqueador — pero
es el tipo de brecha que se vuelve más costosa cuanto más tiempo pasa sin
decidirse conscientemente.

---

## Security failure behavior

**HECHO**, consolidado desde `API-ARCHITECTURE.md` con foco de seguridad:

| Código | Significa | Cuerpo de respuesta |
|---|---|---|
| `400` | Validación o regla de negocio falló | `ProblemDetails` con `detail` específico |
| `401` | No autenticado (sin JWT, o JWT inválido/expirado) | `ProblemDetails` |
| `403` | Autenticado pero sin el rol requerido | **Sin cuerpo — hallazgo del "403 vacío"**, ver `API-ARCHITECTURE.md` → Contrato de errores. Mecanismo: `JwtBearerHandler` no tiene `OnForbidden` configurado, así que el forbid nunca pasa por `GlobalExceptionHandler` |
| `404` | Recurso inexistente **o perteneciente a otro usuario, según el handler** — ambos casos pueden producir el mismo 404, lo cual es en realidad una buena práctica de seguridad (no revela si el recurso existe pero no es tuyo, vs. no existe en absoluto) — **no verificado si esto es consistente en todos los handlers o es coincidencia** | `ProblemDetails` |
| `409` | Conflicto de integridad/concurrencia (email duplicado, carrito ya procesado) | `ProblemDetails` |

**El 403 vacío es, desde la óptica de seguridad, un problema de
*usabilidad* del error, no una vulnerabilidad** — no filtra información
adicional (de hecho filtra *menos* información que los demás códigos, lo
cual accidentalmente es conservador). Se documenta aquí porque **no
tocarlo también es una decisión de seguridad razonable**: agregar detalle
a un 403 es exactamente el tipo de cambio que hay que pensar dos veces
(¿el detalle podría confirmarle a un atacante que el recurso existe pero
no tiene permiso, en vez de simplemente no tener permiso?).

---

## Auditability

**HECHO**, consolidado desde `DATA-ARCHITECTURE.md`/`API-ARCHITECTURE.md`
con foco de seguridad — qué acciones dejan rastro y cuáles no:

| Acción | ¿Deja rastro? | Dónde |
|---|---|---|
| Cambio de stock (entrada/salida) | Sí | `InventoryMovement` (inmutable, con `UserId` de quien lo originó cuando aplica) |
| Cambio de estado de una orden | Parcial — **solo el estado actual, no el historial de transiciones anteriores** (ya señalado como hallazgo en `DATA-ARCHITECTURE.md` → Matriz de fuente de verdad) | `Order.Status` |
| Creación/edición/soft-delete de producto, marca, categoría | Parcial — `CreatedBy`/`UpdatedBy`/`DeletedBy` vía `BaseEntity`, pero **sin un log de "qué campo cambió a qué valor"**, solo quién y cuándo | Columnas de auditoría de `BaseEntity` |
| Login exitoso/fallido | Sí, a nivel de log de aplicación (`_logger?.LogWarning`/`LogInformation` en `IdentityService`) — **no persistido en una tabla consultable**, solo en el log de texto de Serilog | Logs, no base de datos |
| Cambios de rol de un usuario | **No verificado si existe algún camino para cambiar el rol de un usuario después del registro** — no se encontró un endpoint `UpdateUserRole` en el inventario de 45 endpoints de `API-ARCHITECTURE.md`. Si no existe, la pregunta de auditoría es moot; si existe y no se encontró, es un hallazgo pendiente | — |
| Eventos asíncronos (email de confirmación) | Sí | `OutboxMessage.Error`/`ProcessedOn` |

**Qué operaciones sensibles no tienen auditoría explícita, dicho sin
rodeos:** no hay una tabla de auditoría genérica (`AuditLog`) que capture
"quién vio qué pedido de quién" — un Admin viendo el pedido de un cliente
no deja rastro de que lo vio, solo de que el pedido existe y fue
modificado. Para el tamaño actual del negocio esto se documenta como
**MEJORA**, no como defecto — se vuelve más relevante si el volumen de
datos de clientes crece o si hay requisitos de cumplimiento normativo
futuros (protección de datos personales en Colombia, Ley 1581 — **fuera
del alcance verificado de este documento, se señala como pregunta abierta
para `00-Governance/` si alguna vez se necesita formalizar**).

---

## Security risks / backlog — clasificación estricta

Siguiendo exactamente la instrucción de no convertir automáticamente una
ausencia en vulnerabilidad:

### 🔴 DEFECTO demostrado
Ninguno encontrado en esta pasada — todo lo identificado es ausencia de
control o comportamiento no verificado, no un comportamiento incorrecto
reproducido con evidencia. (Distinto de los defectos ya cerrados de Puntos
1–5, que viven en `06-Quality/AUDIT-PUNTOS-1-5.md` cuando se escriba.)

### 🟠 RIESGO arquitectónico
- **Ausencia de `FallbackPolicy` de autorización global** — un controller
  nuevo sin `[Authorize]` queda público por defecto. **No se corrige en
  este documento** — candidato explícito a su propio ADR (ver cierre).
- **Sin rate limiting** en login/registro/checkout.
- **JWT sin lifecycle completo de sesión** (sin logout server-side, sin
  revocación, sin refresh).
- **`Jwt:Key` sin estrategia de rotación.**
- **Ownership de `GET /api/orders/{id}` no verificado línea por línea en
  esta pasada** — posible IDOR, marcado explícitamente como pregunta
  abierta, no como hallazgo confirmado ni descartado.
- **Sin auditoría de acceso de lectura** (quién vio qué) para datos de
  clientes por parte de Admin.

### 🟡 MEJORA recomendada
- Formalizar por qué se eliminaron los refresh tokens (ADR retroactivo) en
  vez de dejar la decisión solo en el mensaje de una migración.
- Considerar `httpOnly` cookie en vez de `localStorage` para el JWT, si el
  riesgo de XSS se considera relevante para este producto.
- Tabla de auditoría genérica para operaciones administrativas sensibles.

### 🔵 REQUIREMENT DE PRODUCCIÓN
- **Revisar la exposición del `TransactionId` de pago cuando `ADR-007`
  (pasarela simulada) se reemplace por una integración real** — hoy es
  inocuo porque es un valor `SIM-...` sin significado externo.
- Rate limiting específico para checkout, antes de que exista una pasarela
  real que pueda cobrar comisión por intento.

---

## Cierre: el hallazgo de secure-by-default queda documentado, no corregido

**Tal como se pidió explícitamente:** la ausencia de una política de
autorización global (`FallbackPolicy`) — que hace que cualquier controller
nuevo sin `[Authorize]` explícito quede público por defecto — se deja
documentada aquí como **RIESGO arquitectónico de secure-by-default**, sin
tocar código. La decisión de si se pasa a un modelo donde los endpoints son
**privados por defecto** y `[AllowAnonymous]` se vuelve la excepción
explícita (en vez de al revés, como es hoy) es una decisión arquitectónica
que le corresponde a su propio ADR — no un parche que se aplica de paso
mientras se documenta seguridad.

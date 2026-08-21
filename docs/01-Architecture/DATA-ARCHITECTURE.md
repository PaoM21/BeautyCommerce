# Data Architecture — BeautyCommerce (Haldy&Co)

Este documento fija **propiedad y ciclo de vida del dato**, no solo su
forma. El objetivo es que ninguna feature nueva tenga que adivinar quién es
dueño de un dato, cuándo se congela, o qué pasa cuando su origen se borra.
Todo lo marcado **HECHO** se verificó leyendo el handler o la configuración
EF citada — no se infiere de la forma de las tablas. Ninguna afirmación de
este documento resultó en un cambio de código: donde se encontró algo
discutible, se documenta como **RIESGO**, no se corrige.

## Mapa de entidades

### Catálogo

| Entidad | Propietario | Propósito | Relaciones | Cardinalidad | Restricciones/Índices | Soft delete | Fuente de verdad | Consumidores |
|---|---|---|---|---|---|---|---|---|
| `Brand` | Admin (vía `BrandsController`) | Marca comercial | `Product` | 1:N | Sin índice único en `Name` | Sí (global) | Sí misma | Catálogo público, admin |
| `Category` | Admin | Categoría, con jerarquía propia | `Product`; self-FK `ParentCategory`/`Children` | 1:N; self 1:N | FK padre `Restrict`; **`Slug` sin índice único** | Sí (global) | Sí misma | Catálogo público (filtro `/productos?categoria=`), admin |
| `Product` | Admin | Ficha de producto | `Brand`, `Category` (N:1, requeridas, `Restrict`); `Images`, `Variants`, `WishlistItems` (1:N) | ver arriba | **`Slug` sin índice único** | Sí (global); **no cascada a hijos** (ver sección Soft Delete) | Sí misma (excepto precio, ver Modelo de precios) | Ficha pública, listados, admin |
| `ProductVariant` | Admin (creación); **nadie después** — ver Modelo de precios | Unidad real de venta: SKU, precio, stock | `Product` (N:1, `Cascade` en hard-delete); `OrderItem`, `ShoppingCartItem`, `InventoryMovement` (1:N) | 1 producto : N variantes | `SKU` y `Barcode` **únicos**; `Stock/Price/Cost/OldPrice/MinimumStock ≥ 0` (check constraints) | Sí (global); no cascada desde `Product` | Sí misma para `Stock`/`Price` actuales | Carrito, checkout, inventario, `OrderItem` (snapshot) |
| `ProductImage` | Admin / sync Drive→Cloudinary | Imagen de producto | `Product` (N:1, `Cascade`) | 1 producto : N imágenes | Sin índice único; `DisplayOrder` default `1` | Sí (global); no cascada desde `Product` | Sí misma | Ficha pública, admin |

### Comercio

| Entidad | Propietario | Propósito | Relaciones | Cardinalidad | Restricciones/Índices | Soft delete | Fuente de verdad | Consumidores |
|---|---|---|---|---|---|---|---|---|
| `ShoppingCart` | Cliente (por `UserId`) | Carrito activo | `Items` (1:N) | 1 por usuario | **Índice único en `UserId`** | Sí (global) | Sí misma | Checkout, UI de carrito |
| `ShoppingCartItem` | Cliente | Línea de carrito, precio "vivo" | `ShoppingCart` (N:1); `ProductVariant` (N:1, `Restrict`) | N por carrito | `Quantity > 0`, `UnitPrice ≥ 0` | Sí (global) | **No** — `UnitPrice` es una copia refrescada de `ProductVariant.Price`, ver Modelo de precios | Checkout (fuente del `OrderItem.UnitPrice`) |
| `Order` | Sistema (creado por checkout), propiedad del cliente vía `UserId` | Registro histórico de una compra | `Items` (1:N) | 1 por checkout | `Total ≥ 0`, `SubTotal ≥ 0` (check constraints); **`ShippingCost`/`Tax` sin check constraint** | Sí (global) | Sí misma | Cuenta de cliente, admin de pedidos, loyalty |
| `OrderItem` | Sistema (snapshot al checkout) | Línea histórica inmutable de una compra | `Order` (N:1, `Cascade`); `ProductVariant` (N:1, `Restrict`) | N por orden | `Quantity > 0`, `UnitPrice ≥ 0` | Sí (global) | **Sí, es el snapshot final** — ver Modelo de precios | Detalle de pedido (cliente y admin), reportes de ventas, dashboard |
| `InventoryMovement` | Sistema (`InventoryService`) | Historial inmutable de cambios de stock | `ProductVariant` (N:1, requerida) | N por variante | `Quantity > 0` (el signo lo da `IsEntry`) | Sí (global, hereda `BaseEntity`) | Sí misma (histórico) | Sin UI de consulta hoy (ver `00-Governance/SCOPE.md` → Backlog) |

### Engagement de cliente

| Entidad | Propietario | Propósito | Relaciones | Cardinalidad | Restricciones/Índices | Soft delete | Fuente de verdad | Consumidores |
|---|---|---|---|---|---|---|---|---|
| `Review` | Cliente (por `UserId`) | Reseña de producto | `Product` (N:1, `Cascade`); `Order?` (N:1 opcional, `SetNull`) | N por producto | **Índice único compuesto** `(UserId, ProductId, OrderId)` | Sí (global) | Sí misma | Ficha de producto, reseñas destacadas en Home |
| `Wishlist` | Cliente (por `UserId`) | Lista de favoritos (contenedor) | `Items` declarado en clase, **sin FK real** — ver sección Wishlist | 1 por usuario (no forzado por índice) | **Sin índice único en `UserId`** | Sí (global) | Ambigua — ver sección Wishlist | Página `/favoritos` |
| `WishlistItem` | Cliente | Ítem individual de favoritos | `Product` (N:1); **sin relación a `Wishlist`** | N por usuario/producto | **Índice único compuesto** `(UserId, ProductId)` | Sí (global) | Sí misma | Página `/favoritos`, botón wishlist (falta en `ProductDetail`) |
| `LoyaltyAccount` | Cliente (por `UserId`) | Puntos y nivel de fidelización | `Transactions` (1:N) | 1 por usuario | **Índice único en `UserId`** | Sí (global) | Sí misma para `Points`/`Level` actuales | `/mi-cuenta/rewards` |
| `LoyaltyTransaction` | Sistema (`LoyaltyService`, al entregar una orden) | Historial de movimientos de puntos | `LoyaltyAccount` (N:1, `Cascade`); `Order?` (N:1 opcional, `SetNull`) | N por cuenta | — | Sí (global) | Sí misma (histórico) | `/mi-cuenta/rewards` |
| `NewsletterSubscriber` | Visitante/cliente | Suscripción a newsletter | Ninguna | — | **Sin archivo de configuración EF — unicidad de `Email` no confirmada** | Sí (global, hereda `BaseEntity`) | Sí misma | `NewsletterController` |

### Plataforma

| Entidad | Propietario | Propósito | Relaciones | Cardinalidad | Restricciones/Índices | Soft delete | Fuente de verdad | Consumidores |
|---|---|---|---|---|---|---|---|---|
| `OutboxMessage` | Sistema | Cola de eventos para efectos asíncronos (email) | Ninguna | — | Sin configuración EF propia; **no hereda `BaseEntity`** — sin soft delete ni auditoría | No aplica | Sí misma | `OutboxProcessor` |

---

## Modelo de precios — sección crítica

**HECHO, verificado en tres handlers distintos.** No existe un campo
`Product.Price` — `Product.cs` no tiene ninguna propiedad de precio. El
"precio" que se muestra en un listado de catálogo es un valor **derivado**,
no almacenado: `GetProductsQueryHandler` lo calcula como
`Variants.Min(v => v.Price)` en el momento de la consulta. El precio real
vive únicamente en `ProductVariant.Price`.

### La cadena real de copiado

```
ProductVariant.Price                         ← única fuente de verdad del
   (mutable, sin historial propio)               precio "actual"
        │
        │  AddCartItemCommandHandler.cs:101,110
        │  se copia (y se REFRESCA en cada nuevo add-to-cart
        │  del mismo ítem, mientras el carrito siga vivo)
        ▼
ShoppingCartItem.UnitPrice                   ← snapshot "vivo", no histórico
   (se sobrescribe con el precio actual            todavía — puede cambiar
    cada vez que el usuario agrega más              varias veces antes del
    unidades del mismo ítem al carrito)              checkout
        │
        │  CheckoutCommandHandler.cs:186
        │  se copia UNA SOLA VEZ, al crear la orden
        ▼
OrderItem.UnitPrice                          ← snapshot final, inmutable
   (nunca se vuelve a escribir por ningún           para siempre — es el
    comando del sistema; ninguna migración           precio histórico real
    ni handler lo actualiza después de creado)        de esa compra
```

### Quién puede modificar cada uno

- **`ProductVariant.Price`**: se establece en `CreateProductCommandHandler`
  al crear el producto con sus variantes. **HECHO, verificado por ausencia**:
  no existe ningún comando `UpdateVariant*` ni referencia a `Price` en
  `UpdateProductCommandHandler.cs` — hoy **no hay ningún camino en el
  backend**, ni con UI ni sin ella, para modificar el precio de una
  variante después de creada. Esto es más fuerte que el hallazgo ya
  registrado en `00-Governance/SCOPE.md` ("falta UI para editar
  variantes") — no es que falte la UI sobre una capacidad existente, es que
  la capacidad de edición no existe en absoluto en la capa de aplicación.
- **`ShoppingCartItem.UnitPrice`**: solo el sistema lo escribe, y solo en
  `AddCartItemCommandHandler`, siempre igualándolo al `ProductVariant.Price`
  vigente en ese momento — el cliente nunca lo fija directamente.
- **`OrderItem.UnitPrice`**: solo `CheckoutCommandHandler` lo escribe, una
  única vez, al crear la orden.

### Qué dato usar para cada pregunta de negocio

| Pregunta | Dato correcto |
|---|---|
| "¿Cuánto cuesta este producto hoy?" | `ProductVariant.Price` (vía el mínimo entre variantes para el listado) |
| "¿Cuánto pagó el cliente en esta orden?" | `OrderItem.UnitPrice` — **nunca** `ProductVariant.Price` actual, que puede haber cambiado o ni siquiera existir si la variante fue soft-deleted después |
| "¿Cuánto vale el carrito ahora mismo?" | `ShoppingCartItem.UnitPrice`, sabiendo que se resincroniza en cada add-to-cart pero **no se refresca automáticamente si el precio del catálogo cambia mientras el ítem simplemente permanece en el carrito sin que el usuario vuelva a agregarlo** — ver Riesgo abajo |

**RIESGO, no defecto confirmado:** si el precio de una variante cambia
mientras un ítem ya está en el carrito de un cliente (sin que ese cliente
vuelva a tocar "agregar"), `ShoppingCartItem.UnitPrice` queda desactualizado
hasta el checkout, momento en el cual `CheckoutCommandHandler` usa
`item.UnitPrice` (el valor potencialmente viejo del carrito), **no**
`ProductVariant.Price` vigente (`CheckoutCommandHandler.cs:108-109,186`) —
confirmado leyendo el handler línea por línea. Esto significa que un
cliente puede pagar un precio distinto al que el catálogo muestra en ese
instante, en cualquier dirección (a su favor o en contra). No hay evidencia
de que esto se haya reproducido como problema; se documenta como riesgo de
diseño porque el mecanismo que lo permitiría existe y es determinista.

---

## Inventario

**HECHO**, verificado en `InventoryService.cs`. `ProductVariant.Stock` es
el **estado actual**, mutado exclusivamente a través de dos operaciones
atómicas de una sola sentencia SQL (`ExecuteUpdateAsync`, no
lectura-luego-escritura):

- **Entrada** (`RegisterEntryAsync`): `Stock = Stock + quantity`,
  incondicional.
- **Salida** (`TryRegisterExitAsync`): `Stock = Stock - quantity`
  **con la condición `Stock >= quantity` en la misma sentencia SQL** — si
  dos checkouts concurrentes compiten por el último ítem, la base de datos
  resuelve la carrera de forma atómica; el segundo `ExecuteUpdateAsync`
  afecta 0 filas y el método devuelve `false` en vez de dejar el stock
  negativo. **Esto es una DECISIÓN de diseño defendible, no un accidente**:
  el mismo patrón "la base de datos es la autoridad final" que ya se
  estableció en `01-Architecture/DOMAIN-MODEL.md` y en el ADR-004 (registro
  concurrente) se repite aquí para stock.

Cada entrada o salida exitosa crea un `InventoryMovement` — el **historial
inmutable**, nunca actualizado ni borrado por ningún comando encontrado.
`IsEntry` distingue el sentido; `Reason` es texto libre que hoy documenta el
origen (`"Salida por pedido {orderNumber}"`,
`"Restitución por cancelación del pedido {orderNumber}"`).

**Relación con cancelaciones (HECHO):**
`UpdateOrderStatusCommandHandler.cs:58-70` solo restituye stock en la
transición exacta `Paid → Cancelled`, iterando cada `OrderItem` de la orden
y llamando `RegisterEntryAsync` (nunca acepta que la salida no exista —
asume que si la orden estaba `Paid`, el stock ya se descontó). Ninguna otra
transición del ciclo de vida de la orden toca inventario.

**Fuente de verdad:** `ProductVariant.Stock` para "cuánto hay ahora";
`InventoryMovement` para "qué pasó" — son complementarios, no
intercambiables. No hay ningún mecanismo que recalcule `Stock` sumando el
historial de movimientos (si alguna vez se desincronizan, no hay
reconciliación automática — **BACKLOG**, no implementado).

---

## Pedidos

**HECHO.** `Order` + `OrderItem` son el registro histórico de una compra.
`OrderItem` no tiene relación con `Product` directamente, solo con
`ProductVariant` — el nombre/marca/imagen del producto que se muestra en un
historial de pedido siempre se resuelve **en el momento de la consulta**
navegando `OrderItem → ProductVariant → Product`, no desde una copia
guardada en `OrderItem` (a diferencia del precio, que sí es una copia).

### Qué pasa cuando el producto se elimina (soft delete) — RIESGO real, no reproducido

**HECHO, mecánica verificada; RIESGO, no confirmado con una reproducción
en ejecución.** `GetOrderByIdQueryHandler.cs` y
`GetMyOrdersQueryHandler.cs` proyectan `item.ProductVariant.Product.Name`
(y `Color`, `Size`, `Images`) sin usar `IgnoreQueryFilters()`. El filtro
global de soft delete (`e => !e.IsDeleted`) de EF Core se aplica también a
las entidades cargadas por navegación (`Include`/`ThenInclude`), no solo a
la raíz de la consulta. Consecuencia razonada: si el `Product` o el
`ProductVariant` de una línea de un pedido histórico se soft-elimina
después de la compra, esa línea del historial **puede dejar de mostrar el
nombre/imagen del producto** para el cliente o el admin — el pedido en sí
sigue existiendo (`Order`/`OrderItem` no se tocan), pero la información
descriptiva que depende de la navegación queda invisible detrás del filtro.
El precio (`OrderItem.UnitPrice`) **no se ve afectado**, porque es una
copia, no una navegación. No hay un test que reproduzca este escenario hoy;
se documenta como RIESGO porque el mecanismo que lo produciría es real y
verificable en el código, no una especulación sin fundamento.

### Qué información histórica sobrevive

- **Sobrevive siempre:** `OrderItem.UnitPrice`, `Quantity`, `Order.Total` y
  el resto de campos propios de `Order` (son copias o valores propios, no
  navegaciones).
- **Puede dejar de resolverse** si el producto/variante de origen se
  soft-elimina: nombre, marca, imagen, color, talla — todo lo que
  `GetOrderById`/`GetMyOrders` obtiene navegando en vivo.

---

## Identidad

**HECHO**, consistente con lo ya documentado en
`01-Architecture/DOMAIN-MODEL.md`: ningún `UserId` en el dominio (`Order`,
`Review`, `WishlistItem`, `ShoppingCart`, `LoyaltyAccount`,
`InventoryMovement`) tiene FK real hacia el usuario — es un `Guid` suelto,
por diseño de Clean Architecture (Identity vive en Infrastructure).

**Aislamiento por usuario (ownership):** verificado en
`GetMyOrdersQueryHandler.cs:34` — el filtro es
`.Where(x => x.UserId == _currentUser.UserId.Value)`, aplicado **en la capa
de aplicación**, no en la base de datos. **RIESGO documentado, no
defecto:** no existe ningún mecanismo de row-level security en PostgreSQL
ni un query filter global por `UserId` — a diferencia del soft delete (que
sí tiene un backstop automático a nivel de `DbContext`), la propiedad de
los datos de un usuario depende enteramente de que cada handler nuevo
recuerde escribir ese `.Where(...)`. Un handler futuro que olvide ese
filtro no tiene ninguna red de seguridad adicional que lo detenga.

**Lockout:** ver ADR-004 — el efecto de bloqueo de cuenta lo persiste
ASP.NET Identity internamente (`AccessFailedCount`, `LockoutEnd`), fuera
del dominio de esta aplicación; `LoginCommand` está excluido de la
transacción ambiental precisamente para que ese efecto sobreviva.

**Wishlist y Loyalty** ya están cubiertos en el Mapa de entidades — ambos
identifican al dueño por `UserId` suelto, mismo patrón.

---

## Wishlist: el hallazgo de la FK faltante

**HECHO, clasificado por evidencia, no como defecto automático.**
`WishlistItem` no tiene ninguna propiedad `WishlistId`, y no existe ninguna
configuración EF que relacione `WishlistItem` con `Wishlist` mediante FK.
La navegación `Wishlist.Items: ICollection<WishlistItem>` existe en la
clase C#, pero no está respaldada por una relación real en el esquema.

**Consecuencias arquitectónicas concretas:**

- A efectos de base de datos, `Wishlist` y `WishlistItem` son dos conceptos
  **independientes** que comparten `UserId`, no una relación padre/hijo
  real como sí lo es `ShoppingCart`↔`ShoppingCartItem`.
- `Wishlist.Items` en código C# probablemente nunca se popula vía EF Core
  (no hay FK que EF pueda usar para materializar esa colección) — si algo
  en el código intenta usar esa navegación esperando que traiga los ítems
  del usuario, no funcionará como el nombre sugiere. **No se verificó si
  algo en el código realmente intenta usar `Wishlist.Items`** — queda como
  pregunta abierta, no como hallazgo confirmado.
- La entidad `Wishlist` en sí, sin la relación, funciona hoy como un
  contenedor casi vacío (`UserId` + la colección no funcional); toda la
  funcionalidad real de favoritos parece sostenerse sobre `WishlistItem`
  consultado directamente por `UserId`, sin pasar por `Wishlist` en
  absoluto — **INFERENCIA**, no se leyó el query handler de wishlist línea
  por línea en esta pasada para confirmarlo con certeza total.

**Clasificación:** RIESGO de modelado, no defecto activo — no hay evidencia
de que rompa la experiencia de usuario hoy. Antes de construir más
funcionalidad sobre wishlist (por ejemplo, colecciones múltiples o
wishlist compartida), esta relación debe resolverse conscientemente: o se
agrega la FK real, o se elimina `Wishlist` como concepto y se admite que
`WishlistItem` + `UserId` es todo el modelo real.

---

## Slugs e índices

**HECHO.** `Product.Slug` y `Category.Slug` no tienen índice único en su
configuración EF Core. Ambos son consumidos como identificador funcional
público:

- `Category.Slug` — usado en el filtro de categoría
  `/productos?categoria=<slug>` (`cabello`, `rostro`, `piel`, `labios`,
  `unas`), URL indexable para SEO.
- `Product.Slug` — generado al crear el producto, pensado como identidor
  legible de la ficha de producto para SEO (aunque las rutas actuales de
  `AppRoutes.tsx` usan `/productos/:id` con el GUID, no el slug — **a
  confirmar si el slug ya se usa en alguna URL pública real o si es
  preparación para una migración futura de ruteo por slug**, no verificado
  en esta pasada).

**RIESGO, no defecto reproducido:** nada en la base de datos impide crear
dos categorías o dos productos con el mismo slug. Si eso ocurriera, el
filtro `/productos?categoria=cabello` (que probablemente resuelve el slug a
un `CategoryId` antes de filtrar) tendría que decidir arbitrariamente cuál
de las dos categorías duplicadas usar, o fallar. No hay evidencia de que
esto haya ocurrido — el catálogo actual es de datos de prueba controlados
(ver `00-Governance/PROJECT-CHARTER.md`). El riesgo crece en proporción a
qué tan público y editable sea el proceso de creación de categorías/
productos con slugs no auto-generados de forma determinista.

**Pregunta arquitectónica pendiente (no resuelta en este documento, a
propósito):** ¿el slug es identidad funcional del recurso (como una URL
canónica) o solo un atributo de presentación? Esa pregunta determina si la
solución es un `UNIQUE` simple, o si además se necesita una estrategia para
cuándo un slug puede cambiar sin romper enlaces ya indexados por buscadores
— eso pertenece a la intersección de `01-Architecture/API-ARCHITECTURE.md`
y una futura pieza de UX/SEO, no se decide aquí.

---

## Soft delete: cómo se comporta realmente en cascada

**HECHO, verificado en `DeleteProductCommandHandler.cs`.** Borrar un
producto solo modifica el propio registro `Product`
(`IsActive = false`, `IsDeleted = true`, `DeletedAt`, `DeletedBy`). **No
hay ninguna cascada de aplicación que también marque `IsDeleted = true` en
sus `ProductVariant` ni `ProductImage` asociados.** El filtro global de EF
Core hace que esos hijos **parezcan** invisibles cuando se navega desde
`Product` (porque el `Product` padre ya no aparece en una consulta normal),
pero si algo consulta `ProductVariants` o `ProductImages` directamente sin
pasar por `Product`, esos registros siguen teniendo `IsDeleted = false` — 
técnicamente "activos" en su propia tabla.

| Entidad | ¿Se soft-elimina en cascada al borrar su padre? |
|---|---|
| `Product` → `ProductVariant` | **No** — el padre se marca eliminado, los hijos no. |
| `Product` → `ProductImage` | **No**, mismo patrón. |
| `ShoppingCart` → `ShoppingCartItem` | No verificado en esta pasada — no se encontró un `DeleteCart` explícito en el código revisado. |
| `Order` → `OrderItem` | No aplica en la práctica — no existe un comando de "borrar orden" (las órdenes se cierran por estado, `Cancelled`, no se eliminan). |
| `Wishlist` → `WishlistItem` | No aplica — no hay relación real que cascadear (ver hallazgo de Wishlist arriba). |
| `ProductVariant` → `InventoryMovement` | No verificado — no se encontró un comando de "borrar variante" en el código (consistente con el hallazgo del Modelo de precios: las variantes no tienen operaciones de modificación/eliminación posteriores a la creación). |

**Consecuencia arquitectónica:** cualquier consulta que necesite respetar
"este producto (y todo lo que cuelga de él) está eliminado" debe verificar
la cadena completa explícitamente — exactamente lo que hace
`CheckoutCommandHandler.cs:95-100`, comprobando
`v.IsDeleted || v.Product.IsDeleted || v.Product.Category.IsDeleted` a
propósito con `IgnoreQueryFilters()` para poder ver y evaluar esos tres
niveles a la vez. Esa comprobación explícita en checkout es la evidencia de
que el equipo ya sabía, al menos para ese flujo, que el soft delete no
cascada — pero es un patrón que hay que repetir conscientemente en cada
consulta nueva que lo necesite, no algo que el sistema garantice de forma
centralizada.

---

## Matriz de fuente de verdad

| Dato | Fuente de verdad | ¿Snapshot? | ¿Mutable? | ¿Histórico? |
|---|---|---|---|---|
| Precio de catálogo (mostrado en listados) | `ProductVariant.Price` (vía `Min`) | No | Sí | No |
| Precio en el carrito | `ShoppingCartItem.UnitPrice` | Parcial (se refresca en cada add-to-cart, no automáticamente) | Sí | No |
| Precio pagado en una orden | `OrderItem.UnitPrice` | Sí | **No** — nunca se reescribe | Sí |
| Stock actual | `ProductVariant.Stock` | No | Sí (vía `ExecuteUpdateAsync` atómico) | No |
| Movimiento de inventario | `InventoryMovement` | N/A (es el evento en sí) | No | Sí |
| Estado de una orden | `Order.Status` | No | Sí (solo transiciones válidas, `OrderStatusValidator`) | Parcialmente — no hay historial de transiciones anteriores, solo el estado actual |
| Datos de envío de una orden | `Order.Shipping*` | Sí, capturado en checkout | Sí — editable por admin (`UpdateOrderShippingCommand`) después de creada | No — al editarse, se pierde el valor anterior (no hay historial de cambios de envío) |
| Nombre/imagen de producto en un pedido histórico | `Product`/`ProductVariant` actuales, vía navegación en vivo | **No** — se resuelve en cada consulta, no es una copia | Sí (hereda la mutabilidad del catálogo) | **No**, y ese es el RIESGO documentado arriba |
| Puntos de loyalty | `LoyaltyAccount.Points`/`Level` | No | Sí | Historial en `LoyaltyTransaction` |

---

## Reglas de integridad — dónde vive cada una

| Capa | Reglas que aplica hoy |
|---|---|
| **Dominio** (`Domain/Entities/`) | **Ninguna.** Confirmado en `01-Architecture/DOMAIN-MODEL.md`: todas las entidades son property bags sin guard clauses ni invariantes propios. |
| **FluentValidation** (`Application/.../Validators/`) | Reglas de forma de request: campos requeridos, longitudes, rangos superficiales — corren antes de que el handler se ejecute (y, para el camino HTTP estándar, incluso antes de `ValidationBehavior`, ver ADR-002). |
| **EF Core / configuración** (`Infrastructure/.../EntityConfigurations/`) | `MaxLength`, FKs requeridas/opcionales, comportamiento de cascada donde está declarado explícitamente. |
| **PostgreSQL (check constraints e índices únicos)** | Los invariantes que realmente importan si se violan: `Stock ≥ 0`, `Price ≥ 0`, cantidades `> 0`, unicidad de `SKU`/`Barcode`, una reseña por usuario/producto/orden, un carrito/cuenta de loyalty por usuario. **Es la autoridad final**, según ADR-001 y el patrón repetido en ADR-004. |
| **Handlers / servicios de aplicación** | Reglas de negocio que cruzan entidades: disponibilidad del carrito antes de pagar (`CheckoutCommandHandler`), transición de estado válida (`OrderStatusValidator`), restitución de stock solo en `Paid→Cancelled`, aislamiento por `UserId` (ver Identidad). |
| **Sin enforcement en ningún lado (RIESGO)** | Unicidad de `Product.Slug`/`Category.Slug`; unicidad de `NewsletterSubscriber.Email`; cascada de soft delete Producto→Variante→Imagen; sincronización `ShoppingCartItem.UnitPrice` si el precio de catálogo cambia mientras el ítem ya está en un carrito ajeno a un nuevo add-to-cart. |

Esta tabla es, a propósito, el inventario más honesto de "qué pasaría si
algo falla" — separar "funciona hoy" de "es el lugar arquitectónicamente
correcto" es exactamente la distinción que se pidió no perder.

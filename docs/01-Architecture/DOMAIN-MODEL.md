# Domain Model — BeautyCommerce (Haldy&Co)

Todo lo descrito aquí es **HECHO**: se verificó leyendo las 16 clases de
`backend/src/BeautyCommerce.Domain/Entities/`, sus configuraciones EF Core
en `backend/src/BeautyCommerce.Infrastructure/Configurations/EntityConfigurations/`,
y `ApplicationDbContext.cs`. Donde no se pudo verificar algo con certeza
(por ejemplo, un comportamiento de cascada que depende de una convención
implícita de EF Core en vez de una llamada explícita en código), se marca
así explícitamente en vez de asumirlo.

## Convención transversal: `BaseEntity`

Toda entidad excepto `OutboxMessage` hereda de `BaseEntity`
(`Domain/Base/BaseEntity.cs`), que aporta:

```
Id : Guid
CreatedAt : DateTime
UpdatedAt : DateTime?
IsActive : bool = true
IsDeleted : bool
DeletedAt : DateTime?
CreatedBy : Guid?
UpdatedBy : Guid?
DeletedBy : Guid?
```

`BaseEntity` es un contenedor de propiedades puro — no tiene métodos. El
soft delete y la auditoría (`CreatedAt`/`UpdatedAt`/`DeletedAt`) se aplican
de forma centralizada en `ApplicationDbContext.SaveChangesAsync` y en un
`HasQueryFilter` global (`e => !e.IsDeleted`) aplicado reflectivamente a
toda entidad que herede `BaseEntity` — ninguna entidad individual lo
declara por su cuenta.

## Mapa de relaciones

```
Brand ──1:N── Product ──1:N── ProductVariant ──1:N── InventoryMovement
               │  │               │
               │  └─1:N─ ProductImage
               │
Category ──1:N─┘
   │  └─ self-FK (ParentCategory / Children)

ProductVariant ──1:N── ShoppingCartItem ──N:1── ShoppingCart  (1 por UserId)
ProductVariant ──1:N── OrderItem ──N:1── Order  (por UserId, sin FK a usuario)

Product ──1:N── Review ──N:1(opcional)── Order
Product ──1:N── WishlistItem   (sin FK real a Wishlist — ver Hallazgos)
Wishlist  (por UserId, sin relación formal a WishlistItem)

LoyaltyAccount (1 por UserId) ──1:N── LoyaltyTransaction ──N:1(opcional)── Order

NewsletterSubscriber            (independiente, sin relaciones)
OutboxMessage                   (independiente, sin BaseEntity, sin relaciones)
```

Ningún `UserId` en el dominio (`Order`, `Review`, `WishlistItem`,
`ShoppingCart`, `LoyaltyAccount`, `InventoryMovement`) tiene navegación ni
FK real hacia el usuario — es un `Guid` suelto. Esto es correcto para
Clean Architecture (el Domain no puede depender de ASP.NET Identity, que
vive en Infrastructure), pero significa que **la integridad referencial
usuario↔entidad no la garantiza la base de datos**, solo la lógica de
aplicación.

## Catálogo

### Brand
`Name`, `Description`, `LogoUrl` · `Products: ICollection<Product>` (1:N).
Sin índice único sobre `Name`.

### Category
`Name`, `Slug`, `Description`, `ImageUrl` · auto-relación
`ParentCategoryId → ParentCategory` / `Children` · `Products` (1:N). FK a sí
misma es `Restrict` al borrar (no se puede borrar una categoría con hijas).
**`Slug` no tiene índice único** pese a usarse en URLs indexables
(`/productos?categoria=<slug>`) — ver Hallazgos.

### Product
`Name`, `Slug`, `ShortDescription`, `Description`, `IsFeatured`, `IsNew` ·
`BrandId → Brand` (requerido, `Restrict`) · `CategoryId → Category`
(requerido, `Restrict`) · `Images`, `Variants`, `WishlistItems` (1:N cada
una). **`Slug` tampoco tiene índice único** — mismo hallazgo que
`Category.Slug`.

### ProductVariant
`SKU`, `Barcode`, `Cost`, `Price`, `OldPrice?`, `Stock`, `MinimumStock`,
`Color`, `Size` · `ProductId → Product` (`Cascade`). Es la entidad con más
constraints de todo el dominio (ver Invariantes) — `SKU` y `Barcode` tienen
**índice único** cada uno.

### ProductImage
`ImageUrl`, `IsPrimary`, `DisplayOrder` (default `1`) · `ProductId →
Product` (`Cascade`).

## Comercio

### ShoppingCart / ShoppingCartItem
`ShoppingCart`: `UserId` con **índice único** (un carrito por usuario) ·
`Items` (1:N). `ShoppingCartItem`: `Quantity`, `UnitPrice` ·
`ShoppingCartId → ShoppingCart` (comportamiento de cascada **no declarado
explícitamente**; al ser FK requerida, EF Core aplica `Cascade` por
convención — no confirmado por una llamada literal en código) ·
`ProductVariantId → ProductVariant` (`Restrict`: no se puede borrar una
variante mientras esté en un carrito).

### Order / OrderItem
`Order`: `UserId`, `OrderNumber`, `SubTotal/ShippingCost/Tax/Total`,
`OrderDate`, `Status` (enum), `TransactionId?`, datos de envío
(`ShippingRecipientName/Phone/AddressLine/City/Department`), `Carrier?`,
`TrackingNumber?`, `ShippedAt?` · `Items` (1:N). Check constraints solo en
`Total ≥ 0` y `SubTotal ≥ 0` — **`ShippingCost` y `Tax` no tienen check
constraint de no-negatividad** (ver Hallazgos).

`OrderItem`: `Quantity > 0`, `UnitPrice ≥ 0` como check constraints ·
`OrderId → Order` (`Cascade`) · `ProductVariantId → ProductVariant`
(`Restrict`).

### InventoryMovement
`Quantity`, `IsEntry` (bool), `Reason`, `UserId?` · `ProductVariantId →
ProductVariant` (requerido; cascada no declarada explícitamente, inferida
por convención). Check constraint: `Quantity > 0` — el signo del movimiento
lo determina `IsEntry`, no un `Quantity` negativo. Este es el historial
inmutable de cambios de stock mencionado en `PROJECT-CHARTER.md`; hoy no
tiene una UI que lo muestre (ver `SCOPE.md` → Backlog).

## Engagement de cliente

### Review
`Rating`, `Comment` · `ProductId → Product` (`Cascade`) · `OrderId? → Order`
(opcional, `SetNull`) · **índice único compuesto `(UserId, ProductId,
OrderId)`** — un usuario solo puede reseñar un producto una vez por orden.

### Wishlist / WishlistItem
`Wishlist`: `UserId`, `Items: ICollection<WishlistItem>` (navegación
declarada) — **sin índice único sobre `UserId`**, a diferencia de
`ShoppingCart` y `LoyaltyAccount`. `WishlistItem`: `UserId`, `ProductId →
Product`, con **índice único compuesto `(UserId, ProductId)`**.

**Hallazgo estructural (RIESGO, no defecto activo):** `WishlistItem` no
tiene ninguna propiedad `WishlistId`, y no existe ninguna configuración que
relacione `WishlistItem` con `Wishlist` mediante FK. La navegación
`Wishlist.Items` existe en la clase C#, pero no está respaldada por una
relación real en el esquema — a efectos de base de datos, `Wishlist` y
`WishlistItem` son dos conceptos independientes que comparten el mismo
`UserId`, no una relación padre/hijo verdadera como sí lo es
`ShoppingCart`↔`ShoppingCartItem`. No causa un bug hoy porque el código de
aplicación aparentemente no depende de esa navegación para funcionar (la
wishlist se consulta por `UserId` directamente), pero es una inconsistencia
de modelado que alguien debería resolver conscientemente antes de construir
más funcionalidad sobre wishlist.

### LoyaltyAccount / LoyaltyTransaction
`LoyaltyAccount`: `UserId` con **índice único** (una cuenta por usuario),
`Points`, `Level` (default `"Bronze"`) · `Transactions` (1:N).
`LoyaltyTransaction`: `Points`, `Type`, `Description` ·
`LoyaltyAccountId → LoyaltyAccount` (`Cascade`) · `OrderId? → Order`
(opcional, `SetNull`).

### NewsletterSubscriber
`Email` únicamente. **No existe archivo de configuración EF Core para esta
entidad** — se mapea enteramente por convención. Consecuencia verificada:
**no hay índice único confirmado sobre `Email`**, lo que en principio
permitiría suscripciones duplicadas. Marcado como RIESGO a confirmar, no
como defecto reproducido.

## Plataforma

### OutboxMessage
`OccurredOn`, `Type`, `Content`, `ProcessedOn?`, `Error?`. No hereda
`BaseEntity` — sin auditoría, sin soft delete, sin filtro global, y sin
archivo de configuración EF propio (mapeado por convención). Es consumido
por `OutboxProcessor` (ver `01-Architecture/SYSTEM-ARCHITECTURE.md`) para
enviar el correo de confirmación de orden.

## Invariantes de negocio — dónde viven realmente

**HECHO, y arquitectónicamente relevante:** ningún invariante de negocio
está codificado como método o guard clause dentro de una clase de
`Domain/Entities/`. Todas son property bags. Los invariantes reales están
en los **check constraints de PostgreSQL**, declarados vía Fluent API en
cada `*Configuration.cs`:

| Invariante | Dónde se aplica |
|---|---|
| `ProductVariant.Stock ≥ 0` | Check constraint |
| `ProductVariant.Price ≥ 0`, `Cost ≥ 0`, `OldPrice ≥ 0` (si no es null) | Check constraint |
| `ProductVariant.MinimumStock ≥ 0` | Check constraint |
| `ProductVariant.SKU`/`Barcode` no vacíos (`length(trim(x)) > 0`) | Check constraint |
| `ProductVariant.SKU`, `Barcode` únicos | Índice único |
| `InventoryMovement.Quantity > 0` | Check constraint |
| `Order.Total ≥ 0`, `SubTotal ≥ 0` | Check constraint |
| `OrderItem.Quantity > 0`, `UnitPrice ≥ 0` | Check constraint |
| `ShoppingCartItem.Quantity > 0`, `UnitPrice ≥ 0` | Check constraint |
| Una reseña por usuario/producto/orden | Índice único compuesto |
| Un carrito / una cuenta de loyalty por usuario | Índice único |

Esto significa que **cualquier código que use `ApplicationDbContext`
directamente sin pasar por un handler de MediatR ya validado sigue estando
protegido** por estos invariantes — la base de datos es la última
autoridad, no una capa de aplicación que podría saltarse. Es la misma
filosofía que motivó la corrección de la condición de carrera de registro
en Puntos 1–5 (ver `06-Quality/AUDIT-PUNTOS-1-5.md`): no confiar solo en un
pre-check de aplicación para una invariante crítica.

## Hallazgos estructurales (RIESGO — no defectos confirmados)

Estos son observaciones de modelado que no tienen evidencia de causar un
problema hoy, pero que alguien diseñando features nuevas sobre estas
entidades debería conocer antes de asumir que el esquema es más estricto de
lo que realmente es:

1. **`Product.Slug` y `Category.Slug` sin índice único**, pese a ser la
   base de URLs indexables para SEO (`/productos?categoria=<slug>`). Nada
   en la base de datos impide dos productos o categorías con el mismo slug.
2. **`Wishlist`↔`WishlistItem` sin relación FK real** (ver arriba).
3. **`NewsletterSubscriber.Email` sin unicidad confirmada** — sin archivo
   de configuración EF, depende de convención.
4. **`Order.ShippingCost` y `Order.Tax` sin check constraint de
   no-negatividad**, a diferencia de `Total`/`SubTotal` que sí lo tienen.
5. Varias relaciones requeridas (`ShoppingCartItem→ShoppingCart`,
   `InventoryMovement→ProductVariant`, `WishlistItem→Product`) no declaran
   `.OnDelete()` explícitamente — EF Core aplica `Cascade` por convención
   para FKs requeridas, pero al no estar escrito literalmente en el código,
   un cambio futuro de convención de EF Core podría alterar ese
   comportamiento sin que nadie lo note en el diff.

Ninguno de estos cinco puntos se reclasifica como defecto sin una
reproducción real que demuestre que produce un problema observable — esa
es la misma disciplina que se aplicó durante Puntos 1–5 y que
`00-Governance/SCOPE.md` documenta como regla permanente.

# Scope and Boundaries — BeautyCommerce (Haldy&Co)

Este documento es el **mapa de capacidades** de la plataforma: qué existe,
en qué estado, y qué queda fuera. No es una lista plana de "esto existe /
esto no existe" — cada capacidad se ubica dentro de un dominio de negocio
para que después se pueda trazar capacidad → dominio → API → frontend →
datos → UX → tests → riesgos, según cada documento de arquitectura se vaya
escribiendo.

Todo lo marcado 🟢/🟡/🔴 está verificado (**HECHO**) contra los 15
controllers reales de `backend/src/BeautyCommerce.API/Controllers/`, las
rutas de `frontend/web/src/routes/AppRoutes.tsx` y búsquedas de código
puntuales — no es una lista aspiracional. La leyenda de estado:

- 🟢 **Construido** — implementado y operable de punta a punta.
- 🟡 **Parcial** — implementado pero con un hueco funcional conocido.
- 🔴 **No construido** — no existe en el código.

## Mapa de capacidades

```
HALDY&CO PLATFORM (BeautyCommerce)
│
├── Customer Experience
│   ├── Catalog                🟢  ProductsController, filtro por categoría
│   ├── Product Discovery      🟢  búsqueda, más vendidos, reseñas destacadas
│   ├── Wishlist                🟡  falta botón en ProductDetail.tsx (ver Backlog)
│   ├── Cart                    🟢  CartController, persistente por usuario
│   ├── Checkout                🟢  captura envío + costo Bogotá/nacional
│   └── Orders (cliente)        🟢  historial y detalle propio
│
├── Commerce
│   ├── Pricing                 🟡  precio por variante; sin motor de precios/reglas
│   ├── Payments                🔴  PaymentService es un simulador (ver Charter)
│   ├── Orders (negocio)        🟢  ciclo de estados, restock al cancelar
│   ├── Inventory                🟢  stock + InventoryMovement auditable
│   │                                 └─ Historial visual de movimientos  🟡  (ver Backlog)
│   └── Promotions               🔴  no existe Coupon/Discount/Promotion en el dominio
│
├── Administration (rol Admin)
│   ├── Products                 🟢  CRUD, creación con variantes e imágenes
│   │                                 └─ Edición de variantes post-creación  🟡  (ver Backlog)
│   ├── Product Media            🟡  alta en creación + sync Drive→Cloudinary;
│   │                                 sin reordenar/marcar principal/eliminar desde UI
│   ├── Orders                   🟢  cambio de estado, edición de envío
│   ├── Customers                🟢  UsersController
│   ├── Inventory                🟢
│   ├── Brands / Categories      🟢  CRUD completo
│   └── Dashboard                🟢  métricas de ventas, pedidos, clientes
│
├── Identity
│   ├── Registration              🟢
│   ├── Authentication (JWT)      🟢
│   ├── Lockout                    🟢  corregido (Escenario I, ver AUDIT-PUNTOS-1-5)
│   └── Authorization (roles)     🟢  Customer / Admin
│
└── Platform
    ├── Database (PostgreSQL/EF)  🟢
    ├── Caching                    🟢  CachingBehavior + ICacheService, en escritura/lectura de catálogo
    ├── Outbox                     🟢  OutboxMessage + OutboxProcessor (email de confirmación)
    ├── Observability               🟡  por evaluar en SYSTEM-ARCHITECTURE.md
    └── Security                    🟡  por evaluar en SECURITY-ARCHITECTURE.md
```

## Por qué Product Variants y Product Media son dos capacidades distintas

Se listan por separado a propósito, aunque hoy comparten el mismo formulario
de creación de producto. Son dominios de decisión distintos:

```
Product Variant Management              Product Media Management
Product                                  Product
 └── Variant                              └── Media
      ├── SKU                                  ├── URL
      ├── Color                                 ├── Order
      ├── Size                                  ├── IsPrimary
      ├── Price                                 ├── AltText
      └── Stock                                 └── (storage, CDN, formatos, thumbnails...)
```

Media puede evolucionar de forma completamente independiente de variantes
(CDN, optimización de formato, imágenes por variante específica, alt text
para accesibilidad/SEO) y mezclarlas en un solo documento de diseño futuro
haría que ese documento intentara resolver dos problemas de arquitectura
distintos a la vez. Cuando se diseñe la capacidad de gestión avanzada de
cada una, van en documentos separados: `07-Features/PRODUCT-VARIANTS.md` y
`07-Features/PRODUCT-MEDIA.md`.

## Fuera de alcance actual

Estas capacidades no están implementadas y no hay evidencia en el código de
que estén planeadas para el corto plazo. Se listan para que nadie asuma que
existen ni las de por sentado en un diseño futuro:

- **Multi-idioma.** Todo el copy está en español fijo, sin capa de i18n.
- **Multi-moneda.** Precios en COP sin conversión ni configuración de otra
  moneda.
- **Multi-vendor / marketplace de terceros.** El catálogo lo administra un
  único operador (rol `Admin`); no hay onboarding de vendedores externos.
- **Server-side rendering / prerender.** El frontend es un SPA de Vite
  puro; `docs/SEO-ROADMAP.md` lo señala como mejora técnica pendiente, no
  como algo construido.
- **Promociones, cupones y descuentos.** Sin rastro de `Coupon`,
  `Discount` ni `Promotion` en el dominio — confirmado por búsqueda directa
  en `backend/src`.
- **Blog / contenido editorial.** Mencionado como oportunidad en
  `docs/SEO-ROADMAP.md`, no construido.

## Backlog conocido (no son defectos)

Encontrados durante el hardening de Puntos 1–5 y verificados de forma
independiente en esta pasada de documentación. Cada uno vive dentro de la
capacidad a la que pertenece en el mapa de arriba; aquí se listan con su
evidencia y su clasificación de impacto:

| Backlog | Capacidad | Evidencia | Clasificación |
|---|---|---|---|
| No hay botón de wishlist en la ficha de producto (`ProductDetail.tsx`), aunque sí existe en otras vistas de listado. | Customer Experience → Wishlist | Búsqueda de `Wishlist\|FavoriteBorder\|useWishlist` en `ProductDetail.tsx`: sin resultados. | Gap funcional (P2) |
| No hay UI para editar variantes de un producto después de creado. | Administration → Products | Sin referencias a edición de variantes en `AdminProductEdit.tsx`. | Gap funcional (P1) |
| No hay UI para ver el historial de `InventoryMovement` de un producto (el dato existe en base de datos, no se expone visualmente). | Commerce → Inventory | Sin referencias a movimientos/historial en `AdminInventory.tsx`. | Gap funcional (P2) |
| Gestión avanzada de media de producto (reordenar, marcar principal desde la UI, eliminar individualmente post-creación). | Administration → Product Media | Reportado por el equipo durante Puntos 1–5; no se volvió a verificar línea por línea en esta pasada — **pendiente de confirmación**. | Gap funcional (P1) |

### 🟡 Riesgo de mantenibilidad (no defecto, no gap funcional)

Este ítem se clasifica aparte porque no es una capacidad faltante — es una
condición del código que **podría** derivar en un problema, no uno
confirmado. Ascenderlo a defecto sin evidencia de que produce inconsistencia
observable repetiría exactamente el error que aprendimos a evitar durante
Puntos 1–5 (ver `06-Quality/AUDIT-PUNTOS-1-5.md`): no convertir una
posibilidad de problema en un defecto real sin probarlo.

- **Query keys de TanStack Query sin consolidar.** Las claves
  `"admin-product"`, `"admin-product-edit"` y `"admin-products"` están
  repetidas como literales en `AdminProducts.tsx`, `AdminProductEdit.tsx` y
  `AdminProductDetail.tsx` en vez de vivir en una factory central. Hoy no
  hay evidencia de que haya causado una invalidación de caché inconsistente
  en producción — es un riesgo de mantenibilidad a vigilar, no un bug.

## Prioridad de cierre de brechas (P0–P3)

Ver el detalle y el razonamiento completo en `PROJECT-CHARTER.md` →
Release Readiness. Resumen:

| Prioridad | Contenido |
|---|---|
| 🔴 P0 — Viabilidad del producto | Integración de pasarela de pago real. |
| 🟠 P1 — Completitud de comercio | Gestión de variantes, gestión de media, promociones/precios, inventario avanzado. |
| 🟡 P2 — Experiencia | Wishlist en ProductDetail, historial visual de movimientos, búsqueda/recomendaciones. |
| 🟢 P3 — Optimización | Caching avanzado, performance, analytics, observabilidad avanzada. |

## Regla de cierre

Cualquier ítem que se agregue a "fuera de alcance" o "backlog" en el futuro
debe venir con evidencia (archivo, búsqueda, o test) o quedar marcado
explícitamente como **INFERENCIA** o **SUPUESTO** según corresponda — no se
agregan ítems por intuición sin etiquetarlos como tal. Ninguna entrada de
esta tabla se asciende a "defecto" sin una reproducción real que lo
demuestre.

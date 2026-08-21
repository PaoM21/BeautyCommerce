# Project Charter — BeautyCommerce (Haldy&Co)

> **Nota de origen:** este documento fue redactado por un agente a partir del
> código, la interfaz y los documentos existentes del repositorio — no a
> partir de un brief de negocio escrito por el equipo. Las secciones de
> propuesta de valor, audiencia y visión están marcadas **INFERENCIA**
> porque se dedujeron del producto construido, no de una fuente de negocio
> declarada. Deben ser confirmadas o corregidas por quien tiene la visión de
> negocio real; hasta entonces, trátense como una hipótesis fundamentada, no
> como una decisión de producto cerrada.

## ¿Qué es?

**HECHO.** BeautyCommerce es el nombre técnico del repositorio y la
plataforma de e-commerce. El producto de cara al cliente se presenta bajo la
marca **Haldy&Co** ("HALDY&CO — ECOMMERCE" en el header del sitio). Es una
tienda en línea de productos de belleza (cabello, rostro, piel, labios,
uñas) con catálogo multi-marca, carrito, checkout, cuenta de cliente,
programa de fidelización y un panel de administración completo para operar
el negocio.

## ¿Qué problema resuelve?

**INFERENCIA**, a partir del posicionamiento visible en el sitio (copy de
Home: *"Descubre la belleza que resalta tu esencia"*, sección "Haldy&Co
Selection", paleta editorial boutique) y de `docs/SEO-ROADMAP.md`, que
declara explícitamente el objetivo de *"posicionar BeautyCommerce como
referente de belleza premium en Colombia"*:

Ofrecer una alternativa boutique/curada a los marketplaces genéricos de
belleza — un catálogo seleccionado de marcas, presentado con una experiencia
editorial de lujo, en vez de un listado masivo sin curaduría. El objetivo de
negocio no es solo completar transacciones sino construir clientela
recurrente (de ahí el programa de loyalty y la newsletter).

## ¿Para quién?

**INFERENCIA**, del mercado objetivo declarado en `docs/SEO-ROADMAP.md`
("comprar skincare de lujo Colombia", "perfumes originales domicilio",
envío diferenciado Bogotá/resto del país) y de los precios en pesos
colombianos (COP) en todo el frontend:

Consumidoras/es de belleza en Colombia dispuestos a pagar por curaduría y
experiencia, no solo por precio — el segmento premium/aspiracional del
mercado, con Bogotá como plaza principal (costo de envío diferenciado:
`Shipping:BogotaCost` vs. `Shipping:NationalCost`).

## Propuesta de valor

**INFERENCIA**, de las features realmente construidas (ver evidencia abajo):

1. **Curaduría de marca**, no venta masiva sin filtro: sección "Marcas que
   amamos", categorías fijas (Cabello, Rostro, Piel, Labios, Uñas).
2. **Experiencia editorial**, no plantilla genérica de e-commerce: design
   system propio (paleta boutique crema/rosa empolvado/negro,
   tipografía Fraunces + Inter), placeholders de foto con dirección de arte,
   JSON-LD de producto para rich snippets.
3. **Confianza transaccional**: reseñas con calificación agregada, reseñas
   destacadas en Home, "más vendidos" calculado por unidades vendidas reales
   (no curado a mano).
4. **Fidelización activa**: programa de loyalty/rewards para clientes
   registrados, newsletter con suscripción propia (no un servicio externo
   embebido).
5. **Operación seria detrás del escenario**: panel admin con dashboard de
   ventas, gestión de inventario con movimientos auditables, gestión de
   pedidos con estados y reposición de stock al cancelar, gestión de
   marcas/categorías — el negocio se puede operar sin tocar la base de
   datos a mano.

## Qué lo diferencia de un e-commerce tradicional

**INFERENCIA.** La mayoría de plataformas de e-commerce genéricas (Shopify
sin personalizar, WooCommerce por defecto) no tienen: (a) sincronización de
imágenes de producto desde Drive del equipo de contenido hacia Cloudinary
como flujo operativo propio, (b) `InventoryMovement` como historial
inmutable de cada cambio de stock (no solo un contador), (c) un
`TransactionBehavior` de MediatR que envuelve cada comando en una
transacción de base de datos por defecto, con opt-out explícito
(`INotTransactional`) para los casos donde eso sería incorrecto — decisiones
de ingeniería, no de plantilla.

## Qué está dentro del producto (resumen — detalle en `SCOPE.md`)

**HECHO**, verificado contra `AppRoutes.tsx` y los controllers del backend:

Catálogo y descubrimiento · Carrito y checkout con envío · Cuenta de
cliente (registro, login, recuperación de contraseña por email) · Wishlist ·
Programa de loyalty · Reseñas de producto · Newsletter · Panel
administrativo (dashboard, productos, inventario, pedidos, clientes,
marcas, categorías) · Sincronización de imágenes Drive→Cloudinary · SEO
técnico (JSON-LD, sitemap, robots.txt) · Páginas legales.

## Qué está fuera (por ahora)

Ver `SCOPE.md` para el detalle completo y la distinción entre "fuera de
alcance por decisión" y "backlog conocido". Ejemplos de alto nivel:
server-side rendering, blog/contenido editorial, multi-idioma,
multi-moneda, marketplace de terceros (multi-vendor).

## Estado actual

**HECHO.** Proyecto en desarrollo activo, sin confirmación de que exista un
despliegue de producción en uso real por clientes finales. Existe
configuración de despliegue lista para Render
(`render.yaml`, `backend/Dockerfile`, `docs/DEPLOYMENT.md`), pero
`docs/SEO-ROADMAP.md` señala explícitamente que el dominio de producción
todavía es un placeholder (`beautycommerce.co`) pendiente de compra, y que
el catálogo de la base de datos actual tiene datos de prueba genéricos, no
las categorías de belleza reales.

### Checkout: funcional end-to-end, con Payment Simulator

**HECHO.** El flujo completo de compra funciona sin errores de punta a
punta, pero el último eslabón es un simulador, no una pasarela real:

```
Customer
   │
   ▼
Cart
   │
   ▼
Checkout ──── captura envío + calcula costo (Bogotá/nacional)
   │
   ▼
PaymentService  (backend/src/BeautyCommerce.Infrastructure/Services/PaymentService.cs)
   │
   ▼
"SIM-<guid>"   ← transacción simulada, siempre aprueba montos > 0
   │
   ▼
Order (Paid)

                          Payment Gateway real (Stripe/PayU/Wompi/...)
                                          ❌  no integrado
```

Esta distinción importa porque "el checkout funciona" y "podemos vender
dinero real" son afirmaciones distintas, y es fácil confundirlas si solo se
mide por si el flujo completa sin errores.

### Release readiness

| Capacidad | Estado | Nota |
|---|---|---|
| Catálogo | 🟢 | Listado, filtro por categoría, ficha de producto, reseñas. |
| Carrito | 🟢 | Persistente por usuario autenticado. |
| Checkout | 🟢 | E2E funcional, incluye envío y cálculo de costo. |
| Órdenes | 🟢 | Estados, historial, detalle admin y cliente. |
| Inventario | 🟢 | Con `InventoryMovement` como historial auditable. |
| Autenticación | 🟢 | Registro, login, lockout, recuperación de contraseña. |
| Administración | 🟢 | Dashboard, productos, pedidos, clientes, marcas, categorías. |
| **Pago real** | 🔴 | **No implementado.** `PaymentService` es un simulador (`SIM-...`). Ver diagrama arriba. |
| Observabilidad en producción | 🟡 | Por evaluar — no cubierto todavía por esta documentación. |
| Seguridad en producción | 🟡 | Por evaluar — pendiente `01-Architecture/SECURITY-ARCHITECTURE.md`. |
| Deploy en producción | 🟡 | Configuración lista (Render), dominio real y datos de catálogo reales pendientes. |

**Conclusión:** el sistema está funcionalmente completo como flujo
end-to-end de comercio, pero no está listo para procesar ventas reales
hasta integrar una pasarela de pago real. Ese es hoy el bloqueador de
lanzamiento más determinante — por delante del dominio, los datos de
catálogo o cualquier mejora de experiencia. Ver la priorización completa
(P0–P3) en `SCOPE.md`.

## Supuestos de negocio a confirmar

**SUPUESTO.** Estas no son inferencias técnicas — son hipótesis de producto
implícitas en cómo está construido el sistema, que solo alguien con
autoridad de negocio puede confirmar o corregir. Hasta que se confirmen, no
deben leerse como decisiones tomadas:

- **SUPUESTO:** el cliente podrá completar todo el ciclo de compra
  (descubrimiento → pago → posventa) sin necesidad de asesoría humana en
  vivo — no hay chat en vivo, WhatsApp de ventas ni ningún punto de contacto
  humano modelado en el flujo actual.
- **SUPUESTO:** Bogotá es el mercado prioritario y el resto del país es
  secundario — se infiere de que el envío tiene solo dos tarifas
  (`Shipping:BogotaCost` / `Shipping:NationalCost`), no una por ciudad.
- **SUPUESTO:** el segmento objetivo tolera precio premium a cambio de
  curaduría/experiencia — no hay mecanismo de descuentos, cupones o
  promociones en el dominio actual (ver `SCOPE.md`), lo cual es consistente
  con un posicionamiento premium pero no está confirmado como estrategia
  deliberada.

## Visión futura

**INFERENCIA**, no declarada explícitamente en ningún documento del
repositorio salvo el roadmap de SEO. Se deja como pregunta abierta para
`02-Product/PRODUCT-VISION.md` en vez de inventarse aquí. Lo único
documentado hoy sobre dirección futura son los puntos de
`docs/SEO-ROADMAP.md` (SSR/prerender, blog, multi-ciudad) y el backlog
técnico identificado durante el hardening (ver
`06-Quality/AUDIT-PUNTOS-1-5.md`).

## Decisiones estratégicas registradas

Ver `01-Architecture/ADR/` para las decisiones técnicas individuales. Este
charter no repite las decisiones de arquitectura; las referencia.

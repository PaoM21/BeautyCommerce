# Roadmap de SEO — BeautyCommerce

Este documento describe qué se implementó ya en el código (base técnica) y cuáles son los siguientes pasos para posicionar BeautyCommerce como referente de belleza premium en Colombia.

## 0. Lo que ya quedó implementado en esta fase

- **Meta tags base** en [`index.html`](../frontend/web/index.html): title, description, keywords, robots, canonical, Open Graph y Twitter Card, `lang="es-CO"`.
- **Componente `<Seo>`** ([`src/components/seo/Seo.tsx`](../frontend/web/src/components/seo/Seo.tsx)) que usa el soporte nativo de React 19 para `<title>`/`<meta>`/`<link>` (sin dependencias extra) y permite title/description/canonical/OG/JSON-LD por página.
- **Datos estructurados (JSON-LD)**:
  - `Organization` en la Home.
  - `Product` (con `Offer` y `AggregateRating`) en la ficha de producto — habilita rich snippets de precio y estrellas en Google.
- **Jerarquía de encabezados correcta**: un solo `<h1>` por página (Home, Listado, Detalle de producto), `<h2>` para secciones.
- **`robots.txt`** y **`sitemap.xml`** estático en `frontend/web/public/`.
- **Filtrado real por categoría** (`/productos?categoria=cabello`, `rostro`, `piel`, `labios`, `unas`) para que las URLs de categoría sean indexables y muestren contenido único, no un listado genérico duplicado.

## 1. Antes de salir a producción

- [ ] Reemplazar el dominio placeholder `https://www.beautycommerce.co` (usado en `index.html`, `Seo.tsx`, `robots.txt` y `sitemap.xml`) por el dominio real una vez esté comprado y apuntando.
- [ ] Crear la imagen `og-cover.jpg` (1200×630 px) para que las vistas previas en WhatsApp/Facebook/Instagram/X se vean bien.
- [x] Categorías reales cargadas (`cabello`, `rostro`, `piel`, `labios`, `unas`) — hecho.
- [ ] **Reemplazar el catálogo DEMO por el inventario real y verificado.** Se cargaron 8 marcas, 20 productos y 27 variantes de ejemplo (Maybelline New York, L'Oréal Paris, Ruby Rose, Italia Deluxe, Beauty Creations, Moira Cosmetics, Nivea, Garnier) con precios estimados para que el sitio no esté vacío mientras se consigue la lista real — **no son datos verificados de negocio**. Antes de vender de verdad: confirmar autorización real de distribución para cada marca listada, ajustar precios/costos/stock reales, y reemplazar por el catálogo definitivo (vía Admin → Productos/Categorías/Marcas, o con un importador masivo si se define un formato de archivo).
- [ ] Cargar fotos reales de producto (hoy ningún producto tiene imagen — se muestra "Sin imagen" de forma controlada) e incluir `alt` descriptivo y `shortDescription` completa por producto, insumo directo del SEO on-page.

## 2. SEO técnico

- [ ] **Renderizado en servidor o prerender.** Hoy el sitio es un SPA con Vite (client-side rendering puro). Google indexa JS razonablemente bien, pero para competir por keywords transaccionales conviene migrar a SSR/SSG (Next.js) o usar prerendering (`vite-plugin-ssr`, Prerender.io) al menos para Home, listados de categoría y fichas de producto.
- [ ] **Sitemap dinámico.** Generar `sitemap.xml` desde el backend (endpoint que recorra `Products` y `Categories` activos) en vez del archivo estático actual, y regenerarlo en cada deploy o por cron.
- [ ] **Datos estructurados adicionales:** `BreadcrumbList` en categoría/producto, `ItemList` en el listado de productos, `FAQPage` si se agrega una sección de preguntas frecuentes.
- [ ] **Core Web Vitals:** auditar LCP/CLS/INP con Lighthouse una vez haya contenido real; optimizar imágenes (formato WebP/AVIF, `srcset`, lazy loading) y revisar el peso de MUI/Emotion en el bundle inicial.
- [ ] **Canonicalización de filtros:** cuando se agreguen más filtros (precio, marca, orden), usar `rel=canonical` hacia la URL "limpia" para evitar contenido duplicado.
- [ ] Registrar el sitio en **Google Search Console** y **Bing Webmaster Tools**, verificar propiedad y enviar el sitemap.
- [ ] Configurar **Google Analytics 4** (o Plausible/Matomo si se prioriza privacidad) para medir tráfico orgánico y conversión.

## 3. Investigación de palabras clave

- [ ] Priorizar términos de intención de compra en Colombia: *"maquillaje original Bogotá"*, *"comprar skincare de lujo Colombia"*, *"perfumes originales domicilio"*, *"tratamiento capilar premium"*.
- [ ] Usar Google Keyword Planner / Ubersuggest / Semrush con filtro geográfico Colombia para volumen y dificultad.
- [ ] Mapear una palabra clave principal por página de categoría (Cabello, Rostro, Piel, Labios, Uñas) y usarla en `<h1>`, primer párrafo, `title` y URL.
- [ ] Identificar términos de marca (nombres de las marcas premium que se van a vender) — suelen tener alta intención de compra y baja competencia.

## 4. Contenido y autoridad temática

- [ ] Crear un **blog o "Beauty Journal"** con guías de rutina (ej. "Rutina de skincare coreana paso a paso", "Cómo elegir tu labial según tu tono de piel") enlazando a las categorías/productos correspondientes — es la palanca más efectiva para rankear términos informativos y generar backlinks internos.
- [ ] Descripciones de producto únicas y ricas (no copiar la ficha técnica del fabricante) — Google penaliza contenido duplicado entre tiendas que venden las mismas marcas.
- [ ] Página "Sobre nosotros" y de contacto completas (name, address, phone consistentes) para reforzar E-E-A-T y SEO local.

## 5. SEO local (Colombia)

- [ ] Crear y verificar **perfil de Google Business Profile** si hay tienda física o showroom.
- [ ] Asegurar consistencia NAP (Nombre, Dirección, Teléfono) en el sitio, redes sociales y directorios.
- [ ] Considerar páginas o secciones para ciudades principales (Bogotá, Medellín, Cali) si el envío/tiempos de entrega varían, para capturar búsquedas tipo *"[categoría] + [ciudad]"*.

## 6. Link building y señales externas

- [ ] Alianzas con influencers/beauty bloggers colombianos para reseñas con enlace (nofollow o patrocinado, cumpliendo lineamientos de Google).
- [ ] Publicar en directorios de ecommerce y cámaras de comercio locales.
- [ ] Programa de reseñas de producto verificadas (ya existe el módulo de `Reviews` en el backend) — las reseñas alimentan el `AggregateRating` del JSON-LD y son señal de confianza tanto para usuarios como para buscadores.

## 7. Monitoreo continuo

- [ ] Dashboard mensual de posiciones (Search Console), tráfico orgánico (GA4) y Core Web Vitals (PageSpeed Insights / CrUX).
- [ ] Revisión trimestral de contenido: actualizar precios/disponibilidad en JSON-LD, refrescar posts del blog con bajo desempeño, detectar enlaces rotos (404) con Screaming Frog.
- [ ] Alertas de indexación (Search Console → Cobertura) para detectar caídas súbitas de páginas indexadas.

---

**Prioridad sugerida para las próximas 2-4 semanas:** (1) cargar categorías reales en el catálogo, (2) comprar dominio y actualizar placeholders, (3) Search Console + GA4, (4) primeras 5 páginas de categoría optimizadas con su keyword principal, (5) arrancar el blog con 2-3 artículos pilares.

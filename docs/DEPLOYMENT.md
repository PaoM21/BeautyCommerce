# Guía de despliegue a producción

Arquitectura elegida: **Render** (backend .NET en Docker + Postgres gestionado) + **Cloudflare Pages** (frontend React estático) + **Cloudflare Registrar** (dominio). Ver la conversación de decisión en el historial del proyecto para el porqué de esta elección frente a Azure o un VPS.

Todo lo que sigue asume que el repo ya está en GitHub (Render y Cloudflare Pages se conectan directo al repo para hacer deploy automático en cada push).

Lo que ya quedó listo en el código (verificado con un build + smoke test real en Docker local):

- `backend/Dockerfile` y `backend/.dockerignore` — imagen de la API.
- `render.yaml` en la raíz del repo — Render lo detecta automáticamente como "Blueprint" y crea el web service + la base de datos con un clic.
- El backend ahora **aplica las migraciones de EF Core automáticamente al arrancar** (fuera de `Development`), así que no hay que correr `dotnet ef database update` a mano en producción.
- El connection string se normaliza automáticamente: acepta tanto el formato `Host=...` (el que ya usa `appsettings.Development.json`) como el formato `postgres://usuario:clave@host:puerto/db` que entrega Render — así que el Blueprint puede conectar la base de datos al backend automáticamente sin que tengas que copiar/pegar ni reconstruir el string a mano.
- `frontend/web/public/_redirects` — necesario para que las rutas de React Router (`/productos`, `/carrito`, etc.) no den 404 al refrescar la página en Cloudflare Pages.

## Paso 1 — Backend en Render

1. Entra a [render.com](https://render.com) y crea una cuenta (puedes usar tu GitHub).
2. **New → Blueprint**, conecta el repositorio de GitHub. Render va a leer `render.yaml` automáticamente y te va a mostrar dos recursos por crear: la base de datos `beautycommerce-db` y el web service `beautycommerce-api`. Confirma los planes (el archivo sugiere `starter` en ambos; puedes bajar a `Free` para probar, sabiendo que el plan free de Postgres en Render expira en 30 días).
3. Render va a pedirte los valores de las variables marcadas `sync: false` en `render.yaml` (son las que no se pueden versionar por ser secretas):
   - `Jwt__Key`: genera uno localmente y pégalo — **no reutilices ningún ejemplo que hayas visto en un chat o documento**:
     ```bash
     openssl rand -base64 32
     ```
     (En PowerShell: `[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))`)
   - `Cors__AllowedOrigins__0`: de momento pon un valor cualquiera como `https://placeholder.pages.dev` — lo vas a corregir en el Paso 3 con la URL real del frontend. **No lo dejes vacío**: la app está configurada para no arrancar sin al menos un origin válido (por seguridad, para no arrancar con CORS abierto a todos por accidente).
   - `GoogleDrive__FolderId`, `GoogleDrive__ServiceAccountJson`, `Cloudinary__*`: los valores que obtuviste siguiendo `docs/GOOGLE_DRIVE_IMAGE_SYNC.md`. Si todavía no los tienes, puedes dejarlos vacíos por ahora — el resto de la tienda funciona igual, solo el botón "Sincronizar imágenes desde Drive" del Admin fallará hasta que los configures.
   - `Email__ApiKey`: tu API key de Resend — ver `docs/EMAIL_SETUP.md`. Si lo dejas vacío, la tienda sigue funcionando normal, simplemente no se envían correos de confirmación de pedido ni de restablecer contraseña.
   - `Frontend__BaseUrl`: de momento pon el mismo placeholder que usaste en `Cors__AllowedOrigins__0` — lo vas a corregir en el Paso 3 junto con CORS. Se usa para armar el link del correo de "restablecer contraseña".
4. Deploy. La primera build tarda varios minutos (compila el backend completo). Puedes seguir el progreso en la pestaña "Logs" — vas a ver las migraciones de EF Core aplicándose igual que en la prueba local.
5. Cuando termine, Render te da una URL tipo `https://beautycommerce-api.onrender.com`. Verifica que está viva:
   ```bash
   curl https://beautycommerce-api.onrender.com/health
   ```
   Debe responder `Healthy`.

## Paso 2 — Frontend en Cloudflare Pages

1. En el dashboard de Cloudflare, ve a **Workers & Pages → Create → Pages → Connect to Git**, elige el repo.
2. Configuración del build:
   - **Root directory**: `frontend/web`
   - **Build command**: `npm run build`
   - **Build output directory**: `dist`
3. Variable de entorno (Settings → Environment variables), tanto para "Production" como "Preview":
   - `VITE_API_URL` = `https://beautycommerce-api.onrender.com/api` (la URL de Render del Paso 1, con `/api` al final — así está armado `src/services/api.ts`).
4. Deploy. Cloudflare te da una URL tipo `https://beautycommerce-web.pages.dev`.

## Paso 3 — Conectar CORS

Con la URL real del frontend (`https://beautycommerce-web.pages.dev` o la que te haya dado Cloudflare):

1. En Render, ve al servicio `beautycommerce-api` → **Environment** → edita `Cors__AllowedOrigins__0` **y** `Frontend__BaseUrl` con esa misma URL exacta (sin `/` al final).
2. Guarda — Render redeploya automáticamente (no hace falta rehacer el build completo, solo reinicia con la nueva variable).
3. Prueba la tienda real: abre la URL de Cloudflare Pages y verifica que carga el catálogo sin errores de CORS en la consola del navegador.

## Paso 4 — Dominio propio

1. Compra el dominio en [Cloudflare Registrar](https://www.cloudflare.com/products/registrar/) (sin markup sobre el precio mayorista, y ya queda con el DNS en Cloudflare).
2. **Frontend**: en el proyecto de Cloudflare Pages → **Custom domains** → agrega tu dominio (ej. `www.tudominio.com`). Cloudflare configura el DNS automáticamente porque ya administra la zona.
3. **Backend**: en Render, el servicio → **Settings → Custom Domains** → agrega un subdominio, ej. `api.tudominio.com`. Render te da un registro CNAME para crear — como el dominio ya está en Cloudflare, lo agregas ahí en **DNS**.
4. Actualiza las variables que dependen de URLs, y vuelve a desplegar:
   - En Cloudflare Pages: `VITE_API_URL` → `https://api.tudominio.com/api`
   - En Render: `Cors__AllowedOrigins__0` y `Frontend__BaseUrl` → `https://www.tudominio.com`
   - En Render: `Email__FromAddress` → tu correo verificado en el dominio propio (ver `docs/EMAIL_SETUP.md`)
5. Actualiza también los placeholders `https://www.beautycommerce.co` que quedaron en `frontend/web/index.html`, `frontend/web/src/components/seo/Seo.tsx`, `frontend/web/public/robots.txt` y `frontend/web/public/sitemap.xml` (documentado ya en `docs/SEO-ROADMAP.md`) por el dominio real, para que el SEO y las tarjetas de Open Graph apunten al sitio correcto.

## Verificación final

- [ ] `https://api.tudominio.com/health` responde `Healthy`.
- [ ] La tienda carga en `https://www.tudominio.com` sin errores de CORS en consola.
- [ ] Puedes registrar un usuario, iniciar sesión y agregar un producto al carrito (flujo completo end-to-end).
- [ ] El botón "Sincronizar imágenes desde Drive" en Admin funciona (si ya configuraste Google Drive/Cloudinary).

## Limitaciones conocidas / próximos pasos

- **Catálogo de prueba**: la base de datos todavía tiene productos, marcas y categorías genéricas de prueba (ver hallazgo en `docs/SEO-ROADMAP.md`) — hay que reemplazarlas por el catálogo real desde el Admin antes de anunciar la tienda a clientes reales.
- **DataProtection keys efímeras**: ASP.NET Identity usa un sistema de claves para tokens (reset de contraseña, confirmación de email) que hoy se generan dentro del contenedor y se pierden en cada redeploy/reinicio. En la práctica esto invalida links de "recuperar contraseña" que estén pendientes justo cuando Render reinicia el servicio. No bloquea el lanzamiento, pero si esto genera quejas de usuarios, la solución es persistir esas claves (ej. en la misma base de datos, con `PersistKeysToDbContext`) — lo dejamos como mejora futura, no lo resolvimos ahora para no ampliar el alcance de este despliegue.
- **Logs a archivo**: Serilog escribe a `Logs/log-.txt` dentro del contenedor, que también es efímero. Render captura toda la salida por consola igualmente (ese sink ya existe), así que no se pierde visibilidad — solo el archivo local no persiste entre despliegues.
- **Plan pago de Render**: tanto el web service como Postgres en plan `starter` tienen costo (no es el free tier). Ajusta el plan en el dashboard de Render si prefieres empezar en `Free` mientras validas todo, sabiendo que el free de Postgres expira a los 30 días.

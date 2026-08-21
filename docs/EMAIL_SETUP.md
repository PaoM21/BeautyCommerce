# Correos transaccionales (Resend)

BeautyCommerce envía cuatro correos hoy:

1. **Confirmación de pedido** — automático, disparado por el patrón Outbox cuando se crea un pedido (`CheckoutCommandHandler` → `OutboxMessage` tipo `OrderCreated` → `OutboxProcessor` lo procesa cada 10 segundos y envía el correo).
2. **Restablecer contraseña** — disparado por `POST /api/auth/forgot-password`.
3. **Pedido enviado** — automático, disparado por el patrón Outbox cuando el admin cambia el estado del pedido a "Enviado" (`UpdateOrderStatusCommandHandler` → `OutboxMessage` tipo `OrderShipped` → `OutboxProcessor` lo procesa igual que `OrderCreated`). Incluye transportadora y número de guía si ya fueron cargados (`PUT /api/admin/orders/shipping`); si no, el correo avisa que la guía se compartirá pronto.
4. **Bienvenida / cuenta creada** — disparado de forma síncrona en `POST /api/auth/register` (mismo patrón que "olvidé mi contraseña": si Resend falla, no bloquea el registro, solo queda el error en logs).

Ambos usan [Resend](https://resend.com) como proveedor.

## Configuración

### 1. Cuenta de Resend

1. Crea una cuenta gratis en [resend.com](https://resend.com) (3.000 correos/mes gratis, sin tarjeta).
2. En el dashboard, ve a **API Keys** → **Create API Key**. Cópiala — solo se muestra una vez.

### 2. Remitente

Por ahora, mientras no haya dominio propio comprado, se usa el dominio de pruebas de Resend (`onboarding@resend.dev`) — no requiere ninguna verificación, pero:
- Los correos pueden llegar marcados como de un remitente genérico de Resend en vez de "BeautyCommerce".
- Resend limita el volumen/destinatarios en el dominio compartido (revisa sus límites vigentes).

**Cuando compren el dominio propio:**
1. En Resend → **Domains** → **Add Domain**, agrega el dominio.
2. Resend da 2-3 registros DNS (SPF, DKIM) para agregar en Cloudflare — una vez verificados (minutos a un par de horas), el dominio queda listo.
3. Cambia `Email:FromAddress` a algo como `pedidos@tudominio.com` — **no hay que tocar código**, es solo la variable de entorno.

### 3. Configurar las credenciales

Igual que con Google Drive/Cloudinary — **nunca en `appsettings.json`**, solo vía `dotnet user-secrets` en desarrollo o variables de entorno en producción:

```bash
dotnet user-secrets set "Email:ApiKey" "re_tu_api_key_de_resend"
```

En producción (Render), variable de entorno:
```
Email__ApiKey=re_tu_api_key_de_resend
```

`Email:FromAddress` y `Email:FromName` no son secretos — ya están en `appsettings.json` con el valor de prueba. `Frontend:BaseUrl` también debe configurarse (es la URL que se usa para armar el link de "restablecer contraseña" en el correo):

```bash
dotnet user-secrets set "Frontend:BaseUrl" "http://localhost:5173"
```

En producción, debe ser la URL real del frontend (Cloudflare Pages o el dominio final):
```
Frontend__BaseUrl=https://www.tudominio.com
```

## Si no se configura nada

El sistema no falla ni bloquea el checkout o el registro: si `Email:ApiKey` está vacío, `ResendEmailService` simplemente registra un warning en los logs y no envía nada — así que en desarrollo local, sin configurar Resend, todo el resto de la tienda sigue funcionando normal, solo no llegan correos.

## Plantillas

Las plantillas HTML viven en `backend/src/BeautyCommerce.Application/Common/Emails/EmailTemplates.cs` — son strings de HTML con estilos inline (necesario para que se vean bien en la mayoría de clientes de correo). Si se agregan más correos transaccionales (ej. confirmación de cuenta, notificación de envío), van ahí mismo siguiendo el mismo patrón.

## Próximos pasos posibles

- Mover el envío de "olvidé mi contraseña" y "bienvenida" al patrón Outbox también, si se necesita reintentos automáticos (hoy se envían de forma síncrona dentro del request, con el error capturado para no bloquear ni filtrar información al cliente).

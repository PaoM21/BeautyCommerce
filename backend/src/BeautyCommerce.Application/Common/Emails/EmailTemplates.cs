using System.Net;
using System.Text;
using BeautyCommerce.Domain.Entities;

namespace BeautyCommerce.Application.Common.Emails;

public static class EmailTemplates
{
    private const string Wine = "#5c1a28";
    private const string Beige = "#f6efe4";

    public static (string Subject, string Html) OrderConfirmation(Order order)
    {
        var subject = $"Confirmamos tu pedido {order.OrderNumber}";

        var itemsHtml = new StringBuilder();

        foreach (var item in order.Items)
        {
            var name = WebUtility.HtmlEncode(
                item.ProductVariant?.Product?.Name ?? "Producto");

            itemsHtml.Append($@"
                <tr>
                    <td style=""padding:10px 0;border-bottom:1px solid #e5e1dc;"">{name} × {item.Quantity}</td>
                    <td style=""padding:10px 0;border-bottom:1px solid #e5e1dc;text-align:right;"">${item.UnitPrice * item.Quantity:N0}</td>
                </tr>");
        }

        var recipientName = WebUtility.HtmlEncode(order.ShippingRecipientName);
        var addressLine = WebUtility.HtmlEncode(order.ShippingAddressLine);
        var city = WebUtility.HtmlEncode(order.ShippingCity);
        var department = WebUtility.HtmlEncode(order.ShippingDepartment);

        var html = $@"
<div style=""font-family:Arial,Helvetica,sans-serif;max-width:560px;margin:0 auto;color:#211d19;"">
    <div style=""background:{Wine};color:#fff;padding:24px;text-align:center;"">
        <h1 style=""margin:0;font-size:20px;font-weight:normal;letter-spacing:2px;"">BEAUTY COMMERCE</h1>
    </div>

    <div style=""padding:24px;background:{Beige};"">
        <h2 style=""margin-top:0;font-weight:normal;"">¡Gracias por tu compra, {recipientName}!</h2>
        <p>Tu pedido <strong>{order.OrderNumber}</strong> fue confirmado y ya lo estamos preparando.</p>
    </div>

    <div style=""padding:24px;"">
        <table style=""width:100%;border-collapse:collapse;font-size:14px;"">
            {itemsHtml}
            <tr><td style=""padding:10px 0;"">Subtotal</td><td style=""padding:10px 0;text-align:right;"">${order.SubTotal:N0}</td></tr>
            <tr><td style=""padding:10px 0;"">Envío</td><td style=""padding:10px 0;text-align:right;"">${order.ShippingCost:N0}</td></tr>
            <tr><td style=""padding:10px 0;font-weight:bold;"">Total</td><td style=""padding:10px 0;text-align:right;font-weight:bold;"">${order.Total:N0}</td></tr>
        </table>

        <h3 style=""font-weight:normal;margin-top:32px;"">Dirección de envío</h3>
        <p style=""color:#6f6a63;line-height:1.6;"">
            {recipientName}<br />
            {addressLine}<br />
            {city}, {department}
        </p>
    </div>

    <div style=""padding:16px 24px;text-align:center;color:#999;font-size:12px;"">
        BeautyCommerce · Envíos a toda Colombia
    </div>
</div>";

        return (subject, html);
    }

    public static (string Subject, string Html) PasswordReset(string resetUrl)
    {
        const string subject = "Restablece tu contraseña de BeautyCommerce";

        var html = $@"
<div style=""font-family:Arial,Helvetica,sans-serif;max-width:560px;margin:0 auto;color:#211d19;"">
    <div style=""background:{Wine};color:#fff;padding:24px;text-align:center;"">
        <h1 style=""margin:0;font-size:20px;font-weight:normal;letter-spacing:2px;"">BEAUTY COMMERCE</h1>
    </div>

    <div style=""padding:32px 24px;"">
        <h2 style=""font-weight:normal;"">Restablece tu contraseña</h2>
        <p>Recibimos una solicitud para restablecer tu contraseña. Si fuiste tú, haz clic en el siguiente botón. Este enlace vence en 1 hora.</p>

        <p style=""text-align:center;margin:32px 0;"">
            <a href=""{resetUrl}"" style=""background:{Wine};color:#fff;text-decoration:none;padding:14px 32px;display:inline-block;"">
                Restablecer contraseña
            </a>
        </p>

        <p style=""color:#6f6a63;font-size:13px;"">
            Si no solicitaste este cambio, puedes ignorar este correo — tu contraseña actual sigue siendo válida.
        </p>
    </div>
</div>";

        return (subject, html);
    }
}

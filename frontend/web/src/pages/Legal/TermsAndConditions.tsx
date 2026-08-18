// Borrador legal — pendiente de revisión por un abogado colombiano
// especializado en derecho del consumidor antes de publicar. Los campos
// resaltados en amarillo (className="placeholder") son datos reales del
// negocio que no se pueden inventar. Ver docs/LEGAL_PAGES_SETUP.md.
import LegalPageLayout from "./LegalPageLayout";

export default function TermsAndConditions() {
  return (
    <LegalPageLayout
      eyebrow="Legal"
      title="Términos y Condiciones"
      lastUpdated="18 de agosto de 2026"
    >
      <p>
        Estos Términos y Condiciones regulan el acceso y uso del sitio web
        de <span className="placeholder">[Razón social]</span> ("
        HALDY&CO ECOMMERCE", "nosotros"), identificada con NIT{" "}
        <span className="placeholder">[NIT]</span>, así como las compras
        realizadas a través de él. Al registrarte o realizar una compra,
        aceptas estos Términos y nuestra{" "}
        <a href="/tratamiento-de-datos-personales">
          Política de Tratamiento de Datos Personales
        </a>
        .
      </p>

      <h2>1. Quiénes somos</h2>
      <p>
        HALDY&CO ECOMMERCE es una tienda en línea que comercializa productos de
        maquillaje, cuidado de la piel, cabello, labios y uñas, con envíos
        a todo el territorio colombiano. Domicilio:{" "}
        <span className="placeholder">[Ciudad, dirección]</span>. Contacto:{" "}
        <span className="placeholder">[correo@dominio.com]</span>.
      </p>

      <h2>2. Productos y precios</h2>
      <p>
        Los precios se muestran en pesos colombianos (COP) e incluyen los
        impuestos aplicables, salvo que se indique lo contrario. Nos
        reservamos el derecho de modificar precios y disponibilidad sin
        previo aviso; el precio válido para una compra es el vigente al
        momento de confirmar el pedido. Trabajamos únicamente con
        distribuidores autorizados para garantizar la autenticidad de los
        productos.
      </p>

      <h2>3. Proceso de compra y medios de pago</h2>
      <p>
        La compra se confirma una vez el pago es aprobado por nuestra
        pasarela de pagos (
        <span className="placeholder">
          [Wompi / Bold — confirmar cuál se integró finalmente]
        </span>
        ), la cual procesa las transacciones bajo sus propios términos y
        estándares de seguridad. HALDY&CO ECOMMERCE no almacena datos de
        tarjetas de crédito o débito.
      </p>

      <h2>4. Envíos</h2>
      <p>
        Realizamos envíos a todo el territorio nacional a través de
        transportadoras aliadas (Envía, Interrapidísimo u otras que se
        contraten). El costo de envío se calcula según la ciudad de
        destino y se muestra antes de confirmar el pedido. Los tiempos de
        entrega son estimados y pueden variar según la cobertura de la
        transportadora en cada municipio.
      </p>

      <h2>5. Derecho de retracto</h2>
      <p>
        De acuerdo con el artículo 47 de la Ley 1480 de 2011 (Estatuto del
        Consumidor), al ser esta una venta a distancia, tienes derecho a
        retractarte de tu compra dentro de los <strong>cinco (5) días hábiles</strong>{" "}
        siguientes a la entrega del producto, sin necesidad de justificar
        tu decisión, siempre que el producto no haya sido usado y conserve
        su empaque original. Para ejercer este derecho, contáctanos en{" "}
        <span className="placeholder">[correo@dominio.com]</span>.{" "}
        <span className="placeholder">
          [Definir aquí el procedimiento operativo real: quién asume el
          costo de devolución del envío, plazo de reembolso, etc.]
        </span>
      </p>

      <h2>6. Garantía legal</h2>
      <p>
        Todos los productos cuentan con la garantía legal mínima
        establecida por el Estatuto del Consumidor colombiano frente a
        defectos de calidad, idoneidad o seguridad. Si recibes un producto
        defectuoso o distinto al solicitado, contáctanos para gestionar el
        cambio, reparación o devolución según corresponda.
      </p>

      <h2>7. Cuenta de usuario</h2>
      <p>
        Eres responsable de mantener la confidencialidad de tu contraseña
        y de toda actividad realizada desde tu cuenta. Debes notificarnos
        de inmediato cualquier uso no autorizado de tu cuenta.
      </p>

      <h2>8. Propiedad intelectual</h2>
      <p>
        Todos los contenidos del sitio (textos, imágenes, logotipos,
        diseño) son propiedad de HALDY&CO ECOMMERCE o de sus licenciantes y
        están protegidos por las normas de propiedad intelectual
        aplicables. Su reproducción o uso no autorizado está prohibido.
      </p>

      <h2>9. Limitación de responsabilidad</h2>
      <p>
        HALDY&CO ECOMMERCE no será responsable por retrasos o incumplimientos
        atribuibles a terceros (transportadoras, pasarela de pagos) ni por
        causas de fuerza mayor o caso fortuito, sin perjuicio de los
        derechos que la ley colombiana reconoce al consumidor.
      </p>

      <h2>10. Modificaciones</h2>
      <p>
        Podemos actualizar estos Términos en cualquier momento; los
        cambios aplican a las compras realizadas después de su
        publicación en este sitio.
      </p>

      <h2>11. Ley aplicable y jurisdicción</h2>
      <p>
        Estos Términos se rigen por las leyes de la República de Colombia.
        Cualquier controversia se resolverá ante los jueces competentes
        de <span className="placeholder">[ciudad de domicilio]</span>, sin
        perjuicio de los mecanismos de protección al consumidor
        disponibles ante la Superintendencia de Industria y Comercio.
      </p>
    </LegalPageLayout>
  );
}

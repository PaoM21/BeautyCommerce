// Borrador legal — pendiente de revisión por un abogado colombiano
// especializado en protección de datos/derecho del consumidor antes de
// publicar. Los campos resaltados en amarillo (className="placeholder")
// son datos reales del negocio que no se pueden inventar: deben
// completarse (y ese estilo removerse) antes de salir a producción.
// Ver docs/LEGAL_PAGES_SETUP.md.
import LegalPageLayout from "./LegalPageLayout";

export default function DataProcessingPolicy() {
  return (
    <LegalPageLayout
      eyebrow="Legal"
      title="Política de Tratamiento de Datos Personales"
      lastUpdated="18 de agosto de 2026"
    >
      <p>
        Esta Política de Tratamiento de Datos Personales ("Política") regula
        la forma en que{" "}
        <span className="placeholder">[Razón social de la empresa]</span>,
        identificada con NIT{" "}
        <span className="placeholder">[Número de NIT]</span>, con domicilio
        en <span className="placeholder">[Ciudad, dirección]</span>, Colombia
        ("HALDY&CO ECOMMERCE", "nosotros"), recolecta, almacena, usa, circula y
        protege los datos personales de los usuarios y clientes de este
        sitio web, en cumplimiento de la Ley 1581 de 2012, el Decreto 1377
        de 2013 y demás normas que las modifiquen o complementen (en
        conjunto, el "Régimen de Protección de Datos Personales" de
        Colombia).
      </p>

      <h2>1. Responsable del tratamiento</h2>
      <p>
        <strong>Razón social:</strong>{" "}
        <span className="placeholder">[Razón social]</span>
        <br />
        <strong>NIT:</strong> <span className="placeholder">[NIT]</span>
        <br />
        <strong>Domicilio:</strong>{" "}
        <span className="placeholder">[Dirección completa]</span>
        <br />
        <strong>Correo de contacto para temas de datos personales:</strong>{" "}
        <span className="placeholder">
          [correo@dominio.com — debe ser un canal real y monitoreado]
        </span>
        <br />
        <strong>Teléfono:</strong>{" "}
        <span className="placeholder">[Número de contacto]</span>
      </p>

      <h2>2. Datos personales que recolectamos</h2>
      <p>Según cómo interactúes con nuestro sitio, podemos recolectar:</p>
      <ul>
        <li>
          <strong>Datos de identificación y contacto:</strong> nombre,
          apellido, correo electrónico, número de teléfono.
        </li>
        <li>
          <strong>Datos de envío:</strong> dirección, ciudad, departamento,
          nombre y teléfono de quien recibe el pedido.
        </li>
        <li>
          <strong>Datos de la cuenta:</strong> historial de pedidos, lista
          de favoritos, puntos del programa de lealtad.
        </li>
        <li>
          <strong>Datos de pago:</strong> el sitio no almacena números de
          tarjeta ni datos financieros — estos son procesados directamente
          por nuestra pasarela de pagos, sujeta a su propia política de
          privacidad.
        </li>
        <li>
          <strong>Datos técnicos:</strong> dirección IP, tipo de
          dispositivo/navegador y páginas visitadas, recolectados mediante
          cookies y tecnologías similares (ver sección 8).
        </li>
      </ul>

      <h2>3. Finalidades del tratamiento</h2>
      <p>Usamos tus datos personales para:</p>
      <ul>
        <li>Crear y administrar tu cuenta de usuario.</li>
        <li>
          Procesar, facturar, empacar y despachar los pedidos que realices.
        </li>
        <li>
          Compartir la información de envío estrictamente necesaria con
          las transportadoras encargadas de la entrega.
        </li>
        <li>
          Gestionar tu solicitud de recuperación de contraseña y otras
          comunicaciones transaccionales relacionadas con tu cuenta o tus
          pedidos.
        </li>
        <li>
          Administrar el programa de puntos/lealtad, si participas en él.
        </li>
        <li>
          Atender solicitudes, quejas, reclamos y garantías (PQR) y dar
          soporte al cliente.
        </li>
        <li>
          Dar cumplimiento a obligaciones legales, contables, tributarias y
          regulatorias.
        </li>
        <li>
          <span className="placeholder">
            [Si van a enviar promociones/marketing por correo o WhatsApp,
            debe agregarse aquí como finalidad explícita, y requiere
            autorización separada y verificable — no debe asumirse incluida
            por defecto]
          </span>
        </li>
      </ul>

      <h2>4. Transferencia y transmisión a terceros</h2>
      <p>
        Para cumplir con las finalidades anteriores, compartimos datos
        estrictamente necesarios con los siguientes terceros, que actúan
        como encargados o responsables según el caso:
      </p>
      <ul>
        <li>
          <strong>Pasarela de pagos</strong> (ej. Wompi o Bold) — para
          procesar el pago de tu pedido.
        </li>
        <li>
          <strong>Transportadoras</strong> (ej. Envía, Interrapidísimo u
          otras que se contraten) — para la entrega física de tu pedido.
        </li>
        <li>
          <strong>Proveedor de correo transaccional</strong> (Resend) — para
          enviarte confirmaciones de pedido y correos de recuperación de
          contraseña. Este proveedor puede procesar datos en servidores
          fuera de Colombia.
        </li>
        <li>
          <strong>Proveedores de infraestructura y alojamiento de imágenes</strong>{" "}
          (Cloudinary, Google Drive) — usados internamente para gestionar
          fotografías de producto, no para datos personales de clientes.
        </li>
      </ul>
      <p>
        Cuando estos proveedores procesan datos fuera de Colombia, dicha
        transferencia internacional se realiza conforme a lo permitido por
        el Régimen de Protección de Datos Personales.
      </p>

      <h2>5. Derechos de los titulares de datos personales</h2>
      <p>Como titular de tus datos personales, tienes derecho a:</p>
      <ul>
        <li>
          Conocer, actualizar y rectificar tus datos personales.
        </li>
        <li>
          Solicitar prueba de la autorización otorgada para el tratamiento
          de tus datos.
        </li>
        <li>
          Ser informado, previa solicitud, sobre el uso que se ha dado a
          tus datos personales.
        </li>
        <li>
          Presentar quejas ante la Superintendencia de Industria y Comercio
          (SIC) por infracciones al Régimen de Protección de Datos
          Personales.
        </li>
        <li>
          Revocar la autorización y/o solicitar la supresión del dato,
          cuando no exista un deber legal o contractual que impida su
          eliminación.
        </li>
        <li>Acceder de forma gratuita a tus datos personales.</li>
      </ul>

      <h2>6. Procedimiento para ejercer tus derechos</h2>
      <p>
        Puedes ejercer cualquiera de los derechos anteriores enviando una
        solicitud a{" "}
        <span className="placeholder">
          [correo de contacto de datos personales]
        </span>{" "}
        indicando tu nombre completo, el derecho que deseas ejercer y una
        descripción clara de tu solicitud. Responderemos dentro de los
        términos establecidos por la ley (consultas: máximo 10 días
        hábiles, prorrogables 5 días hábiles adicionales; reclamos: máximo
        15 días hábiles, prorrogables 8 días hábiles adicionales).
      </p>

      <h2>7. Seguridad de la información</h2>
      <p>
        Implementamos medidas técnicas, humanas y administrativas
        razonables para proteger tus datos personales contra acceso no
        autorizado, pérdida, alteración o uso indebido, incluyendo el uso
        de conexiones cifradas (HTTPS) y controles de acceso a nuestros
        sistemas.
      </p>

      <h2>8. Uso de cookies</h2>
      <p>
        Este sitio puede usar cookies y tecnologías similares para
        recordar tu sesión, mantener tu carrito de compras y, si se
        habilita en el futuro, para análisis de tráfico (por ejemplo,
        Google Analytics). Puedes configurar tu navegador para rechazar
        cookies, aunque esto puede afectar el funcionamiento del sitio.
      </p>

      <h2>9. Vigencia</h2>
      <p>
        Esta Política rige a partir de su fecha de publicación y
        permanecerá vigente mientras se mantenga el tratamiento de datos
        personales, ajustándose a cualquier cambio normativo o en nuestras
        prácticas. Los cambios sustanciales se comunicarán a través de este
        mismo sitio.
      </p>
    </LegalPageLayout>
  );
}

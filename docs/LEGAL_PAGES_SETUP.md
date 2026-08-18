# Páginas legales — qué falta antes de publicar

Se agregaron dos documentos legales obligatorios para una tienda en línea en Colombia:

- **Términos y Condiciones** — `/terminos-y-condiciones` ([frontend/web/src/pages/Legal/TermsAndConditions.tsx](../frontend/web/src/pages/Legal/TermsAndConditions.tsx))
- **Política de Tratamiento de Datos Personales** — `/tratamiento-de-datos-personales` ([frontend/web/src/pages/Legal/DataProcessingPolicy.tsx](../frontend/web/src/pages/Legal/DataProcessingPolicy.tsx)), exigida por la Ley 1581 de 2012 (Habeas Data) para cualquier negocio que recolecte datos personales de usuarios.

**Importante: esto es un borrador, no asesoría legal.** No soy abogado y este contenido no reemplaza una revisión por un abogado colombiano especializado en protección de datos y derecho del consumidor. Está estructurado siguiendo los requisitos mínimos que exige la ley (identidad del responsable, finalidades, derechos ARCO, procedimiento para ejercerlos, transferencia a terceros, etc.), pero **debe revisarse antes de publicarse a clientes reales**.

## 1. Datos que debes completar tú (no los puedo inventar)

En ambos documentos hay texto resaltado en amarillo con el formato `[Así]` — son los campos que dependen de información real del negocio:

| Campo | Dónde se usa |
|---|---|
| Razón social de la empresa | Ambos documentos |
| NIT | Ambos documentos |
| Dirección/domicilio | Ambos documentos |
| Correo de contacto para temas de datos personales | Política de Datos, para que los usuarios ejerzan sus derechos |
| Ciudad de domicilio (para jurisdicción) | Términos y Condiciones |
| Confirmar si la pasarela final es Wompi o Bold | Términos y Condiciones, sección de pago |
| Procedimiento operativo real del derecho de retracto (quién paga el envío de devolución, plazo de reembolso) | Términos y Condiciones |

Una vez tengas esta información, edita directamente los archivos `.tsx` mencionados arriba y quita la clase `className="placeholder"` de cada campo ya completado (esa clase es la que los resalta en amarillo — sirve para que sea imposible no notarlos mientras falten).

## 2. Verificar si deben registrarse en el RNBD

Dependiendo del tamaño de la empresa y el volumen de datos que manejen, es posible que estén obligados a inscribir sus bases de datos en el **Registro Nacional de Bases de Datos (RNBD)** de la Superintendencia de Industria y Comercio (SIC). Esto es una obligación aparte de tener la política publicada en el sitio — confírmenlo con su abogado.

## 3. Lo que sí quedó implementado

- Ambas páginas, con las secciones mínimas exigidas por la Ley 1581/2012 y referencias correctas a la Ley 1480/2011 (derecho de retracto de 5 días hábiles para ventas a distancia — esto es un mínimo legal real, no un valor que haya inventado).
- Enlaces a ambos documentos en el pie de página del sitio.
- Checkbox obligatorio en el registro de usuarios ("He leído y acepto los Términos y Condiciones y la Política de Tratamiento de Datos Personales") — el botón de crear cuenta queda deshabilitado hasta marcarlo.

## 4. Recomendación pendiente (no implementada todavía)

Hoy el checkbox de aceptación **solo se valida en el frontend**. Si alguien llama directamente a la API de registro sin pasar por el formulario, no hay nada que le impida crear una cuenta sin haber aceptado nada, ni queda registrado en la base de datos *cuándo* un usuario aceptó — lo cual es relevante porque la ley exige poder **probar** que la autorización se dio.

Para cerrar esto bien haría falta (no lo hice ahora para no ampliar el alcance de este cambio sin que ustedes lo pidieran explícitamente):
- Un campo `AcceptedTermsAt` (fecha) en el usuario.
- Que el backend rechace el registro si no viene la confirmación, en vez de confiar solo en el frontend.

Es una mejora real de cumplimiento, pero es un cambio de backend (migración de base de datos incluida) — avísenme cuando quieran que lo hagamos.

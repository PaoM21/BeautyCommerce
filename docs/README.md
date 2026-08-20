# Documentación de BeautyCommerce (Haldy&Co)

Este es el sistema de documentación del proyecto: gobierno, arquitectura,
producto, UX/UI, ingeniería, operación y calidad. El objetivo es que alguien
que no participó en la construcción del sistema pueda entenderlo,
mantenerlo, evolucionarlo y auditarlo sin depender de quienes lo escribieron.

## 🔴 Alerta activa: CI de backend roto (defecto de pipeline confirmado)

**Estado actual: NO CORREGIDO.** No es un defecto funcional de
Haldy&Co/BeautyCommerce — el producto funciona correctamente, verificado
localmente. Es un defecto de la infraestructura de calidad: hoy no hay
garantía automatizada y continua de que la suite crítica de tests siga
pasando en cada cambio.

| Campo | Detalle |
|---|---|
| Impacto | Release Readiness (ver `00-Governance/PROJECT-CHARTER.md`) |
| Riesgo | Los tests críticos de integración (concurrencia, constraints, idempotencia, unicidad) no se ejecutan correctamente en CI — la única verificación real hoy es local, manual, no continua |
| Causa | `dotnet test` corre sin filtro en `.github/workflows/backend-ci.yml` sobre un runner sin servicio de PostgreSQL configurado |
| Evidencia | 22 de las últimas 26 ejecuciones de `backend-ci.yml` fallaron (`gh run list`); la más reciente falla con `Npgsql.NpgsqlException: Failed to connect to 127.0.0.1:5432`; el propio código ya lo anticipaba — comentario explícito en `Concurrency/InventoryConcurrencyTests.cs:16-19` sobre la necesidad de excluir estos tests de un CI "que no tiene Postgres configurado hoy" |
| Detalle completo | [04-Engineering/TESTING-STRATEGY.md](04-Engineering/TESTING-STRATEGY.md) → "Regression testing policy → Regresión de CI, no solo de producto" |
| Decisión pendiente | Reparar CI (agregar servicio de PostgreSQL al workflow, o aplicar `--filter Category!=Integration` a un job separado) antes de considerar el pipeline confiable. **No se corrige mientras el proyecto siga en modo documentación/reconocimiento** — ver la regla de no convertir automáticamente un hallazgo en implementación. |

## Nombre técnico vs. marca

El repositorio, la solución .NET y los namespaces se llaman **BeautyCommerce**
(nombre técnico, histórico, usado en todo el código). La marca visible para
el cliente final — la que aparece en el header, el footer y las
comunicaciones de la tienda — es **Haldy&Co**. Ambos nombres son correctos;
se usan en contextos distintos y así se mantienen en esta documentación: el
código y la infraestructura se describen como BeautyCommerce, el producto de
cara al cliente se describe como Haldy&Co.

## Cómo está organizada

```
docs/
│
├── 00-Governance/     Contrato del proyecto: qué es, alcance, decisiones,
│                       principios de ingeniería, riesgos, glosario
├── 01-Architecture/    Arquitectura de sistema, dominio, datos, API,
│                       seguridad, integraciones, observabilidad
├── 02-Product/         Visión de producto, personas, journeys, reglas de
│                       negocio, requisitos, roadmap
├── 03-UX-UI/           Principios de UX, design system, flujos, UX de
│                       admin y de cliente, accesibilidad
├── 04-Engineering/     Estándares de código, testing, manejo de errores,
│                       caching
├── 05-Operations/      Entornos, despliegue, configuración, respuesta a
│                       incidentes
├── 06-Quality/         Estrategia de calidad, E2E, regresión, seguridad,
│                       performance, auditorías de hardening
└── 07-Features/        Diseño de capacidades nuevas antes de construirlas
                        (variantes, media, pagos reales, etc.)
```

Esta estructura se está construyendo por fases (ver el estado de cada
documento más abajo); no todos los directorios tienen contenido todavía. La
regla de fondo, para no caer en "documentación por documentación": **cada
documento existe porque protege una decisión, reduce una ambigüedad, o
permite que otra persona entienda/implemente/verifique el sistema sin
depender de memoria oral.** Si un documento no cumple eso, no se crea.

## Convención obligatoria: etiquetar la naturaleza de cada afirmación

Cada afirmación relevante en esta documentación debe poder clasificarse en
una de estas seis categorías. Cuando no sea obvia por el contexto, se marca
explícitamente:

| Etiqueta | Significa | Ejemplo |
|---|---|---|
| **HECHO** | Comprobado en código, base de datos o ejecución real, con referencia verificable (archivo, línea, test, query). | "`TransactionBehavior` omite el rollback si el request implementa `INotTransactional` ([TransactionBehavior.cs:28](../backend/src/BeautyCommerce.Application/Common/Behaviors/TransactionBehavior.cs#L28))." |
| **DECISIÓN** | Algo que el equipo decidió conscientemente, con razón registrada. | "Se decidió no cachear la wishlist (ver ADR-009)." |
| **INFERENCIA** | Interpretación arquitectónica razonada a partir de evidencia técnica (código, tests, configuración). Es una deducción, no una hipótesis de negocio. | "El dominio parece asumir una única moneda (COP); no hay soporte multi-moneda en el modelo." |
| **SUPUESTO** | Una hipótesis de producto o negocio que todavía necesita confirmación de quien tiene esa autoridad — no se dedujo del código, se propuso a falta de una fuente declarada. Distinto de INFERENCIA: una inferencia se resuelve leyendo más código; un supuesto solo se resuelve preguntándole a negocio/producto. | "SUPUESTO: el cliente podrá comprar sin necesidad de asesoría humana." |
| **BACKLOG** | Existe y se conoce, pero se decidió no abordarlo todavía. No es un defecto. | "Gestión avanzada de variantes post-creación: no implementada." |
| **RIESGO** | Podría afectar al sistema aunque hoy no sea un defecto activo. No se asciende a defecto sin evidencia de que produce un problema observable. | "Las query keys de productos admin están repetidas como literales en tres archivos — riesgo de mantenibilidad, no un bug confirmado." |

Esta distinción existe para no repetir un error real que cometimos antes de
tener este sistema: mezclar "hay código sin usar" (BACKLOG o RIESGO) con
"hay un bug" (defecto activo). Ver
[06-Quality/AUDIT-PUNTOS-1-5.md](06-Quality/AUDIT-PUNTOS-1-5.md) para el
caso real. La misma disciplina aplica a SUPUESTO vs. INFERENCIA: leer un
SUPUESTO como si fuera una decisión ya tomada es exactamente el tipo de
error que este sistema existe para prevenir.

## Estado de la columna vertebral

| Documento | Estado |
|---|---|
| `00-Governance/PROJECT-CHARTER.md` | ✅ |
| `00-Governance/SCOPE.md` | ✅ |
| `01-Architecture/SYSTEM-ARCHITECTURE.md` | ✅ |
| `01-Architecture/DOMAIN-MODEL.md` | ✅ |
| `01-Architecture/DATA-ARCHITECTURE.md` | ✅ |
| `01-Architecture/API-ARCHITECTURE.md` | ✅ |
| `01-Architecture/SECURITY-ARCHITECTURE.md` | ✅ |
| `03-UX-UI/DESIGN-SYSTEM.md` | ⏳ pendiente |
| `04-Engineering/TESTING-STRATEGY.md` | ✅ |
| `04-Engineering/QUALITY-STRATEGY.md` (con Quality Gate formal) | ✅ |
| `04-Engineering/CI-CD.md` | ✅ |
| `04-Engineering/ERROR-HANDLING.md` | ✅ |
| `04-Engineering/OBSERVABILITY.md` | ✅ |
| `06-Quality/AUDIT-PUNTOS-1-5.md` | ⏳ pendiente |
| `00-Governance/DECISIONS/` (ADR-001 a ADR-007) | ✅ |
| `00-Governance/RISK-REGISTER.md` | ⏳ pendiente |

Los documentos de `02-Product/`, `03-UX-UI/` (más allá de Design System) y
`05-Operations/` (más allá de los archivos de setup que ya existían) se
abordarán después de cerrar esta columna vertebral, según la prioridad
P0–P3 acordada (ver `PROJECT-CHARTER.md`).

## Regla de secuencia: no se abre una feature nueva sin su documentación de arquitectura

Antes de implementar cualquier capacidad nueva no trivial (ejemplo: gestión
de variantes de producto, gestión de media, integración de pago real), debe
existir primero un documento en `07-Features/` que la atraviese con esta
trazabilidad:

```
Product Requirement → Business Rule → Domain Model → Use Case →
API Contract → Database → Frontend State → UX Flow → UI →
Validation → Automated Test → E2E → Observability → Security
```

Esto evita repetir el patrón de tratar una capacidad nueva como "agregar
unos campos a un formulario" cuando en realidad toca dominio, base de
datos, autorización y varias capas del frontend a la vez.

## Documentos de setup ya existentes

Estos se escribieron antes de este sistema y siguen vigentes; se
reorganizarán bajo `05-Operations/` en una fase posterior sin perder su
contenido:

- [DEPLOYMENT.md](DEPLOYMENT.md) — despliegue en Render.
- [EMAIL_SETUP.md](EMAIL_SETUP.md) — configuración de envío de email (Resend).
- [GOOGLE_DRIVE_IMAGE_SYNC.md](GOOGLE_DRIVE_IMAGE_SYNC.md) — sincronización de imágenes de producto.
- [LEGAL_PAGES_SETUP.md](LEGAL_PAGES_SETUP.md) — páginas legales.
- [SEO-ROADMAP.md](SEO-ROADMAP.md) — trabajo de SEO hecho y pendiente.

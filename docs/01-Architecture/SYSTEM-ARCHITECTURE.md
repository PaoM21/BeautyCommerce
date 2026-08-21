# System Architecture — BeautyCommerce (Haldy&Co)

Este documento describe cómo está construido el sistema y, más importante,
**por qué** — no solo el diagrama de capas sino las decisiones de pipeline
que determinan el comportamiento real de cada request. Todo lo marcado
**HECHO** fue verificado leyendo el código citado, no inferido de
convenciones típicas de Clean Architecture.

## Vista de contenedores

```
                    ┌──────────────────────────┐
                    │   Browser                │
                    │   React 19 + TypeScript  │
                    │   (Cloudflare Pages)      │
                    └──────────┬───────────────┘
                               │ HTTPS / JSON
                               ▼
                    ┌──────────────────────────┐
                    │   ASP.NET Core 9 API      │
                    │   (Render, Docker)         │
                    └──────────┬───────────────┘
                               │
             ┌─────────────────┼─────────────────┐
             ▼                 ▼                  ▼
        Application         Domain          Infrastructure
       (MediatR CQRS)   (entidades puras)  (EF Core, Identity,
                                             email, Cloudinary,
                                             Google Drive)
             └────────────────┬──────────────────┘
                               ▼
                    ┌──────────────────────────┐
                    │   PostgreSQL (Render)     │
                    └──────────────────────────┘
```

**HECHO**, verificado en `docs/DEPLOYMENT.md`: el frontend se despliega en
Cloudflare Pages (estático) y el backend + base de datos en Render, como dos
despliegues independientes conectados por CORS y una URL de API configurada
por variable de entorno (`VITE_API_URL`). No es un monolito desplegado
junto — son dos superficies de despliegue separadas.

## Backend: capas y dirección de dependencias

**HECHO**, verificado directamente en los 4 archivos `.csproj` del
backend (no inferido de la convención "Clean Architecture" — confirmado
project por project):

```
BeautyCommerce.Domain           → (sin referencias a otros proyectos)
BeautyCommerce.Application      → Domain
BeautyCommerce.Infrastructure   → Application, Domain
BeautyCommerce.API              → Application, Infrastructure
```

`Domain.csproj` no tiene ningún `<ProjectReference>` — es el único proyecto
sin dependencias internas. Esto es lo que hace la arquitectura "limpia" en
sentido estricto: el modelo de dominio no puede importar EF Core, ASP.NET
Identity, ni ningún detalle de infraestructura, porque el compilador no lo
permite (no hay referencia al assembly). Ver `01-Architecture/DOMAIN-MODEL.md`
para lo que vive en esa capa.

Nota importante para quien lea esto esperando un dominio "rico": **todas las
entidades de `Domain/Entities/` son property bags** — ninguna tiene un
método con lógica de negocio, guard clause, ni invariante encapsulado (por
ejemplo, no existe un método `variant.DecreaseStock(n)` que impida un stock
negativo). Los invariantes de negocio (stock ≥ 0, precio ≥ 0, cantidad > 0,
etc.) están implementados como **check constraints de PostgreSQL**,
aplicados vía la configuración de EF Core de cada entidad — la base de
datos es la autoridad final, no el objeto de dominio. Esto es consistente
con una lección aprendida durante el hardening de Puntos 1–5 (no confiar
solo en validaciones previas de aplicación para invariantes críticos — ver
`06-Quality/AUDIT-PUNTOS-1-5.md`), pero significa que este es un **dominio
anémico** en el sentido de Fowler, no un modelo de dominio rico con
comportamiento. Se documenta como **INFERENCIA** arquitectónica neutral —
es una elección válida para un sistema CQRS+EF Core, no un defecto, pero
alguien diseñando la capacidad de variantes (`07-Features/PRODUCT-VARIANTS.md`)
debe decidir conscientemente si mantiene este patrón o introduce invariantes
en el dominio.

## Patrón de aplicación: CQRS con MediatR

**HECHO.** Cada caso de uso es un `Command` o `Query` de MediatR con su
handler dedicado, bajo `Application/Features/<Dominio>/Commands|Queries/`.
No hay una capa de "servicios" genérica de negocio — la unidad de trabajo es
el caso de uso.

### El pipeline real (no el que uno asumiría)

**HECHO**, verificado leyendo el registro de comportamientos en
`BeautyCommerce.API/Program.cs:57-60` y `:105-109`, y trazando cómo MediatR
14.2.0 resuelve `IEnumerable<IPipelineBehavior<,>>` (registro
`AddTransient` + `AddOpenBehavior` se acumulan en el mismo contenedor de DI,
en orden de registro; MediatR ejecuta en ese mismo orden — el primero
registrado es el que envuelve a todos los demás). El orden real de
ejecución para cualquier request es:

```
LoggingBehavior → PerformanceBehavior → TransactionBehavior →
CachingBehavior → ValidationBehavior → Handler
```

Esto tiene una consecuencia arquitectónica que vale la pena marcar como
**RIESGO**, no como defecto (no hay evidencia de que cause un problema
observable, pero es un orden contraintuitivo): **`TransactionBehavior` abre
una transacción de base de datos antes de que `ValidationBehavior` corra**.
Si un comando falla validación, ya se pagó el costo de abrir una
transacción (que luego hace rollback correctamente — `ValidationException`
está en la lista de excepciones esperadas de `TransactionBehavior`, así que
no hay riesgo de datos corruptos, solo de eficiencia).

### Qué hace cada behavior (verificado línea por línea, no por el nombre)

| Behavior | Actúa sobre | Qué hace realmente |
|---|---|---|
| `LoggingBehavior` | Todo request | Loguea "Handling"/"Handled" en `Information`. Sin correlation ID ni payload. |
| `PerformanceBehavior` | Todo request | Mide con `Stopwatch`; solo loguea `Warning` si supera 500ms hardcodeados. No aborta ni reintenta. |
| `TransactionBehavior` | Requests cuyo **nombre de tipo termina en `"Command"`** y no implementan `INotTransactional` | Abre transacción, ejecuta, `SaveChangesAsync`, commit; rollback + log en excepción. |
| `CachingBehavior` | Requests cuyo **nombre de tipo termina en `"Query"`**, excepto los de los namespaces `ShoppingCart`, `Dashboard`, `Wishlist`, `Loyalty` (excluidos a propósito) | Cachea 5 minutos en `ICacheService` (in-memory), con clave = tipo + usuario + request serializado. |
| `ValidationBehavior` | Requests con al menos un `IValidator<T>` registrado | Corre todos los validadores en paralelo, lanza `ValidationException` si falla alguno. No-op si no hay validadores. |

**RIESGO documentado, no defecto:** tanto `TransactionBehavior` como
`CachingBehavior` deciden si actuar **por el sufijo del nombre del tipo**
(`"Command"` / `"Query"`), no por una interfaz marcadora explícita (como sí
existe `INotTransactional` para el caso de excepción). Es un contrato
implícito: si alguien crea una clase `Command` que no debería ser
transaccional sin heredar `INotTransactional`, o nombra algo `...Query` que
en realidad muta estado, el comportamiento se activa o desactiva
silenciosamente según el nombre. Funciona hoy porque la convención se ha
respetado, pero es frágil frente a errores humanos futuros.

**HECHO, no defecto:** la exclusión de `ShoppingCart`/`Dashboard`/`Wishlist`/
`Loyalty` del caching es una **DECISIÓN** deliberada (consistente con el
registro histórico del proyecto: "Caché ⏭️ Omitido deliberadamente"), no un
descuido.

### Doble validación en el camino HTTP estándar

**HECHO**, verificado con `FluentValidation.AspNetCore` 11.3.1 (auto-validación
de ASP.NET Core vía `[ApiController]`/ModelState) + `ValidationBehavior` de
MediatR coexistiendo. Para el camino más común — una acción de controller
cuyo parámetro `[FromBody]` es exactamente el tipo del `Command` que además
tiene un `IValidator<T>` registrado — **la validación ocurre dos veces**: la
auto-validación de ASP.NET Core rechaza con 400 antes de que la acción
siquiera se ejecute (por lo que `IMediator.Send()` nunca llega a correr con
un comando inválido en ese camino), y `ValidationBehavior` vuelve a validar
si de algún modo el flujo llega hasta el handler por otra vía (dispatch
interno, tests, un parámetro de acción que no coincide 1:1 con el tipo
validado). No es un defecto — es redundancia de defensa en profundidad —
pero es doble trabajo en el camino feliz y vale la pena que quien mantenga
el sistema lo sepa en vez de asumir que solo hay un punto de validación.

## Frontend: capas y patrón de estado

**HECHO**, verificado en `frontend/web/src/`:

- **React 19 + TypeScript + Vite**, MUI como sistema de componentes,
  React Router 7 para ruteo.
- **Estado de servidor**: TanStack Query — cada página/hook declara su
  propio `queryKey` (ver la nota de RIESGO sobre keys sin consolidar en
  `00-Governance/SCOPE.md`).
- **Estado de cliente**: Zustand, usado para sesión/autenticación
  (`store/authStore.ts` — token JWT decodificado localmente para extraer el
  rol, persistido en `localStorage`).
- **Capa de servicios**: un archivo `services/<dominio>Service.ts` por
  dominio, todos sobre un cliente Axios común — no hay generación
  automática de cliente desde el contrato OpenAPI, los servicios están
  escritos a mano.
- **Layouts separados**: `MainLayout` (tienda pública) y `AdminLayout`
  (panel `/admin/*`), con las rutas de `/admin/*` cargadas con
  `React.lazy()` detrás de un único `Suspense` para no enviar el bundle del
  panel administrativo a cada visitante público.

## Middleware HTTP (orden real)

**HECHO**, verificado en `Program.cs:229-241`:

```
UseExceptionHandler → UseSerilogRequestLogging → UseStaticFiles →
UseRouting → UseCors → UseAuthentication → UseAuthorization → Endpoints
```

## Persistencia y convenciones transversales

**HECHO**, verificado en `ApplicationDbContext.cs`:

- **Soft delete global**: toda entidad que hereda `BaseEntity` recibe un
  `HasQueryFilter(e => !e.IsDeleted)` aplicado reflectivamente sobre todo el
  modelo — nadie tiene que recordar agregarlo entidad por entidad.
- **Auditoría automática**: `SaveChangesAsync` está sobrescrito para
  timestampear `CreatedAt`/`CreatedBy` en altas y `UpdatedAt`/`UpdatedBy` en
  modificaciones; un soft-delete se implementa como una actualización
  ordinaria (`IsDeleted = true`), no como un método de dominio dedicado.
- **`OutboxMessage` es la excepción**: no hereda `BaseEntity`, no tiene
  campos de auditoría ni soft delete, y no tiene archivo de configuración
  EF propio — se mapea por convención.

## Despliegue y riesgos operativos conocidos

**HECHO**, ya documentado en `docs/DEPLOYMENT.md` y traído aquí porque son
hallazgos de arquitectura, no solo de operación:

- **RIESGO — claves de DataProtection efímeras**: ASP.NET Identity genera
  sus claves de token (reset de contraseña, confirmación) dentro del
  contenedor; se pierden en cada redeploy/reinicio de Render, lo que
  invalida links de "recuperar contraseña" pendientes justo cuando el
  servicio reinicia. Solución conocida y documentada pero no implementada:
  persistir las claves en la base de datos (`PersistKeysToDbContext`).
- **RIESGO — logs a archivo efímeros**: Serilog escribe a un archivo dentro
  del contenedor que no persiste entre despliegues; la salida por consola
  sí la captura Render, así que no hay pérdida de visibilidad, solo el
  archivo local no sobrevive un redeploy.

## Pendiente en esta capa de documentación

Este documento cubre la arquitectura de sistema y aplicación. Quedan
pendientes, como documentos separados (ver `docs/README.md` para el
estado): `DATA-ARCHITECTURE.md` (detalle de constraints/índices más allá de
lo ya cubierto en `DOMAIN-MODEL.md`), `API-ARCHITECTURE.md` (contrato HTTP,
convenciones de error), `SECURITY-ARCHITECTURE.md` (JWT, roles, CORS,
lockout) y `OBSERVABILITY.md`.

# Observability — BeautyCommerce (Haldy&Co)

`04-Engineering/ERROR-HANDLING.md` responde cómo falla el sistema. Este
documento responde **cómo sabemos qué ocurrió** — no "tenemos logs", sino
si esos logs realmente permiten reconstruir un incidente real. Todo lo
marcado **HECHO** se verificó leyendo `Program.cs`, cada punto de logging
real del flujo de checkout, `GlobalExceptionHandler.cs`, y la
configuración de Serilog/health checks. No se agregó ningún log, métrica,
ni configuración nueva para escribir este documento.

## La pregunta central, respondida con evidencia, no con suposición

> *"Si mañana producción tiene un pedido que el cliente dice 'me cobraron
> pero no aparece mi pedido', ¿tenemos actualmente suficiente evidencia
> para reconstruir qué ocurrió sin entrar manualmente a la base de
> datos?"*

**Respuesta verificada: No.** Y no por una sola razón, sino por una cadena
de gaps que se refuerzan entre sí. Se traza el flujo completo abajo, con
lo que cada paso deja como rastro real.

**Nota de contexto que no cambia la respuesta pero la enmarca:** hoy
`PaymentService` es un simulador (ADR-007) — "me cobraron" no puede
ocurrir literalmente todavía. El escenario real y ya posible es el
equivalente: *"intenté comprar y no sé si se completó, y quiero saber qué
pasó realmente."* Es exactamente el mismo problema de observabilidad.

### El trazado real, paso por paso

```
Cliente hace checkout
   │
   ▼
Request llega al Controller           ¿Queda rastro? PARCIAL
   │                                   UseSerilogRequestLogging() deja UNA
   │                                   línea por request (Method, Path,
   │                                   StatusCode, tiempo) — sin UserId,
   │                                   sin body, sin ningún ID de correlación
   ▼
Authentication (JWT)                   ¿Queda rastro? NO
   │                                   OnAuthenticationFailed configurado
   │                                   pero vacío (Program.cs:145-148) —
   │                                   un JWT inválido/expirado no deja
   │                                   ningún log
   ▼
CheckoutCommandHandler arranca         ¿Queda rastro? SÍ, parcial
   │                                   "Starting checkout for user
   │                                   {UserId}" — Information. Tiene
   │                                   UserId. NO tiene email, NO tiene
   │                                   ningún ID de correlación que lo
   │                                   una a la línea de request logging
   │                                   de arriba.
   ▼
Cart claim (borra ítems)               ¿Queda rastro? NO
   │                                   Sin log explícito de esta operación
   ▼
Inventory reservation                   ¿Queda rastro? NO en logs de
   │                                   aplicación — SÍ como dato en
   │                                   InventoryMovement, pero
   │                                   InventoryService no tiene ningún
   │                                   ILogger (confirmado por ausencia
   │                                   en InventoryService.cs) — un
   │                                   cambio de stock nunca se loguea
   │                                   como evento, solo se persiste
   ▼
Payment (simulado)                      ¿Queda rastro? SÍ, si falla
   │                                   "Payment failed for user {UserId}:
   │                                   {Message}" — Warning. Si tiene
   │                                   éxito, NO se loguea explícitamente
   │                                   (el próximo log es el de éxito
   │                                   general del pedido)
   ▼
Order + OutboxMessage creados            ¿Queda rastro? SÍ
   │                                    "Order created successfully.
   │                                    OrderId {OrderId}, UserId
   │                                    {UserId}, Total {Total}" —
   │                                    Information. Primera vez que
   │                                    aparece un ID que el cliente
   │                                    podría reconocer (indirectamente,
   │                                    vía OrderNumber, que NO está en
   │                                    este log — solo el GUID interno)
   ▼
Si algo falla en el camino:              ¿Queda rastro? SÍ, dos líneas
   TransactionBehavior hace rollback     más, sin UserId en ninguna:
                                         - "Transaction rolled back for
                                         {Request}: {Message}" (Warning)
                                         - GlobalExceptionHandler:
                                         "{ExceptionType} on {Method}
                                         {Path} mapped to {StatusCode}:
                                         {Message}" (Warning) — tiene
                                         Method/Path/Status, NO tiene
                                         UserId
                                          ↓
                                         Ningún log dice explícitamente
                                         "esto se revirtió correctamente,
                                         cero efecto parcial persistido"
                                         — hay que inferirlo sabiendo
                                         cómo funciona TransactionBehavior
                                         (ver 01-Architecture/SYSTEM-
                                         ARCHITECTURE.md), no porque el
                                         log lo diga
```

### Por qué la respuesta es "No", con las tres razones concretas

1. **No hay forma de ir de "cliente con un email/nombre" a "UserId" sin
   consultar la base de datos.** Ningún log del flujo de checkout incluye
   el email — solo el `UserId` (GUID interno). Un operador que reciba el
   reclamo de un cliente **tiene que consultar la tabla de usuarios
   primero** para obtener el `UserId` antes de poder buscar en los logs —
   lo cual ya es "entrar a la base de datos manualmente", incluso antes de
   empezar a investigar el checkout en sí.
2. **No existe ningún ID de correlación/request ID** que una las distintas
   líneas de log de un mismo intento de checkout entre sí — ni siquiera el
   `TraceIdentifier` nativo de ASP.NET Core se propaga a los logs de
   aplicación (confirmado por ausencia: sin `.Enrich.FromLogContext()`,
   sin ningún middleware de correlación, sin customización de
   `UseSerilogRequestLogging`). Reconstruir "estas 4 líneas de log
   pertenecen al mismo intento" depende de que el operador infiera por
   `UserId` + cercanía de timestamp — funciona con poco tráfico
   concurrente, se vuelve ambiguo con más.
3. **Los logs de producción son efímeros y no están indexados.** Ya
   documentado en `docs/DEPLOYMENT.md`: el sink de archivo
   (`Logs/log-.txt`) vive dentro del contenedor y se pierde en cada
   redeploy/reinicio; lo único que sobrevive es lo que Render capture de
   la salida por consola, sin que este proyecto haya configurado ningún
   sistema de agregación/búsqueda (no hay integración con Datadog, ELK,
   Grafana Loki, ni el propio dashboard de logs de Render documentado como
   parte del flujo operativo). Buscar texto en eso es, en el mejor de los
   casos, grep manual contra lo que Render conserve.

**Un detalle técnico adicional que refuerza el punto 2, encontrado al
verificar en vez de asumir:** `Program.cs:45` registra
`builder.Services.AddProblemDetails()` — el servicio nativo de ASP.NET
Core que, usado correctamente, **agregaría automáticamente un `traceId`**
a cada respuesta de error. Pero `GlobalExceptionHandler.TryHandleAsync`
**no usa ese servicio** — construye su propio objeto `ProblemDetails` a
mano y lo serializa directamente con `WriteAsJsonAsync`
(`GlobalExceptionHandler.cs:43-51,62-66`), sin pasar por
`IProblemDetailsService`. El mecanismo de correlación que el framework ya
provee está registrado en el contenedor de DI, pero el código que maneja
todos los errores del sistema lo bypasea sin usarlo.

**Conclusión, en los términos que se pidió:** esto no es un defecto — nada
está roto, y los logs que existen son precisos para lo que cubren. Es un
**Production Readiness Gap / RIESGO**: la capacidad de diagnóstico rápido
de un incidente de cliente específico depende hoy de una investigación
manual, cruzando base de datos y archivos de log a mano, sin ninguna
herramienta ni identificador que lo agilice.

---

## Observability actual (lo que existe hoy)

**HECHO:**

| Capacidad | Estado real |
|---|---|
| Logging estructurado | Serilog, sinks Console + File (`Logs/log-.txt`, rotación diaria, 30 días de retención — **solo dentro del contenedor**, efímero en Render) |
| Correlation/request ID | **No existe.** `TraceIdentifier` nativo no se propaga a logs de aplicación ni a respuestas de error |
| Logging de excepciones | Sí — `GlobalExceptionHandler` loguea `Error` para 500, `Warning` para el resto, con tipo de excepción, método, path y mensaje |
| Logging de requests HTTP | Sí, una línea por request vía `UseSerilogRequestLogging()` (Method, Path, StatusCode, tiempo) — sin identidad del usuario, sin body |
| Eventos de autenticación/seguridad | Login fallido/exitoso: sí, a nivel de mensaje de texto en `IdentityService`. Fallo de validación de JWT: **no**, `OnAuthenticationFailed` vacío. Fallo de autorización por rol (403): **no**, ocurre antes de cualquier código de aplicación |
| Eventos de checkout | Parcial — inicio, pago fallido, éxito. **No** hay log de éxito de pago en sí, ni de "cart claimed", ni de cada línea de inventario reservada |
| Eventos de inventario | **No existen como logs** — `InventoryService` no tiene `ILogger`. El rastro de inventario vive únicamente como dato (`InventoryMovement`), no como evento observable en tiempo real |
| Ciclo de vida de orden | Parcial — creación se loguea; cambios de estado posteriores (`UpdateOrderStatusCommandHandler`) **no tienen logging explícito propio** más allá de lo genérico de `LoggingBehavior` (solo el nombre del tipo de comando) |
| Eventos de pago | Solo el caso de fallo; éxito no se loguea explícitamente aparte del log general de orden creada |
| Fallos de base de datos | Los no capturados explícitamente caen en el `Error` genérico de `GlobalExceptionHandler`/`TransactionBehavior` — con stack trace completo en el log (no en la respuesta al cliente, ver `ERROR-HANDLING.md`) |
| Performance/latencia | `PerformanceBehavior` — Warning únicamente si un comando/query supera 500ms hardcodeados; nada por debajo de ese umbral se registra, ni se agrega en ninguna métrica consultable |
| Health checks | `/health` existe, pero `AddHealthChecks()` se registra **sin ningún chequeo real encadenado** (sin `.AddNpgSql()`, sin `.AddDbContextCheck<>()`) — responde `Healthy` sin verificar si la base de datos está disponible |
| Métricas | **No existen.** Sin OpenTelemetry, Application Insights, Prometheus, ni ningún paquete de telemetría en ningún `.csproj` del backend |
| Tracing distribuido | **No existe** |
| Alerting | **No existe** ningún mecanismo — nadie se entera de un error 500 salvo que alguien lea los logs activamente |
| Dashboards | **No existen** |
| PII/secretos en logs | Contraseñas y JWT: no aparecen (verificado en `SECURITY-ARCHITECTURE.md`). **Email sí aparece** en logs de `IdentityService` (`_logger?.LogWarning("Login failed for {Email}...")`) — una decisión aceptable para logs internos, pero confirma que hay PII en texto plano en los logs |
| Retención | 30 días configurados en el sink de archivo — **irrelevante en producción** porque el archivo no sobrevive un redeploy; la retención real depende de lo que Render conserve de la salida por consola, no documentado ni controlado por este proyecto |
| Niveles de log | `MinimumLevel.Information()` global — no hay override por namespace ni por ambiente (Development vs. Production usan el mismo umbral) |

### Qué puede diagnosticar un operador hoy, y qué no

**Puede:**
- Confirmar si un `UserId` específico (ya conocido de antemano) tuvo un
  checkout exitoso o fallido, y por qué mensaje de negocio, revisando
  varias líneas de log manualmente.
- Ver la traza completa de un error 500 (stack trace incluido) si todavía
  está en el archivo/consola vigente.
- Confirmar si el servicio está "arriba" (`/health`), pero no si la base
  de datos detrás de él está disponible.

**No puede:**
- Partir de un email/nombre de cliente sin consultar la base de datos
  primero.
- Correlacionar automáticamente todas las líneas de log de un mismo
  intento de checkout.
- Saber cuánto tardó el flujo completo de checkout de punta a punta (solo
  sabría si algún paso individual superó 500ms).
- Ver un panel o alerta que le avise proactivamente de un problema — tiene
  que estar buscando activamente.
- Confiar en que los logs de hace más de un redeploy todavía existan.

---

## Observability necesaria para producción

**RECOMENDACIÓN, no implementada.** Lo mínimo para poder responder la
pregunta central de este documento con un "sí":

1. **ID de correlación por request**, generado en el primer middleware,
   propagado a todo log de esa request (vía `LogContext.PushProperty` de
   Serilog o equivalente) y devuelto al cliente — idealmente el mismo
   `traceId` que `IProblemDetailsService` ya sabe producir, si
   `GlobalExceptionHandler` lo usara en vez de bypasearlo.
2. **Agregación de logs fuera del contenedor** — cualquier sink externo
   persistente (desde algo simple como un servicio de logs gestionado
   hasta una solución más completa), para dejar de depender de que Render
   conserve la salida por consola indefinidamente.
3. **Incluir el `UserId` en el log de request HTTP** (o al menos en el
   log de excepción de `GlobalExceptionHandler`) — hoy dos de los logs más
   importantes para diagnosticar un incidente (el resumen de request y el
   de excepción) no tienen la identidad del usuario, mientras que sí la
   tiene el log específico de `CheckoutCommandHandler`.
4. **Health check real**, verificando conectividad a PostgreSQL como
   mínimo (`.AddNpgSql(...)` es una sola línea de configuración) — hoy
   `/health` no puede detectar la causa de indisponibilidad más común de
   un sistema con estado.
5. **Al menos una métrica de negocio activa**: tasa de checkouts
   exitosos/fallidos, para poder detectar una caída sin tener que leer
   logs línea por línea.
6. **Alerta mínima** ante un patrón de 500s sostenido — ni siquiera algo
   sofisticado, un umbral simple sería una mejora real sobre "nadie se
   entera hasta que un cliente se queja".

Ninguno de estos seis puntos se implementa en este documento.

---

## Observability futura (cuando la escala lo justifique)

**RECOMENDACIÓN, explícitamente de más largo plazo, no urgente hoy dado el
tamaño actual del proyecto (ver `00-Governance/PROJECT-CHARTER.md`):**

- Tracing distribuido real (OpenTelemetry) si el sistema crece a más de un
  servicio o gana dependencias externas críticas además de las ya
  existentes (Cloudinary, Google Drive, Resend).
- Dashboards y SLO/SLI formales (ej. "99% de checkouts deben completar en
  menos de 2s", "disponibilidad del 99.5%") — no tiene sentido definir
  SLOs antes de tener las métricas básicas del punto anterior.
- Auditoría de acceso de lectura (quién vio qué pedido de quién) — ya
  señalado como MEJORA en `01-Architecture/SECURITY-ARCHITECTURE.md`, con
  la misma dependencia: no tiene sentido antes de tener un pipeline de
  logging centralizado que lo sostenga.
- Alertas diferenciadas por severidad de negocio (un fallo de pago no es
  igual de urgente que un fallo de sincronización de imágenes de Drive).

---

## Clasificación

### HECHO
- Todo el inventario de "Observability actual" arriba, con su evidencia
  citada por archivo y línea.

### DEFECTO
- Ninguno. Nada de lo encontrado está roto — el sistema hace exactamente
  lo que su logging actual permite, sin fallar silenciosamente ni mentir
  en lo que sí registra.

### RIESGO / Production Readiness Gap
- **La pregunta central de este documento responde "No"** — es el hallazgo
  central, ya desarrollado arriba.
- Health check que no verifica la base de datos.
- PII (email) en logs de texto plano, sin política de retención real
  documentada.
- `IProblemDetailsService` registrado pero no usado — mecanismo de
  correlación disponible en el framework, no aprovechado.

### RECOMENDACIÓN
- Las seis de "Observability necesaria para producción", y las de
  "Observability futura" para más adelante — ninguna implementada aquí.

---

## Cierre de `04-Engineering/`

Con este documento se cierra la serie completa:

```
TESTING        → ¿Cómo demostramos que funciona?       ✅
QUALITY        → ¿Cuándo podemos declarar calidad?      ✅
CI/CD          → ¿Quién lo verifica automáticamente?    ✅ (🔴 roto, documentado)
ERROR HANDLING → ¿Cómo falla?                            ✅
OBSERVABILITY  → ¿Cómo sabemos qué ocurrió?              ✅ (🔴 gap, documentado)
```

Los cinco documentos comparten una misma conclusión de fondo, dicha una
vez para no repetirla en cada uno: **el código y la arquitectura del
producto son sólidos y están bien probados — la brecha real de este
proyecto está en la infraestructura que rodea al código** (CI
desconectado del despliegue, ausencia de correlación en logs, gates de
calidad sin enforcement automático). Ninguna de esas brechas se corrigió
durante esta serie de documentos, tal como se pidió explícitamente en cada
uno.

# Error Handling — BeautyCommerce (Haldy&Co)

Este documento no describe "qué hace `GlobalExceptionHandler`" de forma
aislada — traza el **comportamiento observable de extremo a extremo**: qué
le llega realmente al frontend para cada familia de error, y por qué. Todo
lo marcado **HECHO** se verificó leyendo el código de excepciones,
`GlobalExceptionHandler.cs`, los tests de contrato de excepción, y el
consumidor del frontend. No se corrigió el 403 vacío ni se unificó
`ApiResponse` — se documentan como comportamiento actual (HECHO) o riesgo
(RIESGO), no se implementan.

## El mapa completo, de extremo a extremo

**HECHO**, integrando lo ya verificado en `01-Architecture/API-ARCHITECTURE.md`
y `SECURITY-ARCHITECTURE.md`, con el detalle de transformación de cada capa:

```
HTTP Request
    │
    ▼
Controller (model binding)
    │
    ├──→ [ApiController] + FluentValidation auto-validation
    │     inválido → 400 con ModelState — NUNCA llega a MediatR
    │     (camino paralelo, no pasa por GlobalExceptionHandler)
    ▼
MediatR.Send()
    │
    ▼
LoggingBehavior → PerformanceBehavior → TransactionBehavior →
CachingBehavior → ValidationBehavior
    │                                        │
    │                          inválido → FluentValidation.ValidationException
    ▼
Handler / Service
    │
    ├──→ BadRequestException / NotFoundException / ConflictException /
    │     UnauthorizedException  (excepciones de negocio, lanzadas a mano)
    │
    ├──→ DbUpdateException / DbUpdateConcurrencyException / PostgresException
    │     (algunas capturadas y traducidas a mano; otras NO — ver Riesgo)
    │
    └──→ Cualquier otra excepción no anticipada
    │
    ▼
GlobalExceptionHandler.TryHandleAsync (IExceptionHandler)
    │           ⚠️ Solo intercepta lo que llega como *excepción*.
    │           Un 403 de [Authorize(Roles=...)] NUNCA pasa por aquí
    │           — lo resuelve el middleware de autorización antes.
    ▼
ProblemDetails { status, title, detail, instance } (+ "errors" si es
ValidationException) — excepto el 403, que no tiene cuerpo en absoluto
    │
    ▼
Frontend: axios → *Service.ts → getApiErrorMessage(error, fallback)
    │
    ▼
UI: mensaje mostrado al usuario (real si vino `detail`/`message`;
    el `fallback` hardcodeado del punto de llamada si no vino ninguno —
    lo cual pasa siempre para el 403, y para cualquier 500 real)
```

---

## Por familia de error

### Validación (FluentValidation)

**HECHO.** Dos caminos posibles para el **mismo** comando, con el
**mismo** resultado final, pero por rutas distintas — ya documentado como
"doble validación" en `API-ARCHITECTURE.md`:

1. **Camino HTTP estándar**: `[ApiController]` + auto-validación de
   FluentValidation rechaza con `400` vía `ModelState` **antes** de que el
   controller invoque `IMediator.Send()`. El cuerpo lo arma el
   mecanismo de model-validation de ASP.NET Core, no `GlobalExceptionHandler`
   — **no se verificó en esta pasada si ese cuerpo tiene la misma forma
   (`ProblemDetails`) que el resto de errores** — marcado explícitamente
   como **NO VERIFICADO**.
2. **Camino MediatR**: si algo llega a `IMediator.Send()` sin haber pasado
   por el model-binding estándar (dispatch interno, tests), `ValidationBehavior`
   corre los `IValidator<T>` registrados y lanza `FluentValidation.ValidationException`,
   que sí pasa por `GlobalExceptionHandler.cs:53-60` — mapeada a `400`, con
   `Extensions["errors"]` agrupando los mensajes por campo.

### `BadRequestException` → 400

**HECHO**, `GlobalExceptionHandler.cs:74`. `Title` fijo
("Solicitud inválida."), `Detail` = el mensaje real de la excepción. Se
lanza a mano en handlers para violaciones de reglas de negocio (carrito
vacío, stock insuficiente, transición de orden inválida). También es el
destino de la traducción manual de `DbUpdateConcurrencyException` en
`CheckoutCommandHandler` (ver Idempotencia en `01-Architecture/API-ARCHITECTURE.md`).

### `UnauthorizedException` → 401

**HECHO**, `GlobalExceptionHandler.cs:82-83`. Dos orígenes distintos
producen el **mismo** contrato de salida — verificado en
`Integrity/LoginLockoutReproductionTests.cs`: usuario inexistente,
contraseña incorrecta, y cuenta bloqueada **comparten literalmente el mismo
mensaje** ("Correo o contraseña incorrectos.") — una decisión de seguridad
correcta (no revela cuál de las tres condiciones ocurrió).

### `[Authorize]` sin token → 401 vs. `[Authorize(Roles=...)]` fallido → 403

**HECHO, distinción central de este documento.** No son la misma ruta de
código:

- **Sin token válido**: lo resuelve `JwtBearerHandler` al fallar la
  autenticación — produce `401`. `OnAuthenticationFailed` está configurado
  pero **no hace nada** (`Program.cs:145-148`, `return Task.CompletedTask`)
  — no hay logging de intentos con token inválido/expirado/manipulado.
- **Token válido pero rol insuficiente**: lo resuelve el middleware de
  **autorización** (no autenticación) llamando a `ForbidAsync()` — produce
  `403`. **Ninguno de los dos casos pasa por `GlobalExceptionHandler`** —
  ambos ocurren en una capa de middleware anterior a que cualquier
  controller o handler se ejecute.

### Por qué el 403 llega sin cuerpo — mecanismo completo, ya documentado en Security, repetido aquí con foco de error-handling

**HECHO.** `Program.cs` fija `JwtBearer` como `DefaultChallengeScheme`
(`:126-127`) sin `DefaultForbidScheme` explícito ni `OnForbidden` en
`JwtBearerEvents` (`:143-149`, solo `OnAuthenticationFailed`). El
comportamiento por defecto de `JwtBearerHandler` para un forbid es escribir
únicamente el código `403`, sin cuerpo. **No se corrige en este documento.**
Consecuencia para el frontend: `getApiErrorMessage` no encuentra ni
`detail` ni `message` en la respuesta, así que **siempre** muestra el
`fallback` que el desarrollador haya puesto a mano en ese punto de llamada
— es la única familia de error de todo el sistema donde el backend nunca
puede comunicar la causa real.

### `NotFoundException` → 404

**HECHO**, `GlobalExceptionHandler.cs:79-80`. `KeyNotFoundException`
mapea al mismo contrato — dos tipos de excepción distintos, mismo
resultado, correctamente unificado.

**Contrato real, verificado con evidencia de test**
(`Integrity/ExceptionContractReproductionTests.cs :: Nonexistent_Target_Add_Vs_Remove_Vs_Update_Exception_Contract`):
`AddCartItem` sobre una variante inexistente y `RemoveCartItem`/`UpdateCartItem`
sobre un carrito inexistente comparten el mismo `Status` (404) y el mismo
`Title` ("Recurso no encontrado.") — pero **`Detail` es distinto a
propósito** ("La variante del producto no existe." vs. "Carrito no
encontrado.") porque el recurso faltante realmente es distinto. Esto es lo
que se considera "bien unificado" en este proyecto: mismo contrato de
Status/Title para la misma clase de problema, con Detail libre para
precisión del caso real.

### `ConflictException` → 409

**HECHO**, `GlobalExceptionHandler.cs:85`. Único origen real encontrado:
`IdentityService.RegisterAsync` — tanto por pre-check de duplicado como
por captura de `DbUpdateException` con `PostgresException.SqlState ==
UniqueViolation` (la base de datos como autoridad final, ver ADR y
`01-Architecture/DATA-ARCHITECTURE.md`). Mismo `Detail` en ambos caminos
("El correo ya está registrado.") — correctamente unificado.

### `DbUpdateException` sin capturar → RIESGO real, no un caso hipotético

**HECHO el mecanismo; verificado que el propio equipo ya lo documentó como
comportamiento aceptado, no como accidente sin notar.**
`GlobalExceptionHandler.Map` **no tiene ningún case para `DbUpdateException`
ni `PostgresException`** — cualquier violación de constraint que llegue
como `DbUpdateException` sin ser capturada y traducida a mano cae en el
`_ => 500` por defecto (`GlobalExceptionHandler.cs:87`), con el mensaje
genérico "Ocurrió un error inesperado."

**Evidencia directa, no hipotética:**
`Concurrency/DbConstraintExceptionReproductionTests.cs ::
AddCartItem_Bypassing_The_Validator_Still_Gets_Rejected_By_The_Real_Check_Constraint`
prueba exactamente esto — cuando el constraint `CK_ShoppingCartItems_Quantity_Positive`
es "la última línea de defensa" (bypaseando el validador), el resultado es
un `DbUpdateException` crudo. El test lo asegura con el comentario *"must
remain the last line of defense"* — pero **no verifica qué código HTTP
produce**, a diferencia del test hermano
(`AddCartItem_With_Zero_Quantity_Is_Rejected_By_Validation_Before_Reaching_The_Database`),
que sí llama a `GlobalExceptionHandler.Map` y confirma `400`.

**Alcance real del riesgo, sin sobre-extenderlo:** en el camino de
producción normal, `AddCartItemCommand` está protegido por **dos** capas de
validación (auto-validación HTTP + `ValidationBehavior`), así que este caso
específico probablemente no es alcanzable por un cliente real sin que algo
más ya haya fallado antes. **El riesgo arquitectónico real y generalizable
es otro**: **cualquier check constraint de PostgreSQL que no tenga una
regla de FluentValidation equivalente** convertiría una violación de regla
de negocio legítima en un 500 genérico e indistinguible de un fallo real
del servidor. No se auditó en esta pasada, constraint por constraint,
cuáles de los ya inventariados en `01-Architecture/DATA-ARCHITECTURE.md`
tienen o no una validación de FluentValidation equivalente — **NO
VERIFICADO**, queda como trabajo pendiente antes de descartar el riesgo.

### Errores inesperados → 500

**HECHO**, ya citado: `Detail` se reemplaza siempre por un mensaje
genérico para 500 (`GlobalExceptionHandler.cs:47-49`) — la única familia
donde el backend deliberadamente **no** expone el mensaje real de la
excepción, correcto desde la óptica de no filtrar detalle interno.

### Identity → cómo se transforma

**HECHO**, `IdentityService.cs`, mapeo completo verificado:

| Condición real de Identity | Excepción de aplicación | HTTP |
|---|---|---|
| Email duplicado (pre-check) | `ConflictException` | 409 |
| Email duplicado (constraint de DB, carrera) | `ConflictException` (vía captura de `DbUpdateException`+`PostgresException`) | 409 |
| `IdentityResult.Errors` con código de duplicado | `ConflictException` | 409 |
| Cualquier otro `IdentityResult.Errors` (política de contraseña, etc.) | `BadRequestException` con los mensajes unidos (en español, vía `SpanishIdentityErrorDescriber`) | 400 |
| Usuario inexistente en login | `UnauthorizedException`, mensaje genérico | 401 |
| Contraseña incorrecta en login | `UnauthorizedException`, **mismo** mensaje genérico | 401 |

### PostgreSQL → qué cruza la frontera HTTP

**HECHO**, consolidado: solo se traduce explícitamente a mano en dos
lugares del código (`IdentityService.RegisterAsync` para `UniqueViolation`
de email, `CheckoutCommandHandler` para `DbUpdateConcurrencyException`).
Cualquier otro `PostgresException`/`DbUpdateException` no anticipado
**cruza como 500** — ver el riesgo de la sección anterior.

### Frontend — cómo interpreta cada familia

**HECHO**, `frontend/web/src/services/apiError.ts:1-13`, único punto de
consumo: lee `error.response.data.detail`, si no existe cae a
`.message`, si ninguno existe usa el `fallback` que cada componente define
por su cuenta. **No distingue entre familias de status** — 400, 401, 404,
409 y 500 se procesan exactamente igual en este único punto; la diferencia
de tratamiento (si la hay) vive en cada componente individual, no
centralizada. No se verificó en esta pasada si existe un interceptor de
Axios que redirija automáticamente a `/login` en un 401 — **NO VERIFICADO**.

---

## Escenario J, aplicado a error handling: una condición, dos caminos, dos contratos — ya ocurrió una vez, y quedó corregido

**HECHO, con evidencia histórica directa, no reconstruida.**
`Integrity/ExceptionContractReproductionTests.cs ::
Insufficient_Stock_Add_Vs_Update_Exception_Contract`, línea 172-174, cita
textualmente el fix:

> *"both now throw `BadRequestException` for the identical real-world
> condition, so Title ('Solicitud inválida.') must match too — this is the
> fix: Add previously had a different Title via `InvalidOperationException`"*

Es decir: **hubo una versión anterior de este código donde `AddCartItem`
con stock insuficiente lanzaba `InvalidOperationException`** (que
`GlobalExceptionHandler.cs:77` también mapea a 400, pero con `Title`
"Operación no permitida." — un `Title` **distinto** al de
`BadRequestException`, "Solicitud inválida.") **mientras `UpdateCartItem`,
para la misma condición real de negocio** (no hay suficiente stock),
**lanzaba `BadRequestException`**. Mismo código de status (400 en ambos
casos, por coincidencia de que ambos tipos mapean ahí), pero **`Title`
distinto** — un cliente que decidiera su UI en base al `Title` habría
tratado la misma situación de negocio de dos formas diferentes según cuál
endpoint la reportara. Se unificó a `BadRequestException` en ambos.

**Esto es exactamente el patrón que el Escenario J enseñó a buscar**: no
basta con que cada camino interno termine en "algún" 400 — hay que verificar
que el contrato completo (`Status` + `Title`, no solo el número) sea
idéntico para la misma condición de negocio, sin importar qué ruta interna
la detectó primero. El caso del `DbUpdateException` sin mapear (sección
anterior) es la versión **no resuelta todavía** de este mismo patrón: ahí
la misma condición (cantidad inválida) puede terminar en 400 o en 500
según qué capa la atrape primero.

## Escenario I, aplicado a error handling: una excepción "de autenticación" que mutaba estado persistido

**HECHO, ya documentado en detalle en ADR-004 — resumido aquí con el foco
específico de error handling.** `UnauthorizedException` en un login fallido
no es "solo" un error que el cliente ve como 401 — antes de llegar a
`GlobalExceptionHandler`, esa misma excepción atraviesa `TransactionBehavior`
(ver `01-Architecture/SYSTEM-ARCHITECTURE.md`), que por defecto hace
rollback de **toda** la transacción del comando, incluyendo el efecto
secundario de ASP.NET Identity (`AccessFailedCount`/`LockoutEnd`) que ya se
había persistido. La lección que deja para este documento, distinta a la
de Escenario J: **una excepción no es solo "el mensaje que ve el
cliente"** — también es una señal que atraviesa comportamiento
transaccional real, y el contrato HTTP correcto (401, mensaje genérico) se
cumplía perfectamente incluso cuando el efecto persistido estaba siendo
destruido silenciosamente por el rollback. Verificar el contrato HTTP no
garantiza que el estado del sistema quedó correcto — hay que verificar
ambos por separado.

---

## Clasificación

### HECHO — comportamiento actual
- Todo lo descrito arriba: el mapeo completo de `GlobalExceptionHandler`,
  la doble validación, el mecanismo del 403 vacío, la transformación de
  errores de Identity, el único punto de consumo del frontend.

### DEFECTO — únicamente con evidencia suficiente
- Ninguno nuevo en este documento. El único defecto de error-handling con
  evidencia histórica completa (Add-vs-Update Title mismatch) **ya está
  corregido** — se documenta como caso de estudio, no como defecto activo.

### RIESGO — inconsistencia potencial
- **`DbUpdateException`/`PostgresException` sin mapear caen a 500
  genérico** — mecanismo confirmado con test real
  (`DbConstraintExceptionReproductionTests`), alcance real limitado hoy
  por la doble validación, pero generalizable a cualquier constraint sin
  regla de FluentValidation equivalente — **no auditado constraint por
  constraint**.
- **El 403 sin cuerpo** (ya elevado en `SECURITY-ARCHITECTURE.md`,
  repetido aquí porque es, en última instancia, un problema de contrato de
  error).
- **Inconsistencia de `ApiResponse<T>` vs. objetos anónimos vs. resultado
  crudo** en respuestas de éxito (ya documentada en `API-ARCHITECTURE.md`)
  — no es un error, pero es la misma familia de problema de "contrato no
  unificado" que este documento trata para el camino de error.
- **No verificado si el 400 del camino de auto-validación HTTP tiene la
  misma forma `ProblemDetails`** que el camino de `GlobalExceptionHandler`
  — dos generadores de 400 distintos que podrían no coincidir en forma.
- **No verificado si existe un interceptor de Axios centralizado** para
  401 en el frontend.

### RECOMENDACIÓN — arquitectura objetivo (no implementado aquí)
1. Agregar un `case` explícito para `DbUpdateException`/`PostgresException`
   en `GlobalExceptionHandler.Map`, o auditar cada constraint de
   `01-Architecture/DATA-ARCHITECTURE.md` contra su validador de
   FluentValidation equivalente, para saber con certeza si el riesgo es
   alcanzable hoy.
2. Configurar `OnForbidden` en `JwtBearerEvents` para que el 403 también
   produzca `ProblemDetails`, unificando esa única familia de error
   restante.
3. Decidir un único contrato de respuesta de éxito (`ApiResponse<T>` en
   todos lados, o eliminarlo) — la misma disciplina de unificación que ya
   se aplicó al caso Add-vs-Update, extendida al camino de éxito.
4. Confirmar con un test explícito que el 400 de auto-validación HTTP y el
   400 de `ValidationBehavior` producen el mismo `ProblemDetails`.

Ninguna de estas cuatro recomendaciones se implementa en este documento —
consistente con la regla de no convertir un hallazgo en implementación
mientras el proyecto siga en modo documentación/reconocimiento.

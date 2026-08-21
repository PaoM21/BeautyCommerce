# ADR-004 — Login excluido de la transacción ambiental (`INotTransactional`)

**Estado:** Aceptado (vigente, con regresión) · **Tipo:** Retroactivo, pero con evidencia histórica completa (defecto real, no reconstrucción)

Este es el único ADR de este lote que documenta una decisión con rastro
histórico completo — reproducción, corrección y regresión reales, no una
reconstrucción razonada de un debate que no ocurrió.

## Contexto

ASP.NET Identity implementa el bloqueo de cuenta (*lockout*) tras varios
intentos fallidos de login mediante su propio mecanismo interno
(`AccessFailedCount`, `LockoutEnd`), que persiste ese estado como efecto
secundario de `CheckPasswordSignInAsync` **incluso cuando el resultado es
"credenciales incorrectas."** `LoginCommand`, como todo comando cuyo nombre
termina en `"Command"`, entraba automáticamente en la transacción ambiental
de `TransactionBehavior` (ver ADR-003).

## Evidencia

**HECHO — defecto real reproducido**, documentado en el código con la
etiqueta "Punto 5 / Escenario I":

- `TransactionBehavior` abre una transacción, ejecuta el handler de login,
  y si el handler lanza `UnauthorizedException` (credenciales inválidas),
  hace **rollback** de la transacción completa.
- El efecto secundario de `AccessFailedCount`/`LockoutEnd` que ASP.NET
  Identity ya había persistido dentro de esa misma transacción **se
  revertía con el rollback** — un atacante podía intentar contraseñas
  indefinidamente sin que el lockout nunca se activara, porque cada intento
  fallido deshacía su propio contador.
- Reproducido en
  `backend/tests/BeautyCommerce.Tests/Integrity/LoginLockoutReproductionTests.cs:175-214`:
  una serie de intentos fallidos que, sin el fix, nunca bloquea la cuenta
  porque cada intento resetea `AccessFailedCount` a 0 vía el rollback.

## Decisión

Crear una interfaz marcadora `INotTransactional`
(`Application/Common/Behaviors/INotTransactional.cs`) que `LoginCommand`
implementa. `TransactionBehavior` la revisa explícitamente
(`TransactionBehavior.cs:25-29`) y, si el request la implementa, no abre
transacción — el comando corre con auto-commit normal de EF Core, así que
el efecto secundario de Identity sobrevive aunque el comando termine
lanzando `UnauthorizedException`.

## Alternativas descartadas

Estas sí forman parte de la evidencia real del proceso de corrección
(no reconstrucción retroactiva):

- **Capturar la excepción dentro del handler y hacer `SaveChangesAsync`
  antes de relanzarla.** Habría funcionado pero es frágil: cualquier otro
  código que dependa del mismo patrón tendría que repetir el mismo truco a
  mano, y es fácil de romper si alguien reescribe el handler sin saber por
  qué el `SaveChangesAsync` intermedio está ahí.
- **Excluir `LoginCommand` completamente del pipeline de behaviors.**
  Habría eliminado también logging y medición de performance para el login,
  que sí son deseables — la solución elegida es quirúrgica: solo desactiva
  la transacción, no el resto del pipeline.
- **Cambiar el orden global del pipeline o hacer `TransactionBehavior`
  opt-in en vez de opt-out.** Habría afectado a todos los demás comandos
  transaccionales sin necesidad — ver ADR-003 para por qué ese cambio más
  amplio no se hizo como parte de esta corrección.

## Consecuencia

**Se gana:** el lockout de cuenta funciona correctamente — verificado por
regresión en
`backend/tests/BeautyCommerce.Tests/Behaviors/TransactionBehaviorOptOutTests.cs`,
que prueba **ambas mitades** del comportamiento: (1) un comando marcado
`INotTransactional` efectivamente sobrevive a un rollback simulado, y (2)
un comando ordinario (sin la marca) sigue haciendo rollback exactamente
como antes — la corrección no debilitó la garantía transaccional para el
resto del sistema.

**Se sacrifica:** `LoginCommand` ya no tiene atomicidad transaccional de
EF Core — si su handler hiciera múltiples escrituras (hoy no las hace,
según `LoginCommandHandler`), no habría rollback automático entre ellas.
Es un trade-off aceptado porque el efecto secundario que necesitábamos
preservar (el lockout de Identity) es exactamente el que una transacción
revertiría.

**Advertencia para quien lea este código sin este ADR:** `INotTransactional`
en `LoginCommand` puede parecer una interfaz "rara" o un vestigio de
refactor incompleto si no se conoce esta historia. No se debe eliminar sin
antes releer este documento y `LoginLockoutReproductionTests.cs`.

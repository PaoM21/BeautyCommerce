# ADR-003 — Transacción de base de datos por convención de nombre

**Estado:** Aceptado (vigente, con RIESGO documentado) · **Tipo:** Retroactivo

## Contexto

Muchos casos de uso (`Command`) necesitan que sus efectos secundarios —
escrituras en varias tablas, por ejemplo checkout descontando stock y
creando una orden a la vez — sean atómicos: o se aplican todos, o ninguno.

## Evidencia

**HECHO**, verificado en `TransactionBehavior.cs:20-58`: el behavior actúa
únicamente sobre requests cuyo **nombre de tipo termina en `"Command"`**
(`typeof(TRequest).Name.EndsWith("Command")`) y que no implementan la
interfaz marcadora `INotTransactional` (ver ADR-004). Para los que
califican, abre una transacción de EF Core, ejecuta el handler, llama
`SaveChangesAsync`, hace commit; en excepción, hace rollback y loguea
`Warning` para una lista conocida de excepciones de negocio o `Error` en
cualquier otro caso.

**HECHO, documentado también en ADR-002:** el orden real de ejecución del
pipeline coloca `TransactionBehavior` **antes** de `ValidationBehavior` — la
transacción se abre antes de que el comando se valide.

## Decisión

Envolver automáticamente todo comando (por convención de nombre, no por
interfaz explícita) en una transacción de base de datos, con
`INotTransactional` como mecanismo de opt-out explícito para los casos
donde el comportamiento por defecto sería incorrecto.

## Alternativas descartadas

**INFERENCIA** para el diseño original (reconstrucción razonada):

- **Transacción explícita dentro de cada handler que la necesite**, en vez
  de automática para todos. Más control por caso, pero implica que un
  desarrollador nuevo tiene que acordarse de envolver manualmente cada
  escritura multi-tabla — el diseño actual prioriza "seguro por defecto"
  sobre "explícito por handler".
- **Interfaz marcadora positiva** (`ITransactional`) en vez de negativa
  (`INotTransactional`) — cada comando que necesita transacción la declara,
  en vez de que todos la tengan por defecto salvo excepción. Se habría
  evitado el problema de que un comando nuevo llamado `"...Command"`
  automáticamente entra en una transacción sin que nadie lo decida
  conscientemente — pero el patrón actual ya está en producción con
  decenas de comandos dependiendo de él.

**HECHO, no descartada sino pendiente de decisión explícita** (per la
recomendación del equipo de no tocar esto sin ADR previo): el orden
`TransactionBehavior` antes de `ValidationBehavior` no fue una decisión
consciente registrada en ningún lado — es el resultado de cómo se fueron
agregando behaviors al `Program.cs` en el tiempo. Se documenta aquí como
comportamiento actual, no como decisión deliberada, y **no se cambia en
este ADR** — cualquier cambio de orden requiere su propio ADR que evalúe el
impacto en los ~30+ comandos que hoy dependen de este pipeline.

## Consecuencia

**Se gana:** ningún comando nuevo necesita acordarse de abrir una
transacción — es automático mientras termine su nombre en `"Command"`.

**Se sacrifica:** (a) el contrato es implícito por nombre, no por tipo —
frágil ante errores de nombramiento (ver ADR-002); (b) toda ejecución de un
comando paga el costo de abrir una transacción incluso si luego falla
validación, porque `TransactionBehavior` corre primero; (c) el defecto real
documentado en ADR-004 (lockout de login revertido) es la evidencia directa
de que "transaccional por defecto" puede ser incorrecto para comandos cuyo
efecto secundario debe sobrevivir a un resultado fallido — de ahí que
`INotTransactional` exista como mecanismo de excepción, no como diseño
alternativo completo.

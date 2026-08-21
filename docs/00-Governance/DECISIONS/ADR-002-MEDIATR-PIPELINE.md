# ADR-002 — Cross-cutting concerns como pipeline de MediatR

**Estado:** Aceptado (vigente, con RIESGO documentado) · **Tipo:** Retroactivo

## Contexto

Logging, medición de performance, transacciones de base de datos, cache y
validación son necesidades transversales que casi todo caso de uso
comparte. Hay que decidir dónde vive esa lógica sin duplicarla en cada
handler.

## Evidencia

**HECHO**, verificado en `BeautyCommerce.API/Program.cs:57-60` y
`:105-109`, y en `BeautyCommerce.Application/Common/Behaviors/`: existen 5
`IPipelineBehavior<,>` — `LoggingBehavior`, `PerformanceBehavior`,
`TransactionBehavior`, `CachingBehavior`, `ValidationBehavior` — registrados
en el contenedor de DI de ASP.NET Core. MediatR 14.2.0 resuelve
`IEnumerable<IPipelineBehavior<,>>` en orden de registro, y ejecuta esos
behaviors como capas concéntricas alrededor del handler real. Se trazó el
orden real de ejecución (ver
`01-Architecture/SYSTEM-ARCHITECTURE.md#el-pipeline-real`):

```
LoggingBehavior → PerformanceBehavior → TransactionBehavior →
CachingBehavior → ValidationBehavior → Handler
```

## Decisión

Implementar cross-cutting concerns como una cadena de `IPipelineBehavior<,>`
de MediatR, en vez de middleware HTTP, decoradores explícitos por handler, o
aspectos vía interceptores de terceros. Cada behavior decide si actúa sobre
un request dado inspeccionando su tipo (ver ADR-003 y ADR-006 para los
casos de `TransactionBehavior` y `CachingBehavior`, que deciden por el
sufijo del nombre del tipo).

## Alternativas descartadas

**INFERENCIA** (reconstrucción razonada, no debate documentado):

- **Middleware HTTP de ASP.NET Core** para transacciones/cache/logging. Se
  habría descartado porque el middleware HTTP no tiene visibilidad directa
  del tipo del comando/query de MediatR — tendría que inspeccionar la ruta
  o el body, perdiendo tipado fuerte.
- **Decorators explícitos por handler** (cada handler envuelto a mano en un
  `TransactionalOrderHandler`, etc.). Más explícito y sin convención
  implícita por nombre, pero mucho más código repetitivo por cada nuevo
  caso de uso.
- **Interceptores de terceros (AOP)**. Añade una dependencia y una curva de
  aprendizaje adicional para un problema que MediatR ya resuelve de forma
  nativa.

## Consecuencia

**Se gana:** cualquier caso de uso nuevo hereda logging, medición y (si
aplica por su nombre) transacción/cache sin código adicional — es
"gratis" con solo nombrar el comando/query correctamente.

**Se sacrifica, documentado como RIESGO en
`01-Architecture/SYSTEM-ARCHITECTURE.md`:** el orden de ejecución depende
del orden textual de registro en `Program.cs`, no de una prioridad
explícita — nada impide que alguien agregue un sexto behavior en el lugar
equivocado y altere el orden sin que ningún test lo detecte. Hoy no existe
un test que fije este orden como contrato. Esto no se corrige en este ADR
(ver la disciplina de "no tocar código sin autorización" en
`06-Quality/AUDIT-PUNTOS-1-5.md`) — se documenta para que la próxima
persona que modifique el pipeline lo haga con el orden real en mente, no
con el que asumiría por lectura del código de arriba hacia abajo.

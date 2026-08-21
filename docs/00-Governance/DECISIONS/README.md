# Architecture Decision Records — BeautyCommerce (Haldy&Co)

Un ADR registra una decisión de arquitectura para que dentro de seis meses
nadie tenga que preguntar "¿por qué está hecho así?" ni, peor, lo cambie sin
saber qué se rompe. Cada ADR responde exactamente cinco preguntas:

1. **Contexto** — ¿qué problema estábamos resolviendo?
2. **Evidencia** — ¿qué encontramos realmente en código, base de datos o
   comportamiento observado? (con archivo:línea o test que lo respalde)
3. **Decisión** — ¿qué se decidió hacer?
4. **Alternativas descartadas** — ¿qué otras opciones se consideraron y por
   qué no se eligieron?
5. **Consecuencia** — ¿qué se gana y qué se sacrifica con esta decisión?

## Nota sobre el origen de estos ADRs

Los ADR-001 a ADR-007 son **retroactivos**: se escribieron después de que el
código ya existía, no antes de implementarlo. Para la mayoría de estas
decisiones no existe un registro histórico de qué alternativas se
debatieron en su momento — la sección "Alternativas descartadas" en esos
casos es una reconstrucción razonada de qué otras opciones habría tenido
sentido evaluar, marcada explícitamente como **INFERENCIA** siguiendo la
convención de `docs/README.md`, no como un debate que ocurrió y quedó
documentado. La única excepción con evidencia histórica real y completa es
**ADR-004**, que corresponde a un defecto reproducido, corregido y cubierto
por regresión durante el hardening de Puntos 1–5.

A partir de aquí, cualquier decisión arquitectónica **nueva** (no
retroactiva) debe escribir su ADR *antes* o *durante* la implementación, no
después — para que "Alternativas descartadas" sea evidencia real, no
inferencia.

## Estado

| ADR | Título | Estado |
|---|---|---|
| [ADR-001](ADR-001-ARCHITECTURE-LAYERS.md) | Clean Architecture en 4 capas | Aceptado (vigente) |
| [ADR-002](ADR-002-MEDIATR-PIPELINE.md) | Cross-cutting concerns como pipeline de MediatR | Aceptado (vigente, con RIESGO documentado) |
| [ADR-003](ADR-003-TRANSACTION-BEHAVIOR.md) | Transacción de base de datos por convención de nombre | Aceptado (vigente, con RIESGO documentado) |
| [ADR-004](ADR-004-LOGIN-NOT-TRANSACTIONAL.md) | Login excluido de la transacción ambiental (`INotTransactional`) | Aceptado (vigente, con regresión) |
| [ADR-005](ADR-005-SOFT-DELETE.md) | Soft delete global vía `BaseEntity` + query filter | Aceptado (vigente) |
| [ADR-006](ADR-006-CACHE-STRATEGY.md) | Cache en memoria por convención de nombre, con exclusiones | Aceptado (vigente) |
| [ADR-007](ADR-007-PAYMENT-SIMULATOR.md) | Pasarela de pago simulada (`PaymentService`) | Aceptado temporalmente — **bloqueador de producción (P0)** |

`ADR-008-PRODUCT-VARIANT-MANAGEMENT.md` todavía no se escribe: la gestión
avanzada de variantes es una capacidad nueva sin diseñar, no una decisión ya
tomada que documentar. Se escribirá como parte de
`07-Features/PRODUCT-VARIANTS.md`, antes de implementarla, no después.

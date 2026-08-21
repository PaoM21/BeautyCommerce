# ADR-007 — Pasarela de pago simulada (`PaymentService`)

**Estado:** Aceptado temporalmente — **bloqueador de producción (P0)** · **Tipo:** Retroactivo

## Contexto

El checkout necesita completar un flujo de pago para poder probar y demostrar
el ciclo de compra completo (carrito → checkout → orden pagada) sin
depender todavía de credenciales reales de una pasarela de pago, mientras
el resto del sistema (catálogo, inventario, órdenes, envío) se terminaba de
construir.

## Evidencia

**HECHO**, verificado en
`backend/src/BeautyCommerce.Infrastructure/Services/PaymentService.cs:1-45`:
`IPaymentService.CreatePaymentAsync` está implementado por una clase que no
llama a ningún proveedor externo. Genera un `transactionId` con prefijo
`SIM-` (`$"SIM-{Guid.NewGuid():N}"`) y devuelve éxito para cualquier monto
mayor que cero — no hay integración con Stripe, PayU, Wompi ni ningún otro
proveedor. El checkout (`CheckoutCommandHandler`) invoca esta interfaz igual
que invocaría una real, así que el flujo completo (captura de envío, cálculo
de costo, creación de orden con estado `Paid`, email de confirmación vía
outbox) funciona de punta a punta — el único eslabón simulado es el cobro
en sí.

## Decisión

Mantener `PaymentService` como simulador mientras el resto de la plataforma
se termina de construir y endurecer (hardening de Puntos 1–5, SEO, panel
admin), documentando explícitamente que **esto es un bloqueador de
lanzamiento**, no un detalle menor — ver la tabla de Release Readiness en
`00-Governance/PROJECT-CHARTER.md`.

## Alternativas descartadas

**INFERENCIA** (reconstrucción razonada — no hay registro de por qué se
priorizó así, pero es la lectura más consistente con el estado actual del
repositorio):

- **Integrar una pasarela real desde el principio** (Stripe, PayU, Wompi).
  Se habría necesitado antes de tener el resto del flujo de checkout
  estable, y habría significado depender de credenciales/sandbox de un
  proveedor externo para poder desarrollar y testear cualquier feature de
  checkout, inventario o envío — el simulador desacopla ese desarrollo de
  una dependencia externa mientras el resto de la plataforma maduraba.
- **No implementar checkout en absoluto hasta tener la pasarela real.**
  Habría bloqueado todo el trabajo de órdenes, inventario, envío y las
  pruebas de integridad transaccional (ver ADR-003, ADR-004) que dependen
  de que exista un flujo de checkout funcional para reproducirse.

## Consecuencia

**Se gana:** todo el resto del sistema (inventario, órdenes, envío,
reseñas, loyalty, emails transaccionales) se pudo construir y endurecer
contra un flujo de checkout realista sin esperar una integración de pago
real ni pagar comisiones de sandbox durante el desarrollo.

**Se sacrifica, y esto es lo importante:** el sistema **no puede procesar
una venta real** hasta que este ADR se reemplace por una integración real.
No es un detalle de bajo nivel — es la razón por la que
`00-Governance/PROJECT-CHARTER.md` lo marca como 🔴 y como prioridad **P0**
por encima de cualquier otra brecha (variantes, media, experiencia). Ningún
otro ADR de este lote tiene esa urgencia de negocio asociada.

## Siguiente paso (no ejecutado en este ADR)

Cuando se diseñe la integración real, debe documentarse en
`07-Features/REAL-PAYMENTS.md` **antes** de implementarla — siguiendo la
regla de secuencia establecida en `docs/README.md` (ninguna capacidad nueva
no trivial se implementa sin su documento de arquitectura primero) — y debe
definir explícitamente qué pasa con `IPaymentService` como interfaz: si el
simulador se mantiene como implementación de desarrollo/test (probable) o
se elimina por completo.

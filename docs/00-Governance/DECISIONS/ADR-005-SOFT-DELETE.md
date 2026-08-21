# ADR-005 — Soft delete global vía `BaseEntity` + query filter

**Estado:** Aceptado (vigente) · **Tipo:** Retroactivo

## Contexto

Un e-commerce no puede borrar físicamente productos, categorías, marcas u
órdenes referenciadas por historial de compras, reseñas o reportes
financieros sin romper esa trazabilidad. Se necesita una forma de "borrar"
que preserve el registro.

## Evidencia

**HECHO**, verificado en `Domain/Base/BaseEntity.cs:3-21` y
`ApplicationDbContext.cs:53-72,85-149`: toda entidad que hereda `BaseEntity`
(15 de las 16 entidades del dominio — `OutboxMessage` es la única
excepción, ver `01-Architecture/DOMAIN-MODEL.md`) trae `IsDeleted:bool`,
`DeletedAt:DateTime?`, `DeletedBy:Guid?`. `OnModelCreating` aplica
reflectivamente un `HasQueryFilter(e => !e.IsDeleted)` a toda entidad
asignable a `BaseEntity` — nadie tiene que declararlo entidad por entidad.
`SaveChangesAsync` está sobrescrito para que marcar `IsDeleted = true` en
una entidad modificada dispare automáticamente el timestamp `DeletedAt` y
`DeletedBy` — el soft delete se implementa como una actualización
ordinaria, no como un método de dominio dedicado (`entity.Delete()` no
existe en ninguna entidad).

## Decisión

Implementar soft delete como una convención transversal a nivel de
`BaseEntity` + `ApplicationDbContext`, aplicada automáticamente a toda
entidad nueva que herede de la base, en vez de que cada entidad implemente
su propia lógica de borrado.

## Alternativas descartadas

**INFERENCIA** (reconstrucción razonada):

- **Borrado físico con tablas de auditoría separadas** (`ProductAuditLog`,
  etc.) que registran el estado antes de un `DELETE` real. Preserva
  historial pero duplica el modelo de datos y requiere joins adicionales
  para reconstruir "qué existía cuando".
- **Soft delete declarado manualmente por entidad** (cada
  `*Configuration.cs` agrega su propio `HasQueryFilter`). Más explícito por
  archivo, pero repetitivo y fácil de olvidar en una entidad nueva — el
  enfoque global elegido hace que sea imposible olvidarlo, a costa de que
  sea automático para *toda* entidad nueva que herede `BaseEntity`, incluso
  si en algún caso no se quisiera ese comportamiento.

## Consecuencia

**Se gana:** ninguna entidad nueva puede "olvidarse" de tener soft delete —
es automático con solo heredar `BaseEntity`. Los reportes históricos
(órdenes, reseñas) no pierden su referencia aunque el producto o la marca
subyacente se "elimine" desde el admin.

**Se sacrifica:** cualquier query que use `IgnoreQueryFilters()`
explícitamente (como hacen los tests de este proyecto para limpiar datos de
prueba, ver `06-Quality/AUDIT-PUNTOS-1-5.md`) rompe la protección
automática — hay que saber que existe el filtro para saber que hay que
saltárselo a propósito. También significa que las tablas crecen sin límite
(nada purga físicamente los registros marcados `IsDeleted`), lo cual no
está resuelto ni documentado como estrategia de retención en este momento —
queda como pregunta abierta para `01-Architecture/DATA-ARCHITECTURE.md`.

# ADR-001 — Clean Architecture en 4 capas

**Estado:** Aceptado (vigente) · **Tipo:** Retroactivo (ver nota en `README.md` de esta carpeta)

## Contexto

El backend necesita una organización de código que separe reglas de negocio
de detalles técnicos (base de datos, framework web, servicios externos),
para que el dominio se pueda razonar y testear sin depender de PostgreSQL,
ASP.NET Core ni proveedores externos (Cloudinary, Google Drive, Resend).

## Evidencia

**HECHO**, verificado directamente en los 4 archivos `.csproj` del backend
(no inferido de la convención de nombres de carpetas):

```
BeautyCommerce.Domain.csproj            → sin <ProjectReference>
BeautyCommerce.Application.csproj       → ProjectReference: Domain
BeautyCommerce.Infrastructure.csproj    → ProjectReference: Application, Domain
BeautyCommerce.API.csproj               → ProjectReference: Application, Infrastructure
```

`Domain` es el único proyecto sin referencias a otros proyectos del
backend — el compilador impide, no solo la convención, que el dominio
importe EF Core, ASP.NET Identity o cualquier detalle de infraestructura.
Ver `01-Architecture/SYSTEM-ARCHITECTURE.md` y
`01-Architecture/DOMAIN-MODEL.md` para el detalle de qué vive en cada capa.

## Decisión

Organizar el backend en 4 proyectos con dependencia estrictamente
unidireccional: `Domain ← Application ← Infrastructure ← API`. Los casos de
uso (`Application`) se expresan como comandos/queries de MediatR
(ver ADR-002), no como una capa de "servicios" genérica.

## Alternativas descartadas

**INFERENCIA** (no hay registro histórico de esta deliberación — se listan
las alternativas que un equipo normalmente evaluaría en esta decisión):

- **Arquitectura en capas tradicional (N-Layer) sin inversión de
  dependencias**, donde `Domain` referenciaría directamente EF Core para
  anotaciones de mapeo. Se habría descartado porque acopla el modelo de
  negocio a un ORM específico desde el inicio.
- **Un solo proyecto** (monolito de carpetas sin separación de assembly).
  Más rápido de arrancar, pero no impone la regla de dependencia a nivel de
  compilador — cualquier desarrollador podría importar `Npgsql` desde el
  dominio sin que nada lo impida.

## Consecuencia

**Se gana:** el dominio es testeable sin base de datos real (aunque en la
práctica, ver `04-Engineering/TESTING-STRATEGY.md` cuando se escriba, los
tests de integridad de este proyecto sí usan PostgreSQL real a propósito
para casos donde el comportamiento depende de la traducción SQL exacta).
Los detalles de infraestructura (Cloudinary, Google Drive, Resend, EF Core)
se pueden reemplazar sin tocar `Domain` ni `Application`.

**Se sacrifica:** más indirección — un caso de uso simple todavía cruza 3-4
archivos (Command, Handler, Validator, a veces DTO) en vez de vivir en un
único controller/service. Esto es una decisión consciente de este tipo de
arquitectura, no un costo oculto.

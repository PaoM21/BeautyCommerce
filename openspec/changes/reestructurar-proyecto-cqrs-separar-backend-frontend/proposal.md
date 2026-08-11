# Propuesta: Reestructuración del Proyecto BeautyCommerce con CQRS y Separación Backend/Frontend

## Resumen Ejecutivo
Reestructurar el proyecto BeautyCommerce para establecer una arquitectura profesional separando completamente backend y frontend, manteniendo la arquitectura CQRS con mejores prácticas de desarrollo.

## Problema
- Estructura monolítica actual no separa adecuadamente responsabilidades entre backend y frontend
- Falta de organización clara para equipos distribuidos
- Arquitectura CQRS no está completamente implementada
- Dificultad para escalar y mantener el proyecto a nivel profesional

## Solución Propuesta

### 1. Estructura de Carpetas Raíz
```
beauty/
├── backend/
│   ├── BeautyCommerce.API/              (API REST principal)
│   ├── BeautyCommerce.Application/      (Lógica de negocio - CQRS Commands/Queries)
│   ├── BeautyCommerce.Domain/           (Entidades y value objects)
│   ├── BeautyCommerce.Infrastructure/   (BD, caché, APIs externas)
│   ├── BeautyCommerce.Tests/            (Tests unitarios y de integración)
│   └── docker-compose.yml               (Composición de servicios backend)
├── frontend/
│   ├── web/                             (Aplicación web principal - React/Vue/Angular)
│   ├── mobile/                          (Aplicación móvil - React Native/Flutter)
│   └── shared/                          (Componentes compartidos, tipos TypeScript)
├── docs/                                (Documentación del proyecto)
├── infrastructure/                      (Configuración DevOps, k8s, etc.)
└── openspec/                            (Especificaciones del proyecto)
```

### 2. Backend - Organización CQRS
```
backend/
├── BeautyCommerce.API/
│   ├── Controllers/
│   ├── Middleware/
│   └── Startup.cs
├── BeautyCommerce.Application/
│   ├── Commands/              ← Modificar estado
│   │   ├── Products/
│   │   ├── Orders/
│   │   └── ...
│   ├── Queries/               ← Leer estado (optimizado)
│   │   ├── Products/
│   │   ├── Orders/
│   │   └── ...
│   ├── Handlers/
│   ├── DTOs/
│   └── Validators/
├── BeautyCommerce.Domain/
│   ├── Entities/
│   ├── ValueObjects/
│   ├── Aggregates/
│   ├── Events/
│   └── Interfaces/
├── BeautyCommerce.Infrastructure/
│   ├── Persistence/           (DbContext, Repositories)
│   ├── ExternalServices/
│   ├── Caching/
│   └── EventBus/
└── BeautyCommerce.Tests/
    ├── Unit/
    ├── Integration/
    └── E2E/
```

### 3. Frontend - Estructura Modular
```
frontend/
├── web/
│   ├── src/
│   │   ├── app/              (Módulos principales)
│   │   │   ├── admin/
│   │   │   ├── catalog/
│   │   │   ├── cart/
│   │   │   ├── checkout/
│   │   │   └── ...
│   │   ├── shared/           (Componentes reutilizables)
│   │   ├── core/             (Servicios globales)
│   │   ├── assets/
│   │   ├── styles/
│   │   └── main.tsx
│   ├── tests/
│   └── package.json
└── types/                     (TypeScript types compartidos)
    └── api.ts                 (Interfaces generadas desde OpenAPI)
```

## Beneficios
✅ Escalabilidad: Equipos separados trabajando en paralelo  
✅ Mantenibilidad: Código organizado siguiendo CQRS  
✅ DevOps: Deploy independiente de backend y frontend  
✅ Testing: Facilita testing a nivel de módulos  
✅ DDD: Estructura orientada al dominio del negocio  

## Impacto
- Cambio estructural significativo pero necesario
- Requiere migración ordenada de código existente
- Mejora sustancial en la profesionalidad del proyecto

## No-Objetivos
- No cambiar las tecnologías existentes (.NET Core, BD actual)
- No reescribir lógica de negocio
- No perder funcionalidad actual

## Próximos Pasos
1. Crear especificaciones detalladas (specs.md)
2. Diseñar plan de migración (design.md)
3. Generar tareas de implementación (tasks.md)

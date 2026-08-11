# Especificaciones: Reestructuración BeautyCommerce

## Especificación 1: Estructura Base del Proyecto

### Ubicación Actual
```
C:\Users\ducua\OneDrive\Desktop\beauty\BeautyCommerce\
├── .git/
├── .claude/
├── .gitignore
├── docker-compose.yml
├── Dockerfile
├── BeautyCommerce.API/
├── BeautyCommerce.Application/
├── BeautyCommerce.Application.Tests/
├── BeautyCommerce.Domain/
├── BeautyCommerce.Infrastructure/
├── BeautyCommerce.Tests/
├── BeautyCommerce.Web/
└── openspec/
```

### Estructura Meta-Proyecto (Raíz)
Crear estructura que contenga backend, frontend y docs:
```
beauty/ (raíz del repositorio)
├── backend/
├── frontend/
├── docs/
├── infrastructure/
├── openspec/
├── .git/
├── .gitignore
└── README.md
```

---

## Especificación 2: Backend - Carpetas y Namespaces

### Estructura Física
```
backend/
├── src/
│   ├── BeautyCommerce.API/
│   ├── BeautyCommerce.Application/
│   ├── BeautyCommerce.Domain/
│   └── BeautyCommerce.Infrastructure/
├── tests/
│   ├── BeautyCommerce.Application.Tests/
│   └── BeautyCommerce.Tests/
├── BeautyCommerce.sln
├── docker-compose.yml
└── README.md
```

### Application Layer - CQRS
**Comandos (Commands):**
- `backend/src/BeautyCommerce.Application/Commands/`
  - `Products/CreateProductCommand.cs`
  - `Products/UpdateProductCommand.cs`
  - `Products/DeleteProductCommand.cs`
  - `Orders/CreateOrderCommand.cs`
  - `Orders/UpdateOrderStatusCommand.cs`
  - `Users/RegisterUserCommand.cs`
  - `Users/UpdateProfileCommand.cs`

**Queries (Queries):**
- `backend/src/BeautyCommerce.Application/Queries/`
  - `Products/GetProductByIdQuery.cs`
  - `Products/GetAllProductsQuery.cs`
  - `Products/SearchProductsQuery.cs`
  - `Orders/GetOrderByIdQuery.cs`
  - `Orders/GetUserOrdersQuery.cs`
  - `Users/GetUserByIdQuery.cs`

**Handlers, DTOs, Validators:**
- `backend/src/BeautyCommerce.Application/Handlers/`
- `backend/src/BeautyCommerce.Application/DTOs/`
- `backend/src/BeautyCommerce.Application/Validators/`

### Domain Layer - DDD
```
backend/src/BeautyCommerce.Domain/
├── Entities/
│   ├── Product.cs
│   ├── Order.cs
│   ├── User.cs
│   └── ...
├── ValueObjects/
│   ├── Money.cs
│   ├── Address.cs
│   └── ...
├── Aggregates/
│   ├── ProductAggregate.cs
│   ├── OrderAggregate.cs
│   └── ...
├── Events/
│   ├── ProductCreatedEvent.cs
│   ├── OrderConfirmedEvent.cs
│   └── ...
└── Interfaces/
    ├── IRepository.cs
    └── ...
```

### Infrastructure Layer
```
backend/src/BeautyCommerce.Infrastructure/
├── Persistence/
│   ├── Context/
│   │   └── BeautyCommerceDbContext.cs
│   └── Repositories/
│       ├── ProductRepository.cs
│       ├── OrderRepository.cs
│       └── ...
├── ExternalServices/
│   ├── PaymentService.cs
│   ├── EmailService.cs
│   └── ...
├── Caching/
│   └── CacheService.cs
├── EventBus/
│   └── EventBusService.cs
└── Configuration/
    └── ServiceCollectionExtensions.cs
```

### API Layer
```
backend/src/BeautyCommerce.API/
├── Controllers/
│   ├── ProductsController.cs
│   ├── OrdersController.cs
│   ├── UsersController.cs
│   └── HealthController.cs
├── Middleware/
│   ├── ErrorHandlingMiddleware.cs
│   ├── AuthenticationMiddleware.cs
│   └── ...
├── Filters/
├── Startup.cs
└── Program.cs
```

---

## Especificación 3: Frontend - Estructura Web

### Estructura Física
```
frontend/
├── web/
│   ├── src/
│   │   ├── app/
│   │   │   ├── admin/
│   │   │   │   ├── pages/
│   │   │   │   ├── components/
│   │   │   │   └── services/
│   │   │   ├── catalog/
│   │   │   │   ├── pages/
│   │   │   │   ├── components/
│   │   │   │   └── services/
│   │   │   ├── cart/
│   │   │   ├── checkout/
│   │   │   ├── auth/
│   │   │   └── ...
│   │   ├── shared/
│   │   │   ├── components/
│   │   │   │   ├── Header.tsx
│   │   │   │   ├── Footer.tsx
│   │   │   │   ├── Button.tsx
│   │   │   │   └── ...
│   │   │   └── ui/
│   │   ├── core/
│   │   │   ├── services/
│   │   │   │   ├── api.service.ts
│   │   │   │   ├── auth.service.ts
│   │   │   │   └── ...
│   │   │   ├── guards/
│   │   │   ├── interceptors/
│   │   │   └── utils/
│   │   ├── assets/
│   │   │   ├── images/
│   │   │   └── icons/
│   │   ├── styles/
│   │   │   ├── globals.css
│   │   │   ├── variables.css
│   │   │   └── tailwind.config.js
│   │   ├── types/
│   │   ├── App.tsx
│   │   └── main.tsx
│   ├── tests/
│   │   ├── unit/
│   │   ├── integration/
│   │   └── e2e/
│   ├── public/
│   ├── package.json
│   ├── vite.config.ts
│   ├── tsconfig.json
│   └── README.md
├── shared/
│   ├── types/
│   │   ├── api.types.ts
│   │   ├── domain.types.ts
│   │   └── ...
│   ├── utils/
│   └── package.json
└── README.md
```

---

## Especificación 4: Configuración y DevOps

### Docker
**`backend/docker-compose.yml`:**
- SQL Server (o BD actual)
- Redis (para caché)
- Backend .NET
- Servicios auxiliares

**`frontend/web/Dockerfile`:**
- Build stage: Node.js
- Runtime stage: Nginx

### GitHub Workflows
```
.github/workflows/
├── backend-build-test.yml       (CI/CD backend)
├── frontend-build-test.yml      (CI/CD frontend)
├── deploy-backend.yml           (Deploy backend)
└── deploy-frontend.yml          (Deploy frontend)
```

---

## Especificación 5: Configuración y Variables de Entorno

### Backend - `appsettings.json`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "ApiSettings": {
    "BaseUrl": "https://api.beautycommerce.local",
    "Port": 5000
  },
  "CacheSettings": {
    "Enabled": true,
    "RedisConnection": "..."
  }
}
```

### Frontend - `.env.local`
```
VITE_API_URL=http://localhost:5000
VITE_API_TIMEOUT=30000
VITE_ENABLE_MOCK_API=false
```

---

## Especificación 6: Namespaces y Convenciones

### Backend Namespaces
```
BeautyCommerce.API.*
BeautyCommerce.Application.Commands.*
BeautyCommerce.Application.Queries.*
BeautyCommerce.Application.Handlers.*
BeautyCommerce.Domain.Entities.*
BeautyCommerce.Domain.ValueObjects.*
BeautyCommerce.Infrastructure.Persistence.*
```

### Frontend Naming
```
camelCase: variables, functions, services
PascalCase: React components, TypeScript interfaces
kebab-case: file names (Button.tsx, api-service.ts)
UPPER_SNAKE_CASE: constants
```

---

## Especificación 7: Documentación

### Raíz - `docs/`
```
docs/
├── README.md                    (Índice principal)
├── ARCHITECTURE.md              (Arquitectura general)
├── SETUP.md                     (Guía de configuración)
├── API.md                       (Documentación OpenAPI)
├── CONTRIBUTING.md              (Guía para contribuidores)
└── TROUBLESHOOTING.md           (Solución de problemas)
```

### Backend - `backend/README.md`
- Setup local
- Ejecutar tests
- Migraciones BD
- Estructura CQRS

### Frontend - `frontend/web/README.md`
- Setup local
- Build y deploy
- Testing
- Estructura de módulos

---

## Especificación 8: Git y Control de Versiones

### Rama Estrategia
- `main`: Producción estable
- `develop`: Rama de desarrollo
- `feature/*`: Nuevas características
- `bugfix/*`: Correcciones

### Commits
```
Format: [scope] message
Ejemplos:
  backend: Add CQRS command handler for products
  frontend: Implement product catalog page
  chore: Update dependencies
```

### Pull Requests
- Require code review
- CI/CD checks deben pasar
- Rebase antes de merge

---

## Especificación 9: Testing

### Backend
- **Unit Tests**: `tests/BeautyCommerce.Application.Tests/`
- **Integration Tests**: `tests/BeautyCommerce.Tests/`
- **xUnit Framework**
- Cobertura mínima: 80%

### Frontend
- **Unit Tests**: `frontend/web/tests/unit/`
- **Integration Tests**: `frontend/web/tests/integration/`
- **Vitest + React Testing Library**
- Cobertura mínima: 70%

---

## Criterios de Aceptación

✅ Estructura de carpetas reorganizada según especificación  
✅ Todos los proyectos C# compilables en `backend/`  
✅ Frontend preparado en `frontend/web/`  
✅ Documentación en `docs/`  
✅ GitHub workflows configurados  
✅ Docker compose funcional  
✅ Convenciones de naming aplicadas  
✅ Git workflow establecido  

---

## Notas Importantes

1. **Sin cambios de lógica**: Solo reorganización física
2. **Compatibilidad**: Mantener funcionalidad existente 100%
3. **Gradual**: Migrar módulos en orden de dependencias
4. **Testing**: Validar cada paso con tests existentes

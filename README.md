# BeautyCommerce

Plataforma de e-commerce de productos de belleza, con el backend (.NET) y el frontend (React) separados en carpetas independientes.

## Estructura del Proyecto

```
BeautyCommerce/
├── backend/                 # API REST (.NET 9)
│   ├── src/
│   │   ├── BeautyCommerce.API/              # Controllers, Program.cs
│   │   ├── BeautyCommerce.Application/      # CQRS (Commands/Queries/Handlers)
│   │   ├── BeautyCommerce.Domain/           # Entidades y lógica de dominio
│   │   └── BeautyCommerce.Infrastructure/   # Persistencia (PostgreSQL/EF Core)
│   ├── tests/
│   │   ├── BeautyCommerce.Tests/
│   │   └── BeautyCommerce.Application.Tests/
│   └── BeautyCommerce.sln
├── frontend/
│   └── web/                 # Aplicación React 19 + TypeScript + Vite + MUI
├── openspec/                 # Especificaciones y changes de OpenSpec
└── README.md
```

## Quick Start

### Backend

```bash
cd backend
dotnet restore
dotnet build
dotnet run --project src/BeautyCommerce.API
```

### Frontend

```bash
cd frontend/web
npm install
npm run dev
```

Por defecto el frontend corre en `http://localhost:5173` y el backend acepta ese origen vía CORS (ver `Program.cs`).

## Stack

- **Backend**: .NET 9, CQRS + MediatR, Entity Framework Core, PostgreSQL, JWT auth
- **Frontend**: React 19, TypeScript, Vite, MUI, TanStack Query, Axios

## Documentación

- [backend/README.md](./backend/README.md)

---

**Versión**: 2.0.0 (reestructuración backend/frontend)

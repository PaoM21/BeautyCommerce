# Backend - BeautyCommerce API

API REST en .NET 9 con arquitectura CQRS (MediatR) y Entity Framework Core sobre PostgreSQL.

## Estructura

```
backend/
├── src/
│   ├── BeautyCommerce.API/              # Controllers, Program.cs, appsettings
│   ├── BeautyCommerce.Application/      # Commands, Queries, Handlers (CQRS)
│   ├── BeautyCommerce.Domain/           # Entidades y reglas de negocio
│   └── BeautyCommerce.Infrastructure/   # DbContext, repositorios, servicios externos
├── tests/
│   ├── BeautyCommerce.Tests/
│   └── BeautyCommerce.Application.Tests/
└── BeautyCommerce.sln
```

## Setup

Requisitos: .NET SDK 9.0+, PostgreSQL.

```bash
cd backend
dotnet restore
dotnet build
dotnet run --project src/BeautyCommerce.API
```

Configura la cadena de conexión en `src/BeautyCommerce.API/appsettings.Development.json`.

## Testing

```bash
cd backend
dotnet test
```

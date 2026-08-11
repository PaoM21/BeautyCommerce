# Tareas: Implementación de Reestructuración

## FASE 1: PREPARACIÓN (Tareas 1-5)

### Tarea 1: Crear estructura de carpetas raíz
**Duración**: 30 minutos  
**Complejidad**: 🟢 Baja  
**Dependencias**: Ninguna

**Descripción**:
Crear carpetas base en la raíz del proyecto para separar backend, frontend y documentación.

**Pasos**:
1. En `C:\Users\ducua\OneDrive\Desktop\beauty\` crear:
   - `backend/`
   - `frontend/`
   - `frontend/web/`
   - `frontend/shared/`
   - `docs/`
   - `infrastructure/`

2. Verificar estructura con:
   ```powershell
   Get-ChildItem -Directory
   ```

**Definición de Hecho**:
- [ ] Carpetas creadas
- [ ] Estructura visible en explorador
- [ ] Git aún funciona en raíz

---

### Tarea 2: Actualizar .gitignore para nuevas rutas
**Duración**: 30 minutos  
**Complejidad**: 🟢 Baja  
**Dependencias**: Tarea 1

**Descripción**:
Actualizar `.gitignore` para ignorar correctamente archivos en las nuevas ubicaciones.

**Pasos**:
1. Abrir `.gitignore` en raíz
2. Agregar patrones para:
   ```
   backend/bin/
   backend/obj/
   backend/packages/
   frontend/web/node_modules/
   frontend/web/dist/
   frontend/shared/node_modules/
   docs/node_modules/
   .env.local
   .env.*.local
   ```

3. Probar con:
   ```bash
   git status
   ```

**Definición de Hecho**:
- [ ] `.gitignore` actualizado
- [ ] `git status` limpio (solo cambios esperados)
- [ ] Commit confirmado

---

### Tarea 3: Crear documentación base (README, ARCHITECTURE, SETUP)
**Duración**: 1 hora  
**Complejidad**: 🟡 Media  
**Dependencias**: Tarea 1

**Descripción**:
Crear documentación principal del proyecto.

**Pasos**:
1. Crear `README.md` en raíz:
   ```markdown
   # BeautyCommerce
   Plataforma de e-commerce especializada en productos de belleza.
   
   ## Estructura del Proyecto
   - `backend/` - API REST (.NET Core)
   - `frontend/` - Aplicación web (React)
   - `docs/` - Documentación
   - `infrastructure/` - Configuración DevOps
   
   ## Quick Start
   [Ver SETUP.md]
   ```

2. Crear `docs/ARCHITECTURE.md` con diagrama:
   ```
   Frontend (React)
        ↕
   API Backend (.NET)
        ↕
   Database + Cache
   ```

3. Crear `docs/SETUP.md` con instrucciones básicas

**Definición de Hecho**:
- [ ] README.md creado y actualizado
- [ ] docs/ARCHITECTURE.md creado
- [ ] docs/SETUP.md creado
- [ ] Links válidos
- [ ] Archivos confirmados en git

---

### Tarea 4: Crear estructura de carpetas para backend
**Duración**: 45 minutos  
**Complejidad**: 🟢 Baja  
**Dependencias**: Tarea 1

**Descripción**:
Preparar carpetas dentro de `backend/` para recibir proyectos C#.

**Pasos**:
1. Crear en `backend/`:
   - `backend/src/`
   - `backend/tests/`

2. Copiar (no mover aún):
   - `docker-compose.yml` → `backend/docker-compose.yml`
   - `Dockerfile` → `backend/Dockerfile` (si existe)

3. Crear `backend/README.md` con guía específica

**Definición de Hecho**:
- [ ] Carpetas creadas
- [ ] docker-compose.yml en backend/
- [ ] backend/README.md creado

---

### Tarea 5: Crear estructura de carpetas para frontend
**Duración**: 45 minutos  
**Complejidad**: 🟢 Baja  
**Dependencias**: Tarea 1

**Descripción**:
Preparar carpetas dentro de `frontend/` para la aplicación web.

**Pasos**:
1. Crear `frontend/web/` con subcarpetas:
   - `src/app/`
   - `src/shared/`
   - `src/core/`
   - `src/assets/`
   - `src/styles/`
   - `src/types/`
   - `tests/`
   - `public/`

2. Crear `frontend/README.md`

3. Crear `frontend/shared/types/` con tipos iniciales

**Definición de Hecho**:
- [ ] Carpetas de frontend creadas
- [ ] Estructura lista para Vite
- [ ] frontend/README.md creado

---

## FASE 2: REORGANIZACIÓN BACKEND (Tareas 6-12)

### Tarea 6: Mover proyectos C# a backend/src/ con git mv
**Duración**: 1.5 horas  
**Complejidad**: 🟡 Media  
**Dependencias**: Tarea 4

**Descripción**:
Mover proyectos C# a `backend/src/` preservando historia de git.

**Pasos**:
1. Usar `git mv` para cada proyecto:
   ```powershell
   cd C:\Users\ducua\OneDrive\Desktop\beauty\BeautyCommerce
   git mv BeautyCommerce.API backend/src/BeautyCommerce.API
   git mv BeautyCommerce.Application backend/src/BeautyCommerce.Application
   git mv BeautyCommerce.Domain backend/src/BeautyCommerce.Domain
   git mv BeautyCommerce.Infrastructure backend/src/BeautyCommerce.Infrastructure
   ```

2. Compilar para validar:
   ```powershell
   cd backend
   dotnet build
   ```

3. Si hay errores, actualizar referencias en `.sln`

**Definición de Hecho**:
- [ ] Todos los proyectos movidos con `git mv`
- [ ] `.sln` actualizado
- [ ] Compilación exitosa
- [ ] Commit confirmado

---

### Tarea 7: Mover tests a backend/tests/
**Duración**: 45 minutos  
**Complejidad**: 🟡 Media  
**Dependencias**: Tarea 6

**Descripción**:
Mover proyectos de tests a `backend/tests/`.

**Pasos**:
1. Mover proyectos:
   ```powershell
   git mv BeautyCommerce.Application.Tests backend/tests/BeautyCommerce.Application.Tests
   git mv BeautyCommerce.Tests backend/tests/BeautyCommerce.Tests
   ```

2. Actualizar `.sln`:
   ```xml
   Project("{...}") = "BeautyCommerce.Application.Tests", "backend/tests/BeautyCommerce.Application.Tests/..."
   ```

3. Ejecutar tests:
   ```powershell
   cd backend
   dotnet test
   ```

**Definición de Hecho**:
- [ ] Tests movidos
- [ ] `.sln` actualizado
- [ ] Todos los tests pasan
- [ ] Commit confirmado

---

### Tarea 8: Reorganizar Application layer para CQRS
**Duración**: 2 horas  
**Complejidad**: 🟡 Media  
**Dependencias**: Tarea 6

**Descripción**:
Reestructurar `BeautyCommerce.Application/` siguiendo patrón CQRS.

**Pasos**:
1. Dentro de `BeautyCommerce.Application/`, crear:
   ```
   ├── Commands/
   │   ├── Products/
   │   ├── Orders/
   │   └── Users/
   ├── Queries/
   │   ├── Products/
   │   ├── Orders/
   │   └── Users/
   ├── Handlers/
   │   ├── CommandHandlers/
   │   └── QueryHandlers/
   ├── DTOs/
   ├── Validators/
   └── Interfaces/
   ```

2. Mover archivos existentes a carpetas apropiadas

3. Actualizar namespaces en código:
   ```csharp
   namespace BeautyCommerce.Application.Commands.Products { }
   namespace BeautyCommerce.Application.Queries.Products { }
   ```

4. Compilar y validar:
   ```powershell
   dotnet build
   ```

**Definición de Hecho**:
- [ ] Estructura CQRS implementada
- [ ] Namespaces actualizados
- [ ] Compilación exitosa
- [ ] Tests pasan
- [ ] Commit confirmado

---

### Tarea 9: Reorganizar Domain layer (DDD)
**Duración**: 1.5 horas  
**Complejidad**: 🟡 Media  
**Dependencias**: Tarea 6

**Descripción**:
Reestructurar `BeautyCommerce.Domain/` siguiendo Domain-Driven Design.

**Pasos**:
1. Dentro de `BeautyCommerce.Domain/`, crear:
   ```
   ├── Entities/
   ├── ValueObjects/
   ├── Aggregates/
   ├── Events/
   ├── Interfaces/
   └── Exceptions/
   ```

2. Mover entidades existentes:
   ```csharp
   BeautyCommerce.Domain/Entities/Product.cs
   BeautyCommerce.Domain/Entities/Order.cs
   ```

3. Actualizar namespaces

4. Compilar y validar

**Definición de Hecho**:
- [ ] Estructura DDD implementada
- [ ] Namespaces actualizados
- [ ] Compilación exitosa
- [ ] Commit confirmado

---

### Tarea 10: Reorganizar Infrastructure layer
**Duración**: 1.5 horas  
**Complejidad**: 🟡 Media  
**Dependencias**: Tarea 6

**Descripción**:
Reestructurar `BeautyCommerce.Infrastructure/` para separar concerns.

**Pasos**:
1. Crear estructura:
   ```
   ├── Persistence/
   │   ├── Context/
   │   └── Repositories/
   ├── ExternalServices/
   ├── Caching/
   ├── EventBus/
   └── Configuration/
   ```

2. Mover DbContext a `Persistence/Context/`

3. Mover repositorios a `Persistence/Repositories/`

4. Mover servicios externos

5. Compilar y validar

**Definición de Hecho**:
- [ ] Estructura organizada
- [ ] Namespaces actualizados
- [ ] Compilación exitosa
- [ ] Commit confirmado

---

### Tarea 11: Reorganizar API layer (Controllers)
**Duración**: 1 hora  
**Complejidad**: 🟢 Baja  
**Dependencias**: Tarea 6

**Descripción**:
Organizar controllers en `BeautyCommerce.API/`.

**Pasos**:
1. Crear carpetas:
   ```
   ├── Controllers/
   │   ├── ProductsController.cs
   │   ├── OrdersController.cs
   │   └── UsersController.cs
   ├── Middleware/
   ├── Filters/
   ```

2. Mover controllers existentes

3. Crear controllers faltantes si es necesario

4. Compilar y validar

**Definición de Hecho**:
- [ ] Controllers organizados
- [ ] Compilación exitosa
- [ ] API responde
- [ ] Commit confirmado

---

### Tarea 12: Ejecutar suite completa de tests backend
**Duración**: 45 minutos  
**Complejidad**: 🟢 Baja  
**Dependencias**: Tareas 7-11

**Descripción**:
Ejecutar todos los tests para validar que la reorganización no rompió nada.

**Pasos**:
1. Ejecutar:
   ```powershell
   cd backend
   dotnet test
   ```

2. Si hay fallos, investigar y corregir

3. Ejecutar con cobertura:
   ```powershell
   dotnet test /p:CollectCoverage=true
   ```

4. Documentar cobertura alcanzada

**Definición de Hecho**:
- [ ] Todos los tests pasan
- [ ] Cobertura ≥ 80%
- [ ] Reporte documentado
- [ ] Commit confirmado

---

## FASE 3: PREPARAR FRONTEND (Tareas 13-16)

### Tarea 13: Inicializar proyecto Vite + React + TypeScript
**Duración**: 1 hora  
**Complejidad**: 🟡 Media  
**Dependencias**: Tarea 5

**Descripción**:
Crear proyecto base React con Vite.

**Pasos**:
1. Ejecutar:
   ```powershell
   cd frontend\web
   npm create vite@latest . -- --template react-ts
   npm install
   ```

2. Verificar que inicia:
   ```powershell
   npm run dev
   ```

3. Crear carpetas base:
   ```
   src/app/
   src/core/
   src/shared/
   tests/
   ```

4. Crear `src/App.tsx` básico

**Definición de Hecho**:
- [ ] Proyecto Vite creado
- [ ] npm install exitoso
- [ ] `npm run dev` funciona
- [ ] Estructura base lista
- [ ] Commit confirmado

---

### Tarea 14: Crear tipos TypeScript compartidos (frontend/shared)
**Duración**: 1 hora  
**Complejidad**: 🟡 Media  
**Dependencias**: Tarea 13

**Descripción**:
Crear paquete npm con tipos compartidos entre backend y frontend.

**Pasos**:
1. Crear `frontend/shared/package.json`:
   ```json
   {
     "name": "@beauty/shared",
     "version": "1.0.0",
     "main": "types/index.ts"
   }
   ```

2. Crear `frontend/shared/types/index.ts`:
   ```typescript
   export interface Product {
     id: string;
     name: string;
     price: number;
   }
   
   export interface Order {
     id: string;
     items: OrderItem[];
     total: number;
   }
   
   export interface User {
     id: string;
     email: string;
     name: string;
   }
   ```

3. En `frontend/web/package.json`, agregar:
   ```json
   {
     "dependencies": {
       "@beauty/shared": "workspace:*"
     }
   }
   ```

4. Verificar importaciones

**Definición de Hecho**:
- [ ] Paquete compartido creado
- [ ] Tipos definidos
- [ ] Workspace configurado
- [ ] Importaciones funcionan
- [ ] Commit confirmado

---

### Tarea 15: Crear servicio API client (axios + interceptores)
**Duración**: 1.5 horas  
**Complejidad**: 🟡 Media  
**Dependencias**: Tarea 14

**Descripción**:
Crear cliente HTTP centralizado para comunicarse con el backend.

**Pasos**:
1. Instalar:
   ```bash
   npm install axios
   ```

2. Crear `src/core/services/api.service.ts`:
   ```typescript
   import axios, { AxiosInstance } from 'axios';
   
   const apiClient: AxiosInstance = axios.create({
     baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000',
     timeout: 30000
   });
   
   apiClient.interceptors.request.use((config) => {
     const token = localStorage.getItem('token');
     if (token) {
       config.headers.Authorization = `Bearer ${token}`;
     }
     return config;
   });
   
   export default apiClient;
   ```

3. Crear métodos genéricos en el servicio

4. Crear `src/core/services/product.service.ts`:
   ```typescript
   import apiClient from './api.service';
   import { Product } from '@beauty/shared';
   
   export const productService = {
     getAll: () => apiClient.get<Product[]>('/products'),
     getById: (id: string) => apiClient.get<Product>(`/products/${id}`)
   };
   ```

**Definición de Hecho**:
- [ ] axios instalado
- [ ] api.service.ts creado
- [ ] Interceptores implementados
- [ ] Servicios de dominio creados
- [ ] Commit confirmado

---

### Tarea 16: Crear estructura de módulos (app/catalog, app/cart, app/auth)
**Duración**: 1.5 horas  
**Complejidad**: 🟡 Media  
**Dependencias**: Tarea 15

**Descripción**:
Crear estructura modular base para la aplicación.

**Pasos**:
1. Crear módulos base:
   ```
   src/app/
   ├── catalog/
   │   ├── pages/
   │   │   └── CatalogPage.tsx
   │   ├── components/
   │   └── services/
   ├── cart/
   │   ├── pages/
   │   ├── components/
   │   └── services/
   ├── auth/
   │   ├── pages/
   │   │   ├── LoginPage.tsx
   │   │   └── RegisterPage.tsx
   │   ├── components/
   │   └── services/
   └── admin/
       ├── pages/
       ├── components/
       └── services/
   ```

2. Crear componentes básicos para cada módulo

3. Crear routing base en `src/App.tsx`:
   ```typescript
   import { BrowserRouter, Routes, Route } from 'react-router-dom';
   import CatalogPage from './app/catalog/pages/CatalogPage';
   
   function App() {
     return (
       <BrowserRouter>
         <Routes>
           <Route path="/" element={<CatalogPage />} />
         </Routes>
       </BrowserRouter>
     );
   }
   ```

4. Compilar y validar:
   ```bash
   npm run build
   npm run dev
   ```

**Definición de Hecho**:
- [ ] Módulos creados
- [ ] Routing configurado
- [ ] Componentes base funcionales
- [ ] Build exitoso
- [ ] Commit confirmado

---

## FASE 4: DEVOPS Y CI/CD (Tareas 17-20)

### Tarea 17: Configurar GitHub Actions para Backend
**Duración**: 1 hora  
**Complejidad**: 🟡 Media  
**Dependencias**: Tarea 6

**Descripción**:
Crear workflow CI/CD para backend.

**Pasos**:
1. Crear `.github/workflows/backend-build-test.yml`:
   ```yaml
   name: Backend CI/CD
   on:
     push:
       paths:
         - 'backend/**'
         - '.github/workflows/backend-*'
   
   jobs:
     build:
       runs-on: ubuntu-latest
       steps:
         - uses: actions/checkout@v3
         - uses: actions/setup-dotnet@v3
           with:
             dotnet-version: '7.0'
         - run: cd backend && dotnet build
         - run: cd backend && dotnet test
   ```

2. Probar con push a rama

3. Verificar que workflow corre

**Definición de Hecho**:
- [ ] Workflow creado
- [ ] Se dispara en cambios de backend
- [ ] Build y tests pasan en CI
- [ ] Commit confirmado

---

### Tarea 18: Configurar GitHub Actions para Frontend
**Duración**: 1 hora  
**Complejidad**: 🟡 Media  
**Dependencias**: Tarea 13

**Descripción**:
Crear workflow CI/CD para frontend.

**Pasos**:
1. Crear `.github/workflows/frontend-build-test.yml`:
   ```yaml
   name: Frontend CI/CD
   on:
     push:
       paths:
         - 'frontend/**'
         - '.github/workflows/frontend-*'
   
   jobs:
     build:
       runs-on: ubuntu-latest
       steps:
         - uses: actions/checkout@v3
         - uses: actions/setup-node@v3
           with:
             node-version: '18'
         - run: cd frontend/web && npm ci
         - run: cd frontend/web && npm run build
         - run: cd frontend/web && npm test
   ```

2. Probar con push

3. Verificar que workflow corre

**Definición de Hecho**:
- [ ] Workflow creado
- [ ] Se dispara en cambios de frontend
- [ ] Build y tests pasan en CI
- [ ] Commit confirmado

---

### Tarea 19: Actualizar docker-compose.yml para stack completo
**Duración**: 1.5 horas  
**Complejidad**: 🟡 Media  
**Dependencias**: Tarea 12

**Descripción**:
Configurar Docker para ejecutar backend localmente.

**Pasos**:
1. Actualizar `backend/docker-compose.yml`:
   ```yaml
   version: '3.8'
   services:
     sqlserver:
       image: mcr.microsoft.com/mssql/server:2019-latest
       environment:
         SA_PASSWORD: YourPassword123!
         ACCEPT_EULA: Y
       ports:
         - "1433:1433"
     
     redis:
       image: redis:7-alpine
       ports:
         - "6379:6379"
   ```

2. Actualizar rutas de Dockerfile si existe

3. Probar:
   ```powershell
   cd backend
   docker-compose up -d
   docker-compose down
   ```

**Definición de Hecho**:
- [ ] docker-compose.yml actualizado
- [ ] Servicios inician correctamente
- [ ] Puertos accesibles
- [ ] Commit confirmado

---

### Tarea 20: Crear appsettings.json para configuración
**Duración**: 45 minutos  
**Complejidad**: 🟢 Baja  
**Dependencias**: Tarea 12

**Descripción**:
Configurar appsettings para desarrollo y producción.

**Pasos**:
1. Crear `backend/src/BeautyCommerce.API/appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=BeautyCommerce;User Id=sa;Password=YourPassword123!;"
     },
     "CacheSettings": {
       "Enabled": true,
       "RedisConnection": "localhost:6379"
     },
     "Logging": {
       "LogLevel": {
         "Default": "Debug"
       }
     }
   }
   ```

2. Crear `.env.example` para referencia

3. Actualizar `.gitignore` para no commitear secretos

**Definición de Hecho**:
- [ ] appsettings actualizado
- [ ] .env.example creado
- [ ] .gitignore protege secretos
- [ ] Commit confirmado

---

## FASE 5: DOCUMENTACIÓN (Tareas 21-23)

### Tarea 21: Documentar estructura CQRS en backend
**Duración**: 1 hora  
**Complejidad**: 🟡 Media  
**Dependencias**: Tarea 8

**Descripción**:
Crear documentación de cómo usar CQRS en el proyecto.

**Pasos**:
1. Crear `backend/docs/CQRS-GUIDE.md` con:
   - Explicación del patrón
   - Ejemplo de Command
   - Ejemplo de Query
   - Cómo agregar nuevas features

2. Actualizar `backend/README.md` con referencias

**Definición de Hecho**:
- [ ] Documentación creada
- [ ] Ejemplos incluidos
- [ ] Links válidos
- [ ] Commit confirmado

---

### Tarea 22: Documentar estructura de Frontend
**Duración**: 1 hora  
**Complejidad**: 🟡 Media  
**Dependencias**: Tarea 16

**Descripción**:
Crear documentación de arquitectura frontend.

**Pasos**:
1. Crear `frontend/web/docs/ARCHITECTURE.md` con:
   - Estructura modular
   - Ejemplo de componente
   - Cómo agregar nueva página
   - Testing

2. Actualizar `frontend/web/README.md`

**Definición de Hecho**:
- [ ] Documentación creada
- [ ] Ejemplos incluidos
- [ ] Links válidos
- [ ] Commit confirmado

---

### Tarea 23: Crear guía de CONTRIBUTING.md
**Duración**: 45 minutos  
**Complejidad**: 🟡 Media  
**Dependencias**: Tareas 17-18

**Descripción**:
Crear guía para nuevos contribuidores.

**Pasos**:
1. Crear `docs/CONTRIBUTING.md` con:
   - Setup del proyecto
   - Rama workflow (feature branches)
   - Commit message format
   - PR checklist
   - Testing requirements

**Definición de Hecho**:
- [ ] CONTRIBUTING.md creado
- [ ] Claro y accesible
- [ ] Commit confirmado

---

## FASE 6: VALIDACIÓN FINAL (Tareas 24-26)

### Tarea 24: Validar compilación y tests completos
**Duración**: 1 hora  
**Complejidad**: 🟢 Baja  
**Dependencias**: Tareas 12, 16

**Descripción**:
Ejecutar validación final de todo el proyecto.

**Pasos**:
1. Backend:
   ```powershell
   cd backend
   dotnet clean
   dotnet build
   dotnet test
   ```

2. Frontend:
   ```powershell
   cd frontend/web
   rm -r node_modules
   npm install
   npm run build
   npm test
   ```

3. Documentar resultados

**Definición de Hecho**:
- [ ] Backend build exitoso
- [ ] Backend tests pasan
- [ ] Frontend build exitoso
- [ ] Frontend tests pasan
- [ ] Reporte documentado

---

### Tarea 25: Validar Docker y ambiente local
**Duración**: 45 minutos  
**Complejidad**: 🟡 Media  
**Dependencias**: Tarea 19

**Descripción**:
Validar que todo funciona en ambiente Docker.

**Pasos**:
1. Stack local:
   ```powershell
   cd backend
   docker-compose up -d
   # Esperar 30 segundos
   # Verificar en http://localhost:1433
   ```

2. Backend API:
   ```powershell
   cd backend
   dotnet run --project src/BeautyCommerce.API
   ```

3. Frontend:
   ```powershell
   cd frontend/web
   npm run dev
   # Acceder a http://localhost:5173
   ```

**Definición de Hecho**:
- [ ] Docker services up
- [ ] API responde
- [ ] Frontend sirve
- [ ] Sin errores en logs
- [ ] Documentado

---

### Tarea 26: Merge a develop y crear tag de versión
**Duración**: 30 minutos  
**Complejidad**: 🟢 Baja  
**Dependencias**: Tarea 25

**Descripción**:
Finalizar la reestructuración con merge y tag.

**Pasos**:
1. Verificar status:
   ```bash
   git status
   git log --oneline -10
   ```

2. Crear tag:
   ```bash
   git tag -a v2.0.0 -m "Reestructuración CQRS y separación backend/frontend"
   git push origin v2.0.0
   ```

3. Crear pull request a main con descripción

4. Merge y celebrar 🎉

**Definición de Hecho**:
- [ ] Tag creado
- [ ] PR creado y documentado
- [ ] Merge a main
- [ ] Rama limpia

---

## Resumen de Tareas

| Fase | Tareas | Duración | Estado |
|------|--------|----------|--------|
| Preparación | 1-5 | 3.5 horas | 🔲 |
| Backend | 6-12 | 9 horas | 🔲 |
| Frontend | 13-16 | 4.5 horas | 🔲 |
| DevOps | 17-20 | 4 horas | 🔲 |
| Documentación | 21-23 | 2.75 horas | 🔲 |
| Validación | 24-26 | 2 horas | 🔲 |
| **TOTAL** | **26** | **~25 horas** | |

**Equivalente**: 3 días de trabajo intenso o 1 semana a ritmo normal

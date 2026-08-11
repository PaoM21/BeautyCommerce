# Diseño: Plan de Migración y Implementación

## Fase 1: Preparación y Setup (1-2 días)

### 1.1 Crear Estructura de Carpetas Base
**Objetivo**: Establecer la estructura raíz `backend/`, `frontend/`, `docs/`

**Pasos**:
1. En raíz del proyecto, crear carpetas:
   ```bash
   mkdir backend
   mkdir frontend
   mkdir frontend/web
   mkdir frontend/shared
   mkdir docs
   mkdir infrastructure
   ```

2. Mover archivos relevantes:
   - `.git/` (mantener en raíz)
   - `Dockerfile` y `docker-compose.yml` → `backend/`
   - Proyectos C# → `backend/src/`
   - Crear `frontend/web/` (aún no hay aplicación web)

3. Actualizar `.gitignore` para nuevas rutas

4. Verificar que git sigue funcionando correctamente

### 1.2 Crear Repositorios de Configuración
**Objetivo**: Estructura inicial de configuración

**Pasos**:
1. Crear `README.md` en raíz explicando estructura
2. Crear `docs/ARCHITECTURE.md` con diagrama general
3. Crear `docs/SETUP.md` con instrucciones de setup
4. Crear `.github/workflows/` (vacío por ahora)

**Salida esperada:**
```
beauty/
├── backend/
│   ├── BeautyCommerce.API/
│   ├── BeautyCommerce.Application/
│   ├── ...
│   ├── docker-compose.yml
│   └── README.md
├── frontend/
│   ├── web/
│   ├── shared/
│   └── README.md
├── docs/
│   ├── ARCHITECTURE.md
│   ├── SETUP.md
│   └── README.md
├── README.md
└── .git/
```

---

## Fase 2: Reorganización Backend (3-5 días)

### 2.1 Reestructurar Proyectos C#
**Objetivo**: Reorganizar bajo `backend/src/` con estructura CQRS clara

**Pasos**:
1. Mover proyectos a `backend/src/`:
   - `BeautyCommerce.API/`
   - `BeautyCommerce.Application/`
   - `BeautyCommerce.Domain/`
   - `BeautyCommerce.Infrastructure/`

2. Actualizar archivo `.sln` (rutas de proyectos)

3. Reorganizar `BeautyCommerce.Application/`:
   ```
   Application/
   ├── Commands/
   │   └── [Feature]/
   │       └── *Command.cs
   ├── Queries/
   │   └── [Feature]/
   │       └── *Query.cs
   ├── Handlers/
   │   ├── Commands/
   │   └── Queries/
   ├── DTOs/
   ├── Validators/
   └── Interfaces/
   ```

4. Verificar compilación después de cada cambio

### 2.2 Reorganizar Tests
**Objetivo**: Separar tests en carpeta `backend/tests/`

**Pasos**:
1. Mover `BeautyCommerce.Application.Tests/` → `backend/tests/`
2. Mover `BeautyCommerce.Tests/` → `backend/tests/`
3. Actualizar referencias en `.sln`
4. Ejecutar todos los tests para validar

### 2.3 Actualizar Docker
**Objetivo**: Configurar Docker para nueva estructura

**Pasos**:
1. Actualizar `backend/docker-compose.yml`
2. Actualizar `backend/Dockerfile` (si existe)
3. Ajustar rutas de contexto en docker-compose
4. Verificar que servicios inician correctamente

**Validación**:
```bash
cd backend
docker-compose up -d
dotnet test
```

---

## Fase 3: Preparar Frontend (2-3 días)

### 3.1 Inicializar Proyecto Web
**Objetivo**: Crear estructura base de aplicación web

**Pasos**:
1. Crear `frontend/web/` con estructura inicial:
   ```bash
   cd frontend/web
   npm create vite@latest . -- --template react-ts
   ```

2. Crear carpetas base:
   ```
   src/
   ├── app/
   ├── shared/
   ├── core/
   ├── assets/
   ├── styles/
   └── types/
   ```

3. Instalar dependencias iniciales:
   ```bash
   npm install axios react-router-dom zustand
   npm install -D vitest @testing-library/react tailwindcss
   ```

### 3.2 Crear Tipos Compartidos
**Objetivo**: Tipos TypeScript para comunicación API

**Pasos**:
1. Crear `frontend/shared/types/`:
   ```typescript
   // api.types.ts
   export interface Product { }
   export interface Order { }
   export interface User { }
   ```

2. Crear `package.json` en `frontend/shared/`

3. Referencia desde web:
   ```json
   {
     "dependencies": {
       "@beauty/shared": "workspace:*"
     }
   }
   ```

### 3.3 Crear API Client
**Objetivo**: Cliente HTTP centralizado

**Pasos**:
1. Crear `frontend/web/src/core/services/api.service.ts`
2. Implementar interceptores para autenticación
3. Configurar base URL desde variables de entorno
4. Crear métodos genéricos para GET, POST, PUT, DELETE

---

## Fase 4: Configuración DevOps (2-3 días)

### 4.1 GitHub Actions - CI/CD Backend
**Objetivo**: Pipeline de integración continua

**Archivo**: `.github/workflows/backend-build-test.yml`

```yaml
name: Backend CI/CD
on:
  push:
    paths:
      - 'backend/**'

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
      - run: cd backend && dotnet build
      - run: cd backend && dotnet test
```

### 4.2 GitHub Actions - CI/CD Frontend
**Objetivo**: Pipeline para aplicación web

**Archivo**: `.github/workflows/frontend-build-test.yml`

```yaml
name: Frontend CI/CD
on:
  push:
    paths:
      - 'frontend/**'

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

### 4.3 Docker Compose Completo
**Objetivo**: Stack funcional local

**Actualizar** `backend/docker-compose.yml`:
```yaml
version: '3.8'
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server
    environment:
      SA_PASSWORD: YourPassword123!
      ACCEPT_EULA: Y

  redis:
    image: redis:latest

  api:
    build: .
    ports:
      - "5000:80"
    depends_on:
      - sqlserver
      - redis
    environment:
      ConnectionStrings__DefaultConnection: ...
```

---

## Fase 5: Documentación y Guías (1-2 días)

### 5.1 Documentación Principal
**Archivos**:
- `docs/README.md` - Índice y guía rápida
- `docs/ARCHITECTURE.md` - Diagrama y explicación
- `docs/SETUP.md` - Instrucciones completas
- `docs/CONTRIBUTING.md` - Guía para colaboradores

### 5.2 README Específicos
- `backend/README.md` - Guía backend
- `frontend/web/README.md` - Guía frontend

### 5.3 Actualizar README Raíz
- Estructura del proyecto
- Links a documentación
- Quick start

---

## Fase 6: Validación e Integración (1-2 días)

### 6.1 Validación de Compilación
**Checklist**:
```
□ Backend compila sin errores
□ Tests de backend pasan
□ Frontend compila sin errores
□ Tests de frontend pasan
```

### 6.2 Validación de Docker
```
□ docker-compose up funciona
□ API responde en http://localhost:5000
□ BD está disponible
□ Redis está disponible
```

### 6.3 Validación de Git
```
□ Historia de git intacta
□ .gitignore actualizado
□ Ramas funcionales
□ Workflows disparan correctamente
```

### 6.4 Validación de Documentación
```
□ README.md actualizado
□ SETUP.md completo
□ ARCHITECTURE.md claro
□ Links válidos
```

---

## Riesgos y Mitigación

| Riesgo | Probabilidad | Impacto | Mitigación |
|--------|-------------|--------|-----------|
| Compilación falla después de mover | Media | Alto | Mover por lotes, compilar frecuentemente |
| Tests rompen | Media | Medio | Ejecutar tests después de cada cambio |
| Git history se corrompe | Baja | Crítico | Usar `git mv`, no copiar/eliminar |
| Referencias de rutas rotas | Alta | Medio | Actualizar `.sln` y config cuidadosamente |

---

## Timeline Estimado

```
Fase 1: Preparación                    1-2 días
Fase 2: Backend                        3-5 días
Fase 3: Frontend                       2-3 días
Fase 4: DevOps                         2-3 días
Fase 5: Documentación                  1-2 días
Fase 6: Validación                     1-2 días
────────────────────────────────────────────────
TOTAL:                               10-17 días
```

---

## Comandos Clave

### Mover con Git (preserva historia)
```bash
git mv BeautyCommerce.API backend/src/BeautyCommerce.API
git mv BeautyCommerce.Application backend/src/BeautyCommerce.Application
git mv BeautyCommerce.Domain backend/src/BeautyCommerce.Domain
git mv BeautyCommerce.Infrastructure backend/src/BeautyCommerce.Infrastructure
```

### Compilar y Testear Backend
```bash
cd backend
dotnet build
dotnet test
```

### Compilar y Testear Frontend
```bash
cd frontend/web
npm install
npm run build
npm test
```

### Iniciar Stack Completo
```bash
cd backend
docker-compose up -d
# En otra terminal
cd frontend/web
npm run dev
```

---

## Criterios de Éxito

✅ Estructura de carpetas implementada  
✅ Todos los proyectos compilan  
✅ Todos los tests pasan  
✅ Docker funciona  
✅ Git history preservado  
✅ Documentación completa  
✅ Workflows CI/CD funcionales  
✅ Proyecto listo para desarrollo  

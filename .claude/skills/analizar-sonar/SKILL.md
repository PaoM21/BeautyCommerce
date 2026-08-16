---
name: analizar-sonar
description: Compila y analiza el backend (.NET) o el frontend (React/Vite) de BeautyCommerce con SonarScanner y envía los resultados al SonarQube local. Pregunta al usuario si quiere analizar el front o el back. Úsalo cuando el usuario pida "analiza el proyecto con sonar", "corre el análisis de sonar", "escanea con sonarqube" o similar.
user-invocable: true
allowed-tools:
  - Bash(*)
  - Bash(dotnet *)
  - Bash(npm *)
  - Bash(docker *)
  - Bash(curl *)
---

# /analizar-sonar — Analizar backend o frontend con SonarQube local

Depende del entorno que levanta el skill **levantar-sonar** (SonarQube en `http://localhost:9002`, servicios definidos en `sonar/docker-compose.yml`). No dupliques esa lógica: si SonarQube no está arriba, sigue el mismo procedimiento de esa skill para levantarlo antes de continuar.

## Qué hace

1. Pregunta al usuario si quiere analizar el **backend** o el **frontend** (si no lo dijo ya en el mismo mensaje que disparó el skill).
2. Verifica que SonarQube local esté corriendo; si no, lo levanta (mismos pasos que `levantar-sonar`).
3. Resuelve el token de autenticación contra SonarQube.
4. Compila el proyecto elegido y ejecuta el scanner correspondiente contra `http://localhost:9002`.
5. Informa al usuario el resultado y el link al dashboard del proyecto en SonarQube.

## Pasos

### 1. Elegir front o back

Si el mensaje del usuario no especifica cuál, pregúntaselo directamente (front o back) antes de ejecutar nada.

### 2. Asegurar que SonarQube está arriba

```
curl -sf http://localhost:9002/api/system/status
```

Si falla o el `status` no es `UP`, ejecuta los mismos pasos del skill `levantar-sonar` (`docker compose -f sonar/docker-compose.yml up -d`, esperar a `UP`) antes de seguir.

### 3. Resolver el token de SonarQube

SonarQube local exige autenticación (`sonar.forceAuthentication=true`). Busca un token en la variable de entorno `SONAR_TOKEN`.
- Si existe, úsalo.
- Si no existe, pídeselo al usuario y explícale cómo generarlo si no tiene uno: entrar a `http://localhost:9002` → `My Account` → `Security` → `Generate Token`. No lo inventes ni lo dejes vacío — sin token la publicación de resultados falla.

No imprimas el token en texto plano en tu respuesta al usuario; pásalo solo como argumento del comando.

### 4a. Analizar el backend (.NET)

Desde `backend/`:

```
dotnet sonarscanner begin /k:"beautycommerce-backend" /d:sonar.host.url="http://localhost:9002" /d:sonar.token="<TOKEN>"
dotnet build BeautyCommerce.sln
dotnet sonarscanner end /d:sonar.token="<TOKEN>"
```

Si `dotnet-sonarscanner` no está instalado como herramienta global, instálalo primero con `dotnet tool install --global dotnet-sonarscanner` (ya se usó antes en este repo, así que probablemente ya está).

### 4b. Analizar el frontend (React/Vite)

Desde `frontend/web/`:

1. Compila para detectar errores antes de escanear: `npm run build`.
2. Ejecuta el scanner con la imagen oficial de SonarSource, montando el proyecto (usa `host.docker.internal` para llegar al SonarQube que corre en el host desde dentro del contenedor):

```
docker run --rm -e SONAR_HOST_URL="http://host.docker.internal:9002" -e SONAR_TOKEN="<TOKEN>" -v "$(pwd):/usr/src" sonarsource/sonar-scanner-cli
```

El proyecto ya tiene `frontend/web/sonar-project.properties` con `sonar.projectKey=beautycommerce-frontend` y `sonar.sources=src`, así que el scanner los toma automáticamente sin flags adicionales.

### 5. Reportar resultado

Al terminar, confirma que el análisis se envió correctamente (el scanner imprime `EXECUTION SUCCESS` / `ANALYSIS SUCCESSFUL`) y dale al usuario el link directo al proyecto:
- Backend: `http://localhost:9002/dashboard?id=beautycommerce-backend`
- Frontend: `http://localhost:9002/dashboard?id=beautycommerce-frontend`

Si el scanner falla, muestra el motivo del error tal cual lo reporta la herramienta (no lo resumas ni lo omitas) para que el usuario sepa qué corregir.

## Notas

- No mezcles análisis de front y back en una sola ejecución salvo que el usuario pida explícitamente ambos.
- No cambies `sonar.projectKey` de ninguno de los dos proyectos sin que el usuario lo pida — romperías el historial de análisis en SonarQube.

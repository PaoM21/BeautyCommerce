---
name: levantar-sonar
description: Levanta el entorno local de SonarQube (Postgres + SonarQube) con Docker Compose desde BeautyCommerce/sonar, espera a que el servidor quede disponible y devuelve al usuario la URL de acceso en localhost. Úsalo cuando el usuario pida "levantar sonar", "iniciar sonarqube", "arrancar sonar" o similar.
user-invocable: true
allowed-tools:
  - Bash(docker *)
  - Bash(curl *)
---

# /levantar-sonar — Levantar SonarQube local

## Qué hace

1. Levanta con Docker Compose los servicios de `sonar/` (Postgres + SonarQube).
2. Espera activamente a que la API de SonarQube reporte estado `UP`.
3. Devuelve al usuario la URL local para entrar al dashboard.

## Pasos

1. **Verifica Docker**: `docker info` (silencioso). Si falla, dile al usuario que Docker Desktop no está corriendo y detente ahí — no tiene sentido seguir.

2. **Levanta los servicios** desde la carpeta `sonar/` del repo (contiene `docker-compose.yml` y `Dockerfile`):
   ```
   docker compose -f sonar/docker-compose.yml up -d --build
   ```
   Usa `--build` solo si es la primera vez o si el `Dockerfile` cambió; si los contenedores ya existen y están corriendo, un `up -d` normal basta. Puedes comprobar el estado antes con `docker compose -f sonar/docker-compose.yml ps`.

3. **Espera a que SonarQube esté listo**: consulta `http://localhost:9002/api/system/status` en un bucle corto (cada 5s, hasta ~2 minutos) hasta que la respuesta contenga `"status":"UP"`. SonarQube tarda entre 30s y 2 min en arrancar la primera vez porque inicializa la base de datos.
   - Si tras 2 minutos sigue en `STARTING` o `DB_MIGRATION_NEEDED`, sigue esperando un poco más (es normal en el primer arranque) e infórmale al usuario que sigue inicializando.
   - Si devuelve `DOWN` o no responde tras varios minutos, revisa `docker compose -f sonar/docker-compose.yml logs sonarqube --tail=50` y muéstraselo al usuario.

4. **Responde al usuario** con:
   - La URL: **http://localhost:9002**
   - Si es el primer arranque (contenedores recién creados), recuérdale que el login inicial es `admin` / `admin` y que SonarQube pedirá cambiar la contraseña.

## Notas

- El puerto expuesto es `9002` (mapeado a `9000` dentro del contenedor), definido en `sonar/docker-compose.yml`.
- No detengas ni elimines los contenedores como parte de este skill — solo levántalos. Si el usuario pide bajar el entorno, usa `docker compose -f sonar/docker-compose.yml down` (sin `-v`, para no perder los datos del volumen a menos que el usuario lo pida explícitamente).

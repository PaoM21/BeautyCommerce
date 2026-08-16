# Dockerfile para SonarQube
# Extiende la imagen oficial para permitir personalizaciones futuras.
ARG SONARQUBE_VERSION=10-community
FROM sonarqube:${SONARQUBE_VERSION}

# Mantener la imagen oficial tal cual; sirve para producción y pruebas.
LABEL maintainer="you@example.com"

# Si se desea añadir plugins personalizados, colocar los .jar en ./extensions
# y descomentar la siguiente línea:
# COPY extensions/ /opt/sonarqube/extensions/plugins/

# No se cambia el usuario ni el entrypoint de la imagen oficial.

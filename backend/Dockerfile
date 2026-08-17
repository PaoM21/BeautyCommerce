# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/BeautyCommerce.API/BeautyCommerce.API.csproj src/BeautyCommerce.API/
COPY src/BeautyCommerce.Application/BeautyCommerce.Application.csproj src/BeautyCommerce.Application/
COPY src/BeautyCommerce.Infrastructure/BeautyCommerce.Infrastructure.csproj src/BeautyCommerce.Infrastructure/
COPY src/BeautyCommerce.Domain/BeautyCommerce.Domain.csproj src/BeautyCommerce.Domain/

RUN dotnet restore src/BeautyCommerce.API/BeautyCommerce.API.csproj

COPY src/ src/

RUN dotnet publish src/BeautyCommerce.API/BeautyCommerce.API.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Puerto fijo en el que escucha Kestrel dentro del contenedor.
# En Render, configura el servicio web para enrutar tráfico a este puerto.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "BeautyCommerce.API.dll"]

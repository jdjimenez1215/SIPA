# syntax=docker/dockerfile:1

# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Props y csproj primero: la capa de restore se cachea mientras no cambien las dependencias.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/MsInscripcion.Domain/MsInscripcion.Domain.csproj src/MsInscripcion.Domain/
COPY src/MsInscripcion.Application/MsInscripcion.Application.csproj src/MsInscripcion.Application/
COPY src/MsInscripcion.Infrastructure/MsInscripcion.Infrastructure.csproj src/MsInscripcion.Infrastructure/
COPY src/MsInscripcion.Api/MsInscripcion.Api.csproj src/MsInscripcion.Api/

# Se restaura el proyecto Api (arrastra sus referencias); la .sln no se usa porque incluye tests/, excluido por .dockerignore.
RUN dotnet restore src/MsInscripcion.Api/MsInscripcion.Api.csproj

COPY src/ src/
RUN dotnet publish src/MsInscripcion.Api/MsInscripcion.Api.csproj \
    -c Release --no-restore -o /app/publish /p:UseAppHost=false

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish .

# La imagen aspnet:8.0 ya trae el usuario sin privilegios "app".
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "MsInscripcion.Api.dll"]

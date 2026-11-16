# syntax=docker/dockerfile:1

# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the csproj files first so the restore layer is cached.
COPY src/MiniIdentityApi.Domain/MiniIdentityApi.Domain.csproj src/MiniIdentityApi.Domain/
COPY src/MiniIdentityApi.Application/MiniIdentityApi.Application.csproj src/MiniIdentityApi.Application/
COPY src/MiniIdentityApi.Infrastructure/MiniIdentityApi.Infrastructure.csproj src/MiniIdentityApi.Infrastructure/
COPY src/MiniIdentityApi.Api/MiniIdentityApi.Api.csproj src/MiniIdentityApi.Api/

RUN dotnet restore src/MiniIdentityApi.Api/MiniIdentityApi.Api.csproj

COPY src/ src/
RUN dotnet publish src/MiniIdentityApi.Api/MiniIdentityApi.Api.csproj \
    -c Release --no-restore -o /app/publish /p:UseAppHost=false

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish .

# Non-root user shipped with the .NET runtime images.
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "MiniIdentityApi.Api.dll"]

# Despliegue en Azure

El repo incluye `azure-pipelines.yml`, que ejecuta tests, publica las tres imágenes en ACR y actualiza Container Apps.

## Recursos necesarios

- **Azure Container Registry** (`acrName`).
- **PostgreSQL Flexible Server** (o el servicio que uses) con las bases `inscripcion` e `identidad`.
- **Container Apps**: `api`, `identity`, `front` (este último con ingress externo; los otros dos internos).
- **Key Vault** o secretos de Container Apps:
  - `jwt-key` (mínimo 32 bytes, compartido).
  - `connection-strings-default` para api (`Database=inscripcion`) e identity (`Database=identidad`).
- Service connections de Azure DevOps: `azureServiceConnection` (ARM) y `acrServiceConnection` (Docker Registry).
- Variable group con `acrName`, `resourceGroup`, `apiApp`, `identityApp`, `frontApp`.

## Variables de entorno de Container Apps

**api**

```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
Swagger__Enabled=false
ConnectionStrings__Default=<secret>
Jwt__Issuer=MiniIdentityApi
Jwt__Audience=MiniIdentityApiUsers
Jwt__Key=<secret jwt-key>
```

**identity**

```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
ConnectionStrings__Default=<secret>
Jwt__Issuer=MiniIdentityApi
Jwt__Audience=MiniIdentityApiUsers
Jwt__Key=<secret jwt-key>
```

**front**

```
AUTH_ORIGIN=http://identity
ENROLLMENT_ORIGIN=http://api
```

## Notas

- `identity` aplica migraciones EF al arrancar (`Database:ApplyMigrationsOnStartup=true`).
- `api` aplica migraciones y carga la semilla al arrancar; en producción conviene desactivar la semilla o usar una migración de datos controlada.
- El front del compose es `dev-server.py`. Para producción se recomienda servir el front con un reverse proxy (nginx/Caddy) que replique las rutas de `dev-server.py`; el pipeline usa `Dockerfile.frontend` como punto de partida.
- No publiques los puertos de `api`/`identity`/`db` a Internet: solo el front debe tener ingress externo.

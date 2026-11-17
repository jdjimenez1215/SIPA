# Tasks: ms-asistente

- [x] 1.1 Crear `backend/ms-asistente/src/MsAsistente.Api` (.NET 10 minimal API).
- [x] 1.2 Autenticación JWT con la misma clave/issuer/audience que identity.
- [x] 1.3 `POST /api/asistente/chat` leyendo `estudianteId` solo del claim.
- [x] 1.4 `InscripcionClient` reenviando el Bearer del usuario.
- [x] 1.5 Abstracción `IAsistenteLlm` + Groq + Anthropic.
- [x] 1.6 Fallback determinístico `ReglasAsistenteLlm`.
- [x] 1.7 Sanitizar IDs: solo `Sugerida`, sin duplicados y dentro del límite de créditos.
- [x] 1.8 Explicaciones de bloqueos por motivo.
- [x] 1.9 Dockerfile no-root y health checks.
- [x] 2.1 Ruta `/api/asistente/*` en `dev-server.py`.
- [x] 2.2 Servicio `assistant` en `docker-compose.yml` + secretos por entorno.
- [x] 2.3 Panel de chat en `sugerencia-matricula.html` y `app.js`.
- [x] 2.4 La IA solo pre-marca casillas; Confirmar sigue igual.
- [x] 2.5 Manejo de 403/502 y mensaje de fallback en el front.
- [x] 3.1 Tests con LLM simulado (propuesta válida, límite, fallback, bloqueos).
- [x] 3.2 Smoke test del endpoint vía proxy con cuenta de estudiante.
- [x] 3.3 CI y `azure-pipelines.yml` incluyen el servicio.
- [x] 3.4 README, guía de Azure y diagrama de arquitectura actualizados.

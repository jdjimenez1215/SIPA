# ms-asistente

Microservicio de IA para la matrícula. Recibe una pregunta, lee la sugerencia del estudiante autenticado y devuelve una propuesta de materias para pre-marcar en el front.

**Nunca inscribe**: la inscripción sigue pasando por `ms-inscripcion` y su motor de reglas.

## Endpoint

`POST /api/asistente/chat` (requiere JWT de estudiante)

```json
{ "mensaje": "¿qué materias me conviene inscribir?", "periodo": "2026-2" }
```

```json
{
  "mensaje": "Te propongo 6 materias para 18 créditos...",
  "materiaIds": [27, 28, 29, 30, 31, 32],
  "explicaciones": [
    { "codigo": "603701", "motivo": "LIMITE_CREDITOS_EXCEDIDO", "mensaje": "Ya se alcanzó el límite de créditos del período." }
  ],
  "fallback": false,
  "limiteCreditos": 18
}
```

- `estudianteId` sale **solo** del claim del JWT; un Admin sin claim recibe `403 ASISTENTE_SOLO_ESTUDIANTES`.
- El mismo Bearer se reenvía a `ms-inscripcion`.

## Proveedores

| Variable | Efecto |
|---|---|
| `GROQ_API_KEY` | Usa Groq (OpenAI-compatible). Modelo: `ASISTENTE_MODEL` (def. `openai/gpt-oss-20b`). |
| `ANTHROPIC_API_KEY` | Usa Anthropic. Modelo: `ASISTENTE_MODEL` (def. `claude-3-5-haiku-latest`). |
| ninguna | Modo de reglas determinístico (`fallback=true`). |

## Configuración

```
JWT_KEY=<misma clave que identity/ms-inscripcion>
Jwt__Issuer=MiniIdentityApi
Jwt__Audience=MiniIdentityApiUsers
Inscripcion__BaseUrl=http://api:8080
GROQ_API_KEY=<secreto>
ASISTENTE_MODEL=openai/gpt-oss-20b
```

## Tests

```bash
dotnet test backend/ms-asistente/tests/MsAsistente.Tests
```

Cubren propuesta válida, descarte de IDs inválidos, límite de créditos, explicaciones, exclusión por días (no lunes/miércoles/viernes) y fallback cuando el LLM falla.

# Guía de integración backend ↔ frontend (SIPA)

Contrato vigente entre `index.html` / `sugerencia-matricula.html` y las APIs, a través de `dev-server.py` (`http://localhost:5500`).

## Autenticación (MiniIdentityApi, :5132)

| Método y ruta | Descripción |
|---|---|
| `POST /api/auth/login` | `{ usernameOrEmail, password }` → `{ accessToken, tokenType, username, email, roles, estudianteId }` |
| `POST /api/auth/register` | Solo Admin. `{ username, email, password, estudianteId? }` |
| `GET /api/users/{id}` | Admin o el propio usuario |

Todas las rutas de `ms-inscripcion` requieren `Authorization: Bearer <accessToken>`.
El token incluye el claim `estudianteId` cuando la cuenta está vinculada a un estudiante.

## Inscripción (ms-inscripcion, :8080)

| Método y ruta | Descripción |
|---|---|
| `GET /api/estudiantes/{id}/sugerencia?periodo=2026-2` | Filas de la sugerencia (incluye cupos, promedio, límite) |
| `GET /api/estudiantes/{id}/inscripciones?periodo=2026-2` | Inscripciones activas |
| `POST /api/inscripciones` | `{ estudianteId, periodo, materiaIds[] \| codigosMaterias[] }` |
| `DELETE /api/inscripciones/{id}` | Cancela (dueño o Admin) |
| `POST /api/asistente/chat` | Solo estudiante. `{ mensaje, periodo }` → propuesta de `materiaIds`; nunca inscribe |

### Respuesta de sugerencia

```json
{
  "nombreEstudiante": "Laura Gómez Ríos",
  "programa": "Ingeniería de Sistemas",
  "semestreActual": 6,
  "periodo": "2026-2",
  "totalCreditos": 18,
  "promedio": 3.82,
  "limiteCreditos": 18,
  "materiasSugeridas": [
    {
      "id": 27,
      "codigo": "603601",
      "nombre": "Ingeniería de Software II",
      "creditos": 3,
      "semestre": 6,
      "estado": "Sugerida",
      "prerrequisitoCumplido": true,
      "motivo": null,
      "cuposDisponibles": 30,
      "cuposMaximos": 30,
      "horarios": [
        { "diaSemana": "Lunes", "horaInicio": "14:00", "horaFin": "16:00" }
      ]
    }
  ],
  "notificaciones": []
}
```

### Reglas que aplica el backend

- Orden: materias adeudadas más antiguas → semestre N → N+1.
- Tope de **18 créditos**; **20** si el promedio es **4.0 o más**.
- Hasta **3 materias extra** (adeudadas + N+1).
- Prerrequisitos estrictos, cruce de horarios, cupos y duplicados.
- Toda violación responde `422 application/problem+json` con `code: "INSCRIPCION_RECHAZADA"` y `violations[]`.

## Front

- `assets/js/login.js`: login/logout local, guarda `accessToken` y `estudianteId` en `sessionStorage`.
- `assets/js/app.js`: sugerencia, selección por materia, confirmación y cancelación.
- `useMock: true` usa el fixture `src/data/mocks/sugerencia_mock.json` sin red.

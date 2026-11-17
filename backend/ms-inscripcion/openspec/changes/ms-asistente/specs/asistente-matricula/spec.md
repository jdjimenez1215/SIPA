# Spec: asistente de matrícula

## Escenarios

### Propuesta para el estudiante del JWT
- Dado un token con claim `estudianteId`,
- cuando `POST /api/asistente/chat` recibe un mensaje,
- entonces consulta la sugerencia de ese estudiante y devuelve `materiaIds` solo de filas `Sugerida`, dentro del límite de créditos.

### Solo propone
- La respuesta nunca crea inscripciones.
- El front pre-marca casillas; la inscripción se confirma con `POST /api/inscripciones`.

### Sin estudiante
- Un token sin claim `estudianteId` (Admin) recibe `403` con código `ASISTENTE_SOLO_ESTUDIANTES`.

### Fallback
- Si el proveedor de IA falla, tarda o devuelve JSON inválido, responde `200` con `fallback=true` usando las reglas.
- Si el LLM propone IDs inexistentes o bloqueados, se descartan.

### Explicaciones
- Cada fila bloqueada se devuelve en `explicaciones[]` con `codigo`, `motivo` y un mensaje en español.

### Límite
- La suma de créditos propuestos no supera `limiteCreditos` (18, o 20 con promedio ≥ 4.0).

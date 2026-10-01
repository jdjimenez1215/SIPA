# Delta for API Error Handling

ProblemDetails contract, global middleware and thin controllers are UNCHANGED.

## MODIFIED Requirements

### Requirement: Status mapping
| Situation | Status | Code |
|---|---|---|
| Rule violations | 422 | INSCRIPCION_RECHAZADA + `violations[]` |
| Shared cap exceeded (owed + N+1) | 422 | INSCRIPCION_RECHAZADA, violation LIMITE_SIGUIENTE_SEMESTRE_EXCEDIDO |
| Input validation (incl. materiaIds/codigosMaterias both or neither) | 400 | VALIDACION_FALLIDA + `errors` |
| Duplicate ids in request | 400 | MATERIA_DUPLICADA_EN_SOLICITUD |
| Not found | 404 | ESTUDIANTE_/MATERIA_/INSCRIPCION_/CARRERA_NO_ENCONTRADA |
| Duplicate codigo / in use / already cancelled / concurrency | 409 | CODIGO_DUPLICADO / ENTIDAD_EN_USO / INSCRIPCION_YA_CANCELADA / CONFLICTO_CONCURRENCIA |
| Concurrent duplicate detected under the lock (normal case) | 422 | INSCRIPCION_RECHAZADA (violation MATERIA_YA_INSCRITA) |
| Duplicate reaching the active-enrollment unique index (23505, defensive backstop) | 409 | MATERIA_YA_INSCRITA |
| Invalid horario / cyclic prerequisite | 422 | HORARIO_INVALIDO / PRERREQUISITO_CICLICO |
| Unhandled | 500 | ERROR_INTERNO |
(Previously: no cap violation code)

#### Scenario: Violations payload
- GIVEN a POST with 2 violations
- WHEN rejected
- THEN 422 with `violations` items holding code, materiaId, materiaCodigo, message, details

#### Scenario: Validation error
- GIVEN an empty materiaIds
- WHEN POST
- THEN 400 with `code` VALIDACION_FALLIDA and an `errors` dictionary

#### Scenario: Cap violation
- GIVEN a POST with 4 extra materias
- WHEN rejected
- THEN 422 INSCRIPCION_RECHAZADA with a violation code LIMITE_SIGUIENTE_SEMESTRE_EXCEDIDO in Spanish

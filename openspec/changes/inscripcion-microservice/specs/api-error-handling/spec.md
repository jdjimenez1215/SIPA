# API Error Handling Specification

## Purpose
Uniform RFC 7807 errors with explicit codes and a global exception middleware.

## Requirements

### Requirement: ProblemDetails contract
Every error response MUST be `application/problem+json` with type, title, status, detail, instance, `code`, `traceId`. Titles/details MUST be in Spanish.

#### Scenario: Shape
- GIVEN any failing request
- WHEN the response is produced
- THEN it contains those fields and a stable `code`

### Requirement: Status mapping
| Situation | Status | Code |
|---|---|---|
| Rule violations | 422 | INSCRIPCION_RECHAZADA + `violations[]` |
| Input validation | 400 | VALIDACION_FALLIDA + `errors` |
| Duplicate ids in request | 400 | MATERIA_DUPLICADA_EN_SOLICITUD |
| Not found | 404 | ESTUDIANTE_/MATERIA_/INSCRIPCION_/CARRERA_NO_ENCONTRADA |
| Duplicate codigo / in use / already cancelled / concurrency | 409 | CODIGO_DUPLICADO / ENTIDAD_EN_USO / INSCRIPCION_YA_CANCELADA / CONFLICTO_CONCURRENCIA |
| Concurrent duplicate detected under the lock (normal case) | 422 | INSCRIPCION_RECHAZADA (violation MATERIA_YA_INSCRITA) |
| Duplicate reaching the active-enrollment unique index (23505, defensive backstop) | 409 | MATERIA_YA_INSCRITA |
| Invalid horario / cyclic prerequisite | 422 | HORARIO_INVALIDO / PRERREQUISITO_CICLICO |
| Unhandled | 500 | ERROR_INTERNO |

#### Scenario: Violations payload
- GIVEN a POST with 2 violations
- WHEN rejected
- THEN 422 with `violations` items holding code, materiaId, materiaCodigo, message, details

#### Scenario: Validation error
- GIVEN an empty materiaIds
- WHEN POST
- THEN 400 with `code` VALIDACION_FALLIDA and an `errors` dictionary

### Requirement: Global exception middleware
The middleware MUST map domain exceptions to the table above, log unknown exceptions (Serilog), and MUST NOT leak internals in 500 responses. The persistence layer (Infrastructure) MUST translate PostgreSQL 40001/40P01 into a `ConflictException` that the middleware maps to 409 CONFLICTO_CONCURRENCIA, so the Api stays provider-agnostic.

#### Scenario: Unhandled exception
- GIVEN a handler throws an unexpected exception
- WHEN the request completes
- THEN 500 ERROR_INTERNO with generic detail and traceId, and the error is logged

#### Scenario: Serialization failure
- GIVEN the database raises a serialization failure
- WHEN the request completes
- THEN 409 CONFLICTO_CONCURRENCIA

### Requirement: Thin controllers
Controllers MUST only dispatch through the mediator; they MUST NOT hold business logic.

#### Scenario: Controller review
- GIVEN any controller action
- WHEN inspected
- THEN it only maps the request to a command/query and returns the result

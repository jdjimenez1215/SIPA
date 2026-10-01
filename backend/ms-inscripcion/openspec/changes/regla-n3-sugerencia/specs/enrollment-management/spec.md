# Delta for Enrollment Management

Seat safety, cancel and list requirements are UNCHANGED.

## MODIFIED Requirements

### Requirement: Atomic enrollment (POST /api/inscripciones)
Body `{estudianteId, periodo, materiaIds[] XOR codigosMaterias[]}`. The system MUST run all rules (incl. window, shared cap and strict prerequisites) and persist all enrollments as Activa (201) only if there are zero violations; otherwise it MUST persist none and return 422 with ALL violations. There is no "pass to next": an invalid materia rejects the whole request. Owed subjects are NOT mandatory. The cap counts Activa enrollments of the periodo.
(Previously: body only `materiaIds`; six rules, no cap)

#### Scenario: All valid
- GIVEN 3 valid materias
- WHEN POST
- THEN 201 and 3 Activa enrollments exist

#### Scenario: One fails, none enrolled
- GIVEN 3 materias, one with CRUCE_HORARIO
- WHEN POST
- THEN 422 and zero new enrollments exist

#### Scenario: All violations returned
- GIVEN materia A breaks R1 and R4, materia B breaks R3 and R6
- WHEN POST
- THEN 422 `violations` has 4 entries, each with its materiaId and materiaCodigo

#### Scenario: Owed subject not mandatory
- GIVEN a student in sem 6 owing a sem-4 subject
- WHEN POST with 3 N+1 materias and not the owed one
- THEN 201

#### Scenario: Cap exceeded in one request
- GIVEN 4 extra (N+1/owed) materias in one request
- WHEN POST
- THEN 422 with LIMITE_SIGUIENTE_SEMESTRE_EXCEDIDO and zero enrollments created

#### Scenario: Cap counts active enrollments
- GIVEN 2 Activa extras in the periodo
- WHEN POST 2 more extras
- THEN 422 LIMITE_SIGUIENTE_SEMESTRE_EXCEDIDO

#### Scenario: Strict prerequisite
- GIVEN Laura
- WHEN POST [603601, 603702]
- THEN 422 PREREQUISITO_NO_CUMPLIDO on 603702
- AND POST [603601] alone returns 201

#### Scenario: N+2 materia
- GIVEN Laura (sem 6)
- WHEN POST a sem-8 materia
- THEN 422 SEMESTRE_EXCEDIDO

### Requirement: Request validation and lookups
`periodo` MUST match `^\d{4}-[12]$`. Exactly one of `materiaIds` or `codigosMaterias` MUST be provided and non-empty; both or neither MUST yield 400 VALIDACION_FALLIDA. `codigosMaterias` MUST behave identically to `materiaIds`. Duplicates MUST yield 400 MATERIA_DUPLICADA_EN_SOLICITUD. Unknown student or materia (id or codigo) MUST yield 404.
(Previously: only `materiaIds`, non-empty)

#### Scenario: Bad periodo
- GIVEN periodo "2026-3"
- WHEN POST
- THEN 400 VALIDACION_FALLIDA

#### Scenario: Duplicate ids
- GIVEN materiaIds [M1, M1]
- WHEN POST
- THEN 400 MATERIA_DUPLICADA_EN_SOLICITUD

#### Scenario: Unknown student
- GIVEN estudianteId not in DB
- WHEN POST
- THEN 404 ESTUDIANTE_NO_ENCONTRADO

#### Scenario: codigosMaterias alternative
- GIVEN Laura and codigosMaterias ["603601"]
- WHEN POST
- THEN 201, same result as the equivalent materiaIds

#### Scenario: Both or neither
- GIVEN a body with both materiaIds and codigosMaterias, or with neither
- WHEN POST
- THEN 400 VALIDACION_FALLIDA

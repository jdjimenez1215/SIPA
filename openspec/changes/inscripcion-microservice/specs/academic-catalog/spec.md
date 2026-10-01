# Academic Catalog Specification

## Purpose
CRUD for carreras, materias, horarios and prerrequisitos, plus catalog query.

## Requirements

### Requirement: Carrera and Materia CRUD
The system MUST support create/read/update/delete under `/api/carreras` and `/api/materias`. Codigo MUST be unique (409 CODIGO_DUPLICADO). Deleting an entity referenced by others MUST return 409 ENTIDAD_EN_USO. Unknown ids MUST return 404 (CARRERA_NO_ENCONTRADA / MATERIA_NO_ENCONTRADA).

#### Scenario: Create materia
- GIVEN a valid body with an existing CarreraId
- WHEN POST /api/materias
- THEN 201 with the created materia

#### Scenario: Duplicate codigo
- GIVEN a materia with codigo MAT101 exists
- WHEN another is created with MAT101
- THEN 409 CODIGO_DUPLICADO

#### Scenario: Delete in use
- GIVEN a carrera with materias
- WHEN DELETE it
- THEN 409 ENTIDAD_EN_USO

### Requirement: Horario management
A materia MAY have several horario blocks. HoraInicio MUST be < HoraFin (HH:mm, no midnight crossing), and blocks of one materia MUST NOT overlap (422 HORARIO_INVALIDO).

#### Scenario: Valid block
- GIVEN materia has Lunes 08:00-10:00
- WHEN adding Lunes 10:00-12:00
- THEN 201

#### Scenario: Inverted range
- GIVEN HoraInicio 12:00, HoraFin 10:00
- WHEN adding
- THEN 422 HORARIO_INVALIDO

#### Scenario: Overlapping own block
- GIVEN materia has Lunes 08:00-10:00
- WHEN adding Lunes 09:00-11:00
- THEN 422 HORARIO_INVALIDO

### Requirement: Prerrequisito management
The system MUST reject self-reference and cycles with 422 PRERREQUISITO_CICLICO.

#### Scenario: Valid link
- GIVEN B has no path to A
- WHEN adding A requires B
- THEN 201

#### Scenario: Self-reference
- WHEN adding A requires A
- THEN 422 PRERREQUISITO_CICLICO

#### Scenario: Indirect cycle
- GIVEN A requires B and B requires C
- WHEN adding C requires A
- THEN 422 PRERREQUISITO_CICLICO

### Requirement: Catalog query (GET /api/materias?carreraId=&semestre=)
The system MUST return materias with horarios and prerrequisitos, filtered by the optional parameters.

#### Scenario: Filtered
- GIVEN materias across carreras and semestres
- WHEN GET ?carreraId=X&semestre=3
- THEN only matching materias are returned, each with horarios and prerrequisitos

#### Scenario: No filters
- WHEN GET /api/materias
- THEN all materias are returned

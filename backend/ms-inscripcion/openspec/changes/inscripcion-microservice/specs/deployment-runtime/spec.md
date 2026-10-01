# Deployment Runtime Specification

## Purpose
Run the service and PostgreSQL with one command, migrated and seeded.

## Requirements

### Requirement: Docker composition
A Dockerfile (sdk:8.0 build, aspnet:8.0 runtime) and docker-compose (api + PostgreSQL 16) MUST be provided; the API MUST start after the database is healthy.

#### Scenario: Compose up
- GIVEN Docker is available
- WHEN `docker compose up`
- THEN the API answers and Swagger UI is reachable

### Requirement: Migrate and seed on startup
The API MUST apply migrations at startup and seed idempotently: the main carrera ING-SIS plus a second carrera ADM used only to demonstrate CARRERA_NO_CORRESPONDE (a justified deviation from a single carrera), materias across several semestres, prerrequisitos, and horarios including at least one clashing pair, plus students with history.

#### Scenario: First start
- GIVEN an empty database
- WHEN the API starts
- THEN the schema exists and seed data is present

#### Scenario: Restart
- GIVEN an already seeded database
- WHEN the API restarts
- THEN no duplicate seed rows are created

### Requirement: Configuration
Settings MUST include the connection string, `Enrollment:MaxSemestersAhead` (default 3) and `Enrollment:CurrentPeriod`, overridable by environment variables.

#### Scenario: Override X
- GIVEN env `Enrollment__MaxSemestersAhead=1`
- WHEN a materia beyond SemestreActual+1 is requested
- THEN SEMESTRE_EXCEDIDO is reported

#### Scenario: Default X
- GIVEN no override
- WHEN the API reads options
- THEN X is 3

### Requirement: Documentation
README MUST include run instructions, sample requests, Mermaid entity and structure diagrams, and the assumptions list.

#### Scenario: README content
- GIVEN the repository
- WHEN README is read
- THEN those four items are present

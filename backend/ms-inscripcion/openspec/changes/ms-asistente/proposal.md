# Propuesta: ms-asistente (IA de matrícula)

## Problema

El estudiante ve la sugerencia y las materias bloqueadas, pero debe interpretar solo los motivos y decidir qué marcar. No hay explicación conversacional ni una propuesta inicial adaptada a su caso.

## Objetivo

Agregar un microservicio `ms-asistente` que:

1. Reciba una pregunta del estudiante.
2. Lea **solo** la sugerencia del `estudianteId` del JWT (llamando a ms-inscripción con el mismo token).
3. Use un LLM (Groq/OpenAI-compatible o Anthropic) para proponer materias y explicar bloqueos.
4. Devuelva `materiaIds` para **pre-marcar casillas** en el front.
5. Nunca inscriba: la inscripción sigue pasando por `POST /api/inscripciones` y el motor de reglas.
6. Use un fallback determinístico (reglas) si el LLM no está disponible.

## Seguridad

- El `estudianteId` sale exclusivamente del claim del JWT; no se acepta en el body.
- La cuenta Admin no tiene `estudianteId`: el asistente responde `403 ASISTENTE_SOLO_ESTUDIANTES`.
- El asistente no tiene credenciales de servicio contra ms-inscripción: reenvía el Bearer del usuario.
- La API key del proveedor es un secreto de entorno (`GROQ_API_KEY` / `ANTHROPIC_API_KEY`), nunca del repo.

## Proveedores

- **Groq** (OpenAI-compatible), modelo por defecto `openai/gpt-oss-20b` (configurable).
- **Anthropic** opcional.
- Sin clave: modo de reglas.

## No objetivos

- Reemplazar el motor de reglas.
- Inscribir o cancelar por el estudiante.
- Acceder a datos de otros estudiantes.

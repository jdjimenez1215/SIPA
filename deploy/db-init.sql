-- Least-privilege application role. Runs only on a fresh database volume.
\set ON_ERROR_STOP on

DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'sipa_app') THEN
        CREATE ROLE sipa_app LOGIN PASSWORD 'sipa_app';
    END IF;
END
$$;

-- Identity lives in its own database so EF Core (EnsureCreated) owns the schema.
SELECT 'CREATE DATABASE identidad OWNER sipa_app'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'identidad')\gexec

GRANT CONNECT ON DATABASE inscripcion TO sipa_app;
GRANT CONNECT ON DATABASE identidad TO sipa_app;

\connect inscripcion
ALTER SCHEMA public OWNER TO sipa_app;
GRANT USAGE, CREATE ON SCHEMA public TO sipa_app;

\connect identidad
ALTER SCHEMA public OWNER TO sipa_app;
GRANT USAGE, CREATE ON SCHEMA public TO sipa_app;

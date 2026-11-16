#!/bin/sh
# Crea el rol de aplicación y la base de identidad. Solo corre en un volumen nuevo.
set -e

APP_PASSWORD="${DB_APP_PASSWORD:-sipa_app}"

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<EOSQL
DO \$\$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'sipa_app') THEN
        CREATE ROLE sipa_app LOGIN PASSWORD '${APP_PASSWORD}';
    ELSE
        ALTER ROLE sipa_app WITH PASSWORD '${APP_PASSWORD}';
    END IF;
END
\$\$;

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
EOSQL

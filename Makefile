# ==============================================================================
# Makefile - Gestión de Servicios Backend SIPA
# ==============================================================================
# Backends soportados:
#   1. MiniIdentity API (.NET 10)  -> Puerto 5132 (http://localhost:5132)
#   2. MsInscripcion (.NET 8 + PG) -> Puerto 8080 (http://localhost:8080)
#                                  -> PostgreSQL 5432 (localhost:5432)
#   3. Frontend Dev Server (Python) -> Puerto 5500 (http://localhost:5500) [Opcional]
# ==============================================================================

SHELL := /bin/bash

# Configuración de rutas y puertos
ROOT_DIR           := $(shell pwd)
RUN_DIR            := $(ROOT_DIR)/.run

IDENTITY_DIR       := $(ROOT_DIR)/backend/mini-identity-api-dotnet
IDENTITY_PROJECT   := $(IDENTITY_DIR)/src/MiniIdentityApi.Api
IDENTITY_PORT      := 5132
IDENTITY_PID       := $(RUN_DIR)/identity.pid
IDENTITY_LOG       := $(RUN_DIR)/identity.log

INSCRIPCION_DIR    := $(ROOT_DIR)/backend/ms-inscripcion
INSCRIPCION_COMPOSE:= $(INSCRIPCION_DIR)/docker-compose.yml
INSCRIPCION_PORT   := 8080
DB_PORT            := 5432

FRONTEND_SERVER    := $(ROOT_DIR)/dev-server.py
FRONTEND_PORT      := 5500
FRONTEND_PID       := $(RUN_DIR)/frontend.pid
FRONTEND_LOG       := $(RUN_DIR)/frontend.log

# Colores de terminal ANSI
GREEN  := \033[0;32m
YELLOW := \033[0;33m
RED    := \033[0;31m
BLUE   := \033[0;34m
CYAN   := \033[0;36m
BOLD   := \033[1m
RESET  := \033[0m

.PHONY: all help \
	up start down stop restart status ps \
	up-identity down-identity restart-identity logs-identity \
	up-inscripcion down-inscripcion restart-inscripcion logs-inscripcion down-v up-db \
	up-frontend down-frontend restart-frontend logs-frontend \
	up-all down-all restart-all \
	test test-identity test-inscripcion test-e2e clean

# Objetivo por defecto
all: help

## ----------------------------------------------------------------------
## AYUDA
## ----------------------------------------------------------------------
help:
	@echo -e "$(BOLD)$(CYAN)======================================================================$(RESET)"
	@echo -e "$(BOLD)$(CYAN)          SIPA - Comandos de Gestión de Servicios Backend             $(RESET)"
	@echo -e "$(BOLD)$(CYAN)======================================================================$(RESET)"
	@echo -e ""
	@echo -e "$(BOLD)COMANDOS GENERALES (BACKENDS):$(RESET)"
	@echo -e "  $(GREEN)make up$(RESET) / $(GREEN)make start$(RESET)          Levanta todos los backends (Identity + Inscripción)"
	@echo -e "  $(YELLOW)make down$(RESET) / $(YELLOW)make stop$(RESET)        Detiene todos los backends"
	@echo -e "  $(BLUE)make restart$(RESET)                 Reinicia todos los backends"
	@echo -e "  $(CYAN)make status$(RESET) / $(CYAN)make ps$(RESET)         Muestra el estado y puertos de los servicios"
	@echo -e ""
	@echo -e "$(BOLD)BACKEND DE IDENTIDAD (MiniIdentityApi - Puerto $(IDENTITY_PORT)):$(RESET)"
	@echo -e "  $(GREEN)make up-identity$(RESET)             Levanta MiniIdentity API en segundo plano"
	@echo -e "  $(YELLOW)make down-identity$(RESET)           Detiene MiniIdentity API y libera el puerto $(IDENTITY_PORT)"
	@echo -e "  $(BLUE)make restart-identity$(RESET)        Reinicia MiniIdentity API"
	@echo -e "  $(CYAN)make logs-identity$(RESET)           Muestra los logs en tiempo real de Identity"
	@echo -e ""
	@echo -e "$(BOLD)BACKEND DE INSCRIPCIÓN (MsInscripcion - Puertos $(INSCRIPCION_PORT) y $(DB_PORT)):$(RESET)"
	@echo -e "  $(GREEN)make up-inscripcion$(RESET)          Levanta MsInscripcion y PostgreSQL con Docker Compose"
	@echo -e "  $(YELLOW)make down-inscripcion$(RESET)        Detiene MsInscripcion y PostgreSQL"
	@echo -e "  $(BLUE)make restart-inscripcion$(RESET)     Reinicia MsInscripcion"
	@echo -e "  $(CYAN)make logs-inscripcion$(RESET)        Muestra los logs de Docker Compose"
	@echo -e "  $(YELLOW)make up-db$(RESET)                   Levanta únicamente PostgreSQL en Docker"
	@echo -e "  $(RED)make down-v$(RESET)                  Detiene MsInscripcion y elimina volúmenes (reset BD)"
	@echo -e ""
	@echo -e "$(BOLD)FRONTEND DEV SERVER (dev-server.py - Puerto $(FRONTEND_PORT)) [OPCIONAL]:$(RESET)"
	@echo -e "  $(GREEN)make up-frontend$(RESET)             Levanta el servidor dev front en segundo plano"
	@echo -e "  $(YELLOW)make down-frontend$(RESET)           Detiene el servidor dev front"
	@echo -e "  $(CYAN)make logs-frontend$(RESET)           Muestra los logs del front dev server"
	@echo -e "  $(GREEN)make up-all$(RESET)                  Levanta Backends + Frontend Dev Server"
	@echo -e "  $(YELLOW)make down-all$(RESET)                Detiene Backends + Frontend Dev Server"
	@echo -e ""
	@echo -e "$(BOLD)TESTS Y LIMPIEZA:$(RESET)"
	@echo -e "  $(CYAN)make test$(RESET)                    Ejecuta las pruebas unitarias de ambos backends"
	@echo -e "  $(CYAN)make test-identity$(RESET)           Ejecuta pruebas unitarias de MiniIdentity"
	@echo -e "  $(CYAN)make test-inscripcion$(RESET)        Ejecuta pruebas unitarias de MsInscripcion"
	@echo -e "  $(CYAN)make test-e2e$(RESET)                Ejecuta la suite E2E de MsInscripcion (requiere servicio activo)"
	@echo -e "  $(RED)make clean$(RESET)                   Limpia archivos PID y temporales (.run/)"
	@echo -e ""

# Crear carpeta de runtime
$(RUN_DIR):
	@mkdir -p $(RUN_DIR)

# ==============================================================================
# LEVANTAR / DETENER GLOBAL DE BACKENDS
# ==============================================================================
up: up-inscripcion up-identity
	@echo -e ""
	@echo -e "$(BOLD)$(GREEN)✔ Todos los backends han sido iniciados correctamente.$(RESET)"
	@$(MAKE) --no-print-directory status

start: up

down: down-identity down-inscripcion
	@echo -e ""
	@echo -e "$(BOLD)$(YELLOW)✔ Todos los backends han sido detenidos correctamente.$(RESET)"

stop: down

restart: down up

# ==============================================================================
# MINIIDENTITY API (.NET 10 - InMemory - Puerto 5132)
# ==============================================================================
up-identity: $(RUN_DIR)
	@echo -e "$(BOLD)[MiniIdentity]$(RESET) Verificando servicio..."
	@if (echo > /dev/tcp/127.0.0.1/$(IDENTITY_PORT)) >/dev/null 2>&1; then \
		echo -e "$(YELLOW)[MiniIdentity] Ya está en ejecución en http://localhost:$(IDENTITY_PORT)$(RESET)"; \
	else \
		echo -e "$(GREEN)[MiniIdentity] Iniciando en http://localhost:$(IDENTITY_PORT)...$(RESET)"; \
		ASPNETCORE_ENVIRONMENT=Development setsid dotnet run --project $(IDENTITY_PROJECT) --launch-profile http </dev/null > $(IDENTITY_LOG) 2>&1 & \
		echo -n "[MiniIdentity] Esperando a que el puerto $(IDENTITY_PORT) esté activo"; \
		READY=0; \
		for i in {1..25}; do \
			if (echo > /dev/tcp/127.0.0.1/$(IDENTITY_PORT)) >/dev/null 2>&1; then \
				READY=1; \
				PID=$$(lsof -ti:$(IDENTITY_PORT) 2>/dev/null | head -n1); \
				if [ -n "$$PID" ]; then echo "$$PID" > $(IDENTITY_PID); fi; \
				break; \
			fi; \
			echo -n "."; \
			sleep 1; \
		done; \
		if [ $$READY -eq 1 ]; then \
			echo -e "\n$(GREEN)[MiniIdentity] ✔ Activo en http://localhost:$(IDENTITY_PORT)$(RESET)"; \
			echo -e "              Swagger: http://localhost:$(IDENTITY_PORT)/swagger"; \
		else \
			echo -e "\n$(YELLOW)[MiniIdentity] ⚠ Tardó en responder. Revisa: make logs-identity$(RESET)"; \
		fi; \
	fi

down-identity:
	@echo -e "$(BOLD)[MiniIdentity]$(RESET) Deteniendo servicio..."
	@PIDS=$$(lsof -ti:$(IDENTITY_PORT) 2>/dev/null); \
	if [ -n "$$PIDS" ]; then \
		kill $$PIDS 2>/dev/null || true; \
		sleep 1; \
		fuser -k $(IDENTITY_PORT)/tcp 2>/dev/null || true; \
	fi
	@if [ -f $(IDENTITY_PID) ]; then \
		PID=$$(cat $(IDENTITY_PID) 2>/dev/null); \
		if [ -n "$$PID" ]; then \
			kill $$PID 2>/dev/null || true; \
		fi; \
		rm -f $(IDENTITY_PID); \
	fi
	@echo -e "$(GREEN)[MiniIdentity] ✔ Detenido y puerto $(IDENTITY_PORT) liberado.$(RESET)"

restart-identity: down-identity up-identity

logs-identity:
	@if [ -f $(IDENTITY_LOG) ]; then \
		tail -f $(IDENTITY_LOG); \
	else \
		echo -e "$(YELLOW)No existe archivo de log $(IDENTITY_LOG). El servicio no ha sido iniciado.$(RESET)"; \
	fi

# ==============================================================================
# MS-INSCRIPCION (.NET 8 + PostgreSQL - Docker Compose - Puertos 8080 y 5432)
# ==============================================================================
up-inscripcion:
	@echo -e "$(BOLD)[MsInscripcion]$(RESET) Verificando servicio..."
	@if (echo > /dev/tcp/127.0.0.1/$(INSCRIPCION_PORT)) >/dev/null 2>&1; then \
		echo -e "$(YELLOW)[MsInscripcion] Ya está en ejecución en http://localhost:$(INSCRIPCION_PORT)$(RESET)"; \
	else \
		echo -e "$(GREEN)[MsInscripcion] Iniciando con Docker Compose...$(RESET)"; \
		docker compose -f $(INSCRIPCION_COMPOSE) up -d --build; \
		echo -n "[MsInscripcion] Esperando a que el servicio esté listo"; \
		READY=0; \
		for i in {1..40}; do \
			if curl -s -o /dev/null http://localhost:$(INSCRIPCION_PORT)/swagger/index.html; then \
				READY=1; \
				break; \
			fi; \
			echo -n "."; \
			sleep 1; \
		done; \
		if [ $$READY -eq 1 ]; then \
			echo -e "\n$(GREEN)[MsInscripcion] ✔ Activo en http://localhost:$(INSCRIPCION_PORT)$(RESET)"; \
			echo -e "               Swagger: http://localhost:$(INSCRIPCION_PORT)/swagger"; \
			echo -e "               PostgreSQL: localhost:$(DB_PORT)"; \
		else \
			echo -e "\n$(YELLOW)[MsInscripcion] ⚠ Tardó en responder. Revisa: make logs-inscripcion$(RESET)"; \
		fi; \
	fi

up-db:
	@echo -e "$(BOLD)[MsInscripcion]$(RESET) Iniciando solo PostgreSQL..."
	@docker compose -f $(INSCRIPCION_COMPOSE) up -d db
	@echo -e "$(GREEN)[MsInscripcion] ✔ Base de datos PostgreSQL iniciada en puerto $(DB_PORT).$(RESET)"

down-inscripcion:
	@echo -e "$(BOLD)[MsInscripcion]$(RESET) Deteniendo contenedores..."
	@docker compose -f $(INSCRIPCION_COMPOSE) down
	@echo -e "$(GREEN)[MsInscripcion] ✔ Detenido correctamente.$(RESET)"

down-v:
	@echo -e "$(BOLD)[MsInscripcion]$(RESET) Deteniendo y borrando volúmenes (reset BD)..."
	@docker compose -f $(INSCRIPCION_COMPOSE) down -v
	@echo -e "$(GREEN)[MsInscripcion] ✔ Detenido y base de datos reseteada.$(RESET)"

restart-inscripcion: down-inscripcion up-inscripcion

logs-inscripcion:
	@docker compose -f $(INSCRIPCION_COMPOSE) logs -f

# ==============================================================================
# ESTADO DE SERVICIOS
# ==============================================================================
status:
	@echo -e ""
	@echo -e "$(BOLD)$(CYAN)===================== ESTADO DE LOS SERVICIOS =====================$(RESET)"
	@echo -e ""
	@printf "%-20s %-12s %-15s %-30s\n" "SERVICIO" "PUERTO" "ESTADO" "URL / DETALLE"
	@echo -e "-------------------------------------------------------------------"
	@# MiniIdentity
	@if (echo > /dev/tcp/127.0.0.1/$(IDENTITY_PORT)) >/dev/null 2>&1; then \
		PID=$$(lsof -ti:$(IDENTITY_PORT) 2>/dev/null | head -n1); \
		STATUS_TXT="RUNNING"; \
		if [ -n "$$PID" ]; then STATUS_TXT="RUNNING ($$PID)"; fi; \
		printf "%-20s %-12s $(GREEN)%-15s$(RESET) %-30s\n" "MiniIdentity API" "$(IDENTITY_PORT)/http" "$$STATUS_TXT" "http://localhost:$(IDENTITY_PORT)/swagger"; \
	else \
		printf "%-20s %-12s $(RED)%-15s$(RESET) %-30s\n" "MiniIdentity API" "$(IDENTITY_PORT)/http" "STOPPED" "-"; \
	fi
	@# MsInscripcion API
	@if (echo > /dev/tcp/127.0.0.1/$(INSCRIPCION_PORT)) >/dev/null 2>&1; then \
		printf "%-20s %-12s $(GREEN)%-15s$(RESET) %-30s\n" "MsInscripcion API" "$(INSCRIPCION_PORT)/http" "RUNNING" "http://localhost:$(INSCRIPCION_PORT)/swagger"; \
	else \
		printf "%-20s %-12s $(RED)%-15s$(RESET) %-30s\n" "MsInscripcion API" "$(INSCRIPCION_PORT)/http" "STOPPED" "-"; \
	fi
	@# PostgreSQL
	@if (echo > /dev/tcp/127.0.0.1/$(DB_PORT)) >/dev/null 2>&1; then \
		printf "%-20s %-12s $(GREEN)%-15s$(RESET) %-30s\n" "PostgreSQL" "$(DB_PORT)/tcp" "RUNNING" "inscripcion (user: postgres)"; \
	else \
		printf "%-20s %-12s $(RED)%-15s$(RESET) %-30s\n" "PostgreSQL" "$(DB_PORT)/tcp" "STOPPED" "-"; \
	fi
	@# Frontend Dev Server
	@if (echo > /dev/tcp/127.0.0.1/$(FRONTEND_PORT)) >/dev/null 2>&1; then \
		PID=$$(lsof -ti:$(FRONTEND_PORT) 2>/dev/null | head -n1); \
		STATUS_TXT="RUNNING"; \
		if [ -n "$$PID" ]; then STATUS_TXT="RUNNING ($$PID)"; fi; \
		printf "%-20s %-12s $(GREEN)%-15s$(RESET) %-30s\n" "Frontend Dev" "$(FRONTEND_PORT)/http" "$$STATUS_TXT" "http://localhost:$(FRONTEND_PORT)"; \
	else \
		printf "%-20s %-12s $(YELLOW)%-15s$(RESET) %-30s\n" "Frontend Dev" "$(FRONTEND_PORT)/http" "STOPPED" "Opcional (make up-frontend)"; \
	fi
	@echo -e "-------------------------------------------------------------------"
	@echo -e ""

ps: status

# ==============================================================================
# FRONTEND DEV SERVER (dev-server.py - Opcional)
# ==============================================================================
up-frontend: $(RUN_DIR)
	@echo -e "$(BOLD)[Frontend]$(RESET) Verificando servidor de desarrollo..."
	@if (echo > /dev/tcp/127.0.0.1/$(FRONTEND_PORT)) >/dev/null 2>&1; then \
		echo -e "$(YELLOW)[Frontend] Ya está en ejecución en http://localhost:$(FRONTEND_PORT)$(RESET)"; \
	else \
		echo -e "$(GREEN)[Frontend] Iniciando en http://localhost:$(FRONTEND_PORT)...$(RESET)"; \
		setsid python3 $(FRONTEND_SERVER) </dev/null > $(FRONTEND_LOG) 2>&1 & \
		echo -n "[Frontend] Esperando a que el puerto $(FRONTEND_PORT) esté activo"; \
		READY=0; \
		for i in {1..10}; do \
			if (echo > /dev/tcp/127.0.0.1/$(FRONTEND_PORT)) >/dev/null 2>&1; then \
				READY=1; \
				PID=$$(lsof -ti:$(FRONTEND_PORT) 2>/dev/null | head -n1); \
				if [ -n "$$PID" ]; then echo "$$PID" > $(FRONTEND_PID); fi; \
				break; \
			fi; \
			echo -n "."; \
			sleep 1; \
		done; \
		if [ $$READY -eq 1 ]; then \
			echo -e "\n$(GREEN)[Frontend] ✔ Activo en http://localhost:$(FRONTEND_PORT)$(RESET)"; \
		else \
			echo -e "\n$(YELLOW)[Frontend] ⚠ No inició correctamente. Revisa: make logs-frontend$(RESET)"; \
		fi; \
	fi

down-frontend:
	@echo -e "$(BOLD)[Frontend]$(RESET) Deteniendo servidor dev..."
	@PIDS=$$(lsof -ti:$(FRONTEND_PORT) 2>/dev/null); \
	if [ -n "$$PIDS" ]; then \
		kill $$PIDS 2>/dev/null || true; \
		sleep 1; \
		fuser -k $(FRONTEND_PORT)/tcp 2>/dev/null || true; \
	fi
	@rm -f $(FRONTEND_PID)
	@echo -e "$(GREEN)[Frontend] ✔ Servidor detenido y puerto $(FRONTEND_PORT) liberado.$(RESET)"

restart-frontend: down-frontend up-frontend

logs-frontend:
	@if [ -f $(FRONTEND_LOG) ]; then \
		tail -f $(FRONTEND_LOG); \
	else \
		echo -e "$(YELLOW)No existe log $(FRONTEND_LOG)$(RESET)"; \
	fi

up-all: up up-frontend
down-all: down down-frontend
restart-all: restart restart-frontend

# ==============================================================================
# TESTS & LIMPIEZA
# ==============================================================================
test: test-identity test-inscripcion
	@echo -e ""
	@echo -e "$(BOLD)$(GREEN)✔ Todas las pruebas unitarias pasaron exitosamente.$(RESET)"

test-identity:
	@echo -e "$(BOLD)[MiniIdentity]$(RESET) Ejecutando pruebas unitarias..."
	@dotnet test $(IDENTITY_DIR)/src/MiniIdentityApi.Tests

test-inscripcion:
	@echo -e "$(BOLD)[MsInscripcion]$(RESET) Ejecutando pruebas unitarias..."
	@dotnet test $(INSCRIPCION_DIR)/tests/MsInscripcion.UnitTests

test-e2e:
	@echo -e "$(BOLD)[MsInscripcion]$(RESET) Ejecutando pruebas E2E..."
	@if ! curl -s -o /dev/null http://localhost:$(INSCRIPCION_PORT)/swagger/index.html; then \
		echo -e "$(RED)MsInscripcion no está corriendo. Ejecuta 'make up-inscripcion' primero.$(RESET)"; \
		exit 1; \
	fi
	@cd $(INSCRIPCION_DIR) && PORT=$(INSCRIPCION_PORT) bash scripts/e2e.sh

clean:
	@echo -e "Limpiando archivos temporales..."
	@rm -rf $(RUN_DIR)
	@echo -e "$(GREEN)✔ Limpieza completada.$(RESET)"

.DEFAULT_GOAL := help
COMPOSE := docker compose
SLN := src/api/com.jobsite.chat.slnx

.PHONY: help install up down clean logs ps test test-api test-web

help: ## Show available targets
	@grep -E '^[a-zA-Z_-]+:.*?## ' $(MAKEFILE_LIST) | awk 'BEGIN{FS=":.*?## "}{printf "  \033[36m%-10s\033[0m %s\n", $$1, $$2}'

install: up ## Build and start the whole stack (alias for `up`)

up: ## Build + start rabbitmq, api, bot, web (detached)
	$(COMPOSE) up --build -d
	@echo ""
	@echo "  App:         http://localhost:3000"
	@echo "  API:         http://localhost:5080  (health: /health, readiness: /health/ready)"
	@echo "  RabbitMQ UI: http://localhost:15672  (guest/guest)"

down: ## Stop and remove containers (keeps the database volume)
	$(COMPOSE) down

clean: ## Stop and remove containers AND the SQLite volume
	$(COMPOSE) down -v

logs: ## Tail logs from all services
	$(COMPOSE) logs -f

ps: ## Show service status
	$(COMPOSE) ps

test: test-api test-web ## Run all tests (backend + frontend)

test-api: ## Run backend tests
	dotnet test $(SLN)

test-web: ## Run frontend tests
	cd src/app && npm ci && npx vitest run

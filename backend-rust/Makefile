.DEFAULT_GOAL := all

# Aggregate target for the full workflow
all: start-docker wait-for-database apply-migrations

# Setup application with a generated or user-provided password
setup:
	@echo "Generating a random database password and setting up the environment..."
	@DB_PASSWORD=$$(openssl rand -base64 12) && \
	export DB_PASSWORD=$$DB_PASSWORD && \
	cat .env.template | envsubst > .env

setup-env-with-password:
	@echo "Please enter the database password manually"
	read DB_PASSWORD && \
	export DB_PASSWORD=$$DB_PASSWORD; \
	cat .env.template | envsubst > .env

# Start Docker containers
start-docker:
	@echo "Starting Docker containers for database..."
	docker compose up -d db

# Wait for the PostgreSQL database to be ready
wait-for-database:
	@echo "Waiting for the database to become ready..."
	@max_attempts=10; \
	current_attempt=1; \
	until docker compose exec -T db pg_isready -U postgres; do \
		sleep 5; \
		current_attempt=$$((current_attempt+1)); \
		if [ $$current_attempt -gt $$max_attempts ]; then \
			echo "Database did not become ready in time."; \
			exit 1; \
		fi; \
		echo "Retrying ($$current_attempt/$$max_attempts)..."; \
	done

# Apply migrations to create tables
apply-migrations:
	@echo "Running SQL migrations to create tables..."
	sqlx migrate run

.PHONY: setup all start-docker wait-for-database apply-migrations

.PHONY: help restore build build-all build-app build-web build-manga build-tester test run-app run-web docker-up

CONFIGURATION ?= Debug
DOTNET ?= dotnet
DOCKER_COMPOSE ?= docker-compose

APP_PROJECT := MatrixEase.App/MatrixEase.App.csproj
WEB_PROJECT := MatrixEase.Web/MatrixEase.Web.csproj
MANGA_PROJECT := MatrixEase.Manga/MatrixEase.Manga.csproj
TESTER_PROJECT := MatrixEase.Tester/MatrixEase.Tester.csproj
TEST_PROJECT := MatrixEase.Tester.Tests/MatrixEase.Tester.Tests.csproj
DOCKER_COMPOSE_FILE := docker/matrixease_dotnet/docker-compose.yml

help:
	@printf "MatrixEase Make targets:\n"
	@printf "  make restore                 Restore all .NET projects\n"
	@printf "  make build                   Build all .NET projects\n"
	@printf "  make build-app               Build MatrixEase.App\n"
	@printf "  make build-web               Build MatrixEase.Web\n"
	@printf "  make build-manga             Build MatrixEase.Manga\n"
	@printf "  make build-tester            Build MatrixEase.Tester\n"
	@printf "  make test                    Run MatrixEase.Tester.Tests\n"
	@printf "  make run-app                 Run MatrixEase.App\n"
	@printf "  make run-web                 Run MatrixEase.Web\n"
	@printf "  make docker-up               Start the docker deployment stack\n"
	@printf "\n"
	@printf "Override build configuration with CONFIGURATION=Release\n"

restore:
	$(DOTNET) restore $(APP_PROJECT)
	$(DOTNET) restore $(WEB_PROJECT)
	$(DOTNET) restore $(MANGA_PROJECT)
	$(DOTNET) restore $(TESTER_PROJECT)
	$(DOTNET) restore $(TEST_PROJECT)

build: build-all

build-all: build-app build-web build-manga build-tester

build-app:
	$(DOTNET) build $(APP_PROJECT) --configuration $(CONFIGURATION)

build-web:
	$(DOTNET) build $(WEB_PROJECT) --configuration $(CONFIGURATION)

build-manga:
	$(DOTNET) build $(MANGA_PROJECT) --configuration $(CONFIGURATION)

build-tester:
	$(DOTNET) build $(TESTER_PROJECT) --configuration $(CONFIGURATION)

test:
	$(DOTNET) test $(TEST_PROJECT) --configuration $(CONFIGURATION)

run-app:
	$(DOTNET) run --project $(APP_PROJECT)

run-web:
	$(DOTNET) run --project $(WEB_PROJECT)

docker-up:
	$(DOCKER_COMPOSE) -f $(DOCKER_COMPOSE_FILE) up -d

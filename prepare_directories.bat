@echo off
echo Creating directory structure for TapSystem...

:: Root project structure
mkdir src
mkdir tests
mkdir deploy
mkdir scripts
mkdir docs

:: API project structure
mkdir src\TapSystem.Api
mkdir src\TapSystem.Api\Controllers
mkdir src\TapSystem.Api\Services
mkdir src\TapSystem.Api\Models
mkdir src\TapSystem.Api\Infrastructure
mkdir src\TapSystem.Api\Configuration

:: Worker project structure
mkdir src\TapSystem.Worker
mkdir src\TapSystem.Worker\Services
mkdir src\TapSystem.Worker\Models
mkdir src\TapSystem.Worker\Infrastructure

:: Shared project structure
mkdir src\TapSystem.Shared
mkdir src\TapSystem.Shared\Models
mkdir src\TapSystem.Shared\Infrastructure

:: Test project structure
mkdir tests\TapSystem.Api.Tests
mkdir tests\TapSystem.Worker.Tests
mkdir tests\TapSystem.Integration.Tests
mkdir tests\TapSystem.Performance.Tests

:: Deployment and infrastructure
mkdir deploy\docker
mkdir deploy\init-scripts
mkdir deploy\config

:: Documentation
mkdir docs\api
mkdir docs\architecture
mkdir docs\performance

echo Directory structure created successfully!
param(
    [string]$Environment = "Performance",
    [string]$OraclePassword = "YourStrongPassword123!",
    [string]$ApiPort = "5000"
)

$ErrorActionPreference = "Stop"
$ScriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path

# Check prerequisites
Write-Host "Checking prerequisites..."
$prerequisites = @("docker", "docker-compose", "dotnet")
foreach ($prereq in $prerequisites) {
    if (-not (Get-Command $prereq -ErrorAction SilentlyContinue)) {
        throw "$prereq is not installed or not in PATH"
    }
}

# Create necessary directories
Write-Host "Creating directories..."
$directories = @(
    "output",
    "logs"
)

foreach ($dir in $directories) {
    $path = Join-Path $ScriptPath $dir
    if (-not (Test-Path $path)) {
        New-Item -Path $path -ItemType Directory -Force | Out-Null
    }
}

# Set up environment variables
$env:ASPNETCORE_ENVIRONMENT = $Environment
$env:ORACLE_PASSWORD = $OraclePassword
$env:API_PORT = $ApiPort

# Build and start containers
Write-Host "Starting services..."
Push-Location $ScriptPath
try {
    # Build solution first
    dotnet restore
    if ($LASTEXITCODE -ne 0) { throw "Failed to restore packages" }
    
    dotnet build --configuration Release
    if ($LASTEXITCODE -ne 0) { throw "Failed to build solution" }

    # Start Docker services
    docker-compose -f deploy/docker/docker-compose.yml up --build -d

    # Wait for services to be healthy
    Write-Host "Waiting for services to be ready..."
    $maxAttempts = 30
    $attempt = 0
    do {
        $attempt++
        $services = docker-compose -f deploy/docker/docker-compose.yml ps --format json | ConvertFrom-Json
        $allHealthy = $true
        foreach ($service in $services) {
            if ($service.Health -ne "healthy") {
                $allHealthy = $false
                break
            }
        }
        if (-not $allHealthy -and $attempt -lt $maxAttempts) {
            Write-Host "Waiting for services... Attempt $attempt of $maxAttempts"
            Start-Sleep -Seconds 5
        }
    } while (-not $allHealthy -and $attempt -lt $maxAttempts)

    if (-not $allHealthy) {
        throw "Services failed to become healthy within timeout"
    }

    Write-Host "`nSystem is ready!"
    Write-Host "API URL: http://localhost:$ApiPort"
    Write-Host "To run performance tests: .\scripts\run-performance-test.ps1"
}
catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    docker-compose -f deploy/docker/docker-compose.yml logs
    exit 1
}
finally {
    Pop-Location
}
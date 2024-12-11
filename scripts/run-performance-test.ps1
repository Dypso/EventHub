param(
    [int]$Duration = 30,
    [int]$Connections = 1000,
    [int]$Rate = 30000
)

$ErrorActionPreference = "Stop"
$ScriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootPath = (Get-Item $ScriptPath).Parent.FullName

# Check if Bombardier is installed
if (-not (Get-Command bombardier -ErrorAction SilentlyContinue)) {
    Write-Host "Installing Bombardier..."
    if ($IsWindows) {
        # Download and install Bombardier for Windows
        $bombardierUrl = "https://github.com/codesenberg/bombardier/releases/download/v1.2.5/bombardier-windows-amd64.exe"
        $bombardierPath = Join-Path $env:TEMP "bombardier.exe"
        Invoke-WebRequest -Uri $bombardierUrl -OutFile $bombardierPath
        Move-Item $bombardierPath "$env:USERPROFILE\bin\bombardier.exe" -Force
        $env:Path += ";$env:USERPROFILE\bin"
    } else {
        # For Linux/macOS, use go install
        go install github.com/codesenberg/bombardier@latest
    }
}

# Navigate to performance test directory
Push-Location (Join-Path $RootPath "tests\TapSystem.Performance.Tests")

try {
    # Install dependencies if needed
    if (-not (Test-Path "node_modules")) {
        npm install
    }

    # Run the test
    $env:TEST_DURATION = "${Duration}s"
    $env:TEST_CONNECTIONS = $Connections
    $env:TEST_RATE = $Rate
    
    npm test

    # Display results summary
    Write-Host "`nPerformance Test Summary:"
    Write-Host "------------------------"
    Write-Host "Duration: $Duration seconds"
    Write-Host "Connections: $Connections"
    Write-Host "Target Rate: $Rate req/sec"
}
finally {
    Pop-Location
}
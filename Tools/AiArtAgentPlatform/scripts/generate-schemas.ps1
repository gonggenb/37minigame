$ErrorActionPreference = "Stop"

$platformRoot = Split-Path -Parent $PSScriptRoot
$previousPythonPath = $env:PYTHONPATH

try {
    $env:PYTHONPATH = Join-Path $platformRoot "backend"
    Push-Location $platformRoot
    python -m app.schemas.export
}
finally {
    Pop-Location
    $env:PYTHONPATH = $previousPythonPath
}

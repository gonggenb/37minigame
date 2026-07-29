[CmdletBinding()]
param(
    [string]$OutputDirectory = ""
)

$ErrorActionPreference = "Stop"
$platformRoot = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $platformRoot "shared\pilot\wuxia-stage-9.yaml"
$presetDirectory = Join-Path $platformRoot "shared\presets"
$resolvedOutput = if ($OutputDirectory) {
    [IO.Path]::GetFullPath($OutputDirectory)
} else {
    Join-Path $platformRoot "pilot-output\wuxia-stage-9"
}

if (Test-Path -LiteralPath $resolvedOutput) {
    throw "Pilot output directory already exists and cannot be overwritten: $resolvedOutput"
}

$previousPythonPath = $env:PYTHONPATH
try {
    $env:PYTHONPATH = Join-Path $platformRoot "backend"
    python -m app.pilot.runner `
        --manifest $manifestPath `
        --preset-dir $presetDirectory `
        --output $resolvedOutput
    if ($LASTEXITCODE -ne 0) {
        throw "Offline pilot failed with exit code: $LASTEXITCODE"
    }
} finally {
    $env:PYTHONPATH = $previousPythonPath
}

Write-Host "Offline pilot generated: $resolvedOutput"

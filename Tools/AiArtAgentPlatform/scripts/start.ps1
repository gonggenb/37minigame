param(
    [switch]$SmokeTest
)

$ErrorActionPreference = "Stop"

$platformRoot = Split-Path -Parent $PSScriptRoot
$checkScript = Join-Path $PSScriptRoot "check-environment.ps1"
$backendProcess = $null
$frontendProcess = $null

function Wait-ForEndpoint {
    param(
        [Parameter(Mandatory = $true)][string]$Uri,
        [Parameter(Mandatory = $true)][int]$TimeoutSeconds
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        try {
            $response = Invoke-WebRequest -Uri $Uri -UseBasicParsing -TimeoutSec 2
            if ($response.StatusCode -eq 200) {
                return
            }
        }
        catch {
            Start-Sleep -Milliseconds 250
        }
    }

    throw "Timed out waiting for local endpoint: $Uri"
}

function Get-DescendantProcessIds {
    param([Parameter(Mandatory = $true)][int]$ParentProcessId)

    $children = Get-CimInstance Win32_Process -Filter "ParentProcessId=$ParentProcessId"
    foreach ($child in $children) {
        Get-DescendantProcessIds -ParentProcessId $child.ProcessId
        Write-Output $child.ProcessId
    }
}

function Stop-ProcessTree {
    param([System.Diagnostics.Process]$Process)

    if ($null -eq $Process) {
        return
    }

    $descendantIds = @(Get-DescendantProcessIds -ParentProcessId $Process.Id)
    foreach ($processId in $descendantIds) {
        Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
    }

    if (-not $Process.HasExited) {
        Stop-Process -Id $Process.Id -Force -ErrorAction SilentlyContinue
        $Process.WaitForExit(5000) | Out-Null
    }
}

& powershell -NoProfile -ExecutionPolicy Bypass -File $checkScript
if ($LASTEXITCODE -ne 0) {
    throw "Environment check failed; the workbench was not started."
}

$pythonCommand = (Get-Command python.exe -ErrorAction Stop).Source
$pnpmCommand = (Get-Command pnpm.cmd -ErrorAction Stop).Source

try {
    $backendProcess = Start-Process `
        -FilePath $pythonCommand `
        -ArgumentList @(
            "-m", "uvicorn", "app.main:app",
            "--app-dir", "backend",
            "--host", "127.0.0.1",
            "--port", "8765"
        ) `
        -WorkingDirectory $platformRoot `
        -WindowStyle Hidden `
        -PassThru

    $frontendProcess = Start-Process `
        -FilePath $pnpmCommand `
        -ArgumentList @("--dir", "frontend", "dev") `
        -WorkingDirectory $platformRoot `
        -WindowStyle Hidden `
        -PassThru

    Wait-ForEndpoint -Uri "http://127.0.0.1:8765/api/v1/health" -TimeoutSeconds 30
    Wait-ForEndpoint -Uri "http://127.0.0.1:5173" -TimeoutSeconds 30

    Write-Output "Workbench started: http://127.0.0.1:5173"
    Write-Output "Backend PID: $($backendProcess.Id)"
    Write-Output "Frontend PID: $($frontendProcess.Id)"

    if ($SmokeTest) {
        Write-Output "Smoke test passed; stopping test processes."
        return
    }

    Write-Output "Press Ctrl+C to stop the frontend and backend."
    while (-not $backendProcess.HasExited -and -not $frontendProcess.HasExited) {
        Start-Sleep -Milliseconds 500
    }

    if ($backendProcess.HasExited) {
        throw "Backend process exited unexpectedly with code $($backendProcess.ExitCode)."
    }
    throw "Frontend process exited unexpectedly with code $($frontendProcess.ExitCode)."
}
finally {
    Stop-ProcessTree -Process $frontendProcess
    Stop-ProcessTree -Process $backendProcess
}

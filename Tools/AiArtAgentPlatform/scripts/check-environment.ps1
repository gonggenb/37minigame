$ErrorActionPreference = "Stop"

$platformRoot = Split-Path -Parent $PSScriptRoot
$failures = [System.Collections.Generic.List[string]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()

function Add-CheckResult {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][bool]$Passed,
        [Parameter(Mandatory = $true)][string]$Detail
    )

    $marker = if ($Passed) { "[OK]" } else { "[FAIL]" }
    Write-Output "$marker $Name - $Detail"
    if (-not $Passed) {
        $script:failures.Add("$Name - $Detail")
    }
}

function Test-LocalPortAvailable {
    param([Parameter(Mandatory = $true)][int]$Port)

    $listener = [System.Net.Sockets.TcpListener]::new(
        [System.Net.IPAddress]::Loopback,
        $Port
    )
    try {
        $listener.Start()
        return $true
    }
    catch {
        return $false
    }
    finally {
        $listener.Stop()
    }
}

Write-Output "2D Game AI Art Workbench - Environment Check"
Write-Output "Platform root: $platformRoot"

Add-CheckResult `
    -Name "PowerShell" `
    -Passed ($PSVersionTable.PSVersion.Major -ge 5) `
    -Detail $PSVersionTable.PSVersion.ToString()

$pythonCommand = Get-Command python.exe -ErrorAction SilentlyContinue
if ($null -eq $pythonCommand) {
    Add-CheckResult -Name "Python" -Passed $false -Detail "python.exe was not found"
}
else {
    $pythonVersion = & $pythonCommand.Source -c "import sys; print(f'{sys.version_info.major}.{sys.version_info.minor}.{sys.version_info.micro}')"
    $pythonMatches = $pythonVersion -match '^3\.12\.'
    Add-CheckResult -Name "Python" -Passed $pythonMatches -Detail "$pythonVersion ($($pythonCommand.Source))"
}

$nodeCommand = Get-Command node.exe -ErrorAction SilentlyContinue
if ($null -eq $nodeCommand) {
    Add-CheckResult -Name "Node.js" -Passed $false -Detail "node.exe was not found"
}
else {
    $nodeVersion = (& $nodeCommand.Source --version).TrimStart('v')
    $nodeMajor = [int]($nodeVersion.Split('.')[0])
    Add-CheckResult -Name "Node.js" -Passed ($nodeMajor -ge 20) -Detail "$nodeVersion ($($nodeCommand.Source))"
}

$pnpmCommand = Get-Command pnpm.cmd -ErrorAction SilentlyContinue
if ($null -eq $pnpmCommand) {
    Add-CheckResult -Name "pnpm" -Passed $false -Detail "pnpm.cmd was not found"
}
else {
    $pnpmVersion = (& $pnpmCommand.Source --version).Trim()
    Add-CheckResult -Name "pnpm" -Passed $true -Detail "$pnpmVersion ($($pnpmCommand.Source))"
}

$envPath = Join-Path $platformRoot ".env"
if (Test-Path -LiteralPath $envPath) {
    $envContent = Get-Content -Raw -Encoding UTF8 -LiteralPath $envPath
    $keyConfigured = $envContent -match '(?m)^OPENAI_API_KEY=\s*\S+'
    $keyLabel = if ($keyConfigured) { "configured" } else { "not configured" }
    Write-Output "[INFO] OpenAI API Key - $keyLabel"
}
else {
    $warnings.Add(".env was not found; the workbench can run offline, but model calls are unavailable")
    Write-Output "[WARN] OpenAI API Key - not configured (.env is missing)"
}

$dataDir = Join-Path $platformRoot "data"
try {
    New-Item -ItemType Directory -Path $dataDir -Force | Out-Null
    $probePath = Join-Path $dataDir ".write-probe"
    Set-Content -LiteralPath $probePath -Value "ok" -Encoding ASCII
    Remove-Item -LiteralPath $probePath -Force
    Add-CheckResult -Name "Data directory" -Passed $true -Detail $dataDir
}
catch {
    Add-CheckResult -Name "Data directory" -Passed $false -Detail $_.Exception.Message
}

foreach ($port in @(5173, 8765)) {
    Add-CheckResult `
        -Name "Port $port" `
        -Passed (Test-LocalPortAvailable -Port $port) `
        -Detail "127.0.0.1:$port"
}

if ($warnings.Count -gt 0) {
    Write-Output ""
    Write-Output "Warnings:"
    foreach ($warning in $warnings) {
        Write-Output "- $warning"
    }
}

if ($failures.Count -gt 0) {
    Write-Output ""
    Write-Output "Environment check failed:"
    foreach ($failure in $failures) {
        Write-Output "- $failure"
    }
    exit 1
}

Write-Output ""
Write-Output "Environment check passed."
exit 0

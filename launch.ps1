$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path $MyInvocation.MyCommand.Path -Parent
$executable = Join-Path $projectRoot 'dist\CodexUsageViewer.exe'
if (-not (Test-Path -LiteralPath $executable)) {
    throw "Build output is missing: $executable"
}

Get-Process -Name CodexUsageViewer -ErrorAction SilentlyContinue | ForEach-Object {
    try {
        $_ | Stop-Process
        $_ | Wait-Process -Timeout 5 -ErrorAction SilentlyContinue
    }
    catch {
        Write-Warning "Could not close an older CodexUsageViewer process (PID $($_.Id))."
    }
}

Start-Process -FilePath $executable
Write-Output "Started: $executable"

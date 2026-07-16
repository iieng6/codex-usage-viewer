$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path $MyInvocation.MyCommand.Path -Parent
$sourceRoot = Join-Path $projectRoot 'src'
$outputRoot = Join-Path $projectRoot 'dist'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$presentationCore = 'C:\Windows\Microsoft.NET\assembly\GAC_64\PresentationCore\v4.0_4.0.0.0__31bf3856ad364e35\PresentationCore.dll'
$presentationFramework = 'C:\Windows\Microsoft.NET\assembly\GAC_MSIL\PresentationFramework\v4.0_4.0.0.0__31bf3856ad364e35\PresentationFramework.dll'
$windowsBase = 'C:\Windows\Microsoft.NET\assembly\GAC_MSIL\WindowsBase\v4.0_4.0.0.0__31bf3856ad364e35\WindowsBase.dll'
$systemXaml = 'C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.Xaml\v4.0_4.0.0.0__b77a5c561934e089\System.Xaml.dll'

if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'Windows .NET Framework C# compiler was not found.'
}

New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null

$sources = Get-ChildItem -LiteralPath $sourceRoot -Recurse -Filter '*.cs' |
    ForEach-Object { $_.FullName }

& $compiler /nologo /target:winexe /platform:x64 /optimize+ `
    /out:"$outputRoot\CodexUsageViewer.exe" `
    /reference:"$presentationCore" `
    /reference:"$presentationFramework" `
    /reference:"$windowsBase" `
    /reference:"$systemXaml" `
    /reference:System.Runtime.Serialization.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    $sources

if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE."
}

Write-Output (Join-Path $outputRoot 'CodexUsageViewer.exe')

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path $MyInvocation.MyCommand.Path -Parent
$sourceRoot = Join-Path $projectRoot 'src'
$outputRoot = Join-Path $projectRoot 'dist'
$outputFile = Join-Path $outputRoot 'CodexUsageViewer.exe'
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
$englishResource = Join-Path $projectRoot 'Resources\Strings.en-US.txt'
$chineseResource = Join-Path $projectRoot 'Resources\Strings.zh-CN.txt'

& $compiler /nologo /target:winexe /platform:x64 /optimize+ `
    /out:"$outputFile" `
    /reference:"$presentationCore" `
    /reference:"$presentationFramework" `
    /reference:"$windowsBase" `
    /reference:"$systemXaml" `
    /reference:System.Runtime.Serialization.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    /resource:"$englishResource",CodexUsageViewer.Resources.Strings.en-US.txt `
    /resource:"$chineseResource",CodexUsageViewer.Resources.Strings.zh-CN.txt `
    $sources

if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE."
}

Write-Output $outputFile

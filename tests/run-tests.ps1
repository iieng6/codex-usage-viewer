param([string]$ExePath)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$exe = if ([string]::IsNullOrWhiteSpace($ExePath)) { Join-Path $root 'dist\CodexUsageViewer.exe' } else { $ExePath }
if (-not (Test-Path -LiteralPath $exe)) { throw 'Build output is missing.' }
$assembly = [Reflection.Assembly]::LoadFile($exe)

function Get-TypeRequired([string]$name) {
    $type = $assembly.GetType($name, $false)
    if ($null -eq $type) { throw "Type not found: $name" }
    return $type
}
function Assert-Equal($expected, $actual, [string]$message) {
    if ($expected -ne $actual) { throw "$message Expected=$expected Actual=$actual" }
}

$settings = Get-TypeRequired 'CodexUsageViewer.AppSettings'
$flags = [Reflection.BindingFlags]'Static,Public,NonPublic'
Assert-Equal 178 ($settings.GetField('CompactWindowWidth', $flags).GetRawConstantValue()) 'Compact geometry must contain only two color panes and shadow margin.'
Assert-Equal 378 ($settings.GetField('ExpandedWindowWidth', $flags).GetRawConstantValue()) 'Expanded geometry must add the localized detail width.'
Assert-Equal 60 ($settings.GetField('WindowHeight', $flags).GetRawConstantValue()) 'Window height must stay constant.'
Assert-Equal 85 ($settings.GetField('ColorPaneWidth', $flags).GetRawConstantValue()) 'Color pane widths must remain fixed.'
Assert-Equal 200 ($settings.GetField('DetailWidth', $flags).GetRawConstantValue()) 'Localized detail width must fit English status text and reset columns.'
Assert-Equal 4000 ($settings.GetField('IdleDelayMilliseconds', $flags).GetRawConstantValue()) 'Idle delay must remain four seconds.'
Assert-Equal 100 ($settings.GetField('IdleFadeMilliseconds', $flags).GetRawConstantValue()) 'Idle fade must remain fast.'
Assert-Equal 0.8 ($settings.GetField('IdleOpacity', $flags).GetRawConstantValue()) 'Idle opacity must preserve readability.'
Assert-Equal 12 ($settings.GetField('CollapsedVisualSize', $flags).GetRawConstantValue()) 'Collapsed dot must remain visually tiny.'
Assert-Equal 20 ($settings.GetField('CollapsedHitSize', $flags).GetRawConstantValue()) 'Collapsed hit target must be larger than the dot.'
Assert-Equal 0.5 ($settings.GetField('CollapsedOpacity', $flags).GetRawConstantValue()) 'Collapsed dot must default to low visual interference.'
Assert-Equal 5 ($settings.GetField('DragThreshold', $flags).GetRawConstantValue()) 'Click and drag must use a deliberate movement threshold.'
$padding = $settings.GetField('ShellPadding', $flags).GetRawConstantValue()
$compact = $settings.GetField('CompactWindowWidth', $flags).GetRawConstantValue()
$pane = $settings.GetField('ColorPaneWidth', $flags).GetRawConstantValue()
foreach ($scale in @(1.0, 1.25, 1.5)) {
    $overlapPixels = ((2 * $pane) - ($compact - 2 * $padding)) * $scale
    if ($overlapPixels -lt 1) { throw "Center overlap is insufficient at scale $scale." }
}
$colorMethod = $settings.GetMethod('ColorForRemaining', [Reflection.BindingFlags]'Static,Public,NonPublic')
Assert-Equal '#FF539169' ($colorMethod.Invoke($null, @([Nullable[int]]80))).ToString() '80% must be green.'
Assert-Equal '#FFB85353' ($colorMethod.Invoke($null, @([Nullable[int]]10))).ToString() '10% must be red.'
Assert-Equal '#FFF6F6F3' ($colorMethod.Invoke($null, @([Nullable[int]]0))).ToString() '0% must be white.'
Assert-Equal '#FF747980' ($colorMethod.Invoke($null, @($null))).ToString() 'Missing data must be gray.'

$windowType = Get-TypeRequired 'CodexUsageViewer.Usage.UsageWindow'
$snapshotType = Get-TypeRequired 'CodexUsageViewer.Usage.UsageSnapshot'
$binding = [Reflection.BindingFlags]'Instance,Public,NonPublic'
$short = [Activator]::CreateInstance($windowType, $binding, $null, @([int]20, [long]2000000000, [long]300), $null)
$long = [Activator]::CreateInstance($windowType, $binding, $null, @([int]58, [long]2000000100, [long]10080), $null)
$snapshot = [Activator]::CreateInstance($snapshotType, $binding, $null, @($short, $long), $null)
$mainType = Get-TypeRequired 'CodexUsageViewer.MainWindow'
$toCached = $mainType.GetMethod('ToCached', [Reflection.BindingFlags]'Static,Public,NonPublic')
$cached = $toCached.Invoke($null, @($snapshot, [DateTimeOffset]::Now))
Assert-Equal 80 $cached.ShortRemaining 'Used percent must convert to remaining percent.'
Assert-Equal 42 $cached.LongRemaining 'Weekly value must remain independent.'
Assert-Equal 2000000000 $cached.ShortResetUnixSeconds 'Five-hour reset must stay separate.'
Assert-Equal 2000000100 $cached.LongResetUnixSeconds 'Weekly reset must stay separate.'

$formatReset = $mainType.GetMethod('FormatResetParts', [Reflection.BindingFlags]'Static,Public,NonPublic')
$localSample = [DateTime]::new(2026, 7, 25, 13, 5, 0, [DateTimeKind]::Unspecified)
$sampleOffset = [TimeZoneInfo]::Local.GetUtcOffset($localSample)
$sampleUnix = ([DateTimeOffset]::new($localSample, $sampleOffset)).ToUnixTimeSeconds()
$formatted = $formatReset.Invoke($null, @([Nullable[long]]$sampleUnix))
Assert-Equal '07.25' $formatted[0] 'Reset date must use invariant zero-padded MM.dd.'
Assert-Equal '13:05' $formatted[1] 'Reset time must use invariant 24-hour HH:mm.'
$invalidFormatted = $formatReset.Invoke($null, @([Nullable[long]]0))
Assert-Equal '--' $invalidFormatted[0] 'Invalid reset date must remain unavailable.'
Assert-Equal '--' $invalidFormatted[1] 'Invalid reset time must remain unavailable.'

$localization = Get-TypeRequired 'CodexUsageViewer.Localization'
$resolveLanguage = $localization.GetMethod('Resolve', [Reflection.BindingFlags]'Static,NonPublic')
Assert-Equal 'zh-CN' ($resolveLanguage.Invoke($null, @('system', [Globalization.CultureInfo]::GetCultureInfo('zh-CN')))) 'zh-CN system UI must select Simplified Chinese.'
Assert-Equal 'zh-CN' ($resolveLanguage.Invoke($null, @('system', [Globalization.CultureInfo]::GetCultureInfo('zh-Hans-SG')))) 'zh-Hans system UI must select Simplified Chinese.'
Assert-Equal 'en-US' ($resolveLanguage.Invoke($null, @('system', [Globalization.CultureInfo]::GetCultureInfo('fr-FR')))) 'Non-Chinese system UI must select English.'
$setPreference = $localization.GetMethod('SetPreference', [Reflection.BindingFlags]'Static,Public,NonPublic')
$getText = $localization.GetMethod('Get', [Reflection.BindingFlags]'Static,Public,NonPublic')
$setPreference.Invoke($null, @('zh-CN')) | Out-Null
$expectedChinese = ([char]0x4E94).ToString() + [char]0x5C0F + [char]0x65F6
Assert-Equal $expectedChinese ($getText.Invoke($null, @('FiveHour'))) 'Chinese resource must load from the executable.'
Assert-Equal 'English fallback' ($getText.Invoke($null, @('FallbackProbe'))) 'Missing Chinese translations must fall back to English.'
$setPreference.Invoke($null, @('en-US')) | Out-Null
Assert-Equal '5-hour' ($getText.Invoke($null, @('FiveHour'))) 'English resource must switch immediately.'

$measureStatus = $mainType.GetMethod('MeasureStatusWidth', [Reflection.BindingFlags]'Static,NonPublic')
$statusTextWidth = [double]$measureStatus.Invoke($null, @())
$detailWidth = [double]$settings.GetField('DetailWidth', $flags).GetRawConstantValue()
$centerWidth = [Math]::Ceiling($statusTextWidth) + 8
$sideWidth = [Math]::Floor(($detailWidth - $centerWidth) / 2)
if ($centerWidth -lt ($statusTextWidth + 8)) { throw 'Center status column lacks required padding.' }
if ($sideWidth -lt 35) { throw 'Date columns are too narrow for MM.dd and HH:mm.' }

'All logic tests passed.'

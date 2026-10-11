param(
    [string]$UnityPath,
    [switch]$ShowPreview,
    [switch]$UserFixes,
    [switch]$ButtonStyles,
    [switch]$AdventureTypography,
    [switch]$WindowsHandoff
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
$versionFile = Join-Path $project 'ProjectSettings\ProjectVersion.txt'
$versionLine = Get-Content -LiteralPath $versionFile |
    Where-Object { $_ -match '^m_EditorVersion: ' } | Select-Object -First 1
$editorVersion = ([string]$versionLine -replace '^m_EditorVersion: ', '').Trim()
if (-not $editorVersion) { throw "No Unity editor version found in $versionFile." }
if (-not $UnityPath) {
    $UnityPath = Join-Path ${env:ProgramFiles} "Unity\Hub\Editor\$editorVersion\Editor\Unity.exe"
}
if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity $editorVersion not found. Install that version with Windows Build Support (Mono), or pass -UnityPath to its Unity.exe."
}
$UnityPath = (Resolve-Path -LiteralPath $UnityPath).Path
$logs = Join-Path $project 'Logs'
New-Item -ItemType Directory -Force -Path $logs | Out-Null
$buildLog = Join-Path $logs 'visual-validation-build.log'
$build = Start-Process -FilePath $UnityPath -ArgumentList @('-batchmode','-quit','-buildTarget','Win64','-projectPath',('"' + $project + '"'),'-executeMethod','Emberfall.Editor.VisualValidationBuild.Build','-logFile',('"' + $buildLog + '"')) -WindowStyle Hidden -PassThru -Wait
if ($build.ExitCode -ne 0) { throw "Visual validation build failed. See $buildLog" }
$output = Join-Path $project ('Tests\TestResults\Visual-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $output | Out-Null
$player = Join-Path $project 'Builds\VisualValidation\Emberfall.exe'
$playerLog = Join-Path $output 'player.log'
$windowStyle = if ($ShowPreview) { 'Normal' } else { 'Hidden' }
$extraArgs=if($WindowsHandoff){@('--windows-handoff')}elseif($AdventureTypography){@('--adventure-typography')}elseif($ButtonStyles){@('--button-styles')}elseif($UserFixes){@('--user-fixes')}else{@()}
$run = Start-Process -FilePath $player -ArgumentList (@('--visual-validation-root',('"' + $output + '"'),'-logFile',('"' + $playerLog + '"'),'-screen-fullscreen','0')+$extraArgs) -WindowStyle $windowStyle -PassThru
if (-not $run.WaitForExit(210000)) { Stop-Process -Id $run.Id; throw "Visual validation timed out. See $playerLog" }
$reportPath = Join-Path $output 'visual-validation-report.json'
if (-not (Test-Path -LiteralPath $reportPath)) { throw "No visual report was written. See $playerLog" }
$report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
if ($run.ExitCode -ne 0 -or $report.status -ne 'PASS') { throw "Visual validation failed: $($report.failure). See $reportPath. If the hidden window has a blank framebuffer, run again with -ShowPreview for a visible review window." }
Write-Host "PASS: $($report.assertions) checks; $($report.screenshots.Count) actual full-frame captures."
Write-Host "Inspect the PNG files in $output"

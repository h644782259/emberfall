#requires -Version 7.0
param(
    [string]$UnityPath,
    [string]$OutputDirectory,
    [ValidatePattern('^[A-Za-z][A-Za-z0-9.-]+$')][string]$BundleId = 'com.h644782259.emberfall.ios',
    [ValidatePattern('^$|^[A-Z0-9]{10}$')][string]$TeamId = '',
    [switch]$CheckOnly
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$versionText = Get-Content -LiteralPath (Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt') |
    Where-Object { $_ -match '^m_EditorVersion: ' } | Select-Object -First 1
$editorVersion = $versionText -replace '^m_EditorVersion: ', ''
if (-not $UnityPath) { $UnityPath = Join-Path ${env:ProgramFiles} "Unity/Hub/Editor/$editorVersion/Editor/Unity.exe" }
if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) { throw "Unity $editorVersion not found. Pass -UnityPath or use the Mac export script." }
$UnityPath = (Resolve-Path -LiteralPath $UnityPath).Path
$iosSupport = Join-Path (Split-Path -Parent $UnityPath) 'Data/PlaybackEngines/iOSSupport'
if (-not (Test-Path -LiteralPath $iosSupport -PathType Container)) {
    throw "This Unity editor has no iOS Build Support: $iosSupport. No Xcode project or IPA was generated. On your Mac, add iOS Build Support to Unity $editorVersion in Unity Hub, then run: bash Tools/Export-iOS.sh"
}
Write-Host "iOS Build Support found: $iosSupport"
Write-Host 'Windows can only export Xcode source with a supported Unity module. Xcode signing and installing on iPhone require your Mac.'
if ($CheckOnly) { Write-Host 'Prerequisite check only; Unity was not started.'; exit 0 }
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $projectRoot "Builds/iOS/Xcode" }
if (-not [IO.Path]::IsPathRooted($OutputDirectory)) { $OutputDirectory = Join-Path $projectRoot $OutputDirectory }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
$allowed = [IO.Path]::GetFullPath((Join-Path $projectRoot 'Builds/iOS')).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (-not $OutputDirectory.StartsWith($allowed, [StringComparison]::OrdinalIgnoreCase)) { throw 'Output must be a separate directory below this project Builds/iOS.' }
if ((Test-Path -LiteralPath $OutputDirectory) -and @(Get-ChildItem -LiteralPath $OutputDirectory -Force).Count -gt 0) { throw 'Output is not empty. Select a new directory to preserve previous Xcode work.' }
foreach ($argument in @($projectRoot, $OutputDirectory, $BundleId, $TeamId)) {
    if ($argument.Contains('"') -or $argument.Contains("`r") -or $argument.Contains("`n")) { throw 'Export paths and arguments must not contain quotes or line breaks.' }
}
$logDirectory = Join-Path $projectRoot 'Logs'
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
$logFile = Join-Path $logDirectory "ios-export-latest.log"
$arguments = @('-batchmode', '-quit', '-buildTarget', 'iOS', '-projectPath', ('"' + $projectRoot + '"'), '-executeMethod', 'Emberfall.Editor.IOSBuild.Export', '-logFile', ('"' + $logFile + '"'), '-emberfallIosOutput', ('"' + $OutputDirectory + '"'), '-emberfallIosBundleId', $BundleId)
if ($TeamId) { $arguments += @('-emberfallIosTeamId', $TeamId) }
Write-Host 'Close this project in Unity before starting the export.'
$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) {
    if (Test-Path -LiteralPath $logFile) { Get-Content -LiteralPath $logFile -Tail 60 }
    throw "Unity iOS export failed (exit $($process.ExitCode)). Log: $logFile"
}
if (-not (Test-Path -LiteralPath (Join-Path $OutputDirectory 'Unity-iPhone.xcodeproj/project.pbxproj'))) { throw "No complete Xcode project found. Inspect $logFile" }
Write-Host "Xcode source exported: $OutputDirectory"
Write-Host 'Copy the complete output to your Mac. Open the workspace/project in Xcode, configure your Team and connected iPhone, then Run. No signed IPA was generated.'

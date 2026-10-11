param([string]$UnityPath)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$versionFile = Join-Path $projectRoot 'ProjectSettings\ProjectVersion.txt'
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
$logDirectory = Join-Path $projectRoot 'Logs'
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
$logFile = Join-Path $logDirectory 'windows-build.log'
if (Test-Path -LiteralPath $logFile) { Remove-Item -LiteralPath $logFile }
$arguments = @('-batchmode', '-quit', '-buildTarget', 'Win64', '-projectPath', ('"' + $projectRoot + '"'), '-executeMethod', 'Emberfall.Editor.ProjectTools.BuildWindows', '-logFile', ('"' + $logFile + '"'))
Write-Host "Building Windows64 with Unity ${editorVersion}: $UnityPath"
$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
$startedAt = Get-Date
while (-not $process.WaitForExit(10000)) {
    $elapsed = [int]((Get-Date) - $startedAt).TotalSeconds
    $lastLine = if (Test-Path -LiteralPath $logFile) { Get-Content -LiteralPath $logFile -Tail 1 } else { 'Waiting for Unity log...' }
    Write-Host "Unity running ($elapsed seconds): $lastLine"
}
$process.WaitForExit()
if ($process.ExitCode -ne 0) {
    if (Test-Path -LiteralPath $logFile) { Select-String -LiteralPath $logFile -Pattern 'error CS\d+|Assertion .*failed' | ForEach-Object { Write-Host $_.Line } }
    if (Test-Path -LiteralPath $logFile) { Get-Content -LiteralPath $logFile -Tail 60 }
    throw "Unity build failed (exit $($process.ExitCode)). Log: $logFile"
}
$gamePath = Join-Path $projectRoot 'Builds\Windows\Emberfall.exe'
if (-not (Test-Path -LiteralPath $gamePath)) { throw "Unity exited without producing the game. See $logFile" }
if (-not (Test-Path -LiteralPath $logFile) -or
    -not (Select-String -LiteralPath $logFile -SimpleMatch 'Emberfall build ready:' -Quiet)) {
    throw "Unity did not confirm a successful fresh build. Refusing to package existing files. See $logFile"
}
Write-Host "Game ready: $gamePath"
Write-Host 'Distribute the entire Builds\Windows folder, including the _Data directory.'

[CmdletBinding()]
param(
    [string]$ProjectDirectory,
    [string]$InstallDirectory = 'E:\EMBERFALL-exe',
    [string]$PackageDirectory,
    [string]$UnityPath,
    [switch]$SkipDesktopShortcut
)
$ErrorActionPreference = 'Stop'
if (-not (Get-Command Get-FileHash -ErrorAction SilentlyContinue)) {
    Import-Module (Join-Path $PSHOME 'Modules\Microsoft.PowerShell.Utility\Microsoft.PowerShell.Utility.psd1') -ErrorAction Stop
}
if (-not $PackageDirectory) {
    $PackageDirectory = 'E:\EMBERFALL-packages'
}
$projectRoot = if ($ProjectDirectory) { [IO.Path]::GetFullPath($ProjectDirectory) } else { Split-Path -Parent (Split-Path -Parent $PSScriptRoot) }
if (-not (Test-Path -LiteralPath (Join-Path $projectRoot 'Tools\Build-Windows.ps1') -PathType Leaf)) {
    throw 'Cannot find Emberfall source. Keep this launcher in Tools\WindowsInstall, or pass -ProjectDirectory with the complete source path.'
}
$logDirectory = Join-Path $projectRoot 'Logs'
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
Start-Transcript -Path (Join-Path $logDirectory 'windows-install.log') -Force | Out-Null
try {
    & (Join-Path $PSScriptRoot 'Build-Install-Windows.ps1') -ProjectDirectory $projectRoot -InstallDirectory $InstallDirectory -PackageDirectory $PackageDirectory -UnityPath $UnityPath -SkipDesktopShortcut:$SkipDesktopShortcut
} finally {
    Stop-Transcript | Out-Null
}

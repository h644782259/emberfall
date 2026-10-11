param([string]$UnityPath)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if (-not $UnityPath) {
    $editorRoot = Join-Path ${env:ProgramFiles} 'Unity\Hub\Editor'
    if (Test-Path -LiteralPath $editorRoot) {
        $editor = Get-ChildItem -LiteralPath $editorRoot -Directory |
            Where-Object { $_.Name -match '^(6000\.|2022\.3\.)' } |
            Sort-Object { [version]($_.Name -replace 'f','.') } -Descending |
            Select-Object -First 1
        if ($editor) { $UnityPath = Join-Path $editor.FullName 'Editor\Unity.exe' }
    }
}
if (-not $UnityPath -or -not (Test-Path -LiteralPath $UnityPath)) {
    throw 'Unity editor not found. In Unity Hub install Unity 6, then Add project from disk and select this project folder. Or pass -UnityPath to this script.'
}
# This helper intentionally opens the interactive editor for the user.
Start-Process -FilePath (Resolve-Path -LiteralPath $UnityPath).Path -ArgumentList @('-projectPath', ('"' + $projectRoot + '"')) -WindowStyle Normal

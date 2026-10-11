#requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Add-Type -Path @(
    (Join-Path $projectRoot 'Assets\Scripts\Core\GameTypes.cs'),
    (Join-Path $projectRoot 'Assets\Scripts\Core\SkillRuntime.cs'),
    (Join-Path $projectRoot 'Assets\Scripts\Core\CombatBalance.cs'),
    (Join-Path $PSScriptRoot 'SkillRuntimeTests.cs')
)
[SkillRuntimeTests]::Run()

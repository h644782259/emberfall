#requires -Version 7.0
param([switch]$DownloadReferences)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$referenceRoot = Join-Path $projectRoot 'Tools\ReferenceAssemblies'
$unityReferences = Join-Path $referenceRoot 'UnityEngine\lib\netstandard2.0'
if (-not (Test-Path -LiteralPath $unityReferences)) {
    if (-not $DownloadReferences) { throw 'Reference assemblies missing. Re-run with -DownloadReferences to retrieve the compile-only UnityEngine.Modules NuGet package.' }
    New-Item -ItemType Directory -Force -Path $referenceRoot | Out-Null
    $archive = Join-Path $referenceRoot 'unityengine.modules.zip'
    Invoke-WebRequest -Uri 'https://api.nuget.org/v3-flatcontainer/unityengine.modules/2021.3.33/unityengine.modules.2021.3.33.nupkg' -OutFile $archive
    Expand-Archive -LiteralPath $archive -DestinationPath (Join-Path $referenceRoot 'UnityEngine') -Force
}
$references = @((Get-ChildItem -LiteralPath (Join-Path $PSHOME 'ref') -Filter '*.dll').FullName) + @((Get-ChildItem -LiteralPath $unityReferences -Filter '*.dll').FullName)
$sources = @((Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets\Scripts') -Recurse -Filter '*.cs').FullName)
Add-Type -Path $sources -ReferencedAssemblies $references -CompilerOptions '/langversion:9.0' -ErrorAction Stop
Write-Host "PASS: $($sources.Count) runtime source files compile against Unity 2021.3 LTS reference assemblies (C# 9)."
Write-Host 'This is API/source validation only. It does not run the Unity engine, editor tools, shaders, UI, or a Windows build.'

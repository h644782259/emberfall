param([string]$BuildDirectory, [string]$OutputDirectory)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$projectDirectory = Split-Path -Parent $PSScriptRoot
if (-not $BuildDirectory) { $BuildDirectory = Join-Path $projectDirectory 'Builds\Windows' }
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $projectDirectory 'Builds\Release' }
$buildRoot = [IO.Path]::GetFullPath($BuildDirectory).TrimEnd([IO.Path]::DirectorySeparatorChar)
$releaseRoot = [IO.Path]::GetFullPath($OutputDirectory).TrimEnd([IO.Path]::DirectorySeparatorChar)
if (-not (Test-Path -LiteralPath $buildRoot -PathType Container)) { throw "Build folder not found: $buildRoot. Run Tools\Build-Windows.ps1 first." }
if ($releaseRoot.Equals($buildRoot, [StringComparison]::OrdinalIgnoreCase) -or $releaseRoot.StartsWith($buildRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The release output must not be inside the source build directory.'
}
foreach ($required in @('Emberfall.exe', 'UnityPlayer.dll', 'Emberfall_Data')) {
    if (-not (Test-Path -LiteralPath (Join-Path $buildRoot $required))) { throw "The Unity build is incomplete: $required is missing." }
}
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (-not (Test-Path -LiteralPath $compiler)) { throw 'The .NET Framework C# compiler was not found. Windows 10/11 with .NET Framework 4.8 is required.' }
$frameworkDirectory = Split-Path -Parent $compiler
$source = Join-Path $PSScriptRoot 'Installer\Installer.cs'
if (-not (Test-Path -LiteralPath $source)) { throw "Installer source not found: $source" }
$quickStartSource = Join-Path $PSScriptRoot 'Installer\快速上手.txt'
if (-not (Test-Path -LiteralPath $quickStartSource -PathType Leaf)) { throw "Quick-start guide not found: $quickStartSource" }
New-Item -ItemType Directory -Path $releaseRoot -Force | Out-Null
$archivePath = Join-Path $releaseRoot 'Emberfall-Windows-x64.zip'
$installerPath = Join-Path $releaseRoot 'Emberfall-Setup.exe'
$checksumPath = Join-Path $releaseRoot 'SHA256SUMS.txt'

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
if ((Get-Item -LiteralPath $buildRoot).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'The build root cannot be a reparse point.' }
$buildAncestor = [IO.Path]::GetDirectoryName($buildRoot)
while ($buildAncestor) {
    if ((Get-Item -LiteralPath $buildAncestor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'The build directory cannot be reached through a reparse point.' }
    $buildAncestor = [IO.Path]::GetDirectoryName($buildAncestor)
}
$quickStartTarget = [IO.Path]::GetFullPath((Join-Path $buildRoot '快速上手.txt'))
if (-not $quickStartTarget.StartsWith($buildRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'The quick-start guide destination is outside the build directory.' }
if (Test-Path -LiteralPath $quickStartTarget) {
    $existingGuide = Get-Item -LiteralPath $quickStartTarget -Force
    if ($existingGuide.PSIsContainer -or ($existingGuide.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'The quick-start guide destination must be an ordinary file.' }
}
if ((Get-Item -LiteralPath $quickStartSource).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'The quick-start guide source cannot be a reparse point.' }
# Only this release-owned guide is copied. Personal saves are never read or copied.
Copy-Item -LiteralPath $quickStartSource -Destination $quickStartTarget -Force
$pendingDirectories = New-Object 'Collections.Generic.Queue[string]'
$pendingDirectories.Enqueue($buildRoot)
$filesToPackage = New-Object 'Collections.Generic.List[IO.FileInfo]'
while ($pendingDirectories.Count -gt 0) {
    $directory = $pendingDirectories.Dequeue()
    foreach ($item in Get-ChildItem -LiteralPath $directory -Force) {
        if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "Reparse points are not allowed in a release build: $($item.FullName)" }
        if ($item.Name.StartsWith('emberfall-save', [StringComparison]::OrdinalIgnoreCase)) { throw "A personal save was found in the build folder; it must not be distributed: $($item.FullName)" }
        if ($item.PSIsContainer) { $pendingDirectories.Enqueue($item.FullName) }
        else { $filesToPackage.Add($item) }
    }
}
$buildFiles = @($filesToPackage | Sort-Object FullName)
if ($buildFiles.Count -eq 0) { throw 'The build folder contains no files.' }
Write-Host "Packaging $($buildFiles.Count) game files from $buildRoot"
$archiveStream = [IO.File]::Open($archivePath, [IO.FileMode]::Create, [IO.FileAccess]::Write, [IO.FileShare]::None)
try {
    $archive = New-Object IO.Compression.ZipArchive($archiveStream, [IO.Compression.ZipArchiveMode]::Create, $true)
    try {
        foreach ($file in $buildFiles) {
            $relative = $file.FullName.Substring($buildRoot.Length + 1).Replace('\', '/')
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $relative, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    }
    finally { $archive.Dispose() }
}
finally { $archiveStream.Dispose() }

$references = @('System.dll', 'System.Core.dll', 'System.Drawing.dll', 'System.Windows.Forms.dll', 'System.IO.Compression.dll', 'System.IO.Compression.FileSystem.dll')
$compilerArguments = @('/nologo', '/target:winexe', '/platform:anycpu', '/optimize+', '/codepage:65001', ('/out:' + $installerPath), ('/resource:' + $archivePath + ',Emberfall.Payload.zip'))
foreach ($reference in $references) { $compilerArguments += '/reference:' + (Join-Path $frameworkDirectory $reference) }
$compilerArguments += $source
& $compiler @compilerArguments
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $installerPath)) { throw "Installer compilation failed (exit $LASTEXITCODE)." }

# Verifies the embedded archive without installing, creating shortcuts, launching
# the game, or reading/writing the user's separate save directory.
$verification = Start-Process -FilePath $installerPath -ArgumentList '--verify-payload' -WindowStyle Hidden -Wait -PassThru
if ($verification.ExitCode -ne 0) { throw "The embedded installer payload failed verification (exit $($verification.ExitCode))." }
$hashLines = foreach ($artifact in @($archivePath, $installerPath)) {
    $hash = Get-FileHash -LiteralPath $artifact -Algorithm SHA256
    $hash.Hash.ToLowerInvariant() + '  ' + [IO.Path]::GetFileName($artifact)
}
[IO.File]::WriteAllLines($checksumPath, $hashLines, (New-Object Text.UTF8Encoding($false)))
Write-Host "Portable game: $archivePath"
Write-Host "Installer:     $installerPath"
Write-Host "Checksums:     $checksumPath"
Write-Host 'Payload verified. No game installation was performed. Saves are not included.'

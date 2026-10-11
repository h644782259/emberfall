#requires -Version 7.0
# Exercises the production installer with synthetic payloads only. No Unity build,
# personal save, real installation, shortcut or game launch is used by these tests.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$projectDirectory = Split-Path -Parent $PSScriptRoot
$resultsDirectory = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'TestResults'))
$runDirectory = [IO.Path]::GetFullPath((Join-Path $resultsDirectory ('installer-' + [Guid]::NewGuid().ToString('N'))))
if (-not $runDirectory.StartsWith($resultsDirectory.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase) -or (Test-Path -LiteralPath $runDirectory)) {
    throw 'Installer tests require a brand-new directory under Tests/TestResults.'
}
New-Item -ItemType Directory -Path $runDirectory -Force | Out-Null
$script:installerReport = [ordered]@{
    status = 'RUNNING'
    startedUtc = [DateTime]::UtcNow.ToString('o')
    completedUtc = ''
    directory = $runDirectory
    assertions = 0
    checks = @()
    failure = ''
    scope = 'Synthetic payloads and newly created installation directories under Tests/TestResults only.'
}

function Assert-InstallerCheck([bool]$Condition, [string]$Description) {
    $script:installerReport.assertions++
    if (-not $Condition) { throw ('Assertion ' + $script:installerReport.assertions + ' failed: ' + $Description) }
    $script:installerReport.checks += $Description
    Write-Host ('PASS: ' + $Description)
}

function Assert-TestPath([string]$Path) {
    $absolute = [IO.Path]::GetFullPath($Path)
    if (-not $absolute.StartsWith($runDirectory + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw ('Refusing a test path outside this isolated run: ' + $absolute)
    }
}

function New-SyntheticPayload([string]$Name, [System.Collections.IDictionary]$Entries) {
    $path = Join-Path $runDirectory ($Name + '.zip')
    Assert-TestPath $path
    $stream = [IO.File]::Open($path, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try {
        $archive = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create, $true)
        try {
            foreach ($pair in $Entries.GetEnumerator()) {
                $entry = $archive.CreateEntry([string]$pair.Key)
                $output = $entry.Open()
                try {
                    $bytes = [Text.Encoding]::UTF8.GetBytes([string]$pair.Value)
                    $output.Write($bytes, 0, $bytes.Length)
                }
                finally { $output.Dispose() }
            }
        }
        finally { $archive.Dispose() }
    }
    finally { $stream.Dispose() }
    return $path
}

function Build-TestInstaller([string]$Payload, [string]$Name, [string]$Harness = '') {
    $output = Join-Path $runDirectory ($Name + '.exe')
    Assert-TestPath $output
    Assert-TestPath $Payload
    $compilerArguments = @('/nologo', '/platform:anycpu', '/optimize+', '/codepage:65001', ('/out:' + $output), ('/resource:' + $Payload + ',Emberfall.Payload.zip'))
    if ($Harness) { $compilerArguments += '/target:exe'; $compilerArguments += '/main:InstallerRollbackHarness' }
    else { $compilerArguments += '/target:winexe' }
    foreach ($reference in @('System.dll', 'System.Core.dll', 'System.Drawing.dll', 'System.Windows.Forms.dll', 'System.IO.Compression.dll', 'System.IO.Compression.FileSystem.dll')) {
        $compilerArguments += '/reference:' + (Join-Path $frameworkDirectory $reference)
    }
    $compilerArguments += (Join-Path $projectDirectory 'Tools\Installer\Installer.cs')
    if ($Harness) { Assert-TestPath $Harness; $compilerArguments += $Harness }
    & $compiler @compilerArguments | Out-Host
    if ($LASTEXITCODE -ne 0) { throw ('Test installer compilation failed: ' + $Name) }
    return $output
}

function Invoke-TestInstaller([string]$Executable, [string[]]$Arguments, [string]$Name, [int]$ExpectedExit) {
    Assert-TestPath $Executable
    if ($Arguments.Length -eq 2 -and $Arguments[0] -eq '--install-dir') { Assert-TestPath $Arguments[1] }
    $quoted = @($Arguments | ForEach-Object { '"' + $_ + '"' })
    $result = Start-Process -FilePath $Executable -ArgumentList $quoted -WorkingDirectory $projectDirectory -WindowStyle Hidden -Wait -PassThru `
        -RedirectStandardOutput (Join-Path $runDirectory ($Name + '.stdout.txt')) -RedirectStandardError (Join-Path $runDirectory ($Name + '.stderr.txt'))
    Assert-InstallerCheck ($result.ExitCode -eq $ExpectedExit) ($Name + ' returns exit code ' + $ExpectedExit + ' (actual ' + $result.ExitCode + ')')
}

function Get-InstallSnapshot([string]$Directory) {
    Assert-TestPath $Directory
    $lines = @(Get-ChildItem -LiteralPath $Directory -File -Recurse | Sort-Object FullName | ForEach-Object {
        $_.FullName.Substring($Directory.Length + 1) + ' ' + (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
    })
    return [string]::Join("`n", $lines)
}

function No-TransactionsRemain {
    return @(Get-ChildItem -LiteralPath $runDirectory -Directory -Force | Where-Object { $_.Name.StartsWith('.Emberfall-install-', [StringComparison]::Ordinal) }).Count -eq 0
}

try {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    if (-not (Test-Path -LiteralPath $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
    if (-not (Test-Path -LiteralPath $compiler)) { throw 'The .NET Framework C# compiler is required for installer tests.' }
    $frameworkDirectory = Split-Path -Parent $compiler
    if (@(Get-Process -Name Emberfall -ErrorAction SilentlyContinue).Count -gt 0) {
        throw 'A real Emberfall process is running. Production installer safety checks refuse all updates; this test will not terminate it.'
    }

    $payloadOne = New-SyntheticPayload 'payload-one' ([ordered]@{
        'Emberfall.exe' = 'SYNTHETIC executable v1 - never launch'
        'UnityPlayer.dll' = 'SYNTHETIC engine v1'
        'Emberfall_Data/Managed/Assembly-CSharp.dll' = 'SYNTHETIC game v1'
        'Emberfall_Data/version.txt' = '1'
        'version-one-only.txt' = 'Preserve this obsolete-but-unlisted file.'
    })
    $payloadTwo = New-SyntheticPayload 'payload-two' ([ordered]@{
        'Emberfall.exe' = 'SYNTHETIC executable v2 - never launch'
        'UnityPlayer.dll' = 'SYNTHETIC engine v2'
        'Emberfall_Data/Managed/Assembly-CSharp.dll' = 'SYNTHETIC game v2'
        'Emberfall_Data/version.txt' = '2'
        'changelog.txt' = 'New release v2 file.'
    })
    $payloadFailed = New-SyntheticPayload 'payload-failed' ([ordered]@{
        'Emberfall.exe' = 'SYNTHETIC executable v3 - must be rolled back'
        'new-feature.txt' = 'New v3 file - must be removed during rollback'
        'UnityPlayer.dll' = 'SYNTHETIC engine v3 - deliberately locked at commit'
        'Emberfall_Data/Managed/Assembly-CSharp.dll' = 'SYNTHETIC game v3'
        'Emberfall_Data/version.txt' = '3'
    })
    $installerOne = Build-TestInstaller $payloadOne 'Setup-One'
    $installerTwo = Build-TestInstaller $payloadTwo 'Setup-Two'
    $destination = Join-Path $runDirectory 'SyntheticInstallation'
    Assert-TestPath $destination
    Invoke-TestInstaller $installerOne @('--verify-payload') 'verify-one' 0
    Invoke-TestInstaller $installerOne @('--install-dir', $destination) 'initial-install' 0
    Assert-InstallerCheck ((Get-Content -LiteralPath (Join-Path $destination 'Emberfall.exe') -Raw) -eq 'SYNTHETIC executable v1 - never launch') 'initial CLI install writes the embedded executable bytes'
    Assert-InstallerCheck ((Get-Content -LiteralPath (Join-Path $destination 'Emberfall_Data\Managed\Assembly-CSharp.dll') -Raw) -eq 'SYNTHETIC game v1') 'initial CLI install creates nested payload files'
    Assert-InstallerCheck (No-TransactionsRemain) 'initial installation cleans its staging and backup directory'

    $extraDirectory = Join-Path $destination 'UserMods'
    New-Item -ItemType Directory -Path $extraDirectory | Out-Null
    [IO.File]::WriteAllText((Join-Path $extraDirectory 'notes.txt'), 'SYNTHETIC user-owned extra file', [Text.UTF8Encoding]::new($false))
    # A fake sentinel with a save-like filename tests that unrelated files survive.
    # This file is newly generated here; no real player save is read or copied.
    [IO.File]::WriteAllText((Join-Path $destination 'emberfall-save.json'), '{"synthetic_test_sentinel":true}', [Text.UTF8Encoding]::new($false))
    Invoke-TestInstaller $installerTwo @('--install-dir', $destination) 'update-install' 0
    Assert-InstallerCheck ((Get-Content -LiteralPath (Join-Path $destination 'Emberfall.exe') -Raw) -eq 'SYNTHETIC executable v2 - never launch') 'update CLI replaces an existing executable'
    Assert-InstallerCheck ((Get-Content -LiteralPath (Join-Path $destination 'Emberfall_Data\Managed\Assembly-CSharp.dll') -Raw) -eq 'SYNTHETIC game v2') 'update CLI replaces nested game files'
    Assert-InstallerCheck ((Get-Content -LiteralPath (Join-Path $destination 'changelog.txt') -Raw) -eq 'New release v2 file.') 'update CLI adds new payload files'
    Assert-InstallerCheck ((Get-Content -LiteralPath (Join-Path $extraDirectory 'notes.txt') -Raw) -eq 'SYNTHETIC user-owned extra file') 'update preserves unrelated user files'
    Assert-InstallerCheck ((Get-Content -LiteralPath (Join-Path $destination 'emberfall-save.json') -Raw) -eq '{"synthetic_test_sentinel":true}') 'update preserves a synthetic extra file with a save-like filename'
    Assert-InstallerCheck ((Get-Content -LiteralPath (Join-Path $destination 'version-one-only.txt') -Raw) -eq 'Preserve this obsolete-but-unlisted file.') 'update preserves old files absent from the new payload'
    Assert-InstallerCheck (No-TransactionsRemain) 'successful update cleans all transaction directories'

    $baseline = Get-InstallSnapshot $destination
    $lockedFile = [IO.File]::Open((Join-Path $destination 'UnityPlayer.dll'), [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::None)
    try { Invoke-TestInstaller $installerOne @('--install-dir', $destination) 'locked-preflight' 1 }
    finally { $lockedFile.Dispose() }
    Assert-InstallerCheck ((Get-InstallSnapshot $destination) -eq $baseline) 'a pre-existing file lock rejects an update without altering any file'
    Assert-InstallerCheck (No-TransactionsRemain) 'rejected preflight leaves no staging directory'

    $harnessPath = Join-Path $runDirectory 'InstallerRollbackHarness.cs'
    $harnessSource = @'
using System;
using System.IO;
using EmberfallInstaller;

internal static class InstallerRollbackHarness
{
    private static int Main(string[] args)
    {
        if (args.Length != 1) return 2;
        string destination = Path.GetFullPath(args[0]);
        string allowed = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "Tests", "TestResults")) + Path.DirectorySeparatorChar;
        if (!destination.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)) return 3;
        FileStream locked = null;
        bool injected = false;
        try
        {
            InstallEngine.Install(destination, delegate(int percent, string message)
            {
                if (message == "正在安装：new-feature.txt")
                {
                    if (File.ReadAllText(Path.Combine(destination, "Emberfall.exe")) != "SYNTHETIC executable v3 - must be rolled back" || !File.Exists(Path.Combine(destination, "new-feature.txt")))
                        throw new InvalidOperationException("The failure must happen after both replacement and addition.");
                    locked = new FileStream(Path.Combine(destination, "UnityPlayer.dll"), FileMode.Open, FileAccess.Read, FileShare.None);
                    injected = true;
                    Console.WriteLine("Injected real file lock after replacing the executable and adding a new file.");
                }
            });
            Console.Error.WriteLine("Expected a sharing violation during commit, but installation succeeded.");
            return 4;
        }
        catch (IOException error)
        {
            Console.WriteLine(error.Message);
            return injected && error.Message.Contains("本次游戏文件变更已回滚") ? 0 : 5;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 6; }
        finally { if (locked != null) locked.Dispose(); }
    }
}
'@
    [IO.File]::WriteAllText($harnessPath, $harnessSource, [Text.UTF8Encoding]::new($false))
    $rollbackHarness = Build-TestInstaller $payloadFailed 'Rollback-Harness' $harnessPath
    Invoke-TestInstaller $rollbackHarness @($destination) 'mid-commit-rollback' 0
    Assert-InstallerCheck ((Get-InstallSnapshot $destination) -eq $baseline) 'mid-commit I/O failure restores the exact SHA-256 snapshot of every pre-existing file'
    Assert-InstallerCheck (-not (Test-Path -LiteralPath (Join-Path $destination 'new-feature.txt'))) 'rollback removes the newly installed file'
    Assert-InstallerCheck (No-TransactionsRemain) 'successful rollback cleans its staged files and backups'

    $unrelated = Join-Path $runDirectory 'UnrelatedDirectory'
    New-Item -ItemType Directory -Path $unrelated | Out-Null
    [IO.File]::WriteAllText((Join-Path $unrelated 'notes.txt'), 'Unrelated directory sentinel')
    $unrelatedBaseline = Get-InstallSnapshot $unrelated
    Invoke-TestInstaller $installerTwo @('--install-dir', $unrelated) 'reject-unrelated-directory' 1
    Assert-InstallerCheck ((Get-InstallSnapshot $unrelated) -eq $unrelatedBaseline) 'a nonempty unrelated directory is rejected without modifications'

    $badPayload = New-SyntheticPayload 'payload-traversal' ([ordered]@{
        'Emberfall.exe' = 'SYNTHETIC'
        'UnityPlayer.dll' = 'SYNTHETIC'
        'Emberfall_Data/version.txt' = 'SYNTHETIC'
        '../escaped.txt' = 'This must never be extracted.'
    })
    $badInstaller = Build-TestInstaller $badPayload 'Setup-Unsafe'
    Invoke-TestInstaller $badInstaller @('--verify-payload') 'reject-traversal-payload' 1
    $rejectedDestination = Join-Path $runDirectory 'RejectedInstallation'
    Invoke-TestInstaller $badInstaller @('--install-dir', $rejectedDestination) 'reject-traversal-install' 1
    Assert-InstallerCheck (-not (Test-Path -LiteralPath $rejectedDestination) -and -not (Test-Path -LiteralPath (Join-Path $runDirectory 'escaped.txt'))) 'unsafe payload extraction creates neither a destination nor an escaped file'
    Assert-InstallerCheck (No-TransactionsRemain) 'all rejected installs leave no transaction directories'
    $savePayload = New-SyntheticPayload 'payload-personal-slot' ([ordered]@{
        'Emberfall.exe' = 'SYNTHETIC'
        'UnityPlayer.dll' = 'SYNTHETIC'
        'Emberfall_Data/version.txt' = 'SYNTHETIC'
        'emberfall-save-0123456789abcdef0123456789abcdef.json' = '{"synthetic_test_sentinel":true}'
    })
    $saveInstaller = Build-TestInstaller $savePayload 'Setup-PersonalSlot'
    Invoke-TestInstaller $saveInstaller @('--verify-payload') 'reject-personal-slot-payload' 1
    $script:installerReport.status = 'PASS'
    Write-Host ('PASS: ' + $script:installerReport.assertions + ' installer assertions. Report: ' + (Join-Path $runDirectory 'installer-test-report.json'))
}
catch {
    $script:installerReport.status = 'FAIL'
    $script:installerReport.failure = $_.Exception.ToString()
    throw
}
finally {
    $script:installerReport.completedUtc = [DateTime]::UtcNow.ToString('o')
    [IO.File]::WriteAllText((Join-Path $runDirectory 'installer-test-report.json'), ($script:installerReport | ConvertTo-Json -Depth 5), [Text.UTF8Encoding]::new($false))
}

#!/usr/bin/env python3
"""Restore only the missing stage cleanup and require the production lifecycle oracle to reject it."""
import os
from pathlib import Path
import subprocess
import sys
import tempfile

root = Path(__file__).resolve().parents[1]
dotnet = sys.argv[1] if len(sys.argv) > 1 else os.environ.get('DOTNET', 'dotnet')
source = (root / 'Assets/Scripts/UI/CollectionModelPreview.cs').read_text()
guard = 'try {CreateStage();}\n                catch {Dispose();state.SetYaw(yaw);throw;}'
if source.count(guard) != 1:
    raise SystemExit('FAIL: expected exactly one production stage cleanup guard; update the mutation explicitly')
expected = 'failed stage creation releases partial lights, materials and contact texture immediately'
with tempfile.TemporaryDirectory(prefix='collection-stage-recovery-negative-') as temporary:
    folder = Path(temporary)
    (folder/'RendererGroupCache.cs').write_text((root/'Assets/Scripts/Core/RendererGroupCache.cs').read_text())
    (folder / 'CollectionModelPreview.cs').write_text(source.replace(guard, 'CreateStage();', 1))
    for name in ['CollectionPreviewState', 'CollectionPreviewComposition']:
        (folder / (name + '.cs')).write_text((root / ('Assets/Scripts/UI/' + name + '.cs')).read_text())
    (folder / 'CollectionRenderLifecycleTests.cs').write_text((root / 'Tests/CollectionRenderLifecycleTests.cs').read_text())
    (folder / 'Program.cs').write_text('System.Console.WriteLine(CollectionRenderLifecycleTests.Run());')
    project = folder / 'Validation.csproj'
    project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework>'
                       '<OutputType>Exe</OutputType><LangVersion>9.0</LangVersion><NuGetAudit>false</NuGetAudit>'
                       '</PropertyGroup></Project>')
    config = folder / 'NuGet.Config'
    config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    env = dict(os.environ, DOTNET_CLI_HOME=str(folder / 'cli'), DOTNET_CLI_TELEMETRY_OPTOUT='1')
    # A restore or compile failure is a failing check, never evidence that the mutant was rejected.
    subprocess.run([dotnet, 'restore', str(project), '--configfile', str(config), '--verbosity', 'quiet'], env=env, check=True)
    subprocess.run([dotnet, 'build', str(project), '--no-restore', '-c', 'Release', '--verbosity', 'quiet'], env=env, check=True)
    result = subprocess.run([dotnet, 'run', '--project', str(project), '--no-build', '--no-restore', '-c', 'Release'],
                            env=env, capture_output=True, text=True)
    output = result.stdout + result.stderr
    if result.returncode == 0 or ('System.Exception: ' + expected) not in output or 'CollectionRenderLifecycleTests.Check' not in output:
        raise SystemExit('FAIL: mutant did not reach the exact partial-resource cleanup assertion\n' + output)
    print('PASS: removing only stage failure cleanup fails the production partial-resource lifecycle assertion '
          '(compiled successfully; controlled resource fixture, not native Unity allocation).')

#!/usr/bin/env python3
"""Compile exact side-event host methods with failure-injecting managed dependencies."""
import os
import sys
from pathlib import Path
import subprocess
import tempfile
root=Path(__file__).resolve().parents[1]
source=(root/'Assets/Scripts/Core/GameSession.Expedition.cs').read_text()
start=source.index('        private SideEventRun sideEventRun;')
end=source.index('        private void BuildSideEvent()',start)
host=source[start:end]
legacy='--legacy-baseline' in sys.argv
if legacy:
    old=subprocess.check_output(['git','show','3b224e36f58d300b102a01b287c8077cc5ff1261:Assets/Scripts/Core/GameSession.Expedition.cs'],cwd=root,text=True)
    first=old.index('            if (sideEventEnemies.Remove(enemy)')
    last=old.index('\n        }\n\n        private void BuildSideEvent()',first)
    firstHost=host.index('        private void RecordSideEventDefeat(')
    lastHost=host.index('        public bool TrySettleSideEventRewards()',firstHost)
    host=host[:firstHost]+'private void RecordSideEventDefeat(EnemyController enemy){'+old[first:last]+'}\n'+host[lastHost:]
with tempfile.TemporaryDirectory(prefix='side-event-host-') as tmp:
    tmp=Path(tmp)
    (tmp/'Host.cs').write_text('using System.Collections.Generic;using UnityEngine;namespace Emberfall{public sealed partial class GameSession{'+host+'}}')
    for name in ['Assets/Scripts/Core/SideEventRun.cs','Tests/SideEventProductionTests.cs']:
        (tmp/Path(name).name).write_text((root/name).read_text())
    (tmp/'Program.cs').write_text('System.Console.WriteLine(SideEventProductionTests.Run());')
    (tmp/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    (tmp/'Validation.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
    dotnet=os.environ.get('DOTNET','dotnet')
    subprocess.run([dotnet,'restore',str(tmp/'Validation.csproj'),'--configfile',str(tmp/'NuGet.Config')],check=True)
    result=subprocess.run([dotnet,'run','--project',str(tmp/'Validation.csproj'),'--no-restore','-c','Release'],text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
    print(result.stdout)
    if legacy:
        assert result.returncode!=0 and 'failed durable grant must not publish materials' in result.stdout,'legacy must fail the durability assertion, not compilation'
        print('PASS: original production side-event reward callback fails the durability regression as expected')
    else:
        result.check_returncode()

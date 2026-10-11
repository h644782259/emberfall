#!/usr/bin/env python3
import os,sys,tempfile,subprocess
from pathlib import Path
r=Path(__file__).resolve().parents[1]
dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
with tempfile.TemporaryDirectory(prefix='water-flow-') as d:
 p=Path(d)
 for f in ['Assets/Scripts/Core/WaterFlowPath.cs','Assets/Scripts/Core/WaterPresentation.cs','Assets/Scripts/World/WaterFlowBands.cs','Tests/WaterFlowProductionFixture.cs']:(p/Path(f).name).write_text((r/f).read_text())
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
 subprocess.run([dotnet,'restore',str(p/'Test.csproj'),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,check=True)
 subprocess.run([dotnet,'run','--project',str(p/'Test.csproj'),'--no-restore'],env=env,check=True)
 # The retained optional flow-band component still has its own clock/lifecycle
 # contract. The current world entry is one static, colored mesh with shader waves.
 from ShoreContactProductionTests import run as validate_current_surface
 validate_current_surface(dotnet,variants=('current',))

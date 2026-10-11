#!/usr/bin/env python3
import os,sys,tempfile,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
source=(root/'Assets/Scripts/Combat/EnemyController.cs').read_text()
def method(name):
 start=source.index(name);i=source.index('{',start)+1;depth=1
 while depth:
  if source[i]=='{':depth+=1
  elif source[i]=='}':depth-=1
  i+=1
 return source[start:i]
with tempfile.TemporaryDirectory(prefix='guard-return-') as temp:
 p=Path(temp)
 for file in ['Assets/Scripts/Core/EscapePostPolicy.cs','Tests/GuardReturnFixture.cs']:(p/Path(file).name).write_text((root/file).read_text())
 host=p/'Host.cs';host.write_text('using UnityEngine;namespace Emberfall {public sealed partial class EnemyController {'+method('private bool ReturnToEscapePost(')+method('private Vector3 WalkForAnimation(')+'}}')
 (p/'Program.cs').write_text('System.Console.WriteLine(GuardReturnTests.Run());')
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
 subprocess.run([dotnet,'restore',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,check=True)
 cmd=[dotnet,'run','--project',str(project),'--no-restore'];subprocess.run(cmd,env=env,check=True)
 original=host.read_text();host.write_text(original.replace('moved,preparing||chargeTime>0','dt,false'))
 subprocess.run([dotnet,'build',str(project),'--no-restore','-v:q'],env=env,check=True,stdout=subprocess.DEVNULL)
 result=subprocess.run(cmd+['--no-build'],env=env,capture_output=True,text=True)
 expected='three-second chase then stationary telegraph must complete without post cancellation'
 if result.returncode==0 or 'System.Exception: '+expected not in result.stdout+result.stderr:raise AssertionError(result.stdout+result.stderr)
 print('PASS: old elapsed-time/pre-warning-return mutation fails the exact committed-warning assertion')

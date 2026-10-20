#!/usr/bin/env python3
"""Complete every seeded room objective without a branch selection."""
from pathlib import Path
import os,sys,tempfile,subprocess
root=Path(__file__).resolve().parents[1]
test='''using System;using Emberfall;class Test{static int count;static void C(bool ok,string message){count++;if(!ok)throw new Exception(message);}static void Main(){
for(int seed=0;seed<18;seed++){var run=new RoomChainState(seed);for(int room=0;room<5;room++){var plan=run.Room;C(plan.Index==room&&!run.BranchChoiceOpen,"sequential room without choice");for(int i=0;i<plan.EnemyCount;i++)run.Register(plan,i);for(int i=0;i<plan.EnemyCount;i++)run.Defeat(plan,i);for(int i=0;i<20;i++){run.AdvanceSeal(0,.25f,true,true,false);run.Advance(.25f,true,true,false);}if(room<4){C(run.DoorUnlocked,"goal opens gate");C(!run.Next(false,false)&&!run.Next(true,true),"distance and input gate preserved");C(run.Next(true,false),"direct room travel without branch selection");}else C(run.Finished&&!run.Failed&&run.ClaimReward(true),"boss finish and reward intact");}}
Console.WriteLine("PASS "+count+" direct corridor progression assertions");}}'''

with tempfile.TemporaryDirectory(prefix='mastery-fixed-') as d:
 p=Path(d)
 for name in ['RoomChainState','RoomTactics']:(p/(name+'.cs')).write_text((root/('Assets/Scripts/Core/'+name+'.cs')).read_text())
 (p/'Test.cs').write_text(test)
 (p/'t.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
 subprocess.run([dotnet,'build',str(p/'t.csproj'),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,check=True)
 subprocess.run([dotnet,str(p/'bin/Debug/net8.0/t.dll')],env=env,check=True)

#!/usr/bin/env python3
"""Execute real core rules across the old upgrade boundary and activation gate."""
from pathlib import Path
import os,sys,tempfile,subprocess
root=Path(__file__).resolve().parents[1]
test='''using System;using Emberfall;
namespace Emberfall{public enum MasteryType{Offense,Vitality,Guard,Technique}public static class MasteryProgressionRules{public const int InitialInvestment=10;}}
class Test{static int count;static void C(bool ok,string message){count++;if(!ok)throw new Exception(message);}static void Main(){
foreach(int rank in new[]{10,19,20,35}){
var r=new MasteryCoreRuntime();r.Configure(0,rank);r.SkillHit(1);C(r.BasicHit()==1f,"fixed offense damage");r.SkillHit(2);C(r.BasicHit()==0,"cooldown blocks repeat");r.Advance(4);r.SkillHit(3);C(r.BasicHit()==1f,"fixed four second cooldown");
r.Configure(1,rank);C(r.DamageTaken(.4f)==.05f,"fixed heal");C(r.DamageTaken(.4f)==0,"heal cooldown");r.Advance(10);C(r.DamageTaken(.4f)==.05f,"fixed ten second cooldown");
r.Configure(2,rank);C(r.WardReduction==.25f&&r.PerfectDodge()==3,"fixed ward strength and duration");r.Advance(6);C(r.PerfectDodge()==3,"fixed ward cooldown");
r.Configure(3,rank);C(r.SkillSpent(44).Energy==0,"threshold not reached");var proc=r.SkillSpent(1);C(proc.Energy==12&&proc.CooldownReduction==.7f,"fixed resource proc");C(r.SkillSpent(45).Energy==0,"resource cooldown");r.Advance(6);C(r.SkillSpent(45).Energy==12,"fixed resource cooldown");}
foreach(int core in new[]{-1,0,1,2,3}){var r=new MasteryCoreRuntime();r.Configure(core,9);r.SkillHit(1);C(r.BasicHit()==0&&r.DamageTaken(.3f)==0&&r.PerfectDodge()==0&&r.SkillSpent(100).Energy==0,"locked core has no effect");}
var off=new MasteryCoreRuntime();off.Configure(-1,35);off.SkillHit(1);C(off.BasicHit()==0&&off.DamageTaken(.3f)==0&&off.WardReduction==0&&off.SkillSpent(100).Energy==0,"unselected core has no effect");
var stable=new MasteryCoreRuntime();stable.Configure(0,19);stable.SkillHit(1);stable.BasicHit();stable.Configure(0,20);stable.SkillHit(2);C(stable.BasicHit()==0,"crossing old upgrade boundary cannot reset cooldown");
Console.WriteLine("PASS "+count+" fixed mastery core assertions");}}
'''
with tempfile.TemporaryDirectory(prefix='mastery-fixed-') as d:
 p=Path(d)
 for name in ['MasteryCoreRuntime','CastFirstHitReceipt']:(p/(name+'.cs')).write_text((root/('Assets/Scripts/Core/'+name+'.cs')).read_text())
 (p/'Test.cs').write_text(test)
 (p/'t.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
 subprocess.run([dotnet,'build',str(p/'t.csproj'),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,check=True)
 subprocess.run([dotnet,str(p/'bin/Debug/net8.0/t.dll')],env=env,check=True)

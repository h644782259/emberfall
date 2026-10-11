#!/usr/bin/env python3
"""Actual scenery contact paths and rounded projectile bounds under result policy."""
from pathlib import Path
import os,sys,subprocess,tempfile
r=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
def member(rel,key):
 s=(r/'Assets/Scripts'/rel).read_text();a=s.index(key);b=s.index('{',a)+1;depth=1
 while depth:depth+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
shell=(r/'Tests/EquipmentCompositionProductionTests.Fixture.cs').read_text().split('namespace Emberfall {')[0]
shell=shell.replace('public static Vector3 zero=>','public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;public static float Distance(Vector3 a,Vector3 b)=>(a-b).magnitude;public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*t;public static float Angle(Vector3 a,Vector3 b)=>0;public static Vector3 zero=>')
shell=shell.replace('public static float deltaTime=.016f;','public static float deltaTime=.016f,time;')
shell=shell.replace('public const float PI=', 'public static int CeilToInt(float v)=>(int)Math.Ceiling(v);public static int Max(int a,int b)=>Math.Max(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static float Clamp01(float v)=>Clamp(v,0,1);public const float PI=')
shell=shell.replace('bool inactive)','bool inactive=true)').replace('public bool enabled=true,receiveShadows;','public Bounds bounds;public bool enabled=true,receiveShadows;')
shell+='namespace UnityEngine{public struct Bounds{public Vector3 center,size;public Bounds(Vector3 c,Vector3 s){center=c;size=s;}public void Encapsulate(Bounds b){Vector3 lo=new Vector3(Math.Min(center.x-size.x*.5f,b.center.x-b.size.x*.5f),Math.Min(center.y-size.y*.5f,b.center.y-b.size.y*.5f),Math.Min(center.z-size.z*.5f,b.center.z-b.size.z*.5f));Vector3 hi=new Vector3(Math.Max(center.x+size.x*.5f,b.center.x+b.size.x*.5f),Math.Max(center.y+size.y*.5f,b.center.y+b.size.y*.5f),Math.Max(center.z+size.z*.5f,b.center.z+b.size.z*.5f));center=(lo+hi)*.5f;size=hi-lo;}}}'
source=(r/'Assets/Scripts/World/DestructibleProp.cs').read_text().split('    internal sealed class DestructibleDebrisBurst')[0]+'}'
with tempfile.TemporaryDirectory(prefix='post-victory-props-') as d:
 p=Path(d);(p/'Shell.cs').write_text(shell);(p/'Actual.cs').write_text(source)
 for rel in ['Core/AdventureResultPolicy.cs','Core/DestructiblePropRules.cs','Combat/CombatDamage.cs']:(p/Path(rel).name).write_text((r/'Assets/Scripts'/rel).read_text())
 motion=''.join(member('Combat/PhysicalSkillMotion.cs',key) for key in ['internal static bool BoxContact(', 'private static float BoxDistanceSquared(', 'internal static bool ClearFlight('])
 (p/'PhysicalContact.cs').write_text('using UnityEngine;namespace Emberfall{internal static class PhysicalSkillMotion{'+motion+'}}')
 (p/'ContactGeometry.cs').write_text('using UnityEngine;namespace Emberfall{public static partial class CombatFx{'+member('Combat/CombatEffects.cs','public static float SegmentDistance(')+'}}')
 effects=member('Core/GameSession.DungeonRewards.cs','public bool CombatEffectsEnded')
 (p/'EffectsPolicy.cs').write_text('namespace Emberfall{public sealed partial class GameSession{'+effects+'}}')
 (p/'Test.cs').write_text((r/'Tests/PostVictoryPropProductionTests.cs').read_text())
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414;0169</NoWarn></PropertyGroup></Project>');cfg=p/'NuGet.Config';cfg.write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1');build=[dotnet,'build',str(project),'--configfile',str(cfg),'-v:q'];run=[dotnet,str(p/'bin/Debug/net8.0/Test.dll')]
 subprocess.run(build,env=env,check=True);subprocess.run(run,env=env,check=True)
 for before,after,oracle in [('&&!game.CombatEffectsEnded','','failed result cannot damage scenery through any combat path'),('&&!game.PracticeActive','','active practice never damages real scene props')]:
  path=p/'Actual.cs';good=path.read_text();assert good.count(before)==1;path.write_text(good.replace(before,after));subprocess.run(build+['--no-restore'],env=env,check=True,stdout=subprocess.DEVNULL)
  result=subprocess.run(run,env=env,text=True,capture_output=True);path.write_text(good)
  assert result.returncode and 'System.Exception: '+oracle in result.stdout+result.stderr,result.stdout+result.stderr
  print('PASS compiled post-result prop negative: '+oracle,flush=True)
 print('PASS: 2 compiled post-result scenery scope mutations rejected',flush=True)

#!/usr/bin/env python3
"""Production chain with only Unity/scene APIs doubled; actual authored bytes and clipping execute."""
from pathlib import Path
import tempfile,subprocess,os,sys
root=Path(__file__).resolve().parents[1]
ns={'__file__':str(root/'Tests/EffectPriorityProductionTests.py')}
exec((root/'Tests/EffectPriorityProductionTests.py').read_text().split('effects=',1)[0],ns)
s=ns['shell'];boundaries=ns['boundaries']
s=s.replace('    internal static class AuthoredSpellBases { internal static UnityEngine.Mesh Load(string name){return null;} internal static UnityEngine.Mesh Identity(string name){return null;} }','')
s=s.replace('public float Opacity;public void SetPropertyBlock(MaterialPropertyBlock block){Opacity=block.Opacity;}', 'public float Opacity,TintAlpha;public void SetPropertyBlock(MaterialPropertyBlock block){Opacity=block.Opacity;TintAlpha=block.TintAlpha;}').replace('public float Opacity;public void SetColor(string name,Color value){}','public float Opacity,TintAlpha=1;public void SetColor(string name,Color value){TintAlpha=value.a;}')
s=s.replace('public Vector3[] vertices;','public Vector3[] vertices,normals;').replace('public enum PrimitiveType{Sphere}','public enum PrimitiveType{Sphere,Capsule,Cube,Cylinder}')
s=s.replace('public static class Resources{public static T Load<T>(string name)where T:new()=>new T();}',r'''public static class Resources{public static string Root,Bad;public static string BadName="Crystal";public static T Load<T>(string name)where T:new(){if(typeof(T)==typeof(TextAsset)){if(name=="BlenderSpellBases/"+BadName&&Bad=="missing")return default(T);var path=System.IO.Path.Combine(Root,name+".bytes");if(!System.IO.File.Exists(path))return default(T);return (T)(object)new TextAsset{bytes=name=="BlenderSpellBases/"+BadName&&Bad=="malformed"?new byte[]{1,2,3}:System.IO.File.ReadAllBytes(path)};}return new T();}}''')
boundaries=boundaries.replace(' internal static class AuthoredActorMeshes{public static Mesh Decode(byte[] b,string n)=>HeroSkillBodyAssets.Rock;}','')
s+='namespace UnityEngine {public class Camera:Component{public static Camera main;public DepthTextureMode depthTextureMode;}public partial struct Color{public static Color Lerp(Color a,Color b,float t)=>new Color(a.r+(b.r-a.r)*t,a.g+(b.g-a.g)*t,a.b+(b.b-a.b)*t,a.a+(b.a-a.a)*t);}}'
boundaries+='namespace Emberfall{internal static class VanguardWardVisual{internal static void Prewarm(){}}}'

s=s.replace('public bool HasStarted,ModeFinished,InputBlocked;', 'public bool HasStarted,ModeFinished,InputBlocked,InDungeon,DungeonCleared,FinishedResultDismissed,PracticeActive;public bool CombatEnded=>ModeFinished||DungeonCleared;public bool CombatEffectsEnded=>!AdventureResultPolicy.AcceptsPlayerEffects(HasStarted,CombatEnded,InDungeon,DungeonCleared,PracticeActive,FinishedResultDismissed);')
with tempfile.TemporaryDirectory(prefix='post-victory-visual-') as d:
 p=Path(d);(p/'Stubs.cs').write_text(s);(p/'Boundaries.cs').write_text('using System;using System.Linq;using System.Collections.Generic;using UnityEngine;'+boundaries)
 for f in ['Assets/Scripts/Core/AdventureResultPolicy.cs','Assets/Scripts/Core/ElementalistFieldTuning.cs','Assets/Scripts/Core/FilledVfxRecipes.cs','Assets/Scripts/Core/FilledVfxPlacement.cs','Assets/Scripts/Core/SkillVisualAftermathTiming.cs','Assets/Scripts/Core/CombatVisualBudget.cs','Assets/Scripts/Combat/CombatVisualLease.cs','Assets/Scripts/Combat/AnchoredImpactMesh.cs','Assets/Scripts/Combat/PhysicalSkillMotion.cs','Assets/Scripts/Combat/FilledSkillVfx.cs','Assets/Scripts/Combat/AuthoredActorMeshes.cs','Assets/Scripts/Combat/AuthoredSpellBases.cs','Tests/PostVictoryVisualProductionTests.cs']:(p/Path(f).name).write_text((root/f).read_text())
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414;0169</NoWarn></PropertyGroup></Project>');cfg=p/'NuGet.Config';cfg.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1');dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
 build=[dotnet,'build',str(p/'Test.csproj'),'--configfile',str(cfg),'-v:q'];run=[dotnet,str(p/'bin/Debug/net8.0/Test.dll'),str(root/'Assets/Resources')]
 subprocess.run(build,env=env,check=True);subprocess.run(run,env=env,check=True)
 for before,after,oracle in [('if(game.CombatEffectsEnded)\n            {','if(game.ModeFinished)\n            {','new post-result filled body survives its normal active lifetime'),('if(settling&&game.CombatEffectsEnded)','if(settling&&game.CombatEnded)','new post-result filled aftermath is visible and decays normally')]:
  path=p/'FilledSkillVfx.cs';good=path.read_text();assert good.count(before)==1;path.write_text(good.replace(before,after));subprocess.run(build+['--no-restore'],env=env,check=True,stdout=subprocess.DEVNULL)
  result=subprocess.run(run,env=env,text=True,capture_output=True);path.write_text(good)
  assert result.returncode and 'System.Exception: '+oracle in result.stdout+result.stderr,result.stdout+result.stderr
  print('PASS compiled post-result visual negative: '+oracle,flush=True)
 print('PASS: 2 compiled post-result visual mutations rejected',flush=True)

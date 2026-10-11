#!/usr/bin/env python3
import os,sys,tempfile,subprocess,argparse
from pathlib import Path
root=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser();parser.add_argument('dotnet',nargs='?',default=os.environ.get('DOTNET','dotnet'));parser.add_argument('--combat-model',type=Path);args=parser.parse_args();dotnet=args.dotnet
source=(args.combat_model or root/'Assets/Scripts/Combat/CombatModel.cs').read_text()
pilot_hook='SampleBlenderPilot(acting,t,hurt)' in source
def method(signature):
 start=source.index(signature);i=source.index('{',start)+1;depth=1
 while depth:
  if source[i]=='{':depth+=1
  elif source[i]=='}':depth-=1
  i+=1
 return source[start:i]
pose_partials=['CombatModel.Recovery','CombatModel.CastPoses','CombatModel.SkillPerformance','CombatModel.SkillReleasePose']
def skill_pose_fixture():
 # Only engine/model-factory boundaries are doubled; every skill action and
 # performance clock comes from its unchanged production partial below.
 fixture=(root/'Tests/HeroPoseCommitFixture.cs').read_text()
 fixture=fixture.replace('public enum Space {Self}','public enum PrimitiveType {Cube}\n public enum Space {Self}')
 fixture=fixture.replace('public const float PI=','public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp01(t);public const float PI=')
 fixture=fixture.replace('public CombatModel(HeroClass hero){heroClass=hero;}', '''
  private Transform leftFoot=new Transform(),rightFoot=new Transform();
  private Transform Part(string name,PrimitiveType shape,Vector3 at,Vector3 scale,Color color,Transform parent=null,VisualSurface? surface=null,string meshModule=null)=>new Transform{parent=parent,localPosition=at,localScale=scale};
  public CombatModel(HeroClass hero){heroClass=hero;}
''')
 return fixture+'\nnamespace Emberfall{public enum VisualSurface{Metal}}'
with tempfile.TemporaryDirectory(prefix='hero-pose-commit-') as path:
 p=Path(path)
 for name in ['HeroMotionStyle','GameTypes','CombatBalance','SkillDamageBudgets','BasicActionTimeline','VisualMotionEnvelope','CasterPoseRecipe']:(p/(name+'.cs')).write_text((root/'Assets/Scripts/Core'/(name+'.cs')).read_text())
 for name in pose_partials:(p/(name+'.cs')).write_text((root/'Assets/Scripts/Combat'/(name+'.cs')).read_text())
 (p/'Fixture.cs').write_text(skill_pose_fixture())
 body='\n'.join(method(x) for x in ['public bool SwordActionActive','public bool TryClaimSwordRibbon(', 'public void PlayAction(','public void CancelAction(','public void ReleaseCharge(','private void CommitActionPose(','private static Quaternion Pose(','private void AnimateHero('])
 production=p/'HeroPose.cs';production.write_text('using UnityEngine;namespace Emberfall {public sealed partial class CombatModel {'+body+'}}')
 (p/'Program.cs').write_text('HeroPoseCommitTests.RequirePilotSample='+str(pilot_hook).lower()+';System.Console.WriteLine(HeroPoseCommitTests.Run());')
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>0649;0169</NoWarn></PropertyGroup></Project>')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
 subprocess.run([dotnet,'restore',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,check=True)
 cmd=[dotnet,'run','--project',str(project),'--no-restore'];subprocess.run(cmd,env=env,check=True)
 original=production.read_text()
 for before,after,expected in [('CommitActionPose();','', 'committed action writes its release arm before any later Update'),('BeginVisualRecovery(false);','BeginVisualRecovery();','LateUpdate charge release replaces old charge arm and weapon immediately')]:
  production.write_text(original.replace(before,after))
  subprocess.run([dotnet,'build',str(project),'--no-restore','-v:q'],env=env,check=True,stdout=subprocess.DEVNULL)
  result=subprocess.run(cmd+['--no-build'],env=env,capture_output=True,text=True)
  if result.returncode==0 or 'System.Exception: '+expected not in result.stdout+result.stderr:raise AssertionError(result.stdout+result.stderr)
 production.write_text(original)
 if pilot_hook:
  production.write_text(original.replace('SampleBlenderPilot(acting,t,hurt)','SampleBlenderPilot(acting,0,hurt)'))
  subprocess.run([dotnet,'build',str(project),'--no-restore','-v:q'],env=env,check=True,stdout=subprocess.DEVNULL)
  result=subprocess.run(cmd+['--no-build'],env=env,capture_output=True,text=True)
  assert result.returncode and 'System.Exception: pilot receives contact phase synchronously at basic commit' in result.stdout+result.stderr,result.stdout+result.stderr
  production.write_text(original)
  print('PASS: compiled zero-phase pilot mutation fails synchronous contact forwarding assertion')
 print('PASS: 2 compiled old-pose/handoff negative controls hit exact runtime assertions')

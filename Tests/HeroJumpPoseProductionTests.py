#!/usr/bin/env python3
"""Real jump phasing and rig composition; engine transforms are managed boundaries."""
from pathlib import Path
import os, sys, subprocess, tempfile
root=Path(__file__).resolve().parents[1]
ns={'__file__':str(root/'Tests/HeroSkillReleasePoseProductionTests.py')}
exec((root/'Tests/HeroSkillReleasePoseProductionTests.py').read_text().split("driver = r",1)[0],ns)
fixture=ns['fixture'].replace('private readonly PoseLocomotion locomotion=new PoseLocomotion();','private readonly LocomotionPoseState locomotion=new LocomotionPoseState();').replace('private void ApplyHeroJumpPose(bool casting){}','').replace('private void ApplyHeroLocomotion(){}','private void ApplyHeroLocomotion(){leftFoot.localRotation=rightFoot.localRotation=Quaternion.identity;} // Flat stationary anatomy baseline, outside tested airborne composition\n')
fixture=fixture.replace('public Vector3 Hip=>', 'public Quaternion[] JumpBones()=>new[]{spine.localRotation,headRig.localRotation,leftArm.localRotation,rightArm.localRotation,leftElbow.localRotation,rightElbow.localRotation,leftLeg.localRotation,rightLeg.localRotation,leftKnee.localRotation,rightKnee.localRotation,leftFoot.localRotation,rightFoot.localRotation,cloak.localRotation};public void Jump(float dt,bool air,float t){locomotion.Advance(0,0,dt,4,false,air,t);}public LocomotionPoseState Gait=>locomotion;public Vector3 Hip=>')
driver=r"""
using System;using Emberfall;using UnityEngine;
static class Program {
 static int count;static void C(bool yes,string why){count++;if(!yes)throw new Exception(why);}static bool Same(Quaternion a,Quaternion b)=>Quaternion.Difference(a,b)<.00001f;static int Diff(Quaternion[] a,Quaternion[] b){int n=0;for(int i=0;i<a.Length;i++)if(!Same(a[i],b[i]))n++;return n;}
 static void Main(){foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass))){
  var m=new CombatModel(hero);m.Frame(0);var idle=m.JumpBones();var root=m.ModelRoot;var phases=new Quaternion[4][];int i=0;
  foreach(float t in new[]{0f,.25f,.5f,.85f}){m.Jump(.016f,true,t);m.Frame(0);phases[i++]=m.JumpBones();C(Diff(idle,m.JumpBones())>=9,"airborne rig coordinates torso, head, limbs, ankles and cloak");C(!Same(idle[8],m.JumpBones()[8])&&!Same(idle[9],m.JumpBones()[9]),"both knees participate in jump phases");C(!Same(idle[10],m.JumpBones()[10])&&!Same(idle[11],m.JumpBones()[11]),"both ankles counterbalance jump phases");var pose=m.JumpBones();var hip=m.Hip;for(int repeat=0;repeat<8;repeat++){m.Frame(0);C(Diff(pose,m.JumpBones())==0&&Math.Abs(hip.y-m.Hip.y)<.000001f,"repeat zero-delta render does not accumulate joint or hip offsets");}C(Math.Abs(root.x-m.ModelRoot.x)<.000001f&&Math.Abs(root.z-m.ModelRoot.z)<.000001f,"jump pose preserves controller-owned root trajectory");}
  for(i=1;i<phases.Length;i++)C(Diff(phases[i-1],phases[i])>=8,"push, tuck and landing reach are distinct coordinated rig phases");
  m.Jump(.016f,false,1);m.Frame(0);var landed=m.JumpBones();C(m.Gait.Landing==1&&Diff(idle,landed)>=8&&m.Hip.y<.83f,"air-to-ground transition absorbs landing across joints and pelvis");
  var progress=m.Gait.JumpProgress;var landing=m.Gait.Landing;m.Jump(0,true,.3f);m.Jump(float.NaN,true,.3f);m.Frame(0);C(m.Gait.JumpProgress==progress&&m.Gait.Landing==landing&&!m.Gait.Airborne&&Diff(landed,m.JumpBones())==0,"paused/invalid advance freezes airborne and landing state");
  m.Jump(.21f,false,1);m.Frame(0);C(m.Gait.Landing==0&&Diff(idle,m.JumpBones())==0,"landing recovers to the ordinary rig within the existing finite clock");
  Time.frameCount++;var groundCast=new CombatModel(hero);groundCast.PlayAction(0,false);var airCast=new CombatModel(hero);airCast.Jump(.016f,true,.5f);airCast.PlayAction(0,false);var a=groundCast.JumpBones();var b=airCast.JumpBones();for(i=2;i<=5;i++)C(Same(a[i],b[i]),"airborne cast retains real local release arm and elbow pose");C(!Same(a[8],b[8])&&!Same(a[9],b[9]),"airborne cast still composes coordinated lower-body tuck");
 }
 Console.WriteLine("PASS "+count+" real jump rig/phasing/landing/root/zero-delta/cast-composition assertions; managed transforms, not device motion acceptance.");}
}
"""
dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
with tempfile.TemporaryDirectory(prefix='emberfall-hero-jump-') as directory:
 work=Path(directory)
 for name in ['HeroMotionStyle','GameTypes','CombatBalance','SkillDamageBudgets','BasicActionTimeline','VisualMotionEnvelope','CasterPoseRecipe','LocomotionPoseState']:(work/(name+'.cs')).write_text((root/'Assets/Scripts/Core'/(name+'.cs')).read_text())
 for name in ['CombatModel.Recovery','CombatModel.CastPoses','CombatModel.SkillPerformance','CombatModel.SkillReleasePose','CombatModel.HeroJumpPose']:(work/(name+'.cs')).write_text((root/'Assets/Scripts/Combat'/(name+'.cs')).read_text())
 (work/'Fixture.cs').write_text(fixture+'\nnamespace Emberfall{public enum VisualSurface{Metal}}')
 (work/'Production.cs').write_text('using UnityEngine;namespace Emberfall{public sealed partial class CombatModel{'+ns['methods']+'}}');(work/'Program.cs').write_text(driver)
 project=work/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>0649;0169;0414</NoWarn></PropertyGroup></Project>')
 config=work/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(work/'cli'),DOTNET_NOLOGO='1')
 subprocess.run([dotnet,'restore',str(project),'--configfile',str(config),'-v:q'],env=env,check=True)
 subprocess.run([dotnet,'run','--project',str(project),'--no-restore','-c','Release'],env=env,check=True)

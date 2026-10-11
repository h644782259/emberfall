#!/usr/bin/env python3
"""Class-specific accepted-displacement poses; controlled transform fixture, not Unity rendering."""
from pathlib import Path
import os,sys,tempfile,subprocess,hashlib
r=Path(__file__).resolve().parents[1]
def member(s,key):
 start=s.index(key);end=s.index('{',start)+1;depth=1
 while depth:depth+=(s[end]=='{')-(s[end]=='}');end+=1
 return s[start:end]
motion=(r/'Assets/Scripts/Combat/CombatModel.Motion.cs').read_text();apply=member(motion,'private void ApplyHeroLocomotion(')
program=r'''
using System;using Emberfall;
namespace UnityEngine {
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);}
 // Euler storage and additive composition are explicit test doubles, not quaternion/render verification.
 public struct Quaternion {public Vector3 e;public static Quaternion Euler(float x,float y,float z)=>new Quaternion{e=new Vector3(x,y,z)};public static Quaternion operator*(Quaternion a,Quaternion b)=>new Quaternion{e=a.e+b.e};}
 public class Transform {public Vector3 localPosition;public Quaternion localRotation;}
 public static class Mathf {public static float Sin(float x)=>(float)Math.Sin(x);public static float Abs(float x)=>Math.Abs(x);public static float Max(float a,float b)=>Math.Max(a,b);}
}
namespace Emberfall {
 public enum HeroClass{Vanguard,Ranger,Arcanist,Summoner}
 public class Cloth {public float pitch,side;public void SetInertia(float p,float s){pitch=p;side=s;}}
 public sealed partial class CombatModel {
 public HeroClass heroClass;public LocomotionPoseState locomotion=new LocomotionPoseState();public VisualMotionEnvelope visualMotion=new VisualMotionEnvelope();
 public UnityEngine.Transform leftLeg=new UnityEngine.Transform(),rightLeg=new UnityEngine.Transform(),leftKnee=new UnityEngine.Transform(),rightKnee=new UnityEngine.Transform(),pelvis=new UnityEngine.Transform(),spine=new UnityEngine.Transform(),cloak=new UnityEngine.Transform();public Cloth tailoredCloth=new Cloth();
 public void Frame(float x,float z,float dt,float turn=0){locomotion.Advance(x,z,dt,4,true,false,0);visualMotion.Advance(turn,locomotion.Side,locomotion.Forward,dt);pelvis.localPosition=new UnityEngine.Vector3(0,.83f,0);leftKnee.localRotation=rightKnee.localRotation=spine.localRotation=default;ApplyHeroLocomotion();}
 }
}
class Program {
 static int checks;static void C(bool b,string why){checks++;if(!b)throw new Exception(why);}static bool N(float a,float b)=>Math.Abs(a-b)<.0001f;
 static void Main(){
 var models=new CombatModel[4];for(int j=0;j<4;j++)models[j]=new CombatModel{heroClass=(HeroClass)j};
 for(int i=0;i<30;i++)foreach(var m in models)m.Frame(0,4f/60,1f/60);
 C(Math.Abs(models[1].leftLeg.localRotation.e.x)>Math.Abs(models[2].leftLeg.localRotation.e.x)*1.5f,"ranger light step differs from restrained caster");
 C(models[0].pelvis.localPosition.y<models[2].pelvis.localPosition.y-.02f,"vanguard lower center");C(models[0].leftLeg.localRotation.e.z<-4,"vanguard wider stance");
 float phase=models[0].locomotion.Phase;
 foreach(var m in models)C(N(m.locomotion.Phase,phase),"style never changes accepted distance clock");
 for(int i=0;i<120;i++)foreach(var m in models)m.Frame(0,0,1f/60);
 foreach(var m in models){C(N(m.locomotion.Phase,phase),"wall/stop does not walk in place");C(Math.Abs(m.leftLeg.localRotation.e.x)<.001f,"stop settles legs");}
 for(int i=0;i<45;i++)foreach(var m in models)m.Frame(4f/60,0,1f/60);
 C(Math.Abs(models[1].leftLeg.localRotation.e.z)>Math.Abs(models[2].leftLeg.localRotation.e.z)*1.3f,"ranger side-step readable");
 for(int i=0;i<180;i++)foreach(var m in models){m.Frame(0,0,1f/60,i<30?2:0);C(Math.Abs(m.cloak.localRotation.e.y)<=30,"turn cloth stays bounded");}
 foreach(var m in models){float p=m.locomotion.Phase;m.Frame(2,2,0);C(N(p,m.locomotion.Phase),"paused pose clock frozen");}
 C(HeroMotionStyle.For(HeroClass.Summoner).Observation>0&&HeroMotionStyle.For(HeroClass.Arcanist).Observation==0,"summoner observation distinct from steady casting");
 Console.WriteLine("PASS "+checks+" actual ApplyHeroLocomotion assertions; no Unity/render/weapon intersection acceptance");
 }
}
'''
with tempfile.TemporaryDirectory(prefix='hero-motion-round2-') as tmp:
 p=Path(tmp);(p/'Program.cs').write_text(program);(p/'Motion.cs').write_text('using UnityEngine;namespace Emberfall{public sealed partial class CombatModel{'+apply+'}}')
 for name in ['HeroMotionStyle','LocomotionPoseState','VisualMotionEnvelope']:(p/(name+'.cs')).write_bytes((r/('Assets/Scripts/Core/'+name+'.cs')).read_bytes())
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>');proj=p/'Test.csproj';proj.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
 sdk=sys.argv[1] if len(sys.argv)>1 else 'dotnet';env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 for mutation in [False,True]:
  (p/'Motion.cs').write_text('using UnityEngine;namespace Emberfall{public sealed partial class CombatModel{'+((r/'Tests/Fixtures/LegacySharedHeroLocomotion.txt').read_text() if mutation else apply)+'}}')
  b=subprocess.run([sdk,'build',str(proj),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,capture_output=True,text=True);print(b.stdout,b.stderr);assert b.returncode==0
  q=subprocess.run([sdk,str(p/'bin/Debug/net8.0/Test.dll')],env=env,capture_output=True,text=True);print(q.stdout,q.stderr)
  if mutation:assert q.returncode!=0 and 'ranger light step differs' in q.stderr;print('PASS exact baseline shared-style method compiled and rejected')
  else:assert q.returncode==0
model=(r/'Assets/Scripts/Combat/CombatModel.cs').read_text()
assert 'gaitPhase = locomotion.Phase' in model and 'speed = smoothedSpeed = locomotion.Speed' in model
assert 'motionStyle.Bob' in model and '1.12f - motionStyle.Crouch' in model and '(acting ? 0 : breathing * motionStyle.Observation)' in model
assert 'ApplyAuthoredVanguardPose(acting,t,hurt);' in model and 'ApplyVisualRecovery(dt);' in model
print('PASS real hero pose uses class style, authored layer and original recovery; controls/physics not executed')

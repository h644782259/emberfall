#!/usr/bin/env python3
"""Compile actual impulse solver and CombatModel reaction methods with managed TRS.

This is numerical/pose evidence; native mesh skinning and appearance use the
NaturalModelsScenePreview editor harness.
"""
from pathlib import Path
import os, subprocess, sys, tempfile

root = Path(__file__).resolve().parents[1]
source = (root / 'Assets/Scripts/Combat/CombatModel.cs').read_text()

def member(signature):
    start = source.index(signature)
    end = source.index('{', start) + 1
    depth = 1
    while depth:
        depth += (source[end] == '{') - (source[end] == '}')
        end += 1
    return source[start:end]

fixture = r'''
using System;using UnityEngine;
namespace UnityEngine{
 public class MonoBehaviour{public Transform transform=new Transform();}
 public struct Vector3{
  public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static Vector3 zero=>new Vector3();public static Vector3 forward=>new Vector3(0,0,1);public static Vector3 right=>new Vector3(1,0,0);public static Vector3 up=>new Vector3(0,1,0);public float sqrMagnitude=>x*x+y*y+z*z;public float magnitude=>(float)Math.Sqrt(sqrMagnitude);public Vector3 normalized=>magnitude>0?this/magnitude:zero;
  public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator-(Vector3 a)=>zero-a;public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);public static Vector3 operator/(Vector3 a,float b)=>a*(1/b);public static Vector3 ClampMagnitude(Vector3 a,float max)=>a.magnitude>max?a.normalized*max:a;
 }
 public struct Quaternion{
  public System.Numerics.Quaternion value;public static Quaternion identity=>new Quaternion{value=System.Numerics.Quaternion.Identity};public static Quaternion Euler(float x,float y,float z)=>new Quaternion{value=System.Numerics.Quaternion.CreateFromYawPitchRoll(y*(float)Math.PI/180,x*(float)Math.PI/180,z*(float)Math.PI/180)};public static Quaternion Slerp(Quaternion a,Quaternion b,float t)=>new Quaternion{value=System.Numerics.Quaternion.Slerp(a.value,b.value,t)};public static Quaternion operator*(Quaternion a,Quaternion b)=>new Quaternion{value=a.value*b.value};public static Vector3 operator*(Quaternion a,Vector3 b){var p=System.Numerics.Vector3.Transform(new System.Numerics.Vector3(b.x,b.y,b.z),a.value);return new Vector3(p.X,p.Y,p.Z);}
 }
 public class Transform{public Transform parent;public Vector3 localPosition;public Quaternion localRotation=Quaternion.identity;public Vector3 forward=>localRotation*Vector3.forward;public Vector3 InverseTransformDirection(Vector3 v)=>new Quaternion{value=System.Numerics.Quaternion.Inverse(localRotation.value)}*v;}
 public static class Time{public static float deltaTime,time;public static int frameCount;}
 public static class Mathf{public static float Exp(float a)=>(float)Math.Exp(a);public static float Sin(float a)=>(float)Math.Sin(a);public static float Cos(float a)=>(float)Math.Cos(a);public static float Clamp(float a,float b,float c)=>Math.Min(c,Math.Max(b,a));public static float Clamp01(float a)=>Clamp(a,0,1);public static float MoveTowards(float a,float b,float d)=>Math.Abs(b-a)<=d?b:a+Math.Sign(b-a)*d;}
}
namespace Emberfall{
 public class EnemyStatusEffects{public bool KnockedDown,IsAirborne,IsFrozen;public float AirborneHeight,AirborneVelocity;}
 public class EnemyController{public bool IsDead,IsBoss,IsStunned;public EnemyStatusEffects StatusEffects=new EnemyStatusEffects();}
 public sealed partial class CombatModel:MonoBehaviour{
  private EnemyController enemyOwner;private bool dying,isHero,articulatedEnemy;private float recoilStarted,recoilStrength;private Vector3 recoilDirection,recoilOffset,recoilVelocity;private int recoilFrame=-1;
  private Transform body=new Transform(),spine=new Transform(),headRig=new Transform(),pelvis=new Transform(),leftArm=new Transform(),rightArm=new Transform(),leftElbow=new Transform(),rightElbow=new Transform(),leftLeg=new Transform(),rightLeg=new Transform(),leftKnee=new Transform(),rightKnee=new Transform(),leftFoot=new Transform(),rightFoot=new Transform();
  public CombatModel(bool hero){isHero=hero;articulatedEnemy=!hero;transform.parent=new Transform();}
  public void Control(bool stunned,bool frozen=false,bool down=false,bool airborne=false){if(enemyOwner==null)enemyOwner=new EnemyController();enemyOwner.IsStunned=stunned;enemyOwner.StatusEffects.IsFrozen=frozen;enemyOwner.StatusEffects.KnockedDown=down;enemyOwner.StatusEffects.IsAirborne=airborne;}
  public void ControlPose(float delta){Pose(0);ApplyEnemyControlPose(delta);}
  public Vector3 Offset=>recoilOffset;public Vector3 Velocity=>recoilVelocity;
  public void Pose(float delta){transform.localPosition=Vector3.zero;foreach(var joint in new[]{body,spine,headRig,pelvis,leftArm,rightArm,leftElbow,rightElbow,leftLeg,rightLeg,leftKnee,rightKnee,leftFoot,rightFoot})joint.localRotation=Quaternion.identity;ApplyRecoil(delta);}
  public Vector3 Chest=>spine.localRotation*Vector3.up;public Vector3 Gaze=>headRig.localRotation*Vector3.up;public Vector3 Knee=>leftKnee.localRotation*Vector3.up;public Vector3 Foot=>leftFoot.localRotation*Vector3.up;
 }
}
'''
fixture=fixture.replace('public static float Exp(float a)', 'public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp01(t);public static float SmoothStep(float a,float b,float t){t=Clamp01(t);return a+(b-a)*t*t*(3-2*t);}public static float Exp(float a)')
driver = r'''
using System;using Emberfall;using UnityEngine;
static class ActorChecks{
 static int checks;static void C(bool ok,string label){checks++;if(!ok)throw new Exception(label);}static bool Same(Vector3 a,Vector3 b,float epsilon=.000003f)=>(a-b).magnitude<epsilon;
 static void Main(){
  Vector3 expected=Vector3.zero;int sample=0;
  foreach(int hz in new[]{15,30,60,120}){
   Vector3 offset=Vector3.zero,velocity=Vector3.zero;ActorImpactSpring.Impulse(ref velocity,Vector3.forward,1);for(int i=0;i<hz;i++)ActorImpactSpring.Advance(ref offset,ref velocity,1f/hz);
   C(ActorImpactSpring.Finite(offset)&&ActorImpactSpring.Finite(velocity),"finite across device frame rates");if(sample++==0)expected=offset;else C(Same(expected,offset),"elapsed reaction agrees at 15/30/60/120 Hz");
  }
  foreach(bool hero in new[]{false,true}){
   var model=new CombatModel(hero);var logical=model.transform.parent;logical.localPosition=new Vector3(5,0,8);logical.localRotation=Quaternion.Euler(0,40,0);
   Time.frameCount++;Time.deltaTime=1f/60;model.Recoil(logical.forward,1);model.Pose(Time.deltaTime);
   C(model.Offset.z>0&&Math.Abs(model.Offset.x)<.00001,"direction converts into owner space");C(Math.Abs(model.Chest.z)>.01,"actual chest absorbs confirmed impact");C(model.Chest.z*model.Gaze.z<0,"head counterbalances chest");C(model.Knee.z*model.Foot.z<0,"knee absorption keeps ankle counterbalanced");
   C(Same(logical.localPosition,new Vector3(5,0,8)),"physical visual never moves controller/collider");var offset=model.Offset;var velocity=model.Velocity;var pose=model.Chest;model.Pose(Time.deltaTime);C(Same(offset,model.Offset)&&Same(velocity,model.Velocity)&&Same(pose,model.Chest),"same-frame pose resample advances once and does not accumulate");
   Time.frameCount++;model.Pose(0);C(Same(offset,model.Offset)&&Same(velocity,model.Velocity),"zero-delta cast socket sample does not advance physics");
   foreach(float delta in new[]{-1f,float.NaN,float.PositiveInfinity}){Time.frameCount++;model.Pose(delta);C(Same(offset,model.Offset)&&Same(velocity,model.Velocity),"invalid delta freezes reaction");}
   for(int i=0;i<120;i++){Time.frameCount++;model.Pose(1f/60);}C(model.Offset==Vector3.zero&&model.Velocity==Vector3.zero,"reaction settles without residual joint jitter");
   for(int i=0;i<60;i++){Time.frameCount++;model.Recoil(Vector3.right,1.6f);model.Pose(1f/60);C(model.Offset.magnitude<=.22001&&model.Velocity.magnitude<=4.50001,"repeated hits stay bounded");}
   var before=model.Velocity;model.Recoil(new Vector3(float.NaN,0,0),1);model.Recoil(Vector3.forward,float.PositiveInfinity);C(Same(before,model.Velocity),"invalid contact inputs cannot poison pose");
  }
  {
   var model=new CombatModel(false);model.Control(true);for(int i=0;i<10;i++){Time.frameCount++;model.ControlPose(1f/60);}C(model.Chest.z>.15f&&model.Knee.z>.2f,"granted stun visibly bends supporting chest and knees");
   var pose=model.Chest;model.ControlPose(0);C(Same(pose,model.Chest),"paused granted control holds exact supporting pose");
   model.Control(false);for(int i=0;i<12;i++){Time.frameCount++;model.ControlPose(1f/60);}C(Same(model.Chest,Vector3.up),"stun expiry settles to original live pose");
   model.Control(true,true);for(int i=0;i<10;i++){Time.frameCount++;model.ControlPose(1f/60);}C(model.Chest.z>.05f&&model.Chest.z<.12f,"frozen status holds a rigid reduced brace");
   model.Control(true,false,true);for(int i=0;i<12;i++){Time.frameCount++;model.ControlPose(1f/60);}C(Same(model.Chest,Vector3.up),"knockdown owns pose instead of a competing stun layer");
  }
  Console.WriteLine("PASS "+checks+" actual impulse solver and hero/enemy joint reaction checks; managed TRS, not native Unity rendering.");
 }
}
'''
driver = driver.replace('model.Offset==Vector3.zero&&model.Velocity==Vector3.zero', 'model.Offset.sqrMagnitude==0&&model.Velocity.sqrMagnitude==0')
dotnet = sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
with tempfile.TemporaryDirectory(prefix='emberfall-actor-physical-motion-') as directory:
    work=Path(directory)
    (work/'Fixture.cs').write_text(fixture)
    (work/'CombatModel.ControlPose.cs').write_text((root/'Assets/Scripts/Combat/CombatModel.ControlPose.cs').read_text())
    (work/'ActorImpactSpring.cs').write_text((root/'Assets/Scripts/Combat/ActorImpactSpring.cs').read_text())
    body='\n'.join(member(s) for s in ['public void Recoil(', 'private void ApplyRecoil()', 'private void ApplyRecoil(float delta)'])
    production=work/'Motion.cs';production.write_text('using UnityEngine;namespace Emberfall{public sealed partial class CombatModel{'+body+'}}')
    (work/'Program.cs').write_text(driver)
    (work/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
    (work/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    env=dict(os.environ,DOTNET_CLI_HOME=str(work/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
    command=[dotnet,'run','--project',str(work/'Test.csproj')]
    subprocess.run(command,env=env,check=True)
    original=production.read_text()
    for label,before,after,expected in [
        ('no-hero-reaction','(isHero || articulatedEnemy)','(articulatedEnemy)','actual chest absorbs confirmed impact'),
        ('duplicate-clock','delta > 0 && recoilFrame != Time.frameCount','delta > 0','same-frame pose resample advances once'),
    ]:
        assert before in original
        production.write_text(original.replace(before,after))
        result=subprocess.run(command,env=env,capture_output=True,text=True)
        if result.returncode==0 or expected not in result.stdout+result.stderr:raise AssertionError(label+' mutation was not rejected\n'+result.stdout+result.stderr)
        print('PASS '+label+' production mutation rejected',flush=True)
        production.write_text(original)

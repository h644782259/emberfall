"""Actual four Hero factories/AnimateHero on numerical TRS; no Unity/device execution.
Reuses existing authored-rig exporter assembly. Accepted displacement is supplied by
this harness; player physics/input and optional five-clip preview remain boundaries.
"""
from pathlib import Path
import os,sys,tempfile,subprocess
root=Path(__file__).resolve().parents[1]
sdk=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
with tempfile.TemporaryDirectory(prefix='hero-motion-factory-') as tmp:
 out=Path(tmp).resolve()
 source=(root/'ArtSource/VanguardActions/export_actions.py').read_text()
 prefix=source[:source.index("subprocess.run([dotnet,'run'")]
 original_argv=sys.argv;sys.argv=['export',str(out),sdk]
 ns={'__file__':str(root/'ArtSource/VanguardActions/export_actions.py')}
 try:exec(compile(prefix,ns['__file__'],'exec'),ns)
 finally:sys.argv=original_argv
 p=out/'export';extract=ns['extract'];hero=(root/'Assets/Scripts/Combat/CombatModel.cs').read_text()
 motion=(p/'MotionExport.cs').read_text()
 for stub in ['private readonly VisualMotionEnvelope visualMotion=new VisualMotionEnvelope();','private void AdvanceVisualMotion(float dt){throw new Exception("clock must be frozen");}','private void ApplyVisualRecovery(float dt){}','private void ApplyCasterSkillPose(float t){throw new Exception("not a vanguard");}','private void AimArm(Transform a,Transform b,Vector3 c,Vector3 d){throw new Exception("not a vanguard");}']:
  assert stub in motion;motion=motion.replace(stub,'')
 motion=motion.replace('public void ExportPose(int mode)',extract(hero,'private static void AimArm(')+'public void ExportPose(int mode)')
 (p/'MotionExport.cs').write_text(motion)
 for f in ['Core/HeroMotionStyle','Core/CasterPoseRecipe','Combat/CombatModel.CastPoses','Combat/CombatModel.Recovery']:
  (p/(Path(f).name+'.cs')).write_bytes((root/('Assets/Scripts/'+f+'.cs')).read_bytes())
 f=(p/'Fixture.cs').read_text()
 f=f.replace('public struct Vector3 {','public struct Vector3 {public static Vector3 ProjectOnPlane(Vector3 a,Vector3 n)=>a-n*((a.x*n.x+a.y*n.y+a.z*n.z)/Math.Max(.000001f,n.sqrMagnitude));')
 f=f.replace('public struct Quaternion {','public struct Quaternion {public static Quaternion FromToRotation(Vector3 from,Vector3 to){var a=from.normalized;var b=to.normalized;float dot=a.x*b.x+a.y*b.y+a.z*b.z;var cross=Vector3.Cross(a,b);if(dot<-.9999f)return Euler(180,0,0);return new Quaternion{q=System.Numerics.Quaternion.Normalize(new System.Numerics.Quaternion(cross.x,cross.y,cross.z,1+dot))};}')
 f=f.replace('public class Transform:Component,IEnumerable<Transform> {','public class Transform:Component,IEnumerable<Transform> {public Vector3 eulerAngles{get{var r=rotation.q;return new Vector3(0,(float)(Math.Atan2(2*(r.W*r.Y+r.X*r.Z),1-2*(r.Y*r.Y+r.X*r.X))*180/Math.PI),0);}}')
 f=f.replace('public static class Mathf{','public static class Mathf{public static float Sqrt(float a)=>(float)Math.Sqrt(a);public static float DeltaAngle(float a,float b){float d=Repeat(b-a,360);return d>180?d-360:d;}')
 (p/'Fixture.cs').write_text(f)
 (p/'Exporter.cs').write_text(r'''
using System;using System.Linq;using UnityEngine;using Emberfall;
namespace Emberfall {public sealed partial class CombatModel {
 public static int checks;static void C(bool b,string why){checks++;if(!b)throw new Exception(why);}static bool Near(float a,float b)=>Math.Abs(a-b)<.0001f;
 string Snapshot()=>string.Join(";",GetComponentsInChildren<Transform>(true).Select(t=>t.name+":"+t.localPosition.x+","+t.localPosition.y+","+t.localPosition.z+":"+t.localRotation.q));
 void Stable(){string expected=Snapshot();float age=actionAge,phase=locomotion.Phase;for(int i=0;i<20;i++){AnimateHero(0,0,false,0);C(Snapshot()==expected,"absolute pose does not accumulate "+heroClass);C(age==actionAge&&phase==locomotion.Phase,"zero-time resampling preserves clocks");}}
 void Frame(float x,float z,float yaw){Time.frameCount++;Time.time+=.016f;transform.parent.localRotation=Quaternion.Euler(0,yaw,0);locomotion.Advance(x,z,.016f,4,true,false,0);AnimateHero(0,0,false,.016f);}
 void Anchor(){WeaponVisualAnchor a=heroClass==HeroClass.Vanguard?WeaponVisualAnchor.SwordGrip:heroClass==HeroClass.Ranger?WeaponVisualAnchor.BowGrip:WeaponVisualAnchor.StaffGrip;Vector3 actual;C(TryGetWeaponVisualAnchor(a,out actual),"real visual anchor exists");Transform rig=heroClass==HeroClass.Vanguard?swordRig:heroClass==HeroClass.Ranger?bowRig:staffRig;C((actual-rig.TransformPoint(WeaponAnchorLocal(a))).magnitude<.00001f,"actual anchor follows bound rig");C(rig.parent!=null,"weapon remains bound");}
 public void CheckMotion(){ConfigureVanguardArt();var style=HeroMotionStyle.For(heroClass);float timeStart=Time.time;
 for(int i=0;i<50;i++)Frame(0,.064f,0);Anchor();Stable();
 float lift=Math.Abs((float)Math.Sin(locomotion.Phase));C(Near(transform.localPosition.y,lift*lift*.032f*locomotion.Speed*style.Bob),"real AnimateHero uses class bob");
 C(Near(spine.localPosition.y,1.12f-style.Crouch+(float)Math.Sin(Time.time*2+phase)*.009f),"real spine crouch/breathing");
 float phaseStop=locomotion.Phase;for(int i=0;i<90;i++)Frame(0,0,0);C(Near(phaseStop,locomotion.Phase),"stop does not walk");C(locomotion.Speed<.001f,"stop settles");Stable();
 for(int i=0;i<40;i++)Frame(.064f,0,i);Stable();Anchor();
 actionBasic=true;actionSkill=0;actionDuration=1;actionAge=.2f;actionStartedFrame=Time.frameCount;float before=actionAge;AnimateHero(0,0,false,.016f);C(actionAge==before,"start frame no double action tick");
 for(int i=0;i<10;i++){before=actionAge;Frame(0,.064f,40);C(Near(actionAge,before+.016f),"moving basic preserves action clock");Anchor();}Stable();
 actionBasic=false;actionSkill=0;actionDuration=1;actionAge=.2f;for(int i=0;i<10;i++){before=actionAge;Frame(0,.064f,40);C(Near(actionAge,before+.016f),"first skill preserves action clock");Anchor();}Stable();
 actionDuration=0;isolatedPreview=true;previewTime=.37f;AnimateHero(0,0,false,0);string preview=Snapshot();Time.time+=900;AnimateHero(0,0,false,0);C(Snapshot()==preview,"preview pose ignores wall clock");
 if(heroClass==HeroClass.Summoner){var first=headRig.localRotation;previewTime=.93f;AnimateHero(0,0,false,0);C(Math.Abs(System.Numerics.Quaternion.Dot(first.q,headRig.localRotation.q))<.999999f,"real summoner head observation");}
 Stable();Anchor();C(Time.time>=timeStart,"time is harness owned");
 foreach(var t in GetComponentsInChildren<Transform>(true)){C(!float.IsNaN(t.localPosition.y),"finite factory transform");C(Math.Abs(t.localRotation.q.LengthSquared()-1)<.0003f,"numeric unit quaternion");}
 }
}}
class Exporter {static void Main(){foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass))){Time.time=1;var host=new GameObject("actual hero factory");var model=CombatModel.Hero(host.transform,hero);model.CheckMotion();UnityEngine.Object.Destroy(host);UnityEngine.Object.Flush();}Console.WriteLine("PASS "+CombatModel.checks+" actual four-class Hero/AnimateHero/locomotion/first-skill/numeric quaternion/anchor assertions. Player physics, imported five-clip preview, Unity cloth/render are unexecuted boundaries.");}}
''')
 env=dict(os.environ,DOTNET_CLI_HOME=str(out/'cli'),DOTNET_NOLOGO='1',DOTNET_GENERATE_ASPNET_CERTIFICATE='false')
 result=subprocess.run([sdk,'run','--project',str(p/'Export.csproj')],env=env,capture_output=True,text=True)
 print(result.stdout,result.stderr);assert result.returncode==0
 original=(p/'MotionExport.cs').read_text()
 for old,new,expected in [('speed * motionStyle.Bob','speed * 1f','real AnimateHero uses class bob'),('1.12f - motionStyle.Crouch','1.12f - 0f','real spine crouch/breathing'),('breathing * motionStyle.Observation','breathing * 0f','real summoner head observation')]:
  assert old in original
  (p/'MotionExport.cs').write_text(original.replace(old,new))
  built=subprocess.run([sdk,'build',str(p/'Export.csproj'),'--no-restore','-v:q'],env=env,capture_output=True,text=True)
  assert built.returncode==0,built.stdout+built.stderr
  result=subprocess.run([sdk,str(p/'bin/Debug/net8.0/Export.dll')],env=env,capture_output=True,text=True)
  assert result.returncode!=0 and 'System.Exception: '+expected in result.stdout+result.stderr,result.stdout+result.stderr
  print('PASS compiled production mutation rejected:',expected)
 (p/'MotionExport.cs').write_text(original)

// Real pose/commit/recovery methods are extracted unchanged by HeroPoseCommitTests.py.
// Transform/quaternion and locomotion inputs replace Unity, not the tested pose logic.
using System;using Emberfall;using UnityEngine;
namespace UnityEngine
{
 public struct Color {public Color(float r,float g,float b,float a=1){}}
 public class GameObject {public bool active;public void SetActive(bool v){active=v;}}
 public struct Vector3
 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 zero=>new Vector3();public static Vector3 one=>new Vector3(1,1,1);public static Vector3 up=>new Vector3(0,1,0);public static Vector3 forward=>new Vector3(0,0,1);public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*t;public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);}
 public struct Quaternion
 {internal System.Numerics.Quaternion value;public static Quaternion identity=>new Quaternion{value=System.Numerics.Quaternion.Identity};public static Quaternion Euler(float x,float y,float z)=>new Quaternion{value=System.Numerics.Quaternion.CreateFromYawPitchRoll(y*Mathf.PI/180,x*Mathf.PI/180,z*Mathf.PI/180)};public static Quaternion Euler(Vector3 v)=>Euler(v.x,v.y,v.z);public static Quaternion Slerp(Quaternion a,Quaternion b,float t)=>new Quaternion{value=System.Numerics.Quaternion.Slerp(a.value,b.value,t)};public static Quaternion operator*(Quaternion a,Quaternion b)=>new Quaternion{value=a.value*b.value};public static Vector3 operator*(Quaternion q,Vector3 p){var v=System.Numerics.Vector3.Transform(new System.Numerics.Vector3(p.x,p.y,p.z),q.value);return new Vector3(v.X,v.Y,v.Z);}public static float Difference(Quaternion a,Quaternion b)=>1-Math.Abs(System.Numerics.Quaternion.Dot(a.value,b.value));}
 public enum Space {Self}
 public class Transform
 {public Transform namedChild;public string childName;public Transform Find(string name){return name==childName?namedChild:null;}public readonly GameObject gameObject=new GameObject();public Transform parent;public Vector3 localPosition,localScale,eulerAngles;public Quaternion localRotation=Quaternion.identity;public Quaternion rotation{get=>localRotation;set=>localRotation=value;}public Vector3 TransformPoint(Vector3 p)=>localPosition+localRotation*p;public Vector3 InverseTransformPoint(Vector3 p)=>p-localPosition;public void Rotate(float x,float y,float z,Space s){localRotation*=Quaternion.Euler(x,y,z);}}
 public class LineRenderer {public void SetPosition(int i,Vector3 p){}}
 public static class Time {public static float deltaTime=.016f,time;public static int frameCount;}
 public static class Mathf
 {public const float PI=(float)Math.PI;public static float Clamp01(float a)=>Math.Max(0,Math.Min(1,a));public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static float Abs(float a)=>Math.Abs(a);public static float Sin(float a)=>(float)Math.Sin(a);public static float Pow(float a,float b)=>(float)Math.Pow(a,b);public static float SmoothStep(float a,float b,float t){t=Clamp01(t);return a+(b-a)*t*t*(3-2*t);}public static float DeltaAngle(float a,float b)=>b-a;}
}
namespace Emberfall
{
 public class PoseLocomotion {public float Speed,Phase,Side,Forward;}
 public class ClothStub {public void SetMotion(float a,float b){}public void SamplePreview(float time,float action){}}
 public enum WeaponVisualAnchor {BowGrip,BowNock}
 public sealed partial class CombatModel
 {
  private void ApplyRecoil(float delta){} // Physical layer tested independently by ActorImpactMotionProductionTests.
  // Optional imported visual boundary: disabled, always returns to procedural poses.
  // It records arguments only; no Animator, asset importer or render behavior is simulated.
  private bool pilotCharging;
  // Explicit optional authored-motion boundary. This legacy suite owns original pose
  // timing/handoff only; real binary loading and authored offsets are covered by
  // VanguardActionsProductionTests and the actual factory/AnimateHero pose export.
  private void ApplyAuthoredVanguardPose(bool acting,float progress,bool hurt) {}
  private object blenderPilot;
  private void ConfigureBlenderPilot() { blenderPilot=null; }

  // This fixture executes procedural poses only; imported rig sampling has its own suite.
  private void SetBlenderPilotVisible(bool visible){if(visible)throw new Exception("Procedural pose fixture cannot enable imported visual");}
  public bool PilotCharging=>pilotCharging;public void PretendPilotCharge(){pilotCharging=true;}
  public int PilotSamples;public bool PilotActing,PilotHurt;public float PilotProgress;
  private bool SampleBlenderPilot(bool acting,float progress,bool hurt)
  {PilotSamples++;PilotActing=acting;PilotProgress=progress;PilotHurt=hurt;return false;}
  private bool isHero=true,actionBasic,isolatedPreview;private float previewTime;private HeroClass heroClass;private int actionSkill,swingCount,actionStartedFrame,weaponActionId,lastRibbonAction;private float actionAge,actionDuration,gaitPhase,smoothedSpeed,phase;
  private readonly Transform spine=new Transform(),headRig=new Transform(),leftArm=new Transform(),rightArm=new Transform(),leftElbow=new Transform(),rightElbow=new Transform(),swordRig=new Transform(),staffRig=new Transform(),bowRig=new Transform(),cloak=new Transform(),pelvis=new Transform(),leftLeg=new Transform(),rightLeg=new Transform(),leftKnee=new Transform(),rightKnee=new Transform(),castingOrb=new Transform(),arrowRig=new Transform(),decoration=new Transform();
  private Transform fashionWings;
  private readonly Transform transform=new Transform();private readonly LineRenderer bowstring=new LineRenderer();private readonly PoseLocomotion locomotion=new PoseLocomotion();private ClothStub tailoredCloth;
  private int WeaponSwingSide=>swingCount%2==0?1:-1;private Vector3 WeaponAnchorLocal(WeaponVisualAnchor a)=>Vector3.zero;
  private void ApplyHeroLocomotion(){}private void ApplyHeroJumpPose(bool casting){}private void AimArm(Transform a,Transform b,Vector3 p,Vector3 q){}
  public CombatModel(HeroClass hero){heroClass=hero;}
  public float Age=>actionAge;public float Duration=>actionDuration;public int Identity=>weaponActionId;public Quaternion Arm=>rightArm.localRotation;public Quaternion Weapon=>heroClass==HeroClass.Vanguard?swordRig.localRotation:staffRig.localRotation;
  public void Frame(float dt)=>AnimateHero(0,1,false,dt);public void PoseAgain()=>CommitActionPose();public void PretendCharge(){rightArm.localRotation=Quaternion.Euler(-145,80,30);staffRig.localRotation=Quaternion.Euler(88,60,10);}
 }
}
public static class HeroPoseCommitTests
{
 public static bool RequirePilotSample;
 static int n;static void Check(bool yes,string text){n++;if(!yes)throw new Exception(text);}
 static bool Same(Quaternion a,Quaternion b)=>Quaternion.Difference(a,b)<.00001f;
 public static string Run()
 {
  foreach(var hero in new[]{HeroClass.Vanguard,HeroClass.Arcanist,HeroClass.Summoner})
  {
   Time.frameCount=20;var model=new CombatModel(hero);model.PlayAction(1,false);var desired=model.Arm;var weapon=model.Weapon;float age=model.Age;
   Check(model.TryClaimSwordRibbon(model.Identity)==(hero==HeroClass.Vanguard)&&!model.TryClaimSwordRibbon(model.Identity),"production swing identity claims once and rejects non-sword heroes");
   Check(!Same(desired,Quaternion.identity),"committed action writes its release arm before any later Update");
   model.Frame(.016f);Check(Same(desired,model.Arm)&&model.Age==age,"first-frame render keeps committed attack phase");
   for(int i=0;i<4;i++)model.PoseAgain();Check(model.Age==age&&Same(weapon,model.Weapon),"pose resampling cannot advance action clock or weapon phase");
   Time.frameCount++;model.Frame(.02f);var old=model.Arm;model.PretendCharge();model.ReleaseCharge(1);
   // Both use the same release recipe; seed the reference with identical swing parity.
   var reference=new CombatModel(hero);reference.PlayAction(1,false);reference.ReleaseCharge(1);
   Check(Same(model.Arm,reference.Arm)&&Same(model.Weapon,reference.Weapon),"LateUpdate charge release replaces old charge arm and weapon immediately");
   var fresh=new CombatModel(hero);fresh.PlayAction(0,false);fresh.PlayAction(1,false);
   var chained=new CombatModel(hero);chained.PlayAction(0,false);Time.frameCount++;chained.Frame(.12f);chained.PlayAction(1,false);
   Check(Same(fresh.Arm,chained.Arm)&&Same(fresh.Weapon,chained.Weapon),"attack handoff never drags force arm or weapon toward the previous action");
   var cancel=chained.Arm;int identity=chained.Identity;chained.CancelAction();chained.Frame(0);
   Check(!chained.TryClaimSwordRibbon(chained.Identity),"cancelled action cannot claim another sword ribbon");
   Check(chained.Identity!=identity&&Same(chained.Arm,cancel),"cancel retires attack identity but starts bounded visual settling");
   chained.Frame(.13f);Check(!Same(chained.Arm,cancel),"cancel settling reaches idle without extending combat recovery");
  }
  if(RequirePilotSample)
  {
   foreach(float interval in new[]{.18f,.46f,.9f})
   {
    var model=new CombatModel(HeroClass.Vanguard);model.PretendPilotCharge();model.PlayAction(-1,true,interval);
    Check(!model.PilotCharging,"committed action clears optional visual charge flag");
    Check(model.PilotSamples==1&&model.PilotActing&&!model.PilotHurt&&Math.Abs(model.PilotProgress-.52f)<.00001f,"pilot receives contact phase synchronously at basic commit");
    float age=model.Age;int samples=model.PilotSamples;model.PoseAgain();
    Check(model.PilotSamples==samples+1&&model.Age==age&&Math.Abs(model.PilotProgress-.52f)<.00001f,"optional pilot resampling preserves immediate contact and action clock");
   }
  }
  var castArms=new Quaternion[4];var castWeapons=new Quaternion[4];int[] skills={0,1,5,2};
  for(int i=0;i<skills.Length;i++)
  {
   var model=new CombatModel(HeroClass.Summoner);model.PlayAction(skills[i],false);castArms[i]=model.Arm;castWeapons[i]=model.Weapon;
   Check(model.Duration==SkillDamageBudgets.SkillPoseDuration(HeroClass.Summoner,skills[i],false),"pose family preserves actual shared nominal duration");
   Check(Math.Abs(model.Age/model.Duration-SkillDamageBudgets.SkillPoseStart(HeroClass.Summoner,skills[i],false))<.00001f,"pose family commits at unchanged gameplay release phase");
  }
  for(int i=0;i<4;i++)for(int j=i+1;j<4;j++)Check(!Same(castArms[i],castArms[j])&&!Same(castWeapons[i],castWeapons[j]),"direction/ground/guard/contract alter both real arm and staff pose");
  Check(CombatModel.SkillReleaseGesture(HeroClass.Arcanist,4)==CombatModel.ReleaseGesture.ChainPoint&&
   CombatModel.SkillReleaseGesture(HeroClass.Arcanist,1)==CombatModel.ReleaseGesture.MeteorThrow&&
   CombatModel.SkillReleaseGesture(HeroClass.Arcanist,5)==CombatModel.ReleaseGesture.FireStride,"elementalist maps chain/meteor/stride to actual release gestures");
  var summonGestures=new[]{CombatModel.ReleaseGesture.WolfCall,CombatModel.ReleaseGesture.TurretCall,CombatModel.ReleaseGesture.TreantCall};
  int summonIndex=0;foreach(int skill in new[]{2,4,9})Check(CombatModel.SkillReleaseGesture(HeroClass.Summoner,skill)==summonGestures[summonIndex++],"wolf/turret/treant use their actual summoning gestures");
  return "PASS: "+n+" production hero commit/pose/recovery checks (managed transforms, not rendered frames)";
 }
}

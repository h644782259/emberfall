// Imported-adapter tests explicitly authorize preview construction; subsequent clock tests exercise its sampler in isolation.
using System;using System.Linq;using UnityEngine;using Emberfall;
namespace UnityEngine{
 public struct Vector2{public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public float magnitude=>(float)Math.Sqrt(x*x+y*y);}
 public enum KeyCode{J}
 public static class Input{public static Vector2 mousePosition;public static bool GetMouseButton(int n)=>true;public static bool GetKey(KeyCode k)=>false;public static bool GetKeyDown(KeyCode k)=>false;}
 public class Camera{public static Camera main;}
}
namespace Emberfall{
 public class GameProfile{public int[] hotbarKeys;}
 public static class GameBalance{public const int HotbarSize=4,HotbarPotion=-3,SkillCount=10;public static int[] DefaultHotbarKeys=new int[0];}
 public static class MobileControls{public static bool Active=false,AttackHeld=true;}
 public class GameUI{public int MobileKeyboardSkill(int key)=>-1;}
 public partial class GameSession{public float ArenaRadius=100;public bool PointerOverUI;public T GetComponent<T>()where T:class=>null;public void UseHotbarConsumable(){throw new Exception("unexpected hotbar");}}
 public class EnemyController:MonoBehaviour{}
 public sealed partial class CombatModel{
  private bool articulatedEnemy;private float enemyStepPhase,enemyStepWeight,controlPoseWeight;private int recoilFrame,controlPoseFrame;private Vector3 recoilOffset,recoilVelocity;
  private class RecoveryBoundary{public void Reset(){}}private RecoveryBoundary visualMotion=new RecoveryBoundary();private bool visualYawReady;private int visualMotionFrame;
  public void Animate(float speed,float attack,bool hurt)=>Tick(Time.deltaTime,hurt);
  public void ForgetWalking()=>ResetLocomotion();
 }
 public partial class PlayerController{
  private float platformFallSpeed;private bool HasPendingSkillRelease=>false;private bool SkillReleaseInputBlocked=>false;private void CancelPendingSkillRelease(){}private void AdvanceSkillRelease(float dt){}private EnemyController ResolveDesktopBasicTarget()=>null;
  public Vector3 Aim;public float attackCooldown,mobilityTime,attackAnimation,jumpAge;private bool suppressBasicUntilReleased;public bool TraversalStartedThisFrame;private Vector3 aimPoint;private float MovementMultiplier=>1;
  private bool ValidAimTarget(EnemyController e)=>e!=null;private int HotbarSkill(int slot)=>-1;
  private Vector3 ResolveMobileAim(Vector3 movement)=>Aim;private Vector3 ResolveAim(Camera c,Vector2 screen)=>Aim;
  private bool MobilePinnedActionAllowed(int skill,bool basic)=>true;private EnemyController MagicConeTarget()=>null;
  public void Setup(){model.ForgetWalking();}
 }
}
static class PilotFacingFixture{
 static int n;static void C(bool value,string label){n++;if(!value)throw new Exception(label);}
 static PlayerController Create(){
  var asset=new GameObject("asset");asset.AddComponent<Renderer>();asset.AddComponent<MeshFilter>().sharedMesh=new Mesh();LayerFixture.Build(asset);
  Resources.Items["BlenderPilot/Vanguard"]=asset;Resources.Items["BlenderPilot/Pilot_Atlas_Standard"]=new Material();Resources.Clips=new[]{"Pilot_Idle","Pilot_Move","Pilot_Basic","Pilot_Hit","Pilot_Skill"}.Select(s=>new AnimationClip{name=s,length=2}).ToArray();BlenderPilotArt.Enabled=true;
  var root=new GameObject("player");var player=root.AddComponent<PlayerController>();var model=new GameObject("model");model.transform.SetParent(root.transform);player.model=model.AddComponent<CombatModel>();player.model.heroClass=HeroClass.Vanguard;player.model.isolatedPreview=true;player.model.Init();player.model.isolatedPreview=false;player.Setup();return player;
 }
 static void Frame(PlayerController p,Vector3 walk,Vector3 aim){Time.frameCount++;Time.deltaTime=.02f;Time.time+=.02f;p.attackCooldown=0;p.Aim=p.transform.position+walk*.12f+aim*10;p.FacingFrame(walk,.02f);}
 public static void Run(){
  foreach(float angle in new[]{90f,180f,-90f}){
   var p=Create();Vector3 forward=new Vector3(0,0,1);for(int i=0;i<20;i++)Frame(p,forward,forward);
   C(p.model.Visible,"steady forward pilot supported");float speed=p.model.locomotion.Speed;int advances=p.model.locomotion.Advances;float expectedPhase=p.model.locomotion.Phase;
   Vector3 turned=Quaternion.Euler(0,angle,0)*forward;
   for(int i=0;i<4;i++){
    Frame(p,forward,turned);C(!p.model.Visible,"current-facing moving basic rejects turn "+angle+" frame "+i);
    expectedPhase=(expectedPhase+.12f*2.6f)%(2*(float)Math.PI);C(Math.Abs(p.model.locomotion.Phase-expectedPhase)<.00002f,"raw facing guard preserves accepted gait phase");
    C(p.model.locomotion.Advances==advances+i+1,"raw facing guard never advances locomotion twice");
    C(p.model.actionAge==p.model.actionDuration*.52f,"turned fallback preserves synchronous contact");
   }
   C(p.model.locomotion.Speed>=speed,"raw facing does not reset smoothed speed");
   for(int i=0;i<40;i++)Frame(p,forward,forward);C(p.model.Visible,"turning back reenters only when existing smoothed gates also permit");
  }
  var ownerBasis=Create();ownerBasis.model.transform.localRotation=Quaternion.Euler(30,65,-20);Frame(ownerBasis,new Vector3(0,0,1),new Vector3(0,0,1));C(ownerBasis.model.Visible,"raw walking basis is Player owner not procedural model pose");
  var stop=Create();var z=new Vector3(0,0,1);for(int i=0;i<20;i++)Frame(stop,z,z);
  for(int i=0;i<4;i++){Frame(stop,Vector3.zero,new Vector3(1,0,0));C(stop.model.Visible&&stop.model.locomotion.Speed>.05f,"legal zero accepted walking preserves decaying gait basic");}
  stop.model.SetLocomotion(Vector3.zero,0,6,true);stop.model.PlayAction(-1,true,.46f);C(stop.model.Visible,"paused legal zero does not invalidate decaying gait basis");
  // Collision accepts zero despite nonzero requested movement; guard uses accepted displacement.
  WorldTraversal.Accepted=Vector3.zero;Frame(stop,z,new Vector3(-1,0,0));C(stop.model.Visible,"blocked actual movement clears previous raw direction");WorldTraversal.Accepted=null;
  var unknown=Create();unknown.model.locomotion.Speed=1;unknown.model.PlayAction(-1,true,.46f);C(!unknown.model.Visible,"unknown walking basis fails closed");
  for(int i=0;i<20;i++)Frame(unknown,z,z);
  unknown.model.SetLocomotion(new Vector3(float.NaN,0,1),.02f,6,true);unknown.model.PlayAction(-1,true,.46f);C(!unknown.model.Visible,"invalid raw displacement fails closed");
  Console.WriteLine("PASS: "+n+" actual Update movement/aim tail + FaceAim + BasicAttack visual-commit facing assertions; damage dispatch and Unity frame loop excluded");
 }
}

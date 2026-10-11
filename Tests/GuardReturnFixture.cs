using System;using Emberfall;using UnityEngine;
namespace UnityEngine
{
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public float sqrMagnitude=>x*x+y*y+z*z;public float magnitude=>(float)Math.Sqrt(sqrMagnitude);public Vector3 normalized=>magnitude>.00001f?this*(1/magnitude):new Vector3();public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);}
 public struct Quaternion {public static Quaternion LookRotation(Vector3 v)=>new Quaternion();public static Quaternion Slerp(Quaternion a,Quaternion b,float t)=>new Quaternion();}
 public class Transform {public Vector3 position;public Quaternion rotation;}
 public static class Mathf {public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);}
 public static class Time {public static float deltaTime=.1f;}
}
namespace Emberfall
{
 public static class CombatFx {public static Vector3 Flat(Vector3 p)=>p;}
 public static class WorldTraversal {public static bool Blocked;public static bool HasLineOfSight(Vector3 a,Vector3 b)=>!Blocked;public static Vector3 Move(Vector3 p,Vector3 v,float r)=>Blocked?p:p+v;}
 public class Route {public Vector3 Direction(Vector3 a,Vector3 b,float r)=>(b-a).normalized;}
 public sealed partial class EnemyController
 {
  private EnemyController mobileSupplier,mobileFirst,mobileSecond;public bool IsDead,isActiveAndEnabled=true;private float attackAnimation;
  private EscapePostPolicy escapePost=new EscapePostPolicy(EscapeRole.GateGuard);private float escapeChaseMovement,chargeTime,hurtTime;private bool preparing;private object companionTarget;
  private readonly Transform transform=new Transform();private Vector3 escapePostPosition,walkingDisplacement;private Route route=new Route();private float NavigationRadius=>.6f;
  public int Cancels,ReleaseCount;public bool Preparing {get=>preparing;set=>preparing=value;}public Vector3 Position {get=>transform.position;set=>transform.position=value;}
  private void CancelAttack(){Cancels++;preparing=false;}private void AnimateModel(float a,float b,bool c){}private void ClampPosition(){}
  public bool Return(Vector3 target)=>ReturnToEscapePost(Time.deltaTime,2,target);
  public void Walk(Vector3 displacement){transform.position=WalkForAnimation(displacement);}
 }
}
public static class GuardReturnTests
{
 static int n;static void Check(bool yes,string text){n++;if(!yes)throw new Exception(text);}
 public static string Run()
 {
  var enemy=new EnemyController{Position=new Vector3(1,0,0)};var target=new Vector3(4,0,0);
  for(int i=0;i<30;i++){Check(!enemy.Return(target),"guard can pursue within the initial movement budget");enemy.Walk(new Vector3(.02f,0,0));}
  enemy.Preparing=true;
  for(int i=0;i<40;i++)Check(!enemy.Return(target)&&enemy.Preparing&&enemy.Cancels==0,"three-second chase then stationary telegraph must complete without post cancellation");
  enemy.Preparing=false;enemy.ReleaseCount++;
  for(int i=0;i<5;i++){Check(!enemy.Return(target),"stationary windup did not consume chase seconds");enemy.Walk(new Vector3(.02f,0,0));}
  Check(enemy.Return(target)&&enemy.ReleaseCount==1,"spent movement budget returns before another attack can start");
  var tether=new EnemyController{Position=new Vector3(6,0,0),Preparing=true};
  Check(!tether.Return(target)&&tether.Preparing,"crossing tether during a committed warning only queues return");tether.Preparing=false;tether.ReleaseCount++;
  Check(tether.Return(target)&&tether.ReleaseCount==1,"queued tether return runs immediately after release");
  var blocked=new EnemyController{Position=new Vector3(1,0,0)};WorldTraversal.Blocked=true;
  for(int i=0;i<50;i++){Check(!blocked.Return(target),"blocked movement is not chase time");blocked.Walk(new Vector3(.1f,0,0));}WorldTraversal.Blocked=false;
  var policy=new EscapePostPolicy(EscapeRole.GateGuard);Check(!policy.ReturnToPost(4,2,4,0,true),"long attack-only frame does not consume movement budget");Check(!policy.ReturnToPost(.1f,2,4,0,false),"attack-only interval leaves movement budget available");
  foreach(var role in new[]{EscapeRole.None,EscapeRole.Pursuer})Check(!new EscapePostPolicy(role).ReturnToPost(20,15,20,20,false),"non-post roles remain unrestricted");
  return "PASS: "+n+" production guard movement-budget and committed-warning checks";
 }
}

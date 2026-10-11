// Production companion part builder + detached retirement component. Transform/material
// doubles expose hierarchy/ownership only; this is not Unity mesh or animation rendering.
using System;using System.Collections.Generic;using System.Reflection;using Emberfall;using UnityEngine;
namespace UnityEngine {
 public class Object {public bool destroyed;public static void Destroy(Object o){o.destroyed=true;}}
 public class Component:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;}
 public class MonoBehaviour:Component {}
 public class GameObject:Object {public bool activeSelf=true;public Transform transform;public List<Component> components=new List<Component>();public GameObject(string name){transform=new Transform{gameObject=this};}public T AddComponent<T>() where T:Component,new(){var c=new T{gameObject=this};components.Add(c);return c;}public T Get<T>()where T:Component=>(T)components.Find(c=>c is T);public void SetActive(bool value){activeSelf=value;foreach(var c in components)Call(c,value?"OnEnable":"OnDisable");}public static void Call(object c,string m){c.GetType().GetMethod(m,BindingFlags.Instance|BindingFlags.NonPublic)?.Invoke(c,null);}}
 public class Transform:Component {public Transform parent;public Vector3 localScale=Vector3.one,localPosition,position;public Quaternion localRotation,rotation;public void SetParent(Transform t,bool world){parent=t;}}
 public struct Vector3 {public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static Vector3 one=>new Vector3(1,1,1);public static Vector3 zero=>new Vector3();public static Vector3 up=>new Vector3(0,1,0);public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);}
 public struct Quaternion {internal System.Numerics.Quaternion q;public static Quaternion Euler(float a,float b,float c)=>new Quaternion{q=System.Numerics.Quaternion.CreateFromYawPitchRoll(b*(float)Math.PI/180,a*(float)Math.PI/180,c*(float)Math.PI/180)};public static Quaternion Inverse(Quaternion a)=>new Quaternion{q=System.Numerics.Quaternion.Inverse(a.q)};public static Quaternion operator *(Quaternion a,Quaternion b)=>new Quaternion{q=a.q*b.q};public static Vector3 operator *(Quaternion a,Vector3 b){var v=System.Numerics.Vector3.Transform(new System.Numerics.Vector3(b.x,b.y,b.z),a.q);return new Vector3(v.X,v.Y,v.Z);}}
 public struct Color {public Color(float a,float b,float c){}}
 public enum PrimitiveType {Cube,Sphere,Capsule}
 public static class Mathf {public const float PI=(float)Math.PI;public static int Clamp(int a,int b,int c)=>Math.Max(b,Math.Min(c,a));public static float Clamp01(float a)=>Math.Max(0,Math.Min(1,a));public static int Max(int a,int b)=>Math.Max(a,b);public static float Sin(float a)=>(float)Math.Sin(a);}
 public static class Time {public static float deltaTime=.1f,time;}
 public enum RuntimeInitializeLoadType {SubsystemRegistration}public class RuntimeInitializeOnLoadMethodAttribute:Attribute {public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType value){}}
}
namespace Emberfall {
 public enum VisualSurface {Metal,Crystal,Wood}
 public class PlayerController:MonoBehaviour {public int CombatEpoch;public bool IsDead;}
 public class GameSession:MonoBehaviour {public PlayerController Player;public bool HasStarted=true,InputBlocked;}
 public sealed partial class CombatModel:MonoBehaviour {
  public Transform body=new GameObject("body").transform,decoration,wolfJaw,leftArm,rightArm;public bool quadruped,treantCompanion,floating;
  public void ReplayPose(){ApplyCompanionPose();}public Transform Rigid=>companionRigid;
  public readonly List<Transform> Parts=new List<Transform>();public bool BeganDeath;public float Opacity=1;
  private Transform Joint(string name,Vector3 at){var t=new GameObject(name).transform;t.SetParent(transform,false);return t;}
  private Transform Part(string name,PrimitiveType shape,Vector3 at,Vector3 size,Color color,Transform parent,VisualSurface surface){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=at;t.localScale=size;Parts.Add(t);return t;}
  public void BeginDeath(){BeganDeath=true;}public void SetDeathOpacity(float a){Opacity=a;}
 }
 public sealed partial class SummonedCompanion:MonoBehaviour {public enum Kind {Wolf,Spirit,Treant}internal static readonly List<SummonedCompanion> active=new List<SummonedCompanion>();public CombatModel model;public PlayerController Owner;public GameSession session;}
}
public static class CompanionAppearanceTests {
 static int count;static void Check(bool b,string m){count++;if(!b)throw new Exception(m);}
 static CombatModel Model(){var m=new GameObject("model").AddComponent<CombatModel>();m.body.localRotation=Quaternion.Euler(0,0,0);m.body.localPosition=new Vector3(0,1.2f,0);return m;}
 public static void Main(){
 foreach(SummonedCompanion.Kind form in Enum.GetValues(typeof(SummonedCompanion.Kind)))for(int rank=0;rank<=3;rank++)foreach(bool permanent in new[]{false,true}){
 var m=Model();m.SetCompanionAppearance(form,rank,permanent);int expected=rank*2+(permanent?(form==SummonedCompanion.Kind.Treant?1:2):0);Check(m.Parts.Count==expected,"actual builder has structural rank steps and permanent distinction");m.SetCompanionAppearance(form,rank,permanent);Check(m.Parts.Count==expected,"same appearance creates no new parts");foreach(var p in m.Parts)Check(p.localScale.x>0&&p.localScale.y>0&&Math.Abs(p.localPosition.x)<1&&p.localPosition.y<2.1,"finite local ornament envelope");var old=expected>0?m.Parts[0].parent:null;m.SetCompanionAppearance(form,rank,!permanent);if(old!=null)Check(!old.gameObject.activeSelf&&old.gameObject.destroyed,"swap immediately retires old structural group");m.SetCompanionRecall(.5f);m.SetCompanionRecall(0);}
 var posed=Model();posed.SetCompanionAppearance(SummonedCompanion.Kind.Spirit,3,true);posed.body.localRotation=Quaternion.Euler(30,0,0);posed.body.localScale=new Vector3(.5f,2,.5f);posed.ReplayPose();
 var pivot=posed.Rigid.localPosition+posed.Rigid.localRotation*new Vector3(0,1.2f,0);Check(Math.Abs(pivot.y-1.2f)<.001f&&Math.Abs(posed.Rigid.localPosition.z)>.1f,"rigid attachments follow body rotation around its authored pivot");Check(posed.Rigid.localScale.y==1,"soft body squash never scales hardware");int parts=posed.Parts.Count;for(int i=0;i<1000;i++)posed.ReplayPose();Check(posed.Parts.Count==parts,"pose does not allocate new parts");
 var ready=Model();ready.quadruped=true;ready.wolfJaw=new GameObject("jaw").transform;ready.SetCompanionAppearance(SummonedCompanion.Kind.Wolf,1,true);ready.SetCompanionAttackPreparation(.5f);ready.ReplayPose();Check(ready.transform.localPosition.y<0,"actual preparation lowers visual wolf without navigation body movement");ready.body.localRotation=Quaternion.Euler(0,0,0);ready.transform.localPosition=Vector3.zero;ready.SetCompanionAttackPreparation(0);ready.ReplayPose();Check(ready.transform.localPosition.y==0,"cancelled preparation clears additive crouch next authored pose");
 var owner=new GameObject("owner").AddComponent<PlayerController>();var session=new GameObject("session").AddComponent<GameSession>();session.Player=owner;
 var host=new GameObject("host").AddComponent<SummonedCompanion>();host.Owner=owner;host.session=session;host.model=Model();host.model.transform.SetParent(host.transform,false);SummonedCompanion.active.Add(host);
 typeof(SummonedCompanion).GetMethod("Dismiss",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(host,new object[]{CompanionRetirementReason.Expired});Check(!SummonedCompanion.active.Contains(host)&&!host.gameObject.activeSelf&&host.gameObject.destroyed,"actual Dismiss releases active actor immediately");Check(host.model.transform.parent==null&&host.model.BeganDeath,"only pure model survives independently");var visual=host.model.gameObject.Get<CompanionRetirementVisual>();session.InputBlocked=true;GameObject.Call(visual,"Update");Check(host.model.Opacity==1,"paused visual does not advance");session.InputBlocked=false;for(int i=0;i<6;i++)GameObject.Call(visual,"Update");Check(host.model.gameObject.destroyed,"owned visual bounded duration ends");
 var defeat=CompanionRetirementRules.Pose(CompanionRetirementReason.Defeated,.5f);var expire=CompanionRetirementRules.Pose(CompanionRetirementReason.Expired,.5f);Check(defeat.Height<0&&defeat.Roll>0&&expire.Height>0&&expire.Roll==0,"expiry rises while defeat collapses");
 var models=new List<CombatModel>();for(int i=0;i<8;i++){var m=Model();Check(CompanionRetirementVisual.Detach(m,owner,session,CompanionRetirementReason.Replaced),"budget accepts first eight");models.Add(m);}Check(!CompanionRetirementVisual.Detach(Model(),owner,session,CompanionRetirementReason.Replaced),"retirement visuals capped");owner.CombatEpoch++;foreach(var m in models){GameObject.Call(m.gameObject.Get<CompanionRetirementVisual>(),"Update");Check(m.gameObject.destroyed,"epoch change clears detached model");}Check(!CompanionRetirementVisual.Detach(Model(),owner,session,CompanionRetirementReason.ContextEnded),"context teardown leaves no visual orphan");
 Console.WriteLine("PASS "+count+" production companion structures/retirement lifecycle assertions (Unity API doubles)");
 }
}

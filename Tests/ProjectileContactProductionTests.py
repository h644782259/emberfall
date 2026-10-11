#!/usr/bin/env python3
"""Execute real pet/prop entry methods and real projectile contact-selection blocks.
Numeric Unity/PhysX doubles; no rendering or device claim.
"""
import os,sys,subprocess,tempfile
from pathlib import Path
r=Path(__file__).resolve().parents[1]
def member(s,key):
 a=s.index(key);b=s.index('{',a)+1;n=1
 while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
math=(r/'Tests/DestructibleTraversalTests.cs').read_text();math='using System;using UnityEngine;'+math[math.index('namespace Emberfall'):]
prop=(r/'Assets/Scripts/World/DestructibleProp.cs').read_text();pet=(r/'Assets/Scripts/Combat/SummonedCompanion.cs').read_text();projectile=(r/'Assets/Scripts/Combat/CombatEffects.cs').read_text()
update=member(projectile[projectile.index('    internal sealed class CombatProjectile'):],'        private void Update()')
terrain=update[update.index('            Vector3 plannedPosition=nextPosition;'):update.index('            if (hostile)')]
# The body's flight alignment is separately exercised by HeroBodyFlightProductionTests.
terrain=terrain[:terrain.index('            if(bodyHeightFlight)')]
selection=update[update.index('                DestructibleProp hitProp;float propFraction;'):update.index('                sweptHits.Clear();')]
fixture=r"""
namespace UnityEngine{
 public class Object{public bool Destroyed;public static void Destroy(Object o){if(o!=null)o.Destroyed=true;}}
 public class MonoBehaviour:Object{public GameObject gameObject=new GameObject();public Transform transform=>gameObject.transform;}
 public class GameObject:Object{public bool activeInHierarchy=true;public Transform transform;public GameObject(){transform=new Transform();}}
 public class Transform{public Vector3 position;public Renderer[] Renderers;public T[] GetComponentsInChildren<T>()where T:class=>Renderers as T[];}
 public class Renderer{public bool enabled=true;public Bounds bounds;}
 public struct Bounds{public Vector3 center,size;public Bounds(Vector3 c,Vector3 s){center=c;size=s;}public Vector3 min=>center-size*.5f;public Vector3 max=>center+size*.5f;public void Encapsulate(Bounds b){var lo=new Vector3(Math.Min(min.x,b.min.x),Math.Min(min.y,b.min.y),Math.Min(min.z,b.min.z));var hi=new Vector3(Math.Max(max.x,b.max.x),Math.Max(max.y,b.max.y),Math.Max(max.z,b.max.z));center=(lo+hi)*.5f;size=hi-lo;}}
 public struct Color{public Color(float r,float g,float b,float a=1){}}
 public class Collider{public T GetComponentInParent<T>()where T:class=>null;}public struct RaycastHit{public Collider collider;public float distance;public Vector3 normal;}public enum QueryTriggerInteraction{Ignore}public static class Physics{public const int DefaultRaycastLayers=-1;public static int SphereCastNonAlloc(Vector3 p,float r,Vector3 v,RaycastHit[] a,float l,int mask,QueryTriggerInteraction t)=>0;}
}
namespace Emberfall{
 public enum EnemyKind{Guardian,Slime,Wisp}
 public class GameSession{public static GameSession Instance;public PlayerController Player;public bool PracticeActive,InputBlocked,HasStarted=true;}
 public class PlayerController:MonoBehaviour{public bool IsDead;}
 public class EnemyController{}
 public static class PhysicalSkillVfx{public static Vector3 Contact;public static void Impact(Vector3 p,Vector3 d,Color c,float s){Contact=p;}}
 public class SummonedCompanion:MonoBehaviour{
  static readonly List<SummonedCompanion> active=new List<SummonedCompanion>();public enum Kind{Wolf,Spirit,Wisp,Treant}public Kind Form;public bool IsAlive=true;public float Damage;
  public static void Reset(){active.Clear();}public static SummonedCompanion Add(Vector3 at,Kind kind){var p=new SummonedCompanion{Form=kind};p.transform.position=at;active.Add(p);return p;}public void TakeDamage(float d){Damage+=d;}
  PET
 }
 public class DestructibleProp:MonoBehaviour{
  private static readonly List<DestructibleProp> props=new List<DestructibleProp>();private bool initialized,blocksPath;public bool Broken;private float radius;private Transform visual;private Renderer[] projectileSurfaces;private WorldTraversal.ObstacleHandle obstacle;
  public static void Reset(){props.Clear();}public static DestructibleProp Add(Vector3 at,float rad,float height,bool blocking){var p=new DestructibleProp{initialized=true,blocksPath=blocking,radius=rad};p.transform.position=at;p.visual=new Transform{Renderers=new[]{new Renderer{bounds=new Bounds{center=at+Vector3.up*(height*.5f),size=new Vector3(rad*2,height,rad*2)}}}};props.Add(p);p.RegisterObstacle();return p;}
  PROPS
 }
 public class ContactSelector:MonoBehaviour{
  private PlayerController owner;private bool hostile;private float radius;
  public DestructibleProp Selected;public Vector3 End;
  public ContactSelector(PlayerController p,float size){owner=p;radius=size;}
  public void Sweep(Vector3 previous,Vector3 nextPosition){TERRAIN SELECTION Selected=hitProp;End=transform.position;}
 }
}
class Program{
 static int n;static void C(bool value,string name){n++;if(!value)throw new Exception(name);}
 static void Main(){
  WorldTraversal.Reset(ZoneKind.Dungeon);var owner=new PlayerController();GameSession.Instance=new GameSession{Player=owner};var a=new Vector3(-5,1,0);var b=new Vector3(5,1,0);float playerFraction;PhysicalSkillMotion.BodyContact(a,b,Vector3.zero,1.8f,.46f,.32f,out playerFraction);
  SummonedCompanion.Reset();var pet=SummonedCompanion.Add(new Vector3(.05f,0,0),SummonedCompanion.Kind.Treant);C(SummonedCompanion.HitHostileProjectile(a,b,8,playerFraction,.32f)&&pet.Damage==8,"pet's earlier body surface intercepts before closer player center");C(PhysicalSkillVfx.Contact.x<-.77f,"intercept debris uses actual surface point");
  SummonedCompanion.Reset();var high=SummonedCompanion.Add(new Vector3(-1,3,0),SummonedCompanion.Kind.Wolf);C(!SummonedCompanion.HitHostileProjectile(a,b,8,playerFraction,.32f)&&high.Damage==0,"upper pet cannot intercept lower flight");
  SummonedCompanion.Reset();var behind=SummonedCompanion.Add(new Vector3(2,0,0),SummonedCompanion.Kind.Wolf);C(!SummonedCompanion.HitHostileProjectile(a,b,8,playerFraction,.32f)&&behind.Damage==0,"pet behind player's entry cannot intercept");
  DestructibleProp.Reset();var shortProp=DestructibleProp.Add(Vector3.zero,.4f,.3f,false);DestructibleProp found;float fraction;C(!DestructibleProp.FindProjectileHit(owner,a,b,.22f,out found,out fraction),"short prop visual bounds allow flight over top");
  var lowA=new Vector3(-3,.2f,0);var lowB=new Vector3(3,.2f,0);C(DestructibleProp.FindProjectileHit(owner,lowA,lowB,.1f,out found,out fraction)&&ReferenceEquals(found,shortProp)&&fraction<.5f,"short prop still accepts actual low contact");
  DestructibleProp.Reset();WorldTraversal.Reset(ZoneKind.Dungeon);var tall=DestructibleProp.Add(Vector3.zero,.4f,1.2f,true);var selector=new ContactSelector(owner,.32f);selector.Sweep(a,b);C(ReferenceEquals(selector.Selected,tall),"first blocking prop survives full-radius terrain clipping epsilon");
  DestructibleProp.Reset();WorldTraversal.Reset(ZoneKind.Dungeon);var wideShort=DestructibleProp.Add(Vector3.zero,.4f,.3f,true);selector=new ContactSelector(owner,.1f);selector.Sweep(lowA,lowB);C(ReferenceEquals(selector.Selected,wideShort),"low wide blocking prop remains hittable after terrain clip");
  selector=new ContactSelector(owner,.22f);selector.Sweep(a,b);C(selector.Selected==null&&selector.End.x>4.99f,"flight above blocking short prop is not stopped by navigation footprint");
  DestructibleProp.Reset();WorldTraversal.Reset(ZoneKind.Dungeon);DestructibleProp.Add(new Vector3(-1,0,0),.4f,.3f,true);var highBehind=DestructibleProp.Add(new Vector3(1,0,0),.4f,1.2f,true);selector=new ContactSelector(owner,.22f);selector.Sweep(a,b);C(ReferenceEquals(selector.Selected,highBehind),"high prop remains hittable behind a short physical blocker flown over");
  Console.WriteLine("PASS: "+n+" actual pet interception/prop bounds/projectile terrain-selection entry checks (managed engine doubles)");
 }
}
"""
body_properties='\n'.join(line for line in pet.splitlines() if 'public float PhysicalBodyHeight =>' in line or 'public float PhysicalBodyRadius =>' in line)
fixture=fixture.replace('PET',body_properties+member(pet,'        public static bool HitHostileProjectile(')).replace('PROPS',member(prop,'        private static bool CanAffect(')+member(prop,'        private void RegisterObstacle()')+member(prop,'        private Bounds ProjectileBounds()')+member(prop,'        internal static bool FindProjectileHit(')).replace('TERRAIN',terrain).replace('SELECTION',selection)
with tempfile.TemporaryDirectory(prefix='emberfall-projectile-contact-') as d:
 p=Path(d);(p/'Math.cs').write_text(math);(p/'Program.cs').write_text('using System;using System.Collections.Generic;using Emberfall;using UnityEngine;'+fixture)
 for source in ['Assets/Scripts/World/WorldTraversal.cs','Assets/Scripts/Combat/PhysicalSkillMotion.cs','Assets/Scripts/Combat/BossScaleRules.cs']:(p/Path(source).name).write_text((r/source).read_text())
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1')
 dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
 subprocess.run([dotnet,'run','--project',str(p/'Test.csproj'),'--configuration','Release'],env=env,check=True)
 program=(p/'Program.cs').read_text()
 controls=[('pet.transform.position,height,','new Vector3(pet.transform.position.x,0,pet.transform.position.z),height,',"upper pet cannot intercept lower flight",'flattened pet height'),('Bounds bounds=ProjectileBounds();WorldTraversal.SetPhysicalBounds(obstacle,bounds.center,bounds.size);','',"flight above blocking short prop is not stopped by navigation footprint",'infinite navigation prop column')]
 for old,new,reason,label in controls:
  assert old in program,'negative control must match actual production entry code: '+label
  (p/'Program.cs').write_text(program.replace(old,new,1))
  built=subprocess.run([dotnet,'build',str(p/'Test.csproj'),'--configuration','Release','--no-restore','-v:q'],env=env,text=True,capture_output=True)
  if built.returncode:print(built.stdout+built.stderr);built.check_returncode()
  result=subprocess.run([dotnet,str(p/'bin/Release/net8.0/Test.dll')],env=env,text=True,capture_output=True)
  assert result.returncode and reason in result.stderr,result.stdout+result.stderr
  print('PASS: compiled '+label+' negative control fails actual contact assertion')

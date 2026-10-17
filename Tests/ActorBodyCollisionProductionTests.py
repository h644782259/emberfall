#!/usr/bin/env python3
"""Execute the actual body movement, finite scenery and body-contact code."""
from pathlib import Path
import os, subprocess, sys, tempfile
root=Path(__file__).resolve().parents[1]
math=(root/'Tests/DestructibleTraversalTests.cs').read_text()
math='using System;using UnityEngine;'+math[math.index('namespace Emberfall'):]
math=math.replace('public static float Abs(float x)','public static float Sqrt(float x)=>(float)Math.Sqrt(x);public static float Exp(float x)=>(float)Math.Exp(x);public static float Abs(float x)')
fixture=r'''
using System;using System.Collections.Generic;using Emberfall;using UnityEngine;
namespace UnityEngine{public struct Quaternion{float angle;public static Quaternion Euler(float x,float y,float z)=>new Quaternion{angle=y*(float)Math.PI/180};public static Vector3 operator *(Quaternion q,Vector3 v)=>new Vector3(v.x*(float)Math.Cos(q.angle)+v.z*(float)Math.Sin(q.angle),v.y,-v.x*(float)Math.Sin(q.angle)+v.z*(float)Math.Cos(q.angle));}public class Transform{public Vector3 position;}public class GameObject{public bool activeInHierarchy=true;}}
namespace Emberfall{
 public static class PerformanceDiagnostics{public enum Section{ActorSlide}public struct Scope:IDisposable{public void Dispose(){}}public static Scope Measure(Section s)=>new Scope();}
 public class PlayerController{public Transform transform=new Transform();public bool IsDead;}
 public class EnemyController{public int WalkingSlideSide;public Transform transform=new Transform();public GameObject gameObject=new GameObject();public bool IsDead;public float NavigationRadius=.45f,PhysicalBodyHeight=2.8f;}
 public sealed partial class GameSession{public List<EnemyController> Enemies=new List<EnemyController>();public PlayerController Player=new PlayerController();}
}
class Program{
 static int n;static void C(bool ok,string m){n++;if(!ok)throw new Exception(m);}
 static void Reset(){WorldTraversal.Reset(ZoneKind.Wilderness);WorldTerrain.ReliefEnabled=false;WorldTerrain.Configure(ZoneKind.Wilderness,1);}
 static Vector3 Move(GameSession g,Vector3 from,Vector3 delta){float fall=0;return g.MovePlayerWithBodies(from,delta,.1f,ref fall);}
 static void Main(){Reset();var g=new GameSession();
  var wall=WorldTraversal.AddDynamicBox(new Vector3(5,0,0),new Vector2(1,1));
  C(WorldTraversal.IsActorCapsuleClear(new Vector3(8,0,0),.45f,1.8f),"index starts without unregistered physical extent");
  WorldTraversal.SetPhysicalBounds(wall,new Vector3(6,1,0),new Vector3(5,2,2));
  C(WorldTraversal.IsWalkable(new Vector3(3.9f,0,0)),"authored navigation footprint is preserved");
  C(!WorldTraversal.IsActorCapsuleClear(new Vector3(3.9f,0,0),.45f,1.8f),"body collides with rendered solid beyond navigation footprint");
  C(!WorldTraversal.IsActorCapsuleClear(new Vector3(8,0,0),.45f,1.8f),"late bounds assignment invalidates warmed broad phase");
  C(WorldTraversal.IsActorCapsuleClear(new Vector3(8,2.02f,0),.45f,1.8f),"finite wall permits a capsule genuinely above its top");
  C(WorldTraversal.IsActorCapsuleStepClear(new Vector3(3.4f,0,0),new Vector3(3.3f,0,0),.45f,1.8f),"existing scenery overlap can escape away from solid");
  C(!WorldTraversal.IsActorCapsuleStepClear(new Vector3(3.4f,0,0),new Vector3(3.5f,0,0),.45f,1.8f),"existing scenery overlap cannot move deeper");
  var result=Move(g,Vector3.zero,new Vector3(12,0,0));C(result.x>3.5f-ActorBodyCollision.PlayerRadius-.09f&&result.x<=3.5f-ActorBodyCollision.PlayerRadius+.001f,"swept movement stops at the visible wall without tunneling");
  C(WorldTraversal.RemoveDynamicObstacle(wall)&&Move(g,Vector3.zero,new Vector3(8,0,0)).x>7.9f,"broken prop releases collision immediately");
  Reset();var slab=WorldTraversal.AddPlatform(Vector3.zero,new Vector2(4,4),.8f);WorldTraversal.SetPhysicalBounds(slab,new Vector3(0,.4f,0),new Vector3(4,.8f,4));
  C(WorldTraversal.IsActorCapsuleClear(new Vector3(0,.8f,0),.45f,1.8f),"capsule on platform touches floor without becoming blocked");
  C(!WorldTraversal.IsActorCapsuleClear(new Vector3(0,.76f,0),.45f,1.8f),"capsule below slab top is blocked");
  result=Move(g,new Vector3(0,.8f,0),new Vector3(.6f,0,0));C(result.x>.59f&&Math.Abs(result.y-.8f)<.00001f,"platform support and horizontal movement remain authoritative");
  Reset();var enemy=new EnemyController();enemy.transform.position=new Vector3(3,0,0);g.Enemies.Add(enemy);
  result=Move(g,Vector3.zero,new Vector3(20,0,0));C(result.x>3-ActorBodyCollision.PlayerRadius-.45f-.09f&&result.x<=3-ActorBodyCollision.PlayerRadius-.45f+.011f,"player cannot pass through living monster at high movement speed");
  enemy.IsDead=true;C(Move(g,Vector3.zero,new Vector3(6,0,0)).x>5.9f,"dead monster no longer blocks player");enemy.IsDead=false;
  enemy.gameObject.activeInHierarchy=false;C(Move(g,Vector3.zero,new Vector3(6,0,0)).x>5.9f,"retired monster no longer blocks player");enemy.gameObject.activeInHierarchy=true;
  enemy.transform.position=new Vector3(2,0,1);result=Move(g,Vector3.zero,new Vector3(4,0,2));C(result.x>3.9f&&Math.Abs(result.z)<.01f,"blocked diagonal movement slides along a valid tangent");
  enemy.transform.position=new Vector3(.4f,0,0);result=Move(g,Vector3.zero,new Vector3(-1,0,0));C(result.x<-.99f,"preexisting overlap can leave without sticking");
  C(!ActorBodyCollision.PermitsStep(Vector3.zero,new Vector3(.2f,0,0),.45f,1.8f,enemy.transform.position,.45f,2.8f),"overlap cannot move deeper through monster centre");
  enemy.transform.position=new Vector3(3,0,0);g.Player.transform.position=Vector3.zero;
  result=g.ResolveEnemyBodyMove(new Vector3(6,0,0),Vector3.zero,enemy);C(result.x>=ActorBodyCollision.PlayerRadius+.45f-.011f&&result.x<ActorBodyCollision.PlayerRadius+.45f+.09f,"enemy cannot move through player in the reverse direction");
  result=g.ClipPlayerAirMotion(new Vector3(0,1,0),new Vector3(6,1,0));C(result.x<=3-ActorBodyCollision.PlayerRadius-.45f+.011f,"jump path cannot pass through a tall monster");
  result=g.ClipPlayerAirMotion(new Vector3(0,1,0),new Vector3(6,0,0));C(result.x<=3-ActorBodyCollision.PlayerRadius-.45f+.011f&&Math.Abs(result.y)<.00001f,"horizontal body contact preserves legal falling motion x="+result.x+" y="+result.y);
  C(!ActorBodyCollision.PermitsStep(new Vector3(3,3,0),new Vector3(3,2.7f,0),.45f,1.8f,enemy.transform.position,.45f,2.8f),"descending from vertical clearance cannot enter a fresh body overlap");
  float bodyFall=2;result=g.MovePlayerWithBodies(new Vector3(3,2.79f,0),new Vector3(1,0,0),.1f,ref bodyFall);C(result.x>3.8f&&bodyFall<3,"body support permits horizontal escape without accumulating fall speed");
  enemy.PhysicalBodyHeight=1.3f;result=g.ClipPlayerAirMotion(new Vector3(0,1.65f,0),new Vector3(6,1.65f,0));C(result.x>5.9f,"real vertical clearance allows jumping over short monster");
  enemy.PhysicalBodyHeight=2.8f;result=g.ResolvePlayerLanding(Vector3.zero,new Vector3(3,0,0));C(result.x<=3-ActorBodyCollision.PlayerRadius-.45f+.011f&&WorldTraversal.CanStand(result),"skill landing finds a grounded free point before occupied target");
  result=g.ResolvePlayerLanding(Vector3.zero,new Vector3(6,0,0));C(result.x>5.9f,"teleport keeps traversal semantics when its landing is clear");
  g.Player.IsDead=true;C(g.ResolveEnemyBodyMove(new Vector3(6,0,0),Vector3.zero,enemy).x<.01f,"dead player does not obstruct encounter actors");g.Player.IsDead=false;
  g.Enemies.Clear();Reset();
  var pebble=WorldTraversal.AddDynamicCircle(new Vector3(2,0,0),.6f);WorldTraversal.SetPhysicalBounds(pebble,new Vector3(2,.2f,0),new Vector3(1,.4f,1));
  C(WorldTraversal.RemoveLowRockObstacle(pebble)&&Move(g,Vector3.zero,new Vector3(4,0,0)).x>3.9f,"low decorative rock does not obstruct walking");
  var boulder=WorldTraversal.AddDynamicCircle(new Vector3(2,0,0),.6f);WorldTraversal.SetPhysicalBounds(boulder,new Vector3(2,.6f,0),new Vector3(1,1.2f,1));
  C(!WorldTraversal.RemoveLowRockObstacle(boulder),"tall rock remains solid");
  Reset();WorldTraversal.SetArenaRadius(10);
  var edgeRock=WorldTraversal.AddDynamicCircle(new Vector3(8,0,0),.6f);
  var stuck=new Vector3(6.95f,0,0);result=Move(g,stuck,new Vector3(.2f,0,0));
  C((result-stuck).sqrMagnitude>.001f&&WorldTraversal.IsWalkable(result),"navigation contact uses angled slide instead of returning stuck");
  var edge=new Vector3(9.55f,0,0);result=Move(g,edge,new Vector3(-.2f,0,0));C(result.x<edge.x-.1f,"arena edge permits inward escape");
  Reset();g.Player.transform.position=new Vector3(-10,0,-10);
  var wide=WorldTraversal.AddDynamicBox(new Vector3(5,0,0),new Vector2(1,1));
  WorldTraversal.SetPhysicalBounds(wide,new Vector3(5,1,0),new Vector3(4,2,2));
  var start=new Vector3(2.55f,0,0);result=g.MoveEnemyWalking(start,new Vector3(.15f,0,0),enemy);
  C((result-start).sqrMagnitude>.001f&&WorldTraversal.IsActorCapsuleClear(result,enemy.NavigationRadius,enemy.PhysicalBodyHeight),"enemy slides past physical extent instead of freezing");
  for(int i=0;i<100;i++)result=g.MoveEnemyWalking(result,(new Vector3(9,0,0)-result).normalized*.15f,enemy);
  C(result.x>7.5f,"enemy continues around wide solid without crossing it x="+result.x+" z="+result.z);
  Reset();
  for(int i=0;i<100;i++)Move(g,Vector3.zero,new Vector3(.2f,0,0));long bytes=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<10000;i++)Move(g,Vector3.zero,new Vector3(.2f,0,0));bytes=GC.GetAllocatedBytesForCurrentThread()-bytes;C(bytes==0,"warmed body movement allocates zero bytes");
  Console.WriteLine("PASS "+n+" actual scene/body movement, sliding, vertical clearance, landing, retirement and index invalidation assertions; warm allocations="+bytes);
 }
}
'''
with tempfile.TemporaryDirectory(prefix='emberfall-body-collision-') as directory:
 p=Path(directory)
 for name in ['World/WorldTraversal','World/WorldTraversal.ActorBodies','Core/ActorBodyCollision','Core/GameSession.ActorBodies']:
  (p/(Path(name).name+'.cs')).write_text((root/'Assets/Scripts'/(name+'.cs')).read_text())
 (p/'Math.cs').write_text(math);(p/'Program.cs').write_text(fixture)
 project=p/'Tests.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet');env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1')
 subprocess.run([dotnet,'run','--project',str(project),'--configuration','Release'],env=env,check=True)
 for file,before,after,message in [
  ('GameSession.ActorBodies.cs','if((result-intended).sqrMagnitude<.000001f&&\n                CombatFx.Flat(result-from).sqrMagnitude>=CombatFx.Flat(delta).sqrMagnitude*.25f)','if((result-intended).sqrMagnitude<.000001f)','navigation contact uses angled slide instead of returning stuck'),
  ('WorldTraversal.ActorBodies.cs','if(CapsuleSolidDistanceSquared(feet,radius,height,o)<radius*radius-.00001f)return false;','if(false)return false;','body collides with rendered solid beyond navigation footprint'),
  ('ActorBodyCollision.cs','if(distance>=sum*sum)return true;','return true;','player cannot pass through living monster at high movement speed')]:
  path=p/file;original=path.read_text();assert before in original;path.write_text(original.replace(before,after))
  built=subprocess.run([dotnet,'build',str(project),'--no-restore','--configuration','Release','-v:q'],env=env,capture_output=True,text=True);assert built.returncode==0,built.stdout+built.stderr
  result=subprocess.run([dotnet,str(p/'bin/Release/net8.0/Tests.dll')],env=env,capture_output=True,text=True);assert result.returncode and message in result.stdout+result.stderr,result.stdout+result.stderr
  path.write_text(original);print('PASS compiled negative: '+message)

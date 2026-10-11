#!/usr/bin/env python3
"""Run real PhysicalSkillMotion + WorldTraversal against numeric engine/PhysX doubles."""
import os,sys,subprocess,tempfile
from pathlib import Path
r=Path(__file__).resolve().parents[1]
math=(r/'Tests/DestructibleTraversalTests.cs').read_text();math='using System;using UnityEngine;'+math[math.index('namespace Emberfall'):]
fixture=r"""
namespace UnityEngine{
 public class Collider{public object Actor;public T GetComponentInParent<T>()where T:class=>Actor as T;}
 public struct RaycastHit{public Collider collider;public float distance;public Vector3 normal;}
 public enum QueryTriggerInteraction{Ignore}
 public static class Physics{public const int DefaultRaycastLayers=-1;public static int Casts;public static RaycastHit[] Hits=new RaycastHit[0];public static int SphereCastNonAlloc(Vector3 from,float radius,Vector3 direction,RaycastHit[] result,float distance,int layers,QueryTriggerInteraction trigger){Casts++;int n=Math.Min(Hits.Length,result.Length);Array.Copy(Hits,result,n);return n;}}
}
namespace Emberfall{public class PlayerController{}public class EnemyController{}public class SummonedCompanion{}}
class Program{
 static int n;static void C(bool value,string name){n++;if(!value)throw new Exception(name);}static bool Near(float a,float b)=>Math.Abs(a-b)<.001f;
 static void Main(){
  var a=new Vector3(-5,1,0);var b=new Vector3(5,1,0);float entry;
  C(PhysicalSkillMotion.BodyContact(a,b,Vector3.zero,2,.4f,.1f,out entry)&&Near(entry,.45f),"capsule returns first surface entry rather than closest axis");
  C(!PhysicalSkillMotion.BodyContact(new Vector3(-5,3,0),new Vector3(5,3,0),Vector3.zero,2,.4f,.1f,out entry),"flight over head cannot deal a flat-distance hit");
  C(!PhysicalSkillMotion.BodyContact(new Vector3(-5,1,2),new Vector3(5,1,2),Vector3.zero,2,.4f,.1f,out entry),"wide side miss is rejected");
  C(PhysicalSkillMotion.BodyContact(Vector3.zero,new Vector3(.01f,0,0),Vector3.zero,2,.4f,.1f,out entry)&&entry==0,"spawn already touching capsule enters at zero");
  float small,big;C(PhysicalSkillMotion.BodyContact(a,b,Vector3.zero,2,.2f,.1f,out small)&&PhysicalSkillMotion.BodyContact(a,b,new Vector3(1,0,0),4,2,.1f,out big)&&big<small,"body surface ordering is independent of center ordering");
  C(PhysicalSkillMotion.BoxContact(a,b,new Vector3(0,.6f,0),new Vector3(.8f,1.2f,.8f),.32f,out entry)&&Near(entry,.428f),"rounded prop box returns first full-width surface entry");
  C(!PhysicalSkillMotion.BoxContact(new Vector3(-3,.5f,.6f),new Vector3(3,.5f,.6f),new Vector3(0,.15f,0),new Vector3(.8f,.3f,.8f),.22f,out entry),"expanded box corner is rejected outside rounded sphere sweep");
  C(PhysicalSkillMotion.BoxContact(new Vector3(-3,.42f,.52f),new Vector3(3,.42f,.52f),new Vector3(0,.15f,0),new Vector3(.8f,.3f,.8f),.22f,out entry)&&entry<.5f,"actual rounded upper prop corner accepts real contact");
  C(PhysicalSkillMotion.BoxContact(new Vector3(0,.2f,0),new Vector3(3,.2f,0),new Vector3(0,.15f,0),new Vector3(.8f,.3f,.8f),.1f,out entry)&&entry==0,"spawn touching prop returns immediate surface entry");
  var from=new Vector3(0,3,0);var to=new Vector3(8,1,0);float duration=.8f;C((PhysicalSkillMotion.BallisticPosition(from,to,duration,duration)-to).magnitude<.0001f,"gravity flight reaches exact contact at authoritative time");var v0=PhysicalSkillMotion.BallisticVelocity(from,to,.2f,duration);var v1=PhysicalSkillMotion.BallisticVelocity(from,to,.3f,duration);C(Near(v1.y-v0.y,-.981f)&&Near(v1.x,v0.x),"gravity acceleration is 9.81 and horizontal momentum remains constant");
  WorldTraversal.Reset(ZoneKind.Dungeon);WorldTraversal.AddBox(Vector3.zero,new Vector2(.04f,6));C(!PhysicalSkillMotion.ClearFlight(new Vector3(-3,1,0),new Vector3(3,1,0),.1f),"thin registered wall clips flight even without PhysX collider");
  WorldTraversal.Reset(ZoneKind.Dungeon);WorldTraversal.AddPlatform(Vector3.zero,new Vector2(2,2),.8f);C(!PhysicalSkillMotion.ClearFlight(new Vector3(-3,.5f,0),new Vector3(3,.5f,0),.1f),"platform side blocks a low projectile");C(PhysicalSkillMotion.ClearFlight(new Vector3(-3,1.2f,0),new Vector3(3,1.2f,0),.1f),"high flight passes over finite platform top");
  WorldTraversal.Reset(ZoneKind.Dungeon);var prop=WorldTraversal.AddDynamicCircle(Vector3.zero,.4f);WorldTraversal.SetPhysicalBounds(prop,new Vector3(0,.15f,0),new Vector3(.8f,.3f,.8f));C(!WorldTraversal.IsWalkable(Vector3.zero,.1f)&&WorldTraversal.IsPhysicalSpaceClear(new Vector3(0,1,0),.22f),"finite prop body preserves blocked navigation while allowing physical flight over top");C(Near(WorldTraversal.SurfaceHeight(Vector3.zero,.05f),0)&&Near(WorldTraversal.PhysicalSurfaceHeight(new Vector3(0,.4f,0),.05f,.4f),.3f),"short prop supports debris without becoming a climbable navigation platform");C(Near(WorldTraversal.PhysicalSurfaceHeight(new Vector3(0,.1f,0),.05f,.1f),0),"fragment below prop top is not snapped upward");C(WorldTraversal.HasLineOfSightIgnoringObstacle(new Vector3(-3,1,0),new Vector3(0,1,0),prop),"own-prop exclusion is preserved in high line-of-sight branch");
  WorldTraversal.Reset(ZoneKind.Dungeon);WorldTraversal.AddBox(Vector3.zero,new Vector2(.04f,6));var point=new Vector3(-1,1,0);var speed=new Vector3(40,0,0);C(PhysicalSkillMotion.Advance(ref point,ref speed,.07f,.05f)&&point.x<0&&speed.x<0,"fast fragment cannot tunnel through registered thin wall");
  WorldTraversal.Reset(ZoneKind.Dungeon);point=new Vector3(3,.08f,0);speed=new Vector3(1,-3,0);C(PhysicalSkillMotion.Advance(ref point,ref speed,.07f,.016f)&&point.y>=.07f&&speed.y>=0,"ground contact bounces above surface without sinking");
  var response=new Vector3(4,-10,2);PhysicalSkillMotion.ResolveContact(ref response,Vector3.up,.3f,.65f,.016f);C(Near(response.y,3)&&Near(response.x,2.6f)&&Near(response.z,1.3f),"restitution preserves normal rebound while friction removes only tangential energy");
  response=new Vector3(10,4,2);PhysicalSkillMotion.ResolveContact(ref response,new Vector3(-1,0,0),.3f,.65f,.016f);C(Near(response.x,-3)&&Near(response.y,2.6f)&&Near(response.z,1.3f),"wall collision separates the rebound axis from sliding friction");
  response=new Vector3(1,2,0);PhysicalSkillMotion.ResolveContact(ref response,Vector3.up,.3f,.65f,.016f);C(Near(response.x,1)&&Near(response.y,2),"departing surface cannot create a second collision impulse");
  WorldTraversal.Reset(ZoneKind.Dungeon);float firstSlide=-1;
  foreach(int fps in new[]{30,60,120}){point=new Vector3(3,.1f,0);speed=new Vector3(4,0,0);for(int i=0;i<fps/10;i++)PhysicalSkillMotion.Advance(ref point,ref speed,.1f,1f/fps);if(firstSlide<0)firstSlide=speed.x;C(Math.Abs(speed.x-firstSlide)<.002f&&Math.Abs(point.y-.1f)<.001f,"supported sliding momentum is independent of 30/60/120 FPS and substep count");}
  WorldTraversal.Reset(ZoneKind.Dungeon);var support=WorldTraversal.AddDynamicCircle(new Vector3(3,0,0),.6f);WorldTraversal.SetPhysicalBounds(support,new Vector3(3,.15f,0),new Vector3(1.2f,.3f,1.2f));point=new Vector3(3,.4f,0);speed=Vector3.zero;int supportRevision=-1;
  C(PhysicalSkillMotion.AdvanceSupported(ref point,ref speed,ref supportRevision,.1f,.016f)&&supportRevision==WorldTraversal.Revision,"settled contact records the actual registered support revision");int casts=UnityEngine.Physics.Casts;
  for(int i=0;i<120;i++)PhysicalSkillMotion.AdvanceSupported(ref point,ref speed,ref supportRevision,.1f,.016f);
  C(UnityEngine.Physics.Casts==casts&&Near(point.y,.4f),"sleeping rubble preserves its landing without repeating physical sweeps");
  WorldTraversal.RemoveDynamicObstacle(support);PhysicalSkillMotion.AdvanceSupported(ref point,ref speed,ref supportRevision,.1f,.016f);C(point.y<.4f&&speed.y<0&&supportRevision==-1&&UnityEngine.Physics.Casts>casts,"removing support wakes rubble and restores gravity");
  var held=point;PhysicalSkillMotion.Advance(ref point,ref speed,.1f,float.NaN);C((point-held).sqrMagnitude==0,"invalid delta cannot poison fragment positions");
  UnityEngine.Physics.Hits=new[]{new UnityEngine.RaycastHit{collider=new UnityEngine.Collider(),distance=2,normal=Vector3.up},new UnityEngine.RaycastHit{collider=new UnityEngine.Collider{Actor=new Emberfall.PlayerController()},distance=.1f,normal=Vector3.up},new UnityEngine.RaycastHit{collider=new UnityEngine.Collider(),distance=.8f,normal=Vector3.up}};UnityEngine.RaycastHit hit;C(PhysicalSkillMotion.SweepScenery(a,b,.1f,out hit)&&Near(hit.distance,.8f),"unsorted PhysX hits choose first scenery contact and exclude actors");
  Console.WriteLine("PASS: "+n+" physical capsule entry/order/height/flight/gravity/no-tunneling/bounce checks (production methods; explicit PhysX double)");
 }
}
"""
with tempfile.TemporaryDirectory(prefix='emberfall-physical-collision-') as d:
 p=Path(d);(p/'Math.cs').write_text(math);(p/'Program.cs').write_text('using System;using Emberfall;using UnityEngine;'+fixture)
 for source in ['Assets/Scripts/World/WorldTraversal.cs','Assets/Scripts/Combat/PhysicalSkillMotion.cs']:(p/Path(source).name).write_text((r/source).read_text())
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1')
 dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
 subprocess.run([dotnet,'run','--project',str(p/'Test.csproj'),'--configuration','Release'],env=env,check=True)
 source=(p/'PhysicalSkillMotion.cs').read_text()
 assert 'fraction=high;return true;' in source,'first-entry negative control must match the production return'
 body_start=source.index('        internal static bool BodyContact(');body_end=source.index('        internal static bool BoxContact(',body_start)
 (p/'PhysicalSkillMotion.cs').write_text(source[:body_start]+source[body_start:body_end].replace('fraction=high;return true;','fraction=s;return true;')+source[body_end:])
 built=subprocess.run([dotnet,'build',str(p/'Test.csproj'),'--configuration','Release','-v:q'],env=env,text=True,capture_output=True)
 if built.returncode:print(built.stdout+built.stderr);built.check_returncode()
 control=subprocess.run([dotnet,str(p/'bin/Release/net8.0/Test.dll')],env=env,text=True,capture_output=True)
 assert control.returncode and 'capsule returns first surface entry rather than closest axis' in control.stderr,control.stdout+control.stderr
 print('PASS: compiled closest-axis negative control fails first-entry contact assertion')
 for old,new,reason,label in [
  ('velocity=tangent*retention+normal*(bounce?-toward*restitution:0);','velocity=(tangent+normal*(bounce?-toward*restitution:0))*retention;','restitution preserves normal rebound while friction removes only tangential energy','double-damped rebound'),
  ('supportRevision==WorldTraversal.Revision&&velocity.sqrMagnitude<.000001f','supportRevision>=0&&velocity.sqrMagnitude<.000001f','removing support wakes rubble and restores gravity','stale sleeping support')]:
  assert old in source,'negative control must match production: '+label
  (p/'PhysicalSkillMotion.cs').write_text(source.replace(old,new,1))
  built=subprocess.run([dotnet,'build',str(p/'Test.csproj'),'--configuration','Release','-v:q'],env=env,text=True,capture_output=True)
  if built.returncode:print(built.stdout+built.stderr);built.check_returncode()
  control=subprocess.run([dotnet,str(p/'bin/Release/net8.0/Test.dll')],env=env,text=True,capture_output=True)
  assert control.returncode and reason in control.stderr,control.stdout+control.stderr
  print('PASS: compiled '+label+' negative control fails actual physical assertion')

#!/usr/bin/env python3
"""Run production navigation against its unchanged full-scan geometry oracle.
The temporary assembly adds counters only; engine doubles do not claim device FPS.
"""
from pathlib import Path
import os, sys, subprocess, tempfile

root = Path(__file__).resolve().parents[1]
source = (root / 'Assets/Scripts/World/WorldTraversal.cs').read_text()
enemy = (root / 'Assets/Scripts/Combat/EnemyController.cs').read_text()

def member(text, marker):
    start = text.index(marker)
    end = text.index('{', start) + 1
    depth = 1
    while depth:
        depth += (text[end] == '{') - (text[end] == '}')
        end += 1
    return text[start:end]

math = (root / 'Tests/DestructibleTraversalTests.cs').read_text()
math = 'using System;using UnityEngine;' + math[math.index('namespace Emberfall'):]
math = math.replace('public static float Abs(float x)', 'public static float Sqrt(float x)=>(float)Math.Sqrt(x);public static float Exp(float x)=>(float)Math.Exp(x);public static float Abs(float x)')
math += '''
namespace UnityEngine{
 public class Transform{public Vector3 position;public Quaternion rotation;}
 public struct Quaternion{public static Quaternion LookRotation(Vector3 p)=>new Quaternion();public static Quaternion Slerp(Quaternion a,Quaternion b,float t)=>a;}
 public static class Random{public static float value=>.5f;public static float Range(float a,float b)=>a;}
}
'''
fixture = r'''
using System;using System.Collections.Generic;using System.Diagnostics;using Emberfall;using UnityEngine;using Random=UnityEngine.Random;
public static class TraversalCounters{public static long ActualVisits,LegacyVisits,Searches;}
namespace Emberfall{
 public class WanderSession{public float ArenaRadius=>WorldTraversal.ArenaRadius;}
 public class ActualWander{
  public Transform transform=new Transform();private WanderSession session=new WanderSession();
  private WorldTraversal.Route route=new WorldTraversal.Route();private float wanderPause,wanderBlocked,wanderAge;
  private bool hasWanderGoal;private Vector3 wanderGoal;private float NavigationRadius=>.45f;
  private void AnimateModel(float a,float b,bool c){}
  private Vector3 WalkForAnimation(Vector3 d)=>WorldTraversal.Move(transform.position,d,NavigationRadius);
  WANDER
 }
}
class Program{
 static int checks;static void C(bool v,string why){checks++;if(!v)throw new Exception(why);}
 static bool Near(float a,float b)=>Math.Abs(a-b)<.000001f;
 static void Reset(float arena=35){WorldTraversal.Reset(ZoneKind.Wilderness);FullScanTraversal.Reset(ZoneKind.Wilderness);WorldTraversal.SetArenaRadius(arena);FullScanTraversal.SetArenaRadius(arena);WorldTerrain.ReliefEnabled=FullScanTerrain.ReliefEnabled=false;WorldTerrain.Configure(ZoneKind.Wilderness,1);FullScanTerrain.Configure(ZoneKind.Wilderness,1);}
 static void Circle(Vector3 p,float radius){WorldTraversal.AddCircle(p,radius);FullScanTraversal.AddCircle(p,radius);}
 static void Box(Vector3 p,Vector2 size){WorldTraversal.AddBox(p,size);FullScanTraversal.AddBox(p,size);}
 static void Point(Vector3 p,float r){C(WorldTraversal.TestClearOfSolids(p,r)==FullScanTraversal.TestClearOfSolids(p,r),"spatial candidates preserve full-scan exact point clearance");C(WorldTraversal.IsWalkable(p,r)==FullScanTraversal.IsWalkable(p,r),"water/bridge policy unchanged by broad phase");}
 static void Main(){
  Reset();var rng=new System.Random(77123);
  for(int i=0;i<600;i++){
   var p=new Vector3((float)(rng.NextDouble()*68-34),0,(float)(rng.NextDouble()*68-34));
   if(i%2==0)Circle(p,.12f+(float)rng.NextDouble()*.34f);else Box(p,new Vector2(.15f+(float)rng.NextDouble()*.55f,.15f+(float)rng.NextDouble()*.55f));
  }
  WorldTraversal.SetRiver(new[]{new Vector3(-30,0,0),new Vector3(30,0,0)},1.8f,new Rect(-2,-3,4,6));FullScanTraversal.SetRiver(new[]{new Vector3(-30,0,0),new Vector3(30,0,0)},1.8f,new Rect(-2,-3,4,6));
  TraversalCounters.ActualVisits=TraversalCounters.LegacyVisits=0;
  for(int i=0;i<24000;i++){var p=new Vector3((float)(rng.NextDouble()*76-38),0,(float)(rng.NextDouble()*76-38));float r=i%11==0?-.2f:.01f+(float)rng.NextDouble()*2.65f;Point(p,r);}
  long actual=TraversalCounters.ActualVisits,legacy=TraversalCounters.LegacyVisits;C(actual*8<legacy,"nearby buckets reduce exact solid visits by at least eightfold");
  Console.WriteLine("POINT BROAD PHASE actual="+actual+" fullScan="+legacy+" reduction="+((double)legacy/actual).ToString("F1")+"x");
  for(int i=0;i<180;i++){
   var a=new Vector3((float)(rng.NextDouble()*60-30),0,(float)(rng.NextDouble()*60-30));var b=new Vector3((float)(rng.NextDouble()*60-30),0,(float)(rng.NextDouble()*60-30));float r=.04f+(float)rng.NextDouble();
   C(WorldTraversal.HasGroundPath(a,b,r)==FullScanTraversal.HasGroundPath(a,b,r),"all sampled ground segments match full scan");C(WorldTraversal.HasLineOfSight(a,b)==FullScanTraversal.HasLineOfSight(a,b),"sampled LOS matches full scan");
  }
  foreach(float x in new[]{-8f,-4f,0f,4f,8f})foreach(float z in new[]{-8f,-4f,0f,4f,8f}){
   Reset();Circle(new Vector3(x+.25f,0,z),.25f);Box(new Vector3(x,0,z+.25f),new Vector2(.5f,.5f));
   foreach(float epsilon in new[]{-.0001f,0f,.0001f})foreach(float r in new[]{0f,.04f,.45f,2.65f})Point(new Vector3(x+epsilon,0,z+epsilon),r);
  }
  Reset();var handles=new List<WorldTraversal.ObstacleHandle>();var refs=new List<FullScanTraversal.ObstacleHandle>();
  for(int i=0;i<90;i++){var p=new Vector3((i%10)*5-22,0,(i/10)*5-20);handles.Add(WorldTraversal.AddDynamicBox(p,new Vector2(5.5f,1)));refs.Add(FullScanTraversal.AddDynamicBox(p,new Vector2(5.5f,1)));}
  for(int i=0;i<90;i+=2){C(WorldTraversal.RemoveDynamicObstacle(handles[i])==FullScanTraversal.RemoveDynamicObstacle(refs[i]),"removal updates all shifted bucket indices");Point(new Vector3((i%10)*5-22,0,(i/10)*5-20),.65f);}
  for(int i=0;i<90;i++){var a=new Vector3((i%10)*5-22,0,(i/10)*5-20);var b=a+new Vector3(0,0,2);C(WorldTraversal.HasLineOfSightIgnoringObstacle(a,b,handles[i])==FullScanTraversal.HasLineOfSightIgnoringObstacle(a,b,refs[i]),"ignored handle skips only its own cross-bucket obstacle");}
  var old=handles[1];Reset();var door=WorldTraversal.AddDynamicBox(Vector3.zero,new Vector2(.2f,12));var oldDoor=FullScanTraversal.AddDynamicBox(Vector3.zero,new Vector2(.2f,12));
  C(!WorldTraversal.RemoveDynamicObstacle(old),"prior-room token cannot remove new door");Point(Vector3.zero,.45f);C(WorldTraversal.RemoveDynamicObstacle(door)&&FullScanTraversal.RemoveDynamicObstacle(oldDoor),"door removal committed immediately");Point(Vector3.zero,.45f);
  Reset();Box(Vector3.zero,new Vector2(10000,10000));Circle(new Vector3(100000,0,0),99990);Circle(new Vector3(float.NaN,0,0),1);Box(new Vector3(1,0,1),new Vector2(-2,-2));
  foreach(var p in new[]{Vector3.zero,new Vector3(4,0,4),new Vector3(-4,0,-4),new Vector3(float.NaN,0,0)})foreach(float r in new[]{.45f,-.2f,float.NaN,float.PositiveInfinity})Point(p,r);
  Reset();WorldTraversal.BeginSceneBuild(1.5f);FullScanTraversal.BeginSceneBuild(1.5f);Box(new Vector3(4,0,-4),new Vector2(1,3));Circle(new Vector3(-4,0,4),.6f);WorldTraversal.EndSceneBuild();FullScanTraversal.EndSceneBuild();
  Point(new Vector3(6,0,-6),.4f);Point(new Vector3(-6,0,6),.4f);C(WorldTraversal.NavigationMapKey==FullScanTraversal.NavigationMapKey,"scene scale and navigation identity preserved");
  Reset();WorldTraversal.AddPlatform(Vector3.zero,new Vector2(3,3),.8f);FullScanTraversal.AddPlatform(Vector3.zero,new Vector2(3,3),.8f);
  WorldTraversal.AddSurfaceTriangle(new Vector3(-2,.03f,-2),new Vector3(2,.09f,-2),new Vector3(0,.06f,2));FullScanTraversal.AddSurfaceTriangle(new Vector3(-2,.03f,-2),new Vector3(2,.09f,-2),new Vector3(0,.06f,2));
  foreach(var p in new[]{Vector3.zero,new Vector3(1,0,1),new Vector3(-2,0,0),new Vector3(3,0,0)})C(Near(WorldTraversal.SurfaceHeight(p),FullScanTraversal.SurfaceHeight(p)),"mesh and raised-platform height preserved");
  C(WorldTraversal.CanStand(new Vector3(0,.8f,0))==FullScanTraversal.CanStand(new Vector3(0,.8f,0)),"height-aware stand semantics unchanged");
  // Stationary goals on a valid detour must not force A* every .85 seconds.
  Reset(22);Box(Vector3.zero,new Vector2(1,8));var from=new Vector3(-6,0,0);var to=new Vector3(6,0,0);var route=new WorldTraversal.Route();Time.time=0;TraversalCounters.Searches=0;
  C(route.Direction(from,to).sqrMagnitude>0,"actual static detour available");for(int i=1;i<=600;i++){Time.time=i/60f;C(route.Direction(from,to).sqrMagnitude>0,"cached static path stays reachable");}
  C(TraversalCounters.Searches==1,"stable wander detour builds exactly one path across ten seconds");
  long allocated=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<1200;i++)route.Direction(from,to);allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;C(allocated==0,"warmed route reuses path without per-frame managed allocation");Console.WriteLine("ROUTE static searches="+TraversalCounters.Searches+" warmed allocated="+allocated);
  Time.time+=.01f;route.Direction(from,to,.451f);C(TraversalCounters.Searches==2,"exact radius change immediately invalidates cached path");
  Time.time+=.01f;route.Direction(from,to+new Vector3(0,0,.4f),.451f);C(TraversalCounters.Searches==2,"small moving goal retains original retry interval");Time.time+=.86f;route.Direction(from,to+new Vector3(0,0,.4f),.451f);C(TraversalCounters.Searches==3,"moving goal refreshed within original .85-second interval");
  var block=WorldTraversal.AddDynamicCircle(new Vector3(-3,0,0),.2f);route.Direction(from,to,.451f);C(TraversalCounters.Searches==4,"new solid revision invalidates cache immediately");WorldTraversal.RemoveDynamicObstacle(block);route.Direction(from,to,.451f);C(TraversalCounters.Searches==5,"removed door revision invalidates cache immediately");
  // A failed route retries on the budget, rather than every player Update.
  Reset(22);Box(Vector3.zero,new Vector2(50,1));route=new WorldTraversal.Route();from=new Vector3(0,0,-5);to=new Vector3(0,0,5);Time.time=0;TraversalCounters.Searches=0;
  for(int i=0;i<120;i++){Time.time=i/60f;C(route.Direction(from,to).sqrMagnitude==0,"unreachable route remains stopped");}C(TraversalCounters.Searches==3,"failed route retries three times in two seconds");
  // Execute the real Wander method's successful candidate and Direction chain.
  Reset(22);Box(Vector3.zero,new Vector2(1,8));var wander=new ActualWander();wander.transform.position=new Vector3(-6,0,0);Time.time=0;TraversalCounters.Searches=0;wander.Wander(.016f,3);
  C(TraversalCounters.Searches==1,"actual Wander retains candidate path for same-frame Direction");
  Reset(22);Box(Vector3.zero,new Vector2(1,8));route=new WorldTraversal.Route();from=new Vector3(-6,0,0);to=new Vector3(6,0,0);TraversalCounters.Searches=0;C(route.TryPlan(from,to),"candidate plan reaches destination");route.Direction(from,to);C(TraversalCounters.Searches==1,"committed candidate plan is not rebuilt by Direction");
  // Warm allocation measurement also covers cross-bucket de-duplication.
  WorldTraversal.TestClearOfSolids(new Vector3(4,0,4),2.65f);allocated=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<10000;i++)WorldTraversal.TestClearOfSolids(new Vector3(4,0,4),2.65f);allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;C(allocated==0,"warmed spatial point queries allocate zero bytes");
  Console.WriteLine("PASS "+checks+" production spatial/full-scan, dynamic door, radius, height, route retry and actual Wander assertions");
 }
}
'''.replace('WANDER', member(enemy, 'private void Wander(').replace('private void Wander', 'public void Wander'))

def instrument(code, oracle=False):
    code = code.replace('private static bool ClearOfSolids(', 'public static bool TestClearOfSolids(').replace('ClearOfSolids(', 'TestClearOfSolids(').replace('TestTestClearOfSolids(', 'TestClearOfSolids(')
    if oracle:
        code = code.replace('CollectSolidCandidates(point,radius);\n            foreach (int index in solidCandidates)\n            {\n                Obstacle obstacle=obstacles[index];', 'foreach (Obstacle obstacle in obstacles)\n            {\n                TraversalCounters.LegacyVisits++;')
        code = code.replace('WorldTraversal', 'FullScanTraversal').replace('WorldTerrain', 'FullScanTerrain')
    else:
        code = code.replace('Obstacle obstacle=obstacles[index];', 'Obstacle obstacle=obstacles[index];TraversalCounters.ActualVisits++;')
        code = code.replace('public static List<Vector3> FindPath(Vector3 from, Vector3 target, float radius = .45f)\n        {', 'public static List<Vector3> FindPath(Vector3 from, Vector3 target, float radius = .45f)\n        {\n            TraversalCounters.Searches++;')
    return code

with tempfile.TemporaryDirectory(prefix='emberfall-traversal-perf-') as directory:
    p = Path(directory)
    (p / 'Math.cs').write_text(math)
    (p / 'Program.cs').write_text(fixture)
    (p / 'World.cs').write_text(instrument(source))
    (p / 'Oracle.cs').write_text(instrument(source, True))
    (p / 'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>')
    (p / 'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    env = dict(os.environ, DOTNET_CLI_HOME=str(p / 'cli'), DOTNET_NOLOGO='1', DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1')
    dotnet = sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET', 'dotnet')
    subprocess.run([dotnet, 'run', '--project', str(p / 'Test.csproj'), '--configuration', 'Release'], env=env, check=True)
    # Reject missing revision invalidation, spatial omissions, obsolete periodic
    # search, ignored exact radius and the discarded Wander candidate path.
    controls = [
        ('World.cs', 'solidIndexRevision==revision', 'solidIndexRevision>=0', 'ArgumentOutOfRangeException'),
        ('World.cs', 'broadSolids.Add(i);continue;', 'continue;', 'spatial candidates preserve full-scan exact point clearance'),
        ('World.cs', 'Time.time>=refresh&&(waypoints.Count==0||goalMovement>.0001f)', 'Time.time>=refresh', 'stable wander detour builds exactly one path across ten seconds'),
        ('World.cs', 'builtRadius!=radius || ', '', 'exact radius change immediately invalidates cached path'),
        ('Program.cs', 'if(!route.TryPlan(transform.position,candidate,NavigationRadius))continue;', 'if(!WorldTraversal.HasGroundPath(transform.position,candidate,NavigationRadius)&&WorldTraversal.FindPath(transform.position,candidate,NavigationRadius).Count==0)continue;', 'actual Wander retains candidate path for same-frame Direction'),
    ]
    for file, old, new, expected in controls:
        original = (p / file).read_text()
        assert old in original, old
        (p / file).write_text(original.replace(old, new))
        built = subprocess.run([dotnet, 'build', str(p / 'Test.csproj'), '--no-restore', '--configuration', 'Release', '-v:q'], env=env, text=True, capture_output=True)
        assert built.returncode == 0, built.stdout + built.stderr
        run = subprocess.run([dotnet, str(p / 'bin/Release/net8.0/Test.dll')], env=env, text=True, capture_output=True)
        assert run.returncode and expected in run.stdout + run.stderr, run.stdout + run.stderr
        print('PASS compiled negative control: ' + expected)
        (p / file).write_text(original)

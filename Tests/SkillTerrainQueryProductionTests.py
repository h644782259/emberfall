#!/usr/bin/env python3
"""Compare production skill height/face queries with the unchanged full-scan math.
Counters and allocation evidence describe CPU work, not device frame rate.
"""
from pathlib import Path
import os, subprocess, sys, tempfile

root = Path(__file__).resolve().parents[1]
source = (root / 'Assets/Scripts/World/WorldTraversal.cs').read_text()

def member(code, marker):
    start = code.index(marker)
    end = code.index('{', start) + 1
    depth = 1
    while depth:
        depth += (code[end] == '{') - (code[end] == '}')
        end += 1
    return code[start:end]

def instrument(code, oracle=False):
    for marker in ['public static float SurfaceHeight(', 'public static bool HasClearVisualTriangle(', 'private static bool ClearAtHeight(', 'private static bool OpenSightBounds(']:
        old = member(code, marker)
        changed = old
        if oracle:
            start = changed.index('            CollectSolidCandidates')
            end = changed.index('            foreach(int index in solidCandidates)', start)
            changed = changed[:start] + changed[end:]
            changed = changed.replace('foreach(int index in solidCandidates)', 'foreach(var obstacle in obstacles)')
            changed = changed.replace('Obstacle obstacle=obstacles[index];', '')
            changed = changed.replace('var o=obstacles[index];', 'var o=obstacle;')
            # SurfaceHeight's lazy terrain locals must remain defined.
            if 'SurfaceHeight(' in marker:
                changed = changed.replace('foreach(var obstacle in obstacles)', 'bool terrainSampled=false;float terrainHeight=0;\n            foreach(var obstacle in obstacles)')
        loop = 'foreach(var obstacle in obstacles)' if oracle else 'foreach(int index in solidCandidates)'
        changed = changed.replace(loop + '\n            {', loop + '\n            {\n                QueryCounters.' + ('Legacy' if oracle else 'Actual') + '++;')
        code = code.replace(old, changed)
    code = code.replace('private static bool ClearAtHeight(', 'public static bool ClearAtHeight(')
    if oracle:
        code = code.replace('(t.Terrain?terrainHeight:0)', '(t.Terrain?WorldTerrain.Height(point):0)')
        code = code.replace('WorldTraversal', 'FullScanTraversal').replace('WorldTerrain', 'FullScanTerrain')
    return code

math = (root / 'Tests/DestructibleTraversalTests.cs').read_text()
math = 'using System;using UnityEngine;' + math[math.index('namespace Emberfall'):]
math = math.replace('public static float Abs(float x)', 'public static float Sqrt(float x)=>(float)Math.Sqrt(x);public static float Exp(float x)=>(float)Math.Exp(x);public static float Abs(float x)')
fixture = r'''
using System;using System.Collections.Generic;using Emberfall;using UnityEngine;
public static class QueryCounters{public static long Actual,Legacy;}
class Program{
 static int checks;static void C(bool v,string m){checks++;if(!v)throw new Exception(m);}
 static void Reset(float arena=80){WorldTraversal.Reset(ZoneKind.Wilderness);FullScanTraversal.Reset(ZoneKind.Wilderness);WorldTraversal.SetArenaRadius(arena);FullScanTraversal.SetArenaRadius(arena);WorldTerrain.ReliefEnabled=FullScanTerrain.ReliefEnabled=true;WorldTerrain.Configure(ZoneKind.Wilderness,73);FullScanTerrain.Configure(ZoneKind.Wilderness,73);}
 static void Height(Vector3 p,float r){float a=WorldTraversal.SurfaceHeight(p,r),b=FullScanTraversal.SurfaceHeight(p,r);C(a==b||(float.IsNaN(a)&&float.IsNaN(b)),"surface exactly matches full scan");C(WorldTraversal.ClearAtHeight(p,r,p.y+.9f)==FullScanTraversal.ClearAtHeight(p,r,p.y+.9f),"height collision exactly matches full scan");C(WorldTraversal.HasLineOfSight(p,p+new Vector3(3,0,2))==FullScanTraversal.HasLineOfSight(p,p+new Vector3(3,0,2)),"sight exactly matches full scan");}
 static void Face(Vector3 o,Vector3 a,Vector3 b,Vector3 c){C(WorldTraversal.HasClearVisualTriangle(o,a,b,c)==FullScanTraversal.HasClearVisualTriangle(o,a,b,c),"visual face exactly matches full scan");}
 static void Box(Vector3 p,Vector2 s){WorldTraversal.AddBox(p,s);FullScanTraversal.AddBox(p,s);}
 static void Circle(Vector3 p,float r){WorldTraversal.AddCircle(p,r);FullScanTraversal.AddCircle(p,r);}
 static void Main(){Reset();var rng=new System.Random(47153);var handles=new List<WorldTraversal.ObstacleHandle>();var refs=new List<FullScanTraversal.ObstacleHandle>();
  for(int i=0;i<1200;i++){var p=new Vector3((float)(rng.NextDouble()*140-70),0,(float)(rng.NextDouble()*140-70));var size=new Vector2(1.2f+(float)rng.NextDouble()*2,1.2f+(float)rng.NextDouble()*2);
   if(i%3==0){handles.Add(WorldTraversal.AddPlatform(p,size,.2f+(i%8)*.1f));refs.Add(FullScanTraversal.AddPlatform(p,size,.2f+(i%8)*.1f));}
   else if(i%3==1){WorldTraversal.AddJumpPlatform(p,size.x,.7f);FullScanTraversal.AddJumpPlatform(p,size.x,.7f);}else Box(p,size);
  }
  for(int i=0;i<20;i++){float h=.005f*i;WorldTraversal.AddSurfaceBox(Vector3.zero,new Vector2(6,6),h,true);FullScanTraversal.AddSurfaceBox(Vector3.zero,new Vector2(6,6),h,true);}
  WorldTraversal.AddSurfaceTriangle(new Vector3(-3,.2f,-3),new Vector3(3,.8f,-3),new Vector3(0,.5f,3));FullScanTraversal.AddSurfaceTriangle(new Vector3(-3,.2f,-3),new Vector3(3,.8f,-3),new Vector3(0,.5f,3));
  QueryCounters.Actual=QueryCounters.Legacy=0;
  for(int i=0;i<12000;i++){var p=new Vector3((float)(rng.NextDouble()*140-70),(float)rng.NextDouble()*2,(float)(rng.NextDouble()*140-70));float r=i%17==0?-.8f:.01f+(float)rng.NextDouble()*1.2f;Height(p,r);
   var a=p+new Vector3((float)rng.NextDouble()*4,0,(float)rng.NextDouble()*4);var b=p+new Vector3(-2,.2f,3);var c=p+new Vector3(2,-.1f,-3);Face(p,a,b,c);}
  long actual=QueryCounters.Actual,legacy=QueryCounters.Legacy;C(actual*8<legacy,"local skill queries reduce exact obstacle visits at least eightfold");Console.WriteLine("SKILL TERRAIN exact visits actual="+actual+" fullScan="+legacy+" reduction="+((double)legacy/actual).ToString("F1")+"x");
  // Box support expands for negative radii; circle support and exact tangencies retain their old rules.
  foreach(float x in new[]{-8f,-4f,0f,4f,8f})foreach(float z in new[]{-8f,-4f,0f,4f,8f})foreach(float e in new[]{-.0001f,0f,.0001f})foreach(float r in new[]{-.9f,0f,.01f,.45f,2f}){Height(new Vector3(x+e,0,z),r);var p=new Vector3(x+e,.7f,z);Face(p,p+new Vector3(.04f,0,0),p+new Vector3(0,0,4),p+new Vector3(-4,0,0));}
  for(int i=0;i<handles.Count;i+=2){C(WorldTraversal.RemoveDynamicObstacle(handles[i])==FullScanTraversal.RemoveDynamicObstacle(refs[i]),"dynamic removal invalidates height and visual candidates");Height(new Vector3(i%20-10,0,i%17-8),.01f);Face(Vector3.zero,new Vector3(6,0,3),new Vector3(-2,0,5),new Vector3(1,.8f,-4));}
  Reset(100000);Box(Vector3.zero,new Vector2(10000,10000));Circle(new Vector3(100000,0,0),99990);Circle(new Vector3(float.NaN,0,0),1);Box(new Vector3(1,0,1),new Vector2(-2,-2));WorldTraversal.AddJumpPlatform(new Vector3(4100,0,0),2,.8f);FullScanTraversal.AddJumpPlatform(new Vector3(4100,0,0),2,.8f);
  foreach(var p in new[]{Vector3.zero,new Vector3(4100,0,0),new Vector3(-4100,0,-4),new Vector3(float.NaN,0,0),new Vector3(float.PositiveInfinity,0,0)})foreach(float r in new[]{.01f,-.8f,float.NaN,float.PositiveInfinity}){Height(p,r);Face(p,p+new Vector3(1,0,0),p+new Vector3(0,0,3),p+new Vector3(-2,0,-1));}
  Face(Vector3.zero,new Vector3(5000,0,0),new Vector3(0,0,5000),new Vector3(-5000,0,-5000));
  Reset();WorldTraversal.AddPlatform(Vector3.zero,new Vector2(4,4),.8f);FullScanTraversal.AddPlatform(Vector3.zero,new Vector2(4,4),.8f);WorldTraversal.BeginSceneBuild(1.5f);FullScanTraversal.BeginSceneBuild(1.5f);Box(new Vector3(4,0,-4),new Vector2(1,3));WorldTraversal.EndSceneBuild();FullScanTraversal.EndSceneBuild();
  foreach(float h in new[]{0f,.8f,1.5f}){var p=new Vector3(0,h,0);Height(p,.01f);Face(p,p+new Vector3(1,0,0),p+new Vector3(0,0,1),p+new Vector3(-1,0,-1));}
  // Warm both shared buffers before measuring repeated production contact queries.
  for(int i=0;i<100;i++){WorldTraversal.SurfaceHeight(Vector3.zero,.01f);WorldTraversal.HasClearVisualTriangle(Vector3.zero,new Vector3(2,0,0),new Vector3(0,0,2),new Vector3(-2,0,-2));}
  long bytes=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<10000;i++){WorldTraversal.SurfaceHeight(Vector3.zero,.01f);WorldTraversal.HasClearVisualTriangle(Vector3.zero,new Vector3(2,0,0),new Vector3(0,0,2),new Vector3(-2,0,-2));}bytes=GC.GetAllocatedBytesForCurrentThread()-bytes;C(bytes==0,"warm skill contact queries allocate zero bytes");Console.WriteLine("PASS "+checks+" terrain/triangle full-scan equivalence, boundary, platform, dynamic invalidation and fallback assertions; warm allocations="+bytes);
 }
}
'''
with tempfile.TemporaryDirectory(prefix='emberfall-skill-terrain-') as directory:
    p = Path(directory)
    for name, code in [('Math.cs', math), ('Program.cs', fixture), ('World.cs', instrument(source)), ('Oracle.cs', instrument(source, True))]:
        (p / name).write_text(code)
    (p / 'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>')
    (p / 'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    env = dict(os.environ, DOTNET_CLI_HOME=str(p / 'cli'), DOTNET_NOLOGO='1', DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1')
    dotnet = sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET', 'dotnet')
    subprocess.run([dotnet, 'run', '--project', str(p / 'Test.csproj'), '--configuration', 'Release'], env=env, check=True)
    original = (p / 'World.cs').read_text()
    # A compiled omission of all regular candidates must be rejected by the oracle.
    old = 'solidMarks[i]=solidQuery;solidCandidates.Add(i);}'
    assert old in original
    (p / 'World.cs').write_text(original.replace(old, 'solidMarks[i]=solidQuery;}'))
    built = subprocess.run([dotnet, 'build', str(p / 'Test.csproj'), '--no-restore', '--configuration', 'Release', '-v:q'], env=env, text=True, capture_output=True)
    assert built.returncode == 0, built.stdout + built.stderr
    run = subprocess.run([dotnet, str(p / 'bin/Release/net8.0/Test.dll')], env=env, text=True, capture_output=True)
    assert run.returncode and 'exactly matches full scan' in run.stdout + run.stderr, run.stdout + run.stderr
    print('PASS compiled missing-candidate negative control')

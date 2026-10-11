#!/usr/bin/env python3
"""Execute actual worn-floor mesh generation, registration and traversal certificates.
Engine doubles store numeric mesh data/transforms only; no native render claim.
"""
from pathlib import Path
import os,sys,tempfile,subprocess,re
r=Path(__file__).resolve().parents[1]
def member(s,key):
 a=s.index(key);b=s.index('{',a)+1;n=1
 while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
source=(r/'Assets/Scripts/World/WorldBuilder.GroundSupport.cs').read_text()
registration=member(source,'private static void RegisterGroundSurfaceSupport(').replace('private static','public static')
paving=(r/'Assets/Scripts/World/NaturalPavingSurface.cs').read_text()
# Execute the unchanged production geometry prefix through final mesh winding.
# Material binding and GameObject creation remain separate native evidence.
paving=paving[paving.index('            Vector2 center='):paving.index('resources.Own(mesh);')]+ 'return mesh;'
paving='public static Mesh Paving(string name,Vector2[] outline,float height){'+paving+'}'
math=(r/'Tests/DestructibleTraversalTests.cs').read_text();math='using System;using UnityEngine;'+math[math.index('namespace Emberfall'):]
math=math.replace('public static float FindSafeBlinkDistance(float d,Func<float,bool>a,Func<float,bool>b){throw new NotSupportedException("Blink is outside this fixture");}',member((r/'Assets/Scripts/Combat/PlayerUpgradeRules.cs').read_text(),'public static float FindSafeBlinkDistance('))
math=math.replace('public static Vector3 zero=>','public static Vector3 Cross(Vector3 a,Vector3 b)=>new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);public static Vector3 zero=>')
math=math.replace('public float sqrMagnitude=>x*x+y*y;', 'public float sqrMagnitude=>x*x+y*y;public float magnitude=>(float)Math.Sqrt(sqrMagnitude);public static Vector2 zero=>new Vector2();public static Vector2 operator +(Vector2 a,Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y);public static Vector2 operator /(Vector2 a,float b)=>a*(1/b);public static Vector2 Lerp(Vector2 a,Vector2 b,float t)=>a+(b-a)*t;public static float Distance(Vector2 a,Vector2 b)=>(a-b).magnitude;')
math=math.replace('public static float Abs(float x)', 'public static float Lerp(float a,float b,float t)=>a+(b-a)*Mathf.Clamp01(t);public static float SmoothStep(float a,float b,float t){t=Clamp01(t);return Lerp(a,b,t*t*(3-2*t));}public static float Abs(float x)')
fixture=r'''
namespace UnityEngine{
 public struct Color{public float r,g,b,a;public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color white=>new Color(1,1,1,1);}
 public class Material{public bool terrain;public bool HasProperty(string s)=>s=="_TerrainFollow";public float GetFloat(string s)=>terrain?1:0;}
 public class Renderer{public bool enabled=true;public Material sharedMaterial=new Material();}
 public class Mesh{public string name;public bool isReadable=true;public Vector3[] vertices;public int[] triangles;public void SetVertices(List<Vector3> value){vertices=value.ToArray();}public void SetTriangles(List<int> value,int submesh){triangles=value.ToArray();}public void SetUVs(int channel,List<Vector2> value){}public void SetColors(List<Color> value){}public void RecalculateNormals(){}public void RecalculateBounds(){}}
 public class GameObject{public string name;}
 public class MeshFilter{public GameObject gameObject=new GameObject();public Mesh sharedMesh;public Transform transform=new Transform();public Renderer renderer=new Renderer();public T GetComponent<T>()where T:class=>renderer as T;}
 public class Transform{public Vector3 position,scale=new Vector3(1,1,1);public float angle;public MeshFilter[] filters=new MeshFilter[0];public Vector3 TransformPoint(Vector3 p){p=new Vector3(p.x*scale.x,p.y*scale.y,p.z*scale.z);float a=angle*Mathf.PI/180;return position+new Vector3(p.x*Mathf.Cos(a)+p.z*Mathf.Sin(a),p.y,-p.x*Mathf.Sin(a)+p.z*Mathf.Cos(a));}public T[] GetComponentsInChildren<T>()where T:class=>filters as T[];}
}
namespace Emberfall{public static class SurfaceBuilder{REG PAVING}}
class Program{
 static int n;static void C(bool v,string m){n++;if(!v)throw new Exception(m);}static bool Near(float a,float b)=>Math.Abs(a-b)<.0005f;
 static void Flat(){WorldTraversal.Reset(ZoneKind.Wilderness);WorldTerrain.Configure(ZoneKind.Wilderness,1);}
 static MeshFilter Floor(string name,Vector3 at,Vector3 size,bool terrain=false){return new MeshFilter{gameObject=new GameObject{name=name},sharedMesh=new Mesh{vertices=new[]{new Vector3(-.5f,.5f,-.5f),new Vector3(.5f,.5f,-.5f),new Vector3(.5f,.5f,.5f),new Vector3(-.5f,.5f,.5f),new Vector3(-.5f,-.5f,-.5f)},triangles=new[]{0,2,1,0,3,2}},transform=new Transform{position=at,scale=size},renderer=new Renderer{sharedMaterial=new Material{terrain=terrain}}};}
 static MeshFilter WornFloor(string name,Vector3 at,Vector3 size){Mesh mesh=SurfaceBuilder.Paving(name,new[]{new Vector2(at.x-size.x*.5f,at.z-size.z*.5f),new Vector2(at.x+size.x*.5f,at.z-size.z*.5f),new Vector2(at.x+size.x*.5f,at.z+size.z*.5f),new Vector2(at.x-size.x*.5f,at.z+size.z*.5f)},at.y+size.y*.5f);Vector3[] vertices=mesh.vertices;for(int i=0;i<vertices.Length;i++)vertices[i]-=at;mesh.vertices=vertices;return new MeshFilter{gameObject=new GameObject{name=name},sharedMesh=mesh,transform=new Transform{position=at}};}
 static bool Closed(Mesh mesh){var edges=new Dictionary<(int,int),(int count,int direction)>();double volume=0;for(int i=0;i<mesh.triangles.Length;i+=3){int a=mesh.triangles[i],b=mesh.triangles[i+1],c=mesh.triangles[i+2];volume+=Vector3.Dot(mesh.vertices[a],Vector3.Cross(mesh.vertices[b],mesh.vertices[c]))/6.0;foreach(var pair in new[]{(a,b),(b,c),(c,a)}){var key=pair.Item1<pair.Item2?pair:(pair.Item2,pair.Item1);edges.TryGetValue(key,out var old);edges[key]=(old.count+1,old.direction+(pair.Item1<pair.Item2?1:-1));}}foreach(var value in edges.Values)if(value.count!=2||value.direction!=0)return false;return volume>0;}
 static void Main(){
  Flat();string nav=WorldTraversal.NavigationMapKey;
  var court=WornFloor("Caravan resting court",new Vector3(0,.025f,-10),new Vector3(7.4f,.035f,5.8f));var rug=WornFloor("Camp rug",new Vector3(0,.047f,-10),new Vector3(3.8f,.018f,2.7f));SurfaceBuilder.RegisterGroundSurfaceSupport(new Transform{filters=new[]{court,rug}});
  C(court.sharedMesh.vertices.Length>100&&rug.sharedMesh.vertices.Length>100&&Closed(court.sharedMesh)&&Closed(rug.sharedMesh),"actual NaturalPavingSurface court and rug remain closed positively wound volumes");
  C(Near(WorldTraversal.SurfaceHeight(new Vector3(0,0,-10),.01f),.056f),"actual Camp rug top .056 supports grounded effects");C(Near(WorldTraversal.SurfaceHeight(new Vector3(2.55f,0,-10),.01f),.0425f),"actual resting court top .0425 supports grounded effects");C(Near(WorldTraversal.SurfaceHeight(new Vector3(4,0,-10),.01f),0),"outside actual floor footprint remains terrain");
  float wornEdge=WorldTraversal.SurfaceHeight(new Vector3(3,0,-10),.01f);C(wornEdge>0&&wornEdge<.0425f,"actual worn court shoulder lowers support to match descending mesh rather than cube top");
  C(nav==WorldTraversal.NavigationMapKey&&WorldTraversal.IsWalkable(new Vector3(0,0,-10),1.8f),"thin floors do not become navigation blockers or change nav identity");
  C(Near(WorldTraversal.Move(new Vector3(0,.056f,-10),new Vector3(.3f,0,0)).y,.056f)&&Near(WorldTraversal.NearestWalkable(new Vector3(0,0,-10)).y,.056f),"actual movement and spawn feet use rendered floor top");float fall=0;C(Near(WorldTraversal.MovePlayer(new Vector3(0,.056f,-10),new Vector3(.1f,0,0),.016f,ref fall).y,.056f)&&Near(fall,0),"standing on thin rug does not enter platform-fall branch");
  C(Near(WorldTraversal.PhysicalSurfaceHeight(new Vector3(0,.10f,-10),.02f,.10f),.056f),"fragment bounce support uses same actual rug plane");Vector3 blink;C(WorldTraversal.TryResolveBlink(new Vector3(0,.056f,-10),new Vector3(1,0,0),1,.45f,22,out blink)&&Near(blink.y,.056f),"blink arrival on rug is grounded to rendered top");
  Flat();WorldTraversal.BeginSceneBuild(1.25f);WorldTraversal.AddSurfaceBox(new Vector3(5,0,-3),new Vector2(2,4),.08f);WorldTraversal.EndSceneBuild();C(Near(WorldTraversal.SurfaceHeight(new Vector3(6.25f,0,-3.75f),.01f),.08f)&&Near(WorldTraversal.SurfaceHeight(new Vector3(4.9f,0,-3.75f),.01f),0),"authored surface box scope scales xz once and leaves top height");
  Flat();WorldTraversal.BeginSceneBuild(1.25f);var tile=WornFloor("Processional paving joint",new Vector3(10,.03f,6),new Vector3(3,.02f,1));tile.transform.angle=45;SurfaceBuilder.RegisterGroundSurfaceSupport(new Transform{filters=new[]{tile}});WorldTraversal.EndSceneBuild();C(Near(WorldTraversal.SurfaceHeight(new Vector3(10,0,6),.01f),.04f),"completed rotated tile uses true world transform without duplicate encounter scale");C(Near(WorldTraversal.SurfaceHeight(new Vector3(11.4f,0,6),.01f),0),"rotated top triangles do not fill their bounding-box corners");
  Flat();WorldTraversal.AddSurfaceTriangle(new Vector3(-2,.02f,-2),new Vector3(2,.10f,-2),new Vector3(0,.06f,2));C(Near(WorldTraversal.SurfaceHeight(Vector3.zero,.01f),.06f),"sloped thin surface interpolates actual vertex heights");
  WorldTerrain.Configure(ZoneKind.Wilderness,0);float relief=WorldTerrain.Height(new Vector3(-13,0,-11));var path=Floor("Woodland branch path",new Vector3(-13,.022f,-11),new Vector3(2,.02f,2),true);SurfaceBuilder.RegisterGroundSurfaceSupport(new Transform{filters=new[]{path}});C(Near(WorldTraversal.SurfaceHeight(new Vector3(-13,0,-11),.01f),relief+.032f),"thin support follows exact shader terrain once");
  Flat();var shelf=Floor("Archive gallery floor",new Vector3(0,4,0),new Vector3(5,.06f,5));var furniture=Floor("Camp rug shelf",new Vector3(3,1,0),new Vector3(2,2,2));SurfaceBuilder.RegisterGroundSurfaceSupport(new Transform{filters=new[]{shelf,furniture}});C(Near(WorldTraversal.SurfaceHeight(Vector3.zero,.01f),0)&&Near(WorldTraversal.SurfaceHeight(new Vector3(3,0,0),.01f),0),"high gallery and tall furniture never become low floor supports");
  WorldTraversal.AddSurfaceTriangle(new Vector3(float.NaN,.05f,0),new Vector3(1,.05f,0),new Vector3(0,.05f,1));C(Near(WorldTraversal.SurfaceHeight(Vector3.zero,.01f),0),"invalid support vertices are rejected");
  Flat();WorldTraversal.AddPlatform(Vector3.zero,new Vector2(3,3),.8f);Vector3 origin=new Vector3(0,.81f,0),a=new Vector3(-.6f,.81f,.2f),b=new Vector3(.6f,.81f,.2f),c=new Vector3(0,.81f,.9f);
  C(WorldTraversal.HasClearVisualTriangle(origin,a,b,c),"grounded visual on registered platform top is not clipped as a wall");c.y=.6f;C(!WorldTraversal.HasClearVisualTriangle(origin,a,b,c),"one low face vertex still collides with platform side");c.y=.81f;WorldTraversal.AddBox(new Vector3(0,0,.5f),new Vector2(.2f,.2f));C(!WorldTraversal.HasClearVisualTriangle(origin,a,b,c),"other cover remains solid above supported floor");
  Flat();WorldTraversal.AddPlatform(Vector3.zero,new Vector2(3,3),.8f);C(!WorldTraversal.HasClearVisualTriangle(Vector3.zero,new Vector3(-.6f,0,.2f),new Vector3(.6f,0,.2f),new Vector3(0,0,.9f)),"low-origin closed fan cannot ignore raised platform");
  Flat();C(Near(WorldTraversal.SurfaceHeight(new Vector3(0,0,-10),.01f),0),"world reset clears prior thin supports");
  Console.WriteLine("PASS "+n+" production mesh-top/rotated/terrain/actor/debris/blink/no-nav-block/platform-cover checks");
 }
}
'''.replace('REG',registration).replace('PAVING',paving)
# Fixture dimensions above are guarded against the actual final floor authoring.
builder=(r/'Assets/Scripts/World/WorldBuilder.cs').read_text()
assert 'new Vector3(0,.025f,-10), new Vector3(7.4f,.035f,5.8f)' in builder
assert 'new Vector3(0,.047f,-10), new Vector3(3.8f,.018f,2.7f)' in builder
assert builder.index('RegisterGroundSurfaceSupport(root.transform);')>builder.index('BuildNaturalGroundDetail(geometry,resources,dungeon,hub);')
with tempfile.TemporaryDirectory(prefix='emberfall-ground-support-') as d:
 p=Path(d);(p/'Math.cs').write_text(math);(p/'Program.cs').write_text('using System;using System.Collections.Generic;using Emberfall;using UnityEngine;'+fixture);(p/'WorldTraversal.cs').write_text((r/'Assets/Scripts/World/WorldTraversal.cs').read_text())
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1');dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
 subprocess.run([dotnet,'run','--project',str(p/'Test.csproj'),'--configuration','Release'],env=env,check=True)
 controls=[
  ('WorldTraversal.cs','height=Mathf.Max(height,top);','height=height;','actual Camp rug top .056 supports grounded effects'),
  ('WorldTraversal.cs','A=a,B=b,C=c,','A=ScenePoint(a),B=ScenePoint(b),C=ScenePoint(c),','completed rotated tile uses true world transform without duplicate encounter scale'),
  ('WorldTraversal.cs','obstacle.Height>0&&VisualAboveSupport(origin,obstacle)','false&&obstacle.Height>0&&VisualAboveSupport(origin,obstacle)','grounded visual on registered platform top is not clipped as a wall'),
  ('WorldTraversal.cs','&&VisualAboveSupport(c,obstacle)','', 'one low face vertex still collides with platform side'),
  ('WorldTraversal.cs','+(t.Terrain?terrainHeight:0)','', 'thin support follows exact shader terrain once'),
 ]
 for file,old,new,why in controls:
  src=(p/file).read_text();assert old in src;(p/file).write_text(src.replace(old,new));built=subprocess.run([dotnet,'build',str(p/'Test.csproj'),'--no-restore','--configuration','Release','-v:q'],env=env,text=True,capture_output=True)
  assert built.returncode==0,built.stdout+built.stderr
  run=subprocess.run([dotnet,str(p/'bin/Release/net8.0/Test.dll')],env=env,text=True,capture_output=True);assert run.returncode and why in run.stderr,run.stdout+run.stderr
  print('PASS compiled negative control: '+why);(p/file).write_text(src)

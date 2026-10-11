#!/usr/bin/env python3
"""Execute production scenery bound registration and real traversal; numeric Unity doubles."""
import os
import subprocess
import sys
import tempfile
from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "Assets/Scripts/World/WorldBuilder.cs").read_text()
start = source.index("        private static WorldTraversal.ObstacleHandle RegisterSceneryBox(")
end = source.index("        private static void Tree(", start)
helpers = source[start:end]
chapter_source = (root / "Assets/Scripts/World/ChapterRoomGeometry.cs").read_text()
def member(text, key):
    start = text.index(key)
    end = text.index("{", start) + 1
    depth = 1
    while depth:
        depth += (text[end] == "{") - (text[end] == "}")
        end += 1
    return text[start:end]
chapter_register = member(chapter_source, "        public static WorldTraversal.ObstacleHandle[] Register(")
branch = member((root / "Assets/Scripts/World/WorldBuilder.NaturalDetail.cs").read_text(), "            internal void Branch(")
chapter_types = member(chapter_source, "    public struct ChapterObstacle") + member(chapter_source, "    public sealed class ChapterRoomPlan")
math = (root / "Tests/DestructibleTraversalTests.cs").read_text()
math = "using System;using UnityEngine;" + math[math.index("namespace Emberfall"):]
math = math.replace("public static Vector3 zero=>", "public static Vector3 forward=>new Vector3(0,0,1);public static Vector3 Cross(Vector3 a,Vector3 b)=>new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);public static Vector3 zero=>")
math = math.replace("public const float PI=", "public static float Lerp(float a,float b,float t)=>a+(b-a)*t;public const float PI=")
fixture = r'''
namespace UnityEngine{
 public struct Color{public static Color white=>new Color();}
 public struct Bounds{public Vector3 center,size;public Bounds(Vector3 c,Vector3 s){center=c;size=s;}public Vector3 min=>center-size*.5f;public Vector3 max=>center+size*.5f;public void Encapsulate(Vector3 p){var a=min;var b=max;var lo=new Vector3(Math.Min(a.x,p.x),Math.Min(a.y,p.y),Math.Min(a.z,p.z));var hi=new Vector3(Math.Max(b.x,p.x),Math.Max(b.y,p.y),Math.Max(b.z,p.z));center=(lo+hi)*.5f;size=hi-lo;}}
 public class Transform{public Vector3 position,scale=new Vector3(1,1,1);public Vector3 lossyScale=>scale;public bool quarterTurn;public float yaw;public Vector3 TransformPoint(Vector3 p){p=new Vector3(p.x*scale.x,p.y*scale.y,p.z*scale.z);if(quarterTurn)p=new Vector3(-p.z,p.y,p.x);float a=yaw*Mathf.PI/180;p=new Vector3(p.x*Mathf.Cos(a)+p.z*Mathf.Sin(a),p.y,-p.x*Mathf.Sin(a)+p.z*Mathf.Cos(a));return position+p;}}
 public class Material{public bool terrain;public bool HasProperty(string p)=>p=="_TerrainFollow";public float GetFloat(string p)=>terrain?1:0;}
 public class Renderer{public bool enabled=true;public Material sharedMaterial=new Material();public Bounds bounds=new Bounds(Vector3.zero,new Vector3(100,100,100));}
 public class Mesh{public string name;public bool isReadable=true;public Vector3[] vertices;public Bounds bounds;}
 public class MeshFilter{public Mesh sharedMesh;public Transform transform=new Transform();public Renderer renderer=new Renderer();public T GetComponent<T>()where T:class=>renderer as T;}
 public class GameObject{public MeshFilter[] filters;public T[] GetComponentsInChildren<T>()where T:class=>filters as T[];}
}
namespace Emberfall{public static partial class WorldBuilder{
 HELPERS
 public static void Box(Vector3 at,Vector2 nav,GameObject body){RegisterSceneryBox(at,nav,body);}
 public static void Circle(Vector3 at,float nav,GameObject body){RegisterSceneryCircle(at,nav,body);}
 sealed class TrunkFixture{
 internal readonly System.Collections.Generic.List<Vector3> vertices=new System.Collections.Generic.List<Vector3>();readonly System.Collections.Generic.List<int> triangles=new System.Collections.Generic.List<int>();
 void Vertex(Vector3 p,Color c,Vector2 uv=default(Vector2)){vertices.Add(p);}void Face(int a,int b,int c){triangles.Add(a);triangles.Add(b);triangles.Add(c);}
 BRANCH
 }
 public static GameObject Trunk(Vector3 at,float size,Transform actual,bool terrain){
  var geometry=new TrunkFixture();geometry.Branch(Vector3.zero,new Vector3(.06f,1.6f,0),.18f,.12f);geometry.Branch(new Vector3(.06f,1.6f,0),new Vector3(.10f,2.9f,.05f),.12f,.035f);if(geometry.vertices.Count!=172)throw new Exception("actual main trunk prefix remains 172 vertices");geometry.Branch(new Vector3(.06f,1.3f,0),new Vector3(3,3.3f,2),.065f,.025f);
  actual.position=at;actual.scale=new Vector3(actual.scale.x*size,actual.scale.y*size,actual.scale.z*size);var tree=new GameObject{filters=new[]{new MeshFilter{sharedMesh=new Mesh{name="Shared tapered tree wood",vertices=geometry.vertices.ToArray()},transform=actual,renderer=new Renderer{sharedMaterial=new Material{terrain=terrain}}}}};
  var h=WorldTraversal.AddDynamicCircle(at,.22f);if(!SetNaturalTreeTrunkBounds(h,tree))throw new Exception("production natural trunk helper accepts actual mesh prefix");return tree;
 }
}}
namespace Emberfall{public enum EnemyKind{Slime,Goblin,Wisp,Guardian}public sealed class SummonedCompanion{public enum Kind{Wolf,Spirit,Treant,Wisp}}public enum ChapterNode{ForestCourt,Redrock,StarPlatform}CHAPTER_TYPES public static class ChapterRoomGeometry{CHAPTER_REGISTER}}
class Program{
 static int n;static void C(bool ok,string message){n++;if(!ok)throw new Exception(message);}static bool Near(float a,float b)=>Math.Abs(a-b)<.001f;
 static GameObject Body(Vector3 at,Vector3 size,bool terrain=false,bool readable=true,bool turn=false){var box=new Bounds(Vector3.zero,size);var lo=box.min;var hi=box.max;var v=new Vector3[8];for(int i=0;i<8;i++)v[i]=new Vector3((i&1)==0?lo.x:hi.x,(i&2)==0?lo.y:hi.y,(i&4)==0?lo.z:hi.z);return new GameObject{filters=new[]{new MeshFilter{sharedMesh=new Mesh{vertices=v,bounds=box,isReadable=readable},transform=new Transform{position=at,quarterTurn=turn},renderer=new Renderer{sharedMaterial=new Material{terrain=terrain}}}}};}
 static void Flat(){WorldTraversal.Reset(ZoneKind.Wilderness);WorldTerrain.Configure(ZoneKind.Wilderness,1);}
 static void Main(){
  Flat();var shortRock=Body(new Vector3(0,.2f,0),new Vector3(.8f,.4f,.8f));WorldBuilder.Circle(Vector3.zero,.8f,shortRock);
  C(!WorldTraversal.IsWalkable(Vector3.zero,.1f),"finite rock keeps authored navigation footprint");
  C(!WorldTraversal.IsPhysicalSpaceClear(new Vector3(0,.2f,0),.05f),"actual low rock volume blocks physical contact");
  C(WorldTraversal.IsPhysicalSpaceClear(new Vector3(0,1,0),.05f),"flight above rock clears finite body despite huge renderer culling bounds");
  C(Near(WorldTraversal.PhysicalSurfaceHeight(new Vector3(0,.5f,0),.05f,.5f),.4f),"debris lands on real rock top");
  C(Near(WorldTraversal.SurfaceHeight(Vector3.zero,.05f),0),"rock does not invent climbable terrain");
  Flat();var rotated=Body(new Vector3(2,.5f,3),new Vector3(4,1,.4f),turn:true);rotated.filters[0].transform.scale=new Vector3(1.5f,2,1);WorldBuilder.Box(new Vector3(2,0,3),new Vector2(4,.4f),rotated);
  C(!WorldTraversal.IsPhysicalSpaceClear(new Vector3(2,.5f,5.8f),.02f),"rotated scaled geometry uses actual world transform");
  C(WorldTraversal.IsPhysicalSpaceClear(new Vector3(2.8f,.5f,3),.02f),"rotation does not retain unrotated invisible body");
  Flat();var unreadable=Body(new Vector3(0,.5f,0),new Vector3(.8f,1,.8f),readable:false);WorldBuilder.Box(Vector3.zero,new Vector2(.8f,.8f),unreadable);
  C(!WorldTraversal.IsPhysicalSpaceClear(new Vector3(0,.8f,0),.05f)&&WorldTraversal.IsPhysicalSpaceClear(new Vector3(0,1.3f,0),.05f),"unreadable imported mesh uses finite local geometry bounds");
  WorldTraversal.Reset(ZoneKind.Wilderness);WorldTerrain.Configure(ZoneKind.Wilderness,0,32);var hill=Body(new Vector3(-13,2,-11),new Vector3(.2f,4,.2f),terrain:true);WorldBuilder.Box(new Vector3(-13,0,-11),new Vector2(.2f,.2f),hill);
  C(!WorldTraversal.IsPhysicalSpaceClear(new Vector3(-13,5.8f,-11),.03f),"scenery collision follows shader terrain height at actual vertices");
  C(WorldTraversal.IsPhysicalSpaceClear(new Vector3(-13,6.3f,-11),.03f),"terrain-following top remains finite");
  C(WorldTraversal.PhysicalSurfaceHeight(new Vector3(-13,6.5f,-11),.03f,6.5f)>5.99f,"debris support includes terrain height without double offset");
  Flat();WorldBuilder.Trunk(Vector3.zero,2,new Transform(),false);
  C(!WorldTraversal.IsWalkable(new Vector3(.20f,0,0),.01f)&&WorldTraversal.IsWalkable(new Vector3(.26f,0,0),.01f),"tree navigation radius remains original .22 independent of visual scale");
  C(!WorldTraversal.IsPhysicalSpaceClear(new Vector3(.1f,4,0),.03f),"scaled tree trunk remains physically solid");
  C(WorldTraversal.IsPhysicalSpaceClear(new Vector3(1,4,0),.03f),"open branch and canopy space does not become solid wall");
  C(WorldTraversal.IsPhysicalSpaceClear(new Vector3(.1f,6.2f,0),.03f),"projectile can clear trunk top");
  foreach(float yaw in new[]{0,31,62,93,186})foreach(bool terrain in new[]{false,true}){
   if(terrain){WorldTraversal.Reset(ZoneKind.Wilderness);WorldTerrain.Configure(ZoneKind.Wilderness,0,32);}else Flat();
   Vector3 at=terrain?new Vector3(-13,0,-11):new Vector3(3,0,5);var actual=new Transform{yaw=yaw,scale=new Vector3(1.3f,1.7f,.8f)};var trunk=WorldBuilder.Trunk(at,1.4f,actual,terrain);var points=trunk.filters[0].sharedMesh.vertices;float top=float.NegativeInfinity;
   for(int i=0;i<172;i++){var vertex=actual.TransformPoint(points[i]);if(terrain)vertex.y+=WorldTerrain.Height(vertex);top=Math.Max(top,vertex.y);C(!WorldTraversal.IsPhysicalSpaceClear(vertex,.0001f),"actual main trunk bounds include rotated scaled original production vertices");}
   Vector3 axis=actual.TransformPoint(new Vector3(.05f,0,.025f));axis.y=top+1;C(WorldTraversal.IsPhysicalSpaceClear(axis,.03f),"actual trunk top stays finite for every rotation and terrain");C(Near(WorldTraversal.PhysicalSurfaceHeight(axis,.03f,axis.y),top),"debris finds highest actual trunk vertex after transform and terrain exactly once");
   C(WorldTraversal.IsPhysicalSpaceClear(new Vector3(at.x+1,top*.55f,at.z),.03f),"later branch geometry never expands the solid main trunk body");
  }
  for(int hub=1;hub<=2;hub++){
   Flat();for(int i=0;i<6;i++)WorldTraversal.AddBox(HubSettlementPlan.Building(hub,i),new Vector2(5.5f,5.5f));if(hub==1){WorldTraversal.AddBox(new Vector3(-7,0,2),new Vector2(1.4f,1.4f));WorldTraversal.AddBox(new Vector3(7,0,2),new Vector2(1.4f,1.4f));}else WorldTraversal.AddCircle(new Vector3(0,0,3),1.1f);string map=WorldTraversal.NavigationMapKey;
   Flat();var handles=HubSettlementPlan.RegisterTownNavigation(hub);C(map==WorldTraversal.NavigationMapKey&&handles.Length==(hub==1?8:7),"town navigation shapes/order remain identical after finite-bound ownership");
  }
  for(int index=0;index<2;index++){
   Flat();Vector3 at=new Vector3(index==0?-6:6,0,-16);WorldTraversal.AddCircle(at,.43f);WorldTraversal.AddBox(at+new Vector3(0,0,.65f),new Vector2(1.4f,.5f));string map=WorldTraversal.NavigationMapKey;
   Flat();var handles=HubSettlementPlan.RegisterNpcNavigation(index);C(map==WorldTraversal.NavigationMapKey&&handles.Length==2,"NPC body and role-prop navigation and interaction coordinates remain identical");
  }
  for(int layout=20;layout<=25;layout++){
   Flat();foreach(var wall in TacticalRoomGeometry.Walls(layout))WorldTraversal.AddBox(wall.Position,wall.Size);if(TacticalRoomGeometry.Flooded(layout))WorldTraversal.SetRiver(TacticalRoomGeometry.River(),3.2f,TacticalRoomGeometry.Bridge(layout));string map=WorldTraversal.NavigationMapKey;
   Flat();var handles=TacticalRoomGeometry.Register(layout);C(map==WorldTraversal.NavigationMapKey&&handles.Length==TacticalRoomGeometry.Walls(layout).Length,"all tactical layouts retain authored walls river and crossing navigation");
  }
  Flat();var chapter=new ChapterRoomPlan(ChapterNode.ForestCourt,0,123,new[]{new ChapterObstacle(new Vector3(-4,0,2),2,1.2f),new ChapterObstacle(new Vector3(7,0,5),new Vector2(2,4),1.8f)},new Vector3[0],new Vector3[0],1);
  WorldTraversal.AddCircle(new Vector3(-4,0,2),2);WorldTraversal.AddBox(new Vector3(7,0,5),new Vector2(2,4));string chapterMap=WorldTraversal.NavigationMapKey;
  Flat();var chapterHandles=ChapterRoomGeometry.Register(chapter);C(chapterMap==WorldTraversal.NavigationMapKey&&chapterHandles.Length==2,"chapter mixed island and retaining block navigation stays identical");
  Console.WriteLine("PASS "+n+" production scenery finite-volume/navigation/transform/terrain/debris checks");
 }
}
'''.replace("HELPERS", helpers).replace("BRANCH", branch).replace("CHAPTER_TYPES", chapter_types).replace("CHAPTER_REGISTER", chapter_register)

with tempfile.TemporaryDirectory(prefix="emberfall-scenery-physical-") as temporary:
    work = Path(temporary)
    (work / "Math.cs").write_text(math)
    for name in ["Assets/Scripts/Combat/BossScaleRules.cs","Assets/Scripts/World/ArenaSizeRules.cs"]:
        (work / Path(name).name).write_text((root / name).read_text())
    (work / "WorldTraversal.cs").write_text((root / "Assets/Scripts/World/WorldTraversal.cs").read_text())
    (work / "HubSettlementPlan.cs").write_text((root / "Assets/Scripts/World/HubSettlementPlan.cs").read_text())
    (work / "TacticalRoomGeometry.cs").write_text((root / "Assets/Scripts/World/TacticalRoomGeometry.cs").read_text())
    program = "using System;using Emberfall;using UnityEngine;" + fixture
    (work / "Program.cs").write_text(program)
    (work / "Test.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
    (work / "NuGet.Config").write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    env = dict(os.environ, DOTNET_CLI_HOME=str(work / "cli"), DOTNET_NOLOGO="1", DOTNET_SKIP_FIRST_TIME_EXPERIENCE="1")
    dotnet = sys.argv[1] if len(sys.argv) > 1 else os.environ.get("DOTNET", "dotnet")
    subprocess.run([dotnet, "run", "--project", str(work / "Test.csproj"), "--configuration", "Release"], env=env, check=True)
    for old, replacement, reason in [
        ("if(found)WorldTraversal.SetPhysicalBounds(obstacle,bounds.center,bounds.size);", "", "flight above rock clears finite body despite huge renderer culling bounds"),
        ("if(terrain)point.y+=WorldTerrain.Height(point);", "", "scenery collision follows shader terrain height at actual vertices"),
        ("for(int i=0;i<trunkVertices;i++)", "for(int i=0;i<vertices.Length;i++)", "open branch and canopy space does not become solid wall"),
    ]:
        assert old in program
        (work / "Program.cs").write_text(program.replace(old, replacement, 1))
        subprocess.run([dotnet, "build", str(work / "Test.csproj"), "--no-restore", "--configuration", "Release", "-v:q"], env=env, check=True, stdout=subprocess.DEVNULL)
        result = subprocess.run([dotnet, str(work / "bin/Release/net8.0/Test.dll")], env=env, text=True, capture_output=True)
        assert result.returncode and reason in result.stderr, result.stdout + result.stderr
        print("PASS compiled negative control: " + reason)

# Integration source evidence: helpers execute above; these assert final factory wiring.
assert "RegisterSceneryCircle(NavigationScenePoint(parent,p),NavigationSceneRadius(parent,scale*.82f),rock);" in source
tree_member = member(source, "        private static void Tree(")
assert tree_member.index("BuildBranchTree(parent, r, p, size, seed);") < tree_member.index("SetNaturalTreeTrunkBounds(obstacle,tree)")
assert "parent.GetChild(firstChild).gameObject" in tree_member and "WorldTraversal.AddDynamicCircle(NavigationScenePoint(parent,p),NavigationSceneRadius(parent,.22f))" in tree_member
assert "SetSceneryPhysicalBounds(obstacle,pedestal,column,capital,collar,binding);" in source
assert "RegisterSceneryBox(barricade*1.5f, barrierSize*1.5f, partition);" in source
assert "SetSceneryPhysicalBounds(obstacle,block);" in source
hubs = (root / "Assets/Scripts/World/WorldBuilder.Hubs.cs").read_text()
assert "SetSceneryPhysicalBounds(townObstacles[index],building);" in hubs
assert "SetSceneryPhysicalBounds(npcObstacles[0],actor.gameObject);SetSceneryPhysicalBounds(npcObstacles[1],station.gameObject);" in hubs
idle = (root / "Assets/Scripts/World/HubNpcIdle.cs").read_text()
if "Initialize(int kind,Transform actor,Transform face,Transform right,Transform left,Transform ornament)" in idle:
    assert "Initialize(index,actor,head,right,left,dial)" in hubs
    assert "actor.localPosition=p;" in hubs and "head.localPosition=Vector3.up*1.66f;" in hubs
    assert "NpcNaturalBodies.Apply(actor,index,Vector3.zero,r);" in hubs
    assert "detail.SetParent(head,true);" in hubs
else:
    assert "Initialize(index,right,left,dial)" in hubs
linked = (root / "Assets/Scripts/World/WorldBuilder.LinkedRooms.cs").read_text()
assert "RegisterSceneryBox(new Vector3(side*16,0,0),new Vector2(1.1f,29),outerWall);" in linked
assert "RegisterSceneryBox(new Vector3(x,0,z),new Vector2(8,1.2f),beam);" in linked
challenge = (root / "Assets/Scripts/World/WorldBuilder.ChallengeArenas.cs").read_text()
assert "RegisterSceneryBox(p,new Vector2(5,1.2f),cover);" in challenge
tactical = (root / "Assets/Scripts/World/WorldBuilder.TacticalRooms.cs").read_text()
assert "SetSceneryPhysicalBounds(wallObstacles[wallIndex++],solid);" in tactical
chapter_builder = (root / "Assets/Scripts/World/WorldBuilder.ChapterRooms.cs").read_text()
assert "SetSceneryPhysicalBounds(chapterObstacles[obstacleIndex++],solid);" in chapter_builder
assert source.index('NaturalSceneryMeshLibrary.Apply(rock,"Weathered rock"') < source.index("RegisterSceneryCircle(NavigationScenePoint(parent,p),NavigationSceneRadius(parent,scale*.82f),rock);")
print("PASS source wiring for final-mesh rocks/columns/partitions/river blocks/town/NPC/linked/quarry/tactical/chapter bounds")

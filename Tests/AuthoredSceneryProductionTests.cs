using System;using System.Linq;using System.Collections.Generic;using UnityEngine;using Emberfall;
namespace Emberfall {
 // This suite exercises the retained procedural fallback; resource readiness has separate coverage.
 public static class BlenderSceneryArt {public static GameObject Create(string n,Transform p,Vector3 at,WorldResources r)=>null;public static GameObject CreatePilotProp(string n,Transform p,Vector3 at)=>null;}

 public class WorldResources {public bool TerrainFollowing=>false;public List<Mesh> owned=new List<Mesh>();internal Material Material(Color c,bool e=false,VisualSurface s=VisualSurface.Stone)=>new Material(Shader.Find("test"));public void Own(Mesh m){owned.Add(m);}}
 public static class WorldTraversal {public sealed class ObstacleHandle{}public static float SceneBuildScale=>1;public static float SceneLocalArenaRadius=>22;public static int circles;public static float radius;public static void AddCircle(Vector3 p,float r){circles++;radius=r;}public static ObstacleHandle AddDynamicCircle(Vector3 p,float r){AddCircle(p,r);return new ObstacleHandle();}}
 // Material textures are not part of this procedural fallback geometry check.
 internal static class SurfaceTextureLibrary {public static void Apply(Material material,VisualSurface surface){}}
 public static class CameraOcclusionSurface {public static readonly HashSet<GameObject> marked=new HashSet<GameObject>();public static void Mark(GameObject o){marked.Add(o);}public static void MarkHierarchy(GameObject o){marked.Add(o);}}
 public static partial class WorldBuilder {
  // Explicit disabled resource boundaries; this suite owns procedural fallback only.
  static bool BuildNaturalTree(Transform parent,WorldResources r,Vector3 p,float size,int seed)=>false;
  static bool SetNaturalTreeTrunkBounds(WorldTraversal.ObstacleHandle obstacle,GameObject tree)=>false;
  // Native physical-volume assignment is outside the original circle/geometry assertions.
  static void SetSceneryLocalBounds(WorldTraversal.ObstacleHandle obstacle,Transform parent,Bounds local,bool terrain){}
  static Transform Region(Transform p,string n){var o=new GameObject(n);o.transform.SetParent(p,false);return o.transform;}
  static GameObject Primitive(Transform p,string n,PrimitiveType type,Vector3 pos,Vector3 size,Material m,bool cameraOccluder=false){var o=new GameObject(n);o.transform.SetParent(p,false);o.transform.localPosition=pos;o.transform.localScale=size;o.AddComponent<MeshFilter>().sharedMesh=ProceduralVisuals.Shape(type);if(cameraOccluder)CameraOcclusionSurface.Mark(o);return o;}
  public static void TestTree(Transform p,WorldResources r,Vector3 at,float size,int seed)=>Tree(p,r,at,size,seed);
  public static void TestRoof(Transform p,WorldResources r)=>BuildWorkshopRoof(p,r,Vector3.zero,1,new Material(Shader.Find("test")),new Material(Shader.Find("test")));
 }
}
class AuthoredSceneryProductionTests {
 static object Geometry(GameObject host,string name)=>new {name=name,parts=host.GetComponentsInChildren<MeshFilter>(false).Select(f=>new {name=f.transform.name,vertices=f.sharedMesh.vertices.Select(v=>{var w=f.transform.TransformPoint(v);return new[]{w.x,w.y,w.z};}).ToArray(),triangles=f.sharedMesh.triangles}).ToArray()};
 static int checks;static void Check(bool b,string why){checks++;if(!b)throw new Exception(why);}
 static void Main(){var signatures=new HashSet<string>();
 for(int seed=0;seed<3;seed++){var host=new GameObject("tree");var r=new WorldResources();WorldTraversal.circles=0;WorldBuilder.TestTree(host.transform,r,new Vector3(5,0,5),1,seed);
 Check(WorldTraversal.circles==1&&WorldTraversal.radius==.22f,"preserve exactly original trunk navigation footprint");Check(r.owned.Count==5,"tree owns exactly five leaf fan meshes");
 signatures.Add(string.Join("/",r.owned[0].vertices.Select(v=>v.ToString())));
 foreach(var m in r.owned){Check(m.vertices.Length==14&&m.triangles.Length==72,"bounded closed leaf fan topology");Check(m.triangles.All(i=>i>=0&&i<m.vertices.Length),"valid fan indices");}
 var fans=host.GetComponentsInChildren<MeshFilter>(false).Where(f=>f.transform.name=="Open canopy leaf fan").ToArray();Check(fans.Length==5&&CameraOcclusionSurface.marked.Contains(host.transform.Find("Branching open canopy tree").gameObject)&&fans.All(f=>!CameraOcclusionSurface.marked.Contains(f.gameObject)),"leaf fans belong to one logical whole-tree occlusion entry");Check(fans.Select(f=>f.transform.position.y).Distinct().Count()==5,"canopies occupy five distinct branch levels");
 Check(host.GetComponentsInChildren<Transform>(false).Count(t=>t.name=="Ascending branch"||t.name=="Exposed branch fork")==10,"branches actually fork beneath open crown");
 }
 Check(signatures.Count==3,"three authored canopy variants differ geometrically");var distant=new GameObject("outside");WorldTraversal.circles=0;WorldBuilder.TestTree(distant.transform,new WorldResources(),new Vector3(23,0,0),1,-2);Check(WorldTraversal.circles==0,"outside world tree adds no new obstacle");
 var house=new GameObject("house");WorldBuilder.TestRoof(house.transform,new WorldResources());var parts=house.GetComponentsInChildren<Transform>(false);Check(parts.Count(t=>t.name=="Pitched workshop roof module")==2,"two physical roof slopes");Check(parts.Count(t=>t.name=="Workshop projecting eave")==2&&parts.Count(t=>t.name=="Kiln chimney cap")==1,"modular eaves and chimney cap");Check(parts.Where(t=>t!=house.transform).All(t=>CameraOcclusionSurface.marked.Contains(t.gameObject)),"all rooftop parts retain occlusion marks");Check(WorldTraversal.circles==0,"roof doesn't mutate navigation");
 string path=Environment.GetEnvironmentVariable("SCENERY_EXPORT");if(!string.IsNullOrEmpty(path)){var samples=new List<object>();for(int seed=0;seed<3;seed++){var tree=new GameObject("sample");WorldBuilder.TestTree(tree.transform,new WorldResources(),Vector3.zero,1,seed);samples.Add(Geometry(tree,"tree variant "+seed));}samples.Add(Geometry(house,"workshop roof"));System.IO.File.WriteAllText(path,System.Text.Json.JsonSerializer.Serialize(samples));}
 Console.WriteLine("PASS: "+checks+" production tree/roof topology, bounds, occlusion and navigation checks; managed construction only");}
}

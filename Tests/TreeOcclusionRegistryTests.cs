using System;using System.Linq;using System.Collections.Generic;using System.Reflection;using UnityEngine;using Emberfall;
namespace Emberfall {
 // This suite exercises the retained procedural fallback; resource readiness has separate coverage.
 public static class BlenderSceneryArt {public static GameObject Create(string n,Transform p,Vector3 at,WorldResources r)=>null;public static GameObject CreatePilotProp(string n,Transform p,Vector3 at)=>null;}

 public class WorldResources {public List<Mesh> owned=new List<Mesh>();internal Material Material(Color c,bool e=false,VisualSurface s=VisualSurface.Stone)=>new Material(Shader.Find("test")){color=c};public void Own(Mesh m){owned.Add(m);}}
 public static class WorldTraversal {public static void AddCircle(Vector3 p,float r){}public static void AddBox(Vector3 p,Vector2 s){}}
 public static class CombatFx {public static Material NewGlow()=>new Material(Shader.Find("test"));}
 public static partial class WorldBuilder {
  static Transform Region(Transform p,string n){var o=new GameObject(n);o.transform.SetParent(p,false);return o.transform;}
  public static GameObject Primitive(Transform p,string n,PrimitiveType type,Vector3 pos,Vector3 size,Material m,bool cameraOccluder=false){var o=new GameObject(n);o.transform.SetParent(p,false);o.transform.localPosition=pos;o.transform.localScale=size;o.AddComponent<MeshFilter>().sharedMesh=ProceduralVisuals.Shape(type);o.AddComponent<MeshRenderer>().sharedMaterial=m;if(cameraOccluder)CameraOcclusionSurface.Mark(o);return o;}
  public static void TestTree(Transform p,WorldResources r,Vector3 at,float size,int seed)=>Tree(p,r,at,size,seed);
 }
}
class TreeOcclusionRegistryTests {
 static int n;static void Check(bool b,string why){n++;if(!b)throw new Exception(why);}
 static List<CameraOcclusionSurface> Registry=>(List<CameraOcclusionSurface>)typeof(CameraOcclusionSurface).GetField("surfaces",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
 static int Faded=>(int)typeof(CameraOcclusionSurface).GetField("fadedCount",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
 static void Aim(Vector3 at){CameraOcclusionSurface.Advance(at+new Vector3(0,1,-10),at+new Vector3(0,1,8),at+new Vector3(0,.1f,8),at+new Vector3(0,1,8),false,.1f);}
 static GameObject Tree(string name,Vector3 at){var root=new GameObject(name);WorldBuilder.TestTree(root.transform,new WorldResources(),at,1,0);return root;}
 static void Main(){
 var world=new GameObject("actual wilderness");WorldBuilder.TestWildernessTrees(world.transform,new WorldResources());
 var trees=world.GetComponentsInChildren<Transform>(false).Where(t=>t.name=="Branching open canopy tree").ToArray();
 Check(trees.Length>=33&&trees.Sum(t=>t.GetComponentsInChildren<Renderer>(false).Length)>=528,"actual production wilderness loop reproduces more than256 child-renderer demand");
 Check(trees.All(t=>t.GetComponentsInChildren<CameraOcclusionSurface>(false).Length==1&&Registry.Contains(t.GetComponent<CameraOcclusionSurface>()))&&Registry.Count<256,"real registry admits every wilderness tree as one logical surface");
 var late=Tree("later tree",new Vector3(100,0,0));var parts=late.GetComponentsInChildren<Renderer>(false);var originals=parts.Select(v=>v.sharedMaterial).ToArray();
 Check(Registry.Contains(late.GetComponentsInChildren<CameraOcclusionSurface>(false).Single()),"later tree registers after actual wilderness density");
 var house=new GameObject("later building");var white=new Material(Shader.Find("test"));for(int i=0;i<3;i++)WorldBuilder.Primitive(house.transform,"upper building",PrimitiveType.Cube,new Vector3(200,i+1,0),Vector3.one,white);
 BuildingOcclusionGroup.Configure(house.transform,new Vector3(200,0,0),new Vector2(5,5));
 Check(house.GetComponentsInChildren<CameraOcclusionSurface>(false).All(s=>Registry.Contains(s)),"later building upper geometry registers after actual wilderness density");
 Aim(new Vector3(100,0,0));Check(parts.Length==16&&parts.All(v=>v.sharedMaterial.name=="Camera hierarchy fade (owned)"&&v.sharedMaterial.color.a<1),"all16 tree renderers fade together including branches and fans");Check(Faded==2,"one tree needs two owned shared-material clones not16");
 Check(originals.All(m=>m.color.a==1),"shared authored materials never mutated");late.SetActive(false);Check(Faded==0&&parts.Select((v,i)=>ReferenceEquals(v.sharedMaterial,originals[i])).All(b=>b),"disable restores all originals and releases both material slots");
 late.SetActive(true);Check(Registry.Contains(late.GetComponentsInChildren<CameraOcclusionSurface>(false).Single()),"re-enable restores one registry slot");Aim(new Vector3(200,0,0));Check(house.GetComponentsInChildren<Renderer>(false).Where(v=>!(v is LineRenderer)).All(v=>v.sharedMaterial.color.a<1)&&house.GetComponentsInChildren<LineRenderer>(false).Single().enabled,"later building still fades as complete group with footprint");CameraOcclusionSurface.RestoreAll();
 world.SetActive(false);late.SetActive(false);house.SetActive(false);Check(Registry.Count==0&&Faded==0,"scene disable frees all actual world registry entries");
 var cap=new GameObject("fade-cap");var singles=new List<Renderer>();for(int i=0;i<31;i++){var o=WorldBuilder.Primitive(cap.transform,"occluding single",PrimitiveType.Cube,new Vector3(0,1,0),Vector3.one,white,true);singles.Add(o.GetComponent<Renderer>());}Aim(Vector3.zero);Check(Faded==31,"real fade cap is nearly occupied");
 var refused=Tree("atomic tree",Vector3.zero);var untouched=refused.GetComponentsInChildren<Renderer>(false).Select(v=>v.sharedMaterial).ToArray();Aim(Vector3.zero);Check(Faded==31&&refused.GetComponentsInChildren<Renderer>(false).Select((v,i)=>ReferenceEquals(v.sharedMaterial,untouched[i])).All(b=>b),"only one free material slot refuses entire two-material tree without partial fade");
 cap.SetActive(false);Aim(Vector3.zero);Check(Faded==2,"released capacity admits entire tree next frame");UnityEngine.Object.Destroy(refused);UnityEngine.Object.Flush();Check(Faded==0&&Registry.Count==0,"destroy releases root registry and clones exactly once");
 var limit=new GameObject("registry cap");for(int i=0;i<257;i++)WorldBuilder.Primitive(limit.transform,"limited",PrimitiveType.Cube,new Vector3(i+300,1,0),Vector3.one,white,true);Check(Registry.Count==256,"logical registration cap remains256 without unbounded fallback");limit.SetActive(false);Check(Registry.Count==0&&Faded==0,"full registry cap cleans up completely");
 Console.WriteLine("PASS: "+n+" REAL bounded registry/world-density/material admission/lifecycle checks; Unity API doubles, not GPU rendering");
 }
}

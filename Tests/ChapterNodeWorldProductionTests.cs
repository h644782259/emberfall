using System;using System.Linq;using System.Collections.Generic;using UnityEngine;using Emberfall;
namespace UnityEngine
{
 public enum LightType{Directional,Point}public class Light:Component{public LightType type;public float intensity;public Color color;}
 public static class RenderSettings{public static Color fogColor,ambientSkyColor,ambientEquatorColor,ambientGroundColor;public static float fogDensity;}
 public static class ColorUtility{public static string ToHtmlStringRGBA(Color c)=>$"{c.r},{c.g},{c.b},{c.a}";}
 public struct Rect{public float xMin,xMax,yMin,yMax;public Rect(float x,float y,float w,float h){xMin=x;xMax=x+w;yMin=y;yMax=y+h;}}
 public partial struct Vector3{public void Normalize(){this=normalized;}public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;public static float Distance(Vector3 a,Vector3 b)=>(a-b).magnitude;public static Vector3 ClampMagnitude(Vector3 a,float m)=>a.magnitude>m?a.normalized*m:a;}
 public static partial class Mathf{public const float Deg2Rad=(float)Math.PI/180;public static int Min(int a,int b)=>Math.Min(a,b);public static int Clamp(int a,int b,int c)=>Math.Min(c,Math.Max(a,b));public static int CeilToInt(float v)=>(int)Math.Ceiling(v);public static int RoundToInt(float v)=>(int)Math.Round(v);}
}
namespace Emberfall
{
 public enum ChapterNode{ForestCourt,Redrock,StarPlatform}public enum ZoneKind{Dungeon,Wilderness}internal enum VisualSurface{Stone,Water,Wood,Crystal,Metal,Foliage}
 public static class ProceduralVisuals{internal static void ApplySurface(Material m,VisualSurface s){}}
 public static class PlayerUpgradeRules{public static float FindSafeBlinkDistance(float d,Func<float,bool>a,Func<float,bool>b){throw new NotSupportedException();}}
 public static partial class CombatFx{public static float SegmentDistance(Vector3 p,Vector3 a,Vector3 b){p.y=a.y=b.y=0;var d=b-a;return Vector3.Distance(p,a+d*(d.sqrMagnitude<.0001f?0:Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude)));}}
 public static partial class WorldBuilder
 {
  public static void Generate(Transform parent,WorldResources r,ChapterRoomPlan plan){BuildChapterRoom(parent,r,plan);}
  private static GameObject Primitive(Transform p,string name,PrimitiveType type,Vector3 at,Vector3 size,Material m,bool cameraOccluder=false){var o=new GameObject(name);o.transform.SetParent(p,false);o.transform.localPosition=at;o.transform.localScale=size;o.AddComponent<MeshRenderer>().sharedMaterial=m;return o;}
  private static void Crystal(Transform p,WorldResources r,Vector3 at,float size,Material m){Primitive(p,"crystal",PrimitiveType.Cube,at,Vector3.one*size,m);}
  private static void Ring(Transform p,WorldResources r,string name,Vector3 at,float radius,float width,Material m,bool vertical){Primitive(p,name,PrimitiveType.Cube,at,new Vector3(radius*2,.01f,radius*2),m);}
  private static void BuildPortalFocus(Transform p,WorldResources r,Vector3 at){}
 }
}
public static class ChapterNodeWorldProductionTests
{
 static int checks;static void Check(bool v,string why){checks++;if(!v)throw new Exception(why);}
 static void Clear(){foreach(var o in GameObject.All.ToArray())UnityEngine.Object.Destroy(o);GameObject.All.Clear();}
 static bool[] Navigation(){var values=new List<bool>();for(int x=-14;x<=14;x+=2)for(int z=-12;z<=14;z+=2)values.Add(WorldTraversal.IsWalkable(new Vector3(x,0,z),.65f));return values.ToArray();}
 public static string Run()
 {
  foreach(ChapterNode node in Enum.GetValues(typeof(ChapterNode)))for(int room=0;room<(node==ChapterNode.StarPlatform?1:2);room++)for(int seed=0;seed<2;seed++)
  {
   Clear();var plan=ChapterRoomGeometry.Plan(node,room,seed);WorldTraversal.Reset(ZoneKind.Dungeon);ChapterRoomGeometry.Register(plan);var expected=Navigation();
   WorldTraversal.Reset(ZoneKind.Dungeon);int revision=WorldTraversal.Revision;var root=new GameObject("chapter");var r=root.AddComponent<WorldResources>();foreach(string name in new[]{"Sun","Fill"}){var light=new GameObject(name);light.transform.SetParent(root.transform,false);light.AddComponent<Light>().type=LightType.Directional;}
   RenderSettings.fogDensity=.016f;WorldBuilder.Generate(root.transform,r,plan);
   Check(RenderSettings.fogDensity<(node==ChapterNode.Redrock?.012f:.010f)&&RenderSettings.fogDensity>=.006f,"chapter generator selects readable node fog");Check(RenderSettings.ambientGroundColor.r>=.17f&&GameObject.All.Count(o=>o.GetComponent<Light>()!=null)==2,"chapter lighting keeps readable ground and reuses only existing two lights");
   Check(WorldTraversal.Revision==revision+plan.Obstacles.Length&&Navigation().SequenceEqual(expected),"node presentation preserves exact registered obstacle footprints");Check(GameObject.All.Count<75,"node scenery has bounded object count");
   if(node==ChapterNode.ForestCourt)
   {
    var roots=GameObject.All.Where(o=>o.name.EndsWith("forest root vein")).ToArray();Check(roots.Length==8,"forest has eight physical root veins");
    foreach(var branch in roots){Check(branch.GetComponent<MeshFilter>().sharedMesh.vertices.All(v=>CombatFx.Flat(v).magnitude<3.4f),"forest root detail stays inside existing solid island");Check(room==0?branch.name.StartsWith("Dormant"):branch.name.StartsWith("Restored"),"purified forest retreat rebuilds restored root veins");}
   }
   else if(node==ChapterNode.Redrock)
   {var cable=GameObject.All.Single(o=>o.name=="Mine hunt-side feeder conduit").GetComponent<MeshFilter>().sharedMesh;Check(cable.vertices[0].x*((seed&1)==0?1:-1)>0,"mine feeder begins on actual mirrored hunt side");Check(GameObject.All.Count(o=>o.name=="Forked mine flush rail")==4,"both mine route rails remain explicit");}
   else{Check(GameObject.All.Count(o=>o.name=="Open star ritual inset")==8,"star adds bounded flush ritual composition");Check(WorldTraversal.HasGroundPath(plan.Entrance,plan.Exit,1.3f),"open star arena keeps large-body central transit clear");}
   var meshes=GameObject.All.Select(o=>o.GetComponent<MeshFilter>()?.sharedMesh).Where(m=>m!=null).ToArray();UnityEngine.Object.Destroy(root);Check(meshes.All(m=>m.Destroyed),"world-owned chapter ribbons release on teardown");
  }
  Clear();return "PASS: "+checks+" actual chapter generator atmosphere/scenery/traversal/resource checks (primitive render substitutes, not Unity visuals)";
 }
}

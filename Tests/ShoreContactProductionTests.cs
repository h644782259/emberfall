using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Emberfall;

public static class ShoreContactProductionTests
{
 static int checks;
 static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
 static void Clear(){foreach(var o in GameObject.All.ToArray())UnityEngine.Object.Destroy(o);GameObject.All.Clear();}
 static bool[] Nav(){var values=new List<bool>();for(int x=-15;x<=15;x++)for(int z=-10;z<=10;z++)values.Add(WorldTraversal.IsWalkable(new Vector3(x,0,z),.45f));return values.ToArray();}
 static void Entry(int scene,Transform p,WorldResources r,bool baseline){if(scene==0)WorldBuilder.Brook(p,r,22,baseline);else if(scene==1)WorldBuilder.Courtyard(p,r,22,baseline);else WorldBuilder.Tactical(p,r,scene==2?22:23,baseline);}
 static Vector3[] RegisteredRiver()=> (Vector3[])typeof(WorldTraversal).GetField("river",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
 static float RegisteredHalfWidth()=> (float)typeof(WorldTraversal).GetField("riverHalfWidth",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
 static float DistanceToRiver(Vector3 point,Vector3[] path){float nearest=float.PositiveInfinity;for(int i=1;i<path.Length;i++)nearest=Math.Min(nearest,CombatFx.SegmentDistance(point,path[i-1],path[i]));return nearest;}
 static void WithinNavigation(Vector3 point,Vector3[] path,float halfWidth){Check(DistanceToRiver(point,path)<=halfWidth+.0001f,"WATER_OUTSIDE_NAVIGATION_WIDTH");}

 static void ValidateNormals(Mesh mesh,string tag)
 {
  var sums=new Vector3[mesh.vertices.Length];var used=new int[sums.Length];
  for(int i=0;i<mesh.triangles.Length;i+=3){int a=mesh.triangles[i],b=mesh.triangles[i+1],c=mesh.triangles[i+2];Check(a>=0&&b>=0&&c>=0&&a<sums.Length&&b<sums.Length&&c<sums.Length,tag+" actual triangle indices in range");var n=Vector3.Cross(mesh.vertices[b]-mesh.vertices[a],mesh.vertices[c]-mesh.vertices[a]);Check(n.sqrMagnitude>1e-12f,tag+" triangle has nonzero area");foreach(int j in new[]{a,b,c}){sums[j]+=n;used[j]++;}if(tag!="waterline")Check(n.y>0,"CONTINUOUS_WATER_TOP_FACES_UPWARD");}
  Check(mesh.normals!=null&&mesh.normals.Length==sums.Length,tag+" production Geometry recalculates normals");
  for(int i=0;i<sums.Length;i++){
   Check(used[i]>0&&sums[i].sqrMagnitude>1e-12f,tag=="waterline"?"WATERLINE_NORMAL_ZERO":"upward surface incident normal is nonzero");
   Vector3 normal=mesh.normals[i];Check(float.IsFinite(normal.x)&&float.IsFinite(normal.y)&&float.IsFinite(normal.z)&&Math.Abs(normal.magnitude-1)<.00001f,tag+" normal is finite and normalized");
   Check(Vector3.Dot(normal,sums[i].normalized)>.99999f,tag+" normal follows actual incident faces");Check(tag=="waterline"?Math.Abs(normal.y)<.00001f:normal.y>.99999f,tag+" vertical line or upward plane has the proper normal");
  }
 }

 static void ValidateContinuous(Mesh mesh,Vector3[] path,float halfWidth,Material material)
 {
  ValidateNormals(mesh,"continuous water");Check(mesh.colors!=null&&mesh.colors.Length==mesh.vertices.Length&&mesh.uv!=null&&mesh.uv.Length==mesh.vertices.Length,"actual water has continuous color and UV per vertex");
  Check(material.GetFloat("_VertexTint")==1&&material.GetFloat("_WaterSurface")==1,"actual world-owned water enables continuous vertex tint and shader waves");
  var adjacency=new List<int>[mesh.vertices.Length];for(int i=0;i<adjacency.Length;i++)adjacency[i]=new List<int>();
  for(int i=0;i<mesh.vertices.Length;i++){Vector3 v=mesh.vertices[i];Check(float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z),"actual water vertices finite");WithinNavigation(v,path,halfWidth);Check(Math.Abs(mesh.colors[i].a-1)<.000001f,"continuous vertex color is opaque");}
  for(int i=0;i<mesh.triangles.Length;i+=3){int a=mesh.triangles[i],b=mesh.triangles[i+1],c=mesh.triangles[i+2];foreach(var edge in new[]{(a,b),(b,c),(c,a)}){adjacency[edge.Item1].Add(edge.Item2);adjacency[edge.Item2].Add(edge.Item1);WithinNavigation((mesh.vertices[edge.Item1]+mesh.vertices[edge.Item2])*.5f,path,halfWidth);}WithinNavigation((mesh.vertices[a]+mesh.vertices[b]+mesh.vertices[c])/3,path,halfWidth);}
  var reached=new HashSet<int>();var queue=new Queue<int>();queue.Enqueue(0);reached.Add(0);while(queue.Count>0){foreach(int next in adjacency[queue.Dequeue()])if(reached.Add(next))queue.Enqueue(next);}
  Check(reached.Count==mesh.vertices.Length,"water is one connected actual surface rather than separate strips");
 }

 static void ValidateContact(GameObject water,GameObject wet,GameObject line,float deck)
 {
  Mesh surface=water.GetComponent<MeshFilter>().sharedMesh,wm=wet.GetComponent<MeshFilter>().sharedMesh,lm=line.GetComponent<MeshFilter>().sharedMesh;
  int sections=wm.vertices.Length/4,across=surface.vertices.Length/sections;
  Check(sections>=2&&across>=3&&surface.vertices.Length==sections*across,"continuous water has multiple cross-stream samples at every shore section");ValidateNormals(wm,"wet");ValidateNormals(lm,"waterline");
  Check(lm.vertices.Length==8*sections&&lm.triangles.Length==24*(sections-1)&&wm.triangles.Length==12*(sections-1),"both banks merge into static contact geometry with independent reverse faces");
  int front=lm.vertices.Length/2,frontIndices=lm.triangles.Length/2;
  for(int i=0;i<front;i++){Check((lm.vertices[i]-lm.vertices[i+front]).sqrMagnitude<1e-12f,"opposite shoreline faces coincide with independent vertices");Check(Vector3.Dot(lm.normals[i],lm.normals[i+front])<-.99999f,"opposite shoreline normals cannot cancel");}
  for(int i=0;i<frontIndices;i+=3)for(int j=0;j<3;j++)Check(lm.triangles[frontIndices+i+j]==lm.triangles[i+2-j]+front,"back triangles reverse winding using their own vertices");
  Check(ReferenceEquals(wet.GetComponent<MeshRenderer>().sharedMaterial,line.GetComponent<MeshRenderer>().sharedMaterial),"both contact layers share a world-owned material");
  for(int side=0;side<2;side++)for(int i=0;i<sections;i++)
  {
   Vector3 high=lm.vertices[side*sections*2+i*2],low=lm.vertices[side*sections*2+i*2+1];Vector3 left=surface.vertices[i*across],right=surface.vertices[(i+1)*across-1];Vector3 edge=(CombatFx.Flat(high-left).sqrMagnitude<CombatFx.Flat(high-right).sqrMagnitude)?left:right;
   Check(CombatFx.Flat(edge-high).sqrMagnitude<1e-10f&&high.x==low.x&&high.z==low.z,"vertical waterline meets the current irregular continuous water edge");Check(Math.Abs(high.y-low.y-.016f)<.00001f&&low.y<edge.y&&high.y>edge.y,"vertical waterline straddles actual water height");
   Vector3 inner=wm.vertices[side*sections*2+i*2],outer=wm.vertices[side*sections*2+i*2+1];float span=CombatFx.Flat(outer-inner).magnitude;Check(span>=.0469f&&span<=.0971f,"wet land contact is a bounded natural bank strip");
   Color bank=surface.colors[i*across+(side==0?0:across-1)],deep=surface.colors[i*across+across/2];Check(bank.r+bank.g+bank.b>deep.r+deep.g+deep.b+.30f,"one surface carries a continuous shallow-to-deep color gradient");
  }
  Check(lm.vertices.All(v=>v.y<deck)&&wm.vertices.All(v=>v.y<deck),"shore details remain below the real bridge support plane");
 }

 public static string Run()
 {
  checks=0;
  for(int scene=0;scene<4;scene++)
  {
   Clear();WorldTraversal.Reset(ZoneKind.Wilderness);var root=new GameObject("world");var resources=root.AddComponent<WorldResources>();Entry(scene,root.transform,resources,true);bool[] baseline=Nav();float bx=scene<2?0:scene==2?7:-7;bool route=WorldTraversal.CanReach(new Vector3(bx,0,-5),new Vector3(bx,0,5),.45f);Check(route,"real authored bridge crossing is reachable before visual emission");
   Clear();WorldTraversal.Reset(ZoneKind.Wilderness);root=new GameObject("world");resources=root.AddComponent<WorldResources>();int revision=WorldTraversal.Revision;Entry(scene,root.transform,resources,false);
   Check(WorldTraversal.Revision==revision+1&&Nav().SequenceEqual(baseline)&&WorldTraversal.CanReach(new Vector3(bx,0,-5),new Vector3(bx,0,5),.45f)==route,"actual water/shore entry preserves river registration and route reachability");
   GameObject[] wet=GameObject.All.Where(o=>o.name.EndsWith("shore damp seam")).ToArray(),line=GameObject.All.Where(o=>o.name.EndsWith("shore vertical waterline")).ToArray();Check(wet.Length==1&&line.Length==1,"actual water entry must build both static shoreline layers");
   GameObject[] surfaces=GameObject.All.Where(o=>o.name.EndsWith("continuous rippling water")).ToArray();Check(surfaces.Length==1,"actual water entry emits exactly one continuous surface");var water=surfaces[0];Mesh waterMesh=water.GetComponent<MeshFilter>().sharedMesh;
   ValidateContinuous(waterMesh,RegisteredRiver(),RegisteredHalfWidth(),water.GetComponent<MeshRenderer>().sharedMaterial);ValidateContact(water,wet[0],line[0],scene==0?.049f:scene==1?.07f:.14f);
   Mesh wm=wet[0].GetComponent<MeshFilter>().sharedMesh,lm=line[0].GetComponent<MeshFilter>().sharedMesh;Material material=line[0].GetComponent<MeshRenderer>().sharedMaterial;UnityEngine.Object.Destroy(root);Check(wm.Destroyed&&lm.Destroyed&&waterMesh.Destroyed&&material.Destroyed,"real WorldResources teardown releases every emitted surface/contact mesh and material");
  }
  Clear();WorldTraversal.Reset(ZoneKind.Wilderness);var longRoot=new GameObject("long");Vector3[] path=WorldBuilder.LongPath(longRoot.transform,longRoot.AddComponent<WorldResources>());GameObject longWater=GameObject.All.Single(o=>o.name.EndsWith("continuous rippling water")),longWet=GameObject.All.Single(o=>o.name.EndsWith("shore damp seam")),longLine=GameObject.All.Single(o=>o.name.EndsWith("shore vertical waterline"));
  ValidateContinuous(longWater.GetComponent<MeshFilter>().sharedMesh,path,1,longWater.GetComponent<MeshRenderer>().sharedMaterial);ValidateContact(longWater,longWet,longLine,.049f);
  int sections=longWet.GetComponent<MeshFilter>().sharedMesh.vertices.Length/4;Check(sections>=path.Length&&sections<=(path.Length-1)*128+1,"long authored path retains the full waterline within the production per-segment sample bound");Clear();
  return "PASS: "+checks+" actual brook/courtyard/both tactical/long continuous-water, upward mesh, colors, shoreline, ownership and original navigation-width checks (managed; GPU/shader rendering separate).";
 }
}

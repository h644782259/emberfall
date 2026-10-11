using System;using System.Linq;using System.IO;using System.Collections.Generic;using System.Reflection;using System.Text.Json;using UnityEngine;using Emberfall;
class G07FactoryInventoryTests {
 static object V(Vector3 p)=>new[]{p.x,p.y,p.z};
 static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
 static void Main(){var rows=new List<object>();for(int i=0;i<7;i++){
  CameraOcclusionSurface.RestoreAll();WorldTraversal.Reset(ZoneKind.Wilderness);var root=new GameObject("factory "+i);var owned=root.AddComponent<WorldResources>();
  AuthoredFixedScenery.Enabled=true;if(i<6)WorldBuilder.FixedSample(root.transform,owned,i);else WorldBuilder.MakeRoomObjective(Vector3.zero,true).transform.SetParent(root.transform,false);
  var renderers=root.GetComponentsInChildren<Renderer>(true);var meshes=root.GetComponentsInChildren<MeshFilter>(true).Select(x=>x.sharedMesh).Where(x=>x!=null).Distinct().ToArray();
  var mats=renderers.SelectMany(x=>x.sharedMaterials).Where(x=>x!=null).Distinct().ToArray();
  if(root.GetComponentsInChildren<Collider>(true).Length!=0)throw new Exception("unexpected physics");
  rows.Add(new {factory=i,objects=root.GetComponentsInChildren<Transform>(true).Length,renderers=renderers.Length,uniqueMeshes=meshes.Length,uniqueMaterials=mats.Length,
   uniqueSourceTriangles=meshes.Sum(m=>m.triangles.Length/3),instancedMeshTriangles=root.GetComponentsInChildren<MeshFilter>(true).Where(x=>x.sharedMesh!=null).Sum(x=>x.sharedMesh.triangles.Length/3),lights=root.GetComponentsInChildren<Light>(true).Length,
   navigationBoxes=WorldTraversal.boxes.Select(x=>new{position=V(x.p),size=new[]{x.size.x,x.size.y}}).ToArray(),navigationCircles=WorldTraversal.circles.Select(x=>new{position=V(x.p),radius=x.radius}).ToArray(),
   occlusion=root.GetComponentsInChildren<CameraOcclusionSurface>(true).Select(x=>new{path=PathOf(x.transform),group=(typeof(CameraOcclusionSurface).GetField("group",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(x) is BuildingOcclusionGroup g)?PathOf(g.transform):null}).ToArray(),
   parts=root.GetComponentsInChildren<Transform>(true).Select(t=>new{path=PathOf(t),position=V(t.localPosition),scale=V(t.localScale),forward=V(t.localRotation*Vector3.forward)}).ToArray()});
  root.SetActive(false);UnityEngine.Object.Destroy(root);UnityEngine.Object.Flush();
 }
 File.WriteAllText(Environment.GetEnvironmentVariable("G07_INVENTORY"),JsonSerializer.Serialize(rows,new JsonSerializerOptions{WriteIndented=true}));
 Console.WriteLine("PASS: seven complete affected factories inventoried with actual decoded resources; renderer counts are not draw calls; managed boundaries only");}
}

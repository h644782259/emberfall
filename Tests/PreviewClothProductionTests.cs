using System;using System.Linq;using UnityEngine;using Emberfall;
public static class PreviewClothProductionTests
{
 static int n;static void Check(bool ok,string why){n++;if(!ok)throw new Exception(why);}
 static bool Equal(Vector3[] a,Vector3[] b)=>a.Zip(b,(x,y)=>(x-y).magnitude<.000001f).All(x=>x);
 public static void Main()
 {
  var host=new GameObject("preview cloth");var material=new Material(Shader.Find("test"));var cloth=host.AddComponent<TailoredCloth>();cloth.Initialize(material);var mesh=host.GetComponent<MeshFilter>().sharedMesh;
  Time.time=10;cloth.SamplePreview(.2f,.4f);var first=(Vector3[])mesh.vertices.Clone();Time.time=700;cloth.SamplePreview(.2f,.4f);Check(Equal(first,mesh.vertices),"manual preview cloth uses supplied clock rather than world time");
  cloth.SamplePreview(.9f,.4f);Check(!Equal(first,mesh.vertices),"local preview clock actually deforms authored cloak vertices");
  int count=UnityEngine.Object.All.Count;for(int i=0;i<500;i++){cloth.SamplePreview(i*.01f,.6f);Check(host.GetComponent<MeshFilter>().sharedMesh==mesh&&mesh.vertices.All(v=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z)),"manual cloth keeps one finite owned mesh");}
  Check(UnityEngine.Object.All.Count==count,"manual cloak animation creates no additional engine objects");UnityEngine.Object.Destroy(host);UnityEngine.Object.Flush();Check(mesh.destroyed&&!material.destroyed,"cloak lifecycle frees only its mesh not borrowed palette");
  var signatures=new System.Collections.Generic.HashSet<string>();
  foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass)))
  {
   var h=new GameObject("class silhouette");var c=h.AddComponent<TailoredCloth>();c.Initialize(material,hero);var m=h.GetComponent<MeshFilter>().sharedMesh;
   Check(signatures.Add(string.Join(";",m.vertices.Select(v=>v.ToString()))),"each class has distinct actual cloth geometry");
   Check(m.triangles.Length==(hero==HeroClass.Arcanist||hero==HeroClass.Summoner?432:504),"split cloth has real disconnected center seam topology");
   if(hero==HeroClass.Ranger)Check(m.vertices.Max(v=>v.x)<.06f,"ranger cloak leaves right quiver clearance");
   Check(m.vertices.All(v=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z)),"all rear silhouettes finite");
   UnityEngine.Object.Destroy(h);UnityEngine.Object.Flush();
  }
  UnityEngine.Object.Destroy(material);UnityEngine.Object.Flush();
  Console.WriteLine("PASS: "+n+" actual manual cloth deformation/resource checks (managed mesh, not Unity cloth or GPU)");
 }
}

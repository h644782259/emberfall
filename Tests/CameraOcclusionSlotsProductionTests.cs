using System;using System.Linq;using System.Reflection;using System.Collections.Generic;using UnityEngine;using Emberfall;
class CameraOcclusionSlotsProductionTests
{
 static int n;static void C(bool yes,string why){n++;if(!yes)throw new Exception(why);}
 static int Faded=>(int)typeof(CameraOcclusionSurface).GetField("fadedCount",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
 static Material Mat(string name)=>new Material(Shader.Find("test")){name=name,color=Color.white};
 static Renderer Part(GameObject root,params Material[] slots){var o=new GameObject("part");o.transform.SetParent(root.transform,false);o.transform.localPosition=new Vector3(0,1,0);o.AddComponent<MeshFilter>().sharedMesh=new Mesh{vertices=new[]{new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,.5f,.5f)},triangles=new int[0]};var r=o.AddComponent<MeshRenderer>();r.sharedMaterials=slots;return r;}
 static void Aim()=>CameraOcclusionSurface.Advance(new Vector3(0,1,-10),new Vector3(0,1,10),.1f);
 static void Dispose(GameObject root){root.SetActive(false);UnityEngine.Object.Destroy(root);UnityEngine.Object.Flush();C(Faded==0,"complete release after teardown");}
 static void Main()
 {
  foreach(bool hierarchy in new[]{false,true})
  {
   var root=new GameObject("two-slot tree");var bark=Mat("Bark");var leaf=Mat("Leaf");var r=Part(root,bark,leaf,bark);
   if(hierarchy)CameraOcclusionSurface.MarkHierarchy(root);else CameraOcclusionSurface.Mark(r.gameObject);
   Aim();var owned=r.sharedMaterials;C(owned.Length==3&&owned.All(m=>m.color.a<1),"all material slots fade");C(Faded==2&&ReferenceEquals(owned[0],owned[2]),"duplicate sources share two owned clones");C(bark.color.a==1&&leaf.color.a==1,"original palette not changed");
   root.SetActive(false);C(r.sharedMaterials.SequenceEqual(new[]{bark,leaf,bark})&&Faded==0,"disable restores all slots");UnityEngine.Object.Flush();C(owned.All(m=>m.destroyed),"every owned clone destroyed");root.SetActive(true);Aim();C(Faded==2,"reenable fades full material set");
   UnityEngine.Object.Destroy(root);UnityEngine.Object.Flush();C(Faded==0&&r.sharedMaterials.SequenceEqual(new[]{bark,leaf,bark}),"destroy restores and releases all slots once");
  }
  foreach(bool hierarchy in new[]{false,true})foreach(bool missing in new[]{false,true})
  {
   var root=new GameObject("unsupported second slot");var a=Mat("first");var b=missing?null:Mat("unsupported");if(b!=null)b.supported=false;var r=Part(root,a,b);
   if(hierarchy)CameraOcclusionSurface.MarkHierarchy(root);else CameraOcclusionSurface.Mark(r.gameObject);
   Aim();C(Faded==0&&r.sharedMaterials.SequenceEqual(new[]{a,b}),"null or unsupported secondary refuses complete surface");Dispose(root);
  }
  var capRoot=new GameObject("cap");for(int i=0;i<31;i++){var r=Part(capRoot,Mat("single"));CameraOcclusionSurface.Mark(r.gameObject);}Aim();C(Faded==31,"fill 31 real slots");
  var waiting=new GameObject("waiting dual");var ma=Mat("a");var mb=Mat("b");var wr=Part(waiting,ma,mb);CameraOcclusionSurface.MarkHierarchy(waiting);Aim();C(Faded==31&&wr.sharedMaterials.SequenceEqual(new[]{ma,mb}),"one remaining capacity refuses both tree slots");capRoot.SetActive(false);Aim();C(Faded==2,"released capacity admits both tree slots");Dispose(waiting);Dispose(capRoot);
  var external=new GameObject("external edits");ma=Mat("a");mb=Mat("b");var er=Part(external,ma,mb);CameraOcclusionSurface.Mark(er.gameObject);Aim();var old=er.sharedMaterials;var replacement=Mat("replacement");er.sharedMaterials=new[]{old[0],replacement,Mat("appended")};CameraOcclusionSurface.RestoreAll();C(er.sharedMaterials.Length==3&&ReferenceEquals(er.sharedMaterials[0],ma)&&ReferenceEquals(er.sharedMaterials[1],replacement),"restore keeps external replacement and appended slot");C(Faded==0,"external change releases original clones");UnityEngine.Object.Flush();C(old.All(m=>m.destroyed),"detached external clones not leaked");
  Aim();old=er.sharedMaterials;er.sharedMaterials=new[]{replacement};CameraOcclusionSurface.RestoreAll();C(er.sharedMaterials.Length==1&&ReferenceEquals(er.sharedMaterial,replacement)&&Faded==0,"external shortened array preserved");Dispose(external);
  var invalidated=new GameObject("destroyed source");ma=Mat("a");mb=Mat("b");var ir=Part(invalidated,ma,mb);CameraOcclusionSurface.MarkHierarchy(invalidated);Aim();UnityEngine.Object.Destroy(mb);UnityEngine.Object.Flush();Aim();C(Faded==0&&ReferenceEquals(ir.sharedMaterials[0],ma),"destroyed source releases complete fade without color access");Dispose(invalidated);
  // Genuine multi-renderer logical hierarchy, sharing bark across slot lists.
  var tree=new GameObject("multi mesh tree");ma=Mat("bark");mb=Mat("leaf");var tr1=Part(tree,ma,mb);var tr2=Part(tree,ma);CameraOcclusionSurface.MarkHierarchy(tree);Aim();C(Faded==2&&ReferenceEquals(tr1.sharedMaterials[0],tr2.sharedMaterial)&&tr1.sharedMaterials.All(m=>m.color.a<1),"multi-renderer hierarchy deduplicates across all slots");Dispose(tree);
  foreach(int fault in new[]{0,1,2})
  {
   bool bad=fault!=0;var building=new GameObject("atomic building");ma=Mat("a");mb=fault==2?null:Mat("b");if(fault==1)mb.supported=false;var r1=Part(building,ma);var r2=Part(building,ma,mb);BuildingOcclusionGroup.Configure(building.transform,Vector3.zero,new Vector2(5,5));Aim();
   C(bad?Faded==0:Faded==3,"whole building admission includes all slots of all members");C(bad?r1.sharedMaterial==ma:r1.sharedMaterial.color.a<1,"unsupported member refuses otherwise supported building member");C(building.GetComponentsInChildren<LineRenderer>(true).Single().enabled==!bad,"group outline agrees with complete group admission");Dispose(building);
  }
  var changedGroup=new GameObject("changed group");ma=Mat("a");mb=Mat("b");var cr1=Part(changedGroup,ma);var cr2=Part(changedGroup,ma,mb);BuildingOcclusionGroup.Configure(changedGroup.transform,Vector3.zero,new Vector2(5,5));Aim();C(Faded==3,"group active before external secondary replacement");replacement=Mat("unsupported external");replacement.supported=false;var edited=cr2.sharedMaterials;edited[1]=replacement;cr2.sharedMaterials=edited;Aim();C(Faded==0&&cr1.sharedMaterial==ma&&cr2.sharedMaterials[0]==ma&&cr2.sharedMaterials[1]==replacement,"invalidated group restores every member immediately and preserves external slot");Dispose(changedGroup);
  capRoot=new GameObject("group cap");for(int i=0;i<30;i++){var r=Part(capRoot,Mat("single"));CameraOcclusionSurface.Mark(r.gameObject);}Aim();
  var limited=new GameObject("three slot building");ma=Mat("a");mb=Mat("b");var lr1=Part(limited,ma);var lr2=Part(limited,ma,mb);BuildingOcclusionGroup.Configure(limited.transform,Vector3.zero,new Vector2(5,5));Aim();C(Faded==30&&lr1.sharedMaterial==ma&&lr2.sharedMaterials.SequenceEqual(new[]{ma,mb}),"two remaining slots refuse complete three-slot building");capRoot.SetActive(false);Aim();C(Faded==3&&lr1.sharedMaterial.color.a<1&&lr2.sharedMaterials.All(m=>m.color.a<1),"capacity admits complete multi-renderer building next frame");Dispose(limited);Dispose(capRoot);
  var decoy=new GameObject("expanded culling decoy");decoy.transform.localPosition=new Vector3(12,0,0);var dr=Part(decoy,Mat("wood"));dr.cullingBounds=new Bounds(new Vector3(0,1,0),Vector3.one*40);CameraOcclusionSurface.MarkHierarchy(decoy);Aim();C(CameraOcclusionSurface.LastHeroOccluders==0&&Faded==0,"expanded culling bounds do not spend hero fade slots");Dispose(decoy);
  Console.WriteLine("PASS: "+n+" actual multislot occlusion ownership/cap/lifecycle/group assertions; managed Unity API boundaries");
 }
}

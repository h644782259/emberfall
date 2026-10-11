// Imported-adapter tests explicitly authorize preview construction; subsequent clock tests exercise its sampler in isolation.
using System;using System.Linq;using UnityEngine;using Emberfall;
static class BlenderPilotReadinessTests
{
 static int n;static void Check(bool b,string reason){n++;if(!b)throw new Exception(reason);}
 static void Ready()
 {
  Resources.Items.Clear();var root=new GameObject("asset");root.AddComponent<Renderer>();root.AddComponent<MeshFilter>().sharedMesh=new Mesh();
  LayerFixture.Build(root);
  Resources.Items["BlenderPilot/Vanguard"]=root;Resources.Items["BlenderPilot/SupplyCrate"]=root;Resources.Items["BlenderPilot/Pilot_Atlas_Standard"]=new Material();
  Resources.Clips=new[]{"Pilot_Idle","Pilot_Move","Pilot_Basic","Pilot_Hit","Pilot_Skill"}.Select(x=>new AnimationClip{name=x}).ToArray();BlenderPilotArt.Enabled=true;
 }
 static void Fail(string reason,Action invalidate,bool prop=true)
 {
  Ready();invalidate();int before=GameObject.All.Count;var root=new GameObject("procedural host");var old=root.AddComponent<Renderer>();var model=root.AddComponent<CombatModel>();model.heroClass=HeroClass.Vanguard;model.isolatedPreview=true;model.Init();model.isolatedPreview=false;
  Check(model.View==null&&!model.Sample()&&old.enabled,"invalid pilot readiness keeps original hero: "+reason);
  if(prop)Check(BlenderPilotArt.CreateProp("SupplyCrate",null,Vector3.zero)==null,"invalid pilot readiness keeps original prop: "+reason);
  Check(!GameObject.All.Skip(before).Any(g=>!g.destroyed&&(g.name=="Blender pilot visual (sampled skeleton)"||g.name=="asset")),"invalid import instance retired immediately: "+reason);
 }
 static Material Material=>Resources.Items["BlenderPilot/Pilot_Atlas_Standard"] as Material;
 public static void Run()
 {
  Fail("missing material",()=>Resources.Items.Remove("BlenderPilot/Pilot_Atlas_Standard"));
  Fail("missing shader",()=>Material.shader=null);Fail("unsupported shader",()=>Material.shader.isSupported=false);Fail("wrong shader",()=>Material.shader.name="Hidden/InternalErrorShader");
  Fail("missing shader properties",()=>Material.Properties=false);Fail("disabled metallic map",()=>Material.Keyword=false);
  Fail("missing albedo",()=>Material.Textures.Remove("_MainTex"));Fail("missing metallic smoothness",()=>Material.Textures.Remove("_MetallicGlossMap"));
  Fail("invalid atlas",()=>((Texture2D)Material.Textures["_MainTex"]).width=1);Fail("invalid metallic smoothness",()=>Material.Textures["_MetallicGlossMap"]=new Texture());
  Fail("missing renderers",()=>((GameObject)Resources.Items["BlenderPilot/Vanguard"]).Components.RemoveAll(x=>x is Renderer));
  Fail("missing geometry",()=>((GameObject)Resources.Items["BlenderPilot/Vanguard"]).GetComponentsInChildren<MeshFilter>(true)[0].sharedMesh=null);
  Fail("disabled geometry",()=>((GameObject)Resources.Items["BlenderPilot/Vanguard"]).GetComponentsInChildren<Renderer>(true)[0].enabled=false);
  foreach(float length in new[]{0f,-1f,float.NaN,float.PositiveInfinity})Fail("invalid clip length",()=>Resources.Clips[0].length=length,false);
  Fail("empty animation",()=>Resources.Clips[0].empty=true,false);Fail("unbound animation",()=>Resources.Clips[0].Bound=false,false);Fail("sampling failure",()=>Resources.Clips[0].ThrowOnSample=true,false);
  Ready();var material=Material;var hero=BlenderPilotVisual.Create(null);var prop=BlenderPilotArt.CreateProp("SupplyCrate",null,Vector3.zero);
  Check(hero!=null&&hero.Ready&&prop!=null,"complete pilot material geometry and clips remain usable");
  UnityEngine.Object.Destroy(hero.gameObject);UnityEngine.Object.Destroy(prop);Check(!material.destroyed&&!material.Textures["_MainTex"].destroyed,"pilot loader never owns shared material or textures");
  Console.WriteLine("PASS: "+n+" actual loader readiness/fallback checks");
 }
}

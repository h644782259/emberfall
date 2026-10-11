#!/usr/bin/env python3
"""Execute actual spell-asset and supply-part methods without collider engine types.

This checks native-resource ownership and reachable factory calls at a managed
boundary; an IL2CPP player build and runtime interaction remain separate checks.
"""
from pathlib import Path
import os
import subprocess
import sys
import tempfile

root = Path(__file__).resolve().parents[1]
sdk = sys.argv[1] if len(sys.argv) > 1 else "dotnet"
vfx = (root / "Assets/Scripts/Combat/SkillPerformanceVfx.cs").read_text()
visual = (root / "Assets/Scripts/Combat/ProceduralVisuals.cs").read_text()
supply = (root / "Assets/Scripts/World/GroundSupplyPickup.cs").read_text()


def member(source, marker):
    start = source.index(marker)
    end = source.index("{", start) + 1
    depth = 1
    while depth:
        depth += (source[end] == "{") - (source[end] == "}")
        end += 1
    return source[start:end]


fixture = r'''
using System;using System.Collections.Generic;using System.Linq;using UnityEngine;
namespace UnityEngine {
 public enum PrimitiveType{Cube,Cylinder,Capsule,Sphere,Quad}
 public enum DepthTextureMode{None,Depth}
 public class Object {
  public static readonly List<Object> All=new List<Object>();public string name;public bool destroyed;
  public Object(){All.Add(this);}public static void Destroy(Object o){if(o!=null)o.destroyed=true;}
  public static Mesh Instantiate(Mesh o)=>new Mesh{name=o.name,vertices=(Vector3[])o.vertices.Clone(),normals=(Vector3[])o.normals.Clone(),uv=(Vector2[])o.uv.Clone(),triangles=(int[])o.triangles.Clone()};
  public static bool operator ==(Object a,Object b)=>ReferenceEquals(a,b)||(ReferenceEquals(a,null)&&!ReferenceEquals(b,null)&&b.destroyed)||(ReferenceEquals(b,null)&&!ReferenceEquals(a,null)&&a.destroyed);
  public static bool operator !=(Object a,Object b)=>!(a==b);public override bool Equals(object o)=>ReferenceEquals(this,o);public override int GetHashCode()=>base.GetHashCode();
 }
 public class Component:Object{public GameObject gameObject;public Transform transform=>gameObject.transform;}
 public class MonoBehaviour:Component{}
 public class Transform:Object{public Vector3 localPosition,localScale;public Transform parent;public void SetParent(Transform t,bool world){parent=t;}}
 public class GameObject:Object {
  public readonly Transform transform=new Transform();public readonly Dictionary<Type,Component> components=new Dictionary<Type,Component>();
  public GameObject(string n){name=n;}public T AddComponent<T>()where T:Component,new(){var o=new T{gameObject=this};components[typeof(T)]=o;return o;}
  public T GetComponent<T>()where T:Component=>components.Values.OfType<T>().FirstOrDefault();public void SetActive(bool v){}
  public static GameObject CreatePrimitive(PrimitiveType t){throw new InvalidOperationException("implicit stripped collider requested: "+t);}
 }
 public class Mesh:Object{public Vector3[] vertices,normals;public Vector2[] uv;public int[] triangles;public void RecalculateBounds(){}public void RecalculateTangents(){}}
 public class Renderer:Component{public Material sharedMaterial;}
 public class MeshRenderer:Renderer{public Rendering.ShadowCastingMode shadowCastingMode;public bool receiveShadows;}
 public class MeshFilter:Component{public Mesh sharedMesh;}
 public class Camera:Object{public static Camera main=new Camera();public DepthTextureMode depthTextureMode;}
 public class Shader:Object{}public class Texture2D:Object{}
 public class Material:Object{public Material(Shader s){if(s==null)throw new Exception("missing shader");}public void SetTexture(string n,Texture2D t){}}
 public static class Resources{public static T Load<T>(string n)where T:new()=>new T();}
 public static class Application{public static bool isMobilePlatform=true;}
 namespace Rendering{public enum ShadowCastingMode{On}}
}
namespace Emberfall {
 internal static class SkillBodyMaterialLibrary{internal static void Bind(Material m){}}
 internal static class ProceduralVisuals {
  private static readonly Dictionary<PrimitiveType,Mesh> shapes=new Dictionary<PrimitiveType,Mesh>();
  SHARED_METHODS
 }
 internal class SkillPerformanceVfx:MonoBehaviour {
  private static Mesh windWall,orb,lensQuad;private static Material energy,lens,body;private static int lights;
  private static void ResetDischarges(){}private static Mesh WindCloud()=>ProceduralVisuals.Shape(PrimitiveType.Sphere);
  VFX_METHODS
  internal static void Load()=>Assets();internal static void Clear()=>Reset();
  internal static Mesh[] Owned=>new[]{lensQuad,orb};internal static Material[] Materials=>new[]{energy,lens,body};
 }
 internal class GroundSupplyPickup:MonoBehaviour {
  SUPPLY_METHOD
  internal void Add(PrimitiveType t,Vector3 p,Vector3 s,Material m)=>Part(t,p,s,m);
 }
}
class Program {
 static int checks;static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
 static void Main(){
  var sharedQuad=Emberfall.ProceduralVisuals.Shape(PrimitiveType.Quad);var sharedOrb=Emberfall.ProceduralVisuals.Shape(PrimitiveType.Sphere);
  // WindCloud owns its own geometry in production. The substitute below clones
  // the shared sphere so reset ownership is exercised without copying that recipe.
  Emberfall.SkillPerformanceVfx.Load();var owned=Emberfall.SkillPerformanceVfx.Owned;var materials=Emberfall.SkillPerformanceVfx.Materials;
  Check(owned.All(m=>m!=null)&&materials.All(m=>m!=null),"cold first cast builds all required assets with collider types absent");
  Check(!ReferenceEquals(owned[0],sharedQuad)&&!ReferenceEquals(owned[1],sharedOrb),"spell cache owns clones, not the shared mesh cache");
  Check(owned[0].triangles.SequenceEqual(sharedQuad.triangles)&&owned[1].vertices.Length==sharedOrb.vertices.Length,"lens topology and sphere dimensions preserve factory meshes");
  Check((Camera.main.depthTextureMode&DepthTextureMode.Depth)!=0,"gravity lens still requests scene depth");
  int count=UnityEngine.Object.All.Count;for(int i=0;i<100;i++)Emberfall.SkillPerformanceVfx.Load();
  Check(UnityEngine.Object.All.Count==count&&owned.SequenceEqual(Emberfall.SkillPerformanceVfx.Owned),"100 warm casts reuse meshes and materials without construction");
  Emberfall.SkillPerformanceVfx.Clear();Check(owned.All(m=>m.destroyed)&&materials.All(m=>m.destroyed),"subsystem reset destroys the owned resources");
  Check(!sharedQuad.destroyed&&!sharedOrb.destroyed,"spell reset preserves meshes used by other renderers");
  Emberfall.SkillPerformanceVfx.Load();Check(Emberfall.SkillPerformanceVfx.Owned.All(m=>m!=null)&&!ReferenceEquals(owned[0],Emberfall.SkillPerformanceVfx.Owned[0]),"first cast after reset rebuilds valid owned resources");
  var parent=new GameObject("supply").AddComponent<Emberfall.GroundSupplyPickup>();var material=new Material(new Shader());
  foreach(var type in new[]{PrimitiveType.Cylinder,PrimitiveType.Sphere}){
   for(int i=0;i<50;i++){var pos=new Vector3(.45f,.25f,0);var size=new Vector3(.25f,.35f,.25f);parent.Add(type,pos,size,material);
    var part=UnityEngine.Object.All.OfType<GameObject>().Last();
    Check(part.components.Count==2&&part.GetComponent<MeshFilter>()!=null&&part.GetComponent<MeshRenderer>()!=null,"supply only adds visual filter and renderer");
    Check(part.GetComponent<MeshRenderer>().sharedMaterial==material&&ReferenceEquals(part.GetComponent<MeshFilter>().sharedMesh,Emberfall.ProceduralVisuals.Shape(type)),"coin and potion reuse explicit mesh and supplied material");
    Check(ReferenceEquals(part.transform.parent,parent.transform)&&(part.transform.localPosition-pos).sqrMagnitude==0&&(part.transform.localScale-size).sqrMagnitude==0,"supply placement and size are preserved");
   }
  }
  Console.WriteLine("PASS "+checks+" actual spell assets cold/warm/reset and supply visual-only checks; collider types absent from engine boundary.");
 }
}
'''
fixture = fixture.replace("SHARED_METHODS", "\n".join(member(visual, k) for k in [
    "public static Mesh Shape(", "public static Mesh Build(", "public static GameObject Create("]))
vfx_methods = "\n".join(member(vfx, k) for k in ["private static void Reset()", "private static void Assets()"])
fixture = fixture.replace("VFX_METHODS", vfx_methods).replace("SUPPLY_METHOD", member(supply, "private void Part("))
fixture = fixture.replace("private static Mesh WindCloud()=>ProceduralVisuals.Shape(PrimitiveType.Sphere);", "private static Mesh WindCloud()=>Instantiate(ProceduralVisuals.Shape(PrimitiveType.Sphere));")
math_source = (root / "Tests/ProceduralVisualTests.cs").read_text()
math_source = math_source[math_source.index("namespace UnityEngine"):]
with tempfile.TemporaryDirectory(prefix="skill-collider-free-") as temporary:
    p = Path(temporary)
    (p / "Program.cs").write_text(fixture)
    (p / "Math.cs").write_text("using System;\n" + math_source)
    (p / "VisualMeshRecipes.cs").write_text((root / "Assets/Scripts/Combat/VisualMeshRecipes.cs").read_text())
    (p / "Tests.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>')
    (p / "NuGet.Config").write_text('<configuration><packageSources><clear/></packageSources></configuration>')
    env = dict(os.environ, DOTNET_CLI_HOME=str(p / "cli"), DOTNET_NOLOGO="1")
    command = [sdk, "run", "--project", str(p / "Tests.csproj")]
    subprocess.run(command, env=env, check=True)
    # Reachable old factory is deliberately rejected by the stripped boundary.
    old_quad = 'if(lensQuad==null){var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.SetActive(false);lensQuad=Instantiate(quad.GetComponent<MeshFilter>().sharedMesh);Destroy(quad);}'
    mutant = fixture.replace('if(lensQuad==null)lensQuad=Instantiate(ProceduralVisuals.Shape(PrimitiveType.Quad));', old_quad)
    assert mutant != fixture
    (p / "Program.cs").write_text(mutant)
    result = subprocess.run(command, env=env, capture_output=True, text=True)
    assert result.returncode != 0 and "implicit stripped collider requested: Quad" in result.stdout + result.stderr, result.stdout + result.stderr
    print("PASS reinstating old first-cast CreatePrimitive path fails at stripped collider boundary.")
    old_part = 'private void Part(PrimitiveType type,Vector3 position,Vector3 size,Material material){var p=GameObject.CreatePrimitive(type);p.transform.SetParent(transform,false);p.transform.localPosition=position;p.transform.localScale=size;p.GetComponent<Renderer>().sharedMaterial=material;}'
    mutant = fixture.replace(member(supply, "private void Part("), old_part)
    assert mutant != fixture
    (p / "Program.cs").write_text(mutant)
    result = subprocess.run(command, env=env, capture_output=True, text=True)
    assert result.returncode != 0 and "implicit stripped collider requested: Cylinder" in result.stdout + result.stderr, result.stdout + result.stderr
    print("PASS reinstating old supply CreatePrimitive path fails at stripped collider boundary.")

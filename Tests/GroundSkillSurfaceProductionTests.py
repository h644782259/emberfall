#!/usr/bin/env python3
"""Execute the complete production thin-deposit builder/lifecycle against doubles.

This checks actual mesh topology, support/cliff behavior, alpha coverage, dedup,
pause, parent-time fade, epoch cleanup and pooled-parent reuse. Native textures,
scene illumination/depth and visual acceptance are verified by RunGroundFusion.
"""
from pathlib import Path
import os
import subprocess
import sys
import tempfile

root = Path(os.environ.get('EMBERFALL_TEST_ROOT', Path(__file__).resolve().parents[1]))
source = (root / 'Assets/Scripts/Combat/GroundSkillSurface.cs').read_text()
fixture = r'''
using System;using System.Linq;using System.Collections.Generic;using System.Reflection;using Emberfall;using UnityEngine;
namespace UnityEngine {
 public class Object {public bool Destroyed;public static void Destroy(Object value){if(value==null||value.Destroyed)return;value.Destroyed=true;if(value is GameObject go){go.SetActive(false);foreach(var c in go.Components)c.GetType().GetMethod("OnDestroy",BindingFlags.Instance|BindingFlags.NonPublic)?.Invoke(c,null);foreach(var child in go.transform.Children.ToArray())Destroy(child.gameObject);if(go.transform.parent!=null)go.transform.parent.Children.Remove(go.transform);}}}
 public class Component:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;public T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();}
 public class MonoBehaviour:Component{}
 public class GameObject:Object {public string name;public Transform transform;public bool activeSelf=true;public bool activeInHierarchy=>activeSelf&&(transform.parent==null||transform.parent.gameObject.activeInHierarchy);public List<Component> Components=new List<Component>();public GameObject(string name){this.name=name;transform=new Transform{gameObject=this};}public T AddComponent<T>()where T:Component,new(){var c=new T{gameObject=this};Components.Add(c);return c;}public T GetComponent<T>()where T:Component=>Components.OfType<T>().FirstOrDefault();public void SetActive(bool active){if(activeSelf==active)return;activeSelf=active;foreach(var c in Components)c.GetType().GetMethod(active?"OnEnable":"OnDisable",BindingFlags.Instance|BindingFlags.NonPublic)?.Invoke(c,null);if(!active)foreach(var child in transform.Children.ToArray())foreach(var c in child.gameObject.Components)c.GetType().GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic)?.Invoke(c,null);}}
 public class Transform:Component {public Transform parent;public List<Transform> Children=new List<Transform>();public Vector3 localPosition;public Vector3 lossyScale=>new Vector3(1,1,1);public Vector3 position{get=>localPosition+(parent!=null?parent.position:Vector3.zero);set=>localPosition=value-(parent!=null?parent.position:Vector3.zero);}public void SetParent(Transform other,bool world){parent=other;other.Children.Add(this);}public void SetPositionAndRotation(Vector3 p,Quaternion rotation){position=p;}public Vector3 InverseTransformPoint(Vector3 p)=>p-position;public Vector3 TransformPoint(Vector3 p)=>p+position;}
 public struct Quaternion{public static Quaternion identity=>default;public static Vector3 operator*(Quaternion q,Vector3 v)=>v;}
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 zero=>default;public static Vector3 up=>new Vector3(0,1,0);public static Vector3 right=>new Vector3(1,0,0);public static Vector3 forward=>new Vector3(0,0,1);public float sqrMagnitude=>x*x+y*y+z*z;public float magnitude=>(float)Math.Sqrt(sqrMagnitude);public Vector3 normalized=>magnitude>0?this/magnitude:zero;public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator-(Vector3 a)=>new Vector3(-a.x,-a.y,-a.z);public static Vector3 operator*(Vector3 a,float n)=>new Vector3(a.x*n,a.y*n,a.z*n);public static Vector3 operator/(Vector3 a,float n)=>a*(1/n);public static Vector3 Scale(Vector3 a,Vector3 b)=>new Vector3(a.x*b.x,a.y*b.y,a.z*b.z);public static Vector3 Cross(Vector3 a,Vector3 b)=>new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);}
 public struct Vector2{public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
 public struct Color{public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}}
 public static class Mathf{public const float PI=(float)Math.PI;public static float Cos(float n)=>(float)Math.Cos(n);public static float Sin(float n)=>(float)Math.Sin(n);public static float Min(float a,float b)=>Math.Min(a,b);public static int Min(int a,int b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int Max(int a,int b)=>Math.Max(a,b);public static float Abs(float n)=>Math.Abs(n);public static float Clamp(float n,float a,float b)=>Math.Min(b,Math.Max(a,n));public static int Clamp(int n,int a,int b)=>Math.Min(b,Math.Max(a,n));public static float Clamp01(float n)=>Clamp(n,0,1);public static int CeilToInt(float n)=>(int)Math.Ceiling(n);public static float InverseLerp(float a,float b,float v)=>Clamp01((v-a)/(b-a));public static float SmoothStep(float a,float b,float v){float t=Clamp01(v);return a+(b-a)*t*t*(3-2*t);}public static float PerlinNoise(float x,float z)=>(float)(.5+.25*Math.Sin(x*1.7)+.25*Math.Cos(z*1.3));}
 public class Mesh:Object{public string name;public Vector3[] vertices,normals;public int[] triangles;public Color[] colors;public Vector2[] uv;public void SetVertices(List<Vector3> a){vertices=a.ToArray();}public void SetNormals(List<Vector3> a){normals=a.ToArray();}public void SetUVs(int index,List<Vector2> a){uv=a.ToArray();}public void SetColors(List<Color> a){colors=a.ToArray();}public void SetTriangles(List<int> a,int index){triangles=a.ToArray();}public void RecalculateBounds(){}}
 public class MeshFilter:Component{public Mesh sharedMesh;}public class Renderer:Component{public Material sharedMaterial;public UnityEngine.Rendering.ShadowCastingMode shadowCastingMode;public bool receiveShadows;public Dictionary<string,float> Floats;public void SetPropertyBlock(MaterialPropertyBlock p){Floats=new Dictionary<string,float>(p.Values);}}public class MeshRenderer:Renderer{}
 public class Shader:Object{}public class Material:Object{public Material(Shader shader){}}
 public class MaterialPropertyBlock{public Dictionary<string,float> Values=new Dictionary<string,float>();public void SetFloat(string name,float v){Values[name]=v;}}
 public enum DepthTextureMode{None,Depth}public class Camera{public static Camera main;public DepthTextureMode depthTextureMode;}
 public static class Resources{public static T Load<T>(string key)where T:new()=>new T();}public static class Time{public static float deltaTime=.025f;}
 public enum RuntimeInitializeLoadType{SubsystemRegistration}public sealed class RuntimeInitializeOnLoadMethodAttribute:Attribute{public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type){}}
}
namespace UnityEngine.Rendering{public enum ShadowCastingMode{Off,On}}
namespace Emberfall{
 public class PlayerController:MonoBehaviour{public int CombatEpoch=6;public bool IsDead;}
 public class GameSession{public static GameSession Instance;public PlayerController Player;public bool InputBlocked,CombatEffectsEnded,HasStarted=true;}
 public static class EffectPreferences{public static bool ReducedEffects;}
 public static class SkillBodyMaterialLibrary{public static void Bind(Material m){}}
 public static class CombatFx{public static Vector3 Flat(Vector3 p){p.y=0;return p;}}
 public static class WorldTraversal{public static int Revision;public static bool Cliff,Water,AllWater,Island;public static bool IsOpenWater(Vector3 p,float radius=.16f)=>! (Island&&Math.Abs(p.x-1)<.2f&&Math.Abs(p.z)<.2f)&&(AllWater||(Water&&p.x>.45f-radius));public static float SurfaceHeight(Vector3 p,float extent){return 1.5f+p.x*.12f+p.z*.07f+(Cliff&&p.x>.17f?.9f:0);}}
 public static class CombatSight{public static bool Blocked,DiagonalFans;public static bool Area(Vector3 a,Vector3 b)=>!Blocked||b.z<1.4f;public static bool VisualTriangle(Vector3 origin,Vector3 a,Vector3 b,Vector3 c){if(Math.Abs(origin.y-WorldTraversal.SurfaceHeight(origin,.012f))>.0001f)throw new Exception("clipping origin must use actual support");Vector3 middle=(a+b+c)/3;return !DiagonalFans||middle.x*middle.z>0;}}
}
static class Program{
 const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;static int checks;static void C(bool yes,string message){checks++;if(!yes)throw new Exception(message);}static object Get(object o,string field)=>o.GetType().GetField(field,Private).GetValue(o);static void Tick(object o)=>o.GetType().GetMethod("LateUpdate",Private).Invoke(o,null);
 static Mesh M(GroundSkillSurface g)=>g.GetComponent<MeshFilter>().sharedMesh;static float Opacity(GroundSkillSurface g)=>g.GetComponent<Renderer>().Floats["_Opacity"];
 static void Geometry(GroundSkillSurface fx,float radius){
  Mesh m=M(fx);var v=m.vertices;int nodes=v.Length/2;C(nodes>=81&&m.triangles.Length>100,"production ground has dense filled topology");C(m.colors.Length==v.Length&&m.uv.Length==v.Length,"all production vertices retain alpha and natural UVs");
  float min=100,max=-100;int soft=0;for(int i=0;i<nodes;i++){
   var at=fx.transform.TransformPoint(v[i]);float h=WorldTraversal.SurfaceHeight(at,.012f),offset=at.y-h;C(offset>.003f&&offset<.012f,"wet ground actually follows elevated sloped support within millimetres");C(Math.Abs(v[i].y-v[i+nodes].y-.009f)<.00001f,"actual deposit retains nine millimetre closed thickness");C(float.IsFinite(m.normals[i].x)&&float.IsFinite(m.normals[i].y)&&float.IsFinite(m.normals[i].z),"actual ground has finite scene normals");float distance=CombatFx.Flat(at-fx.transform.position).magnitude;C(distance<=radius+.0001f||m.colors[i].a==0,"outside the authoritative radius stays transparent");C(!CombatSight.Blocked||at.z<1.4f||m.colors[i].a==0,"damage-hidden side cannot contain visible wet coverage");C(!WorldTraversal.IsOpenWater(at,.01f)||m.colors[i].a==0,"open water never receives opaque poisonous soil or ash");min=Math.Min(min,m.colors[i].a);max=Math.Max(max,m.colors[i].a);if(m.colors[i].a>.03f&&m.colors[i].a<.7f)soft++;
  }
  C(min<.01f&&max>.45f&&soft>20,"actual muddy islands span fine feathered coverage rather than a complete carpet");
  var edges=new Dictionary<(int,int),(int,int)>();for(int i=0;i<m.triangles.Length;i+=3){int a=m.triangles[i],b=m.triangles[i+1],c=m.triangles[i+2];C(Vector3.Cross(v[b]-v[a],v[c]-v[a]).sqrMagnitude>1e-12f,"no zero-area top/underside/skirt");if(a<nodes&&b<nodes&&c<nodes){var heights=new[]{WorldTraversal.SurfaceHeight(fx.transform.TransformPoint(v[a]),.012f),WorldTraversal.SurfaceHeight(fx.transform.TransformPoint(v[b]),.012f),WorldTraversal.SurfaceHeight(fx.transform.TransformPoint(v[c]),.012f)};C(heights.Max()-heights.Min()<.28f,"actual top triangles do not bridge a physical cliff");var p=fx.transform.TransformPoint(v[a]);var q=fx.transform.TransformPoint(v[b]);var r=fx.transform.TransformPoint(v[c]);foreach(var sample in new[]{p,q,r,(p+q)*.5f,(q+r)*.5f,(r+p)*.5f,(p+q+r)/3})C(!WorldTraversal.IsOpenWater(sample,.01f),"actual top triangles never bridge the open water shoreline");}foreach(var e in new[]{(a,b),(b,c),(c,a)}){var key=(Math.Min(e.Item1,e.Item2),Math.Max(e.Item1,e.Item2));var old=edges.TryGetValue(key,out var n)?n:(0,0);edges[key]=(old.Item1+1,old.Item2+(e.Item1<e.Item2?1:-1));}}
  foreach(var e in edges.Values)C(e.Item1==2&&e.Item2==0,"closed thin ground has exactly two oppositely wound faces on each welded edge");
 }
 static void Main(){
  var hero=new GameObject("actual caster").AddComponent<PlayerController>();GameSession.Instance=new GameSession{Player=hero};Camera.main=new Camera();
  foreach(bool reduced in new[]{false,true})foreach(bool cliff in new[]{false,true})foreach(bool water in new[]{false,true})foreach(bool fans in new[]{false,true})foreach(var type in new[]{GroundSkillSurface.Deposit.WetPoison,GroundSkillSurface.Deposit.Scorch}){
   EffectPreferences.ReducedEffects=reduced;WorldTraversal.Cliff=cliff;WorldTraversal.Water=water;CombatSight.DiagonalFans=fans;CombatSight.Blocked=true;float remaining=4;var parent=new GameObject("actual skill lifetime owner");
   var fx=GroundSkillSurface.Ensure(parent.transform,hero,new Vector3(0,0,0),3.2f,type,()=>remaining);Geometry(fx,3.2f);C(Camera.main.depthTextureMode==DepthTextureMode.Depth,"runtime asks for a real depth texture");C(fx.GetComponent<Renderer>().receiveShadows&&fx.GetComponent<Renderer>().shadowCastingMode==UnityEngine.Rendering.ShadowCastingMode.Off,"thin deposit receives scene shadows without an opaque disk shadow");C(Opacity(fx)>.1f&&Opacity(fx)<.7f,"first contact has a soft visible entry");
   var otherParent=new GameObject("same real contact pulse");var same=GroundSkillSurface.Ensure(otherParent.transform,hero,new Vector3(.04f,0,.025f),3.2f,type,()=>100);C(ReferenceEquals(fx,same)&&otherParent.transform.Children.Count==0,"actual contact and sustained area never double-layer their wet soil");
   for(int i=0;i<4;i++)Tick(fx);C(Opacity(fx)==1,"stable physical deposit fills its coverage");GameSession.Instance.InputBlocked=true;remaining=.12f;Tick(fx);C(Opacity(fx)==1,"pause freezes opacity and lifetime presentation");GameSession.Instance.InputBlocked=false;Tick(fx);C(Opacity(fx)>.3f&&Opacity(fx)<.5f,"deposit fades against its actual parent remaining lifetime");
   var mesh=M(fx);WorldTraversal.Revision++;Tick(fx);C(mesh.Destroyed&&M(fx)!=mesh,"changed support replaces and destroys the actual conforming mesh");
   parent.SetActive(false);C(fx.gameObject.Destroyed&&M(fx).Destroyed,"pooled parent disable permanently retires owned ground and mesh");parent.SetActive(true);remaining=3;var fresh=GroundSkillSurface.Ensure(parent.transform,hero,Vector3.zero,3.2f,type,()=>remaining);C(!ReferenceEquals(fresh,fx)&&parent.transform.Children.Count==1,"rerented parent cannot revive old ground layers");
   hero.CombatEpoch++;Tick(fresh);C(fresh.gameObject.Destroyed,"new combat epoch cancels an actual deposit");
  }
  WorldTraversal.Cliff=false;WorldTraversal.Water=false;CombatSight.Blocked=false;CombatSight.DiagonalFans=false;
  var trackA=new GameObject("damage slot A");var trackB=new GameObject("damage slot B");float slotRemaining=2;
  var slotA=GroundSkillSurface.Ensure(trackA.transform,hero,Vector3.zero,2.2f,GroundSkillSurface.Deposit.Scorch,()=>slotRemaining,independentLifetime:true);
  var slotB=GroundSkillSurface.Ensure(trackB.transform,hero,Vector3.zero,2.2f,GroundSkillSurface.Deposit.Scorch,()=>slotRemaining,independentLifetime:true);
  C(!ReferenceEquals(slotA,slotB)&&slotA.transform.parent==trackA.transform&&slotB.transform.parent==trackB.transform,"coincident damage slots keep independent actual ground ownership");
  trackA.SetActive(false);C(slotA.gameObject.Destroyed&&!slotB.gameObject.Destroyed,"replacing one coincident damage slot cannot retire another slot soil");for(int i=0;i<4;i++)Tick(slotB);C(Opacity(slotB)==1,"remaining coincident slot stays fully represented after other retires");trackB.SetActive(false);
  WorldTraversal.Cliff=false;WorldTraversal.Water=true;CombatSight.Blocked=false;CombatSight.DiagonalFans=false;
  var bodyRoot=new GameObject("cosmetic root inside actual damage radius");var shape=new Mesh{vertices=new[]{new Vector3(-.3f,0,-.4f),new Vector3(.3f,0,.4f),new Vector3(0,2,0)}};
  var biased=new Mesh{vertices=new[]{new Vector3(.2f,0,-.1f),new Vector3(1.3f,1,.3f),new Vector3(.8f,2,-.2f)}};float actualRadius=GroundSkillSurface.HorizontalRadius(biased);C(Math.Abs(actualRadius-Math.Sqrt(1.3f*1.3f+.3f*.3f))<.00001f,"ground field reserves actual root-relative radial extent including off-center bent branches");foreach(var vertex in biased.vertices)C(CombatFx.Flat(vertex*.3f/actualRadius).magnitude<=.30001f,"actual off-center rooted body fits its full reserved envelope after normalization");
  var preferred=new Vector3(1.1f,0,.4f);var dimensions=new Vector3(1,1,1);float radius=3.2f;
  C(GroundSkillSurface.TryPlaceAttached(bodyRoot.transform,shape,ref preferred,ref dimensions,Quaternion.identity,radius),"actual root placement finds nearby dry ground inside unchanged damage radius");
  float footprint=.5f*dimensions.x;C(CombatFx.Flat(preferred).magnitude+footprint<=radius+.00001f,"fitted complete root footprint remains within authoritative radius");C(!WorldTraversal.IsOpenWater(preferred,footprint),"actual poison and fire root footprint is dry");
  WorldTraversal.Island=true;C(!GroundSkillSurface.AttachedFootprintDry(new Vector3(1,0,0),.6f),"river platform cannot conceal its unsupported root perimeter");WorldTraversal.Island=false;
  WorldTraversal.AllWater=true;var attempted=new Vector3(1,0,1);var original=attempted;dimensions=new Vector3(1,1,1);C(!GroundSkillSurface.TryPlaceAttached(bodyRoot.transform,shape,ref attempted,ref dimensions,Quaternion.identity,radius),"all-water hit does not invent poisonous roots or ground fuel");C((attempted-original).sqrMagnitude==0&&dimensions.x==1,"failed cosmetic placement does not mutate gameplay radius or center");
  foreach(var type in new[]{GroundSkillSurface.Deposit.WetPoison,GroundSkillSurface.Deposit.Scorch}){var allWater=GroundSkillSurface.Ensure(new GameObject("water impact owner").transform,hero,Vector3.zero,2,type,()=>1);C(M(allWater).triangles.Length==0&&M(allWater).colors.All(c=>c.a==0),"all-water contact preserves parent lifetime but emits no deposited ground shell");allWater.Retire();}
  WorldTraversal.AllWater=false;WorldTraversal.Water=false;var root=new GameObject("root");var replaced=GroundSkillSurface.Ensure(root.transform,hero,Vector3.zero,2,GroundSkillSurface.Deposit.WetPoison,()=>2);GameSession.Instance.Player=new GameObject("replacement").AddComponent<PlayerController>();Tick(replaced);C(replaced.gameObject.Destroyed,"replaced hero clears stale deposited matter");
  Console.WriteLine("PASS "+checks+" complete production ground topology, support, cliff, shoreline, point-touching island fans, rooted dry placement, feathering, lifetime, duplicate-contact and pooled-parent assertions; managed, not GPU acceptance");
 }
}
'''
with tempfile.TemporaryDirectory(prefix='emberfall-ground-surface-') as directory:
    work = Path(directory)
    (work / 'GroundSkillSurface.cs').write_text(source)
    (work / 'FilledVfxPlacement.cs').write_text((root / 'Assets/Scripts/Core/FilledVfxPlacement.cs').read_text())
    (work / 'Program.cs').write_text(fixture)
    project = work / 'Validation.csproj'
    project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>')
    config = work / 'NuGet.Config'
    config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    dotnet = sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
    env = dict(os.environ,DOTNET_CLI_HOME=str(work/'cli'),DOTNET_NOLOGO='1')
    subprocess.run([dotnet,'restore',str(project),'--configfile',str(config)],env=env,check=True,stdout=subprocess.DEVNULL)
    command=[dotnet,'run','--project',str(project),'--no-restore','-c','Release']
    subprocess.run(command,env=env,check=True)
    controls=[
      ('point-touching-top-fans', 'SplitTouchingFans(top,vertices,normals,uv,colors);', '', 'closed thin ground has exactly two oppositely wound faces on each welded edge'),
      ('off-center-branch-envelope', 'CombatFx.Flat(point).magnitude);return radius;', 'Mathf.Max(Mathf.Abs(point.x),Mathf.Abs(point.z)));return radius;', 'ground field reserves actual root-relative radial extent'),
      ('water-covered-nodes', '||WorldTraversal.IsOpenWater(point,.01f)', '', 'open water never receives opaque poisonous soil or ash'),
      ('water-bridge-triangle', 'if(!DryFace(world[a],world[b],world[c]))return;', '', 'actual top triangles never bridge the open water shoreline'),
      ('unsupported-root-perimeter', 'if(WorldTraversal.IsOpenWater(edge,.01f))return false;', '', 'river platform cannot conceal its unsupported root perimeter'),
      ('duplicate-skirt', 'triangles.Add(b);triangles.Add(a);triangles.Add(a+nodes);triangles.Add(b);triangles.Add(a+nodes);triangles.Add(b+nodes);','triangles.Add(b);triangles.Add(a);triangles.Add(a+nodes);triangles.Add(b);triangles.Add(a+nodes);triangles.Add(b+nodes);triangles.Add(b);triangles.Add(a);triangles.Add(a+nodes);triangles.Add(b);triangles.Add(a+nodes);triangles.Add(b+nodes);','closed thin ground has exactly two'),
      ('cliff-sheet', 'high-low>Mathf.Max(.26f,cell*.8f)', 'high-low>99f', 'actual top triangles do not bridge a physical cliff'),
      ('raised-mud-disc', 'point.y=h+.005f+fine*.0015f;', 'point.y=h+.25f+fine*.0015f;', 'wet ground actually follows elevated sloped support within millimetres'),
      ('hard-lifetime', 'Mathf.Clamp01(remaining/.30f)', '1f', 'deposit fades against its actual parent remaining lifetime'),
      ('coincident-damage-slot', '(!independentLifetime||layer.transform.parent==parent)&&', '', 'coincident damage slots keep independent actual ground ownership'),
      ('stale-pool-ground', 'private void OnDisable(){Retire();}', 'private void OnDisable(){}', 'pooled parent disable permanently retires'),
    ]
    for name,before,after,expected in controls:
        if before not in source:raise AssertionError('missing production mutation '+name)
        (work/'GroundSkillSurface.cs').write_text(source.replace(before,after,1))
        result=subprocess.run(command,env=env,capture_output=True,text=True)
        if result.returncode==0 or expected not in result.stdout+result.stderr:
            raise AssertionError('negative control did not reach expected assertion '+name+'\n'+result.stdout+result.stderr)
        print('PASS '+name+' negative control rejected',flush=True)
        (work/'GroundSkillSurface.cs').write_text(source)

#!/usr/bin/env python3
"""Real mesh effects, HitFeedback, global lease and decorative Slash; managed engine boundaries."""
from CastReceiptFixtureSources import include_cast_receipt_source
import os,sys,subprocess,tempfile
from pathlib import Path
r=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
def member(s,key):
 a=s.index(key);b=s.index('{',a)+1;n=1
 while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
s=(r/'Tests/FilledVfxAllocationTests.cs').read_text();shell='using System;using System.Linq;using System.Collections.Generic;using System.Reflection;using UnityEngine;'+s[s.index('namespace Emberfall'):]
shell=shell.replace('public static class CombatFx{','public static partial class CombatFx{').replace('public static bool ReducedEffects;','public static bool ReducedEffects,CameraShake;').replace('public void RecalculateNormals(){}','public void MarkDynamic(){}public void RecalculateNormals(){}').replace('public struct Color{','public partial struct Color{').replace('public static class Mathf\n','public static partial class Mathf\n').replace('public static Vector3 forward=>','public static Vector3 right=>new Vector3(1,0,0);public static Vector3 forward=>')
shell=shell.replace('public Vector3 right=>localRotation.Rotate','public Vector3 forward=>localRotation.Rotate(Vector3.forward);public Vector3 up=>localRotation.Rotate(Vector3.up);public Vector3 right=>localRotation.Rotate')
shell=shell.replace('public sealed class PlayerController:MonoBehaviour{','public sealed partial class PlayerController:MonoBehaviour{private GameSession session=>GameSession.Instance;')
# Expand only the engine/resource boundaries used by the current complete emitter.
# The tested FilledSkillVfx, material-contact motion, pool and lease stay production.
shell=shell.replace('public bool IsDead;public int CombatEpoch;','public bool IsDead;public int CombatEpoch;public HeroClass HeroClass;')
shell=shell.replace('public static class WorldTraversal {public static int Revision;}','public static class WorldTraversal {public class ObstacleHandle{}public static int Revision;public static float SurfaceHeight(Vector3 p,float r=.45f)=>0;public static float PhysicalSurfaceHeight(Vector3 p,float r,float h)=>0;public static bool IsPhysicalSpaceClear(Vector3 p,float r,ObstacleHandle ignored=null)=>true;}')
shell=shell.replace('public T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();','public T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();public T[] GetComponentsInChildren<T>(bool include)where T:Component=>GameObject.All.Where(o=>o==gameObject||o.transform.parent==transform).Select(o=>o.GetComponent<T>()).Where(c=>c!=null).ToArray();')
shell=shell.replace('public Vector3 right=>localRotation.Rotate','public Vector3 TransformDirection(Vector3 v)=>localRotation.Rotate(v);public Vector3 right=>localRotation.Rotate')
shell=shell.replace('public sealed class Mesh:Object{','public sealed partial class Mesh:Object{public Bounds bounds;')
shell=shell.replace('public void RecalculateBounds(){}','public void RecalculateBounds(){if(vertices==null||vertices.Length==0){bounds=new Bounds();return;}var lo=new Vector3(vertices.Min(v=>v.x),vertices.Min(v=>v.y),vertices.Min(v=>v.z));var hi=new Vector3(vertices.Max(v=>v.x),vertices.Max(v=>v.y),vertices.Max(v=>v.z));bounds=new Bounds((lo+hi)*.5f,hi-lo);}')
shell=shell.replace('public Material(Shader shader){}','public Material(Shader shader){}public void SetTexture(string n,Texture2D t){}public void SetColor(string n,Color c){}public void SetFloat(string n,float f){}')
shell=shell.replace('public static Quaternion Euler(float x,float y,float z)=>','public static Quaternion Euler(Vector3 v)=>Euler(v.x,v.y,v.z);public static Quaternion Euler(float x,float y,float z)=>')
shell=shell.replace('public float x,y;public Vector2(float x,float y)','public float x,y;public float magnitude=>(float)Math.Sqrt(x*x+y*y);public Vector2(float x,float y)')
shell=shell.replace('public static Vector3 Cross(Vector3 a,Vector3 b)=>','public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;public static Vector3 Cross(Vector3 a,Vector3 b)=>')
shell=shell.replace('public static Vector3 up=>','public static Vector3 down=>new Vector3(0,-1,0);public static Vector3 up=>')
shell=shell.replace('ShadowCastingMode{Off}','ShadowCastingMode{Off,On}')
boundaries=r'''
namespace UnityEngine{
 public enum DepthTextureMode{Depth=1}
 public class Texture2D:Object{}
 public class TextAsset:Object{public byte[] bytes=new byte[0];}
 public struct Bounds{public Vector3 center,size;public Bounds(Vector3 c,Vector3 s){center=c;size=s;}public Vector3 min=>center-size*.5f;}
 public struct Matrix4x4{public Vector3 at,scale;public Quaternion rotation;public static Matrix4x4 identity=>TRS(Vector3.zero,Quaternion.identity,Vector3.one);public static Matrix4x4 TRS(Vector3 p,Quaternion q,Vector3 s)=>new Matrix4x4{at=p,rotation=q,scale=s};public Vector3 MultiplyPoint3x4(Vector3 p)=>at+rotation*Vector3.Scale(p,scale);}
 public struct CombineInstance{public Mesh mesh;public Matrix4x4 transform;}
 public sealed partial class Mesh{public void CombineMeshes(CombineInstance[] pieces,bool merge,bool matrices){var v=new List<Vector3>();var u=new List<Vector2>();var t=new List<int>();foreach(var p in pieces){int first=v.Count;v.AddRange(p.mesh.vertices.Select(x=>matrices?p.transform.MultiplyPoint3x4(x):x));u.AddRange(p.mesh.uv);t.AddRange(p.mesh.triangles.Select(x=>x+first));}vertices=v.ToArray();uv=u.ToArray();triangles=t.ToArray();RecalculateBounds();}}
 public static partial class Mathf{public const float Deg2Rad=PI/180;public static int Min(int a,int b)=>Math.Min(a,b);public static float Exp(float x)=>(float)Math.Exp(x);public static int CeilToInt(float x)=>(int)Math.Ceiling(x);}
 public class Collider{public T GetComponentInParent<T>()where T:class=>null;}public struct RaycastHit{public Collider collider;public Vector3 normal;public float distance;}public enum QueryTriggerInteraction{Ignore}public static class Physics{public const int DefaultRaycastLayers=-1;public static int SphereCastNonAlloc(Vector3 a,float r,Vector3 d,RaycastHit[] hits,float distance,int layers,QueryTriggerInteraction t)=>0;}
}
namespace Emberfall{
 public enum HeroClass{Vanguard,Arcanist,Ranger,Summoner}
 public class EnemyController{}public class SummonedCompanion{}
 internal static class HeroSkillBodyAssets{
  static Mesh Make(FilledMeshRecipe data,string name){var mesh=new Mesh{name=name};mesh.vertices=Enumerable.Range(0,data.Positions.Length/3).Select(i=>new Vector3(data.Positions[i*3],data.Positions[i*3+1],data.Positions[i*3+2])).ToArray();mesh.uv=Enumerable.Range(0,data.Uv.Length/2).Select(i=>new Vector2(data.Uv[i*2],data.Uv[i*2+1])).ToArray();mesh.triangles=data.Triangles;mesh.RecalculateBounds();return mesh;}
  public static readonly Mesh Spirit=Make(FilledVfxRecipes.Crescent(),"Resource boundary spirit volume"),Armor=Make(FilledVfxRecipes.Sword(),"Resource boundary armor volume"),Rock=Make(FilledVfxRecipes.ArcaneShard(),"Resource boundary natural rock"),GroundBreak=Make(FilledVfxRecipes.Rupture(),"Resource boundary fractured ground");
 }
 internal static class EnemySkillPhysicalVfx{public static Mesh GelMesh=>HeroSkillBodyAssets.Rock;}
 internal static class SkillBodyMaterialLibrary{public static void Bind(Material m){}}
 internal static class AuthoredActorMeshes{public static Mesh Decode(byte[] b,string n)=>HeroSkillBodyAssets.Rock;}
 internal static class ElementalCombatVfx{public enum Element{Fire,Ice}public static void Burst(PlayerController p,Vector3 at,float radius,Element element){}}
 internal static class PhysicalSkillVfx{public static GameObject Impact(Vector3 p,Vector3 v,Color c,float radius){throw new Exception("physical fault is outside priority-emission cases");}}
 internal static class SkillLifeBanner{public static bool Pulse(PlayerController p){throw new Exception("life banner is outside priority-emission cases");}}
 internal sealed class GroundSkillSurface:MonoBehaviour{public enum Deposit{Scorch,WetPoison}public static GroundSkillSurface Ensure(Transform parent,PlayerController p,Vector3 at,float radius,Deposit d,Func<float> remaining,bool independentLifetime=false){var root=new GameObject("Ground resource boundary");root.transform.SetParent(parent,false);return root.AddComponent<GroundSkillSurface>();}public static bool TryPlaceAttached(Transform root,Mesh mesh,ref Vector3 at,ref Vector3 scale,Quaternion q,float extent,float inflation=1)=>true;public static float HorizontalRadius(Mesh m)=>m.vertices.Max(v=>CombatFx.Flat(v).magnitude);public void Retire(){gameObject.SetActive(false);}}
}
'''
effects=(r/'Assets/Scripts/Combat/CombatEffects.cs').read_text();slash=member(effects,'public static void Slash(')
test_source=(r/'Tests/EffectPriorityProductionTests.cs').read_text()
# The Windows allocation boundary already provides Pow; keep its existing math
# helper while avoiding a duplicate declaration in the shared priority fixture.
if 'static float Pow(' in shell:
 test_source=test_source.replace('public static float Pow(float a,float b)=>(float)Math.Pow(a,b);','')
with tempfile.TemporaryDirectory(prefix='effect-priority-') as temp:
 p=Path(temp)
 for f in ['Core/CombatVisualBudget','Core/FilledVfxRecipes','Core/FilledVfxPlacement','Combat/CombatVisualLease','Combat/FilledSkillVfx','Combat/AnchoredImpactMesh','Combat/HitFeedback','Combat/PlayerController.BurnFeedback','Combat/PhysicalSkillMotion']:(p/(Path(f).name+'.cs')).write_text((r/('Assets/Scripts/'+f+'.cs')).read_text())
 (p/'CurrentResourceBoundaries.cs').write_text('using System;using System.Linq;using System.Collections.Generic;using UnityEngine;'+boundaries)
 (p/'Shell.cs').write_text(shell);(p/'Test.cs').write_text(test_source);(p/'Slash.cs').write_text('using UnityEngine;namespace Emberfall{public static partial class CombatFx{'+slash+member(effects,'internal static void BurnContact(')+'}}')
 project=p/'Tests.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');include_cast_receipt_source(project);cfg=p/'NuGet.Config';cfg.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');subprocess.run([dotnet,'restore',str(project),'--configfile',str(cfg),'-v:q'],env=env,check=True)
 cmd=[dotnet,'run','--project',str(project),'--no-restore'];subprocess.run(cmd,env=env,check=True)
 for name,a,b,error in [('FilledSkillVfx.cs','.4f,new Color(1,.56f,.16f),.32f,\n                priority:CombatVisualPriority.RealContact);','.4f,new Color(1,.56f,.16f),.32f,\n                priority:finale?CombatVisualPriority.Finale:CombatVisualPriority.RealContact);','dense cash contacts cannot evict their own finale main'),('FilledSkillVfx.cs','if(!confirmedFinale)','if(true)','only confirmed finale survives result; skips and owner exits retire immediately'),('Slash.cs','CombatVisualPriority.Decoration','CombatVisualPriority.ActionBody','decorative Slash cannot displace sustained background'),('HitFeedback.cs','if(CombatVisualLease.Attach(obj,priority)==null)return;','','real contact evicts decoration under the global cap'),('FilledSkillVfx.cs','if(priority==CombatVisualPriority.Finale)\n                fx.Add(rupture','if(false)\n                fx.Add(rupture','element finale retains its minimum short tail')]:
  path=p/name;old=path.read_text();assert old.count(a)==1;path.write_text(old.replace(a,b));subprocess.run([dotnet,'build',str(project),'--no-restore','-v:q'],env=env,check=True,stdout=subprocess.DEVNULL);q=subprocess.run(cmd+['--no-build'],env=env,capture_output=True,text=True);path.write_text(old)
  assert q.returncode and 'System.Exception: '+error in q.stdout+q.stderr,q.stdout+q.stderr
 print('PASS: 5 compiled exact-negative priority/contact/finale controls')

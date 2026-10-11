#!/usr/bin/env python3
"""Three named production scenery partitions; Unity APIs are managed doubles.
Default all compiles once. Legacy source-test entrypoints select a partition.
Unrelated world layout and ornament mesh factories remain explicit boundaries.
"""
import os,sys,tempfile,subprocess,re
from pathlib import Path
root=Path(os.environ.get('EMBERFALL_TEST_SOURCE_ROOT',Path(__file__).resolve().parents[1])).resolve()
harness=Path(__file__).resolve().parent
scope=os.environ.get('SCENERY_TEST_SCOPE','all')
assert scope in ('all','environment','town','hub'),('unknown scenery partition',scope)
dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
def extract(source,signature):
 start=source.index(signature);i=source.index('{',start)+1;depth=1
 while depth:
  depth+=(source[i]=='{')-(source[i]=='}');i+=1
 return source[start:i]
def guard_links():
 world=(root/'Assets/Scripts/World/WorldBuilder.cs').read_text()
 linked=(root/'Assets/Scripts/World/WorldBuilder.LinkedRooms.cs').read_text()
 # These builders are NOT executed by this harness: retain named call-link guards,
 # not raw colors or primitive/occluder token counts.
 assert re.search(r'BuildWaterSurface\([^;]*WaterEnvironment\.Brook\)',extract(world,'private static void BuildWilderness(')),'unexecuted wilderness keeps Brook water call'
 assert re.search(r'BuildWaterSurface\([^;]*WaterEnvironment\.Courtyard\)',linked),'unexecuted linked courtyard keeps Courtyard water call'
 assert re.search(r'BuildWaterBankDetail\(\s*lowland\s*,\s*r\s*,\s*stream\s*\)',extract(world,'private static void BuildWilderness(')),'unexecuted wilderness calls bank detail'
 environment=(root/'Assets/Scripts/World/WorldBuilder.Environment.cs').read_text()
 environment=re.sub(r'//[^\n]*|/\*.*?\*/','',environment,flags=re.S)
 assert not re.search(r'\bTakeDamage\w*\s*\(',environment),'noncombat environment helper never dispatches damage'
 print('PASS:3 explicit source-link guards for unexecuted water/bank wiring and1 noncombat damage boundary',flush=True)
if scope in ('all','environment','hub'):guard_links()
fixture=(root/'Tests/EquipmentCompositionProductionTests.Fixture.cs').read_text().split('public static class EquipmentCompositionProductionTests')[0]
fixture=fixture.replace('bool inactive)','bool inactive=false)')
fixture=fixture.replace('public Material sharedMaterial;','private Material[] slots=new Material[0];public Material[] sharedMaterials{get=>(Material[])slots.Clone();set=>slots=value==null?new Material[0]:(Material[])value.Clone();}public Material sharedMaterial{get=>slots.Length==0?null:slots[0];set{if(slots.Length==0)slots=new Material[1];slots[0]=value;}}')
fixture=fixture.replace('public class MonoBehaviour:Component {}','public class MonoBehaviour:Component {public bool enabled=true;}')
fixture=fixture.replace('public void SetActive(bool a){activeSelf=a;}','public void SetActive(bool a){if(activeSelf==a)return;var all=GetComponentsInChildren<MonoBehaviour>(true);activeSelf=a;foreach(var c in all)Call(c,a?"OnEnable":"OnDisable");}')
fixture=fixture.replace('Call(c,"Awake");return c;','Call(c,"Awake");if(activeInHierarchy)Call(c,"OnEnable");return c;')
fixture=fixture.replace('public Quaternion rotation=>parent==null?localRotation:parent.rotation*localRotation;','public Quaternion rotation{get=>parent==null?localRotation:parent.rotation*localRotation;set=>localRotation=parent==null?value:Quaternion.Inverse(parent.rotation)*value;}')
fixture=fixture.replace('public static Quaternion Euler(float x,float y,float z)=>','public static Quaternion FromToRotation(Vector3 a,Vector3 b){var axis=Vector3.Cross(a,b);return new Quaternion{q=System.Numerics.Quaternion.Normalize(new System.Numerics.Quaternion(axis.x,axis.y,axis.z,1+a.x*b.x+a.y*b.y+a.z*b.z))};}public static Quaternion Euler(float x,float y,float z)=>')
fixture=fixture.replace('public float sqrMagnitude=>','public static float Distance(Vector3 a,Vector3 b)=>(a-b).magnitude;public float sqrMagnitude=>',1)
fixture=fixture.replace('public Material(Shader s){}','public Material(Shader s){color=Color.white;}public Material(Material source){color=source.color;}public int renderQueue;public void SetInt(string n,int v){}public void DisableKeyword(string s){}')
fixture=fixture.replace('public static float Sin(float x)=>','public static float Min(float a,float b)=>Math.Min(a,b);public static int Max(int a,int b)=>Math.Max(a,b);public static float Sin(float x)=>')
fixture=fixture.replace('public class Renderer:Component {','public class Renderer:Component {public Bounds bounds{get{var mesh=GetComponent<MeshFilter>()?.sharedMesh;var points=mesh==null?new[]{transform.position}:mesh.vertices.Select(v=>transform.TransformPoint(v)).ToArray();return new Bounds{min=new Vector3(points.Min(v=>v.x),points.Min(v=>v.y),points.Min(v=>v.z)),max=new Vector3(points.Max(v=>v.x),points.Max(v=>v.y),points.Max(v=>v.z))};}}')
fixture=fixture.replace('public class LineRenderer:Renderer {','public class TextMesh:Component {}public class LineRenderer:Renderer {public bool loop;public float widthMultiplier;public Color startColor,endColor;public void SetPositions(Vector3[] value){points=value;}')
fixture+='''\nnamespace UnityEngine {public struct Bounds {public Vector3 min,max;public void Expand(float v){min-=Vector3.one*(v*.5f);max+=Vector3.one*(v*.5f);}public Vector3 ClosestPoint(Vector3 p)=>new Vector3(Mathf.Clamp(p.x,min.x,max.x),Mathf.Clamp(p.y,min.y,max.y),Mathf.Clamp(p.z,min.z,max.z));}}\nnamespace UnityEngine.Rendering {public enum BlendMode {SrcAlpha,OneMinusSrcAlpha}}\n'''
# Extend only the test Unity API boundary. Production geometry/material/group logic stays live.
fixture=fixture.replace('SetParent(Transform p,bool world)','SetParent(Transform p,bool world=true)')
fixture=fixture.replace('public float sqrMagnitude=>','public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*t;public float sqrMagnitude=>',1)
fixture=fixture.replace('public static float Sin(float x)=>','public const float Rad2Deg=180/PI;public static float Max(float a,float b)=>Math.Max(a,b);public static float Pow(float x,float y)=>(float)Math.Pow(x,y);public static float Sin(float x)=>')
fixture=fixture.replace('public static float deltaTime=.016f;','public static float deltaTime=.016f,time;')
fixture=fixture.replace('public void RecalculateNormals(){}','public void SetVertices(List<Vector3> v){vertices=v.ToArray();}public void SetTriangles(List<int> t,int submesh){triangles=t.ToArray();}public void RecalculateNormals(){}')
fixture=fixture.replace('public int positionCount;','int count;public int positionCount{get=>count;set{count=value;Array.Resize(ref points,value);}}public int numCornerVertices,numCapVertices;public bool generateLightingData;')
fixture=fixture.replace('public void SetFloat(string n,float v){}','public readonly Dictionary<string,float> floats=new Dictionary<string,float>();public void SetFloat(string n,float v){floats[n]=v;}public float GetFloat(string n)=>floats.TryGetValue(n,out var v)?v:0;')
fixture+='''
namespace UnityEngine {
 public class Collider:Component{} public class Rigidbody:Component{}
 public class Light:Component{public LightType type;public Color color;public float intensity,range,shadowStrength,shadowBias,shadowNormalBias;public LightShadows shadows;public LightRenderMode renderMode;}
 public enum LightType{Directional,Point}public enum LightShadows{None,Soft}public enum LightRenderMode{Auto,ForceVertex}public enum FogMode{ExponentialSquared}
 public static class RenderSettings{public static Rendering.AmbientMode ambientMode;public static Color ambientSkyColor,ambientEquatorColor,ambientGroundColor,fogColor;public static bool fog;public static FogMode fogMode;public static float fogDensity;}
 public static class ColorUtility{public static string ToHtmlStringRGBA(Color c)=>string.Join(",",c.r,c.g,c.b,c.a);}
}namespace UnityEngine.Rendering{public enum AmbientMode{Trilight}}
'''
world=(root/'Assets/Scripts/World/WorldBuilder.cs').read_text()
methods=['public static GameObject Build(', 'private static Transform Region(', 'private static void Tree(', 'private static void Tent(', 'private static GameObject Primitive(', 'private static void Portal(', 'private static void Crystal(', 'private static GameObject Cone(', 'private static GameObject Ring(', 'private static void PointLight(']
actual='using System.Collections.Generic;using UnityEngine;namespace Emberfall{public static partial class WorldBuilder {'+'\n'.join(extract(world,m) for m in methods)+'}\n'+extract(world,'public sealed class WorldResources')+'}'
# Execute the actual short timber-crossing recipe, with its unrelated road material as boundary.
a=world.index('            Material timber =');b=world.index('            BuildBridgeWaterContact',a)
actual+='namespace Emberfall{public static partial class WorldBuilder{public static void TestBridge(Transform lowland,WorldResources r){var gold=r.Material(Color.white);'+world[a:b]+'}}}'
actual+='namespace Emberfall{public sealed partial class GameSession{'+extract((root/'Assets/Scripts/Core/GameSession.Hubs.cs').read_text(),'public static Vector3 HubNpcPosition(')+'}}'
types=(root/'Assets/Scripts/Core/GameTypes.cs').read_text();data='namespace Emberfall{'+''.join(re.findall(r'public enum (?:HeroClass|Rarity|ZoneKind)\s*\{[^}]*\}',types))+'}'
with tempfile.TemporaryDirectory(prefix='scenery-presentation-') as directory:
 p=Path(directory);(p/'Fixture.cs').write_text(fixture);(p/'Types.cs').write_text(data);(p/'World.cs').write_text(actual)
 for f in ['World/WorldBuilder.Hubs','World/WorldBuilder.Environment','World/WorldBuilder.AuthoredScenery','World/HubSettlementPlan','World/HubNpcIdle','Core/EnvironmentLightProfile','Core/BuildingOcclusionGroup','Core/CameraOcclusionSurface','Core/CameraVisibilityRules','Combat/ProceduralVisuals','Combat/VisualMeshRecipes']:(p/(Path(f).name+'.cs')).write_text((root/('Assets/Scripts/'+f+'.cs')).read_text())
 # Observe the actual ApplySurface argument (including distinctions such as
 # Foliage vs Cloth that have identical current shader floats). No logic is replaced.
 material=p/'ProceduralVisuals.cs';s=material.read_text();marker='bool metal = surface == VisualSurface.Metal';assert marker in s;material.write_text(s.replace(marker,'SceneryTrace.Record(material,surface);'+marker))
 (p/'Tests.cs').write_text((harness/'SceneryPresentationProductionTests.cs').read_text())
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
 def build():
  q=subprocess.run([dotnet,'build',str(project),'--no-restore','-v:q'],env=env,capture_output=True,text=True);assert q.returncode==0,q.stdout+q.stderr
 subprocess.run([dotnet,'restore',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,check=True)
 build();run=[dotnet,str(p/'bin/Debug/net8.0/Test.dll'),scope];q=subprocess.run(run,env=env,capture_output=True,text=True);print(q.stdout,end='');assert q.returncode==0,q.stdout+q.stderr
 # Every control compiles, then must fail its own behavioral assertion.
 controls=[
 ('environment','WorldBuilder.AuthoredScenery.cs','false,VisualSurface.Foliage','false,VisualSurface.Cloth','actual tree assigns Foliage and Wood categories'),
 ('environment','WorldBuilder.AuthoredScenery.cs','false,VisualSurface.Wood','false,VisualSurface.Stone','actual tree assigns Foliage and Wood categories'),
 ('environment','World.cs','ApplyEnvironmentLighting(dungeon,hub,light,rim);','','factory applies actual environment light profile'),
 ('environment','World.cs','light.shadows=LightShadows.None','light.shadows=LightShadows.Soft','actual point lights remain unshadowed vertex accents'),
 ('environment','World.cs','BuildPortalFocus(parent,r,p);','','town portal invokes six focus insets'),
 ('environment','WorldBuilder.Environment.cs','// Flush, opaque travel inlay;','WorldTraversal.AddCircle(center,.4f); // Flush, opaque travel inlay;','focus and light pools remain cosmetic without traversal physics or occlusion'),
 ('town','WorldBuilder.Hubs.cs','BuildWorkshopRoof(building.transform,r,p,side,roof,stone)','BuildWorkshopRoof(parent,r,p,side,roof,stone)','complete upper building parts include roof helper and late ornaments'),
 ('town','WorldBuilder.Hubs.cs','HubSettlementPlan.RegisterTownNavigation(hub);','','town registers each building solid exactly once'),
 ('town','HubSettlementPlan.cs','new Vector2(BuildingWidth,BuildingWidth)','new Vector2(BuildingWidth*.8f,BuildingWidth)','town registers each building solid exactly once'),
 ('town','BuildingOcclusionGroup.cs','AssignGroup(group)','AssignGroup(null)','all upper parts enter real registry and own building group'),
 ('town','BuildingOcclusionGroup.cs','if(renderer.GetComponent<TextMesh>()!=null','if(renderer.transform.name=="Observatory roof spire"||renderer.GetComponent<TextMesh>()!=null','all upper parts enter real registry and own building group'),
 ('town','WorldBuilder.Hubs.cs','building.transform.SetParent(parent,false);','building.transform.SetParent(parent,false);building.transform.localPosition=Vector3.right;','building root preserves parent and identity transform'),
 ('hub','WorldBuilder.Hubs.cs','HubSettlementPlan.RegisterNpcNavigation(index);','','each NPC registers body and role-prop footprint exactly once'),
 ('hub','HubNpcIdle.cs','Mathf.Sin(age*1.3f)','Mathf.Sin(Time.time*1.3f)','same NPC simulation state ignores wall clock during active idle'),
 ('hub','HubNpcIdle.cs','if(Time.deltaTime<=0)return;age+=Time.deltaTime;','age=Time.time;','actual NPC pose and age freeze while paused despite wall clock'),
 ('hub','WorldBuilder.Hubs.cs','Material edge=r.Material(','WorldTraversal.AddCircle(Vector3.zero,.2f);Material edge=r.Material(','ground and bank detail do not mutate navigation or occlusion'),
 ('hub','WorldBuilder.Hubs.cs','Initialize(index,right,left,dial)','Initialize(index,null,left,dial)','actual NPC initializer binds articulated arms and exchange dial'),
 ('hub','World.cs','return HubSettlementPlan.Npc(index);','return HubSettlementPlan.Npc(index)+Vector3.right;','NPC rendering and session interaction share authored positions'),
 ]
 for part,file,old,new,expected in controls:
  if scope not in ('all',part):continue
  path=p/file;original=path.read_text();assert old in original,(file,old);path.write_text(original.replace(old,new));build()
  q=subprocess.run([dotnet,str(p/'bin/Debug/net8.0/Test.dll'),part],env=env,capture_output=True,text=True)
  assert q.returncode!=0 and 'System.Exception: '+expected in q.stdout+q.stderr,q.stdout+q.stderr
  path.write_text(original);print('PASS: compiled '+part+' control rejected: '+expected,flush=True)

 # Benign refactors that broke the old source scripts must continue to pass.
 for part,file,old,new,label in [
  ('environment','WorldBuilder.AuthoredScenery.cs','new Color(.27f,.20f,.14f)','new Color(.31f,.22f,.18f)','tree bark color may change without losing Wood category'),
  ('town','WorldBuilder.AuthoredScenery.cs','cameraOccluder:true','cameraOccluder:false','late building group owns roof registration even without redundant flags')]:
  if scope not in ('all',part):continue
  path=p/file;original=path.read_text();assert old in original;path.write_text(original.replace(old,new));build()
  q=subprocess.run([dotnet,str(p/'bin/Debug/net8.0/Test.dll'),part],env=env,capture_output=True,text=True);assert q.returncode==0,q.stdout+q.stderr
  path.write_text(original);print('PASS: compiled behavior-preserving control: '+label,flush=True)

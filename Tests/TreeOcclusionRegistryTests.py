"""Actual authored wilderness tree loop and REAL bounded camera registry/material lifecycle.
Unity object/math/material APIs are managed doubles; no GPU fade/render claim.
"""
from pathlib import Path
exec((Path(__file__).with_name('EquipmentCompositionProductionTests.py')).read_text().split('with tempfile.TemporaryDirectory')[0])
with tempfile.TemporaryDirectory(prefix='tree-occlusion-') as directory:
 p=Path(directory)
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
 (p/'Fixture.cs').write_text(fixture);(p/'Types.cs').write_text(data)
 world=(root/'Assets/Scripts/World/WorldBuilder.cs').read_text();a=world.index('            Vector3[] trees =');b=world.index('            for (int i = 0; i < 12;',a)
 actual_loop=world[a:b]
 hook='using UnityEngine;namespace Emberfall {public static partial class WorldBuilder {'+extract(world,'private static void Tree(')+extract(world,'private static void Rock(')+'public static void TestWildernessTrees(Transform parent,WorldResources r){var woodland=parent;var ruins=parent;var stone=r.Material(Color.white);'+actual_loop+'}}}'
 (p/'Hook.cs').write_text(hook)
 for f in ['Assets/Scripts/Core/CameraOcclusionSurface.cs','Assets/Scripts/Core/CameraVisibilityRules.cs','Assets/Scripts/Core/BuildingOcclusionGroup.cs','Assets/Scripts/World/WorldBuilder.AuthoredScenery.cs','Assets/Scripts/Combat/ProceduralVisuals.cs','Assets/Scripts/Combat/VisualMeshRecipes.cs','Tests/TreeOcclusionRegistryTests.cs']:(p/Path(f).name).write_text((root/f).read_text())
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');cmd=[dotnet,'run','--project',str(project),'--no-restore'];subprocess.run([dotnet,'restore',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,check=True);subprocess.run(cmd,env=env,check=True)
 source=p/'WorldBuilder.AuthoredScenery.cs';original=source.read_text();source.write_text(original.replace('CameraOcclusionSurface.MarkHierarchy(root.gameObject);','foreach(var renderer in root.GetComponentsInChildren<Renderer>())CameraOcclusionSurface.Mark(renderer.gameObject);'))
 subprocess.run([dotnet,'build',str(project),'--no-restore','-v:q'],env=env,check=True,stdout=subprocess.DEVNULL);result=subprocess.run(cmd+['--no-build'],env=env,capture_output=True,text=True)
 assert result.returncode and 'System.Exception: real registry admits every wilderness tree as one logical surface' in result.stdout+result.stderr,result.stdout+result.stderr
 print('PASS: compiled per-leaf/per-limb registration control exhausts real cap and fails exact registry oracle')

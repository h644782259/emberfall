#!/usr/bin/env python3
"""Run the complete production banner against mesh/time/lifecycle doubles.

Validates its actual thick-cloth topology, pinning, dynamic deformation, real
owner/epoch response and cleanup. Native shader/pixel acceptance remains Unity.
"""
from pathlib import Path
import os
import subprocess
import sys
import tempfile

root = Path(os.environ.get('EMBERFALL_TEST_ROOT', Path(__file__).resolve().parents[1]))
source = (root / 'Assets/Scripts/Combat/SkillLifeBanner.cs').read_text()
fixture = r'''
using System;using System.Linq;using System.Collections.Generic;using System.Reflection;
namespace UnityEngine {
 public class Object {public bool Destroyed;public static void Destroy(Object value){if(value==null||value.Destroyed)return;value.Destroyed=true;if(value is GameObject go){go.SetActive(false);foreach(var component in go.Components)component.GetType().GetMethod("OnDestroy",BindingFlags.Instance|BindingFlags.NonPublic)?.Invoke(component,null);foreach(var child in go.transform.Children.ToArray())Destroy(child.gameObject);}}}
 public class Component:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;}
 public class MonoBehaviour:Component{}
 public class GameObject:Object {public string name;public Transform transform;public bool activeInHierarchy=true;public List<Component> Components=new List<Component>();public GameObject(string name){this.name=name;transform=new Transform{gameObject=this};}public T AddComponent<T>() where T:Component,new(){var c=new T{gameObject=this};Components.Add(c);return c;}public T GetComponent<T>() where T:Component=>Components.OfType<T>().FirstOrDefault();public void SetActive(bool active){activeInHierarchy=active;foreach(var c in Components)c.GetType().GetMethod(active?"OnEnable":"OnDisable",BindingFlags.Instance|BindingFlags.NonPublic)?.Invoke(c,null);}}
 public class Transform:Component {public Transform parent;public List<Transform> Children=new List<Transform>();public Vector3 localPosition;public Vector3 position{get=>localPosition+(parent!=null?parent.position:Vector3.zero);set=>localPosition=value-(parent!=null?parent.position:Vector3.zero);}public void SetParent(Transform other,bool world){parent=other;other.Children.Add(this);}}
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 zero=>default;public static Vector3 up=>new Vector3(0,1,0);public float magnitude=>(float)Math.Sqrt(x*x+y*y+z*z);public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator*(Vector3 a,float n)=>new Vector3(a.x*n,a.y*n,a.z*n);public static Vector3 Cross(Vector3 a,Vector3 b)=>new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;}
 public struct Vector2{public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
 public struct Color{public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color operator*(Color c,float v)=>new Color(c.r*v,c.g*v,c.b*v,c.a*v);}
 public static class Mathf{public static float Sin(float n)=>(float)Math.Sin(n);public static float Max(float a,float b)=>Math.Max(a,b);}
 public class Mesh:Object{public string name;private Vector3[] v;public Vector3[] vertices{get=>(Vector3[])v.Clone();set=>v=(Vector3[])value.Clone();}public int[] triangles;public Vector2[] uv;public void RecalculateNormals(){}public void RecalculateTangents(){}public void RecalculateBounds(){}}
 public class MeshFilter:Component{public Mesh sharedMesh;}public class MeshRenderer:Component{public Material sharedMaterial;}
 public class Shader:Object{}public class Material:Object{public string name;public Color color;public Shader shader;public Dictionary<string,Color> colors=new Dictionary<string,Color>();public Material(Shader shader){this.shader=shader;}public void SetFloat(string name,float n){}public void SetColor(string name,Color c){colors[name]=c;}}
 public static class Resources{public static T Load<T>(string key)where T:new()=>new T();}
 public static class Time{public static float deltaTime=.025f;}
 public enum RuntimeInitializeLoadType{SubsystemRegistration}public sealed class RuntimeInitializeOnLoadMethodAttribute:Attribute{public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type){}}
}
namespace Emberfall {
 using UnityEngine;
 public class PlayerController:MonoBehaviour{public int CombatEpoch=5;public bool IsDead;}
 public class GameSession{public static GameSession Instance;public PlayerController Player;public bool InputBlocked,CombatEffectsEnded;}
 public static class WorldTraversal{public static float SurfaceHeight(Vector3 at,float footprint){if(footprint!=.04f)throw new Exception("wrong footprint");return 1.3f;}}
 public static class SkillBodyMaterialLibrary{public enum Matter{Wood}public static void Apply(Material m,Matter surface){m.name+=" / "+surface;}}
 public enum VisualSurface{Cloth,Wood}public static class ProceduralVisuals{public static void ApplySurface(Material m,VisualSurface surface){m.name+=" / "+surface;}}
 public static class NaturalActorMeshFactory{public static Mesh CurveTube(string name,Vector3[] path,float r){if(path.Length!=3||r!=.035f||path[2].y!=2.65f)throw new Exception("production solid pole recipe");return new Mesh{name=name};}}
 public class OwnedCombatMesh:MonoBehaviour{public Mesh Value;private void OnDestroy(){if(Value!=null)Object.Destroy(Value);}}
 static class Program{
  const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  static int checks;static void C(bool yes,string message){checks++;if(!yes)throw new Exception(message);}static object Get(object o,string field)=>o.GetType().GetField(field,Private).GetValue(o);static void Invoke(object o,string method)=>o.GetType().GetMethod(method,Private).Invoke(o,null);
  static PlayerController Hero(){return new GameObject("Actual healing owner").AddComponent<PlayerController>();}
  static SkillLifeBanner Spawn(PlayerController hero){var anchor=new GameObject("Production healing anchor");anchor.transform.position=new Vector3(2,0,3);SkillLifeBanner.Spawn(anchor.transform,hero);return anchor.transform.Children[0].gameObject.GetComponent<SkillLifeBanner>();}
  static Mesh Cloth(SkillLifeBanner b)=>(Mesh)Get(b,"cloth");
  static void Geometry(Mesh mesh){
   var v=mesh.vertices;var edges=new Dictionary<(int,int),(int,int)>();double volume=0;var tr=mesh.triangles;
   C(v.Length==270&&mesh.uv.Length==270,"real cloth has two dense skins and UVs");
   for(int i=0;i<tr.Length;i+=3){int a=tr[i],b=tr[i+1],c=tr[i+2];C((Vector3.Cross(v[b]-v[a],v[c]-v[a])).magnitude>.00001f,"no zero-area cloth triangle");volume+=Vector3.Dot(v[a],Vector3.Cross(v[b],v[c]))/6d;foreach(var e in new[]{(a,b),(b,c),(c,a)}){var key=(Math.Min(e.Item1,e.Item2),Math.Max(e.Item1,e.Item2));var n=edges.TryGetValue(key,out var old)?old:(0,0);edges[key]=(n.Item1+1,n.Item2+(e.Item1<e.Item2?1:-1));}}
   foreach(var e in edges.Values)C(e.Item1==2&&e.Item2==0,"closed cloth winding uses every directed edge once each way");
   C(Math.Abs(volume-.98*.70*.016)<.000004,"thick flexible cloth encloses positive physical volume");
   for(int i=0;i<135;i++){C(Math.Abs(v[i].x-v[i+135].x)<.000001&&Math.Abs(v[i].y-v[i+135].y)<.000001&&Math.Abs(v[i].z-v[i+135].z-.016f)<.000001,"both cloth skins retain material thickness");C(float.IsFinite(v[i].x)&&float.IsFinite(v[i].y)&&float.IsFinite(v[i].z),"finite production cloth deformation");}
  }
  static void Main(){
   var hero=Hero();GameSession.Instance=new GameSession{Player=hero};var flag=Spawn(hero);var mesh=Cloth(flag);Geometry(mesh);
   C(Math.Abs(flag.transform.position.y-1.3f)<.0001,"planted pole sits on actual elevated surface");C(flag.transform.Children.Count==2&&flag.transform.Children.All(t=>t.gameObject.GetComponent<OwnedCombatMesh>()!=null),"both production meshes have explicit owners");
   var initial=mesh.vertices;for(int j=0;j<10;j++)Invoke(flag,"LateUpdate");var animated=mesh.vertices;
   C((animated[14]-initial[14]).magnitude>.003,"wind changes the free edge of the real mesh");for(int row=0;row<9;row++)C((animated[row*15]-initial[row*15]).magnitude<.000001,"cloth remains pinned to its actual pole");Geometry(mesh);
   GameSession.Instance.InputBlocked=true;var frozen=mesh.vertices;Invoke(flag,"LateUpdate");C(mesh.vertices.Zip(frozen,(a,b)=>(a-b).magnitude).All(n=>n==0),"pause freezes cloth and visual clock");GameSession.Instance.InputBlocked=false;
   var material=(Material)Get(flag,"clothMaterial");C(SkillLifeBanner.Pulse(hero),"matching real healing owner can respond");Invoke(flag,"LateUpdate");C(material.colors["_EmissionColor"].g>.1f,"successful heal gives woven cloth a visible response");for(int j=0;j<20;j++)Invoke(flag,"LateUpdate");C(material.colors["_EmissionColor"].g==0,"response decays without an unconditional health mutation");
   C(!SkillLifeBanner.Pulse(Hero()),"different healing owner never pulses this flag");hero.CombatEpoch++;C(!SkillLifeBanner.Pulse(hero),"stale epoch cannot pulse a surviving renderer");GameSession.Instance.InputBlocked=true;Invoke(flag,"LateUpdate");C(!flag.gameObject.activeInHierarchy&&flag.gameObject.Destroyed,"epoch cancellation retires even during pause");C(mesh.Destroyed&&material.Destroyed,"flag destroys its owned mesh and material");
   GameSession.Instance.InputBlocked=false;var older=Spawn(hero);var newer=Spawn(hero);older.gameObject.SetActive(false);C(SkillLifeBanner.Pulse(hero),"retiring an old flag cannot erase the replacement registration");newer.gameObject.SetActive(false);C(!SkillLifeBanner.Pulse(hero),"disabled current flag has no healing response");
   var terminal=Spawn(hero);GameSession.Instance.CombatEffectsEnded=true;Invoke(terminal,"LateUpdate");C(terminal.gameObject.Destroyed,"terminal combat retires healing identity");
   Console.WriteLine("PASS "+checks+" complete production life-banner topology, deformation, pulse, owner and cleanup checks (managed; not GPU acceptance)");
  }
 }
}
'''

with tempfile.TemporaryDirectory(prefix='emberfall-life-banner-') as directory:
    work = Path(directory)
    (work / 'Program.cs').write_text(fixture)
    (work / 'SkillLifeBanner.cs').write_text(source)
    project = work / 'Validation.csproj'
    project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>')
    config = work / 'NuGet.Config'
    config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    dotnet = sys.argv[1] if len(sys.argv) > 1 else os.environ.get('DOTNET', 'dotnet')
    env = dict(os.environ, DOTNET_CLI_HOME=str(work / 'cli'), DOTNET_NOLOGO='1')
    subprocess.run([dotnet, 'restore', str(project), '--configfile', str(config)], env=env, check=True, stdout=subprocess.DEVNULL)
    command = [dotnet, 'run', '--project', str(project), '--no-restore', '-c', 'Release']
    subprocess.run(command, env=env, check=True)
    controls = [
        ('flipped-cloth-face', 'a, b, c, b, d, c', 'a, c, b, b, d, c', 'closed cloth winding'),
        ('unpinned-fabric', ') * x;', ');', 'cloth remains pinned'),
        ('stale-healing-pulse', ' || banner.epoch != hero.CombatEpoch', '', 'stale epoch cannot pulse'),
        ('stale-owner-cancellation', ' || owner.CombatEpoch != epoch', '', 'epoch cancellation retires'),
    ]
    for name, before, after, expected in controls:
        if before not in source:
            raise AssertionError('mutation target missing: ' + name)
        (work / 'SkillLifeBanner.cs').write_text(source.replace(before, after, 1))
        result = subprocess.run(command, env=env, capture_output=True, text=True)
        if result.returncode == 0 or expected not in result.stdout + result.stderr:
            raise AssertionError('mutation was not rejected: ' + name + '\n' + result.stdout + result.stderr)
        print('PASS: ' + name + ' production mutation rejected', flush=True)

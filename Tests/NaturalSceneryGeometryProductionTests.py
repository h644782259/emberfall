#!/usr/bin/env python3
"""Execute unmodified production topology recipes; test volume/manifold/envelope properties.
Managed Mesh is an array container only. Native material/lighting acceptance is separate.
"""
import os, subprocess, sys, tempfile
from pathlib import Path
root = Path(__file__).resolve().parents[1]
dotnet = sys.argv[1] if len(sys.argv) > 1 else os.environ.get('DOTNET', 'dotnet')
stubs = r'''
using System; using System.Collections.Generic;
namespace UnityEngine {
 public enum RuntimeInitializeLoadType {SubsystemRegistration}
 public class RuntimeInitializeOnLoadMethodAttribute:Attribute {public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType value){}}
 public enum PrimitiveType {Cube,Sphere,Cylinder,Capsule,Quad}
 public class Object { public string name; }
 public class GameObject:Object { public MeshFilter Filter=new MeshFilter(); public Renderer Render=new Renderer(); public T GetComponent<T>() where T:class {return typeof(T)==typeof(MeshFilter)?Filter as T:typeof(T)==typeof(Renderer)?Render as T:null;} }
 public class MeshFilter:Object { public Mesh sharedMesh; }
 public class Renderer:Object { public Material sharedMaterial; }
 public class Material { public bool HasProperty(string s)=>false;public Color GetColor(string s)=>new Color(); }
 public struct Color { public float r,g,b,a; }
 public class Mesh:Object {public Vector3[] vertices;public Vector2[] uv;public int[] triangles;public Bounds bounds;public void RecalculateNormals(){}public void RecalculateTangents(){}public void RecalculateBounds(){if(vertices==null||vertices.Length==0)return;var a=vertices[0];var b=a;foreach(var v in vertices){a=Vector3.Min(a,v);b=Vector3.Max(b,v);}bounds=new Bounds((a+b)*.5f,b-a);} }
 public struct Vector2 { public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;} }
 public struct Vector3 { public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 zero=>new Vector3();public static Vector3 one=>new Vector3(1,1,1);public float sqrMagnitude=>x*x+y*y+z*z;public Vector3 normalized=>sqrMagnitude>0?this/(float)Math.Sqrt(sqrMagnitude):zero;public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);public static Vector3 operator /(Vector3 a,float b)=>a*(1/b);public static Vector3 Scale(Vector3 a,Vector3 b)=>new Vector3(a.x*b.x,a.y*b.y,a.z*b.z);public static Vector3 Min(Vector3 a,Vector3 b)=>new Vector3(Math.Min(a.x,b.x),Math.Min(a.y,b.y),Math.Min(a.z,b.z));public static Vector3 Max(Vector3 a,Vector3 b)=>new Vector3(Math.Max(a.x,b.x),Math.Max(a.y,b.y),Math.Max(a.z,b.z)); }
 public struct Vector3Int {public int x,y,z;public Vector3Int(int x,int y,int z){this.x=x;this.y=y;this.z=z;} }
 public struct Bounds {public Vector3 center,size;public Bounds(Vector3 c,Vector3 s){center=c;size=s;} }
 public static class Mathf {public const float PI=(float)Math.PI;public static float Sin(float v)=>(float)Math.Sin(v);public static float Cos(float v)=>(float)Math.Cos(v);public static float Pow(float a,float b)=>(float)Math.Pow(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static float Abs(float v)=>Math.Abs(v);public static float Clamp(float v,float a,float b)=>Math.Clamp(v,a,b);public static float Clamp01(float v)=>Math.Clamp(v,0,1);public static int RoundToInt(float v)=>(int)Math.Round(v); }
}
namespace Emberfall {public class WorldResources {public readonly List<UnityEngine.Mesh> Owned=new List<UnityEngine.Mesh>();public void Own(UnityEngine.Mesh m){Owned.Add(m);}}}
'''
test = r'''
using System;using System.Collections.Generic;using Emberfall;using UnityEngine;
class Test {
 static int checks,meshes;static void C(bool value,string message){checks++;if(!value)throw new Exception(message);}
 static Vector3 Cross(Vector3 a,Vector3 b)=>new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);
 static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
 static void Validate(NaturalSceneryMeshLibrary.Topology data,bool platform,float halfHeight){
  meshes++;var edges=new Dictionary<(int,int),(int count,int direction)>();var used=new HashSet<int>();double volume=0;int topFaces=0;
  var low=data.Vertices[0];var high=low;
  foreach(var v in data.Vertices){C(float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z),"finite physical vertices");low=Vector3.Min(low,v);high=Vector3.Max(high,v);}
  C(Math.Abs(low.x+.5f)<.000001&&Math.Abs(high.x-.5f)<.000001&&Math.Abs(low.z+.5f)<.000001&&Math.Abs(high.z-.5f)<.000001,"exact original horizontal footprint bounds");
  C(Math.Abs(low.y+halfHeight)<.000001&&Math.Abs(high.y-halfHeight)<.000001,"exact original vertical envelope");
  for(int t=0;t<data.Triangles.Length;t+=3){int a=data.Triangles[t],b=data.Triangles[t+1],c=data.Triangles[t+2];C(a>=0&&b>=0&&c>=0&&a<data.Vertices.Length&&b<data.Vertices.Length&&c<data.Vertices.Length,"triangle indices in range");used.Add(a);used.Add(b);used.Add(c);
   Vector3 va=data.Vertices[a],vb=data.Vertices[b],vc=data.Vertices[c];Vector3 normal=Cross(vb-va,vc-va);C(normal.sqrMagnitude>1e-14f,"no degenerate surface triangles");volume+=Dot(va,Cross(vb,vc))/6.0;
   if(Math.Abs(va.y-halfHeight)<.000001&&Math.Abs(vb.y-halfHeight)<.000001&&Math.Abs(vc.y-halfHeight)<.000001){topFaces++;C(normal.y>0,"support top faces point upward");}
   foreach(var edge in new[]{(a,b),(b,c),(c,a)}){var key=edge.Item1<edge.Item2?edge:(edge.Item2,edge.Item1);edges.TryGetValue(key,out var state);edges[key]=(state.count+1,state.direction+(edge.Item1<edge.Item2?1:-1));}
  }
  C(used.Count==data.Vertices.Length,"no unattached geometry vertices");foreach(var edge in edges.Values)C(edge.count==2&&edge.direction==0,"closed two-manifold with opposite edge winding");C(volume>.20,"positive enclosed volume and consistent outward winding");if(platform)C(topFaces>=24,"substantial perfectly flat original support plane");
 }
 static void Main(){
  foreach(var kind in new[]{NaturalSceneryMeshLibrary.Kind.Stone,NaturalSceneryMeshLibrary.Kind.Timber,NaturalSceneryMeshLibrary.Kind.Rock,NaturalSceneryMeshLibrary.Kind.Column,NaturalSceneryMeshLibrary.Kind.Log})
  foreach(var scale in new[]{new Vector3(.045f,.3f,1.2f),new Vector3(5,1.8f,.7f),new Vector3(.24f,3.2f,.27f),new Vector3(15,.5f,.5f),new Vector3(1,1,1),new Vector3(12,.8f,4.5f),new Vector3(3.8f,.048f,.35f)})
  foreach(int seed in new[]{0,171,65535})Validate(NaturalSceneryMeshLibrary.Build(kind,scale,seed),kind!=NaturalSceneryMeshLibrary.Kind.Rock,kind==NaturalSceneryMeshLibrary.Kind.Column||kind==NaturalSceneryMeshLibrary.Kind.Log?1:.5f);
  foreach(string name in new[]{"Room floor","NPC tunic","Merchant cap","Lit doorway","Fallen star floor emblem","Brook reed clump","Pitched workshop roof module","Camp tent","Loot beam","STAR CORE plinth"})C(NaturalSceneryMeshLibrary.Classify(name,PrimitiveType.Cube,new Vector3(1,1,1))==NaturalSceneryMeshLibrary.Kind.None,"exclude functional, actor and separate authored surfaces: "+name);
  C(NaturalSceneryMeshLibrary.Classify("Timber crossing plank",PrimitiveType.Cube,new Vector3(3.8f,.048f,.35f))==NaturalSceneryMeshLibrary.Kind.Timber,"actual thin bridge board receives closed timber geometry");
  C(NaturalSceneryMeshLibrary.Classify("Bridge damp bank contact",PrimitiveType.Cube,new Vector3(3.8f,.004f,.16f))==NaturalSceneryMeshLibrary.Kind.None,"other thin ground/contact surfaces remain excluded");
  var plank=NaturalSceneryMeshLibrary.Build(NaturalSceneryMeshLibrary.Kind.Timber,new Vector3(3.8f,.048f,.35f),171);float plankTop=-1;foreach(var v in plank.Vertices)plankTop=Math.Max(plankTop,.025f+v.y*.048f);C(Math.Abs(plankTop-.049f)<.000001,"actual bridge board support remains at .049m");
  C(NaturalSceneryMeshLibrary.Classify("Weathered courtyard wall",PrimitiveType.Cube,new Vector3(3,2,.8f))==NaturalSceneryMeshLibrary.Kind.Stone,"architectural wall covered");C(NaturalSceneryMeshLibrary.Classify("Old mine perimeter support",PrimitiveType.Cube,new Vector3(.24f,3,.27f))==NaturalSceneryMeshLibrary.Kind.Timber,"timber support covered");
  var go=new GameObject();go.Filter.sharedMesh=new Mesh{name="Emberfall shared Cube",bounds=new Bounds(new Vector3(.03f,.08f,-.02f),new Vector3(.8f,1.2f,.9f))};var resources=new WorldResources();NaturalSceneryMeshLibrary.Apply(go,"Weathered rock",PrimitiveType.Cube,Vector3.one,resources);C(resources.Owned.Count==0,"initial rock factory hook allocates nothing before its final authored assignment");
  go.Filter.sharedMesh.name="Emberfall weathered boulder";NaturalSceneryMeshLibrary.Apply(go,"Weathered rock",PrimitiveType.Cube,Vector3.one,resources);C(resources.Owned.Count==1,"final rock geometry registers exactly one owned mesh");C(Math.Abs(go.Filter.sharedMesh.bounds.center.y-.08f)<.000001&&Math.Abs(go.Filter.sharedMesh.bounds.size.y-1.2f)<.000001,"non-centred authored bounds preserved by actual Apply");NaturalSceneryMeshLibrary.Apply(go,"Weathered rock",PrimitiveType.Cube,Vector3.one,resources);C(resources.Owned.Count==1,"same rock cannot allocate a duplicate replacement");
  var scene=new WorldResources();Mesh shared=null;
  for(int i=0;i<200;i++){
   var wall=new GameObject();wall.Filter.sharedMesh=new Mesh{bounds=new Bounds(Vector3.zero,Vector3.one)};
   NaturalSceneryMeshLibrary.Apply(wall,"Weathered courtyard wall",PrimitiveType.Cube,new Vector3(3,2,.8f),scene);
   if(shared==null)shared=wall.Filter.sharedMesh;else C(ReferenceEquals(shared,wall.Filter.sharedMesh),"identical geometry shares a native mesh for instancing");
  }
  C(scene.Owned.Count==1,"200 identical walls own one mesh rather than 200 copies");
  foreach(int variant in new[]{0,1,2,3}){
   var wall=new GameObject();wall.Filter.sharedMesh=new Mesh{bounds=new Bounds(Vector3.zero,variant==2?new Vector3(1,2,1):Vector3.one)};
   var owner=variant==3?new WorldResources():scene;
   NaturalSceneryMeshLibrary.Apply(wall,variant==1?"Other courtyard wall":"Weathered courtyard wall",PrimitiveType.Cube,variant==0?new Vector3(4,2,.8f):new Vector3(3,2,.8f),owner);
   C(!ReferenceEquals(shared,wall.Filter.sharedMesh),"distinct scale, seed, envelope or scene must not reuse incorrect or foreign geometry");
  }
  Console.WriteLine("PASS: "+meshes+" production closed solids; "+checks+" manifold, volume, flat-top, original-envelope and factory-ownership checks (managed, not a native render claim).");
 }
}
'''
with tempfile.TemporaryDirectory(prefix='natural-scenery-') as temporary:
    folder=Path(temporary)
    (folder/'Production.cs').write_text((root/'Assets/Scripts/World/NaturalSceneryMeshLibrary.cs').read_text())
    (folder/'Unity.cs').write_text(stubs)
    (folder/'Test.cs').write_text(test)
    project=folder/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
    config=folder/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    env=dict(os.environ,DOTNET_CLI_HOME=str(folder/'cli'),DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_NOLOGO='1')
    build=subprocess.run([dotnet,'build',str(project),'--configfile',str(config),'-v:q'],env=env,text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
    if build.returncode:print(build.stdout);build.check_returncode()
    run=subprocess.run([dotnet,str(folder/'bin/Debug/net8.0/Test.dll')],env=env,text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
    print(run.stdout,end='');run.check_returncode()
    # A compiled open-face control must fail the actual manifold assertion.
    broken=(root/'Assets/Scripts/World/NaturalSceneryMeshLibrary.cs').read_text().replace('Triangles = triangles.ToArray()', 'Triangles = triangles.GetRange(0, triangles.Count - 3).ToArray()')
    (folder/'Production.cs').write_text(broken)
    negative_build=subprocess.run([dotnet,'build',str(project),'--configfile',str(config),'-v:q'],env=env,text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
    if negative_build.returncode:print(negative_build.stdout);negative_build.check_returncode()
    negative=subprocess.run([dotnet,str(folder/'bin/Debug/net8.0/Test.dll')],env=env,text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
    assert negative.returncode and 'closed two-manifold with opposite edge winding' in negative.stdout, negative.stdout
    print('PASS: compiled missing-face negative control fails the production closed-manifold check.')

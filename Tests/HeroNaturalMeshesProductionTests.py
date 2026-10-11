#!/usr/bin/env python3
"""Execute actual closed hero meshes and skin weights with managed mesh storage.

Unity skinning, renderer ownership, socket invariance and appearance need native QA.
"""
from pathlib import Path
import os, subprocess, sys, tempfile

root=Path(__file__).resolve().parents[1]
fixture=r'''
using System;using System.Collections.Generic;using System.Linq;
namespace Emberfall{public enum HeroClass{Vanguard,Arcanist,Ranger,Summoner}}
namespace UnityEngine{
 public class Object{public static void Destroy(Object obj){}}
 public enum RuntimeInitializeLoadType{SubsystemRegistration}
 public class RuntimeInitializeOnLoadMethodAttribute:Attribute{public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type){}}
 public struct Vector2{public float x,y;public Vector2(float a,float b){x=a;y=b;}}
 public struct Vector3{
  public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static Vector3 zero=>new Vector3();public static Vector3 up=>new Vector3(0,1,0);public static Vector3 forward=>new Vector3(0,0,1);public static Vector3 right=>new Vector3(1,0,0);public float sqrMagnitude=>x*x+y*y+z*z;public Vector3 normalized=>sqrMagnitude<1e-15f?zero:this/(float)Math.Sqrt(sqrMagnitude);
  public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator-(Vector3 a)=>zero-a;public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);public static Vector3 operator*(float b,Vector3 a)=>a*b;public static Vector3 operator/(Vector3 a,float b)=>a*(1/b);public static Vector3 Cross(Vector3 a,Vector3 b)=>new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
 }
 public struct Vector4{
  public float x,y,z,w;public Vector4(float a,float b,float c,float d){x=a;y=b;z=c;w=d;}public static Vector4 operator+(Vector4 a,Vector4 b)=>new Vector4(a.x+b.x,a.y+b.y,a.z+b.z,a.w+b.w);public static Vector4 operator-(Vector4 a,Vector4 b)=>new Vector4(a.x-b.x,a.y-b.y,a.z-b.z,a.w-b.w);public static Vector4 operator-(Vector4 a)=>new Vector4(-a.x,-a.y,-a.z,-a.w);public static Vector4 operator*(Vector4 a,float b)=>new Vector4(a.x*b,a.y*b,a.z*b,a.w*b);public static Vector4 operator*(float b,Vector4 a)=>a*b;
 }
 public struct BoneWeight{public int boneIndex0,boneIndex1;public float weight0,weight1;}
 public struct Matrix4x4{public Vector3 shift;public static Matrix4x4 identity=>new Matrix4x4();public static Matrix4x4 Translate(Vector3 p)=>new Matrix4x4{shift=p};public Vector3 MultiplyPoint3x4(Vector3 p)=>p+shift;}
 public struct CombineInstance{public Mesh mesh;public Matrix4x4 transform;}
 public class Mesh:Object{
  public string name;public Vector3[] vertices,normals;public Vector2[] uv;public int[] triangles;public BoneWeight[] boneWeights;public Matrix4x4[] bindposes;public int vertexCount=>vertices.Length;
  public void RecalculateNormals(){normals=new Vector3[vertices.Length];for(int i=0;i<triangles.Length;i+=3){int a=triangles[i],b=triangles[i+1],c=triangles[i+2];var n=Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]);normals[a]+=n;normals[b]+=n;normals[c]+=n;}for(int i=0;i<normals.Length;i++)normals[i]=normals[i].normalized;}
  public void RecalculateTangents(){}public void RecalculateBounds(){}
  public void CombineMeshes(CombineInstance[] parts,bool merge,bool matrices){var v=new List<Vector3>();var n=new List<Vector3>();var u=new List<Vector2>();var t=new List<int>();foreach(var part in parts){int at=v.Count;v.AddRange(part.mesh.vertices.Select(p=>matrices?part.transform.MultiplyPoint3x4(p):p));n.AddRange(part.mesh.normals);u.AddRange(part.mesh.uv);t.AddRange(part.mesh.triangles.Select(i=>i+at));}vertices=v.ToArray();normals=n.ToArray();uv=u.ToArray();triangles=t.ToArray();}
 }
 public static class Mathf{
  public const float PI=(float)Math.PI;public static float Min(float a,float b)=>Math.Min(a,b);public static int Min(int a,int b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int Max(int a,int b)=>Math.Max(a,b);public static float Acos(float a)=>(float)Math.Acos(a);public static float Sin(float a)=>(float)Math.Sin(a);public static float Cos(float a)=>(float)Math.Cos(a);public static float Pow(float a,float b)=>(float)Math.Pow(a,b);public static float Exp(float a)=>(float)Math.Exp(a);public static float Abs(float a)=>Math.Abs(a);public static float Clamp01(float a)=>Math.Min(1,Math.Max(0,a));public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp01(t);public static float InverseLerp(float a,float b,float t)=>Clamp01((t-a)/(b-a));public static float SmoothStep(float a,float b,float t){t=Clamp01(t);return a+(b-a)*t*t*(3-2*t);}
 }
}
'''
driver=r'''
using System;using System.Collections.Generic;using System.Linq;using Emberfall;using UnityEngine;
static class NaturalMeshChecks{
 static int checks;static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
 static string VertexKey(Vector3 p)=>Math.Round(p.x*100000)+","+Math.Round(p.y*100000)+","+Math.Round(p.z*100000);
 static void Surface(Mesh mesh){
  Check(mesh.vertexCount>300&&mesh.triangles.Length>900,"curved body has substantial actual mesh detail");
  Check(mesh.vertices.All(v=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z)),"finite mesh vertices");
  double volume=0;var ids=new Dictionary<string,int>();var verts=new int[mesh.vertexCount];for(int i=0;i<verts.Length;i++){string key=VertexKey(mesh.vertices[i]);if(!ids.TryGetValue(key,out int id)){id=ids.Count;ids.Add(key,id);}verts[i]=id;}
  var edges=new Dictionary<(int,int),int>();
  for(int i=0;i<mesh.triangles.Length;i+=3){int a=mesh.triangles[i],b=mesh.triangles[i+1],c=mesh.triangles[i+2];Check(a>=0&&b>=0&&c>=0&&a<verts.Length&&b<verts.Length&&c<verts.Length,"valid triangle index");volume+=Vector3.Dot(mesh.vertices[a],Vector3.Cross(mesh.vertices[b],mesh.vertices[c]))/6.0;foreach(var pair in new[]{(verts[a],verts[b]),(verts[b],verts[c]),(verts[c],verts[a])}){var edge=pair.Item1<pair.Item2?pair:(pair.Item2,pair.Item1);edges.TryGetValue(edge,out int count);edges[edge]=count+1;}}
  Check(volume>.00001,"outward closed mesh / "+mesh.name);
  Check(edges.Values.All(n=>n==2),"closed manifold after welding UV seams / "+mesh.name);
 }
 static void Main(){
  foreach(string kind in new[]{"face","shoulder","bracer","boot","sole","plate","knee","panel","strap","hat","skirt","helmet","hair-lock","nose","neck","gorget"}){var mesh=HeroNaturalMeshLibrary.Get(kind);Check(ReferenceEquals(mesh,HeroNaturalMeshLibrary.Get(kind)),"shape cache reuses mesh");Surface(mesh);}
  foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass))){var mesh=HeroNaturalMeshLibrary.Torso(hero);Surface(mesh);Check(mesh.vertices.Min(v=>v.y)>.75f&&mesh.vertices.Max(v=>v.y)<1.78f,"torso stays in accepted construction envelope");Check(mesh.boneWeights.All(w=>w.boneIndex0==0&&w.boneIndex1==1&&Math.Abs(w.weight0+w.weight1-1)<.00001),"torso weights normalize on two actual bones");Check(mesh.boneWeights.Any(w=>w.weight0>.98f)&&mesh.boneWeights.Any(w=>w.weight1>.98f)&&mesh.boneWeights.Any(w=>w.weight0>.2f&&w.weight1>.2f),"torso spans pelvis and spine with a continuous blend");}
  foreach(bool guardian in new[]{false,true}){var mesh=HeroNaturalMeshLibrary.EnemyTorso(guardian);Surface(mesh);Check(ReferenceEquals(mesh,HeroNaturalMeshLibrary.EnemyTorso(guardian)),"enemy torso cache reuses mesh");Check(mesh.boneWeights.All(w=>w.boneIndex0==0&&w.boneIndex1==1&&Math.Abs(w.weight0+w.weight1-1)<.00001),"enemy torso skin weights normalize");Check(mesh.boneWeights.Any(w=>w.weight0>.98f)&&mesh.boneWeights.Any(w=>w.weight1>.98f)&&mesh.boneWeights.Any(w=>w.weight0>.2f&&w.weight1>.2f),"enemy torso bends from pelvis to chest");Check(mesh.vertices.Max(v=>Math.Abs(v.x))>(guardian?.58f:.40f),"enemy shoulder contour meets natural sleeves");}
  foreach(bool leg in new[]{false,true}){var mesh=HeroNaturalMeshLibrary.Limb(leg);Surface(mesh);Check(mesh.boneWeights.All(w=>Math.Abs(w.weight0+w.weight1-1)<.00001),"limb weights normalize");Check(mesh.boneWeights.Any(w=>w.weight0==1)&&mesh.boneWeights.Any(w=>w.weight1==1)&&mesh.boneWeights.Any(w=>w.weight0>.2f&&w.weight1>.2f),"limb bends through real joint blend");Check(mesh.bindposes.Length==2&&Math.Abs(mesh.bindposes[1].shift.y-(leg?.37f:.30f))<.0001,"limb keeps actual joint bind offset");}
  Surface(HeroNaturalMeshLibrary.Hand());var hair=HeroNaturalMeshLibrary.Hair();Surface(hair);
  var connections=new Dictionary<string,HashSet<string>>();for(int i=0;i<hair.triangles.Length;i+=3){var keys=new[]{VertexKey(hair.vertices[hair.triangles[i]]),VertexKey(hair.vertices[hair.triangles[i+1]]),VertexKey(hair.vertices[hair.triangles[i+2]])};foreach(var key in keys){if(!connections.TryGetValue(key,out var adjacent))connections[key]=adjacent=new HashSet<string>();foreach(var other in keys)adjacent.Add(other);}}
  var visited=new HashSet<string>();var pending=new Stack<string>();pending.Push(connections.Keys.First());while(pending.Count>0){var key=pending.Pop();if(!visited.Add(key))continue;foreach(var next in connections[key])pending.Push(next);}Check(visited.Count==connections.Count,"hair is one continuous connected sculpt, not independent fringe tubes");
  var frontLine=hair.vertices.Where(v=>v.z>.40f&&v.y<.30f).ToArray();Check(frontLine.Length>8&&frontLine.Max(v=>v.y)-frontLine.Min(v=>v.y)>.05f,"hairline sweeps naturally across forehead");
  Console.WriteLine("PASS "+checks+" actual natural hero mesh topology, volume and bone-weight checks; managed mesh storage, not Unity rendering.");
 }
}
'''
dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
with tempfile.TemporaryDirectory(prefix='emberfall-natural-hero-meshes-') as directory:
    work=Path(directory)
    production=work/'HeroNaturalMeshLibrary.cs';original=(root/'Assets/Scripts/Combat/HeroNaturalMeshLibrary.cs').read_text();production.write_text(original)
    (work/'Fixture.cs').write_text(fixture);(work/'Program.cs').write_text(driver)
    project=work/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
    config=work/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    env=dict(os.environ,DOTNET_CLI_HOME=str(work/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
    subprocess.run([dotnet,'restore',str(project),'--configfile',str(config),'-v:q'],env=env,check=True)
    command=[dotnet,'run','--project',str(project),'--no-restore','-c','Release']
    subprocess.run(command,env=env,check=True)
    for name,before,after,expected in [
        ('inside-facing-body','bool ascending=rings[rings.Length-1].x>rings[0].x;','bool ascending=false;','outward closed mesh'),
        ('rigid-pelvis-body','weight0=1-upper,weight1=upper','weight0=1,weight1=0','torso spans pelvis and spine'),
        ('segmented-hair','mesh=Finish("Continuous sculpted side-swept hair shell",vertices,uv,indices,rows,width,sides);','mesh=Hand();','hair is one continuous connected sculpt'),
    ]:
        assert original.count(before)>=1
        production.write_text(original.replace(before,after,1));result=subprocess.run(command,env=env,capture_output=True,text=True)
        if result.returncode==0 or expected not in result.stdout+result.stderr:raise AssertionError(name+' mutation was not rejected\n'+result.stdout+result.stderr)
        print('PASS: '+name+' production mutation rejected',flush=True)

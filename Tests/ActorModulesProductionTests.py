#!/usr/bin/env python3
"""Actual decoder and selector with minimal engine/resource API doubles, not Unity acceptance."""
import os,sys,tempfile,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1]
with tempfile.TemporaryDirectory(prefix='actor-modules-') as d:
 p=Path(d)
 (p/'Loader.cs').write_text((root/'Assets/Scripts/Combat/AuthoredActorMeshes.cs').read_text())
 (p/'Test.cs').write_text(r'''
using System;using System.IO;using System.Linq;using Emberfall;using UnityEngine;
namespace UnityEngine {
 public class Object { public static void Destroy(Object o){} }
 public class RuntimeInitializeOnLoadMethodAttribute:Attribute{public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t){}}
 public enum RuntimeInitializeLoadType{SubsystemRegistration} public enum PrimitiveType{Cube,Sphere,Capsule,Cylinder}
 public struct Vector3{public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}}
 public struct Vector2{public float x,y;public Vector2(float a,float b){x=a;y=b;}}
 public class Mesh:Object{public string name;public Vector3[] vertices,normals;public Vector2[] uv;public int[] triangles;public void RecalculateBounds(){}}
 public class MeshFilter{public Mesh sharedMesh;}public class GameObject{public MeshFilter filter=new MeshFilter();public T GetComponent<T>() where T:class{return filter as T;}}
 public class TextAsset{public byte[] bytes;}public static class Resources {public static string Root;public static T Load<T>(string n) where T:class {var p=Path.Combine(Root,n+".bytes");return File.Exists(p)?new TextAsset{bytes=File.ReadAllBytes(p)} as T:null;}}
}
class Program {
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Reject(byte[] bytes,string name){Check(AuthoredActorMeshes.Decode(bytes,name)==null,"accepted "+name);}
 static byte[] Change(byte[] b,int at,byte[] value){var c=(byte[])b.Clone();Array.Copy(value,0,c,at,value.Length);return c;}
 static void Main(string[] args){Resources.Root=args[0];var files=Directory.GetFiles(Path.Combine(args[0],"ActorModules"),"*.bytes");Check(files.Length==24,"finite authored module inventory");foreach(var f in files){var b=File.ReadAllBytes(f);var m=AuthoredActorMeshes.Decode(b,f);Check(m!=null&&m.triangles.Length%3==0,"valid asset "+f);Check(m.vertices.All(v=>Math.Abs(v.x)<=.50001f&&Math.Abs(v.z)<=.50001f&&Math.Abs(v.y)<=1.00001f),"preserved local envelope");Reject(b.Take(b.Length-1).ToArray(),"truncated");Reject(b.Concat(new byte[]{0}).ToArray(),"trailing");Reject(Change(b,0,BitConverter.GetBytes(0)),"magic");Reject(Change(b,4,BitConverter.GetBytes(int.MaxValue)),"allocation bomb");Reject(Change(b,8,BitConverter.GetBytes(4)),"incomplete triangle");Reject(Change(b,12,BitConverter.GetBytes(float.NaN)),"nan");Reject(Change(b,12,BitConverter.GetBytes(float.PositiveInfinity)),"inf");Reject(Change(b,12,BitConverter.GetBytes(17f)),"position envelope");Reject(Change(b,24,BitConverter.GetBytes(12f)),"invalid normal");Reject(Change(b,b.Length-4,BitConverter.GetBytes(-1)),"negative index");Reject(Change(b,b.Length-4,BitConverter.GetBytes(m.vertices.Length)),"index overflow");}
 Reject(null,"null");Reject(new byte[4],"short");
 Check(AuthoredActorMeshes.Key("Weathered rock",PrimitiveType.Cube)==null,"scenery excluded");Check(AuthoredActorMeshes.Key("Helmet",PrimitiveType.Cube)==null,"wrong shape excluded");
 var source=File.ReadAllText(args[1]);var names=new[]{"Breastplate","Pauldrons","Helmet","Star spirit","Spirit wolf torso","Wolf head","Paw","Slime Body","Spirit Core","Great Hammer","Crossguard","Swept guard","Layered bow limb","Focus crystal"};foreach(var n in names)Check(source.Contains("\""+n+"\""),"production name "+n);
 var go=new GameObject();var fallback=new Mesh();go.filter.sharedMesh=fallback;AuthoredActorMeshes.Apply(go,"unmapped",PrimitiveType.Cube);Check(go.filter.sharedMesh==fallback,"original fallback");AuthoredActorMeshes.Apply(go,"Helmet",PrimitiveType.Sphere);Check(go.filter.sharedMesh!=fallback,"runtime application");var cached=go.filter.sharedMesh;AuthoredActorMeshes.Apply(go,"Helmet",PrimitiveType.Sphere);Check(go.filter.sharedMesh==cached,"shared cache");AuthoredActorMeshes.Enabled=false;go.filter.sharedMesh=fallback;AuthoredActorMeshes.Apply(go,"Helmet",PrimitiveType.Sphere);Check(go.filter.sharedMesh==fallback,"explicit rollback");Console.WriteLine("PASS actual decoder 24 assets malformed/finite/index/budget guards; exact selector; cache and original fallback. Managed, not Unity.");}}
''')
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
 subprocess.run([dotnet,'run','--project',str(p/'Test.csproj'),'--',str(root/'Assets/Resources'),str(root/'Assets/Scripts/Combat/CombatModel.cs')],env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'),check=True)

#!/usr/bin/env python3
"""Run the production shell builder with managed mesh storage.

Checks finite closed solids and envelope retention. Unity materials, renderer
visibility and native screenshots remain separate validation.
"""
from pathlib import Path
import ast
import os
import subprocess
import sys
import tempfile

root = Path(__file__).resolve().parents[1]
# Reuse the established managed mesh/math fixture; no production method is
# reimplemented here. Only mesh bounds storage is added for envelope checks.
tree = ast.parse((root / 'Tests/HeroNaturalMeshesProductionTests.py').read_text())
fixture = next(ast.literal_eval(node.value) for node in tree.body
               if isinstance(node, ast.Assign) and any(isinstance(t, ast.Name) and t.id == 'fixture' for t in node.targets))
fixture = fixture.replace('public static Vector3 zero=>', 'public static Vector3 one=>new Vector3(1,1,1);public static Vector3 zero=>')
fixture = fixture.replace('public static Matrix4x4 Translate(Vector3 p)=>', 'public static Matrix4x4 TRS(Vector3 p,Quaternion q,Vector3 s)=>new Matrix4x4{shift=p};public static Matrix4x4 Translate(Vector3 p)=>')
fixture = fixture.replace('public void RecalculateTangents(){}public void RecalculateBounds(){}', '''
  public Bounds bounds;
  public void RecalculateTangents(){}
  public void RecalculateBounds(){
   if(vertices.Length==0){bounds=new Bounds();return;}
   Vector3 low=vertices[0],high=vertices[0];
   foreach(Vector3 p in vertices){low=new Vector3(Math.Min(low.x,p.x),Math.Min(low.y,p.y),Math.Min(low.z,p.z));high=new Vector3(Math.Max(high.x,p.x),Math.Max(high.y,p.y),Math.Max(high.z,p.z));}
   bounds=new Bounds((low+high)*.5f,high-low);
  }''')
fixture += r'''
namespace UnityEngine{
 public static class Application{public static bool isMobilePlatform;}
 public struct Vector3Int{public int x,y,z;public Vector3Int(int a,int b,int c){x=a;y=b;z=c;}}
 public struct Bounds{public Vector3 center,size;public Bounds(Vector3 c,Vector3 s){center=c;size=s;}}
 public struct Quaternion{public static Quaternion FromToRotation(Vector3 a,Vector3 b)=>new Quaternion();public static Quaternion Euler(float x,float y,float z)=>new Quaternion();public static Vector3 operator*(Quaternion q,Vector3 v)=>v;}
}
'''
fixture = fixture.replace('public const float PI=', 'public static int RoundToInt(float x)=>(int)Math.Round(x);public const float PI=')
driver = r'''
using System;using System.Collections.Generic;using System.Linq;using Emberfall;using UnityEngine;
static class ShellChecks{
 static int checks;static void Check(bool ok,string text){checks++;if(!ok)throw new Exception(text);}
 static bool Finite(Vector3 p)=>float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z);
 static bool Close(Vector3 a,Vector3 b)=>(a-b).sqrMagnitude<.00000001f;
 static string Key(Vector3 p)=>Math.Round(p.x*100000)+","+Math.Round(p.y*100000)+","+Math.Round(p.z*100000);
 static void Solid(Mesh mesh){
  Check(mesh.vertices.All(Finite),"finite shell vertices including both endpoint rims");
  Check(Finite(mesh.bounds.center)&&Finite(mesh.bounds.size),"finite native-style shell bounds");
  Check(mesh.bounds.size.x>.1f&&mesh.bounds.size.y>.1f&&mesh.bounds.size.z>.04f,"all three dimensions retain physical shell volume");
  Check(mesh.normals.All(Finite),"finite shell normals");
  var ids=new Dictionary<string,int>();var welded=new int[mesh.vertexCount];
  for(int i=0;i<welded.Length;i++){string key=Key(mesh.vertices[i]);if(!ids.TryGetValue(key,out int id)){id=ids.Count;ids.Add(key,id);}welded[i]=id;}
  var edges=new Dictionary<(int,int),int>();double volume=0;
  for(int i=0;i<mesh.triangles.Length;i+=3){
   int a=mesh.triangles[i],b=mesh.triangles[i+1],c=mesh.triangles[i+2];
   Check(a>=0&&b>=0&&c>=0&&a<mesh.vertexCount&&b<mesh.vertexCount&&c<mesh.vertexCount,"valid shell triangle index");
   Check(Vector3.Cross(mesh.vertices[b]-mesh.vertices[a],mesh.vertices[c]-mesh.vertices[a]).sqrMagnitude>1e-12f,"shell triangle keeps nonzero physical area");
   volume+=Vector3.Dot(mesh.vertices[a],Vector3.Cross(mesh.vertices[b],mesh.vertices[c]))/6.0;
   foreach(var pair in new[]{(welded[a],welded[b]),(welded[b],welded[c]),(welded[c],welded[a])}){var key=pair.Item1<pair.Item2?pair:(pair.Item2,pair.Item1);edges.TryGetValue(key,out int n);edges[key]=n+1;}
  }
  Check(volume>.00001,"positive closed shell volume");
  Check(edges.Values.All(n=>n==2),"closed shell manifold including solid rim");
 }
 static void Main(){
  Check(Mathf.Sin(Mathf.PI)<0,"fixture retains actual single precision negative PI sine");
  Check(float.IsNaN(Mathf.Pow(Mathf.Sin(Mathf.PI),.45f)),"fixture reproduces original fractional power NaN");
  foreach(bool mobile in new[]{false,true}) foreach(bool drape in new[]{false,true}){
   Application.isMobilePlatform=mobile;
   var shell=NaturalActorMeshFactory.FrontShell("production shell",drape);Solid(shell);
   foreach(var envelope in new[]{new Bounds(Vector3.zero,new Vector3(1,1,1)),new Bounds(new Vector3(.07f,-.15f,.09f),new Vector3(.98f,2,.42f))}){
    shell=NaturalActorMeshFactory.FrontShell("fitted production shell",drape);NaturalActorMeshFactory.FitEnvelope(shell,envelope);Solid(shell);
    Check(Close(shell.bounds.center,envelope.center),"fitted shell preserves accepted local center");
    Check(Close(shell.bounds.size,envelope.size),"fitted shell preserves accepted local envelope dimensions");
   }
  }
  Console.WriteLine("PASS "+checks+" actual production shell endpoint, solid topology and envelope checks; managed storage, not native Unity rendering.");
 }
}
'''
dotnet = sys.argv[1] if len(sys.argv) > 1 else os.environ.get('DOTNET', 'dotnet')
with tempfile.TemporaryDirectory(prefix='emberfall-natural-actor-shells-') as directory:
    work = Path(directory)
    original = (root / 'Assets/Scripts/Combat/NaturalActorMeshFactory.cs').read_text()
    production = work / 'NaturalActorMeshFactory.cs'
    production.write_text(original)
    (work / 'Fixture.cs').write_text(fixture)
    (work / 'Program.cs').write_text(driver)
    project = work / 'Test.csproj'
    project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
    config = work / 'NuGet.Config'
    config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    env = dict(os.environ, DOTNET_CLI_HOME=str(work / 'cli'), DOTNET_NOLOGO='1', DOTNET_CLI_TELEMETRY_OPTOUT='1')
    subprocess.run([dotnet, 'restore', str(project), '--configfile', str(config), '-v:q'], env=env, check=True)
    command = [dotnet, 'run', '--project', str(project), '--no-restore', '-c', 'Release']
    subprocess.run(command, env=env, check=True)
    before = 'float arch = Mathf.Max(0, Mathf.Sin(t * Mathf.PI));'
    assert original.count(before) == 1
    production.write_text(original.replace(before, 'float arch = Mathf.Sin(t * Mathf.PI);'))
    result = subprocess.run(command, env=env, capture_output=True, text=True)
    if result.returncode == 0 or 'finite shell vertices including both endpoint rims' not in result.stdout + result.stderr:
        raise AssertionError('original negative endpoint production mutation was not rejected\n' + result.stdout + result.stderr)
    print('PASS original negative-PI endpoint mutation rejected before bounds fitting', flush=True)

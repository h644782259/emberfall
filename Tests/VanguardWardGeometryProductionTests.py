#!/usr/bin/env python3
"""Execute the actual four-face ward mesh; GPU/material acceptance is native."""
from pathlib import Path
import os, subprocess, sys, tempfile

root = Path(os.environ.get('EMBERFALL_TEST_ROOT', Path(__file__).resolve().parents[1]))
source = (root / 'Assets/Scripts/Combat/VanguardWardVisual.cs').read_text()
start = source.index('        private static Mesh PlateMesh(')
cursor = source.index('{', start) + 1
depth = 1
while depth:
    depth += (source[cursor] == '{') - (source[cursor] == '}')
    cursor += 1
method = source[start:cursor].replace('private static Mesh PlateMesh', 'public static Mesh PlateMesh')
fixture = r'''
using System;using System.Collections.Generic;using UnityEngine;
namespace UnityEngine {
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public float magnitude=>(float)Math.Sqrt(x*x+y*y+z*z);public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 Cross(Vector3 a,Vector3 b)=>new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;}
 public static class Mathf {public const float PI=(float)Math.PI;public static float Sin(float x)=>(float)Math.Sin(x);public static float Cos(float x)=>(float)Math.Cos(x);}
 public class Mesh {public string name;public Vector3[] vertices;public Vector2[] uv;public int[] triangles;public void SetVertices(List<Vector3> v){vertices=v.ToArray();}public void SetUVs(int channel,List<Vector2> v){uv=v.ToArray();}public void SetTriangles(List<int> v,int sub){triangles=v.ToArray();}public void RecalculateNormals(){}public void RecalculateBounds(){}}
}
static class Production {METHOD}
static class Program {
 static int checks;static void C(bool v,string msg){checks++;if(!v)throw new Exception(msg);}
 static void Main(){foreach(bool border in new[]{false,true}){
  var mesh=Production.PlateMesh(border);var v=mesh.vertices;var t=mesh.triangles;var edges=new Dictionary<(int,int),(int,int)>();var volumes=new double[4];
  C(v.Length==952&&mesh.uv.Length==952,"dense smooth four-face skins preserve authored UVs");
  for(int i=0;i<t.Length;i+=3){int a=t[i],b=t[i+1],c=t[i+2];C(a>=0&&b>=0&&c>=0&&a<v.Length&&b<v.Length&&c<v.Length,"valid production indices");
   C((Vector3.Cross(v[b]-v[a],v[c]-v[a])).magnitude>.000001f,"no collapsed ward triangle");
   C(a/238==b/238&&b/238==c/238,"four physically distinct curved shield panels");volumes[a/238]+=Vector3.Dot(v[a],Vector3.Cross(v[b],v[c]))/6d;
   foreach(var e in new[]{(a,b),(b,c),(c,a)}){var k=(Math.Min(e.Item1,e.Item2),Math.Max(e.Item1,e.Item2));var n=edges.TryGetValue(k,out var old)?old:(0,0);edges[k]=(n.Item1+1,n.Item2+(e.Item1<e.Item2?1:-1));}
  }
  foreach(var e in edges.Values)C(e.Item1==2&&e.Item2==0,"every used ward edge is capped with opposite directed winding");
  foreach(var volume in volumes)C(volume>.001,"each closed plate or border encloses positive outward-wound volume");
  for(int panel=0;panel<4;panel++){int p=panel*238;var center=v[p+3*17+8];float angle=(float)Math.Atan2(center.x,center.z);float expected=panel*Mathf.PI*.5f;C(Math.Abs(Math.Sin(angle-expected))<.000001,"cardinal views each have a real broad shield face");
   for(int j=0;j<119;j++){var a=v[p+j];var b=v[p+119+j];C(float.IsFinite(a.x)&&float.IsFinite(a.y)&&float.IsFinite(a.z),"finite bowed shell");C(Math.Abs(a.y-b.y)<.00001&&Math.Abs((double)Math.Sqrt(a.x*a.x+a.z*a.z)-(double)Math.Sqrt(b.x*b.x+b.z*b.z)-.07)<.00001,"two physical skins retain forge thickness");}}
  Console.WriteLine("PASS "+(border?"raised closed border":"closed steel shell")+" triangles="+t.Length/3);
 }Console.WriteLine("PASS "+checks+" actual ward topology/thickness/cardinal-face assertions; managed, not native rendered pixels.");}
}
'''.replace('METHOD', method)
with tempfile.TemporaryDirectory(prefix='emberfall-ward-geometry-') as directory:
    work=Path(directory);(work/'Program.cs').write_text(fixture)
    project=work/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
    config=work/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
    env=dict(os.environ,DOTNET_CLI_HOME=str(work/'cli'),DOTNET_NOLOGO='1')
    subprocess.run([dotnet,'restore',str(project),'--configfile',str(config)],env=env,check=True,stdout=subprocess.DEVNULL)
    command=[dotnet,'run','--project',str(project),'--no-restore','-c','Release'];subprocess.run(command,env=env,check=True)
    for name,before,after,expected in [
        ('uncapped-frame-opening','if (border)\n                {','if (false)\n                {','every used ward edge is capped'),
        ('inward-skins','triangles[i + 1] = triangles[i + 2]; triangles[i + 2] = b;','triangles[i + 1] = b;','positive outward-wound volume'),
    ]:
        if before not in fixture:raise AssertionError('mutation target missing '+name)
        (work/'Program.cs').write_text(fixture.replace(before,after,1))
        result=subprocess.run(command,env=env,text=True,capture_output=True)
        if result.returncode==0 or expected not in result.stdout+result.stderr:raise AssertionError('mutation not rejected '+name+'\n'+result.stdout+result.stderr)
        print('PASS '+name+' negative control rejected',flush=True)

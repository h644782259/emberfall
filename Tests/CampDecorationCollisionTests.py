"""Run the real four-facility builder with render doubles and production actor navigation."""
from pathlib import Path
import importlib.util,tempfile,subprocess,os,sys
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
source=(root/'Assets/Scripts/World/WorldBuilder.cs').read_text(encoding='utf-8')
a=source.index('private static void BuildCampFacilities(');b=source.index('{',a)+1;depth=1
while depth:
    depth+=(source[b]=='{')-(source[b]=='}');b+=1
program=r'''
using System;using System.Collections.Generic;using UnityEngine;using Emberfall;
namespace UnityEngine{
public class Transform{}public class GameObject{}public class Material{}public enum PrimitiveType{Cylinder}
public struct Color{public Color(float r,float g,float b){}public static Color operator*(Color c,float f)=>c;}}
namespace Emberfall{
public class WorldResources{public Material Material(Color c,bool glow=false)=>new Material();}
public static class AuthoredFixedScenery{public static void Crest(Transform p,string n,Vector3 at,Material m){}}
class Probe{
static List<Vector3> centers=new List<Vector3>();
static Transform Region(Transform p,string name)=>p;
static GameObject Primitive(Transform p,string name,PrimitiveType type,Vector3 at,Vector3 size,Material material)=>new GameObject();
static void Crystal(Transform p,WorldResources r,Vector3 at,float size,Material m){}
static void Label(Transform p,string name,string label,Vector3 at,float size,Color c,bool world){}
static Vector3 NavigationScenePoint(Transform p,Vector3 at)=>at;static float NavigationSceneRadius(Transform p,float r)=>r;
static void RegisterSceneryCircle(Vector3 p,float r,GameObject body){centers.Add(p);WorldTraversal.AddDynamicCircle(p,r);}
BUILD
static void Check(bool value,string message){if(!value)throw new Exception(message);}
static void Main(){int checks=0;
foreach(int progress in new[]{0,1,5,100}){
WorldTraversal.Reset(ZoneKind.Wilderness);centers.Clear();BuildCampFacilities(new Transform(),new WorldResources(),progress);
Check(centers.Count==4,"all four visible decorations register collision");
foreach(var center in centers)foreach(float radius in new[]{.45f,.6f,1.25f}){
Check(!WorldTraversal.IsWalkable(center,radius),"enemy cannot stand inside decoration");
for(int side=0;side<4;side++){
var direction=new Vector3(Mathf.Cos(side*Mathf.PI*.5f),0,Mathf.Sin(side*Mathf.PI*.5f));var start=center+direction*2.6f;
var result=WorldTraversal.Move(start,direction*-5.2f,radius);
Check(Vector3.Distance(center,result)>=.85f+radius-.001f,"enemy movement cannot cross solid decoration");
Check(Vector3.Dot(result-center,direction)>0,"enemy cannot tunnel through decoration");checks+=2;
}
}Check(WorldTraversal.IsWalkable(new Vector3(0,0,-10)),"central arrival path stays open");
}Console.WriteLine("PASS "+checks+" camp decoration movement and collision checks");}}
}
'''.replace('BUILD',source[a:b])
with tempfile.TemporaryDirectory(prefix='camp-collision-') as tmp:
    p=Path(tmp);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
    project=cv.write_project(p/'project',[root/'Assets/Scripts/World/WorldTraversal.cs',root/'Tests/DestructibleTraversalTests.cs'],program)
    subprocess.run([sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(project)],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))

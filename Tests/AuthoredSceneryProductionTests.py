#!/usr/bin/env python3
"""Execute production tree/roof construction against managed transforms and mesh doubles."""
from pathlib import Path
exec((Path(__file__).with_name('EquipmentCompositionProductionTests.py')).read_text().split('with tempfile.TemporaryDirectory')[0])
with tempfile.TemporaryDirectory(prefix='authored-scenery-') as directory:
 p=Path(directory)
 fixture=(root/'Tests/EquipmentCompositionProductionTests.Fixture.cs').read_text().split('public static class EquipmentCompositionProductionTests')[0]
 fixture=fixture.replace('public static Quaternion Euler(float x,float y,float z)=>','public static Quaternion FromToRotation(Vector3 a,Vector3 b){var axis=Vector3.Cross(a,b);return new Quaternion{q=System.Numerics.Quaternion.Normalize(new System.Numerics.Quaternion(axis.x,axis.y,axis.z,1+a.x*b.x+a.y*b.y+a.z*b.z))};}public static Quaternion Euler(float x,float y,float z)=>')
 # Additional engine API surface used by the current production Tree wrapper.
 # Shader/tangent behavior is outside this managed fallback-construction suite.
 fixture=fixture.replace('public Transform Find(string n)=>', 'public int childCount=>children.Count;public Transform GetChild(int index)=>children[index];public Vector3 lossyScale=>new Vector3((TransformPoint(Vector3.right)-position).magnitude,(TransformPoint(Vector3.up)-position).magnitude,(TransformPoint(Vector3.forward)-position).magnitude);public Transform Find(string n)=>')
 fixture=fixture.replace('public class Mesh:Object {', 'public struct Bounds {public Vector3 center,size;public Bounds(Vector3 c,Vector3 s){center=c;size=s;}}public class Mesh:Object {')
 fixture=fixture.replace('public void RecalculateNormals(){}', 'public void RecalculateTangents(){}public void RecalculateNormals(){}')
 fixture=fixture.replace('public static float Abs(float x)=>', 'public static float Max(float x,float y)=>Math.Max(x,y);public static float Abs(float x)=>')
 (p/'Fixture.cs').write_text(fixture);(p/'Types.cs').write_text(data)
 world=(root/'Assets/Scripts/World/WorldBuilder.cs').read_text()
 (p/'Hook.cs').write_text('using UnityEngine;namespace Emberfall {public static partial class WorldBuilder {'+''.join(extract(world,signature) for signature in ['private static Vector3 NavigationScenePoint(', 'private static float NavigationSceneRadius(', 'private static void Tree('])+'}}')
 for f in ['Assets/Scripts/World/WorldBuilder.AuthoredScenery.cs','Assets/Scripts/Combat/ProceduralVisuals.cs','Assets/Scripts/Combat/VisualMeshRecipes.cs','Tests/AuthoredSceneryProductionTests.cs']:(p/Path(f).name).write_text((root/f).read_text())
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');cmd=[dotnet,'run','--project',str(project),'--no-restore'];subprocess.run([dotnet,'restore',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,check=True);subprocess.run(cmd,env=env,check=True)
 env.pop('SCENERY_EXPORT',None)
 source=p/'WorldBuilder.AuthoredScenery.cs';original=source.read_text();source.write_text(original.replace('int variant=((seed%3)+3)%3;','int variant=0;'))
 subprocess.run([dotnet,'build',str(project),'--no-restore','-v:q'],env=env,check=True,stdout=subprocess.DEVNULL);result=subprocess.run(cmd+['--no-build'],env=env,capture_output=True,text=True);assert result.returncode and 'System.Exception: three authored canopy variants differ geometrically' in result.stdout+result.stderr,result.stdout+result.stderr
 print('PASS: collapsed tree variant control compiled and rejected')

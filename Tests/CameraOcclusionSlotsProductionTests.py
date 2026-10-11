#!/usr/bin/env python3
"""Real occlusion/group production with copied material-array API boundaries, not Unity rendering."""
from pathlib import Path
import json,textwrap
source=Path(__file__).with_name('TreeOcclusionRegistryTests.py').read_text()
exec(source.split('\nwith tempfile.TemporaryDirectory')[0])
# Reuse the same geometry/hierarchy/material boundary as the full wilderness registry suite.
setup=source[source.index(' fixture='):source.index(" (p/'Fixture.cs').write_text")]
exec(textwrap.dedent(setup))
fixture=fixture.split('namespace Emberfall {')[0]+fixture[fixture.index('\nnamespace UnityEngine {public struct Bounds'):]
fixture=fixture.replace('public bool HasProperty(string n)=>true;','public bool supported=true;public bool HasProperty(string n)=>supported&&!destroyed;')
# Bounds of the actual mesh are independent of shader-expanded culling bounds.
fixture=fixture.replace('public class Mesh:Object {','public class Mesh:Object {public Bounds bounds{get{return new Bounds{min=new Vector3(vertices.Min(v=>v.x),vertices.Min(v=>v.y),vertices.Min(v=>v.z)),max=new Vector3(vertices.Max(v=>v.x),vertices.Max(v=>v.y),vertices.Max(v=>v.z))};}}')
fixture=fixture.replace('public bool HasProperty(string n)=>supported&&!destroyed;','public bool HasProperty(string n)=>supported&&!destroyed;public float GetFloat(string n)=>0;')
fixture=fixture.replace('public struct Bounds {public Vector3 min,max;','public struct Bounds {public Vector3 min,max;public Bounds(Vector3 center,Vector3 size){min=center-size*.5f;max=center+size*.5f;}public Vector3 center=>(min+max)*.5f;public void Encapsulate(Vector3 p){min=new Vector3(Math.Min(min.x,p.x),Math.Min(min.y,p.y),Math.Min(min.z,p.z));max=new Vector3(Math.Max(max.x,p.x),Math.Max(max.y,p.y),Math.Max(max.z,p.z));}')
fixture=fixture.replace('public Bounds bounds{get{var mesh=','public Bounds? cullingBounds;public Bounds bounds{get{if(cullingBounds.HasValue)return cullingBounds.Value;var mesh=')
fixture+='\nnamespace Emberfall {public static class WorldTerrain {public static float Height(UnityEngine.Vector3 p)=>0;}}\n'
fixture+='\nnamespace Emberfall {public static class CombatFx {public static Material NewGlow()=>new Material(Shader.Find("test"));}}\n'
# The managed fixture models one renderer with Bark/Leaf material slots.
# Native FBX importer acceptance is separate; no saved validation report is an input.
assert (root/'Assets/Resources/BlenderScenery/OpenCanopyTree.fbx').is_file()
with tempfile.TemporaryDirectory(prefix='occlusion-slots-') as tmp:
 p=Path(tmp);(p/'Fixture.cs').write_text(fixture)
 for f in ['Assets/Scripts/Core/CameraOcclusionSurface.cs','Assets/Scripts/Core/CameraVisibilityRules.cs','Assets/Scripts/Core/BuildingOcclusionGroup.cs','Tests/CameraOcclusionSlotsProductionTests.cs']:(p/Path(f).name).write_text((root/f).read_text())
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');cmd=[dotnet,'run','--project',str(project),'--no-restore']
 subprocess.run([dotnet,'restore',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,check=True)
 subprocess.run(cmd,env=env,check=True)
 # The original bug: reading only slot zero must fail a real second-slot fade assertion.
 target=p/'CameraOcclusionSurface.cs';original=target.read_text();target.write_text(original.replace('var slots=renderer.sharedMaterials;','var slots=new[]{renderer.sharedMaterial};'))
 subprocess.run([dotnet,'build',str(project),'--no-restore','-v:q'],env=env,check=True,stdout=subprocess.DEVNULL)
 result=subprocess.run(cmd+['--no-build'],env=env,capture_output=True,text=True)
 assert result.returncode and 'all material slots fade' in result.stdout+result.stderr,result.stdout+result.stderr
 print('PASS: slot-zero-only production mutant rejected by exact dual-slot oracle')

 # The legacy group bug: ignoring unsupported members must fail whole-building refusal.
 target.write_text(original.replace('{if(!surface.CanFade){complete=false;break;}needed+=surface.RequiredFadeSlots;}', '{if(surface.CanFade)needed+=surface.RequiredFadeSlots;}'))
 subprocess.run([dotnet,'build',str(project),'--no-restore','-v:q'],env=env,check=True,stdout=subprocess.DEVNULL)
 result=subprocess.run(cmd+['--no-build'],env=env,capture_output=True,text=True)
 assert result.returncode and 'whole building admission includes all slots' in result.stdout+result.stderr,result.stdout+result.stderr
 print('PASS: unsupported-member-skipping production mutant rejected by whole-building oracle')

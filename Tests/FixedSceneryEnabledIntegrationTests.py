#!/usr/bin/env python3
"""Enabled six-factory integration, exact dispatch/fallback and real occlusion lifecycle.
Unity APIs, traversal storage and text labels are explicit boundaries, not engine execution.
"""
import os,sys,tempfile,subprocess,re
from pathlib import Path
harness=Path(__file__).resolve().parent
prefix=(harness/'FixedSceneryProductionTests.py').read_text().split('\nwith tempfile.TemporaryDirectory')[0]
exec(compile(prefix,str(harness/'FixedSceneryProductionTests.py'),'exec'))
fixture=fixture[:fixture.index('namespace UnityEngine {public class TextAsset:Object')]
fixture+='''namespace UnityEngine {
 public class TextAsset:Object {public byte[] bytes;}
 public static class Resources {
  public static string FaultKey,FaultMode;public static readonly System.Collections.Generic.Dictionary<string,int> Loads=new System.Collections.Generic.Dictionary<string,int>();
  public static T Load<T>(string key) where T:class {Loads[key]=Loads.TryGetValue(key,out var n)?n+1:1;var file=System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("F4_ROOT"),"Assets/Resources",key+".bytes");if(key=="FixedScenery/"+FaultKey&&FaultMode=="missing")return null;var b=System.IO.File.ReadAllBytes(file);if(key=="FixedScenery/"+FaultKey&&FaultMode=="corrupt")b=new byte[]{0,1,2};return new TextAsset{bytes=b} as T;}
 }
 public enum Space{Self,World}
}'''
fixture=fixture.replace('public Material(Material source){color=source.color;}','public Material(Material source){color=source.color;renderQueue=source.renderQueue;foreach(var p in source.floats)floats[p.Key]=p.Value;foreach(var k in source.keywords)keywords.Add(k);}')
fixture=fixture.replace('public void SetInt(string n,int v){}','public void SetInt(string n,int v){floats[n]=v;}').replace('public void DisableKeyword(string s){}','public void DisableKeyword(string s){keywords.Remove(s);}')
fixture=fixture.replace('public void EnableKeyword(string s){}','public readonly HashSet<string> keywords=new HashSet<string>();public void EnableKeyword(string s){keywords.Add(s);}')
fixture=fixture.replace('public bool HasProperty(string n)=>true;','public bool supportsColor=true;public bool HasProperty(string n)=>n!="_Color"||supportsColor;')
fixture=fixture.replace('public class Transform:Component,IEnumerable<Transform> {','public class Transform:Component,IEnumerable<Transform> {public void Rotate(Vector3 e,Space s){localRotation=localRotation*Quaternion.Euler(e.x,e.y,e.z);}')
actual+='namespace Emberfall{'+extract(world,'public sealed class WorldMotion')+'}'
with tempfile.TemporaryDirectory(prefix='fixed-enabled-integration-') as directory:
 p=Path(directory);(p/'Fixture.cs').write_text(fixture);(p/'Types.cs').write_text(data);(p/'World.cs').write_text(actual)
 for f in ['Core/CostumeRecipes','World/AuthoredFixedScenery','World/WorldBuilder.Hubs','World/WorldBuilder.Environment','World/WorldBuilder.AuthoredScenery','World/HubSettlementPlan','World/HubNpcIdle','Core/EnvironmentLightProfile','Core/BuildingOcclusionGroup','Core/CameraOcclusionSurface','Core/CameraVisibilityRules','Combat/ProceduralVisuals','Combat/VisualMeshRecipes']:(p/(Path(f).name+'.cs')).write_text((root/('Assets/Scripts/'+f+'.cs')).read_text())
 s=(harness/'SceneryPresentationProductionTests.cs').read_text();s=re.sub(r' public static class AuthoredFixedScenery[^\n]*\n','',s);s=s.replace('static void BuildCampFacilities(Transform p,WorldResources r,int n){}','').replace('static void Main(string[] args)','static void LegacyMain(string[] args)')
 s=s.replace('public class WorldMotion:MonoBehaviour{public Vector3 spin;public float bob,speed;}','')
 # Unrelated pilot/tree import branches must never be silently exercised as no-ops.
 s=s.replace('=>null;', '=>throw new Exception("unexecuted scenery import boundary");')
 (p/'Boundary.cs').write_text(re.sub(r'public enum WingSilhouette.*?\n','',s))
 (p/'CostumeMeshLibrary.cs').write_text('using UnityEngine;namespace Emberfall{'+extract((root/'Assets/Scripts/Combat/CostumeMeshLibrary.cs').read_text(),'internal static class CostumeMeshLibrary')+'}')
 (p/'Tests.cs').write_text((harness/'FixedSceneryEnabledIntegrationTests.cs').read_text())
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,F4_ROOT=str(root),DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
 subprocess.run([dotnet,'restore',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,check=True)
 def run():return subprocess.run([dotnet,'run','--project',str(project),'--no-restore'],env=env,capture_output=True,text=True)
 result=run();print(result.stdout,result.stderr);assert result.returncode==0
 for file,old,new,expected in [
  ('AuthoredFixedScenery.cs','case "NPC sleeve":return "NpcSleeve";','case "NPC sleeve":return "NpcHead";','exact mapped module for NPC sleeve'),
  ('AuthoredFixedScenery.cs','if(mesh!=null && filter!=null)filter.sharedMesh=mesh;','if(mesh!=null && filter!=null){filter.sharedMesh=mesh;WorldTraversal.AddCircle(obj.transform.position,.1f);}','full navigation membership matches fallback'),
  ('CameraOcclusionSurface.cs','for(int j=0;j<saved.Length;j++)slots[j]=hierarchyFades[saved[j]];','for(int j=0;j<saved.Length;j++)slots[j]=j==0?hierarchyFades[saved[j]]:saved[j];','all material slots get owned fade clones'),
  ('CameraOcclusionSurface.cs','{current[j]=saved[j];changed=true;}','{changed=true;}','all original material references restored'),
 ]:
  if expected is None:continue
  path=p/file;before=path.read_text();assert old in before;path.write_text(before.replace(old,new));result=run();path.write_text(before)
  assert result.returncode!=0 and 'System.Exception: '+expected in result.stdout+result.stderr,result.stdout+result.stderr
  print('PASS compiled negative control:',expected,flush=True)

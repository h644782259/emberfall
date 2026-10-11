#!/usr/bin/env python3
"""Actual fixed-scenery decoder + six production assembly snapshots; managed Unity boundary."""
import os,sys,tempfile,subprocess,re
from pathlib import Path
harness=Path(__file__).resolve().parent
prefix=(harness/'SceneryPresentationProductionTests.py').read_text().split('with tempfile.TemporaryDirectory')[0]
exec(compile(prefix,str(harness/'SceneryPresentationProductionTests.py'),'exec'))
fixture+='namespace UnityEngine {public class TextAsset:Object {public byte[] bytes;}public static class Resources {public static T Load<T>(string path) where T:class {var f=System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("F4_ROOT"),"Assets/Resources",path+".bytes");return System.IO.File.Exists(f)?new TextAsset{bytes=System.IO.File.ReadAllBytes(f)} as T:null;}}}'
actual+='namespace Emberfall{public static partial class WorldBuilder{'+extract(world,'private static void Pillar(')+extract(world,'private static void BuildCampFacilities(')+'''static void Label(Transform p,string n,string v,Vector3 at,float s,Color c,bool f){}
public static void FixedSample(Transform p,WorldResources r,int i){if(i==0)Pillar(p,r,Vector3.zero,3.5f,false);else if(i==1)Portal(p,r,Vector3.zero,r.Material(Color.white));else if(i==2)BuildTown(p,r,1);else if(i==3)BuildTown(p,r,2);else if(i==4)BuildCampFacilities(p,r,2);else BuildHubNpcs(p,r);}}}'''
with tempfile.TemporaryDirectory(prefix='fixed-scenery-') as directory:
 p=Path(directory);(p/'Fixture.cs').write_text(fixture);(p/'Types.cs').write_text(data);(p/'World.cs').write_text(actual)
 for f in ['Core/CostumeRecipes','World/AuthoredFixedScenery','World/WorldBuilder.Hubs','World/WorldBuilder.Environment','World/WorldBuilder.AuthoredScenery','World/HubSettlementPlan','World/HubNpcIdle','Core/EnvironmentLightProfile','Core/BuildingOcclusionGroup','Core/CameraOcclusionSurface','Core/CameraVisibilityRules','Combat/ProceduralVisuals','Combat/VisualMeshRecipes']:(p/(Path(f).name+'.cs')).write_text((root/('Assets/Scripts/'+f+'.cs')).read_text())
 s=(harness/'SceneryPresentationProductionTests.cs').read_text();s=re.sub(r' public static class AuthoredFixedScenery[^\n]*\n','',s);s=s.replace('static void BuildCampFacilities(Transform p,WorldResources r,int n){}','').replace('static void Main(string[] args)','static void LegacyMain(string[] args)');(p/'Boundary.cs').write_text(s)
 (p/'CostumeMeshLibrary.cs').write_text('using UnityEngine;namespace Emberfall{'+extract((root/'Assets/Scripts/Combat/CostumeMeshLibrary.cs').read_text(),'internal static class CostumeMeshLibrary')+'}')
 (p/'Boundary.cs').write_text(re.sub(r'public enum WingSilhouette.*?\n','',s))
 (p/'Tests.cs').write_text((harness/'FixedSceneryProductionTests.cs').read_text())
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,F4_ROOT=str(root),F4_OUTPUT=str(root/"Tests/TestResults/FixedScenery-Latest"),DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
 for args in [['restore',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'],['build',str(project),'--no-restore','-v:q'],[str(p/'bin/Debug/net8.0/Test.dll')]]:
  q=subprocess.run([dotnet]+args,env=env,capture_output=True,text=True);print(q.stdout,q.stderr);assert q.returncode==0

import gzip
snapshot=root/"Tests/TestResults/FixedScenery-Latest/factory-snapshots.json"
(root/"Tests/TestResults/FixedScenery-Latest/factory-snapshots.json.gz").write_bytes(gzip.compress(snapshot.read_bytes(),mtime=0))
snapshot.unlink()

snapshot=root/"Tests/TestResults/FixedScenery-Latest/factory-before-snapshots.json"
(root/"Tests/TestResults/FixedScenery-Latest/factory-before-snapshots.json.gz").write_bytes(gzip.compress(snapshot.read_bytes(),mtime=0))
snapshot.unlink()

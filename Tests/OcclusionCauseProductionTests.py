#!/usr/bin/env python3
"""Execute the actual Advance body, with renderer/group admission doubles (not shader validation)."""
import os,sys,tempfile,subprocess
from pathlib import Path
r=Path(__file__).resolve().parents[1];s=(r/'Assets/Scripts/Core/CameraOcclusionSurface.cs').read_text()
def method(signature):
 start=s.index(signature);end=s.index('{',start);depth=1;end+=1
 while depth:
  depth+=(s[end]=='{')-(s[end]=='}');end+=1
 return s[start:end]
body=method('public static void Advance(Vector3 camera,Vector3 torso')+method('private static bool Protects(')+method('private bool HierarchyProtects(')
prefix='''using System;using System.Collections.Generic;using Emberfall;
struct Vector3 {public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}}
struct Bounds {public Vector3 min,max;public void Expand(float f){min=new Vector3(min.x-f/2,min.y-f/2,min.z-f/2);max=new Vector3(max.x+f/2,max.y+f/2,max.z+f/2);}}
class Renderer {public bool enabled=true;public Bounds bounds;}
class BuildingOcclusionGroup {public void SetOccluded(bool b){}}
class CameraOcclusionSurface {
static List<CameraOcclusionSurface> surfaces=new List<CameraOcclusionSurface>();static HashSet<BuildingOcclusionGroup> hitGroups=new HashSet<BuildingOcclusionGroup>(),admittedGroups=new HashSet<BuildingOcclusionGroup>();static int fadedCount=32;
Renderer visual;Renderer[] hierarchy=null;Dictionary<object,object> hierarchyFades=new Dictionary<object,object>();int RequiredFadeSlots=>fade==null?1:0;object fade=null;BuildingOcclusionGroup group=null;bool requested,heroRequested,targetRequested;bool externalChange;bool BindingsIntact()=>true;void Restore(){}bool CanFade=>true;void SetFade(bool b,float d){}
public static int LastOccluders,LastHeroOccluders,LastTargetOccluders;
'''
test='''public static void Main(){var surface=new CameraOcclusionSurface{visual=new Renderer{bounds=new Bounds{min=new Vector3(4,-1,4),max=new Vector3(6,1,6)}}};surfaces.Add(surface);
var camera=new Vector3(0,0,0);var hero=new Vector3(-10,0,10);var target=new Vector3(10,0,10);
Advance(camera,hero,hero,target,true,.016f);if(LastOccluders!=1||LastHeroOccluders!=0||LastTargetOccluders!=1)throw new Exception("Target obstruction must not claim hero obstruction even with full material cap");
Advance(camera,hero,hero,target,false,.016f);if(LastOccluders!=0||LastHeroOccluders!=0||LastTargetOccluders!=0)throw new Exception("Lost target clears demand next tick");
Advance(camera,target,target,hero,true,.016f);if(LastHeroOccluders!=1||LastTargetOccluders!=0)throw new Exception("Hero obstruction tracked independently");
Advance(camera,target,target,target,true,.016f);if(LastHeroOccluders!=1||LastTargetOccluders!=1||LastOccluders!=1)throw new Exception("Shared obstruction union counted once");
surface.visual.enabled=false;Advance(camera,target,target,target,true,.016f);if(LastOccluders!=0)throw new Exception("Disabled renderer excluded");
Console.WriteLine("PASS 5 actual Advance obstruction cases; renderer/material admission doubled");}}
'''
with tempfile.TemporaryDirectory(prefix='occlusion-cause-') as d:
 p=Path(d);(p/'Host.cs').write_text(prefix+body+test);(p/'Rules.cs').write_text((r/'Assets/Scripts/Core/CameraVisibilityRules.cs').read_text())
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
 subprocess.run([dotnet,'restore',str(p/'Test.csproj'),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,check=True)
 subprocess.run([dotnet,'run','--project',str(p/'Test.csproj'),'--no-restore'],env=env,check=True)

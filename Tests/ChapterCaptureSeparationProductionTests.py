"""Production chapter capture placement and exit separation; managed geometry, no rendering."""
from pathlib import Path
import importlib.util,tempfile,subprocess,os
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
code='''using System;using Emberfall;using UnityEngine;
namespace Emberfall{public enum ChapterNode{ForestCourt,Redrock,StarPlatform}public static class BossScaleRules{public static bool ReviewLegacyDimensions=false;}}
class Program{static int n;static void C(bool v,string m){n++;if(!v)throw new Exception(m);}static void Main(){
foreach(ChapterNode node in Enum.GetValues(typeof(ChapterNode)))for(int room=0;room<2;room++)for(int seed=0;seed<32;seed++){
var p=ChapterRoomGeometry.Plan(node,room,seed);WorldTraversal.Reset(ZoneKind.Dungeon);WorldTraversal.SetArenaRadius(ArenaSizeRules.Radius(ZoneKind.Dungeon,p.Layout,0));ChapterRoomGeometry.Register(p);
foreach(var capture in p.Objectives){C(Vector3.Distance(capture,p.Exit)>7.2f,"capture and exit trigger separated");foreach(float radius in new[]{.45f,.65f,1.3f}){
C(WorldTraversal.IsWalkable(capture,radius),"capture center walkable "+node+"/"+room);
C(WorldTraversal.CanReach(p.Entrance,capture,radius),"entrance reaches capture "+node+"/"+room);
C(WorldTraversal.CanReach(capture,p.Exit,radius),"capture reaches exit "+node+"/"+room);}}}
Console.WriteLine("PASS "+n+" chapter capture separation / reachability assertions");}}
'''
with tempfile.TemporaryDirectory(prefix='capture-separation-') as tmp:
 p=Path(tmp);sources=[root/('Assets/Scripts/World/'+x+'.cs') for x in ['WorldTraversal','TacticalRoomGeometry','ChapterRoomGeometry','ArenaSizeRules']]+[root/'Tests/DestructibleTraversalTests.cs']
 project=cv.write_project(p/'project',sources,code)
 subprocess.run(['/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet','run','--project',str(project)],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',MSBuildEnableWorkloadResolver='false'))

"""Validate expanded populations with production navigation and host spawn placement."""
from pathlib import Path
import importlib.util, tempfile, subprocess, os, sys
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py')
cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
host=(root/'Assets/Scripts/Core/GameSession.Chapter.cs').read_text(encoding='utf-8')
a=host.index('private bool TryChapterSpawn(');b=host.index('{',a)+1;depth=1
while depth:
    depth+=(host[b]=='{')-(host[b]=='}');b+=1
program=r'''
using System;using System.Collections.Generic;using UnityEngine;using Emberfall;
namespace Emberfall {public enum ChapterNode{ForestCourt,Redrock,StarPlatform}public enum EnemyKind{Slime,Goblin,Wisp,Guardian}public class SummonedCompanion{public enum Kind{Treant,Wolf,Other}}}
class Probe {
ChapterRoomPlan chapterPlan;int ChapterSeed=>chapterPlan.Seed;int ChapterRoomIndex=>chapterPlan.Room;
bool ForestMobileLineup=>chapterPlan.Node==ChapterNode.ForestCourt&&(ChapterSeed&2)!=0;
METHOD
static void Main(){int checks=0;
for(int seed=0;seed<32;seed++)foreach(ChapterNode node in Enum.GetValues(typeof(ChapterNode)))for(int room=0;room<(node==ChapterNode.StarPlatform?1:2);room++){
var p=ChapterRoomGeometry.Plan(node,room,seed);WorldTraversal.Reset(ZoneKind.Dungeon);ChapterRoomGeometry.Register(p);
var probe=new Probe{chapterPlan=p};var occupied=new List<Vector3>();
for(int i=0;i<(node==ChapterNode.StarPlatform?21:30);i++){
Vector3 at;float radius=node==ChapterNode.StarPlatform&&i==0?2.4f:.65f;
if(!probe.TryChapterSpawn(i,node==ChapterNode.Redrock,occupied,radius,out at))throw new Exception($"spawn failed {node} room={room} seed={seed} index={i}");
if(!WorldTraversal.CanReach(p.Entrance,at,radius))throw new Exception("unreachable spawn");
foreach(var other in occupied)if(Vector3.Distance(at,other)<2.4f)throw new Exception("overlapping spawns");
occupied.Add(at);checks++;
}}
for(int seed=0;seed<32;seed++)for(int room=0;room<4;room++){
WorldTraversal.Reset(ZoneKind.Dungeon);TacticalRoomGeometry.Register(20+RoomTactics.Terrain(seed,room)*2+(RoomTactics.Mirror(seed)<0?1:0));var occupied=new List<Vector3>();
for(int i=0;i<30;i++){Vector3 at;if(!TacticalRoomGeometry.TrySpawn(seed,room,i,occupied,out at))throw new Exception("expanded expedition spawn failed");occupied.Add(at);checks++;}}
Console.WriteLine("PASS "+checks+" expanded population spawn, spacing and reachability checks");}}
'''.replace('METHOD',host[a:b])
with tempfile.TemporaryDirectory(prefix='expanded-population-') as tmp:
    p=Path(tmp);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
    sources=[root/('Assets/Scripts/'+name+'.cs') for name in ['World/WorldTraversal','World/ChapterRoomGeometry','World/TacticalRoomGeometry','World/ArenaSizeRules','Combat/BossScaleRules','Core/RoomTactics']]+[root/'Tests/DestructibleTraversalTests.cs']
    project=cv.write_project(p/'project',sources,program)
    subprocess.run([sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(project)],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))

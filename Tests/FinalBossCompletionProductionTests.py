#!/usr/bin/env python3
"""Execute production chapter/room state transitions with surviving guards."""
from pathlib import Path
import os, subprocess, tempfile
root=Path(__file__).resolve().parents[1]
code=r'''
using System;using Emberfall;
class Program{
 static int count;static void C(bool ok,string message){count++;if(!ok)throw new Exception(message);}
 static RoomChainState BossRoom(){var run=new RoomChainState(2);for(int room=0;room<4;room++){
 var p=run.Room;for(int i=0;i<p.EnemyCount;i++)run.Register(p,i);
 if(p.Objective==RoomObjective.Hunt){for(int i=0;i<p.EnemyCount;i++)run.Defeat(p,i);}
 else if(p.Objective==RoomObjective.Purify){for(int seal=0;seal<2;seal++)for(int i=0;i<12;i++)run.AdvanceSeal(seal,.25f,true,true,false);}
 else for(int i=0;i<12;i++)run.Advance(.25f,true,true,false);
 if(room==1){C(run.OpenBranchChoice(),"branch opens");C(run.SelectBranch(RoomBranch.Supply),"select branch");}
 C(run.Next(true,false),"ordinary objective still required");}return run;}
 static void Main(){
 var run=BossRoom();var plan=run.Room;C(plan.Boss,"final boss room");C(!run.Defeat(plan,0),"unregistered boss rejected");
 for(int i=0;i<plan.EnemyCount;i++)run.Register(plan,i);
 C(run.Defeat(plan,1)&&!run.Finished,"guard cannot end boss encounter");
 C(run.Defeat(plan,0)&&run.Finished&&!run.Failed,"boss death ends encounter with live guards");
 C(!run.Defeat(plan,2)&&!run.Defeat(plan,0),"late guard and duplicate boss deaths rejected");
 C(!run.ClaimReward(false)&&run.ClaimReward(true)&&!run.ClaimReward(true),"reward remains durable and once only");
 foreach(ChapterDifficulty difficulty in Enum.GetValues(typeof(ChapterDifficulty))){
 var chapter=new ChapterCombatRun(ChapterNode.StarPlatform,difficulty,1);chapter.BindRoom(7);
 for(int i=0;i<chapter.EnemyCount;i++)chapter.Register(0,7,i);
 C(!chapter.Defeat(0,6,0),"stale epoch boss rejected");C(chapter.Defeat(0,7,1)&&!chapter.DoorUnlocked,"chapter guard does not clear boss");
 C(chapter.Defeat(0,7,0)&&chapter.DoorUnlocked&&chapter.LivingRegisteredEnemies>0,"chapter boss unlocks with survivors");
 C(chapter.FinishBoss()&&chapter.Finished&&!chapter.Failed,"chapter finishes immediately");
 C(!chapter.Defeat(0,7,2)&&!chapter.FinishBoss(),"post completion events ignored");}
 Console.WriteLine("PASS "+count+" production boss completion assertions");}}
'''
with tempfile.TemporaryDirectory(prefix='emberfall-boss-completion-') as tmp:
 p=Path(tmp)
 for name in ['RoomTactics','RoomChainState','ChapterCombatRun']:(p/(name+'.cs')).write_text((root/'Assets/Scripts/Core'/(name+'.cs')).read_text())
 definition=(root/'Assets/Scripts/Core/ChapterProgression.cs').read_text();definition=definition[:definition.index('    public static class ChapterProgression')]
 (p/'ChapterDefinition.cs').write_text(definition+'public static class ChapterProgression{public static bool Valid(ChapterNode n)=>(int)n>=0&&(int)n<3;}}')
 (p/'Program.cs').write_text(code)
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 sdk='/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet'
 subprocess.run([sdk,'run','--project',str(p/'Test.csproj')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))
session=(root/'Assets/Scripts/Core/GameSession.cs').read_text()
assert 'if(boss&&DungeonWave==TotalWaves)RetireRemainingEncounterEnemies();' in session
retire=(root/'Assets/Scripts/Core/GameSession.EncounterCompletion.cs').read_text()
assert retire.index('Enemies.Clear()')<retire.index('enemy.gameObject.SetActive(false)') and 'OnEnemyKilled' not in retire
print('PASS final-wave host retires guards without awarding kills')

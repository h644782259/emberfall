#!/usr/bin/env python3
"""Actual session completion + durable progression, with explicit engine boundaries."""
from CastReceiptFixtureSources import include_cast_receipt_source
from pathlib import Path
import os,subprocess,sys,tempfile
root=Path(__file__).resolve().parents[1]
dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
def member(path,signature,after=None):
 source=(root/path).read_text();a=source.index(signature,source.index(after) if after else 0);b=source.index('{',a)+1;depth=1
 while depth:depth+=(source[b]=='{')-(source[b]=='}');b+=1
 return source[a:b]
core=['GameTypes','CombatBalance','ProgressionService','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Trading','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','ChapterProgression','ChapterCombatRun','ChapterResultSnapshot','RoomTactics','RoomChainState','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ExpeditionModeState','AdventureResultPolicy','RunMechanismEvidence','CombatImpactBatch','CampPracticeRecord']
with tempfile.TemporaryDirectory(prefix='dungeon-completion-') as temporary:
 p=Path(temporary)
 for name in core:(p/(name+'.cs')).write_text((root/'Assets/Scripts/Core'/(name+'.cs')).read_text())
 for name in ['ProgressionTests','ExpeditionModeStateTests','DungeonCompletionBoundary']:(p/(name+'.cs')).write_text((root/'Tests'/(name+'.cs')).read_text())
 modes='Assets/Scripts/Core/GameSession.Modes.cs';chapter='Assets/Scripts/Core/GameSession.Chapter.cs';room='Assets/Scripts/Core/GameSession.RoomChain.cs'
 methods=[member('Assets/Scripts/Core/GameSession.cs','public void OnEnemyKilled(')]
 methods += [member(modes,sig) for sig in ['public bool ModeFinished','public bool ModeRewardPending','private void BeginArenaPhase()','private void RecordArenaDefeat(','private void TickArenaRun()','private void FinalizeArenaResult()','private void ApplyRewardPresentation(','private string RewardPresentationText(','public bool TrySettleArenaReward()']]
 methods += [member(chapter,sig) for sig in ['public bool ChapterFinished','public bool ChapterRewardPending','private bool RecordChapterDefeat(','private void FinalizeChapterBoss()','private ChapterResultSnapshot CaptureChapterResult(','private void FinalizeChapter()','public bool TrySettleChapterReward()']]
 methods += [member(room,sig) for sig in ['private void FinalizeRoomChain()','private bool TrySettleRoomReward()']]
 (p/'SessionMethods.cs').write_text('using System;using System.Collections.Generic;using UnityEngine;using Random=UnityEngine.Random;namespace Emberfall{public sealed partial class GameSession{'+''.join(methods)+'}}')
 for path in ['Assets/Scripts/Core/GameSession.EncounterCompletion.cs','Assets/Scripts/Combat/CombatProjectile.EncounterRetirement.cs']:(p/Path(path).name).write_text((root/path).read_text())
 (p/'ProjectileOnDisable.cs').write_text('namespace Emberfall{internal sealed partial class CombatProjectile{'+member('Assets/Scripts/Combat/CombatEffects.cs','private void OnDisable()','internal sealed partial class CombatProjectile')+'}}')
 (p/'Program.cs').write_text('System.Console.WriteLine(ExpeditionModeStateTests.Run());System.Console.WriteLine(Emberfall.GameSession.VerifyCompletion(args[0]));')
 project=p/'Tests.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414;0169</NoWarn></PropertyGroup></Project>');include_cast_receipt_source(project)
 config=p/'NuGet.Config';config.write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
 build=[dotnet,'build',str(project),'--configfile',str(config),'-v:q'];run=[dotnet,str(p/'bin/Debug/net8.0/Tests.dll')]
 subprocess.run(build,env=env,check=True);subprocess.run(run+[str(p/'saves')],env=env,check=True)
 mutations=[
  ('ExpeditionModeState.cs','if(Mode==ExpeditionModeKind.BossGauntlet)\n            {\n                // Adds provide pressure','if(false)\n            {\n                // Adds provide pressure','registered boss alone completes gauntlet phase'),
  ('SessionMethods.cs','ChapterRun.FinishBoss()||ChapterRun.FinishClearedFinalRoom()','ChapterRun.FinishBoss()','final forest clear directly publishes result without portal interaction'),
  ('SessionMethods.cs','arenaEnemies.Clear();RetireRemainingEncounterEnemies();arenaPhaseRetiredFrame','arenaEnemies.Clear();arenaPhaseRetiredFrame','gauntlet boss retires live adds without synthetic deaths'),
  ('CombatProjectile.EncounterRetirement.cs','projectile.session != owner || !projectile.hostile','!projectile.hostile','retirement preserves foreign host friendly casts and player epoch'),
  ('SessionMethods.cs','Time.frameCount>arenaPhaseRetiredFrame&&!CombatImpactBatch.InAction','true','same-frame area attack cannot reach next phase')
 ]
 for index,(name,before,after,oracle) in enumerate(mutations):
  path=p/name;original=path.read_text();assert original.count(before)==1,(name,before)
  path.write_text(original.replace(before,after))
  subprocess.run(build+['--no-restore'],env=env,check=True,stdout=subprocess.DEVNULL)
  result=subprocess.run(run+[str(p/('negative-'+str(index)))],env=env,capture_output=True,text=True)
  path.write_text(original)
  assert result.returncode and 'System.Exception: '+oracle in result.stdout+result.stderr,result.stdout+result.stderr
  print('PASS compiled completion negative: '+oracle,flush=True)
 print('PASS: 5 compiled completion mutations rejected by exact runtime assertions',flush=True)

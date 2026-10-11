"""Real chapter/UI return and session time-scale/reset methods; managed scene doubles, not Unity play."""
from CastReceiptFixtureSources import include_cast_receipt_source
from pathlib import Path
import os,sys,tempfile,subprocess
root=Path(__file__).resolve().parents[1]
dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
def member(file,signature):
 s=(root/file).read_text();a=s.index(signature);b=s.index('{',a)+1;d=1
 while d:d+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
core=['CombatImpactBatch','CampPracticeRecord','ThreatAdmissionPolicy','RunMechanismEvidence','RunChoices','RunChoices.Rooms','RunChoices.Chapter','GameTypes','ProgressionService','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Trading','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','ChapterProgression','ChapterResultSnapshot','ChapterCombatRun','RoomTactics','RoomTacticalRegion','CombatBalance','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','AdventureResultPolicy','GameSession.Chapter','GameSession.ChapterSeals','EscapePostPolicy','ApplicationPauseState','SafeSaveFlow','SaveLifecycleGate','RoomChainState','ExpeditionModeState','ScenePreparationState']
with tempfile.TemporaryDirectory(prefix='chapter-return-clock-') as t:
 p=Path(t)
 for n in core:(p/(n+'.cs')).write_text((root/'Assets/Scripts/Core'/(n+'.cs')).read_text())
 (p/'EnemyControlPolicy.cs').write_text((root/'Assets/Scripts/Combat/EnemyControlPolicy.cs').read_text())
 for path in ['Assets/Scripts/UI/ChapterSealPresentation.cs','Assets/Scripts/World/ChapterRoomGeometry.cs','Assets/Scripts/World/ArenaSizeRules.cs','Assets/Scripts/Combat/BossScaleRules.cs','Assets/Scripts/UI/ChapterEntryPresentation.cs','Tests/ProgressionTests.cs','Tests/ChapterReturnTimeScaleTests.cs']:(p/Path(path).name).write_text((root/path).read_text())
 clock_tests=p/'ChapterReturnTimeScaleTests.cs'
 clock_source=clock_tests.read_text();unlock='s.FixtureUnlock();s.SelectedChapterNode=ChapterNode.StarPlatform;'
 assert unlock in clock_source
 # Persist a role eligible for the real StarPlatform level gate before entry.
 clock_tests.write_text(clock_source.replace(unlock,'s.Progression.Profile.level=ChapterProgression.UnlockLevel(ChapterNode.StarPlatform);'+unlock))
 fixture=(root/'Tests/ChapterHostFixture.cs').read_text()
 replacements={
 'deltaTime=.25f,time;':'deltaTime=.25f,time,timeScale=1;',
 'public int CombatEpoch=1,':'public void Initialize(GameSession s,HeroClass h){}public int CombatEpoch=1,',
 'public class FakeChoices {public void Reset(){}public void Cancel(){}}':'public class FakeChoices {public bool AwaitingChoice;public void Reset(){AwaitingChoice=false;}public void Cancel(){AwaitingChoice=false;}}',
 'Paused,BackgroundPaused,SceneLoading,IsInCamp=true;':'Paused,IsInCamp=true;public bool SceneLoading=>scenePreparation.Active;public bool BackgroundPaused=>pauseState.BackgroundPaused;',
 'public bool InputBlocked=>SceneLoading||Paused||BackgroundPaused||IsDead||ChapterFinished;':member('Assets/Scripts/Core/GameSession.cs','public bool InputBlocked'),
 'void UpdateTimeScale(){}':'',
 'public void Notify(string s)':'string SaveLoadError;GameUI ui;float autosaveTimer;readonly SaveLifecycleGate lifecycleSave=new SaveLifecycleGate();void OnProgressChanged(){}void OnLevelUp(int level){}void DiscardForeignSideEventRewards(){}readonly ApplicationPauseState pauseState=new ApplicationPauseState();readonly ScenePreparationState scenePreparation=new ScenePreparationState();object scenePreparationRoutine;IEnumerator scenePreparationDriver;void CancelScenePreparation(){scenePreparation.Cancel();}public bool InCombat=>false;public bool FinishedResultReady=>ModeFinished||DungeonCleared;public bool FinishedResultDismissed=>false;void UpdateMobileFrameRate(){}string equipmentFingerprint;readonly Dictionary<string,PendingLoot> pendingLoot=new Dictionary<string,PendingLoot>();readonly HashSet<string> collectedGroundLoot=new HashSet<string>();class PendingLoot{public FakePickup Pickup;}class FakePickup{public void Retire(){}}void StopAllCoroutines(){}\npublic void Notify(string s)'
 }
 for a,b in replacements.items():
  assert a in fixture,a
  fixture=fixture.replace(a,b)
 # Rendering and model warmup remain scene boundaries. The real adventure
 # initializer and zone/recovery decisions below still execute unchanged.
 fixture=fixture.replace('private IEnumerator PrepareZone(bool dungeon)','private IEnumerator PrepareZone(bool dungeon,float start=.08f)')
 fixture=fixture.replace('public void BeginDeath(){IsDead=true;}','public void RetireAttackForCampRecovery(){}public void BeginDeath(){IsDead=true;}')
 # Adjacent host assertions share this generated file; route their explicit
 # loading setup through the real state object without a mutable gate field.
 fixture=fixture.replace('s.SceneLoading=true;','s.FixtureSetSceneLoading(true);').replace('s.SceneLoading=false;','s.FixtureSetSceneLoading(false);')
 fixture=fixture.replace('public void Notify(string s)','public void FixtureSetSceneLoading(bool active){if(active)scenePreparation.Begin("fixture loading");else scenePreparation.Cancel();}public void Notify(string s)')
 preparation='Assets/Scripts/Core/GameSession.ScenePreparation.cs'
 driver=member(preparation,'private IEnumerator DriveScenePreparation(')
 cleanup=driver[driver.index('            finally')+len('            finally'):].strip()
 # Drop the outer method brace while retaining its actual finally block.
 cleanup=cleanup[:cleanup.rfind('}')].strip()
 complete='private void CompletePreparationBoundary(Stack<IEnumerator> stack,bool success)'+cleanup
 start=member('Tests/ChapterHostFixture.cs','private void StartScenePreparation(')
 fixture=fixture.replace(start,'private void StartScenePreparation(IEnumerator work,string label){scenePreparation.Begin(label);SuspendInputs();UpdateTimeScale();var stack=new Stack<IEnumerator>();stack.Push(work);bool success=false;try{while(stack.Count>0){if(!stack.Peek().MoveNext()){(stack.Pop() as IDisposable)?.Dispose();continue;}if(stack.Peek().Current is IEnumerator nested)stack.Push(nested);}success=true;}finally{CompletePreparationBoundary(stack,success);}}'+complete)
 fixture+='''namespace UnityEngine{public static class Physics{public static void SyncTransforms(){}}}
 namespace Emberfall{
 public static class CombatModel{public static IEnumerator PrewarmModels(Action<float> progress){yield break;}}
 public static class FilledSkillVfx{public static IEnumerator Prewarm(){yield break;}}
 public static class ElementalCombatVfx{public static IEnumerator Prewarm(){yield break;}}
 public static class PhysicalSkillVfx{public static IEnumerator PrewarmAssets(){yield break;}}
 public static class CombatShaderWarmup{public static IEnumerator Prewarm(Action<float> progress=null){yield break;}}
 }'''
 (p/'Fixture.cs').write_text(fixture)
 methods=[member('Assets/Scripts/Core/GameSession.cs',sig) for sig in ['private bool ContinueAdventure(string slotId, bool discardUnsaved = false, bool alreadySaved = false)','public bool LoadSaveFromPause(','private bool ChangeZone(bool dungeon)','private bool RecoverInCurrentCamp()','private bool CommitZoneState(bool dungeon)','public bool SaveBeforeLeaving()','private void SpawnEnemy(','public void OnEnemyKilled(','public void OnPlayerDied()','public void ReturnToCamp()','private void UpdateTimeScale()','private void BeginAdventure()','private void DiscardTransientAdventureForLoad()','public void Respawn()','public bool QuitToTitle(']]
 methods.append(member(preparation,'private IEnumerator PrepareAdventure()'))
 methods.append(member('Assets/Scripts/Core/GameSession.RoomTactics.cs','private static bool LiveRoomEnemy('))
 methods+=[member('Assets/Scripts/Core/GameSession.Expedition.cs','private void ResetExpedition('),member('Assets/Scripts/Core/GameSession.Modes.cs','public bool ModeFinished')]
 (p/'Lifecycle.cs').write_text('using System;using System.Collections;using System.Collections.Generic;using UnityEngine;using Random=UnityEngine.Random;namespace Emberfall{public sealed partial class GameSession{'+''.join(methods)+'}}')
 ui='namespace Emberfall{public sealed class GameUI{GameSession session;public void RebindProgressionNotifications(ProgressionService a,ProgressionService b){}void BlockUITransition(){}public GameUI(GameSession s){session=s;}public void Return()=>ReturnFromChapter();'+member('Assets/Scripts/UI/GameUI.Chapter.cs','private void ReturnFromChapter()')+'}}'
 (p/'UI.cs').write_text(ui)
 enemy=(root/'Assets/Scripts/Combat/EnemyController.cs').read_text();a=enemy.index('            int challengeTier = game.InDungeon&&game.ChapterActive ? game.DungeonTier : 1;');b=enemy.index('            Health = MaxHealth;',a)+len('            Health = MaxHealth;')
 (p/'EnemyStats.cs').write_text('using UnityEngine;namespace Emberfall{public sealed partial class EnemyController{public float MaxHealth,Health,damage;void ApplySpawnStats(GameSession game,int level,bool boss){var kind=Kind;'+enemy[a:b]+'}}}')
 (p/'Program.cs').write_text('System.Console.WriteLine(Emberfall.GameSession.VerifyReturnClock(args[0]));')
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');include_cast_receipt_source(project)
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
 build=[dotnet,'build',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'];run=[dotnet,str(p/'bin/Debug/net8.0/Test.dll')]
 subprocess.run(build,env=env,check=True);subprocess.run(run+[str(p/'saves')],env=env,check=True)
 source=p/'Lifecycle.cs';original=source.read_text();fixture_source=p/'Fixture.cs';fixture_original=fixture_source.read_text()
 # Mutants must compile before exact runtime failure can count as evidence.
 for signature,expected in [('driver completion finally','successful UI chapter return resumes camp time'),('private void DiscardTransientAdventureForLoad()','discarded role cannot leave an advancing clock')]:
  if signature=='driver completion finally':
   assert 'UpdateTimeScale();' in complete
   fixture_source.write_text(fixture_original.replace(complete,complete.replace('UpdateTimeScale();','')))
  else:
   body=member('Assets/Scripts/Core/GameSession.cs',signature);assert 'UpdateTimeScale();' in body
   source.write_text(original.replace(body,body.replace('UpdateTimeScale();','')))
  subprocess.run(build,env=env,check=True,stdout=subprocess.DEVNULL)
  failed=subprocess.run(run+[str(p/('mutant-'+str(len(signature))))],env=env,capture_output=True,text=True)
  assert failed.returncode and 'System.Exception: '+expected in failed.stdout+failed.stderr,failed.stdout+failed.stderr
  fixture_source.write_text(fixture_original);source.write_text(original)
 source.write_text(original)
 print('PASS: both compiled production driver-completion/discard clock mutations fail exact production-chain assertions')

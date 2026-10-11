#!/usr/bin/env python3
"""Real session result admission, player release/payment/emission and stale-cast epoch."""
from pathlib import Path
import os,sys,tempfile,subprocess
from CastReceiptFixtureSources import include_cast_receipt_source
from MobileBindingFixtureSources import include_mobile_binding_sources
r=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
def member(path,signature):
 s=(r/'Assets/Scripts'/path).read_text();a=s.index(signature);b=s.index('{',a)+1;d=1
 while d:d+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
fixture=(r/'Tests/MobilePinnedTargetProductionTests.cs').read_text()
fixture=fixture.replace('public class GameSession{','public sealed partial class GameSession{')
fixture=fixture.replace('public bool PracticeActive=>false;','public bool PracticeActive;public CampPracticeRecord PracticeRecord;')
fixture=fixture.replace('public bool HasStarted=true,InputBlocked,CombatEnded,CombatEffectsEnded;','public static GameSession Instance;public bool HasStarted=true,SceneLoading,Paused,IsDead,ModeFinished,DungeonCleared,BossDeathPresenting,RoomBranchChoiceOpen,DungeonSelectionOpen;private bool uiBlocking;public ChoiceBoundary RunChoices=new ChoiceBoundary();private PauseBoundary pauseState=new PauseBoundary();public class ChoiceBoundary{public bool AwaitingChoice;}public class PauseBoundary{public bool BackgroundPaused;}private PlayerController dismissedResultOwner;private int dismissedResultEpoch=-1;public void SetUIBlocking(bool value){uiBlocking=value;}private void UpdateTimeScale(){}')
fixture=fixture.replace('public static bool LeapAllowed=true;', 'public static bool IsWalkable(Vector3 p,float radius)=>true;public static bool HasGroundPath(Vector3 a,Vector3 b,float radius)=>true;public static bool LeapAllowed=true;')
fixture=fixture.replace('public static float Angle(Vector3 a,Vector3 b)', 'public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;public static float Angle(Vector3 a,Vector3 b)')
fixture=fixture.replace('public void SkillPerformanceBeat(int skill,int castId){}','public void BeginDelayedSkillRelease(int skill,float delay){}public void SkillPerformanceBeat(int skill,int castId){}')
fixture=fixture.replace('public static void Friendly(PlayerController player,GameSession game,Vector3 at,Vector3 forward,CombatDamage amount,Color tint,bool piercing=false,bool arrow=false,bool basic=false,float size=1,float velocity=0,EnemyController tracking=null,CombatDamage blastDamage=default,float blastRadius=0,int skillIndex=-1,int castId=0,object companionSource=null,ProjectileVolleyBudget<EnemyController> volley=null,EnemyController markTarget=null,float markStrength=0){}','public static void Friendly(PlayerController player,GameSession game,Vector3 at,Vector3 forward,CombatDamage amount,Color tint,bool piercing=false,bool arrow=false,bool basic=false,float size=1,float velocity=0,EnemyController tracking=null,CombatDamage blastDamage=default,float blastRadius=0,int skillIndex=-1,int castId=0,object companionSource=null,ProjectileVolleyBudget<EnemyController> volley=null,EnemyController markTarget=null,float markStrength=0){player.RecordEmission(at);}')
fixture=fixture.replace('public static bool HealingTarget;','public static int Transfers;public static void TransferPermanentPartners(PlayerController owner){Transfers++;}public static bool HealingTarget;')
fixture=fixture.replace('public CombatOpportunityState[] OpportunityWindows=', 'private EnemyController focusedEnemy;private readonly Dictionary<EnemyController,object> aimGeometry=new Dictionary<EnemyController,object>();private void CancelTransientInput(){}public CombatOpportunityState[] OpportunityWindows=')
# Old targeting suite is not executed, so its direct writes to doubled input
# flags are omitted. Its engine/emission boundaries remain explicit above.
fixture=fixture[:fixture.index('public static class MobilePinnedTargetProductionTests')]
fixture+='namespace Emberfall{internal static class FrostFlameStrideVisual{internal static void Spawn(PlayerController hero,System.Func<float> seconds){}}}'
with tempfile.TemporaryDirectory(prefix='post-victory-actions-') as t:
 p=Path(t)
 paths=['Combat/PlayerController.SkillRelease.cs','Combat/PlayerController.SkillAvailability.cs','Combat/PlayerController.ResultDismissal.cs','Core/AdventureResultPolicy.cs','Core/CampPracticeRecord.cs','Core/CombatImpactBatch.cs','Core/SkillRuntime.cs','Core/GameTypes.cs','Core/CombatBalance.cs','Core/MobileCameraGesture.cs','Core/SkillDamageBudgets.cs','Combat/MobileSkillPolicy.cs','Combat/ReturningCounterRules.cs','Combat/PlayerController.MobileFocus.cs','Combat/SkillChargeController.cs','UI/MobileControlLayout.cs','Core/CombatOpportunityState.cs','UI/MobileControls.OpportunityInput.cs','Core/CastFirstHitReceipt.cs','Combat/PlayerController.CastReceipts.cs']
 for path in paths:(p/Path(path).name).write_text((r/'Assets/Scripts'/path).read_text())
 include_mobile_binding_sources(p,r,True)
 binding=p/'BindingMethods.cs';binding.write_text(binding.read_text().replace('public partial class GameUI{','public partial class GameUI{'+member('UI/GameUI.cs','private static bool CombatSkillLearned(')))
 (p/'Fixture.cs').write_text(fixture)
 signatures=['internal bool IsMobileSelfCenteredSkill(', 'private EnemyController ResolveDesktopBasicTarget(', 'internal void MobilityBuff(', 'internal int NewCastId()','private Vector3 ResolveMobileAim(','internal void PrepareMobileSkillAim(','internal void ResolveMobileSkillAim(','private static bool ValidAimTarget(','private static bool ProjectedBounds(','private void FaceAim(','private EnemyController MagicConeTarget(','private void BasicAttack(','private Vector3 BasicAttackMuzzle(','internal bool CastImmediateSkill(','internal bool ConfirmTargetedSkill(','internal bool ExecuteChargedSkill(','private bool CanUseMovementSkill(','internal bool SkillTargetingReady(','internal bool CanBeginSkillTargeting(','internal Vector3 ResolveSkillGroundTarget(','private void CastSkill(','private void CastSkillCore(']
 (p/'PlayerMethods.cs').write_text('using System.Collections.Generic;using UnityEngine;namespace Emberfall{public sealed partial class PlayerController{'+''.join(member('Combat/PlayerController.cs',s) for s in signatures)+'}}')
 controls=['private bool IsMovementStart(', 'public static bool IsScreenPointOverControls(', 'public bool ProcessPointer(','public static void ResetInput()','private void Update()']
 (p/'PointerMethods.cs').write_text('using System.Collections.Generic;using UnityEngine;namespace Emberfall{public sealed partial class MobileControls{'+''.join(member('UI/MobileControls.cs',s) for s in controls)+'}}')
 target=['public enum Shape','public struct Preview','public static Preview Describe(','public static bool RequiresConfirmation(','public bool Begin(','public void Cancel()']
 (p/'TargetMethods.cs').write_text('using UnityEngine;namespace Emberfall{public partial class SkillTargetingController{'+''.join(member('Combat/SkillTargetingController.cs',s) for s in target)+'}}')
 session=[member('Core/GameSession.cs','public bool InputBlocked')]+[member('Core/GameSession.DungeonRewards.cs',s) for s in ['public bool CombatEnded','public bool CombatEffectsEnded']]+[member('Core/GameSession.Expedition.cs',s) for s in ['public bool FinishedResultReady','public bool FinishedResultDismissed','public void DismissFinishedResult()']]
 (p/'SessionMethods.cs').write_text('namespace Emberfall{public sealed partial class GameSession{'+''.join(session)+'}}')
 (p/'Test.cs').write_text((r/'Tests/PostVictoryActionProductionTests.cs').read_text())
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414;0169</NoWarn></PropertyGroup></Project>');include_cast_receipt_source(project)
 cfg=p/'NuGet.Config';cfg.write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
 build=[dotnet,'build',str(project),'--configfile',str(cfg),'-v:q'];run=[dotnet,str(p/'bin/Debug/net8.0/Test.dll')]
 subprocess.run(build,env=env,check=True);subprocess.run(run,env=env,check=True)
 mutations=[
  ('AdventureResultPolicy.cs','return started&&(!terminal||dungeon&&cleared&&!practice&&dismissed);','return started&&!terminal;','dismissed success opens player effects while enemy combat stays terminal'),
  ('PlayerMethods.cs','if(IsDead||session.CombatEffectsEnded)return;\n            if (HeroClass == HeroClass.Summoner)','if(IsDead||session.CombatEnded)return;\n            if (HeroClass == HeroClass.Summoner)','post-result paid cast reaches actual skill emitter'),
  ('SessionMethods.cs','Player.RetireCastsForResultDismissal();','','dismiss result retires old producer epoch once without cooldown reset'),
  ('AdventureResultPolicy.cs','return started&&(!terminal||dungeon&&cleared&&!practice&&dismissed);','return started&&(!terminal||dungeon&&cleared&&!practice);','result page blocks cast before dismissal and spends no budget')
 ]
 for name,before,after,oracle in mutations:
  file=p/name;good=file.read_text();assert good.count(before)==1,(name,before);file.write_text(good.replace(before,after))
  subprocess.run(build+['--no-restore'],env=env,check=True,stdout=subprocess.DEVNULL)
  result=subprocess.run(run,env=env,text=True,capture_output=True);file.write_text(good)
  assert result.returncode and 'System.Exception: '+oracle in result.stdout+result.stderr,result.stdout+result.stderr
  print('PASS compiled post-victory negative: '+oracle,flush=True)
 print('PASS: 4 compiled post-victory policy/cast/epoch mutations rejected',flush=True)

#!/usr/bin/env python3
"""Run real unpaid preparation, input wrappers, cast transaction and SkillRuntime.

The downstream damage recipient is an explicit recording boundary. Character
transforms and effect sockets are checked separately by the Unity preview.
"""
from pathlib import Path
import os
import subprocess
import sys
import tempfile

root = Path(__file__).resolve().parents[1]
dotnet = sys.argv[1] if len(sys.argv) > 1 else os.environ.get('DOTNET', 'dotnet')


def member(path, signature):
    text = (root / 'Assets/Scripts' / path).read_text()
    start = text.index(signature)
    cursor = text.index('{', start) + 1
    depth = 1
    while depth:
        depth += (text[cursor] == '{') - (text[cursor] == '}')
        cursor += 1
    return text[start:cursor]


shell = (root / 'Tests/MobilePinnedTargetProductionTests.cs').read_text().split('public static class MobilePinnedTargetProductionTests')[0]
shell = shell.replace('private bool ConcentratedVenom=>false;', 'public Action OnEmission;public int DamageEvents,LastSlot;public int CastId=>nextCastId;private bool ConcentratedVenom=>false;')
shell = shell.replace('public static float BlockedZ=float.NaN;', 'public static float GroundY=float.NaN,BlockedZ=float.NaN;').replace('=>Visible?b:a;', '=>Visible?new Vector3(b.x,float.IsNaN(GroundY)?b.y:GroundY,b.z):a;')
shell = shell.replace('public void AnimateCharge(float p,int slot){}public void CancelAction(){}', 'public int Prepares,Cancels,Actions,Beats;public float Progress;public void AnimateCharge(float p,int slot){Prepares++;Progress=p;}public void CancelAction(){Cancels++;}')
shell = shell.replace('public void SkillPerformanceBeat(int skill,int castId){}public void ReleaseCharge(int skill){}public void PlayAction(int s,bool a,float c=0){}', 'public void SkillPerformanceBeat(int skill,int castId){Beats++;}public void ReleaseCharge(int skill){Actions++;}public void PlayAction(int s,bool a,float c=0){Actions++;}')
shell = shell.replace('public void AdvanceSkillReleaseForFixture(float dt)=>AdvanceSkillRelease(dt);', 'public float PoseProgress=>model.Progress;public int PrepareCalls=>model.Prepares;public int ActionCalls=>model.Actions;public int PoseBeats=>model.Beats;public int CancelCalls=>model.Cancels;public void DisableForFixture(){gameObject.activeInHierarchy=false;OnDisable();}public void AdvanceSkillReleaseForFixture(float dt)=>AdvanceSkillRelease(dt);')
shell += '\nnamespace Emberfall{internal static class ReturningCounterRules{internal static string Predict(UnityEngine.Vector3 a,UnityEngine.Vector3 b,bool boss,float footprint,out UnityEngine.Vector3 landing){throw new System.Exception("Counter outside release gate fixture");}internal static UnityEngine.Vector3 Advance(UnityEngine.Vector3 a,UnityEngine.Vector3 b,UnityEngine.Vector3 c){throw new System.Exception("Counter outside release gate fixture");}}}\n'

methods = [
    'internal bool IsMobileSelfCenteredSkill(', 'private EnemyController ResolveDesktopBasicTarget(',
    'internal void MobilityBuff(', 'internal int NewCastId()', 'private Vector3 ResolveMobileAim(',
    'internal void PrepareMobileSkillAim(', 'internal void ResolveMobileSkillAim(',
    'private static bool ValidAimTarget(', 'private static bool ProjectedBounds(',
    'private void FaceAim(', 'private EnemyController MagicConeTarget(', 'private void BasicAttack(',
    'internal bool CastImmediateSkill(', 'internal bool ConfirmTargetedSkill(',
    'internal bool ExecuteChargedSkill(', 'private bool CanUseMovementSkill(',
    'internal bool SkillTargetingReady(', 'internal bool CanBeginSkillTargeting(',
    'internal Vector3 ResolveSkillGroundTarget(', 'private void CastSkill(',
]
core = member('Combat/PlayerController.cs', 'private void CastSkillCore(')
core = core[:core.index('            if (HeroClass == HeroClass.Summoner)')] + '''
            RecordEmission(target,executingChargedSkill?charge.TargetEnemy:AimTarget);
            LastSlot=slot;DamageEvents++;
            if(OnEmission!=null)OnEmission();
            }
            finally { CombatImpactBatch.EndAction(); }
        }
'''

driver = r'''
using System;using Emberfall;using UnityEngine;
static class PreparationTests{
 static int checks;
 static void Check(bool value,string reason){checks++;if(!value)throw new Exception(reason);}
 static bool Near(float a,float b)=>Math.Abs(a-b)<.0001f;
 static PlayerController Fresh(HeroClass hero=HeroClass.Vanguard){Time.frameCount++;CombatSight.Visible=true;CombatSight.GroundY=float.NaN;CombatSight.BlockedZ=float.NaN;MobileControls.Active=false;var s=new GameSession();var h=new PlayerController(s){HeroClass=hero};h.skillRuntime=new SkillRuntime(hero);h.aimPoint=new Vector3(3,0,6);s.Enemies.Add(new EnemyController(6));return h;}
 static bool Input(PlayerController h,int skill,Vector3 point)=>SkillTargetingController.RequiresConfirmation(h.HeroClass,skill)?h.ConfirmTargetedSkill(skill,point):h.CastImmediateSkill(skill);
 static void Tick(PlayerController h,float dt){Time.frameCount++;h.AdvanceSkillReleaseForFixture(dt);}
 static void Unpaid(PlayerController h,float energy,string reason){Check(h.CastId==0&&h.Casts==0&&h.DamageEvents==0&&Near(h.Energy,energy)&&h.ActionCalls==0&&h.PoseBeats==0,reason);}
 static void Main(){
  foreach(var hero in new[]{HeroClass.Vanguard,HeroClass.Arcanist,HeroClass.Ranger,HeroClass.Summoner})foreach(int fps in new[]{30,60,120})foreach(int rank in new[]{1,2,3}){
   var h=Fresh(hero);int slot=hero==HeroClass.Ranger||hero==HeroClass.Summoner?1:0;h.session.Progression.Profile.skillRanks[slot]=rank;Vector3 at=h.aimPoint;float energy=h.Energy,cost=GameBalance.SkillEnergyCost(hero,slot);
   Check(Input(h,slot,at)&&h.HasPendingSkillRelease,"normal input accepts unpaid preparation");Unpaid(h,energy,"initial preparation cannot pay, damage or emit");Check(h.PrepareCalls==1&&Near(h.PoseProgress,0),"actual input immediately poses preparation");
   Check(!Input(h,slot,at)&&!Input(h,2,new Vector3(0,0,2)),"spam and different slots cannot replace pending input");
   float age=0;while(age+1f/fps<.14f){Tick(h,1f/fps);age+=1f/fps;Unpaid(h,energy,"all pre-boundary frames remain unpaid");Check(h.PoseProgress>0&&h.PoseProgress<1,"progress drives preparation before release");}
   Tick(h,1f/fps);age+=1f/fps;Check(age>=.14f&&age<.14f+1f/fps+.0001f,"release quantizes to first frame after the physical preparation");
   Check(!h.HasPendingSkillRelease&&h.CastId==1&&h.Casts==1&&h.DamageEvents==1&&h.LastSlot==slot,"one receipt, one emission and one damage endpoint at release");
   Check(Near(h.Energy,energy-cost)&&Near(h.skillRuntime.Remaining(slot),GameBalance.EffectiveCooldown(hero,slot,rank)),"production runtime commits original energy and cooldown once");Check(h.ActionCalls==1&&h.PoseBeats==1,"actual paid transaction poses release before emission");
   Check(!Input(h,2,new Vector3(1,0,2)),"completion frame rejects another cast");Tick(h,5);Check(h.Casts==1&&h.DamageEvents==1&&Near(h.Energy,energy-cost),"later updates cannot duplicate an already consumed preparation");
  }
  {
   var h=Fresh(HeroClass.Ranger);Vector3 point=new Vector3(4,1.6f,7);CombatSight.GroundY=point.y;Check(h.ConfirmTargetedSkill(1,point),"targeted input accepted before receipt");float energy=h.Energy;
   Tick(h,.04f);h.aimPoint=new Vector3(-8,0,-5);h.AimTarget=h.session.Enemies[0];h.transform.forward=new Vector3(-1,0,0);h.session.InputBlocked=true;float progress=h.PoseProgress;Tick(h,9);Check(h.HasPendingSkillRelease&&Near(h.PoseProgress,progress),"pause freezes unpaid preparation clock and pose");Unpaid(h,energy,"paused preparation remains unpaid");
   h.session.InputBlocked=false;Tick(h,.1f);Check(h.Casts==1&&Near(h.LastCast.x,point.x)&&Near(h.LastCast.y,point.y)&&Near(h.LastCast.z,point.z)&&h.LastCastEnemy==null,"normal targeted release retains point and empty target snapshot");
  }
  {
   var h=Fresh();var enemy=h.session.Enemies[0];h.AimTarget=enemy;Check(h.CastImmediateSkill(0),"directional cast begins with selected identity");h.AimTarget=null;Tick(h,.14f);Check(h.LastCastEnemy==enemy,"immediate release retains selected identity snapshot");
   h=Fresh();h.AimTarget=h.session.Enemies[0];Check(h.CastImmediateSkill(0),"dead-target case begins");h.AimTarget.IsDead=true;Tick(h,.14f);Check(h.Casts==1&&h.LastCastEnemy==null,"dead captured identity is removed without changing cast point");
  }
  foreach(string exit in new[]{"cancel","epoch","death","class","session","owner","ended","unstarted","inactive","disable"}){
   var h=Fresh();float energy=h.Energy;Check(h.CastImmediateSkill(0),"cancellation case begins unpaid");Tick(h,.03f);
   if(exit=="cancel")h.CancelCombatPose();else if(exit=="epoch")h.CombatEpoch++;else if(exit=="death")h.IsDead=true;else if(exit=="class")h.HeroClass=HeroClass.Summoner;else if(exit=="session")h.session=new GameSession{Player=h};else if(exit=="owner")h.session.Player=null;else if(exit=="ended")h.session.CombatEffectsEnded=true;else if(exit=="unstarted")h.session.HasStarted=false;else if(exit=="inactive")h.gameObject.activeInHierarchy=false;else h.DisableForFixture();
   Tick(h,.2f);Check(!h.HasPendingSkillRelease,"invalid owner/session/epoch/lifecycle cancels pending preparation "+exit);Unpaid(h,energy,"cancelled input cannot pay or emit "+exit);
  }
  foreach(string invalid in new[]{"rank","energy","cooldown","stockpersist","stocktarget"}){
   var h=Fresh(invalid.StartsWith("stock")?HeroClass.Arcanist:HeroClass.Vanguard);int slot=invalid.StartsWith("stock")?4:invalid=="energy"?1:0;float energy=h.Energy;Check(h.CastImmediateSkill(slot),"late transaction case begins unpaid");
   if(invalid=="rank")h.session.Progression.Profile.skillRanks[slot]=0;else if(invalid=="energy"){foreach(int s in new[]{0,2,4,5,6,7})h.skillRuntime.TryConsume(s,1);Check(h.Energy<GameBalance.SkillEnergyCost(h.HeroClass,slot)&&h.skillRuntime.Remaining(slot)==0,"energy-only failure is below price with original slot still off cooldown");}else if(invalid=="cooldown")h.skillRuntime.TryConsume(slot,1);else if(invalid=="stockpersist")h.skillRuntime.CommitStock=(c,t,d)=>false;else h.session.Enemies.Clear();
   energy=h.Energy;Tick(h,.14f);Check(!h.HasPendingSkillRelease&&h.CastId==0&&h.Casts==0&&h.DamageEvents==0&&Near(h.Energy,energy)&&h.ActionCalls==0,"real transaction revalidates "+invalid+" without emission or another payment");
   if(invalid=="stockpersist")Check(h.skillRuntime.Charges(slot)==2&&h.skillRuntime.StockPersistenceFailed,"actual persistence failure keeps both stored uses");
  }
  {
   var h=Fresh();float energy=h.Energy;Check(h.CastImmediateSkill(0),"nonfinite clock case begins");foreach(float dt in new[]{0f,-1f,float.NaN,float.PositiveInfinity})Tick(h,dt);Unpaid(h,energy,"invalid deltas cannot advance or pay");Tick(h,2);Check(h.Casts==1&&h.CastId==1,"stalled frame commits one release");
   h=Fresh();energy=h.Energy;Check(h.CastImmediateSkill(0),"reentrancy case begins");int callbacks=0;h.skillRuntime.EnergyChanged=delta=>{callbacks++;Check(!h.CastImmediateSkill(2),"resource callback cannot initiate a second cast before receipt");h.AdvanceSkillReleaseForFixture(2);};h.OnEmission=()=>{Check(!h.CastImmediateSkill(2),"damage callback cannot initiate a second cast");h.AdvanceSkillReleaseForFixture(2);};Tick(h,.14f);Check(callbacks==1&&h.CastId==1&&h.Casts==1&&h.DamageEvents==1&&Near(h.Energy,energy-GameBalance.SkillEnergyCost(h.HeroClass,0)),"production resource and damage reentry cannot claim paid input twice");
  }
  foreach(var hero in new[]{HeroClass.Vanguard,HeroClass.Arcanist,HeroClass.Ranger,HeroClass.Summoner}){
   var h=Fresh(hero);float duration=SkillChargeController.Duration(hero,9),energy=h.Energy;Check(Input(h,9,h.aimPoint)&&h.charge.IsCharging&&!h.HasPendingSkillRelease,"existing charged casts do not acquire a second preparation gate");h.AdvanceCharge(duration-.01f);Unpaid(h,energy,"charged cast remains unpaid before its existing boundary");h.AdvanceCharge(.011f);Check(h.CastId==1&&h.Casts==1&&!h.charge.IsCharging&&!h.HasPendingSkillRelease,"charged cast releases once at original duration");Check(Near(h.Energy,energy-GameBalance.SkillEnergyCost(hero,9)),"charged cast uses actual resource transaction");
  }
  Console.WriteLine("PASS "+checks+" preparation/release checks (production input, gate, paid Core prefix and SkillRuntime; downstream recipient/model recorders)");
 }
}
'''

with tempfile.TemporaryDirectory(prefix='emberfall-skill-release-') as directory:
    work = Path(directory)
    for path in ['Combat/PlayerController.SkillRelease.cs', 'Combat/PlayerController.SkillAvailability.cs',
                 'Core/CombatImpactBatch.cs', 'Core/SkillRuntime.cs', 'Core/GameTypes.cs',
                 'Core/CombatBalance.cs', 'Core/MobileCameraGesture.cs', 'Core/SkillDamageBudgets.cs',
                 'Combat/MobileSkillPolicy.cs', 'Combat/PlayerController.MobileFocus.cs',
                 'Combat/SkillChargeController.cs', 'UI/MobileControlLayout.cs', 'Core/CombatOpportunityState.cs',
                 'UI/MobileControls.OpportunityInput.cs', 'Core/CastFirstHitReceipt.cs',
                 'Combat/PlayerController.CastReceipts.cs']:
        (work / Path(path).name).write_text((root / 'Assets/Scripts' / path).read_text())
    from MobileBindingFixtureSources import include_mobile_binding_sources
    include_mobile_binding_sources(work, root, True)
    (work / 'Fixture.cs').write_text(shell)
    (work / 'PlayerMethods.cs').write_text('using System.Collections.Generic;using UnityEngine;namespace Emberfall{public sealed partial class PlayerController{' + ''.join(member('Combat/PlayerController.cs', x) for x in methods) + core + '}}')
    (work / 'PointerMethods.cs').write_text('using System.Collections.Generic;using UnityEngine;namespace Emberfall{public sealed partial class MobileControls{' + ''.join(member('UI/MobileControls.cs', x) for x in ['private bool IsMovementStart(', 'public static bool IsScreenPointOverControls(', 'public bool ProcessPointer(', 'public static void ResetInput()', 'private void Update()']) + '}}')
    (work / 'TargetMethods.cs').write_text('using UnityEngine;namespace Emberfall{public partial class SkillTargetingController{' + ''.join(member('Combat/SkillTargetingController.cs', x) for x in ['public enum Shape', 'public struct Preview', 'public static Preview Describe(', 'public static bool RequiresConfirmation(', 'public bool Begin(', 'public void Cancel()']) + '}}')
    (work / 'Program.cs').write_text(driver)
    (work / 'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414;0169</NoWarn></PropertyGroup></Project>')
    (work / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
    env = dict(os.environ, DOTNET_CLI_HOME=str(work / 'cli'), DOTNET_NOLOGO='1', DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_GENERATE_ASPNET_CERTIFICATE='false', DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE='true')
    build = [dotnet, 'build', str(work / 'Test.csproj'), '--configfile', str(work / 'NuGet.Config'), '-v:q']
    run = [dotnet, str(work / 'bin/Debug/net8.0/Test.dll')]
    subprocess.run(build, env=env, check=True)
    subprocess.run(run, env=env, check=True)
    mutations = [
        ('PlayerMethods.cs', 'if (!executingChargedSkill && !executingPreparedSkill) { BeginSkillRelease(slot); return; }', 'if (false) { BeginSkillRelease(slot); return; }', 'normal input accepts unpaid preparation'),
        ('PlayerController.SkillRelease.cs', 'if (pendingSkillReleaseAge < SkillPreparationSeconds) return;', 'if (pendingSkillReleaseAge < 0) return;', 'all pre-boundary frames remain unpaid'),
        ('PlayerController.SkillRelease.cs', 'if (session.InputBlocked || deltaTime <= 0', 'if (deltaTime <= 0', 'pause freezes unpaid preparation clock and pose'),
        ('PlayerController.SkillRelease.cs', 'Vector3 snapshotAim = pendingSkillReleaseAim', 'Vector3 snapshotAim = aimPoint', 'normal targeted release retains point and empty target snapshot'),
        ('PlayerController.SkillRelease.cs', 'CombatEpoch != pendingSkillReleaseEpoch ||', '', 'cancelled input cannot pay or emit epoch'),
    ]
    for filename, before, after, oracle in mutations:
        file = work / filename
        good = file.read_text()
        assert before in good
        file.write_text(good.replace(before, after))
        subprocess.run(build, env=env, check=True, stdout=subprocess.DEVNULL)
        failed = subprocess.run(run, env=env, text=True, capture_output=True)
        file.write_text(good)
        assert failed.returncode and 'System.Exception: ' + oracle in failed.stdout + failed.stderr, failed.stdout + failed.stderr
        print('PASS compiled negative control:', oracle)

# Complete Update orchestration wiring is source evidence, separate from the
# executed gate/transaction above and native Transform validation.
update = member('Combat/PlayerController.cs', 'private void Update()')
assert update.index('AdvanceSkillRelease(0)') < update.index('bool advancePendingSkillRelease = HasPendingSkillRelease;') < update.index('skillRuntime.Advance(dt)')
assert update.index('skillRuntime.Advance(dt)') < update.index('WorldTraversal.MovePlayer(') < update.index('if (advancePendingSkillRelease) AdvanceSkillRelease(dt);')
assert update.index('bool advancePendingSkillRelease = HasPendingSkillRelease;') < update.index('targeting.Begin(skill)') < update.index('if (advancePendingSkillRelease) AdvanceSkillRelease(dt);')
assert update.index('if (advancePendingSkillRelease) AdvanceSkillRelease(dt);') < update.index('!SkillReleaseInputBlocked && wantsBasic') < update.rindex('model.Animate(')
print('PASS source wiring: old timers/current movement precede paid release; fresh inputs retain full preparation; completion blocks basic. Source-order evidence only.')

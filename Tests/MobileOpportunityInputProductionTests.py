from MobileBindingFixtureSources import include_mobile_binding_sources
"""Actual pointer consumption and control hit test for visible opportunity overlays.
Only engine, optional mechanics and final emission recipients are managed boundaries;
readiness, cost/cooldown commitment and final emitted intent execute production code.
"""
from pathlib import Path
import os,sys,tempfile,subprocess
r=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
def extract(path,signature):
 s=(r/'Assets/Scripts'/path).read_text();a=s.index(signature);b=s.index('{',a)+1;d=1
 while d:d+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
with tempfile.TemporaryDirectory(prefix='opportunity-input-') as t:
 p=Path(t)
 for path in ['Combat/PlayerController.SkillRelease.cs','Combat/PlayerController.SkillAvailability.cs','Core/CombatImpactBatch.cs','Core/SkillRuntime.cs','Core/GameTypes.cs','Core/CombatBalance.cs','Core/MobileCameraGesture.cs','Core/SkillDamageBudgets.cs','Core/HudLogicalScale.cs','Combat/MobileSkillPolicy.cs','Combat/PlayerController.MobileFocus.cs','Combat/SkillChargeController.cs','UI/MobileControlLayout.cs','Core/CombatOpportunityState.cs','UI/MobileControls.OpportunityInput.cs','Core/CastFirstHitReceipt.cs','Combat/PlayerController.CastReceipts.cs']:(p/Path(path).name).write_text((r/'Assets/Scripts'/path).read_text())
 include_mobile_binding_sources(p,r,True)
 fixture=(r/'Tests/MobilePinnedTargetProductionTests.cs').read_text()
 fixture=fixture.replace('public void SetTarget(Vector3 p){TargetPoint=p;}public bool Confirm()=>true;', 'public int AimWrites,Confirms;public void SetTarget(Vector3 p){AimWrites++;TargetPoint=p;}public bool Confirm(){Confirms++;return true;}public void BeginAimForTest(){skill=1;TargetPoint=Vector3.one;}')
 # Execute the production GUI layout, screen conversion and skill capture path.
 # Only persistence, lifecycle admission and unused desktop drag recipients remain boundaries.
 fixture=fixture.replace('public bool HasStarted=true,InputBlocked,CombatEnded,CombatEffectsEnded;', 'public bool HasStarted=true,InputBlocked,CombatEnded,CombatEffectsEnded,Paused,IsDead;')
 fixture=fixture.replace('public void ReportControlFailure(string key,string s){Failure=key+":"+s;}', 'public string ControlFailure(string key)=>Failure??"";public void ReportControlFailure(string key,string s){Failure=key+":"+s;}')
 fixture=fixture.replace('public bool TryBeginTouchSkill(int f,Vector2 p)=>p.x>500&&p.y>250;public void UpdateTouchSkill(int f,Vector2 p,bool e,bool c){}','')
 fixture=fixture.replace('public void CancelMobileCast(){}','')
 fixture=fixture.replace('public void ActivateFreeCommand(bool b){}','public int CommandActivations;public bool LastCommandRecall;public void ActivateFreeCommand(bool b){CommandActivations++;LastCommandRecall=b;}')
 # Compile the real mobile Layout getter, including render/native DPI conversion.
 fixture=fixture.replace('public static float width=568,height=320;', 'public static float width=568,height=320,dpi=163;')
 fixture=fixture.replace('public static class Screen{', 'public sealed class Display{public static Display main=new Display();public float systemWidth=568,systemHeight=320;}public static class Screen{')
 fixture=fixture.replace('public struct Vector3{', 'public struct Vector3{public static bool operator==(Vector3 a,Vector3 b)=>a.x==b.x&&a.y==b.y&&a.z==b.z;public static bool operator!=(Vector3 a,Vector3 b)=>!(a==b);public override bool Equals(object value)=>value is Vector3 v&&this==v;public override int GetHashCode()=>0;')
 old='public static MobileControlLayout Layout=new MobileControlLayout(568,320,160);'
 assert old in fixture
 fixture=fixture.replace(old,'private static MobileControlLayout cachedLayout;private static Vector3 cachedLayoutInputs;private static int cachedPosition;private static bool cachedIPad;public static bool IsIPad;'+extract('UI/MobileControls.cs','public static MobileControlLayout Layout'))
 fixture=fixture.replace('namespace Emberfall\n{','namespace Emberfall\n{public static class EffectPreferences{public static int TouchPosition;}')
 (p/'Fixture.cs').write_text(fixture)
 mobile_members=['private float TouchRatio','private Rect TouchRect(MobileControlLayout.Area','private int mobileCastFinger','public void CancelMobileCast(', 'private bool BeginMobileCast(', 'private void ContinueMobileCast(']
 gui_members=['private Vector2 ScreenToUI(', 'private void RefreshLayout(', 'public bool TryBeginTouchSkill(', 'public void UpdateTouchSkill(']
 boundary='enum Panel{None,Inventory}Panel panel;GameSession session;float scale,width,height;Vector2 guiOffset;Rect hotbarBounds;Rect[] hotbarSlots=new Rect[10],detailSlots=new Rect[10];int mobileSkillPageFrame=-1,hotbarPointerSlot=-1,hotbarTouchFinger=-1000;Vector2 hotbarTouchPosition;bool hotbarPointerConfiguring;readonly MobileSkillTap mobileTap=new MobileSkillTap();void ObserveTouchViewport(Rect area){}void BeginHotbarPointer(int index,Vector2 point,bool configuring){throw new System.Exception("unexpected desktop drag admission");}void ContinueHotbarPointer(Vector2 point){throw new System.Exception("unexpected desktop drag continuation");}void CancelHotbarPointer(){throw new System.Exception("unexpected desktop drag cancellation");}void CompleteHotbarPointer(int target){throw new System.Exception("unexpected desktop drag completion");}public void BindInputSessionForFixture(GameSession value){session=value;RefreshLayout();}public int CapturedSkillForFixture=>mobileTap.Skill;public int BoundForInputFixture(int button)=>BoundMobileSkill(button,mobileSkillPage);'
 (p/'GuiInputMethods.cs').write_text('using UnityEngine;namespace Emberfall{public partial class GameUI{'+boundary+''.join(extract('UI/GameUI.Mobile.cs',x) for x in mobile_members)+''.join(extract('UI/GameUI.cs',x) for x in gui_members)+'}}')
 (p/'OpportunityProbe.cs').write_text((r/'Tests/MobileOpportunityInputProductionTests.cs').read_text())
 player=['internal bool IsMobileSelfCenteredSkill(', 'private EnemyController ResolveDesktopBasicTarget(', 'internal void MobilityBuff(', 'internal int NewCastId()','private Vector3 ResolveMobileAim(','internal void PrepareMobileSkillAim(','internal void ResolveMobileSkillAim(','private static bool ValidAimTarget(','private static bool ProjectedBounds(','private void FaceAim(','private EnemyController MagicConeTarget(','private void BasicAttack(','internal bool CastImmediateSkill(','internal bool ConfirmTargetedSkill(','internal bool ExecuteChargedSkill(','private bool CanUseMovementSkill(','internal bool SkillTargetingReady(','internal bool CanBeginSkillTargeting(','internal Vector3 ResolveSkillGroundTarget(','private void CastSkill(','private void CastSkillCore(']
 controls=['public static bool ConsumeJump()', 'public static bool ConsumePotion()', 'private bool IsMovementStart(', 'public static bool IsScreenPointOverControls(', 'public bool ProcessPointer(','public static void ResetInput()','private void Update()']
 target=['public enum Shape','public struct Preview','public static Preview Describe(','public static bool RequiresConfirmation(','public bool Begin(','public void Cancel()']
 (p/'PlayerMethods.cs').write_text('using System.Collections.Generic;using UnityEngine;namespace Emberfall{public sealed partial class PlayerController{'+''.join(extract('Combat/PlayerController.cs',x) for x in player)+'}}')
 (p/'PointerMethods.cs').write_text('using System.Collections.Generic;using UnityEngine;namespace Emberfall{public sealed partial class MobileControls{'+''.join(extract('UI/MobileControls.cs',x) for x in controls)+'}}')
 (p/'TargetMethods.cs').write_text('using UnityEngine;namespace Emberfall{public partial class SkillTargetingController{'+''.join(extract('Combat/SkillTargetingController.cs',x) for x in target)+'}}')
 (p/'Program.cs').write_text('System.Console.WriteLine(MobileOpportunityInputProductionTests.Run());')
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit><DefineConstants>UNITY_IOS</DefineConstants><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');build=[dotnet,'build',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'];run=[dotnet,str(p/'bin/Debug/net8.0/Test.dll')]
 subprocess.run(build,env=env,check=True);subprocess.run(run,env=env,check=True)
 for before,after,oracle in [
  ('else if (IsOpportunityPoint(point)) role = Role.Consumed;','', 'visible hint never changes pin or combat state'),
  ('instance.IsOpportunityPoint(point) || ','','visible hint is reported as UI control')]:
  path=p/'PointerMethods.cs';good=path.read_text();assert before in good;path.write_text(good.replace(before,after))
  subprocess.run(build,env=env,check=True)
  result=subprocess.run(run,env=env,capture_output=True,text=True);print(result.stdout+result.stderr)
  assert result.returncode and 'System.Exception: '+oracle in result.stdout+result.stderr,result.stdout+result.stderr
  print('PASS compiled negative:',oracle);path.write_text(good)
 # Prove the actual getter cannot regress to native DPI after render downscaling.
 path=p/'Fixture.cs';good=path.read_text()
 before='dpi=MobileControlLayout.RenderDpi(dpi,Screen.width,Screen.height,Display.main.systemWidth,Display.main.systemHeight);'
 assert before in good;path.write_text(good.replace(before,''))
 subprocess.run(build,env=env,check=True)
 result=subprocess.run(run,env=env,capture_output=True,text=True);print(result.stdout+result.stderr)
 oracle='render-grid DPI governs the actual Layout getter'
 assert result.returncode and 'System.Exception: '+oracle in result.stdout+result.stderr,result.stdout+result.stderr
 print('PASS compiled negative:',oracle);path.write_text(good)
 # Reintroduce the former Pad command overlap and require the real corner input to fail.
 path=p/'MobileControlLayout.cs';good=path.read_text()
 before='float commandX=ipad?Potion.X+Potion.Width+8:204;'
 assert before in good;path.write_text(good.replace(before,'float commandX=204;'))
 subprocess.run(build,env=env,check=True)
 result=subprocess.run(run,env=env,capture_output=True,text=True);print(result.stdout+result.stderr)
 oracle='actual iPad potion input never activates recall'
 assert result.returncode and 'System.Exception: '+oracle in result.stdout+result.stderr,result.stdout+result.stderr
 print('PASS compiled negative:',oracle);path.write_text(good)
 # iOS retains its original Pad vertical alignment, so its former jump/arc
 # collision has one platform-specific production fix and negative oracle.
 before='Jump=new Area(Jump.X,Jump.Y+8,Jump.Width,Jump.Height);Cancel=Jump;'
 if before in good:
  path.write_text(good.replace(before,''))
  subprocess.run(build,env=env,check=True)
  result=subprocess.run(run,env=env,capture_output=True,text=True);print(result.stdout+result.stderr)
  oracle='actual iPad jump corners never capture adjacent skill'
  assert result.returncode and 'System.Exception: '+oracle in result.stdout+result.stderr,result.stdout+result.stderr
  print('PASS compiled negative:',oracle);path.write_text(good)

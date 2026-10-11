#!/usr/bin/env python3
"""Actual UI query + LatestCombatResult/BurnCashFeedback + result-channel receipt flow."""
import os,sys,tempfile,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
def member(s,k):
 a=s.index(k);b=s.index('{',a)+1;n=1
 while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
fixture=r'''
using System;
namespace UnityEngine{public static class Time{public static float time;}public static class Mathf{public static float Max(float a,float b)=>Math.Max(a,b);}}
namespace Emberfall{
 public enum HeroClass{Arcanist,Summoner}
 public class GameSession{public PlayerController Player;public bool HasStarted=true,InputBlocked;}
 public static class SummonedCompanion{public static bool EmpoweredHitFeedback(PlayerController owner,out int sequence,out int count,out float age){sequence=count=0;age=0;return false;}}
 public sealed partial class PlayerController{
 public bool IsDead;public HeroClass HeroClass;public int CombatEpoch=1;public object CurrentOpportunityTarget=new object();public GameSession session;
 private int burnFeedbackCast,burnFeedbackEpoch,burnFeedbackTargets;private float burnFeedbackAt;
 public void ActualReceipt(int cast){burnFeedbackCast=cast;burnFeedbackEpoch=CombatEpoch;burnFeedbackTargets=1;burnFeedbackAt=UnityEngine.Time.time;}
 }
 public sealed partial class GameUI{
 private GameSession session;private readonly CombatResultChannel resultChannel=new CombatResultChannel();private CombatOpportunityState lastVisibleResult;
 public GameUI(GameSession s){session=s;}public string Draw()=>CurrentCombatResult();
 }
}
class Program{
 static int n;static void C(bool b,string s){n++;if(!b)throw new Exception(s);}
 static void Main(){foreach(bool previouslyVisible in new[]{false,true}){
 var game=new Emberfall.GameSession();var hero=new Emberfall.PlayerController{session=game};game.Player=hero;var ui=new Emberfall.GameUI(game);
 if(previouslyVisible){hero.ActualReceipt(10);C(ui.Draw()!="","prior visible receipt");}
 hero.ActualReceipt(11); // Actual event, deliberately no UI draw before pause.
 game.InputBlocked=true;C(hero.LatestCombatResult().Kind==Emberfall.CombatOpportunityKind.None,"ordinary paused query stays hidden");C(hero.LatestCombatResult(includeBlocked:true).Receipt==11,"suppression query reads actual unseen receipt");C(ui.Draw()=="","paused result remains invisible");
 game.InputBlocked=false;C(ui.Draw()=="","unseen pre-pause receipt never replays on resume");C(ui.Draw()=="","suppression survives repeated render");hero.ActualReceipt(12);C(ui.Draw()!="","genuinely new receipt remains visible");
 hero.CurrentOpportunityTarget=new object();C(ui.Draw()==""&&ui.Draw()=="","same receipt cannot follow target switch");hero.ActualReceipt(13);C(ui.Draw()!="","new target actual receipt visible");UnityEngine.Time.time+=2.1f;C(ui.Draw()=="","real result expiry hides");}
 var channel=new Emberfall.CombatResultChannel();object owner=new object(),target=new object();
 var first=new Emberfall.CombatOpportunityState(Emberfall.CombatOpportunityKind.EmpoweredHit,2,1,receipt:1);
 C(channel.Observe(first,owner,1,target,false)!="","epoch1 first real receipt visible");C(channel.Observe(first,owner,1,target,true)=="","epoch1 pause suppresses receipt one");
 C(channel.Observe(default,owner,2,target,false)=="","epoch2 empty first frame");C(channel.Observe(first,owner,2,target,false)!="","epoch2 reused receipt one is a genuine visible new result");
 C(channel.Observe(first,owner,2,target,true)=="","epoch2 suppression set");owner=new object();C(channel.Observe(default,owner,2,target,false)=="","new character empty first frame");C(channel.Observe(first,owner,2,target,false)!="","new character reused receipt one is visible");
 C(channel.Observe(first,owner,3,target,false)==""&&channel.Observe(first,owner,3,target,false)=="","new epoch first frame carrying stale result remains suppressed");
 var second=new Emberfall.CombatOpportunityState(Emberfall.CombatOpportunityKind.EmpoweredHit,2,1,receipt:2);C(channel.Observe(second,owner,3,target,false)!="","later new epoch event survives carryover suppression");
 owner=new object();C(channel.Observe(second,owner,3,target,false)==""&&channel.Observe(second,owner,3,target,false)=="","new character carrying old result stays suppressed");
 Console.WriteLine("PASS "+n+" actual UI/runtime receipt pause-boundary assertions; managed only");}}
'''
ui=member((root/'Assets/Scripts/UI/GameUI.CombatOpportunities.cs').read_text(),'private string CurrentCombatResult(')
actor=member((root/'Assets/Scripts/Combat/PlayerController.Opportunities.cs').read_text(),'internal CombatOpportunityState LatestCombatResult(')+member((root/'Assets/Scripts/Combat/PlayerController.BurnFeedback.cs').read_text(),'internal bool BurnCashFeedback(')
with tempfile.TemporaryDirectory(prefix='pause-receipt-') as tmp:
 p=Path(tmp)
 for f in ['CombatOpportunityState','CombatResultChannel']:(p/(f+'.cs')).write_text((root/('Assets/Scripts/Core/'+f+'.cs')).read_text())
 (p/'Fixture.cs').write_text(fixture);(p/'Actor.cs').write_text('using UnityEngine;namespace Emberfall{public sealed partial class PlayerController{'+actor+'}}');view=p/'UI.cs';(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>');proj=p/'Test.csproj';proj.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>0169;0649</NoWarn></PropertyGroup></Project>');env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 channel=p/'CombatResultChannel.cs';channelOriginal=channel.read_text()
 for mode in ['current','old-last-drawn-receipt','old-cross-scope-suppression']:
  channel.write_text(channelOriginal.replace('if(scopeChanged){suppressedReceipt=0;suppressedKind=CombatOpportunityKind.None;}','') if mode=='old-cross-scope-suppression' else channelOriginal)
  code=ui if mode!='old-last-drawn-receipt' else ui.replace('var result=hero==null?default(CombatOpportunityState):hero.LatestCombatResult(includeBlocked:true);','var result=blocked?lastVisibleResult:hero.LatestCombatResult();if(!blocked)lastVisibleResult=result;')
  view.write_text('namespace Emberfall{public sealed partial class GameUI{'+code+'}}')
  q=subprocess.run([dotnet,'build',str(proj),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,capture_output=True,text=True);print(mode,'BUILD',q.stdout,q.stderr);assert q.returncode==0
  q=subprocess.run([dotnet,str(p/'bin/Debug/net8.0/Test.dll')],env=env,capture_output=True,text=True);print(mode,'RUN',q.stdout,q.stderr)
  if mode=='current':assert q.returncode==0
  else:
   oracle='unseen pre-pause receipt never replays on resume' if mode=='old-last-drawn-receipt' else 'epoch2 reused receipt one is a genuine visible new result'
   assert q.returncode!=0 and oracle in q.stderr;print('PASS compiled receipt negative control:',mode)

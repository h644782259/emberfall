using System;using System.Linq;using System.Reflection;using Emberfall;using UnityEngine;
class PostVictoryVisualProductionTests
{
 static int checks;static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
 static FilledSkillVfx Cast(PlayerController player)
 {
  FilledSkillVfx.Impact(player,Vector3.zero,3,FilledVfxKind.Ice,new Color(.3f,.8f,1));
  return GameObject.All.Where(o=>o.activeInHierarchy&&!o.Destroyed).Select(o=>o.GetComponent<FilledSkillVfx>()).Last(f=>f!=null);
 }
 static void Tick(FilledSkillVfx fx,float dt){Time.deltaTime=Time.unscaledDeltaTime=dt;fx.gameObject.Call("Update");}
 static void Main(string[] args)
 {
  Resources.Root=args[0];CombatSight.Wall=float.PositiveInfinity;
  foreach(bool special in new[]{true,false})
  {
   var player=new GameObject("new generation hero").AddComponent<PlayerController>();
   var game=new GameSession{Player=player,HasStarted=true,InDungeon=true,DungeonCleared=true,ModeFinished=special};GameSession.Instance=game;
   var old=Cast(player);game.FinishedResultDismissed=true;player.CombatEpoch++;Tick(old,.01f);Check(!old.gameObject.activeSelf,"old combat producer cannot revive after result closes");
   var fresh=Cast(player);Tick(fresh,.1f);Check(fresh.gameObject.activeSelf,"new post-result filled body survives its normal active lifetime");
   Check(GameObject.All.Any(o=>o.transform.parent==fresh.transform&&o.activeInHierarchy&&o.GetComponent<MeshFilter>()?.sharedMesh!=null),"post-result skill owns actual visible mesh bindings");
   fresh.CompleteAftermath();Tick(fresh,.04f);Check(fresh.gameObject.activeSelf,"new post-result filled aftermath is visible and decays normally");
   Tick(fresh,3);Check(!fresh.gameObject.activeSelf&&CombatVisualLease.Active==0,"post-result visual tail returns its mesh rental and lease");
   Check(game.CombatEnded&&!AdventureResultPolicy.AcceptsDamage(true,game.CombatEnded)&&!AdventureResultPolicy.AcceptsKill(true,game.CombatEnded,true),"visual lifetime never reopens enemy damage or reward admission");
   game.FinishedResultDismissed=false;var blocked=Cast(player);Tick(blocked,.01f);Check(!blocked.gameObject.activeSelf,"undismissed result retires unconfirmed cast body");
  }
  Console.WriteLine("PASS: "+checks+" actual post-result filled mesh/ownership/tail/lease assertions (managed rendering boundary)");
 }
}

using System;using System.Collections.Generic;using UnityEngine;using Emberfall;
public static class ChapterSealRouteProductionTests
{
 static int checks;static void Check(bool v,string why){checks++;if(!v)throw new Exception(why);}
 static ChapterCombatRun Forest(){var run=new ChapterCombatRun(ChapterNode.ForestCourt,ChapterDifficulty.Hard,17);run.BindRoom(7);for(int i=0;i<6;i++)Check(run.Register(0,7,i),"six actual slots register");return run;}
 static float Length(Vector3 from,Vector3 to){float sum=0;var p=from;foreach(var q in WorldTraversal.FindPath(from,to,.65f)){sum+=Vector3.Distance(p,q);p=q;}return sum;}
 public static string Run()
 {
  foreach(int first in new[]{0,1})
  {
   var rings=Forest();for(int i=0;i<6;i++){rings.AdvanceSeal(first,.25f,true,true,false);rings.AdvanceSeal(1-first,.25f,true,true,false);}
   var a=new ChapterSealPresentation(0,rings.SealProgress(0),first==0,false,false,false);
   var b=new ChapterSealPresentation(1,rings.SealProgress(1),first==1,true,false,false);
   Check(a.Seconds==1.5f&&b.Seconds==1.5f&&a.Label.Contains("A")&&b.Label.Contains("B"),"HUD keeps half-complete A and B independent");
   Check(a.Occupied==(first==0)&&b.Occupied==(first==1)&&b.Label.Contains("争夺"),"HUD highlights actual occupied ring and independent contest");
   for(int i=0;i<6;i++)rings.AdvanceSeal(first,.25f,true,true,false);
   var done=new ChapterSealPresentation(first,rings.SealProgress(first),false,true,rings.SealComplete(first),false);
   Check(done.Complete&&!done.Contested&&done.Label.Contains("完成")&&rings.SealProgress(1-first)==1.5f,"either completion order retains other half on return");
  }
  var state=Forest();state.AdvanceSeal(1,.25f,true,true,false);Check(state.SealProgress(1)==.25f&&state.SealProgress(0)==0,"either seal can be started first independently");
  state.AdvanceSeal(0,.25f,true,true,false);Check(state.SealProgress(0)==.25f&&state.SealProgress(1)==.25f,"partial progress remains on both simultaneous seals");
  state.AdvanceSeal(1,.25f,true,true,true);state.AdvanceSeal(0,.25f,false,true,false);Check(state.SealProgress(0)==.25f&&state.SealProgress(1)==.25f,"contest and pause preserve each local seal");
  for(int i=0;i<11;i++)state.AdvanceSeal(1,.25f,true,true,false);Check(state.SealComplete(1)&&state.Seals==1&&!state.DoorUnlocked,"second seal completes without forcing first seal order");
  for(int i=0;i<20;i++)state.AdvanceSeal(1,.25f,true,true,false);Check(state.Seals==1,"completed seal cannot increment aggregate twice");
  for(int i=0;i<11;i++)state.AdvanceSeal(0,.25f,true,true,false);Check(state.Seals==2&&state.DoorUnlocked&&state.SealProgress(0)==3&&state.SealProgress(1)==3,"two independent three-second seals unlock exit once");
  Check(state.Exit(true,false)&&state.RoomIndex==1&&!state.Finished,"forest keeps second escape room");state.BindRoom(8);Check(state.Seals==0&&state.SealProgress(0)==0&&state.SealProgress(1)==0,"new room clears prior seal state");
  state=Forest();state.Fail();state.AdvanceSeal(0,1,true,true,false);Check(state.SealProgress(0)==0,"terminal state rejects retained updates");state=Forest();foreach(float value in new[]{float.NaN,float.PositiveInfinity,-1,0})state.AdvanceSeal(0,value,true,true,false);state.AdvanceSeal(2,.25f,true,true,false);Check(state.Seals==0&&state.SealProgress(0)==0,"invalid deltas and seal indices do not advance");
  var star=new ChapterCombatRun(ChapterNode.StarPlatform,ChapterDifficulty.Normal,0);star.BindRoom(1);Check(star.Objective==RoomObjective.Boss&&star.EnemyCount==3,"star enters the one-room boss roster directly");for(int i=0;i<3;i++){star.Register(0,1,i);star.Defeat(0,1,i);}Check(star.FinishBoss()&&star.Finished,"star room-zero boss finalizes without a phantom second room");
  for(int seed=0;seed<8;seed++)
  {
   var plan=ChapterRoomGeometry.Plan(ChapterNode.ForestCourt,0,seed);WorldTraversal.Reset(ZoneKind.Dungeon);ChapterRoomGeometry.Register(plan);
   float left=Length(plan.Entrance,plan.Objectives[0]),right=Length(plan.Entrance,plan.Objectives[1]);Check(Math.Abs(left-right)<3,"both seal approach routes have comparable real routed lengths");
   Check(Vector3.Distance(plan.SpawnCandidates[0],plan.SpawnCandidates[1])<6&&WorldTraversal.HasLineOfSight(plan.SpawnCandidates[0],plan.SpawnCandidates[1]),"first seal guard really receives nearby supplier LOS");
   Check(Vector3.Distance(plan.SpawnCandidates[0],plan.SpawnCandidates[4])>6&&!WorldTraversal.HasLineOfSight(plan.SpawnCandidates[0],plan.SpawnCandidates[4]),"opposite guard has neither cross-island range nor LOS supply");
   plan=ChapterRoomGeometry.Plan(ChapterNode.Redrock,0,seed);WorldTraversal.Reset(ZoneKind.Dungeon);ChapterRoomGeometry.Register(plan);var clipped=ChapterHazardGeometry.ClipLine(plan.HazardStart,plan.HazardEnd);
   Check(plan.HazardStart.x*plan.Objectives[0].x>0&&plan.HazardEnd.x*plan.Objectives[0].x>0&&plan.HazardStart.z==-4,"heat hazard follows mirrored hunt-side short approach");
   Check(Vector3.Distance(plan.HazardStart,clipped)>1&&Vector3.Distance(plan.HazardStart,clipped)<Vector3.Distance(plan.HazardStart,plan.HazardEnd),"short-lane heat is nonempty and clips at its actual side wall");
   foreach(float radius in new[]{.45f,.65f,.9f,1.3f}){WorldTraversal.Reset(ZoneKind.Dungeon);ChapterRoomGeometry.Register(plan);WorldTraversal.AddBox((plan.HazardStart+clipped)*.5f,new Vector2(Math.Abs(clipped.x-plan.HazardStart.x)+1.1f,1.1f));Check(WorldTraversal.CanReach(plan.Entrance,plan.Objectives[0],radius)&&WorldTraversal.CanReach(plan.Objectives[0],plan.Exit,radius),"heat-covered shortcut leaves a real alternate route for every supported footprint");}
   Check(ChapterRoomGeometry.Plan(ChapterNode.StarPlatform,0,seed).Layout==105,"direct star boss reuses open boss arena layout");
  }
  return "PASS: "+checks+" actual independent seal state and routed chapter geometry assertions";
 }
}

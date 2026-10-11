using System;using Emberfall;
public static class HoldPointStateTests
{
 static int n;static void Check(bool ok,string reason){n++;if(!ok)throw new Exception(reason);}
 public static string Run()
 {
  float radius=ExpeditionModeState.HoldPointRadius;
  Check(ExpeditionModeState.InsideHoldPoint(radius*radius),"visible boundary includes player edge");
  Check(!ExpeditionModeState.InsideHoldPoint((radius+.001f)*(radius+.001f)),"outside circle cannot capture");
  foreach(float footprint in new[]{.45f,.6f,.9f,1.25f})
  {
   float edge=radius+footprint;
   Check(ExpeditionModeState.ContestsHoldPoint(edge*edge,footprint),"touching body footprint contests");
   Check(!ExpeditionModeState.ContestsHoldPoint((edge+.01f)*(edge+.01f),footprint),"knockback clears actual edge");
   Check(!ExpeditionModeState.ContestsHoldPoint(36,footprint),"old six meter radius no longer contests");
  }
  Check(!ExpeditionModeState.ContestsHoldPoint(float.NaN,.45f)&&!ExpeditionModeState.ContestsHoldPoint(1,float.PositiveInfinity),"invalid bounds fail safely");
  var state=new ExpeditionModeState(ExpeditionModeKind.HoldPoint,1,123);ExpeditionPhasePlan phase;state.TryBeginPhase(out phase);
  state.Advance(1,true,false,0);Check(state.HoldState==HoldPointState.Outside&&state.ObjectiveProgressSeconds==0,"outside state truthful");
  state.Advance(2,true,true,0);Check(state.HoldState==HoldPointState.Capturing&&state.ObjectiveProgressSeconds==2,"inside capture accrues");
  state.Advance(3,true,true,1);Check(state.HoldState==HoldPointState.Contested&&state.ObjectiveProgressSeconds==2,"contest preserves progress");
  state.Advance(10,false,true,0);Check(state.ObjectiveProgressSeconds==2&&state.HoldIdleWaitSeconds==0,"pause never advances capture or idle time");
  state.Advance(1,true,true,0);Check(state.HoldState==HoldPointState.Capturing&&state.ObjectiveProgressSeconds==3,"knockback out resumes capture");
  Check(state.HoldIdleWaitSeconds==0,"combat capture is not counted as idle clear-to-capture wait");
  for(int i=0;i<phase.EnemyCount;i++){Check(state.TryRegisterSpawn(phase,i),"register");Check(state.RecordDefeat(phase,i),"defeat");}
  Check(state.Status==ExpeditionModeStatus.Active,"kills alone cannot skip required time");
  state.Advance(2,true,false,0);Check(state.HoldIdleWaitSeconds==2&&state.ObjectiveProgressSeconds==3,"post-clear outside time recorded separately");
  state.Advance(3,true,true,0);Check(state.HoldIdleWaitSeconds==5&&state.ObjectiveProgressSeconds==6,"post-clear capture time recorded");
  state.Advance(100,true,true,0);Check(state.Status==ExpeditionModeStatus.AwaitingSpawn&&state.HoldIdleWaitSeconds==11,"completion records only actual remaining idle time");
  Check(state.HoldState==HoldPointState.Inactive,"between phases inactive");
  state.TryBeginPhase(out phase);state.Advance(phase.HoldSeconds,true,true,0);
  Check(state.HoldState==HoldPointState.Secured&&state.Status==ExpeditionModeStatus.Active,"capture alone still requires kills");
  state.Advance(1,true,true,0,false);Check(state.Status==ExpeditionModeStatus.Failed,"death remains terminal");
  double saved=state.HoldIdleWaitSeconds;state.Advance(10,true,true,0);Check(state.HoldIdleWaitSeconds==saved,"terminal cannot accrue idle time");
  return "PASS: "+n+" hold-point state/footprint/idle-wait assertions";
 }
}

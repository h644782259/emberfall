using System;
using Emberfall;
using UnityEngine;
public static class EnvironmentReadabilityTests
{
 static int n;static void Check(bool b,string m){n++;if(!b)throw new Exception(m);}
 public static string Run()
 {
  Check(!ArenaPulseRules.Warning(4.899f)&&ArenaPulseRules.Warning(4.9f),"warning begins at shared schedule boundary");
  Check(!ArenaPulseRules.Active(6.099f)&&ArenaPulseRules.Active(6.1f),"damage begins only when independent windup arc is complete");
  Check(ArenaPulseRules.Progress(4.9f)==0&&ArenaPulseRules.Progress(6.1f)==1,"timer matches visible warning and active onset");
  Check(ArenaPulseRules.Cycle(6.5f)==1&&ArenaPulseRules.Phase(6.5f)==0,"new cycle resets warning and damage token");
  float r=ArenaPulseRules.Radius;
  Check(ArenaPulseRules.Contains(r*r,true)&&!ArenaPulseRules.Contains((r+.001f)*(r+.001f),true),"same 2.1m boundary defines damage inclusive edge");
  WorldTraversal.Reset(ZoneKind.Dungeon);var origin=Vector3.zero;var player=new Vector3(2,0,0);
  Check(ArenaPulseRules.Contains(4,CombatSight.Area(origin,player)),"visible target inside hazard is eligible");
  WorldTraversal.AddBox(new Vector3(1,0,0),new Vector2(.2f,4));
  Check(!ArenaPulseRules.Contains(4,CombatSight.Area(origin,player)),"actual wall LOS blocks environmental damage");
  var outline=new Vector3[48];CombatSight.FillAreaBoundary(outline,origin,r);
  Check(outline[0].x<1&&CombatSight.Area(origin,outline[0]),"warning boundary clips to the same wall as damage");
  for(int i=0;i<25;i++)Check(ArenaPulseRules.HoldSegments(i/24f)==i,"hold capture progress has deterministic segment count");
  Check(ArenaPulseRules.HoldSegments(float.NaN)==0&&ArenaPulseRules.HoldSegments(2)==24,"segment buffer is bounded");
  for(int lines=1;lines<=3;lines++)foreach(float px in new[]{2f,12f,30f,100f})
  {float scaled=px*WorldLabelReadability.Scale(px,lines);Check(scaled>=12*lines-.001f&&scaled<=22*lines+.001f,"floating label projected height clamped per line");}
  Check(WorldLabelReadability.Scale(float.NaN,1)==1,"invalid projection cannot produce NaN transform scale");
  // Low wall intersects feet only; the former torso-only camera ray misses it.
  Check(CameraVisibilityRules.ProtectsBox(0,5,-10,0,.18f,0,-1,.4f,-1.2f,1,1,-.8f),"feet ray protects ground position");
  Check(!CameraVisibilityRules.ProtectsBox(0,5,-10,0,1.35f,0,-1,.4f,-1.2f,1,1,-.8f),"negative old torso-only ray misses low cover");
  Check(CameraVisibilityRules.ProtectsBox(0,5,-10,0,1.35f,0,-1,3,-5.2f,1,3.5f,-4.8f),"torso ray protects body");
  Check(CameraVisibilityRules.ProtectsBox(0,5,-10,5,1,0,2.2f,2.7f,-5.2f,2.8f,3.3f,-4.8f),"attack target ray protects off-axis enemy");
  Check(!CameraVisibilityRules.ProtectsBox(0,5,-10,0,1,0,2.2f,2.7f,-5.2f,2.8f,3.3f,-4.8f),"negative hero-only ray misses target cover");
  Check(!CameraVisibilityRules.ProtectsBox(0,5,-10,0,1,0,-1,0,2,1,3,4),"building behind protected point never fades");
  int free=4;Check(!CameraVisibilityRules.ReserveGroup(ref free,5)&&free==4,"insufficient capacity cannot partially admit a building");
  Check(CameraVisibilityRules.ReserveGroup(ref free,4)&&free==0,"whole building reserves bounded fade capacity");
  return "PASS: "+n+" shared hazard geometry/time, true wall LOS, capture segments, label and camera protection checks (not Unity rendering)";
 }
}

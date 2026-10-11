using System;using Emberfall;using UnityEngine;
public static class CombatSightTraversalTests
{
 private static int count;
 private static void Check(bool ok,string why){count++;if(!ok)throw new Exception(why);}
 public static string Run()
 {
  WorldTraversal.Reset(ZoneKind.Dungeon);int revision=WorldTraversal.Revision;
  WorldTraversal.AddBox(Vector3.zero,new Vector2(1,8));Check(WorldTraversal.Revision!=revision,"static wall invalidates preview cache");
  var a=new Vector3(-3,0,0);var b=new Vector3(3,0,0);
  Check(!CombatSight.Direct(a,b)&&!CombatSight.Area(a,b)&&!CombatSight.Chain(a,b)&&!CombatSight.Melee(a,b),"solid wall blocks every impact route");
  Vector3 preview=CombatSight.GroundPoint(a,b);
  Check(preview.x<=-.539f&&preview.x>-.55f&&CombatSight.Direct(a,preview),"ground preview/commit clips just before wall");
  Check(CombatSight.Area(a,new Vector3(-1,0,0)),"same-side area remains useful");
  for(int z=-3;z<=3;z++)
  {
   var origin=new Vector3(-4,0,z);var desired=new Vector3(4,0,-z);
   var point=CombatSight.GroundPoint(origin,desired);
   Check(CombatSight.Direct(origin,point)&&point.x<0,"diagonal clipping remains on visible side");
   Check(!CombatSight.Area(point,desired),"clipped center cannot damage hidden target through wall");
  }
  WorldTraversal.Reset(ZoneKind.Wilderness);revision=WorldTraversal.Revision;
  WorldTraversal.SetRiver(new[]{new Vector3(-8,0,0),new Vector3(8,0,0)},2,new Rect(-1,-2,2,4));
  Check(WorldTraversal.Revision!=revision,"water/bridge changes invalidate preview cache");
  a=new Vector3(3,0,-3);b=new Vector3(3,0,3);
  Check(CombatSight.Direct(a,b)&&CombatSight.Area(a,b)&&CombatSight.Chain(a,b),"spells may cross water");
  Check(!CombatSight.Melee(a,b),"ground swing cannot cross river");
  Check(Vector3.Distance(CombatSight.GroundPoint(a,b),b)<.001f,"ground magic placement across water matches policy");
  Check(CombatSight.Melee(new Vector3(0,0,-3),new Vector3(0,0,3)),"ground swing across actual bridge remains allowed");
  WorldTraversal.Reset(ZoneKind.Dungeon);var blocker=WorldTraversal.AddDynamicCircle(Vector3.zero,.8f);a=new Vector3(-3,0,0);b=new Vector3(3,0,0);revision=WorldTraversal.Revision;
  Check(!CombatSight.Area(a,b),"intact dynamic rubble blocks area");
  WorldTraversal.RemoveDynamicObstacle(blocker);
  Check(WorldTraversal.Revision!=revision&&CombatSight.Area(a,b),"broken rubble opens preview and actual hit policy immediately");
  Check(!CombatSight.Direct(a,new Vector3(float.NaN,0,0))&&Vector3.Distance(CombatSight.GroundPoint(a,new Vector3(float.NaN,0,0)),a)<.001f,"invalid coordinates fail safely");
  WorldTraversal.Reset(ZoneKind.Dungeon);WorldTraversal.AddBox(Vector3.zero,new Vector2(1,8));
  a=new Vector3(-3,0,0);var contour=new Vector3[64];CombatSight.FillAreaBoundary(contour,a,5);
  foreach(var point in contour)Check(CombatSight.Area(a,point),"released and preview contour stay on damage-visible side");
  Check(!CombatSight.VisualFootprint(a,new Vector3(-.7f,0,0),.3f),"mesh center visible but full mesh crossing wall is suppressed");
  Check(CombatSight.VisualFootprint(a,new Vector3(-1.5f,0,0),.3f),"complete same-side footprint remains visible");
  WorldTraversal.Reset(ZoneKind.Dungeon);blocker=WorldTraversal.AddDynamicCircle(Vector3.zero,.8f);
  Check(!CombatSight.VisualFootprint(a,b,.2f),"intact dynamic cover clips visuals");WorldTraversal.RemoveDynamicObstacle(blocker);
  Check(CombatSight.VisualFootprint(a,b,.2f),"removed cover immediately restores visuals");
  WorldTraversal.Reset(ZoneKind.Wilderness);
  return "PASS: "+count+" production combat-sight/traversal assertions (managed geometry)";
 }
}

using System;
using System.Collections.Generic;
using Emberfall;
using UnityEngine;
public static class EscapeRoomFormationTests
{
 static int n;static void Check(bool b,string m){n++;if(!b)throw new Exception(m);}
 public static string Run(bool legacy=false)
 {
  n=0;
  for(int seed=0;seed<162;seed++)
  {
   int room=(2-RoomTactics.Opening(seed)+3)%3;var plan=new RoomChainPlan(room,seed);
   WorldTraversal.Reset(ZoneKind.Dungeon);TacticalRoomGeometry.Register(plan.Layout);
   var occupied=new List<Vector3>();int guards=0,pursuers=0,supplier=0,flanker=0;
   for(int index=0;index<6;index++)
   {
    Vector3 point;bool placed=legacy?TacticalRoomGeometry.TrySpawn(seed,room,index,occupied,out point):EscapeRoomFormation.TrySpawn(seed,index,occupied,out point);
    Check(placed,"all six Escape roles have reachable distinct spawn positions");
    Check(WorldTraversal.CanReach(TacticalRoomGeometry.Entrance,point,.65f),"role spawn reachable from shared entrance");
    EscapeRole role=legacy?(index==1||index==4?EscapeRole.GateGuard:EscapeRole.None):EscapeRoomFormation.Role(index);
    if(role==EscapeRole.GateGuard)
    {
     guards++;Vector3 gate=new Vector3(0,0,11);
     Check(RoomTacticalRegion.Contests(CombatFx.Flat(point-gate).sqrMagnitude,.6f,WorldTraversal.HasLineOfSight(point,gate)),"guard post must actually contest visible north circle");
     Check(RoomTacticalRegion.ReceivesSupport(CombatFx.Flat(point-occupied[0]).sqrMagnitude,WorldTraversal.HasLineOfSight(point,occupied[0])),"gate supplier actually supports both guard posts");
    }
    if(role==EscapeRole.Pursuer)pursuers++;if(role==EscapeRole.GateSupplier)supplier++;if(role==EscapeRole.SideFlanker)flanker++;
    foreach(var other in occupied)Check(Vector3.Distance(other,point)>=2.4f,"role spawns do not overlap");
    occupied.Add(point);
   }
   Check(guards==2&&pursuers==2&&supplier==1&&flanker==1,"Escape roster exactly two post guards, two chasers, one supplier and side slime");
  }
  var guard=new EscapePostPolicy(EscapeRole.GateGuard);
  Check(!guard.ReturnToPost(.1f,1,4),"guard can initially be lured from post");
  for(int i=0;i<40;i++)guard.ReturnToPost(.1f,2,4);
  Check(guard.ReturnToPost(.1f,2,4),"short chase eventually yields to return even when target stays within leash");
  Check(guard.ReturnToPost(.1f,0,4),"arrival has a short reset interval rather than immediate endless chase");
  for(int i=0;i<12;i++)guard.ReturnToPost(.1f,0,4);
  Check(!guard.ReturnToPost(.1f,0,4),"guard can re-engage after arriving and resting");
  Check(new EscapePostPolicy(EscapeRole.GateGuard).ReturnToPost(.1f,5.001f,5),"guard beyond five metre tether returns");
  Check(new EscapePostPolicy(EscapeRole.GateSupplier).ReturnToPost(.1f,2.601f,6),"supplier stays with gate group");
  Check(new EscapePostPolicy(EscapeRole.SideFlanker).ReturnToPost(.1f,4.501f,5),"slime retains side route instead of whole-room pursuit");
  Check(!new EscapePostPolicy(EscapeRole.Pursuer).ReturnToPost(20,15,15),"goblins retain full pursuit");
  Check(!new EscapePostPolicy(EscapeRole.None).ReturnToPost(20,15,15),"other rooms are unaffected");
  return "PASS: "+n+" Escape formation, true traversal and post-policy checks (managed; no playtest claim)";
 }
}

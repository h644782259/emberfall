using System;
using Emberfall;
using UnityEngine;
public static class HubSettlementGeometryTests
{
 public static string Run()
 {
  int checks=0;Action<bool,string> check=(ok,message)=>{checks++;if(!ok)throw new Exception(message);};
  bool distinct=false;for(int i=0;i<6;i++)distinct|=Vector3.Distance(HubSettlementPlan.Building(1,i),HubSettlementPlan.Building(2,i))>1;
  check(distinct,"quarry staggered workshops and observatory crescent have different spatial plans");
  for(int hub=1;hub<=2;hub++)foreach(float radius in new[]{.35f,.45f,.65f,.9f})
  {
   WorldTraversal.Reset(ZoneKind.Wilderness);HubSettlementPlan.RegisterTownNavigation(hub);
   for(int i=0;i<3;i++)HubSettlementPlan.RegisterNpcNavigation(i);
   Vector3 entrance=new Vector3(0,0,-10),portal=new Vector3(0,0,11);
   check(WorldTraversal.IsWalkable(entrance,radius)&&WorldTraversal.CanReach(entrance,portal,radius),"camp arrival to portal reachable for actor clearance");
   for(int npc=0;npc<3;npc++)
   {
    Vector3 target=HubSettlementPlan.Npc(npc)+new Vector3(0,0,-1.5f);
    check(WorldTraversal.IsWalkable(target,radius)&&WorldTraversal.CanReach(entrance,target,radius),"all NPC interaction approaches reachable");
    check(Vector3.Distance(target,HubSettlementPlan.Npc(npc))<2.65f,"reachable point within real interaction radius");
   }
   for(int building=0;building<HubSettlementPlan.BuildingCount;building++)
   {Vector3 p=HubSettlementPlan.Building(hub,building);check(!WorldTraversal.IsWalkable(p,radius),"visible building uses production solid footprint");}
   for(int x=-9;x<=9;x+=3)for(int z=-16;z<=16;z+=4)
   {Vector3 p=new Vector3(x,0,z);if(WorldTraversal.IsWalkable(p,radius))check(WorldTraversal.CanReach(entrance,p,radius),"central town routes have no isolated walkable pocket");}
  }
  return "PASS: "+checks+" production town navigation/approach checks (no Unity rendering)";
 }
}

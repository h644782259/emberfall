using UnityEngine;
namespace Emberfall
{
    // The same authored centers drive the visible buildings and production traversal.
    public static class HubSettlementPlan
    {
        public const int BuildingCount=6;
        public const float BuildingWidth=5.5f;
        public static Vector3 Building(int hub,int index)
        {
            int side=index<3?-1:1,row=index%3;
            if(hub==1)return new Vector3(side*13,0,-12+row*10+(side>0?4:0));
            return new Vector3(side*(row==1?18:row==0?15:12),0,row==0?-10:row==1?0:13);
        }
        // Service clearings beside, rather than across, the caravan through-road.
        public static Vector3 Npc(int index){return index==0?new Vector3(-6,0,-16):index==1?new Vector3(6,0,-16):new Vector3(-6,0,-4);}
        public static Vector3 ForgePier(int side){return new Vector3(side*7,0,2);}
        public static Vector3 ObservatoryCenter {get{return new Vector3(0,0,3);}}
        public static WorldTraversal.ObstacleHandle[] RegisterTownNavigation(int hub)
        {
            var handles=new WorldTraversal.ObstacleHandle[BuildingCount+(hub==1?2:1)];
            for(int index=0;index<BuildingCount;index++)handles[index]=WorldTraversal.AddDynamicBox(Building(hub,index),new Vector2(BuildingWidth,BuildingWidth));
            if(hub==1)for(int side=-1;side<=1;side+=2)handles[BuildingCount+(side+1)/2]=WorldTraversal.AddDynamicBox(ForgePier(side),new Vector2(1.4f,1.4f));
            else handles[BuildingCount]=WorldTraversal.AddDynamicCircle(ObservatoryCenter,1.1f);
            return handles;
        }
        public static WorldTraversal.ObstacleHandle[] RegisterNpcNavigation(int index)
        {
            Vector3 p=Npc(index);
            return new[]{WorldTraversal.AddDynamicCircle(p,.43f),WorldTraversal.AddDynamicBox(p+new Vector3(0,0,.65f),new Vector2(1.4f,.5f))};
        }
    }
}

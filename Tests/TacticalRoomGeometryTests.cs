using System;
using Emberfall;
using UnityEngine;
public static class TacticalRoomGeometryTests
{
    static int n;
    static void Check(bool b,string why){n++;if(!b)throw new Exception(why);}
    public static string Run()
    {
        for(int layout=20;layout<=25;layout++)
        {
            WorldTraversal.Reset(ZoneKind.Dungeon);
            // Unchanged linked-room frame and optional rubble, at production dimensions.
            for(int side=-1;side<=1;side+=2)
            {
                WorldTraversal.AddBox(new Vector3(side*16,0,0),new Vector2(1.1f,29));
                WorldTraversal.AddBox(new Vector3(side*3.4f,0,15),new Vector2(1.4f,1.4f));
                WorldTraversal.AddDynamicCircle(new Vector3(side*13.3f,0,2.2f),.7f);
            }
            TacticalRoomGeometry.Register(layout);
            int mirror=layout%2==0?1:-1;
            var entrance=TacticalRoomGeometry.Entrance;var exit=new Vector3(0,0,14);
            foreach(float radius in new[]{.45f,.65f,1.3f})
            {
                foreach(var target in new[]{exit,new Vector3(-mirror*8,0,-6),new Vector3(mirror*8,0,9),new Vector3(0,0,11),new Vector3(-mirror*12,0,-6)})
                {
                    Check(WorldTraversal.CanReach(entrance,target,radius),"layout "+layout+" reachable objective/exit/event radius "+radius);
                    var route=WorldTraversal.FindPath(entrance,target,radius);
                    for(int i=1;i<route.Count;i++)Check(WorldTraversal.HasGroundPath(route[i-1],route[i],radius),"every path segment walkable");
                }
            }
            for(int seed=0;seed<194;seed++)
            for(int room=0;room<3;room++)
            {
                var occupied=new System.Collections.Generic.List<Vector3>();
                for(int index=0;index<6;index++)
                {
                    Vector3 at,again;
                    Check(TacticalRoomGeometry.TrySpawn(seed,room,index,occupied,out at),"all six planned enemies have reachable safe spawns");
                    Check(TacticalRoomGeometry.TrySpawn(seed,room,index,occupied,out again)&&Vector3.Distance(at,again)<.0001f,"spawn deterministic");
                    Check(Vector3.Distance(at,entrance)>=5.5f&&WorldTraversal.CanReach(entrance,at,.65f),"safe reachable spawn");
                    Check(WorldTraversal.CanReach(entrance,at,.45f),"all player classes can walk to every enemy without blink or ranged attacks");
                    bool companionApproach=false;
                    for(int stance=0;stance<8;stance++)
                    {
                        float a=stance*Mathf.PI/4;
                        var near=at+new Vector3(Mathf.Cos(a)*1.4f,0,Mathf.Sin(a)*1.4f);
                        if(WorldTraversal.CanReach(entrance,near,.7f)&&WorldTraversal.HasGroundPath(near,at,.12f)){companionApproach=true;break;}
                    }
                    Check(companionApproach,"largest melee companion has a walkable attack stance within 1.8m");
                    foreach(var other in occupied)Check(Vector3.Distance(at,other)>=2.4f,"spawns cannot stack");
                    occupied.Add(at);
                }
            }
            if(TacticalRoomGeometry.Flooded(layout))
            {
                Check(!WorldTraversal.HasGroundPath(entrance,exit),"water prevents straight walk");
                Check(WorldTraversal.HasLineOfSight(new Vector3(0,0,-3),new Vector3(0,0,3)),"spells cross water");
                Check(WorldTraversal.IsWalkable(new Vector3(mirror*7,0,0),1.3f),"bridge fits widest creature");
            }
            else Check(!WorldTraversal.HasLineOfSight(entrance,exit),"walls split ranged sight lines");
        }
        WorldTraversal.Reset(ZoneKind.Dungeon);
        Check(!WorldTraversal.CanReach(new Vector3(30,0,0),Vector3.zero),"reject out-of-bounds entrance instead of snapping it");
        return "PASS: "+n+" production geometry/path assertions across six layouts (managed Unity shims, not rendered playtest)";
    }
}

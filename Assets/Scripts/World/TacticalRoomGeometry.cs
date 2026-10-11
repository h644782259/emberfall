using UnityEngine;
namespace Emberfall
{
    // Render and navigation consume the same authored wall/bridge geometry.
    public static class TacticalRoomGeometry
    {
        public static Vector3 Entrance {get{return new Vector3(0,0,-12);}}
        public struct Wall
        {
            public Vector3 Position; public Vector2 Size;
            public Wall(float x,float z,float width,float depth){Position=new Vector3(x,0,z);Size=new Vector2(width,depth);}
        }
        public static Wall[] Walls(int layout)
        {
            int terrain=(layout-20)/2,mirror=layout%2==0?1:-1;
            if(terrain==0)return new[]{new Wall(0,0,5,8),new Wall(-mirror*9,7,4,1)};
            if(terrain==2)return new[]{new Wall(-mirror*5,-2,12,1.3f),new Wall(mirror*5,5,12,1.3f)};
            return new Wall[0];
        }
        public static bool Flooded(int layout){return (layout-20)/2==1;}
        public static Vector3[] River(){return new[]{new Vector3(-18,0,0),new Vector3(18,0,0)};}
        public static Rect Bridge(int layout){return new Rect((layout%2==0?7:-7)-2.7f,-3,5.4f,6);}
        public static bool TrySpawn(int seed,int room,int index,System.Collections.Generic.List<Vector3> occupied,out Vector3 position)
        {
            float angle=index*2.39996f+room*.42f+(seed%97)*.06f;
            Vector3 desired=new Vector3(Mathf.Sin(angle)*10,0,Mathf.Cos(angle)*9+2);
            Vector3 entrance=Entrance;
            for(int attempt=0;attempt<48;attempt++)
            {
                Vector3 probe=attempt==0?desired:desired+new Vector3(Mathf.Sin(attempt*2.39996f),0,Mathf.Cos(attempt*2.39996f))*(.5f+attempt*.23f);
                probe=WorldTraversal.NearestWalkable(probe,.65f);
                if(probe.magnitude>16.35f||Vector3.Distance(probe,entrance)<5.5f||!WorldTraversal.CanReach(entrance,probe,.65f))continue;
                bool crowded=false;
                foreach(var other in occupied)if(Vector3.Distance(probe,other)<2.4f){crowded=true;break;}
                if(crowded)continue;
                position=probe;return true;
            }
            position=Vector3.zero;return false;
        }
        public static WorldTraversal.ObstacleHandle[] Register(int layout)
        {
            var walls=Walls(layout);var handles=new WorldTraversal.ObstacleHandle[walls.Length];
            for(int index=0;index<walls.Length;index++)handles[index]=WorldTraversal.AddDynamicBox(walls[index].Position,walls[index].Size);
            if(Flooded(layout))WorldTraversal.SetRiver(River(),3.2f,Bridge(layout));
            return handles;
        }
    }
}

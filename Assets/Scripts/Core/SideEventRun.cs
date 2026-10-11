using System;
using System.Collections.Generic;
namespace Emberfall
{
    // A side encounter never registers with the room's primary victory roster.
    public sealed class SideEventRun
    {
        public const int EnemyCount=2, RoomEnemyLimit=8;
        private readonly object context,owner;
        private readonly int epoch;
        private readonly HashSet<object> remaining=new HashSet<object>();
        private int registered;
        public readonly string Receipt;
        public bool Completed {get;private set;}
        public bool Claimed {get;private set;}
        public bool Abandoned {get;private set;}
        public int Remaining {get{return remaining.Count;}}
        public SideEventRun(object context,object owner,int epoch,string receipt)
        {this.context=context;this.owner=owner;this.epoch=epoch;Receipt=receipt;}
        public bool Register(object enemy)
        {if(enemy==null||registered>=EnemyCount||Abandoned||Completed||!remaining.Add(enemy))return false;registered++;return true;}
        public bool Contains(object enemy,object currentContext,object currentOwner,int currentEpoch,bool active)
        {return active&&!Abandoned&&!Completed&&registered==EnemyCount&&ReferenceEquals(context,currentContext)&&ReferenceEquals(owner,currentOwner)&&epoch==currentEpoch&&enemy!=null&&remaining.Contains(enemy);}
        public bool Defeat(object enemy,object currentContext,object currentOwner,int currentEpoch,bool active)
        {
            if(!active||Abandoned||Completed||registered!=EnemyCount||!ReferenceEquals(context,currentContext)||
                !ReferenceEquals(owner,currentOwner)||epoch!=currentEpoch||enemy==null||!remaining.Remove(enemy))return false;
            Completed=remaining.Count==0;return true;
        }
        public bool TryClaim(Func<string,bool> durableGrant)
        {if(Claimed)return true;if(!Completed||Abandoned||durableGrant==null||!durableGrant(Receipt))return false;Claimed=true;return true;}
        public void Abandon(){if(Completed)return;Abandoned=true;remaining.Clear();}
        public static bool HasRoomCapacity(int alive){return alive>=0&&alive<=RoomEnemyLimit-EnemyCount;}
    }
}

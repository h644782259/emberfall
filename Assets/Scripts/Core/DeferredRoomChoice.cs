namespace Emberfall
{
    // A single room/epoch-owned request. Opening UI is a host-tick boundary,
    // never a side effect in the middle of a synchronous damage callback.
    public sealed class DeferredRoomChoice
    {
        private object room;
        private int epoch,requestedFrame;
        public bool Pending {get{return room!=null;}}
        public bool Request(object identity,int combatEpoch,int frame)
        {
            if(identity==null||Pending)return false;
            room=identity;epoch=combatEpoch;requestedFrame=frame;return true;
        }
        public bool TryClaim(object identity,int combatEpoch,int frame,bool runValid,bool canAdvance)
        {
            if(!Pending)return false;
            if(!runValid||!object.ReferenceEquals(room,identity)||epoch!=combatEpoch){Cancel();return false;}
            if(!canAdvance||frame==requestedFrame)return false;
            Cancel();return true;
        }
        public void Cancel(){room=null;epoch=requestedFrame=0;}
    }
}

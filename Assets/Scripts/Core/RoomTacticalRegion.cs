namespace Emberfall
{
    // Tactical rooms intentionally differ from the ordinary 3.2m HoldPoint trial.
    // All distances are horizontal squared distances; touching the boundary counts.
    public static class RoomTacticalRegion
    {
        public const float CaptureRadius=2.4f, SupportRadius=6f;
        private static bool Valid(float value){return !float.IsNaN(value)&&!float.IsInfinity(value)&&value>=0;}
        public static bool ContainsPlayer(float distanceSquared)
        {return Valid(distanceSquared)&&distanceSquared<=CaptureRadius*CaptureRadius;}
        public static bool Contests(float distanceSquared,float footprint,bool lineOfSight)
        {return lineOfSight&&Valid(distanceSquared)&&Valid(footprint)&&distanceSquared<=(CaptureRadius+footprint)*(CaptureRadius+footprint);}
        public static bool ReceivesSupport(float distanceSquared,bool lineOfSight)
        {return lineOfSight&&Valid(distanceSquared)&&distanceSquared<=SupportRadius*SupportRadius;}
    }
    public enum RoomTargetSupport { None, Supplier, Supported, Severed, Exempt }
    public struct RoomSupportSnapshot
    {
        public readonly bool SupplierAlive;
        public readonly int SupportedCount;
        public readonly RoomTargetSupport Target;
        public RoomSupportSnapshot(bool alive,int count,RoomTargetSupport target)
        {SupplierAlive=alive;SupportedCount=alive?System.Math.Max(0,count):0;Target=target;}
        public string Hint
        {
            get
            {
                if(!SupplierAlive)return "供能者已倒 · 护援已断";
                if(SupportedCount==0)return "供能存活·全断援成功";
                string target=Target==RoomTargetSupport.Supported?"目标受援":Target==RoomTargetSupport.Severed?"目标已断援":Target==RoomTargetSupport.Supplier?"目标供能者":Target==RoomTargetSupport.Exempt?"目标不受援":"未锁定目标";
                return "供能存活·援"+SupportedCount+"·"+target;
            }
        }
    }
}

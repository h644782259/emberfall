using System;
using Emberfall;
// API contract doubles only; actual Unity reference compilation is separately required.
namespace UnityEngine
{
    public struct EntityId
    {
        internal ulong value;
        public static ulong ToULong(EntityId id) { return id.value; }
        [Obsolete("No integer conversion", true)] public static implicit operator int(EntityId id) { return 0; }
    }
    public class Object
    {
        public ulong id;
        public bool destroyed;
        public EntityId GetEntityId() { return new EntityId { value=id }; }
#if UNITY_6000_6_OR_NEWER
        [Obsolete("Use GetEntityId", true)]
#endif
        public int GetInstanceID() { return unchecked((int)id); }
        public static bool operator ==(Object a, Object b) { return (ReferenceEquals(a,null)||a.destroyed) && ReferenceEquals(b,null) || ReferenceEquals(a,b); }
        public static bool operator !=(Object a, Object b) { return !(a==b); }
        public override bool Equals(object o) { return ReferenceEquals(this,o); }
        public override int GetHashCode() { return 7; } // Deliberate collisions must not affect IDs.
    }
}
public static class CombatReviewObjectIdTests
{
    public static string Run()
    {
        var a=new UnityEngine.Object { id=ulong.MaxValue };
        var b=new UnityEngine.Object { id=ulong.MaxValue-1 };
        string first=CombatReviewObjectId.Get(a);
#if UNITY_6000_6_OR_NEWER
        if(first!="18446744073709551615" || CombatReviewObjectId.Get(b)!="18446744073709551614") throw new Exception("64-bit truncation");
#else
        if(first!="-1" || CombatReviewObjectId.Get(b)!="-2") throw new Exception("legacy signed ID changed");
#endif
        if(first!=CombatReviewObjectId.Get(a)||first==CombatReviewObjectId.Get(b))throw new Exception("unstable/colliding ID");
        if(CombatReviewObjectId.Get(null)!="0")throw new Exception("null sentinel");
        a.destroyed=true;
        if(CombatReviewObjectId.Get(a)!="0")throw new Exception("destroyed sentinel");
        return "PASS: adapter full-width identity, repeated reads, hash collision independence, null/destroyed sentinel (API doubles)";
    }
}

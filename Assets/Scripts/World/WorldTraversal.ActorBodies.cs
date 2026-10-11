using UnityEngine;
namespace Emberfall
{
    public static partial class WorldTraversal
    {
        // Body movement uses finite rendered solids as well as the authored
        // navigation footprint. A capsule standing on a slab only touches its top.
        internal static bool IsActorCapsuleClear(Vector3 feet,float radius,float height)
        {
            if(!Finite(feet.x)||!Finite(feet.y)||!Finite(feet.z)||!Finite(radius)||!Finite(height)||radius<=0||height<radius*2)return false;
            CollectSolidCandidates(feet,radius);
            foreach(int index in solidCandidates)
            {
                var o=obstacles[index];if(!o.HasPhysicalBounds)continue;
                if(CapsuleSolidDistanceSquared(feet,radius,height,o)<radius*radius-.00001f)return false;
            }
            return true;
        }
        private static float CapsuleSolidDistanceSquared(Vector3 feet,float radius,float height,Obstacle o)
        {
            float dx=Mathf.Max(0,Mathf.Abs(feet.x-o.PhysicalCenter.x)-o.PhysicalHalf.x);
            float dz=Mathf.Max(0,Mathf.Abs(feet.z-o.PhysicalCenter.z)-o.PhysicalHalf.z);
            float bottom=feet.y+radius,top=feet.y+height-radius;
            float dy=Mathf.Max(0,Mathf.Max(o.PhysicalCenter.y-o.PhysicalHalf.y-top,bottom-o.PhysicalCenter.y-o.PhysicalHalf.y));
            return dx*dx+dy*dy+dz*dz;
        }
        internal static bool IsActorCapsuleStepClear(Vector3 from,Vector3 to,float radius,float height)
        {
            if(!Finite(to.x)||!Finite(to.y)||!Finite(to.z)||radius<=0||height<radius*2)return false;
            CollectSolidCandidates(to,radius);
            foreach(int index in solidCandidates)
            {
                var o=obstacles[index];if(!o.HasPhysicalBounds)continue;
                float distance=CapsuleSolidDistanceSquared(to,radius,height,o),limit=radius*radius-.00001f;
                if(distance>=limit)continue;
                float old=CapsuleSolidDistanceSquared(from,radius,height,o);
                if(old>=limit||distance<old-.000001f)return false;
                // Permit an existing overlap to move out, including from inside
                // a broad box where closest-surface distance remains zero.
                Vector3 centreOffset=Vector3.up*(height*.5f)-o.PhysicalCenter;
                if((to+centreOffset).sqrMagnitude<(from+centreOffset).sqrMagnitude-.000001f)return false;
            }
            return true;
        }
    }
}

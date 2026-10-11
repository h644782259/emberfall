using UnityEngine;
namespace Emberfall
{
    internal static class ReturningCounterRules
    {
        // Admission, button reason and execution use this same predicted landing.
        internal static string Predict(Vector3 origin,Vector3 target,bool boss,float footprint,out Vector3 landing)
        {
            landing=origin;
            if(!CombatSight.Melee(origin,target)||!WorldTraversal.IsWalkable(origin,.45f))return "目标被遮挡";
            landing=Advance(origin,CombatFx.Flat(target-origin).normalized,target);
            if(!WorldTraversal.IsWalkable(landing,.45f)||!CombatSight.Melee(landing,target))return "目标被遮挡";
            return CombatFx.Flat(target-landing).magnitude<=2.8f+(boss?.5f:0)+footprint?"":"距离不足";
        }

        // A ground approach, not a dodge/blink: never relocates an invalid origin,
        // never slides around obstacles and never searches for a point beyond one.
        internal static Vector3 Advance(Vector3 origin,Vector3 forward,Vector3 target)
        {
            Vector3 delta=CombatFx.Flat(target-origin);forward=CombatFx.Flat(forward).normalized;
            if(float.IsNaN(delta.sqrMagnitude)||float.IsInfinity(delta.sqrMagnitude)||forward.sqrMagnitude<.5f||!WorldTraversal.IsWalkable(origin,.45f))return origin;
            float along=Vector3.Dot(delta,forward);
            if(along<=1.8f||(delta-forward*along).magnitude>.35f)return origin;
            float distance=Mathf.Min(2f,along-1.8f);Vector3 end=origin;
            for(float step=.05f;step<distance+.05f;step+=.05f)
            {
                Vector3 next=origin+forward*Mathf.Min(step,distance);
                if(!WorldTraversal.HasGroundPath(end,next,.45f)||!WorldTraversal.IsWalkable(next,.45f))break;
                end=next;
            }
            return end;
        }
    }
}

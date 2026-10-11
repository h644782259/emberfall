using System;
namespace Emberfall
{
    public enum CombatSightKind { Direct, GroundPlacement, Area, Chain, Melee }
    /// <summary>Water blocks ground swings, while solid cover blocks all spell routes.</summary>
    public static class CombatSightRules
    {
        public const int RefinementSteps=14;
        public static bool Allows(CombatSightKind kind,bool solidClear,bool groundClear)
        { return solidClear && (kind != CombatSightKind.Melee || groundClear); }
        public static float VisibleFraction(Func<float,bool> clear)
        {
            if(clear==null || !clear(0))return 0;
            if(clear(1))return 1;
            float low=0,high=1;
            for(int n=0;n<RefinementSteps;n++){float mid=(low+high)*.5f;if(clear(mid))low=mid;else high=mid;}
            return low;
        }
    }
}

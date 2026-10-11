using System;
namespace Emberfall
{
    public static class ArenaPulseRules
    {
        public const float Radius=2.1f,Period=6.5f,WarningAt=4.9f,ActiveAt=6.1f;
        public static int Cycle(float age){return (int)Math.Floor(age/Period);}
        public static float Phase(float age){return age-Cycle(age)*Period;}
        public static bool Warning(float phase){return phase>=WarningAt;}
        public static bool Active(float phase){return phase>=ActiveAt;}
        public static float Progress(float phase){return Math.Max(0,Math.Min(1,(phase-WarningAt)/(ActiveAt-WarningAt)));}
        public static bool Contains(float distanceSquared,bool visible)
        {return visible&&!float.IsNaN(distanceSquared)&&!float.IsInfinity(distanceSquared)&&distanceSquared>=0&&distanceSquared<=Radius*Radius;}
        public static int HoldSegments(float progress,int segments=24)
        {return float.IsNaN(progress)?0:Math.Max(0,Math.Min(segments,(int)Math.Floor(progress*segments)));}
    }
}

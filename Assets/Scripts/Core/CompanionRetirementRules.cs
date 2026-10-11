using System;
namespace Emberfall
{
    public enum CompanionRetirementReason { ContextEnded, Defeated, Expired, Replaced }
    public struct CompanionRetirementPose
    {
        public readonly float Height,Roll,Scale,Opacity;
        public CompanionRetirementPose(float height,float roll,float scale,float opacity){Height=height;Roll=roll;Scale=scale;Opacity=opacity;}
    }
    public static class CompanionRetirementRules
    {
        public const int MaximumVisuals=8;
        public static float Duration(CompanionRetirementReason reason)
        {return reason==CompanionRetirementReason.Defeated?.58f:reason==CompanionRetirementReason.Expired?.48f:reason==CompanionRetirementReason.Replaced?.30f:0;}
        public static CompanionRetirementPose Pose(CompanionRetirementReason reason,float progress)
        {
            float t=float.IsNaN(progress)?1:Math.Max(0,Math.Min(1,progress));
            if(reason==CompanionRetirementReason.Defeated)return new CompanionRetirementPose(-.22f*t,65*t,1-.18f*t,1-t);
            if(reason==CompanionRetirementReason.Expired)return new CompanionRetirementPose(.5f*t,0,1-.65f*t,1-t);
            return new CompanionRetirementPose(.10f*t,0,1-.95f*t,1-t);
        }
    }
}

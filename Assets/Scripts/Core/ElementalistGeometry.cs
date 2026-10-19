using System;
namespace Emberfall
{
    // Shared by contacts, previews and authored presentation.
    public static class ElementalistGeometry
    {
        public static float FrostWidth(int rank)=>5.2f*GameBalance.SkillRangeMultiplier(rank);
        public static float FrostLength(int rank)=>9f*GameBalance.SkillRangeMultiplier(rank);
        public static float SwitchRadius(int rank)=>5f*GameBalance.SkillRangeMultiplier(rank);
        public static float PressRadius(int rank)=>4.8f*GameBalance.SkillRangeMultiplier(rank);
        public static float PressDuration(int rank)=>4+Math.Max(1,Math.Min(3,rank));
        public static float PressTick(int rank)=>.45f+.10f*(Math.Max(1,Math.Min(3,rank))-1);
        public static float SwitchHit(int rank)=>1.5f+.45f*(Math.Max(1,Math.Min(3,rank))-1);
        public static bool InRectangle(float forward,float sideways,float halfLength,float halfWidth)
            =>Math.Abs(forward)<=halfLength&&Math.Abs(sideways)<=halfWidth;
    }
}

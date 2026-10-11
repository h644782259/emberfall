using System;
namespace Emberfall
{
    // Presentation-only settling. No caller may use these seconds for hit schedules.
    public static class SkillVisualAftermathTiming
    {
        public const float FieldTail = .85f;
        public const float FinaleTail = 1.25f;
        public const float SolidFade = .45f;
        public static float Tail(bool finale) => finale ? FinaleTail : FieldTail;
        public static float Coverage(float remaining, float fade = SolidFade)
        {
            if (float.IsNaN(remaining) || float.IsInfinity(remaining) || remaining <= 0) return 0;
            return Math.Min(1, remaining / Math.Max(.05f, fade));
        }
    }
}

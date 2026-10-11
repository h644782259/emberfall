using System;
namespace Emberfall
{
    // Basic damage is immediate. The body starts at contact/release, then recovers
    // for the actual attack interval; no delayed gameplay event is scheduled.
    public static class BasicActionTimeline
    {
        public static bool BlocksBasic(bool basic, float age, float duration)
        { return !basic && duration > 0 && age < duration * .65f; }
        public const float BowRelease = .32f, BowSettled = .48f, ArrowReload = .83f;
        public static float Contact(bool bow) { return bow ? BowRelease : .52f; }
        public static float Duration(bool bow, float interval)
        {
            if (float.IsNaN(interval) || float.IsInfinity(interval)) interval = .18f;
            return Math.Max(.18f, interval) / (1f - Contact(bow));
        }
        public static bool ArrowVisible(float phase, bool acting)
        { return !acting || phase >= ArrowReload; }
        public static float BowDraw(float phase)
        {
            float t = Math.Max(0, Math.Min(1, (phase - BowRelease) / (BowSettled - BowRelease)));
            return 1f - t * t * (3f - 2f * t);
        }
    }
}

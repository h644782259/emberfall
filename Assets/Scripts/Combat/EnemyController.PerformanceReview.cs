namespace Emberfall
{
    public sealed partial class EnemyController
    {
        // A fixed, explicit stress-test workload: high-level heroes must not turn
        // a 24-enemy thermal run into an almost empty arena after a few casts.
        internal bool PreparePerformanceReviewDurability()
        {
            if (System.Environment.GetEnvironmentVariable("EMBERFALL_PERF_REVIEW") != "performance-soak-combat-crowd" ||
                session == null || !session.PracticeActive || session.Progression == null || !session.Progression.IsPracticeOnly ||
                session.PracticeRecord == null || session.PracticeRecord.Started || !session.PracticeRecord.UsesEnemyAI || IsDead)
                return false;
            MaxHealth = Health = System.Math.Max(MaxHealth, 100000000f);
            return true;
        }
    }
}

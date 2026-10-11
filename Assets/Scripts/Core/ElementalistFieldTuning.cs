namespace Emberfall
{
    // User-requested coverage/pull changes. Damage coefficients, contact counts,
    // cadence, resources and cooldowns remain SkillDamageBudgets' authority.
    public static class ElementalistFieldTuning
    {
        public const float GravityRadius = 5.8f;
        public const float GravityPullRadius = 6.4f;
        public const float GravityCollapseRadius = 6.4f;
        public static float GravityPullStrength(int rank) => rank >= 3 ? 12f : 9f;
        public const float UltimatePulseRadius = 6.8f;
        public const float UltimateFinalRadius = 8f;
        public const float UltimateTailRadius = 6.8f;
        public const float UltimateVisualLife = 2.8f;
        public const float UltimateVisualTail = 1.8f;
    }
}

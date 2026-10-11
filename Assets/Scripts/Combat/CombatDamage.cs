namespace Emberfall
{
    // The roll travels with its damage through delayed hits. Plain damage such
    // as poison, burns and pet attacks stays non-critical without cosmetic rolls.
    public readonly struct CombatDamage
    {
        public float Amount { get; }
        public bool IsCritical { get; }
        public float CriticalMultiplier { get; }
        public CombatDamage(float amount, bool isCritical, float criticalMultiplier = 1.65f)
        {
            Amount = amount; IsCritical = isCritical;
            CriticalMultiplier = float.IsNaN(criticalMultiplier) || float.IsInfinity(criticalMultiplier)
                ? 1.65f : System.Math.Max(1f, System.Math.Min(2.25f, criticalMultiplier));
        }
        public static CombatDamage Roll(float amount, float chance, float roll, float criticalMultiplier = 1.65f)
        {
            criticalMultiplier = float.IsNaN(criticalMultiplier) || float.IsInfinity(criticalMultiplier)
                ? 1.65f : System.Math.Max(1f, System.Math.Min(2.25f, criticalMultiplier));
            bool critical = roll < chance;
            return new CombatDamage(amount * (critical ? criticalMultiplier : 1f), critical, criticalMultiplier);
        }
        public CombatDamage WithoutCritical() { return new CombatDamage(IsCritical ? Amount / CriticalMultiplier : Amount, false); }
        public static CombatDamage operator *(CombatDamage value, float multiplier) { return new CombatDamage(value.Amount * multiplier, value.IsCritical, value.CriticalMultiplier); }
        public static implicit operator CombatDamage(float amount)
        { return new CombatDamage(amount, false); }
    }
}

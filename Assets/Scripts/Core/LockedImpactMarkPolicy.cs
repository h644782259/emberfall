namespace Emberfall
{
    public static class LockedImpactMarkPolicy
    {
        // Called only after real collision/visibility acceptance, before damage.
        // Identity is immutable: a dead lock can never move its debuff to a bystander.
        public static bool ShouldApply<T>(T locked, T hit, bool living, float damage, float strength) where T : class
        { return locked != null && object.ReferenceEquals(locked,hit) && living && damage > 0 && strength > 0; }
    }
}

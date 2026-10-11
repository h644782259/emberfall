namespace Emberfall
{
    public static class ConcentratedVenomRules
    {
        public static float DirectCoefficient(int rank) { return BuildCatalog.ConcentratedVenomCoefficient(rank); }
        public const float Radius = .14f;
        public const float SteeringSeconds = .18f;
        public const float SteeringDegrees = 18f;
        public const float DegreesPerSecond = 70f;
    }
}

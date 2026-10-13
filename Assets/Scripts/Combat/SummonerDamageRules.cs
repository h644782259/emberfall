using System;

namespace Emberfall
{
    // Coefficients multiply the cast's rank-scaled attack. Pet stats instead use
    // CompanionRules.RankPower and their own confirmed-attack cadence.
    public static class SummonerDamageRules
    {
        public const float ImpulseCoefficient=1.8f;
        public const float ThornTickCoefficient=.30f, ThornStartup=.25f, ThornInterval=.7f, ThornPoisonFraction=.2f;
        public const float MarkTickCoefficient=.30f, MarkInterval=.8f, MarkFirstTick=0f, MarkLastTick=4f;
        public const float MarkFinisherTime=4.2f, MarkFinisherCoefficient=2.6f;
        public static float ThornDuration(int rank){return 3+Math.Max(1,Math.Min(3,rank));}
        public static float ThornPoisonDuration(int rank){return 3.5f+Math.Max(1,Math.Min(3,rank))*.5f;}
        public static int ThornTicks(int rank){return (int)Math.Floor(ThornDuration(rank)/ThornInterval+.0001f)+1;}
        public static int MarkTicks {get{return (int)Math.Floor((MarkLastTick-MarkFirstTick)/MarkInterval+.0001f)+1;}}
    }
}

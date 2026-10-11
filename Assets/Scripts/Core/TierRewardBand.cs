using System;
namespace Emberfall
{
    /// <summary>One bounded tier schedule shared by ordinary dungeons and every challenge.</summary>
    public static class TierRewardBand
    {
        public static int Clamp(int tier){return Math.Max(1,Math.Min(100,tier));}
        public static int Of(int tier){tier=Clamp(tier);return tier<5?0:tier<10?1:tier<20?2:tier<40?3:4;}
        public static int Materials(int baseAmount,int tier){return baseAmount<=0?0:Math.Min(4,baseAmount)+Of(tier);}
    }
}

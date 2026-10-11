namespace Emberfall
{
    // Pure seeded chapter variation. Defaults preserve the original +35 degree clockwise beam.
    public static class ChapterBossPattern
    {
        public static float SweepSign(bool heroic,int seed,int phase)
        {return heroic&&((seed^phase)&1)!=0?-1:1;}
        public static float StartOffset(bool heroic,int seed,int phase)
        {return heroic?SweepSign(true,seed,phase)*95f:35f;}
        public static float AnchorOffset(bool heroic,int seed,int phase)
        {return heroic?SweepSign(true,seed,phase)*45f:0;}
    }
}

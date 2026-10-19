namespace Emberfall
{
    // Pure seeded chapter variation. Defaults preserve the original +35 degree clockwise beam.
    public static class ChapterBossPattern
    {
        public static float SweepSign(bool seededPattern,int seed,int phase)
        {return seededPattern&&((seed^phase)&1)!=0?-1:1;}
        public static float StartOffset(bool seededPattern,int seed,int phase)
        {return seededPattern?SweepSign(true,seed,phase)*95f:35f;}
        public static float AnchorOffset(bool seededPattern,int seed,int phase)
        {return seededPattern?SweepSign(true,seed,phase)*45f:0;}
    }
}

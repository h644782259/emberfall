namespace Emberfall
{
    public static class EnemyStatusVisualRules
    {
        public static int IceCount(bool frozen,bool boss,bool selected,bool elite,bool reduced)
        {return !frozen||boss?0:reduced?1:selected?3:elite?2:1;}
        public static float Emphasis(bool selected,bool boss,bool elite,bool reduced)
        {return reduced?1f:selected?1.35f:boss||elite?1.15f:1f;}
    }
}

namespace Emberfall
{
    public enum DungeonTactic { Bulwark, Pincer, Crossfire, Upheaval }
    public enum DungeonEnemyRole { Ordinary, Protector, Artillery, Leaper, Crusher }

    public static class DungeonWaveTactics
    {
        // A rotated cycle gives every run all four encounters without repeats.
        public static DungeonTactic Select(int seed, int layout, int wave)
            => (DungeonTactic)(((uint)seed + (uint)layout + (uint)System.Math.Max(0, wave - 1)) % 4);
        private static readonly string[] names = { "守卫护援阵", "双翼突袭", "灵能交叉火力", "遗迹地脉异变" };
        public static string Name(DungeonTactic tactic) => names[(int)tactic];
        public static string Hint(DungeonTactic tactic)
            => new[] { "优先击杀护援魔灵，或将守卫引出护援范围", "两翼同时夹击，避开史莱姆的远距跳扑", "打断轰击魔灵的蓄力，利用掩体接近", "地面隆起后爆发；引怪进入裂隙也能伤敌" }[(int)tactic];
        public static DungeonEnemyRole Role(DungeonTactic tactic, EnemyKind kind)
        {
            if (kind == EnemyKind.Wisp && tactic == DungeonTactic.Bulwark) return DungeonEnemyRole.Protector;
            if (kind == EnemyKind.Wisp && tactic == DungeonTactic.Crossfire) return DungeonEnemyRole.Artillery;
            if (kind == EnemyKind.Slime && tactic == DungeonTactic.Pincer) return DungeonEnemyRole.Leaper;
            if (kind == EnemyKind.Guardian && tactic == DungeonTactic.Upheaval) return DungeonEnemyRole.Crusher;
            return DungeonEnemyRole.Ordinary;
        }
    }
}

namespace Emberfall
{
    // Fields/projectiles detach; only actor-driven attacks retain a channel.
    public static class SkillCastingPolicy
    {
        private static readonly float[,] windup = {
            {.16f,.28f,.32f,0,.14f,.12f,.30f,.40f,0,.18f},
            {.42f,.38f,.65f,0,.20f,.14f,.48f,.50f,0,.65f},
            {.14f,.22f,.38f,0,.16f,.26f,.18f,.30f,0,.16f},
            {.20f,.32f,.45f,0,.40f,.22f,.48f,.50f,0,.70f}
        };
        private static readonly float[,] recovery = {
            {.18f,.30f,.20f,0,.16f,.20f,.22f,.32f,0,.38f},
            {.16f,.26f,.22f,0,.14f,.12f,.22f,.26f,0,.35f},
            {.12f,.18f,.22f,0,.16f,.18f,.14f,.20f,0,.28f},
            {.16f,.20f,.24f,0,.22f,.18f,.24f,.26f,0,.35f}
        };
        public static bool Channeled(HeroClass hero,int skill)
            => hero==HeroClass.Vanguard&&skill==2 || hero==HeroClass.Ranger&&skill==7;
        public static float Preparation(HeroClass hero,int skill) => skill==10?.3f:windup[(int)hero,skill];
        public static float Recovery(HeroClass hero,int skill) => skill==10?.18f:recovery[(int)hero,skill];
    }
}

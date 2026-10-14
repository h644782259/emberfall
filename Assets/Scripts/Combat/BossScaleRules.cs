using UnityEngine;
namespace Emberfall
{
    // Actual encounter dimensions, shared by mesh scale, routing and contact.
    public static class BossScaleRules
    {
        public const float BossMultiplier=1.5f,LargeBossMultiplier=1.45f,TreantMultiplier=1.5f;
        internal static bool ReviewLegacyDimensions;
        public static float EnemyMultiplier(bool boss,bool large=false){return !boss||ReviewLegacyDimensions?1:large?LargeBossMultiplier:BossMultiplier;}
        public static float CompanionMultiplier(SummonedCompanion.Kind form){return form==SummonedCompanion.Kind.Treant&&!ReviewLegacyDimensions?TreantMultiplier:1;}
        public static float NavigationRadius(bool boss,bool large,EnemyKind kind){return (large?1.25f:boss?.9f:kind==EnemyKind.Guardian?.44f:.32f)*EnemyMultiplier(boss,large);}
        public static float ProjectileRadius(bool boss,bool large){return (large?1.3f:boss?1.05f:.6f)*EnemyMultiplier(boss,large);}
        public static float HitFootprintBonus(bool boss,bool large){float scale=EnemyMultiplier(boss,large);return (large?.45f:0)*scale+(boss?.85f:0)*(scale-1);}
        public static float BodyHeight(bool boss,bool large,EnemyKind kind){return (boss?large?4.4f:3.8f:kind==EnemyKind.Slime?1.3f:kind==EnemyKind.Wisp?2.3f:2.8f)*EnemyMultiplier(boss,large);}
        public static float SpawnRadius(bool large=false){return NavigationRadius(true,large,EnemyKind.Guardian)+.15f;}
        public static float CompanionNavigation(SummonedCompanion.Kind form){return (form==SummonedCompanion.Kind.Treant?.7f:form==SummonedCompanion.Kind.Wolf?.4f:.35f)*CompanionMultiplier(form);}
        public static float CompanionBodyHeight(SummonedCompanion.Kind form){return (form==SummonedCompanion.Kind.Treant?3.2f:form==SummonedCompanion.Kind.Wolf?1.4f:1.7f)*CompanionMultiplier(form);}
        public static float CompanionBodyRadius(SummonedCompanion.Kind form){return (form==SummonedCompanion.Kind.Treant?.9f:.48f)*CompanionMultiplier(form);}
    }
}

using System;
namespace Emberfall
{
    [Serializable]
    public sealed class CombatReviewConfiguration
    {
        public string id;
        public HeroClass hero;
        public ElementalistSpecialization specialization;
        public int level = 50, seed = 61453;
        public int[] skillRanks = { 1,1,1,1,1,1,1,1,1,1 };
        public int[] masteryRanks = new int[4];
        public int masteryCore = -1;
        public SummonerRoute summonerRoute = SummonerRoute.Bonded;
        public EquipmentMechanic mechanism;
        public string equipment = "starter-equipment; deterministic seed; exact profile saved alongside log";
    }
    public static class CombatReviewConfigurations
    {
        public static CombatReviewConfiguration[] Create()
        {
            return new[] {
                Make("vanguard",HeroClass.Vanguard),
                Make("arcanist-shatter",HeroClass.Arcanist,ElementalistSpecialization.Shatter),
                Make("arcanist-burn",HeroClass.Arcanist,ElementalistSpecialization.Burn),
                Make("ranger",HeroClass.Ranger), Make("summoner-bonded",HeroClass.Summoner)
            };
        }
        // Additive fixtures: retain the five introductory rank-one baselines.
        public static CombatReviewConfiguration[] CreateGrowthBuilds()
        {
            return new[] {
                Growth("growth-vanguard",HeroClass.Vanguard,EquipmentMechanic.ReturningBlade,MasteryType.Offense),
                Growth("growth-shatter",HeroClass.Arcanist,EquipmentMechanic.FrostEcho,MasteryType.Offense,ElementalistSpecialization.Shatter),
                Growth("growth-burn",HeroClass.Arcanist,EquipmentMechanic.CinderTrail,MasteryType.Technique,ElementalistSpecialization.Burn),
                Growth("growth-ranger",HeroClass.Ranger,EquipmentMechanic.VenomSpread,MasteryType.Technique),
                Growth("growth-twin",HeroClass.Summoner,EquipmentMechanic.TwinSummonResonance,MasteryType.Guard),
                Growth("growth-pack",HeroClass.Summoner,EquipmentMechanic.None,MasteryType.Vitality,route:SummonerRoute.Pack)
            };
        }
        public static CombatReviewConfiguration[] CreateAll()
        {
            var baseline=Create();var growth=CreateGrowthBuilds();var all=new CombatReviewConfiguration[baseline.Length+growth.Length];
            Array.Copy(baseline,all,baseline.Length);Array.Copy(growth,0,all,baseline.Length,growth.Length);return all;
        }
        private static CombatReviewConfiguration Growth(string id,HeroClass hero,EquipmentMechanic mechanism,MasteryType core,
            ElementalistSpecialization specialization=ElementalistSpecialization.None,SummonerRoute route=SummonerRoute.Bonded)
        {
            var c=Make(id,hero,specialization);c.skillRanks=new[]{3,3,3,3,3,3,3,3,3,3};
            c.masteryCore=(int)core;c.masteryRanks[c.masteryCore]=10;c.mechanism=mechanism;c.summonerRoute=route;
            c.equipment="production level-50 epic mechanism in its slot; remaining slots fixed starter baseline; zero upgrades; variant A; not optimized";
            if(mechanism==EquipmentMechanic.None)c.equipment="fixed starter equipment in all slots; zero upgrades; no mechanism; not optimized";
            return c;
        }
        private static CombatReviewConfiguration Make(string id,HeroClass hero,ElementalistSpecialization specialization=ElementalistSpecialization.None)
        { return new CombatReviewConfiguration { id=id,hero=hero,specialization=specialization }; }
    }
}

using System;
namespace Emberfall
{
    /// <summary>Bounded rewards from the tier actually completed, never the menu selection.</summary>
    public static class TierRewardRules
    {
        public static int ClampTier(int tier) { return TierRewardBand.Clamp(tier); }
        public static int Band(int tier) { return TierRewardBand.Of(tier); }
        public static int ClearMaterials(int tier) { return TierRewardBand.Materials(3,tier); }
        public static int ChestGoldMinimum(int tier) { return 60 + 25 * Band(ClampTier(tier)); }
        public static int BossMechanicChance(int tier) { return 25 + 3 * Band(ClampTier(tier)); }
        public static int OrdinaryMechanicChance(int tier) { return 12 + 2 * Band(ClampTier(tier)); }
        public static Rarity DropRarity(bool boss, int tier, int roll, bool campOrdinary = false)
        {
            roll=Math.Max(0,Math.Min(99,roll));
            if(boss)return roll<80?Rarity.Rare:Rarity.Epic;
            // Camp ordinary enemies retain 30% of their previous epic chance (10% -> 3%).
            int epicThreshold=campOrdinary&&tier==0?97:90;
            return roll<65?Rarity.Common:roll<epicThreshold?Rarity.Rare:Rarity.Epic;
        }
    }

    public static class SpecialtyChestPity
    {
        public const int Limit=10;
        public static int Target(bool chapter,int mode)
        {return chapter?(mode==0?8:mode==1?7:0):mode==3?2:mode==2?7:0;}
        public static int Target(GameProfile p)
        {return p.pendingChestGemSource?0:Target(p.pendingChestChapterSource,p.pendingChestMode);}
        public static int Misses(GameProfile p,int target){return Misses(p,target,p.pendingChestChapterSource);}
        public static int Misses(GameProfile p,int target,bool chapter)
        {return Math.Max(0,Math.Min(Limit-1,target==8?p.forestCrystalMisses:target==7?(chapter?p.redrockReforgeMisses:p.arenaReforgeMisses):target==2?p.corridorFashionMisses:0));}
        public static string Name(int target){return target==8?"战技源晶":target==7?"词条重铸石":"时装";}
        public static string Caption(GameProfile p,int target){return Caption(p,target,p.pendingChestChapterSource);}
        public static string Caption(GameProfile p,int target,bool chapter)
        {int left=Limit-Misses(p,target,chapter);return left==1?"下一箱必出"+Name(target):"再开 "+left+" 箱内必出"+Name(target);}
        public static string Rules(int target)
        {return "连续未获得"+Name(target)+"时，第10箱必出；获得后重新计数。"+(target==2?"重复时装转为星纹。":"");}
        public static void Record(GameProfile p,int drawnKind)
        {
            int target=Target(p);if(target==0)return;
            int next=drawnKind==target?0:Math.Min(Limit-1,Misses(p,target)+1);
            if(target==8)p.forestCrystalMisses=next;else if(target==7){if(p.pendingChestChapterSource)p.redrockReforgeMisses=next;else p.arenaReforgeMisses=next;}else p.corridorFashionMisses=next;
        }
    }

    // One authoritative clear-loot table shared by previews and committed rewards.
    public static class AdventureRewardRules
    {
        public const int DuplicateGemMaterials=3;
        public static bool ChestFashion(int mode,bool chapter){return !chapter&&mode==3;}
        // Chapter chests retain the chapter slot mapping: Forest=0, Redrock=1, StarPlatform=2.
        public static bool ChestAffixReforge(int mode,bool chapter){return chapter?mode==1:mode==2;}
        // Keep the stored difficulty index for save compatibility; gameplay uses its fixed level.
        public static int DungeonLevel(int index){return Math.Max(1,Math.Min(10,index))*10;}
        public static int MaximumDungeonIndex(int characterLevel){return Math.Max(1,Math.Min(10,(Math.Max(1,characterLevel)+10)/10));}
        public static bool UsesTierDifficulty(int mode){return mode==2||mode==3;}
        public static int MaximumSelection(int mode,int level,int unlocked)
        {return Math.Max(1,Math.Min(UsesTierDifficulty(mode)?100:MaximumDungeonIndex(level),unlocked));}
        public static string DifficultyName(int mode,int tier)
        {return UsesTierDifficulty(mode)?"第 "+TierRewardBand.Clamp(tier)+" 阶":"Lv"+DungeonLevel(tier);}
        public static int EntryEnemyLevel(int mode,int tier,int characterLevel)
        {return UsesTierDifficulty(mode)?((Math.Max(1,Math.Min(100,characterLevel))+9)/10)*10:DungeonLevel(tier);}

        public static EquipmentMechanic ExclusiveGem(int mode)
        {
            Validate(mode);
            return new[]{EquipmentMechanic.ReturningBlade,EquipmentMechanic.TwinSummonResonance,EquipmentMechanic.CinderTrail,EquipmentMechanic.FrostEcho,EquipmentMechanic.VenomSpread}[mode+1];
        }

        public static int EquipmentCount(int mode){Validate(mode);return mode==3?2:1;}
        public static int EquipmentCount(int mode,int tier){return EquipmentCount(mode)+TierRewardBand.Of(tier)/2;}
        public static int EnemyEquipmentChance(bool boss,bool elite){return boss?100:5;}
        public static int PotionChance(int tier){return 12+3*TierRewardBand.Of(tier);}
        public const int LegendaryPityChests=30;
        // Ordinary saves store a level index (1 = Lv10); chapters store their tier.
        public static int LegendaryChance(int tier){return Math.Min(25,4+TierRewardBand.Clamp(tier)-1);}
        private static void Validate(int mode){if(mode < -1 || mode > 3)throw new ArgumentOutOfRangeException(nameof(mode));}
        public static ItemSlot EquipmentSlot(int mode,int index)
        {Validate(mode);if(index<0||index>=8)throw new ArgumentOutOfRangeException(nameof(index));return mode==0?ItemSlot.Armor:mode==1?ItemSlot.Relic:mode==3?(index%2==0?ItemSlot.Armor:ItemSlot.Relic):ItemSlot.Weapon;}
        public static Rarity MinimumRarity(int mode){Validate(mode);return Rarity.Epic;}
        public static int UpgradeChance(int mode,int tier){Validate(mode);return 100-LegendaryChance(tier);}
        public static Rarity EquipmentRarity(int mode,int tier,int roll)
        {Validate(mode);return roll<LegendaryChance(tier)?Rarity.Legendary:MinimumRarity(mode);}
        public static int MaterialsMinimum(int mode,int tier){return Math.Max(1,Materials(mode,tier)-2);}
        public static int MaterialsMaximum(int mode,int tier){return Materials(mode,tier)+2;}
        public static int ChestMaterials(int mode,int tier,string id)
        {
            int seed=17;unchecked{foreach(char c in id??"")seed=seed*31+c;}
            return new Random(seed).Next(MaterialsMinimum(mode,tier),MaterialsMaximum(mode,tier)+1);
        }
        public static int Materials(int mode,int tier){Validate(mode);return (mode==0||mode==1?5:mode==3?4:3)+TierRewardBand.Of(tier);}
        public static int Gold(int mode,int tier,bool riskContract)
        {Validate(mode);tier=TierRewardBand.Clamp(tier);int value=mode==-1?120+tier*30:mode==0?110+tier*20:mode==1?100+tier*25:mode==2?180+tier*40:200+tier*35;return riskContract?(int)Math.Round(value*1.3f):value;}
        public static int Experience(int mode,int tier)
        {Validate(mode);tier=TierRewardBand.Clamp(tier);return mode==-1?100+tier*20:mode==0?90+tier*20:mode==1?100+tier*20:mode==2?140+tier*30:160+tier*25;}
        public static string EquipmentSummary(int mode,int tier)
        {Validate(mode);string slot=mode==0?"护甲":mode==1?"饰品":mode==3?"护甲 + 饰品":"武器";return "保底 史诗 "+slot+" · "+LegendaryChance(tier)+"% 传说 · "+UpgradeChance(mode,tier)+"% 史诗";}

    }
}

namespace Emberfall
{
    // Reachability is enumerated from the same integer rolls used by actual loot.
    public static class DropPreviewRules
    {
        public static Rarity[] ClearRarities(int mode,int tier)
        {
            var found=new System.Collections.Generic.SortedSet<Rarity>();
            for(int roll=0;roll<100;roll++)found.Add(AdventureRewardRules.EquipmentRarity(mode,tier,roll));
            return new System.Collections.Generic.List<Rarity>(found).ToArray();
        }
        public static Rarity[] EnemyRarities(int tier,bool hasBoss,bool mechanic)
        {
            var found=new System.Collections.Generic.SortedSet<Rarity>();
            for(int roll=0;roll<100;roll++)
            {
                var ordinary=TierRewardRules.DropRarity(false,tier,roll);
                if(!mechanic||ordinary>=Rarity.Epic)found.Add(ordinary);
                if(hasBoss)found.Add(TierRewardRules.DropRarity(true,tier,roll));
            }
            return new System.Collections.Generic.List<Rarity>(found).ToArray();
        }
        public static int SlotCount(int mode,int tier,ItemSlot slot)
        {
            int count=0;for(int i=0;i<AdventureRewardRules.EquipmentCount(mode,tier);i++)
                if(AdventureRewardRules.EquipmentSlot(mode,i)==slot)count++;
            return count;
        }
    }
}

using System;
using System.Collections.Generic;
namespace Emberfall
{
    public partial class ProgressionService
    {
        public IEnumerable<AchievementDefinition> AvailableAchievements
        {
            get
            {
                foreach(var a in Achievements)yield return a;
                for(int n=0;n<3;n++)
                {
                    int claimed=Profile.revivalClaimedTiers[n],best=ChapterProgression.CompletedTier(Profile,(ChapterNode)n);
                    int pending=Math.Max(0,best-claimed);
                    yield return new AchievementDefinition("revival/"+n,ChapterDefinition.Get((ChapterNode)n).Name+" · 新阶首通"+(pending>1?"（"+pending+"阶待领取）":""),2,claimed==int.MaxValue?int.MaxValue:claimed+1,0,0){RevivalStones=(int)Math.Min(999999L,Math.Max(1L,pending)*3)};
                }
            }
        }
        private bool ApplyAchievementReward(GameProfile candidate,AchievementDefinition a)
        {
            if(candidate.revivalStones>999999-a.RevivalStones||candidate.refinementStones>999999-a.Refinement||candidate.affixReforgeStones>999999-a.Reforge)return Fail("道具空间已满，请使用后领取。");
            if(a.Id.StartsWith("revival/"))
            {
                int node=int.Parse(a.Id.Substring(8));int best=ChapterProgression.CompletedTier(candidate,(ChapterNode)node);
                long due=((long)best-candidate.revivalClaimedTiers[node])*3;
                if(due<=0||due>999999-candidate.revivalStones)return Fail("暂无奖励或复活石空间不足。");
                candidate.revivalStones+=(int)due;candidate.revivalClaimedTiers[node]=best;return true;
            }
            if(a.GearSlot>=0)
            {
                if(candidate.inventory.Count+candidate.pendingLoot.Count+candidate.recoveryLoot.Count>=MaximumSavedEquipment)return Fail("装备空间已满，请整理后领取。");
                var slot=(ItemSlot)a.GearSlot;var item=new ItemData{id=Guid.NewGuid().ToString("N"),slot=slot,rarity=a.GearRarity,level=EquipmentGenerationLevel(candidate.level),name=(a.GearRarity==Rarity.Legendary?"烬王":"星辉")+ItemBaseName(slot,candidate.heroClass)};
                SetRolledStats(item);EnsureUpgradeBasis(item);candidate.inventory.Add(item);
            }
            if(a.FashionSlotIndex>=0)
            {
                var slot=(FashionSlot)a.FashionSlotIndex;string id="fashion-"+(int)slot+"-3";
                if(candidate.fashions.Exists(f=>f.id==id))
                {if(candidate.fashionThreads>999991)return Fail("星纹已满，请使用后领取。");candidate.fashionThreads+=8;}
                else candidate.fashions.Add(new FashionData{id=id,slot=slot,rarity=Rarity.Legendary,appearanceTier=3,name=FashionName(slot,Rarity.Legendary,candidate.heroClass)});
            }
            candidate.achievementReceipts.Add(a.Id);candidate.gold+=a.Gold;candidate.mechanicMaterials+=a.Shards;
            candidate.refinementStones+=a.Refinement;candidate.affixReforgeStones+=a.Reforge;return true;
        }
    }
}

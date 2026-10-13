using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private List<EntryRewardPreview> AchievementRewardPreviews(AchievementDefinition a)
        {
            var result=new List<EntryRewardPreview>();
            if(a.Gold>0)result.Add(ResourceItemPreview("gold",a.Gold));
            if(a.Shards>0)result.Add(ResourceItemPreview("shard",a.Shards));
            if(a.Refinement>0)result.Add(ResourceItemPreview("refinement",a.Refinement));
            if(a.Reforge>0)result.Add(ResourceItemPreview("affix-reforge",a.Reforge));
            if(a.RevivalStones>0)result.Add(ResourceItemPreview("revival",a.RevivalStones));
            var hero=session.Progression.Profile.heroClass;
            if(a.GearSlot>=0)
            {
                var slot=(ItemSlot)a.GearSlot;int level=ProgressionService.EquipmentGenerationLevel(session.Progression.Profile.level);
                result.Add(new EntryRewardPreview{Key="achievement-gear:"+a.Id,GearSlot=slot,Rarity=a.GearRarity,Name=GameBalance.SlotName(slot),Icon=UIIconAtlas.EquipmentCardIcon(slot,level,a.GearRarity,hero),Description=ProgressionService.EquipmentDropPreview(slot,a.GearRarity,level)+"\n领取时按当前等级生成。"});
            }
            if(a.FashionSlotIndex>=0)
            {
                var slot=(FashionSlot)a.FashionSlotIndex;
                bool owned=session.Progression.Profile.fashions.Exists(f=>f.id=="fashion-"+(int)slot+"-3");
                if(owned)result.Add(ResourceItemPreview("thread",8));
                else result.Add(new EntryRewardPreview{Key="fashion:achievement:"+a.Id,AppearanceSlot=slot,Rarity=Rarity.Legendary,Name=ProgressionService.FashionName(slot,Rarity.Legendary,hero),Icon=UIIconAtlas.FashionCardIcon(slot,3,hero),Description=ProgressionService.FashionBonus(slot,Rarity.Legendary)+"\n重复拥有时转为星纹 ×8。"});
            }
            return result;
        }
    }
}

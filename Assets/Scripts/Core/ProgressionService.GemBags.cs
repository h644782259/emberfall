using System;
using System.Collections.Generic;
namespace Emberfall
{
    public partial class ProgressionService
    {
        public static EquipmentMechanic[] GemBagPool(HeroClass hero,ItemSlot slot)
        {var pool=new List<EquipmentMechanic>();foreach(var kind in BuildCatalog.GemsFor(hero))if(BuildCatalog.MechanicSlot(kind)==slot)pool.Add(kind);return pool.ToArray();}
        public const int GemBagPrice=100;
        public static Rarity GemBagRarity(int roll)
        {return roll<45?Rarity.Common:roll<75?Rarity.Rare:roll<95?Rarity.Epic:Rarity.Legendary;}
        public static int GemBagDuplicateRefund(Rarity rarity){return new[]{20,35,60,100}[(int)rarity];}
        public string GemBagPurchaseLock(ItemSlot slot,bool atMerchant)
        {
            if(!atMerchant||IsPracticeOnly||(int)slot<0||(int)slot>2)return "请在商店兑换";
            if(Profile.gemBags[(int)slot]>=999999)return "数量已满";
            return Profile.mechanicMaterials<GemBagPrice?"碎片不足":"";
        }
        public bool BuyGemBag(ItemSlot slot,bool atMerchant)
        {
            string reason=GemBagPurchaseLock(slot,atMerchant);if(reason.Length>0)return Fail(reason);
            var candidate=Snapshot();candidate.mechanicMaterials-=GemBagPrice;candidate.gemBags[(int)slot]++;
            return CommitCandidate(candidate);
        }
        public bool OpenGemBag(ItemSlot slot,out string result)
        {
            result="";
            if(IsPracticeOnly||(int)slot<0||(int)slot>2||Profile.gemBags[(int)slot]<=0)return Fail("没有可开启的宝石袋。");
            var pool=GemBagPool(Profile.heroClass,slot);
            if(pool.Length==0)return Fail("宝石袋内容不可用。");
            var roll=new Random(unchecked(Profile.gemBagSeed+Profile.gemBagsOpened*397));
            var rarity=GemBagRarity(roll.Next(100));var mechanic=pool[roll.Next(pool.Length)];
            var candidate=Snapshot();var gem=candidate.attachments.Find(a=>a.mechanic==mechanic);
            bool duplicate=gem!=null&&gem.rarity>=rarity;int refund=duplicate?GemBagDuplicateRefund(rarity):0;
            if(candidate.mechanicMaterials>999999-refund)return Fail("碎片已满，请先使用碎片。");
            candidate.gemBags[(int)slot]--;candidate.gemBagsOpened=candidate.gemBagsOpened==int.MaxValue?0:candidate.gemBagsOpened+1;
            if(gem==null){gem=new MechanicAttachment{id=Guid.NewGuid().ToString("N"),mechanic=mechanic,rarity=rarity,level=EquipmentGenerationLevel(Profile.level),ascensionRank=0};candidate.attachments.Add(gem);}
            else if(!duplicate)gem.rarity=rarity;
            candidate.mechanicMaterials+=refund;
            if(!candidate.discoveredMechanics.Contains(mechanic))candidate.discoveredMechanics.Add(mechanic);
            if(!CommitCandidate(candidate,true))return false;
            result=GameBalance.RarityName(rarity)+" · "+BuildCatalog.GemName(mechanic)+(duplicate?" · 重复转为"+refund+"碎片":"");
            PublishRewardMoment(RewardMomentKind.MechanicExchange,materials:refund,attachment:duplicate?null:Attachment(mechanic));
            return true;
        }
    }
}

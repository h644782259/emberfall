using System;
using Emberfall;
public static class EquipmentComparisonPresentationTests
{
    static int checks;
    static void Check(bool yes,string message){checks++;if(!yes)throw new Exception(message);}
    public static string Run()
    {
        checks=0;
        foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass)))
        foreach(EquipmentMechanic mechanic in Enum.GetValues(typeof(EquipmentMechanic)))
        foreach(ItemSlot slot in Enum.GetValues(typeof(ItemSlot)))
        {
            var item=new ItemData{mechanic=mechanic,slot=slot};
            bool active=mechanic!=EquipmentMechanic.None&&BuildCatalog.MechanicClass(mechanic)==hero&&BuildCatalog.MechanicSlot(mechanic)==slot;
            Check((EquipmentComparisonPresentation.ActiveMechanic(item,hero)!=EquipmentMechanic.None)==active,"mechanism class and slot gate");
            Check(EquipmentComparisonPresentation.SameMechanism(item,item,hero),"same item retains mechanism");
        }
        var frost=new ItemData{mechanic=EquipmentMechanic.FrostEcho,slot=ItemSlot.Relic};
        var alternate=new ItemData{mechanic=EquipmentMechanic.FrostEcho,slot=ItemSlot.Relic,mechanicVariantUnlocked=true,mechanicVariant=1};
        Check(!EquipmentComparisonPresentation.SameMechanism(frost,alternate,HeroClass.Arcanist),"actual unlocked variant change is visible");
        alternate.mechanicVariantUnlocked=false;
        Check(EquipmentComparisonPresentation.SameMechanism(frost,alternate,HeroClass.Arcanist),"unavailable variant cannot falsely appear active");
        string loss=EquipmentComparisonPresentation.Changes(frost,null,HeroClass.Arcanist);
        Check(loss.Contains("失去")&&loss.Contains(BuildCatalog.MechanicName(frost.mechanic)),"loss remains explicit even when candidate has no mechanism");
        Check(EquipmentComparisonPresentation.Changes(null,frost,HeroClass.Arcanist).Contains("获得"),"empty slot gains mechanism");
        Check(EquipmentComparisonPresentation.ActiveMechanic(new ItemData{mechanic=(EquipmentMechanic)999},HeroClass.Arcanist)==EquipmentMechanic.None,"invalid mechanism safe");
        foreach(FashionSlot slot in Enum.GetValues(typeof(FashionSlot)))
        foreach(Rarity rarity in Enum.GetValues(typeof(Rarity)))
        {
            var receipt=new ChestReward{slotIndex=(int)slot,rarityIndex=(int)rarity,id="saved-receipt",gold=20,duplicate=true};
            var preview=EquipmentComparisonPresentation.Receipt(receipt);
            Check(preview!=null&&preview.slot==slot&&preview.rarity==rarity,"exact durable reward slot and rarity drive real-model appearance");
            Check(receipt.id=="saved-receipt"&&receipt.gold==20&&receipt.duplicate,"preview never mutates reward receipt");
            var second=EquipmentComparisonPresentation.Trial(slot,rarity);preview.rarity=Rarity.Common;
            Check(second.rarity==rarity,"trial data independent between callers");
        }
        Check(EquipmentComparisonPresentation.Receipt(new ChestReward{gold=42})==null,"gold-only receipt has no invented fashion");
        Check(EquipmentComparisonPresentation.Receipt(new ChestReward{slotIndex=999,rarityIndex=2})==null,"invalid receipt cannot invent asset");
        Check(EquipmentComparisonPresentation.CollectionState(false,false,false).Contains("仅试穿"),"unowned trial is labelled");
        Check(EquipmentComparisonPresentation.CollectionState(true,false,true).Contains("属性来源"),"unworn highest collection remains stat source");
        Check(EquipmentComparisonPresentation.CollectionState(true,true,false).Contains("非属性来源"),"wearing a lower rarity does not imply its bonus is used");
        return "PASS: "+checks+" equipment mechanism and collection/receipt presentation assertions";
    }
}

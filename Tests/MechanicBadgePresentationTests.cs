using System;
using Emberfall;
public static class MechanicBadgePresentationTests
{
 public static string Run(){int checks=0;Action<bool,string> check=(ok,message)=>{checks++;if(!ok)throw new Exception(message);};
 foreach(EquipmentMechanic mechanic in Enum.GetValues(typeof(EquipmentMechanic)))foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass)))foreach(ItemSlot slot in Enum.GetValues(typeof(ItemSlot))){
  var item=new ItemData{mechanic=mechanic,slot=slot};bool active=EquipmentComparisonPresentation.ActiveMechanic(item,hero)!=EquipmentMechanic.None;
  check(MechanicBadgePresentation.Benefit(item,hero).StartsWith("无")==!active,"benefit follows actual class/slot eligibility");
  check(MechanicBadgePresentation.Cost(item,hero).StartsWith("无")==!active,"cost follows same eligibility");
  check(MechanicBadgePresentation.Title(item,hero)==EquipmentComparisonPresentation.Label(item,hero),"badge shares actual mechanic identity");
 }
 foreach(var mechanic in new[]{EquipmentMechanic.FrostEcho,EquipmentMechanic.CinderTrail}){
  var item=new ItemData{mechanic=mechanic,slot=BuildCatalog.MechanicSlot(mechanic),mechanicVariant=1};var hero=BuildCatalog.MechanicClass(mechanic);
  string benefit=MechanicBadgePresentation.Benefit(item,hero),cost=MechanicBadgePresentation.Cost(item,hero);
  item.mechanicVariantUnlocked=true;check(MechanicBadgePresentation.Benefit(item,hero)!=benefit,"unlocked B benefit visible");check(MechanicBadgePresentation.Cost(item,hero)!=cost,"unlocked B tradeoff visible");
  item.mechanicVariant=0;check(MechanicBadgePresentation.Benefit(item,hero)==benefit&&MechanicBadgePresentation.Cost(item,hero)==cost,"return A restores original summary");
 }
 check(MechanicBadgePresentation.Benefit(null,HeroClass.Vanguard).StartsWith("无"),"empty slot has no invented benefit");
 return "PASS: "+checks+" mechanic badge eligibility/variant assertions";
 }
}

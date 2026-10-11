using System;
using Emberfall;
public static class BlessingDamageBudgetTests
{
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}static bool Near(double a,double b)=>Math.Abs(a-b)<.00002;
 public static RunChoices Reach(HeroClass hero,RunBlessing first,RunBlessing? second=null){var p=new GameProfile{heroClass=hero};
  for(int seed=0;seed<1024;seed++){var run=new RunChoices();run.PrepareRoomChoice(1,p,true,seed);int index=Array.IndexOf(run.Offer,first);if(index<0)continue;run.Choose(index);if(!second.HasValue)return run;
   // Replay the real first choice for each second-room seed; no reflection or direct active-set edits.
   for(int next=0;next<256;next++){var pair=new RunChoices();pair.PrepareRoomChoice(1,p,true,seed);pair.Choose(index);pair.PrepareRoomChoice(2,p,true,next);int chosen=Array.IndexOf(pair.Offer,second.Value);if(chosen>=0){pair.Choose(chosen);return pair;}}
  }throw new Exception("unreachable actual room combination: "+hero+"/"+first+"/"+second);}
 static double Mean(RunChoices run){double total=0;for(int i=0;i<1000;i++)total+=CombatDamage.Roll(100*run.AttackMultiplier,run.CritChance(.1f),(i+.5f)/1000,run.CriticalMultiplier).Amount;return total/100000;}
 public static string Run(){checks=0;double basis=Mean(new RunChoices());Check(Near(basis,1.065),"base 10% crit/165% unchanged");
  foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass))){
   var keen=Reach(hero,RunBlessing.KeenSight);var deadly=Reach(hero,RunBlessing.DeadlyEdge);var both=Reach(hero,RunBlessing.KeenSight,RunBlessing.DeadlyEdge);var fervor=Reach(hero,RunBlessing.BattleFervor);var mix=Reach(hero,RunBlessing.BattleFervor,RunBlessing.KeenSight);
   Check(Near(Mean(keen)/basis-1,.1220657277),"Keen expected direct-damage gain 12.21%");Check(Near(Mean(deadly)/basis-1,.0469483568),"Deadly gain 4.69%");Check(Near(Mean(both)/basis-1,.2629107981),"both crit cards gain 26.29%");Check(Near(Mean(fervor)/basis-1,.1),"Fervor gain exactly10%");Check(Near(Mean(mix)/basis-1,.2342723005),"Fervor+Keen gain23.43%");
   Check(Near(keen.CritChance(.75f),.8)&&Near(keen.CritChance(.95f),.95),"80% blessing cap never lowers existing higher crit");Check(Near(keen.CritChance(float.NaN),.2)&&Near(keen.CritChance(-1),.2),"invalid base chance sanitizes before addition");
   Check(Near(both.AttackMultiplier,1)&&Near(fervor.CriticalMultiplier,1.65),"crit and attack multipliers do not cross-inherit");
   fervor.Reset();Check(Near(fervor.AttackMultiplier,1)&&Near(fervor.CritChance(.1f),.1),"leave/reset clears effects");
  }
  Check(RunChoices.Description(RunBlessing.KeenSight).Contains("+20个百分点")&&RunChoices.Description(RunBlessing.DeadlyEdge).Contains("215%"),"cards disclose real values");
  Check(RunChoices.Description(RunBlessing.DeadlyEdge).Contains("玩家普攻")&&RunChoices.Description(RunBlessing.BattleFervor).Contains("伙伴伤害+10%（仅一次）"),"owner direct vs partner scope explicit");
  return "PASS: "+checks+" reachable blessing direct-damage budget checks (fixed rolls, not real DPS)";
 }
}

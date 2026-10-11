using System;
using Emberfall;
public static class AdventureEntryPresentationTests
{
 public static string Run()
 {
  int checks=0;Action<bool,string> check=(ok,m)=>{checks++;if(!ok)throw new Exception(m);};
  for(int tier=1;tier<=100;tier++)
  {
   check(AdventureEntryPresentation.Materials(-1,tier)==TierRewardRules.ClearMaterials(tier),"ordinary entry shares clear reward");
   for(int mode=0;mode<3;mode++)check(AdventureEntryPresentation.Materials(mode,tier)==new ExpeditionModeState((ExpeditionModeKind)mode,tier,1).Reward.Materials,"trial entry agrees with actual reward ticket");
   check(AdventureEntryPresentation.Materials(3,tier)==TierRewardBand.Materials(4,tier),"chain entry agrees with current reward contract");
   for(int mode=-1;mode<=3;mode++)
   {
    check(AdventureEntryPresentation.RewardLine(mode,tier).Contains("碎片"),"guaranteed materials visible");
    check(AdventureEntryPresentation.EncounterLine(mode).Contains("随机装备"),"random gear is not a guaranteed targeted core");
    check(AdventureEntryPresentation.RewardLine(mode,tier).Contains("无外观宝箱")== (mode!=-1),"only ordinary clear awards cosmetic chest");
   }
  }
  check(AdventureEntryPresentation.Materials(0,1)==1,"hold reward proposal does not silently change economy");
  var profile=new GameProfile{progressionGoal=ProgressionGoalKind.Core,mechanicMaterials=3};
  var goal=new ProgressionGoalState{Identity="Core/2/0",Title="具体核心",MaterialCost=8};
  check(AdventureEntryPresentation.GoalFit(profile,goal,0,1).Contains("保底1")&&AdventureEntryPresentation.GoalFit(profile,goal,0,1).Contains("仍缺 5"),"fit reports exact guaranteed materials and current deficit");
  goal.MaterialCost=0;profile.progressionGoal=ProgressionGoalKind.SecondPreset;
  check(AdventureEntryPresentation.GoalFit(profile,goal,3,40).Contains("保存两套"),"high tier never pretends to complete a preset target");
  profile.progressionGoal=ProgressionGoalKind.Tier;profile.progressionGoalTier=20;
  check(!AdventureEntryPresentation.GoalFit(profile,goal,1,10).Contains("可达成")&&AdventureEntryPresentation.GoalFit(profile,goal,1,20).Contains("可达成"),"fit uses selected tier and target identity");
  check(goal.Identity=="Core/2/0"&&profile.mechanicMaterials==3,"preview has no progression mutations");
  return "PASS: "+checks+" entry reward truth assertions (no completion-time or player-experience claim)";
 }
}

using System;using System.IO;using UnityEngine;
namespace Emberfall
{
 public sealed partial class GameUI
 {
  public static string VerifyEconomyGoalUi(string root)
  {
   int count=0;Action<bool,string> check=(ok,message)=>{count++;if(!ok)throw new Exception(message);};
   var p=new ProgressionService(Path.Combine(root,"goal-ui"));check(p.CreateNewSlot(HeroClass.Arcanist),"create UI character");
   var textUi=new GameUI{session=new Context{Progression=p}};
   string expectedTiers="30/35/50/65/80/95级 → 上限5/10/15/20/30/35点";
   check(textUi.ReplayDesktopMastery().Contains(expectedTiers),"desktop mastery text includes every production tier and cap");
   check(textUi.ReplayMobileMastery().Contains(expectedTiers),"mobile mastery shares all tier text");
   foreach(MasteryType mastery in Enum.GetValues(typeof(MasteryType)))check(BuildCatalog.MasteryDescription(mastery).Contains(expectedTiers),"all mastery detail hints share tier text");
   int[] starts={30,35,50,65,80,95},caps={5,10,15,20,30,35};
   for(int level=1;level<=100;level++){int expected=0;for(int i=0;i<starts.Length;i++)if(level>=starts[i])expected=caps[i];check(ProgressionService.MasteryCap(level)==expected,"service cap remains unchanged at every level");}
   p.Profile.level=10;p.Profile.gold=9999;p.Save();var item=p.CreateMechanicItem(EquipmentMechanic.FrostEcho);check(p.CollectLoot(item),"collect reforge item");
   p.Profile.level=20;p.Save();var ui=new GameUI{session=new Context{Progression=p}};ui.OpenProgressionGoals();
   ui.click="重铸至 20 级";ui.DrawProgressionGoalOptions(520,1,true);
   check(p.Profile.progressionGoalLevel==20,"actual candidate click captures level20");ui.shown.Clear();ui.DrawProgressionGoalOptions(520,1,true);
   check(ui.shown.Exists(x=>x.StartsWith("重铸至 20 级")&&x.EndsWith(" ✓")),"same complete candidate identity is selected");
   p.Profile.level=50;p.Save();string before=JsonUtility.ToJson(p.Profile,true),disk=File.ReadAllText(p.SaveFilePath);int events=0;p.Changed+=()=>events++;
   ui.shown.Clear();ui.DrawProgressionGoalSurface();
   check(ui.shown.Exists(x=>x.StartsWith("重铸至 50 级")&&!x.EndsWith(" ✓"))&&!ui.shown.Exists(x=>x.StartsWith("重铸至 50 级")&&x.EndsWith(" ✓")),"upgraded candidate must not claim fixed level20 selection");
   var fixedGoal=p.SelectedProgressionGoal(true);check(fixedGoal.ReforgeQuote.TargetLevel==20&&fixedGoal.GoldCost==510,"header retains fixed20 quote510");
   check(ui.shown.Exists(x=>x.Contains(fixedGoal.Title)&&x.Contains("510")),"actual current header displays fixed target quote");
   ui.session.IsInCamp=false;ui.shown.Clear();ui.DrawProgressionGoalSurface();
   check(ui.shown.Exists(x=>x.Contains("营地")&&!x.Contains("营地已到达")),"field header names camp prerequisite");
   check(!ui.CurrentProgressionGoalStatus().Contains("营地已到达")&&ui.CurrentProgressionGoalStatus()==p.ProgressionGoalStatus(0,false),"field formatter must use actual camp context");
   check(ui.ReplayRecapDraw(7)==p.ProgressionGoalStatus(7,false)&&ui.ReplayRecapMeasure(7)==ui.ReplayRecapDraw(7)&&!ui.ReplayRecapDraw(7).Contains("营地已到达"),"field recap draw and measure use actual context");
   check(!p.SelectedProgressionGoal(false).CanAct,"field action remains disabled");
   ui.session.IsInCamp=true;
   check(ui.CurrentProgressionGoalStatus().Contains("营地已到达"),"camp formatter reports reached camp");
   check(ui.ReplayRecapDraw(7)==p.ProgressionGoalStatus(7,true)&&ui.ReplayRecapDraw(7).Contains("本局 +7")&&ui.ReplayRecapMeasure(7)==ui.ReplayRecapDraw(7),"camp recap shares formatter and real gain");
   check(events==0&&before==JsonUtility.ToJson(p.Profile,true)&&disk==File.ReadAllText(p.SaveFilePath),"all presentations leave stored target and economy unchanged");
   ui.click="重铸至 50 级";ui.DrawProgressionGoalOptions(520,1,true);check(p.Profile.progressionGoalLevel==50&&p.SelectedProgressionGoal(true).GoldCost==3240&&events==1,"explicit new candidate alone changes target to50");
   return "PASS: "+count+" actual goal UI/context/recap-call assertions (managed GUI, not rendering)";
  }
 }
}

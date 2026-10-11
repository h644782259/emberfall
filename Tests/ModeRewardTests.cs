using System;using System.IO;using Emberfall;
public static class ModeRewardTests
{
 static int n;static void Check(bool b,string m){n++;if(!b)throw new Exception(m);}
 public static string Run(string root)
 {
  n=0;var p=new ProgressionService(Path.Combine(root,"mode-reward-"+Guid.NewGuid().ToString("N")));p.CreateNewSlot(HeroClass.Vanguard);
  string receipt=Guid.NewGuid().ToString("N"),slot=p.CurrentSlotId;int oldGold=p.Profile.gold,clears=p.Profile.clearedRuns,best=p.Profile.bestFloor;
  Check(p.TryGrantModeReward(receipt,150,0,2),"mode reward durable");Check(p.Profile.gold==oldGold+150&&p.Profile.mechanicMaterials==2,"exact receipt amounts");
  for(int i=0;i<5;i++)Check(p.TryGrantModeReward(receipt,150,0,2),"same receipt retry");Check(p.Profile.gold==oldGold+150&&p.Profile.mechanicMaterials==2,"no duplicate payout");
  Check(p.Profile.clearedRuns==clears&&p.Profile.bestFloor==best,"mode does not fake ordinary dungeon completion");
  p.LoadSlot(slot);Check(p.TryGrantModeReward(receipt,150,0,2)&&p.Profile.gold==oldGold+150,"receipt survives reload");
  string next=Guid.NewGuid().ToString("N"),bytes=File.ReadAllText(p.SaveFilePath);int before=p.Profile.gold;
  Directory.CreateDirectory(p.SaveFilePath+".tmp");Check(!p.TryGrantModeReward(next,99,100,1),"failed write blocks grant");Check(p.Profile.gold==before&&p.Profile.lastModeRewardId==receipt&&File.ReadAllText(p.SaveFilePath)==bytes,"failure retains profile and disk");Directory.Delete(p.SaveFilePath+".tmp");
  Check(p.TryGrantModeReward(next,99,100,1)&&p.Profile.gold==before+99,"reward can retry after failure");
  Check(!p.TryGrantModeReward("../bad",99,0,1)&&!p.TryGrantModeReward(Guid.NewGuid().ToString("N"),10001,0,1),"invalid receipts/budgets rejected");
  return n+" atomic mode reward/receipt assertions passed";
 }
}

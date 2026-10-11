using System;using System.IO;using System.Linq;using Emberfall;
public static class StartingSkillBudgetTests
{
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static int Spent(GameProfile p)=>p.skillRanks.Sum()+p.masteryRanks.Sum();
 static void Budget(ProgressionService p){Check(p.Profile.skillPoints+Spent(p.Profile)==GameBalance.SkillPointBudget(p.Profile.level),"shared lifetime budget conserved");}
 public static string Run(string root)
 {
  foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass)))
  {
   var p=new ProgressionService(Path.Combine(root,hero.ToString()));p.NewGame(hero);
   Check(p.Profile.skillRanks[0]==1&&p.Profile.skillPoints==0&&p.Profile.equippedSkills.Contains(0),"every class starts with usable first active rank one");Budget(p);
   p.Profile.skillRanks[0]=0;p.Save();Check(p.Load()&&p.Profile.skillRanks[0]==1,"old unlearned level one migrated once");
   for(int i=0;i<3;i++){p.Save();Check(p.Load(),"repeat persistence");Budget(p);}
   p.GrantExperience(GameBalance.XpToNext(1));Check(p.Profile.level==2&&p.Profile.skillPoints==0,"level two adds no extra point");
   p.GrantExperience(GameBalance.XpToNext(2));Check(p.Profile.level==3&&p.Profile.skillPoints==1,"level three gains one point");Budget(p);
   Check(GameBalance.SkillRankRequiredLevel(0,1)==1&&GameBalance.SkillRankRequiredLevel(0,2)==10&&GameBalance.SkillRankRequiredLevel(0,3)==20,"starting skill rank upgrades remain ten and twenty");
   Check(p.SaveBuildPreset(0,true)&&p.ApplyBuildPreset(0,true),"presets retain starting allocation");p.RefundSkillRanks(true);p.ResetMastery(true);Budget(p);
   p.Profile.level=2;p.Profile.skillRanks=new int[GameBalance.SkillCount];p.Save();Check(p.Load()&&p.Profile.skillRanks[0]==0&&p.Profile.skillPoints==1,"legacy level two unspent allocation preserved");
   p.Profile.level=10;p.Profile.skillRanks[0]=2;p.Profile.skillRanks[1]=1;p.Save();Check(p.Load()&&p.Profile.skillRanks[0]==2&&p.Profile.skillRanks[1]==1&&p.Profile.skillPoints==6,"legacy invested higher levels preserved");
   p.NewGame(hero);Check(p.Profile.skillRanks[0]==1&&p.Profile.skillPoints==0,"reset reinitializes exactly one starting rank");
  }
  for(int mode=0;mode<4;mode++)
  {
   var p=new ProgressionService(Path.Combine(root,"reward"+mode));p.NewGame(HeroClass.Vanguard);
   int xp=GameBalance.XpToNext(1)+GameBalance.XpToNext(2);
   if(mode==0)p.GrantExperience(xp);else if(mode==1)p.GrantEnemyKillReward(0,xp);
   else if(mode==2)Check(p.TryGrantModeReward(Guid.NewGuid().ToString("N"),0,xp,0),"mode reward");
   else Check(p.TryCompleteDungeonRun(Guid.NewGuid().ToString("N"),1,0,xp),"dungeon reward");
   Check(p.Profile.level==3&&p.Profile.skillPoints==1,"each XP entry shares level budget delta");Budget(p);
  }
  var chapter=new ProgressionService(Path.Combine(root,"chapter"));chapter.NewGame(HeroClass.Vanguard);ChapterRunReceipt receipt;
  Check(chapter.TryBeginChapterNode(ChapterNode.ForestCourt,ChapterDifficulty.Normal,1,out receipt),"chapter entry");
  for(int room=0;room<2;room++)for(int i=0;i<6;i++)Check(chapter.RegisterChapterEnemy(receipt,room,i,false),"chapter registration");
  Check(chapter.TryCompleteChapterNode(receipt),"chapter reward");Budget(chapter);int points=chapter.Profile.skillPoints;
  Check(chapter.TryCompleteChapterNode(receipt)&&chapter.Load()&&chapter.Profile.skillPoints==points,"chapter receipt replay never mints points");
  return "PASS: "+checks+" starting skill and shared budget production assertions";
 }
}

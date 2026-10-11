using System;using System.IO;using Emberfall;
public static class ChapterExperienceTests
{
 static int checks;static void Check(bool v,string m){checks++;if(!v)throw new Exception(m);}
 static long Total(ProgressionService p){long xp=p.Profile.xp;for(int l=1;l<p.Profile.level;l++)xp+=GameBalance.XpToNext(l);return xp;}
 static ProgressionService Fresh(string root,string name){var p=new ProgressionService(Path.Combine(root,name+Guid.NewGuid().ToString("N")));Check(p.CreateNewSlot(HeroClass.Vanguard),"create chapter XP save");return p;}
 static ChapterRunReceipt Begin(ProgressionService p,ChapterNode node){ChapterRunReceipt r;Check(p.TryBeginChapterNode(node,ChapterDifficulty.Normal,1,out r),"begin XP attempt");return r;}
 static void Register(ProgressionService p,ChapterRunReceipt r){int rooms=r.Node==ChapterNode.StarPlatform?1:2,count=r.Node==ChapterNode.StarPlatform?3:6;for(int room=0;room<rooms;room++)for(int i=0;i<count;i++)Check(p.RegisterChapterEnemy(r,room,i,r.Node==ChapterNode.StarPlatform&&i==0),"register true budget identity");}
 static void ValidateCoreEntitlement(string root)
 {
  foreach(bool legacyFirst in new[]{false,true})
  {
   var p=Fresh(root,"core-order");var core=BuildCatalog.MechanicsFor(p.Profile.heroClass)[0];
   if(legacyFirst)
   {
    Check(p.TryGrantModeReward(Guid.NewGuid().ToString("N"),0,0,0,1)&&p.Load()&&p.Profile.pendingFirstClearReward,"old completion still qualifies before chapter");
    Check(p.ClaimFirstClearReward(core)&&p.Load()&&p.Profile.firstClearRewardClaimed&&!p.Profile.pendingFirstClearReward,"old completion claims shared flag once");
   }
   foreach(var node in new[]{ChapterNode.ForestCourt,ChapterNode.Redrock})
   {
    var receipt=Begin(p,node);Register(p,receipt);Check(p.TryCompleteChapterNode(receipt)&&!p.Profile.pendingFirstClearReward,"forest and redrock never create chapter core entitlement");
    Check(p.Load()&&!p.Profile.pendingFirstClearReward&&!p.ClaimFirstClearReward(core),"forest and redrock remain ineligible before and after reload");
   }
   var star=Begin(p,ChapterNode.StarPlatform);Register(p,star);string disk=File.ReadAllText(p.SaveFilePath);long sequence=p.Profile.chapterRewardSequence;
   Directory.CreateDirectory(p.SaveFilePath+".tmp");Check(!p.TryCompleteChapterNode(star)&&!p.Profile.pendingFirstClearReward&&p.Profile.chapterRewardSequence==sequence&&File.ReadAllText(p.SaveFilePath)==disk,"failed star completion cannot publish chapter core entitlement");Directory.Delete(p.SaveFilePath+".tmp");
   Check(p.TryCompleteChapterNode(star)&&p.Profile.pendingFirstClearReward==!legacyFirst,"only completed Star chapter enables unclaimed shared core");
   Check(p.Load()&&p.Profile.pendingFirstClearReward==!legacyFirst,"completed Star chapter eligibility survives reload");
   if(!legacyFirst)Check(p.ClaimFirstClearReward(core),"completed chapter can claim existing first core");
   Check(p.Load()&&p.Profile.firstClearRewardClaimed&&!p.Profile.pendingFirstClearReward&&!p.ClaimFirstClearReward(core),"chapter and old completion share one claim in either order");
   Check(p.TryGrantModeReward(Guid.NewGuid().ToString("N"),0,0,0,1)&&p.Load()&&!p.Profile.pendingFirstClearReward&&!p.ClaimFirstClearReward(core),"later old completion never grants second chapter core");
   var repeat=Begin(p,ChapterNode.StarPlatform);Register(p,repeat);Check(p.TryCompleteChapterNode(repeat)&&p.Load()&&!p.Profile.pendingFirstClearReward&&!p.ClaimFirstClearReward(core),"repeated chapter completion never grants second core");
  }
 }
 public static string Run(string root)
 {
  checks=0;
  for(int level=1;level<=100;level++)for(int n=0;n<3;n++)
  {
   var node=(ChapterNode)n;var b=new ChapterExperienceBudget(node,level);int normal=22+level*2,boss=100+level*12,total=node==ChapterNode.StarPlatform?boss+2*normal:12*normal;int shares=node==ChapterNode.StarPlatform?boss*3/10+2*(normal*3/10):12*(normal*3/10);
   Check(b.TotalExperience==total&&!b.AllRegistered&&b.CompletionExperience==0,"entry level freezes expected full budget");
   int ignored;Check(!b.TryClaim(0,0,out ignored)&&ignored==0,"unregistered identity cannot earn chapter XP");
   int rooms=n==2?1:2,count=n==2?3:6;
   for(int room=0;room<rooms;room++)for(int i=0;i<count;i++){bool isBoss=n==2&&i==0;Check(!b.Register(room,i,!isBoss),"wrong boss registration rejected");Check(b.Register(room,i,isBoss)&&!b.Register(room,i,isBoss),"registered identity exactly once");}
   Check(b.AllRegistered&&b.CompletionExperience==total-shares,"completion subtracts every registered floor share including rounding");
   int earned=0;for(int room=0;room<rooms;room++)for(int i=0;i<count;i++){int xp;Check(b.TryClaim(room,i,out xp)&&xp==(n==2&&i==0?boss:normal)*3/10,"death share floors individually");earned+=xp;Check(!b.TryClaim(room,i,out ignored),"duplicate death share rejected");}
   Check(earned==shares&&b.EarnedKillExperience==shares&&earned+b.CompletionExperience==total,"full clear equals fixed total exactly");
   Check(!b.Register(2,0,false)&&!b.TryClaim(-1,0,out ignored),"wrong room identity rejected");
  }
  var p=Fresh(root,"minimum");var receipt=Begin(p,ChapterNode.ForestCourt);Check(p.ChapterExperienceEntryLevel==1&&p.ChapterTotalExperience==288,"fresh character entry XP level is1 not combat clamp2");Check(!p.TryCompleteChapterNode(receipt),"partial enemy registration cannot settle XP");Register(p,receipt);
  int kill;Check(p.TryClaimChapterEnemyExperience(receipt,0,0,out kill)&&kill==7,"level1 chapter kill share7");p.GrantEnemyKillReward(9,kill);long before=Total(p);string disk=File.ReadAllText(p.SaveFilePath);int gold=p.Profile.gold;int events=0;p.Changed+=()=>events++;
  Directory.CreateDirectory(p.SaveFilePath+".tmp");Check(!p.TryCompleteChapterNode(receipt)&&Total(p)==before&&p.Profile.chapterCompletedMask==0&&!p.Profile.pendingFirstClearReward&&events==0&&File.ReadAllText(p.SaveFilePath)==disk,"completion XP unlock and first core are atomic on save failure");Directory.Delete(p.SaveFilePath+".tmp");
  Check(p.TryCompleteChapterNode(receipt)&&Total(p)==211&&p.Profile.gold==gold ,"minimum objective cannot receive skipped death shares");Check(!p.Profile.pendingFirstClearReward,"forest alone does not enable shared first core");long paid=Total(p);int materials=p.Profile.mechanicMaterials;Check(p.TryCompleteChapterNode(receipt)&&Total(p)==paid&&p.Profile.mechanicMaterials==materials,"same receipt completion XP is idempotent");
  Check(p.Load()&&!p.Profile.pendingFirstClearReward&&Total(p)==paid,"forest remains ineligible after reload");
  var retry=Begin(p,ChapterNode.ForestCourt);Register(p,retry);Check(p.TryCompleteChapterNode(receipt)&&Total(p)==paid,"last completed receipt replay remains no-op while next attempt is pending");Check(p.TryCompleteChapterNode(retry)&&!p.TryCompleteChapterNode(receipt),"older receipt cannot payout after newer settlement");
  var abandoned=Fresh(root,"abandon");var a=Begin(abandoned,ChapterNode.ForestCourt);Register(abandoned,a);Check(abandoned.TryClaimChapterEnemyExperience(a,0,0,out kill),"earn failure share");abandoned.GrantEnemyKillReward(0,kill);abandoned.CancelChapterRun();Check(abandoned.Load()&&Total(abandoned)==7&&!abandoned.TryCompleteChapterNode(a),"failure abandon retain kills but no objective payout");
  var full=Fresh(root,"full");var f=Begin(full,ChapterNode.ForestCourt);Register(full,f);for(int room=0;room<2;room++)for(int i=0;i<6;i++){Check(full.TryClaimChapterEnemyExperience(f,room,i,out kill)&&kill==7,"entry budget does not grow with live level");full.GrantEnemyKillReward(0,kill);}Check(full.TryCompleteChapterNode(f)&&Total(full)==288,"full clear fixed entry total288");
  var legacy=Fresh(root,"legacy-total");for(int i=0;i<12;i++)legacy.GrantEnemyKillReward(0,22+legacy.Profile.level*2);Check(Total(legacy)>=288,"previous live-level accounting is distinct from fixed budget");
  var star=Fresh(root,"star");star.Profile.chapterCompletedMask=3;star.Save();var s=Begin(star,ChapterNode.StarPlatform);Register(star,s);int bossXp;Check(star.TryClaimChapterEnemyExperience(s,0,0,out bossXp)&&bossXp==33,"boss included at fixed character level");star.GrantEnemyKillReward(0,bossXp);for(int i=1;i<3;i++){Check(star.TryClaimChapterEnemyExperience(s,0,i,out kill),"star guard share");star.GrantEnemyKillReward(0,kill);}Check(star.TryCompleteChapterNode(s)&&Total(star)==160,"direct star budget includes boss and two guards");
  ValidateCoreEntitlement(root);
  Console.WriteLine("ACCOUNTING: level1 forest fixed full288; zero-kill completion204; one-kill minimum211; old live-level full "+Total(legacy)+"; direct star full160.");
  return "PASS: "+checks+" chapter fixed XP registration, floor shares, atomic receipt and shared first-core assertions";
 }
}

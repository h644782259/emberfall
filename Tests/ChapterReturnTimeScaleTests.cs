using System;using System.IO;using UnityEngine;
namespace Emberfall
{
 public sealed partial class GameSession
 {
  static int clockChecks;static void Clock(bool ok,string why){clockChecks++;if(!ok)throw new Exception(why);}
  static GameSession ClockFixture(string path)
  {
   var s=new GameSession{Progression=new ProgressionService(path)};Clock(s.Progression.CreateNewSlot(HeroClass.Vanguard),"real saved role fixture");s.FixtureUnlock();s.SelectedChapterNode=ChapterNode.StarPlatform;
   Clock(s.ConfirmChapterEnter(),"real chapter entry");Clock(s.ChapterRoomIndex==0&&s.ChapterRun.Objective==RoomObjective.Boss,"direct single boss room entry");return s;
  }
  static void FinishForClock(GameSession s){while(s.Enemies.Count>0)s.OnEnemyKilled(s.Enemies[0]);Clock(s.ChapterFinished&&s.ModeFinished&&Time.timeScale==0,"actual finalize freezes terminal chapter");}
  public static string VerifyReturnClock(string root)
  {
   clockChecks=0;
   var s=ClockFixture(Path.Combine(root,"success"));FinishForClock(s);new GameUI(s).Return();
   Clock(!s.ChapterActive&&!s.ModeFinished&&!s.InDungeon&&!s.InputBlocked&&Time.timeScale==1,"successful UI chapter return resumes camp time");
   s=ClockFixture(Path.Combine(root,"savefail"));Directory.CreateDirectory(s.Progression.SaveFilePath+".tmp");FinishForClock(s);
   int epoch=s.Player.CombatEpoch,builds=WorldBuilder.Builds;var worldBefore=s.world;var receipt=s.Receipt;
   Clock(s.ChapterRewardPending,"reward write failure remains pending");new GameUI(s).Return();
   Clock(s.ChapterRewardPending&&s.Receipt==receipt&&s.Player.CombatEpoch==epoch&&s.world==worldBefore&&worldBefore.activeInHierarchy&&WorldBuilder.Builds==builds&&Time.timeScale==0,"failed reward save cannot restore time or dismantle world");
   Directory.Delete(s.Progression.SaveFilePath+".tmp");Clock(s.TrySettleChapterReward(),"real reward retry succeeds");Clock(Time.timeScale==0&&s.ModeFinished,"reward retry alone does not resume terminal combat");new GameUI(s).Return();Clock(Time.timeScale==1&&!s.ChapterActive,"retried reward then return resumes camp");
   s=ClockFixture(Path.Combine(root,"regularsavefail"));FinishForClock(s);s.Progression.Profile.gold++;Directory.CreateDirectory(s.Progression.SaveFilePath+".tmp");epoch=s.Player.CombatEpoch;new GameUI(s).Return();Clock(Time.timeScale==0&&s.ChapterActive&&s.Player.CombatEpoch==epoch,"ordinary transition save failure retains frozen chapter");Directory.Delete(s.Progression.SaveFilePath+".tmp");new GameUI(s).Return();Clock(Time.timeScale==1,"ordinary save retry resumes only on return");
   s=ClockFixture(Path.Combine(root,"failed"));s.FailForTest();Clock(Time.timeScale==0&&s.ChapterRun.Failed,"actual failure freezes chapter");new GameUI(s).Return();Clock(Time.timeScale==1&&!s.ChapterActive,"failed chapter can return without frozen camp");
   for(int mask=1;mask<16;mask++){
    s=ClockFixture(Path.Combine(root,"pause"+mask));FinishForClock(s);s.Paused=(mask&1)!=0;s.uiBlocking=(mask&2)!=0;s.pauseState.SetFocus((mask&4)==0);s.pauseState.SetSuspended((mask&8)!=0);new GameUI(s).Return();
    Clock(!s.ChapterActive&&Time.timeScale==0&&s.Paused==((mask&1)!=0)&&s.uiBlocking==((mask&2)!=0)&&s.BackgroundPaused==((mask&12)!=0),"return preserves independent manual UI focus and suspend gates");
   }
   s=ClockFixture(Path.Combine(root,"dead"));s.FailForTest();s.IsDead=true;s.Progression.Profile.gold++;Directory.CreateDirectory(s.Progression.SaveFilePath+".tmp");var failedEvidence=s.ChapterResult;new GameUI(s).Return();Clock(s.ChapterActive&&s.IsDead&&s.ChapterResult==failedEvidence&&Time.timeScale==0,"chapter failure respawn write rejection preserves death evidence and frozen world");Directory.Delete(s.Progression.SaveFilePath+".tmp");new GameUI(s).Return();Clock(!s.ChapterActive&&!s.IsDead&&Time.timeScale==1,"chapter failure UI retries actual respawn and restores camp clock");
   s=ClockFixture(Path.Combine(root,"gates"));FinishForClock(s);new GameUI(s).Return();
   s.DungeonSelectionOpen=true;s.UpdateTimeScale();Clock(Time.timeScale==0,"selection gate remains authoritative");s.DungeonSelectionOpen=false;s.RunChoices.PrepareRoomChoice(1,s.Progression.Profile,false,1);s.UpdateTimeScale();Clock(Time.timeScale==0,"choice gate remains authoritative");s.RunChoices.Reset();s.IsDead=true;s.UpdateTimeScale();Clock(Time.timeScale==0,"death gate remains authoritative");s.IsDead=false;s.UpdateTimeScale();Clock(Time.timeScale==1,"all actual gates released resumes clock");
   // A new role's load can fail after old-world discard; no player/world may keep time advancing.
   s.DiscardTransientAdventureForLoad();Clock(!s.HasStarted&&s.Paused&&s.Player==null&&s.world==null&&Time.timeScale==0,"discarded role cannot leave an advancing clock");
   s.Progression=new ProgressionService(Path.Combine(root,"replacement"));Clock(s.Progression.CreateNewSlot(HeroClass.Ranger),"replacement role persisted");s.loadingSaveSnapshot=true;s.BeginAdventure();s.loadingSaveSnapshot=false;Clock(s.HasStarted&&!s.Paused&&!s.uiBlocking&&!s.ChapterActive&&Time.timeScale==1,"actual begin replacement role resets terminal state and resumes");
   s.pauseState.SetFocus(false);s.DiscardTransientAdventureForLoad();s.loadingSaveSnapshot=true;s.BeginAdventure();s.loadingSaveSnapshot=false;Clock(Time.timeScale==0&&s.BackgroundPaused,"replacement role never bypasses OS pause");
   s.pauseState.SetFocus(true);s.UpdateTimeScale();Clock(s.QuitToTitle()&&!s.HasStarted&&!s.ChapterActive&&Time.timeScale==0,"actual title reset stays frozen");
   s=ClockFixture(Path.Combine(root,"switch"));FinishForClock(s);s.Paused=true;
   var target=new ProgressionService(s.Progression.SaveDirectory);Clock(target.CreateNewSlot(HeroClass.Ranger),"independent target slot saved");
   Clock(s.LoadSaveFromPause(target.CurrentSlotId,false)&&s.Progression.Profile.heroClass==HeroClass.Ranger&&!s.ChapterActive&&!s.Paused&&Time.timeScale==1,"real staged role switch resumes new camp");
   s.Paused=true;s.UpdateTimeScale();worldBefore=s.world;Clock(!s.LoadSaveFromPause("invalid",false)&&s.world==worldBefore&&s.Paused&&Time.timeScale==0,"rejected role staging preserves paused world");
   WorldBuilder.ThrowBuild=true;Clock(!s.LoadSaveFromPause(target.CurrentSlotId,false)&&!s.HasStarted&&s.Player==null&&s.world==null&&Time.timeScale==0,"real role load scene failure remains frozen after discard");WorldBuilder.ThrowBuild=false;
   return "PASS: "+clockChecks+" actual UI/session/chapter/time-scale/save/reset assertions (managed scene doubles)";
  }
 }
}

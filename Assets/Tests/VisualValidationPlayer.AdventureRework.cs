#if EMBERFALL_VISUAL_VALIDATION
using System.Collections;
using UnityEngine;
namespace Emberfall {
 public sealed partial class VisualValidationPlayer {
  public static void EnsureInstalled(){if(Object.FindAnyObjectByType<VisualValidationPlayer>()==null)Bootstrap();}
  private IEnumerator VerifyAdventureRework(){
   yield return SetResolution(1280,720);
   SetField("selectedClass",HeroClass.Arcanist);Invoke("StartSelectedHero");yield return WaitForSceneReady();session.Player.enabled=false;
   session.Progression.GrantExperience(1000);
   foreach(var enemy in session.Enemies)if(enemy!=null)enemy.enabled=false;
   session.LogSystem("拾取金币 × 20");session.LogSystem("获得装备 · 旅者护符");session.LogSystem("获得药剂 × 1");
   yield return Capture("camp-growth-log");
   Invoke("OpenChapterSelection");yield return Capture("chapter-reward-icons");ResetPanels();
   session.Player.Teleport(new Vector3(0,0,11));session.EnterDungeon();Check(session.DungeonSelectionOpen,"selection opened");
   for(int mode=-1;mode<4;mode++){session.SelectedArenaMode=mode;yield return Capture("adventure-mode-"+(mode+1));}
   session.SelectedArenaMode=-1;MobileControls.SimulationEnabled=true;
   yield return SetResolution(568,320);yield return Capture("adventure-touch-small");
   SetField("adventureRewardHint","时装：保底稀有 · 15%史诗 · 2%传说");SetField("adventureDetailScroll",new Vector2(0,190));yield return Capture("adventure-touch-detail");
   MobileControls.SimulationEnabled=false;yield return SetResolution(1280,720);session.SelectedChallengeMode=true;session.ConfirmDungeonSelection();yield return WaitForSceneReady();
   Check(session.InDungeon&&!session.ChallengeRun,"removed limited healing");session.Player.enabled=false;foreach(var enemy in session.Enemies)if(enemy!=null)enemy.enabled=false;
   int gold=session.Progression.Profile.gold;var victim=session.Enemies[0];session.OnEnemyKilled(victim);
   Check(session.Progression.Profile.gold==gold&&session.Progression.Profile.groundGold>0,"kill creates escrow");
   Check(Object.FindObjectsByType<GroundSupplyPickup>(FindObjectsSortMode.None).Length>0,"physical supplies exist");
   yield return Capture("ground-supplies");
   session.ReturnToCamp();yield return WaitForSceneReady();Check(!session.InDungeon&&session.Progression.Profile.groundGold==0,"exit preserves supplies");
   string receipt=System.Guid.NewGuid().ToString("N");Check(session.Progression.TryGrantModeReward(receipt,0,0,0,40,3),"completion chest qualifies");yield return null;
   Check(session.Progression.OpenDungeonChest()!=null,"chest commits its drawn reward");Invoke("ResetChestReveal");yield return Capture("completion-chest-reward",2f);
   var reward=session.Progression.LastChestReward;string expectedReward=JsonUtility.ToJson(reward,true);
   Check(reward.rulesRevision==3&&reward.primaryKind>=1&&reward.primaryKind<=7,"Current iOS one-item chest keeps its original reward categories");
   var restored=new ProgressionService(session.Progression.SaveDirectory);
   Check(restored.LoadSlot(session.Progression.CurrentSlotId)&&JsonUtility.ToJson(restored.LastChestReward,true)==expectedReward,"Exact committed chest receipt survives real Unity JSON reload");
  }
 }
}
#endif

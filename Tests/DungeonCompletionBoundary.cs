// Engine construction/rendering doubles. Session kill admission, completion,
// progression writes and hostile retirement methods are compiled from source.
using System;using System.Collections.Generic;using System.IO;using UnityEngine;
namespace UnityEngine
{
 public class MonoBehaviour{protected static void Destroy(GameObject value){value.Destroyed=true;}}
 public sealed class GameObject{public bool activeInHierarchy=true,Destroyed;public Action Disabled;public void SetActive(bool value){bool before=activeInHierarchy;activeInHierarchy=value;if(before&&!value)Disabled?.Invoke();}}
 public struct Vector3{public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public float sqrMagnitude=>x*x+y*y+z*z;public static Vector3 zero=>new Vector3();}
 public static class Time{public static int frameCount;public static float deltaTime=.1f;}
 public static class Random{public static int Range(int a,int b)=>b-1;}
 public static class Mathf{public static int RoundToInt(float x)=>(int)Math.Round(x);public static int CeilToInt(float x)=>(int)Math.Ceiling(x);public static float Clamp(float x,float a,float b)=>Math.Max(a,Math.Min(b,x));public static int Max(int a,int b)=>Math.Max(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int Min(int a,int b)=>Math.Min(a,b);}
 public sealed class Transform{public Vector3 position;}
}
namespace Emberfall
{
 public enum RunBlessing{RiskContract}
 public enum SoundCue{Victory}
 public static class GameAudio{public static int Victories;public static void Play(SoundCue cue){Victories++;}}
 public static class CombatFx{public static Vector3 Flat(Vector3 point)=>point;}
 public sealed class ArenaHazards{public void Advance(float delta){}}
 public sealed class PlayerController{public int CombatEpoch=7;public float MaxHealth=100;public Transform transform=new Transform();public int Heals;public void Heal(float n){Heals++;}}
 public sealed class EnemyController{public enum ThreatTier{Normal,Elite}public ThreatTier Tier;public bool IsBoss,IsDead;public EnemyKind Kind;public float NavigationRadius=>.5f;public Transform transform=new Transform();public GameObject gameObject=new GameObject();public int Deaths;public void BeginDeath(){Deaths++;}}
 internal sealed partial class CombatProjectile:MonoBehaviour
 {
  private static readonly List<CombatProjectile> hostileProjectiles=new List<CombatProjectile>();
  private GameSession session;private bool hostile;private Action hostileEnded;private ResourceBoundary castReceipt;
  public readonly GameObject gameObject=new GameObject();
  private sealed class ResourceBoundary{public int Releases;public void Release(){Releases++;}}
  public static CombatProjectile Fixture(GameSession owner,bool enemy,Action ended=null){var shot=new CombatProjectile{session=owner,hostile=enemy,hostileEnded=ended,castReceipt=new ResourceBoundary()};shot.gameObject.Disabled=shot.OnDisable;hostileProjectiles.Add(shot);return shot;}
 }
 public sealed partial class GameSession:MonoBehaviour
 {
  public ProgressionService Progression;public PlayerController Player=new PlayerController();
  public bool HasStarted=true,InDungeon=true,IsDead,ChallengeRun,changingZone,BackgroundPaused;
  public ExpeditionModeState ModeRun;public RoomChainState RoomChainRun;public ChapterCombatRun ChapterRun;public ChapterResultSnapshot ChapterResult;
  public bool ChapterActive=>ChapterRun!=null;public ChapterNode ActiveChapterNode=>ChapterRun.Node;public ChapterDifficulty ActiveChapterDifficulty=>ChapterRun.Difficulty;
  public int ChapterRoomIndex=>ChapterRun.RoomIndex;public bool CombatEnded=>ModeFinished||DungeonCleared;public bool InputBlocked=>IsDead||ModeFinished;
  public bool DungeonCleared;public int DungeonTier=1,DungeonWave,wavePopulation,HealingCharges,nextArenaEnemy;public bool objectiveHealedThisWave;
  public float arenaSpawnDelay,arenaSpawnBlocked;public int arenaPhaseRetiredFrame=-1,DungeonEntryLevel=1;public bool arenaAwaitingBlessing,arenaResultRecorded,roomResultRecorded;
  public ArenaHazards arenaHazards;public string modeReceipt,LastRunSummary,Notice;public int modeGoldReward,modeXpReward,modeMaterialReward;public bool modeRewardDetailsUnavailable;
  public string ModeName=>"fixture";public string ModeObjectiveStatus=>"fixture";private ProgressionService pendingWildernessSave;
  public int chapterEntryPotions,chapterBossDeathFrame=-1;public float chapterFirstSealSeconds,chapterSecondSealSeconds,lastDamageAmount;public string lastDamageSource;
  private sealed class ArenaEnemyReceipt{public ExpeditionPhasePlan Plan;public int Index;}
  private sealed class ChapterEnemyReceipt{public ChapterCombatRun Run;public int Room,Epoch,Index;}
  private readonly Dictionary<EnemyController,ArenaEnemyReceipt> arenaEnemies=new Dictionary<EnemyController,ArenaEnemyReceipt>();
  private readonly Dictionary<EnemyController,ChapterEnemyReceipt> chapterEnemies=new Dictionary<EnemyController,ChapterEnemyReceipt>();
  private readonly Dictionary<EnemyController,int> roomEnemies=new Dictionary<EnemyController,int>();
  public readonly List<EnemyController> Enemies=new List<EnemyController>();private readonly List<GameObject> transientObjects=new List<GameObject>();
  private sealed class ChoiceBoundary{public void Cancel(){}}private readonly ChoiceBoundary pendingRoomChoice=new ChoiceBoundary();
  public RunMechanismEvidence MechanismEvidence=new RunMechanismEvidence();
  public int runEnemyExperience,runPickupGold,RunPickupPotions;private Queue<int> reinforcementQueue=new Queue<int>();private object waveRoutine;
  public bool PracticeActive=>false;public CampPracticeRecord PracticeRecord=>throw new Exception("practice outside completion fixture");
  private void Notify(string notice){Notice=notice;}private void LogSystem(string notice){Notice=notice;}private string BuildRunSummary(bool success)=>success?"won":"failed";
  private void SuspendInputs(){}private void UpdateTimeScale(){}private bool HasBlessing(RunBlessing blessing)=>false;
  public int LootDrops;private void OnExpeditionEnemyKilled(EnemyController enemy){}private void RecordRoomDefeat(EnemyController enemy){}private void DeliverEnemyLoot(ItemData item,Vector3 point){LootDrops++;}
  private void TrySpawnReinforcements(){throw new Exception("special completion cannot schedule ordinary waves");}private object StartCoroutine(object work)=>throw new Exception("ordinary wave outside fixture");private object NextWave()=>null;
  private void FailChapter(string reason){ChapterRun.Fail();Progression.CancelChapterRun();throw new Exception(reason);}
  private void SpawnArenaEnemies()
  {
   if(ModeRun.Status!=ExpeditionModeStatus.Active)return;var plan=ModeRun.CurrentPhase;
   while(nextArenaEnemy<plan.EnemyCount&&ModeRun.AvailableSpawnSlots>0){int index=nextArenaEnemy++;Check(ModeRun.TryRegisterSpawn(plan,index),"fixture arena spawning uses real admission");var enemy=new EnemyController{IsBoss=plan.Enemies[index]==ExpeditionEnemyRole.Boss};Enemies.Add(enemy);arenaEnemies.Add(enemy,new ArenaEnemyReceipt{Plan=plan,Index=index});}
  }
  private static int checks;private static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
  private static long Earned(GameProfile profile){long xp=profile.xp;for(int level=1;level<profile.level;level++)xp+=GameBalance.XpToNext(level);return xp;}
  private static GameSession Fresh(string path){var s=new GameSession{Progression=new ProgressionService(path),modeReceipt=Guid.NewGuid().ToString("N")};Check(s.Progression.CreateNewSlot(HeroClass.Vanguard),"fixture creates durable role");s.Progression.BeginDungeonStage();return s;}
  private static void Kill(GameSession session,EnemyController enemy){enemy.IsDead=true;session.OnEnemyKilled(enemy);}
  private static void Gauntlet(string path,bool failWrite)
  {
   Time.frameCount=100;var s=Fresh(path);s.ModeRun=new ExpeditionModeState(ExpeditionModeKind.BossGauntlet,1,72);s.BeginArenaPhase();
   var foreign=Fresh(path+"-foreign");var foreignShot=CombatProjectile.Fixture(foreign,true);var friendly=CombatProjectile.Fixture(s,false);
   int terminations=0,epoch=s.Player.CombatEpoch,actualKills=0;
   for(int phase=0;phase<3;phase++)
   {
    Check(s.ModeRun.CurrentPhase.PhaseIndex==phase,"automatic phase begins in order");var boss=s.Enemies.Find(e=>e.IsBoss);var adds=s.Enemies.FindAll(e=>!e.IsBoss);var hostile=CombatProjectile.Fixture(s,true,()=>terminations++);
    foreach(var add in adds)add.gameObject.Disabled=()=>s.OnEnemyKilled(add);
    if(failWrite&&phase==2)Directory.CreateDirectory(s.Progression.SaveFilePath+".tmp");
    Kill(s,boss);actualKills++;
    Check(s.Enemies.Count==0&&adds.TrueForAll(e=>!e.gameObject.activeInHierarchy&&e.gameObject.Destroyed&&e.Deaths==0),"gauntlet boss retires live adds without synthetic deaths");
    Check(!hostile.gameObject.activeInHierarchy&&hostile.gameObject.Destroyed&&terminations==phase+1,"retirement ends hostile volley exactly once");
    Check(foreignShot.gameObject.activeInHierarchy&&friendly.gameObject.activeInHierarchy&&s.Player.CombatEpoch==epoch,"retirement preserves foreign host friendly casts and player epoch");
    Check(s.Progression.Profile.kills==actualKills,"retired add callbacks never grant kills or experience");
    int earned=s.runEnemyExperience,drops=s.LootDrops;foreach(var add in adds)s.OnEnemyKilled(add);s.OnEnemyKilled(boss);Check(s.runEnemyExperience==earned&&s.Progression.Profile.kills==actualKills&&s.LootDrops==drops,"stale phase deaths cannot award again");
    if(phase<2)
    {
     s.TickArenaRun();Check(s.ModeRun.Status==ExpeditionModeStatus.AwaitingSpawn&&s.Enemies.Count==0,"same-frame area attack cannot reach next phase");
     Time.frameCount++;CombatImpactBatch.BeginAction();s.TickArenaRun();Check(s.ModeRun.Status==ExpeditionModeStatus.AwaitingSpawn,"phase waits for finishing action boundary");CombatImpactBatch.EndAction();
     s.TickArenaRun();Check(s.ModeRun.Status==ExpeditionModeStatus.Active&&s.Enemies.Count>0,"next phase starts automatically without add clearing");
    }
   }
   Check(s.DungeonCleared&&s.ModeRun.Status==ExpeditionModeStatus.Won&&s.ModeRun.DefeatedEnemies==1,"only phase bosses complete gauntlet");
   if(failWrite){Check(s.ModeRewardPending&&!s.ModeRun.RewardReserved,"failed final save remains retryable after retirement");Directory.Delete(s.Progression.SaveFilePath+".tmp");Check(s.TrySettleArenaReward(),"same gauntlet receipt retries durable reward");}
   Check(s.ModeRun.RewardClaimed&&s.Progression.Profile.lastModeRewardId==s.modeReceipt,"gauntlet final reward uses durable receipt");
   int gold=s.Progression.Profile.gold,materials=s.Progression.Profile.mechanicMaterials;long xp=Earned(s.Progression.Profile);for(int i=0;i<3;i++){s.FinalizeArenaResult();Check(s.TrySettleArenaReward(),"repeat gauntlet settlement accepted");}
   Check(s.Progression.Profile.gold==gold&&s.Progression.Profile.mechanicMaterials==materials&&Earned(s.Progression.Profile)==xp,"gauntlet repeat completion cannot double rewards");
   var reload=new ProgressionService(s.Progression.SaveDirectory);Check(reload.LoadSlot(s.Progression.CurrentSlotId)&&reload.Profile.kills==3&&reload.Profile.lastModeRewardId==s.modeReceipt,"durable gauntlet has exactly three real kills");
  }
  private static GameSession FinalForest(string path)
  {
   var s=Fresh(path);s.Progression.Profile.level=ChapterProgression.UnlockLevel(ChapterNode.ForestCourt);Check(s.Progression.SaveDungeonCheckpoint(),"chapter level eligible and durable");
   Check(s.Progression.TryBeginChapterNode(ChapterNode.ForestCourt,ChapterDifficulty.Heroic,1,out s.chapterReceipt),"real forest attempt admitted");s.ChapterRun=new ChapterCombatRun(ChapterNode.ForestCourt,ChapterDifficulty.Heroic,5);s.ChapterRun.BindRoom(s.Player.CombatEpoch);
   for(int i=0;i<s.ChapterRun.EnemyCount;i++){Check(s.ChapterRun.Register(0,s.Player.CombatEpoch,i),"first forest room registers real state");Check(s.Progression.RegisterChapterEnemy(s.chapterReceipt,0,i,false),"first room registers actual XP budget");}
   for(int seal=0;seal<2;seal++)for(int tick=0;tick<8;tick++)s.ChapterRun.AdvanceSeal(seal,.25f,true,true,false);
   Check(s.ChapterRun.Exit(true,false),"first forest room still requires objective and exit");s.Player.CombatEpoch++;s.ChapterRun.BindRoom(s.Player.CombatEpoch);
   for(int i=0;i<s.ChapterRun.EnemyCount;i++){Check(s.ChapterRun.Register(1,s.Player.CombatEpoch,i)&&s.Progression.RegisterChapterEnemy(s.chapterReceipt,1,i,false),"final forest room registers state and XP receipt");var enemy=new EnemyController();s.Enemies.Add(enemy);s.chapterEnemies.Add(enemy,new ChapterEnemyReceipt{Run=s.ChapterRun,Room=1,Epoch=s.Player.CombatEpoch,Index=i});}
   return s;
  }
  private ChapterRunReceipt chapterReceipt;
  private static void Forest(string path,bool failWrite)
  {
   var s=FinalForest(path);var realEnemies=s.Enemies.ToArray();var stray=new EnemyController();s.Enemies.Add(stray);stray.gameObject.Disabled=()=>s.OnEnemyKilled(stray);
   for(int i=0;i<realEnemies.Length-1;i++)Kill(s,realEnemies[i]);Check(!s.ChapterFinished&&!s.DungeonCleared,"partial final forest clear never ends encounter");
   if(failWrite)Directory.CreateDirectory(s.Progression.SaveFilePath+".tmp");Kill(s,realEnemies[realEnemies.Length-1]);
   Check(s.ChapterFinished&&s.DungeonCleared&&s.ChapterResult!=null&&!s.ChapterResult.Failed,"final forest clear directly publishes result without portal interaction");
   Check(s.Enemies.Count==0&&!stray.gameObject.activeInHierarchy&&stray.Deaths==0,"chapter terminal clears residual enemy without synthetic kill");
   Check(s.Progression.Profile.kills==realEnemies.Length&&s.ChapterResult.KillExperience==s.runEnemyExperience,"forest result contains actual kills only");
   if(failWrite){Check(s.ChapterRewardPending&&!s.ChapterResult.Saved,"forest failed write keeps terminal result retryable");Directory.Delete(s.Progression.SaveFilePath+".tmp");Check(s.TrySettleChapterReward(),"same forest receipt durably retries");}
   Check(s.ChapterResult.Saved&&s.ChapterRun.RewardClaimed,"forest result acknowledges durable reward");long xp=Earned(s.Progression.Profile);int materials=s.Progression.Profile.mechanicMaterials;
   s.FinalizeChapterBoss();s.OnEnemyKilled(stray);Check(s.TrySettleChapterReward()&&Earned(s.Progression.Profile)==xp&&s.Progression.Profile.mechanicMaterials==materials,"forest stale kills and duplicate finish cannot double payout");
   var reload=new ProgressionService(s.Progression.SaveDirectory);Check(reload.LoadSlot(s.Progression.CurrentSlotId)&&reload.Profile.kills==realEnemies.Length&&reload.Profile.lastChapterRewardId==s.chapterReceipt.Id,"forest clear and actual kill count persist together");
  }
  private static void NarrowChapterPolicy()
  {
   foreach(ChapterNode node in new[]{ChapterNode.ForestCourt,ChapterNode.Redrock,ChapterNode.StarPlatform})
   {
    var run=new ChapterCombatRun(node,ChapterDifficulty.Heroic,8);run.BindRoom(1);for(int i=0;i<run.EnemyCount;i++)Check(run.Register(0,1,i),"policy registers first room");
    for(int i=0;i<run.EnemyCount;i++)Check(run.Defeat(0,1,i),"policy real first-room defeats admitted");Check(!run.FinishClearedFinalRoom(),"forest first room and other chapters retain their original completion rules");
   }
   var failed=new ChapterCombatRun(ChapterNode.ForestCourt,ChapterDifficulty.Heroic,1);failed.Fail();Check(!failed.FinishClearedFinalRoom()&&!failed.FinishBoss()&&failed.Failed,"failed chapter cannot become successful clear");
  }
  private static void TerminalRoomAndFailure(string root)
  {
   var s=Fresh(Path.Combine(root,"room-terminal"));s.RoomChainRun=new RoomChainState(18);
   for(int room=0;room<5;room++)
   {
    var plan=s.RoomChainRun.Room;
    for(int i=0;i<plan.EnemyCount;i++){Check(s.RoomChainRun.Register(plan,i),"room setup registers actual state");Check(s.RoomChainRun.Defeat(plan,i),"room setup admits actual state defeat");}
    if(room<4){for(int tick=0;tick<32;tick++)s.RoomChainRun.Advance(.25f,true,true,false);if(room==1){Check(s.RoomChainRun.OpenBranchChoice()&&s.RoomChainRun.SelectBranch(RoomBranch.Supply),"room setup obeys existing branch gate");}Check(s.RoomChainRun.Next(true,false),"ordinary room transition retains objective and door gates");}
   }
   var residual=new EnemyController();s.Enemies.Add(residual);residual.gameObject.Disabled=()=>s.OnEnemyKilled(residual);s.FinalizeRoomChain();
   Check(s.DungeonCleared&&s.RoomChainRun.RewardClaimed&&s.Enemies.Count==0&&!residual.gameObject.activeInHierarchy&&s.Progression.Profile.kills==0,"successful room chain clears residual enemy without kill reward");
   int gold=s.Progression.Profile.gold;s.FinalizeRoomChain();Check(s.Progression.Profile.gold==gold,"room result cannot repeat payout");
   s=Fresh(Path.Combine(root,"arena-failure"));s.ModeRun=new ExpeditionModeState(ExpeditionModeKind.BossGauntlet,1,1);s.BeginArenaPhase();gold=s.Progression.Profile.gold;long xp=Earned(s.Progression.Profile);
   s.ModeRun.Fail(ExpeditionModeFailure.TimeExpired);s.FinalizeArenaResult();Check(s.TrySettleArenaReward()&&!s.DungeonCleared&&!s.ModeRun.RewardClaimed&&s.Progression.Profile.lastModeRewardId==null&&s.Progression.Profile.gold==gold&&Earned(s.Progression.Profile)==xp,"failed arena never pays success reward");
   s.ModeRun.Dispose();Check(s.TrySettleArenaReward()&&s.Progression.Profile.lastModeRewardId==null&&s.Progression.Profile.gold==gold,"disposed arena never pays completion reward");
   s=FinalForest(Path.Combine(root,"chapter-failure"));gold=s.Progression.Profile.gold;xp=Earned(s.Progression.Profile);s.ChapterRun.Fail();s.FinalizeChapterBoss();Check(s.TrySettleChapterReward()&&!s.DungeonCleared&&!s.ChapterRun.RewardClaimed&&s.Progression.Profile.lastChapterRewardId==null&&s.Progression.Profile.gold==gold&&Earned(s.Progression.Profile)==xp,"failed forest cannot become successful clear or issue reward");
   s=Fresh(Path.Combine(root,"room-failure"));s.RoomChainRun=new RoomChainState();s.RoomChainRun.Fail(RoomFailureReason.Abandoned);gold=s.Progression.Profile.gold;s.FinalizeRoomChain();Check(s.TrySettleRoomReward()&&!s.DungeonCleared&&!s.RoomChainRun.RewardClaimed&&s.Progression.Profile.gold==gold&&s.Progression.Profile.lastModeRewardId==null,"abandoned ordinary room chain cannot issue success reward");
  }
  public static string VerifyCompletion(string root)
  {
   checks=0;Gauntlet(Path.Combine(root,"gauntlet"),false);Gauntlet(Path.Combine(root,"gauntlet-fail"),true);Forest(Path.Combine(root,"forest"),false);Forest(Path.Combine(root,"forest-fail"),true);NarrowChapterPolicy();TerminalRoomAndFailure(root);
   return "PASS: "+checks+" actual dungeon completion/admission/durable reward assertions (managed engine boundaries)";
  }
 }
}

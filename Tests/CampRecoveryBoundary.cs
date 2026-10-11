using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine
{
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 zero=>new Vector3();
        public static Vector3 forward=>new Vector3(0,0,1);
        public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator *(Vector3 a,float n)=>new Vector3(a.x*n,a.y*n,a.z*n);
    }
    public struct Quaternion
    {
        public float x,y,z,w;
        public static Quaternion identity=>new Quaternion{w=1};
        public static Quaternion Euler(float x,float y,float z)=>new Quaternion{x=x,y=y,z=z};
    }
    public sealed class Transform
    {
        public Vector3 position,forward=Vector3.forward;
        public Quaternion localRotation=Quaternion.identity;
    }
    public class GameObject
    {
        public static int Created;
        public readonly string name;
        public bool activeInHierarchy=true,Destroyed;
        public readonly Transform transform=new Transform();
        readonly Dictionary<Type,object> components=new Dictionary<Type,object>();
        public GameObject(string name="boundary"){this.name=name;Created++;}
        public void Put<T>(T value){components[typeof(T)]=value;}
        public T GetComponent<T>() where T:class=>components.TryGetValue(typeof(T),out var value)?value as T:null;
        public void SetActive(bool active){activeInHierarchy=active;}
    }
    public class MonoBehaviour
    {
        public GameObject gameObject=new GameObject();
        public Transform transform=>gameObject.transform;
        public T GetComponent<T>() where T:class=>gameObject.GetComponent<T>();
        public int StoppedCoroutines,CancelledInvokes;
        protected void StopAllCoroutines(){StoppedCoroutines++;}
        protected void CancelInvoke(){CancelledInvokes++;}
        protected static void Destroy(GameObject value){if(value!=null){value.Destroyed=true;value.SetActive(false);}}
    }
    public sealed class Camera:MonoBehaviour {public static Camera main;}
    public static class Physics {public static int Synchronizations;public static void SyncTransforms(){Synchronizations++;}}
    public static class Mathf
    {
        public static float Max(float a,float b)=>Math.Max(a,b);
        public static float Min(float a,float b)=>Math.Min(a,b);
        public static float Clamp(float n,float min,float max)=>Math.Min(max,Math.Max(min,n));
    }
}

namespace Emberfall
{
    public enum HeroClass {Vanguard,Arcanist,Ranger,Summoner}
    public enum FashionSlot {Wings,Weapon}
    public enum ItemSlot {Weapon,Armor,Relic}
    public static class GameBalance
    {
        public const int SkillCount=10;
        public static readonly string[] ClassNames={"Vanguard","Arcanist","Ranger","Summoner"};
        public static bool IsPassive(int skill)=>false;
        public static float SkillEnergyCost(HeroClass hero,int skill)=>25;
        public static float EffectiveCooldown(HeroClass hero,int skill,int rank)=>8;
    }
    public class GameProfile {public int currentHub,masteryCore=-1;public int[] masteryRanks=new int[4];}
    public class StatBlock {public float MaxHealth=120;}
    public sealed class ProgressionService
    {
        public readonly GameProfile Profile=new GameProfile();
        public readonly StatBlock Stats=new StatBlock();
        public bool IsApplyingBuildDraft,TravelFails;
        public string LastError;
        public StatBlock GetStats()=>Stats;
        public object EquippedFashion(FashionSlot slot)=>null;
        public object Equipped(ItemSlot slot)=>null;
        public bool TravelToHub(int hub){if(TravelFails){LastError="travel persistence refused";return false;}Profile.currentHub=hub;return true;}
    }
    public sealed class MasteryBoundary
    {
        public int Core=-1,Tier,Resets;
        public void Configure(int core,int tier){Core=core;Tier=tier;}
        public void Reset(){Resets++;}
    }
    public sealed class RecoveryClockBoundary {public bool Blocked=true;public void Clear(){Blocked=false;}}
    public sealed class FrostBoundary {public int Clears;public void Clear(){Clears++;}}
    public sealed class SkillTargetingController {public bool IsTargeting=true;public int Cancels;public void Cancel(){IsTargeting=false;Cancels++;}}
    public sealed class SkillChargeController {public bool IsCharging=true;public int Cancels;public void Cancel(){IsCharging=false;Cancels++;}}
    public sealed class CombatModel:MonoBehaviour
    {
        public int Resets,Cancellations;public bool OwnerAlive;
        public void ApplyFashion(object a,object b){}
        public void ApplyEquipment(object a,object b,object c){}
        public void SetBlenderPilotOwnerAlive(bool alive){OwnerAlive=alive;}
        public void ResetLocomotion(){Resets++;}
        public void CancelAction(){Cancellations++;}
    }
    public static class WorldTraversal
    {
        public static Vector3 NearestWalkable(Vector3 point,float radius)=>new Vector3(point.x,2.5f,point.z);
    }
    public static class SummonedCompanion
    {
        public static int Transfers;
        public static bool LastTransferOwnerAlive;
        public static void RefreshBuild(PlayerController owner){}
        public static void TransferPermanentPartners(PlayerController owner){Transfers++;LastTransferOwnerAlive=!owner.IsDead;}
    }
    public sealed class AdventureCamera {public int Snaps;public void Snap(){Snaps++;}}
    public sealed class GameUI {public int ForegroundCancellations,BackgroundCancellations;public void CancelForegroundInput(){ForegroundCancellations++;}public void CancelBackgroundInput(){BackgroundCancellations++;}}
    public static class MobileControls {public static bool Held=true;public static int Resets;public static void ResetInput(){Held=false;Resets++;}}
    public static class FloatingNumber {public static int Clears;public static void ClearForWorldTransition(){Clears++;}}
    public sealed class ThreatBoundary {public int Withdrawn,Released;public void Withdraw(int i){Withdrawn++;}public void Release(int i){Released++;}}
    public sealed class BossBoundary {public readonly BossState State=new BossState();public sealed class BossState{public bool Interruptible;}}

    public partial class PlayerController:MonoBehaviour
    {
        GameSession session;StatBlock stats;
        public readonly SkillRuntime Runtime;
        SkillRuntime skillRuntime;
        public readonly CombatModel Model=new CombatModel();
        CombatModel model;
        public readonly SkillTargetingController Targeting=new SkillTargetingController();
        public readonly SkillChargeController Charge=new SkillChargeController();
        SkillTargetingController targeting;SkillChargeController charge;
        public readonly MasteryBoundary masteryCore=new MasteryBoundary();
        readonly RelicGemRuntime relicGems=new RelicGemRuntime();
        readonly RecoveryClockBoundary skillBasicRecovery=new RecoveryClockBoundary();
        readonly FrostBoundary openingFrost=new FrostBoundary();
        readonly Dictionary<int,int> aimGeometry=new Dictionary<int,int>();
        public float Health,MaxHealth=120;
        public bool IsDead=>Health<=0;
        public HeroClass HeroClass;
        public int CombatEpoch=9;
        public object AimTarget=new object();
        public bool Pinned=true,PendingRelease=true;
        public float perfectDodgeCounterTime=1,coreWardTime=1,platformFallSpeed=3,jumpAge=1,movementSkillLock=1;
        public bool jumping=true,rangerVault=true,perfectDodgeAwarded;
        Vector3 aimPoint;
        public float guardTime=1,healingProtectionTime=1,mobilityTime=1,passiveTime=1;
        public int guardCastId=6;
        public float blinkBufferTime=1,perfectDodgeWindow=1,counterTime=1,dodgeShockTime=1,chargedWardTime=1,pursuitTime=1,focusTime=1;
        public object focusedEnemy=new object(),openingFrostTarget=new object();
        public float classDodgeTime=1,burnStrideTime=1,slowTime=1,slowStrength=.7f,attackAnimation=1,attackCooldown=1,blinkInvulnerableUntil=5,invulnerability;
        public int openingFrostIdentity=5;
        public PlayerController(GameSession session,HeroClass hero)
        {
            this.session=session;HeroClass=hero;Runtime=skillRuntime=new SkillRuntime(hero);
            model=Model;targeting=Targeting;charge=Charge;
            gameObject.Put(Targeting);gameObject.Put(Charge);
            transform.position=new Vector3(18,0,7);model.transform.localRotation=Quaternion.Euler(0,0,75);
            if(!Runtime.TryConsume(6,3))throw new Exception("resource fixture admission");
        }
        void ConfigureRelicGems(){}
        void ClearMobilePinnedTarget(){Pinned=false;}
        void CancelPendingSkillRelease(){PendingRelease=false;}
        public bool TemporaryStateCleared=>!jumping&&!rangerVault&&!Pinned&&!PendingRelease&&AimTarget==null&&focusedEnemy==null&&slowTime==0&&slowStrength==0&&movementSkillLock==0&&platformFallSpeed==0&&guardTime==0&&healingProtectionTime==0&&mobilityTime==0&&passiveTime==0&&guardCastId==0&&blinkBufferTime==0&&counterTime==0&&coreWardTime==0&&!Targeting.IsTargeting&&!Charge.IsCharging&&!skillBasicRecovery.Blocked;
    }
    public partial class EnemyController:MonoBehaviour
    {
        public float Health=63;public bool IsDead=>Health<=0;
        public bool aggro=true,preparing=true,activeChargePose=true,pounceHarmless,dodgePending=true;
        public float windup=1,chargeTime=.8f,comboDelay=.4f,sidestepTime=.2f,pounceRemaining=.5f,pounceHeight=1,attackCooldown=.1f;
        public int comboRemaining=2,threatMember=3;
        public object companionTarget=new object(),dodgePlayer=new object(),telegraph=new object();
        public Vector3 knockVelocity=new Vector3(2,0,3);
        public GameObject warning=new GameObject("old hostile warning");
        public readonly ThreatBoundary threatAdmission=new ThreatBoundary();
        public BossBoundary largeBoss;
        public GameSession session;
        public bool CorpseDestroyScheduled=true;
        public bool AttackRetired=>!preparing&&!activeChargePose&&windup==0&&chargeTime==0&&comboDelay==0&&sidestepTime==0&&pounceRemaining==0&&pounceHeight==0&&comboRemaining==0&&!dodgePending&&dodgePlayer==null&&warning==null&&telegraph==null&&!aggro&&companionTarget==null&&knockVelocity.x==0&&knockVelocity.z==0;
    }
    public partial class CombatArea
    {
        PlayerController owner;GameSession session;int epoch;
        public CombatArea(PlayerController player,GameSession session){owner=player;this.session=session;epoch=player.CombatEpoch;}
        public bool Current=>IsCurrentCast;
        public bool ApplyDamage(float amount){if(!IsCurrentCast)return false;owner.Health-=amount;return true;}
    }
    public partial class GameSession
    {
        public readonly ProgressionService Progression=new ProgressionService();
        public PlayerController Player;
        public readonly List<EnemyController> Enemies=new List<EnemyController>();
        public GameObject world=new GameObject("original camp");
        public bool IsDead=true,HasStarted=true,Paused=true,uiBlocking=true,InDungeon,DungeonCleared,InCombat,PracticeActive,BackgroundPaused,changingZone,loadingSaveSnapshot;
        public readonly ScenePreparationState scenePreparation=new ScenePreparationState();
        public bool SceneLoading=>scenePreparation.Active;
        public bool InputBlocked=>SceneLoading||!HasStarted||IsDead||Paused||uiBlocking;
        public bool CombatEffectsEnded=>false;
        public bool PreserveFails,SaveFails,CommitFails;
        public int SaveCalls,PreserveCalls,ResetCalls,CommitCalls,PrepareCalls,StartCalls,WorldBuilds,TimescaleUpdates,PracticeEnds,Interruptions,PreparedHub=-1;
        public string Notification;
        public readonly GameUI ui=new GameUI();
        public bool ResetDungeonArgument=true;
        public GameSession(HeroClass hero=HeroClass.Vanguard){Player=new PlayerController(this,hero);var live=new EnemyController{session=this};var dead=new EnemyController{session=this,Health=0};Enemies.Add(live);Enemies.Add(dead);}
        bool PreserveWorldLoot(){PreserveCalls++;return !PreserveFails;}
        bool SaveBeforeLeaving(){SaveCalls++;return !SaveFails;}
        void ResetExpedition(bool dungeon){ResetCalls++;ResetDungeonArgument=dungeon;}
        void Notify(string value){Notification=value;}
        void UpdateTimeScale(){TimescaleUpdates++;}
        public void EndPractice(string reason){PracticeEnds++;PracticeActive=false;}
        public void OnEnemyInterrupted(EnemyController enemy){Interruptions++;}
        Vector3 DungeonPoint(Vector3 point)=>point;
        // Scene creation is a host boundary. The real ChangeZone/Recover methods
        // decide whether this boundary is entered and whether identities survive.
        bool CommitZoneState(bool dungeon)
        {
            CommitCalls++;
            if(CommitFails||(!loadingSaveSnapshot&&!SaveBeforeLeaving()))return false;
            world?.SetActive(false);world=new GameObject("prepared destination");WorldBuilds++;InDungeon=dungeon;return true;
        }
        IEnumerator PrepareZone(bool dungeon){PrepareCalls++;PreparedHub=CurrentHub;return Empty();}
        static IEnumerator Empty(){yield break;}
        void StartScenePreparation(IEnumerator work,string label){StartCalls++;scenePreparation.Begin(label);}
        public bool EnterDungeon()=>ChangeZone(true);
        public bool RecoverCampForProbe()=>ChangeZone(false);
    }
}

static class CampRecoveryChecks
{
    static int checks;
    static void Check(bool okay,string message){checks++;if(!okay)throw new Exception(message);}
    static void Main()
    {
        var camera=new Camera();var follow=new Emberfall.AdventureCamera();camera.gameObject.Put(follow);Camera.main=camera;
        foreach(bool preserveFail in new[]{false,true})
        {
            var game=new Emberfall.GameSession{SaveFails=!preserveFail,PreserveFails=preserveFail};var world=game.world;int epoch=game.Player.CombatEpoch;
            game.Respawn();
            Check(game.IsDead&&game.Player.IsDead&&game.Paused&&game.uiBlocking&&game.world==world&&game.Player.CombatEpoch==epoch&&game.ResetCalls==0&&game.PrepareCalls==0&&game.Enemies[0].preparing,"failed save leaves death and recovery state intact");
            Check(game.SaveCalls==(preserveFail?0:1)&&game.ui.ForegroundCancellations==0,"refused persistence cannot cancel input or mutate world ownership");
            game.SaveFails=game.PreserveFails=false;game.Respawn();Check(!game.IsDead&&!game.SceneLoading&&game.world==world,"failed persistence can retry recovery in the retained camp");
        }
        foreach(Emberfall.HeroClass hero in Enum.GetValues(typeof(Emberfall.HeroClass)))for(int hub=0;hub<3;hub++)
        {
            var game=new Emberfall.GameSession(hero);game.Progression.Profile.currentHub=hub;
            var world=game.world;var player=game.Player;var runtime=player.Runtime;var model=player.Model;
            var enemy=game.Enemies[0];var corpse=game.Enemies[1];var corpseWarning=corpse.warning;var liveWarning=enemy.warning;
            var oldField=new Emberfall.CombatArea(player,game);int epoch=player.CombatEpoch,created=GameObject.Created;
            float energy=runtime.Energy,cooldown=runtime.Remaining(6);int snaps=follow.Snaps,syncs=Physics.Synchronizations;
            game.Respawn();
            Check(game.world==world&&game.Player==player&&player.Model==model&&game.PrepareCalls==0&&game.StartCalls==0&&game.CommitCalls==0&&game.WorldBuilds==0&&GameObject.Created==created,"same camp recovery keeps world player model and population instead of preparation");
            Check(player.Health==player.MaxHealth&&!player.IsDead&&!game.IsDead&&!game.Paused&&!game.uiBlocking&&!game.InputBlocked&&!game.SceneLoading,"camp recovery restores health before releasing gameplay gate");
            Check(player.Runtime==runtime&&runtime.Energy==energy&&runtime.Remaining(6)==cooldown&&cooldown>0,"actual SkillRuntime energy and cooldown survive camp recovery");
            Check(player.CombatEpoch>epoch&&!oldField.Current&&!oldField.ApplyDamage(99)&&player.Health==player.MaxHealth,"old epoch damaging field cannot survive camp revival");
            Check(player.TemporaryStateCleared&&player.invulnerability==.65f&&player.modelPoseUpright(),"teleport clears temporary combat state and immediately removes death tilt");
            Check(enemy.AttackRetired&&liveWarning.Destroyed&&enemy.Health==63&&game.Enemies[0]==enemy&&enemy.StoppedCoroutines==1&&enemy.CancelledInvokes==1&&enemy.threatAdmission.Withdrawn==1&&enemy.threatAdmission.Released==1,"living enemy attack and warning are retired without replacement");
            Check(game.Enemies[1]==corpse&&corpse.IsDead&&corpse.CorpseDestroyScheduled&&corpse.StoppedCoroutines==0&&corpse.CancelledInvokes==0&&corpse.warning==corpseWarning&&!corpseWarning.Destroyed&&corpse.gameObject.activeInHierarchy,"existing corpse retains its dissolution and scheduled destruction");
            Check(game.CurrentHub==hub&&game.ResetCalls==1&&!game.ResetDungeonArgument&&game.PreserveCalls==1&&game.SaveCalls==1,"camp recovery preserves hub and performs one successful save admission");
            Check(!Emberfall.MobileControls.Held&&game.ui.ForegroundCancellations==1&&follow.Snaps==snaps+1&&Physics.Synchronizations==syncs+1&&Emberfall.SummonedCompanion.LastTransferOwnerAlive,"input camera physics and living partner transfer are reconciled");
            game.Respawn();Check(game.ResetCalls==1&&game.SaveCalls==1&&player.CombatEpoch==epoch+1,"repeated respawn after recovery cannot repeat save or epoch transition");
        }
        {
            var game=new Emberfall.GameSession{InDungeon=true};var world=game.world;game.Respawn();
            Check(game.PrepareCalls==1&&game.StartCalls==1&&game.CommitCalls==1&&game.WorldBuilds==1&&game.world!=world&&!world.activeInHierarchy&&game.SceneLoading&&!game.IsDead&&game.InputBlocked,"dungeon death retains guarded preparation before gameplay resumes");
        }
        {
            var game=new Emberfall.GameSession{InDungeon=true,CommitFails=true};var world=game.world;game.Respawn();
            Check(game.IsDead&&game.Player.IsDead&&game.world==world&&game.StartCalls==0,"failed dungeon transition preserves the death gate");
        }
        {
            var game=new Emberfall.GameSession{IsDead=false,Paused=false,uiBlocking=false};game.Player.Health=22;var world=game.world;
            Check(game.TravelToHub(1)&&game.CurrentHub==1&&game.PreparedHub==1&&game.StartCalls==1&&game.WorldBuilds==1&&game.ResetCalls==0&&game.world!=world&&!world.activeInHierarchy&&game.SceneLoading&&!game.loadingSaveSnapshot,"committed hub travel prepares destination instead of reusing old hub");
        }
        {
            var game=new Emberfall.GameSession{IsDead=false,Paused=false,uiBlocking=false};game.Player.Health=22;var world=game.world;game.Progression.TravelFails=true;
            Check(!game.TravelToHub(1)&&game.CurrentHub==0&&game.world==world&&game.StartCalls==0&&!game.loadingSaveSnapshot,"failed hub transaction retains original destination and world");
        }
        {
            var game=new Emberfall.GameSession{IsDead=false,Paused=false,uiBlocking=false};game.Player.Health=22;var world=game.world;float energy=game.Player.Runtime.Energy,cooldown=game.Player.Runtime.Remaining(6);
            game.ReturnToCamp();Check(game.world==world&&game.StartCalls==0&&!game.SceneLoading&&game.Player.Health==game.Player.MaxHealth&&game.Player.Runtime.Energy==energy&&game.Player.Runtime.Remaining(6)==cooldown,"living ReturnToCamp reuses camp and preserves combat resources");
        }
        {
            var game=new Emberfall.GameSession{IsDead=false,Paused=false,uiBlocking=false,InCombat=true};game.Player.Health=22;game.ReturnToCamp();
            Check(game.SaveCalls==0&&game.ResetCalls==0&&game.PrepareCalls==0&&game.Player.Health==22,"active combat cannot use return-to-camp recovery");
        }
        {
            var game=new Emberfall.GameSession{IsDead=false,PracticeActive=true};game.Player.Health=22;game.ReturnToCamp();
            Check(game.PracticeEnds==1&&game.SaveCalls==0&&game.ResetCalls==0&&game.PrepareCalls==0&&game.Player.Health==22,"practice return remains with isolated owner restoration");
        }
        {
            var game=new Emberfall.GameSession();game.scenePreparation.Begin("busy");var world=game.world;int epoch=game.Player.CombatEpoch;game.Respawn();
            Check(game.IsDead&&game.world==world&&game.Player.CombatEpoch==epoch&&game.StartCalls==0&&game.SaveCalls==0,"existing scene loading cannot enter same-world recovery");
        }
        {
            var game=new Emberfall.GameSession{IsDead=false,Paused=false,uiBlocking=false};game.Player.Health=22;var world=game.world;
            Check(game.EnterDungeon()&&game.world!=world&&game.PrepareCalls==1&&game.SceneLoading,"entering dungeon never takes the camp reuse path");
        }
        Camera.main=null;
        {
            var game=new Emberfall.GameSession();game.Respawn();Check(!game.IsDead&&!game.SceneLoading,"camp recovery tolerates missing optional camera");
        }
        Console.WriteLine("PASS "+checks+" actual camp recovery/teleport/enemy attack/cast-owner checks; real SkillRuntime, scene/persistence/native-render boundaries doubled.");
    }
}

namespace Emberfall
{
    public partial class PlayerController
    {
        public bool modelPoseUpright()=>model.transform.localRotation.x==0&&model.transform.localRotation.y==0&&model.transform.localRotation.z==0&&model.transform.localRotation.w==1&&model.Resets>0&&model.OwnerAlive;
    }
}

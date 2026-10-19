using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameSession
    {
        public bool TryMainObjectivePosition(out Vector3 destination)
        {
            destination=Vector3.zero;if(!HasStarted||IsDead||Player==null)return false;
            if(ChapterActive&&ChapterRun.DoorUnlocked&&!ChapterFinished&&chapterPlan!=null){destination=chapterPlan.Exit;return true;}
            if(InDungeon)return false;
            var goal=Progression.SelectedProgressionGoal(IsInCamp);
            if((goal.Identity??"").StartsWith("main/chapter/")||goal.Identity=="main/complete"){destination=ChapterEntrancePosition;return true;}
            if((goal.Identity??"").StartsWith("main/tier/")){destination=DungeonEntrancePosition;return true;}
            return false;
        }
        public ChapterNode SelectedChapterNode {get;set;}
        private readonly int[] selectedChapterTiers=new int[3];
        public int SelectedChapterTier
        {get{int index=(int)SelectedChapterNode;int maximum=ChapterProgression.AvailableTier(Progression.Profile,SelectedChapterNode);return selectedChapterTiers[index]==0?maximum:Mathf.Clamp(selectedChapterTiers[index],1,maximum);}set{selectedChapterTiers[(int)SelectedChapterNode]=value;}}
        public bool SelectedChapterLimitedHealing {get{return false;}set{}}
        public int SelectedChapterTactic {get;set;}=-1;
        public ChapterCombatRun ChapterRun {get;private set;}
        public int ChapterRewardMaterials {get{return ChapterResult!=null&&ChapterResult.Saved?ChapterResult.Materials:0;}}
        public ChapterResultSnapshot ChapterResult {get;private set;}
        private int chapterEntryPotions,chapterBossDeathFrame=-1;
        private bool chapterResultSkipped;
        private float chapterFirstSealSeconds,chapterSecondSealSeconds;
        public bool ChapterResultReady {get{return ChapterFinished&&(ChapterRun.Failed||chapterResultSkipped||FinishedResultReady);}}
        public void ContinueChapterResult(){if(SceneLoading||!ChapterFinished)return;chapterResultSkipped=true;victoryTransitionRemaining=0;LargeBossShutdownVisual.Skip(this);}
        public bool ChapterActive {get{return ChapterRun!=null;}}
        public bool ChapterFinished {get{return ChapterActive&&ChapterRun.Finished;}}
        public bool ChapterRewardPending {get{return ChapterFinished&&!ChapterRun.Failed&&!ChapterRun.RewardClaimed;}}
        public ChapterNode ActiveChapterNode {get{return ChapterActive?ChapterRun.Node:SelectedChapterNode;}}
        public int ChapterRoomIndex {get{return ChapterActive?ChapterRun.RoomIndex:-1;}}
        public int ChapterSeed {get{return ChapterActive?ChapterRun.Seed:0;}}
        public bool OpenChapterSelectionAllowed {get{return !SceneLoading&&HasStarted&&!InDungeon&&!IsDead&&!Paused&&!BackgroundPaused&&(IsInCamp||IsNearChapterEntrance);}}
        public bool NearChapterExit {get{return ChapterActive&&!ChapterFinished&&ChapterRun.DoorUnlocked&&chapterPlan!=null&&Player!=null&&Vector3.Distance(Player.transform.position,chapterPlan.Exit)<3.8f;}}
        private ChapterRunReceipt chapterReceipt;
        private ChapterRoomPlan chapterPlan;
        private bool enteringChapter;
        private int chapterRetrySeed,chapterRetryTier,lastChapterRetryFrame=-1;
        private bool chapterRetryLimited;
        private RunBlessing? chapterRetryTactic;
        public bool CanRetryChapter {get{return !SceneLoading&&HasStarted&&InDungeon&&ChapterFinished&&ChapterRun.Failed&&Player!=null&&chapterReceipt!=null&&chapterReceipt.SavePath==Progression.SaveFilePath&&!enteringChapter&&!changingZone&&lastChapterRetryFrame!=Time.frameCount;}}
        public bool RetryFailedChapter()
        {return CanRetryChapter&&RestartChapterAttempt();}
        private bool CanAdvanceChapterTier {get{return ChapterFinished&&!ChapterRun.Failed&&!ChapterRewardPending&&chapterReceipt!=null&&chapterReceipt.SavePath==Progression.SaveFilePath&&!enteringChapter&&chapterRetryTier<int.MaxValue&&ChapterProgression.CanEnter(Progression.Profile,ChapterRun.Node,chapterRetryTier+1);}}
        private bool RestartChapterAttempt(int? targetTier=null)
        {
            if(SceneLoading)return false;
            if(!SaveBeforeLeaving())return false;
            ChapterRunReceipt receipt;
            if(!Progression.TryBeginChapterNode(ChapterRun.Node,targetTier??chapterRetryTier,out receipt))
            {Notify(Progression.LastError);return false;}
            // A new receipt invalidates all callbacks from the failed attempt. Seed and
            // the admitted tactic are frozen, independent of mutable selection controls.
            lastChapterRetryFrame=Time.frameCount;
            chapterRetryTier=receipt.Tier;
            chapterReceipt=receipt;ChapterRun=new ChapterCombatRun(receipt.Node,chapterRetrySeed);
            chapterEntryPotions=Progression.Profile.potions;chapterFirstSealSeconds=chapterSecondSealSeconds=0;
            ChapterResult=null;chapterBossDeathFrame=-1;chapterResultSkipped=false;
            enteringChapter=true;ChallengeRun=chapterRetryLimited;
            try
            {
                if(!ChangeZone(true))return false;
                IsDead=false;Paused=false;uiBlocking=false;
                if(ChapterFinished)return false;
                if(chapterRetryTactic.HasValue)RunChoices.RestoreChapterTactic(chapterRetryTactic.Value);
                Notify((targetTier.HasValue?"挑战第 "+receipt.Tier+" 阶":"已按原条件从节点起点重试")+" · "+ChapterObjectiveStatus);return true;
            }
            catch(System.Exception error){IsDead=false;FailChapter("重试生成异常："+error.Message);return false;}
            finally {enteringChapter=false;changingZone=false;UpdateTimeScale();}
        }
        private GameObject chapterExit,chapterObjective;
        private readonly GameObject[] chapterObjectives=new GameObject[2];
        private EnemyController chapterSupplier;
        // Session-only history of the last successfully admitted eligible Redrock replay.
        // Another owner's eligible replay or a new host starts split; other visits do not replace history.
        private string redrockRouteOwner;
        private bool previousRedrockSplit;

        private sealed class ChapterEnemyReceipt {public ChapterCombatRun Run;public int Room,Epoch,Index;}
        private readonly Dictionary<EnemyController,ChapterEnemyReceipt> chapterEnemies=new Dictionary<EnemyController,ChapterEnemyReceipt>();
        private Vector3 ChapterObjectivePoint {get{return chapterPlan.Objectives[Mathf.Clamp(ChapterRun.Seals,0,chapterPlan.Objectives.Length-1)];}}
        public string ChapterObjectiveStatus
        {
            get
            {
                if(!ChapterActive)return "";
                if(ChapterFinished)return ChapterRun.Failed?"章节挑战结束 · 返回营地":ChapterRewardPending?"节点完成 · 奖励待保存":"节点完成 · 奖励已保存";
                string prefix="房间 "+(ChapterRoomIndex+1)+" / "+ChapterDefinition.RoomCount(ActiveChapterNode)+" · ";
                if(ChapterRun.DoorUnlocked)return prefix+(ChapterRoomIndex+1<ChapterDefinition.RoomCount(ActiveChapterNode)?"目标完成 · 前往出口进入下一房":"目标完成 · 前往出口完成节点");
                if(ChapterRun.Objective==RoomObjective.Hunt)return prefix+"击败金环断供目标 · 可绕开其余敌人";
                if(ChapterRun.Objective==RoomObjective.Boss)return prefix+"击败星环执政官即可通关 · 摧毁供能锚可中止扫射";
                return prefix+(ChapterRun.Objective==RoomObjective.Purify?"净化 "+ChapterRun.Seals+"/1":"撤离")+" · 站入光环 "+ChapterRun.Progress.ToString("0.0")+"/"+(ChapterRun.Objective==RoomObjective.Purify?"2":"2.5")+"秒 · 敌人入环时暂停累计";
            }
        }
        public string ChapterObjectiveCompact
        {
            get
            {
                if(!ChapterActive)return "";
                if(ChapterFinished)return ChapterRun.Failed?"挑战结束\n返回营地重整":"节点完成\n"+(ChapterRewardPending?"奖励待保存 · 可重试":"奖励已保存 · 返回营地");
                if(ChapterRun.DoorUnlocked)return ChapterRoomIndex+1<ChapterDefinition.RoomCount(ActiveChapterNode)?"目标完成\n前往出口 · 进入下一房":"目标完成\n前往出口 · 完成节点";
                if(ChapterRun.Objective==RoomObjective.Hunt)return "猎杀金环断供目标\n可绕开其余敌人";
                if(ChapterRun.Objective==RoomObjective.Boss)return "击败首领即可通关\n摧毁供能锚中止扫射";
                return (ChapterRun.Objective==RoomObjective.Purify?"净化 "+ChapterRun.Seals+"/1":"撤离")+" · "+ChapterRun.Progress.ToString("0.0")+"/"+(ChapterRun.Objective==RoomObjective.Purify?"2":"2.5")+"秒\n敌人入环暂停累计";
            }
        }
        public bool ConfirmChapterEnter()
        {
            if(SceneLoading||!OpenChapterSelectionAllowed||Player==null)return false;
            if(!SaveBeforeLeaving())return false;
            ChapterRunReceipt receipt;
            if(!Progression.TryBeginChapterNode(SelectedChapterNode,SelectedChapterTier,out receipt)){Notify(Progression.LastError);return false;}
            chapterEntryPotions=Progression.Profile.potions;chapterFirstSealSeconds=chapterSecondSealSeconds=0;ChapterResult=null;chapterBossDeathFrame=-1;chapterResultSkipped=false;
            int seed=Random.Range(0,1000000);
            bool redrockReplay=receipt.Node==ChapterNode.Redrock&&
                (Progression.Profile.chapterCompletedMask&(1<<(int)ChapterNode.Redrock))!=0;
            if(redrockReplay)seed=ChapterRoomGeometry.RedrockReplaySeed(seed,redrockRouteOwner!=Progression.SaveFilePath||!previousRedrockSplit);
            chapterRetrySeed=seed;chapterRetryTier=receipt.Tier;chapterRetryLimited=SelectedChapterLimitedHealing;chapterRetryTactic=SelectedChapterTactic>=0&&SelectedChapterTactic<3&&RunChoices.ChapterTacticsAvailable(Progression.Profile,receipt.Node)?(RunBlessing?)RunChoices.ChapterTactic(Progression.Profile,MobileControls.Active,SelectedChapterTactic):null;
            chapterReceipt=receipt;ChapterRun=new ChapterCombatRun(receipt.Node,seed);
            enteringChapter=true;ChallengeRun=SelectedChapterLimitedHealing;
            try {if(!ChangeZone(true)){ResetChapterRun();return false;}}
            catch(System.Exception error){FailChapter("房间 "+(ChapterRoomIndex+1)+" 生成异常："+error.Message);return InDungeon;}
            finally {enteringChapter=false;changingZone=false;}
            if(redrockReplay&&ChapterRun!=null&&!ChapterRun.Finished&&!ChapterRun.Failed)
            {redrockRouteOwner=Progression.SaveFilePath;previousRedrockSplit=ChapterRoomGeometry.RedrockSplitRoute(seed);}
            if(ChapterRun!=null&&!ChapterRun.Finished&&!ChapterRun.Failed&&SelectedChapterTactic>=0)RunChoices.ChooseChapterTactic(Progression.Profile,receipt.Node,MobileControls.Active,SelectedChapterTactic);
            foreach(var tactic in RunChoices.Active)chapterRetryTactic=tactic;
            UpdateTimeScale();Notify(ChapterDefinition.Get(receipt.Node).Story+" · "+ChapterObjectiveStatus);return true;
        }
        private ThreatAdmissionPolicy chapterThreatAdmission;
        private void ResetChapterRun()
        {
            if(chapterThreatAdmission!=null)chapterThreatAdmission.Reset();chapterThreatAdmission=null;
            if(ChapterRun!=null)ChapterRun.Fail();ChapterRun=null;chapterReceipt=null;chapterPlan=null;ChapterResult=null;chapterBossDeathFrame=-1;chapterResultSkipped=false;
            chapterEnemies.Clear();chapterSupplier=null;chapterExit=chapterObjective=null;chapterObjectives[0]=chapterObjectives[1]=null;chapterFirstSealSeconds=chapterSecondSealSeconds=0;
            if(Progression!=null)Progression.CancelChapterRun();
        }
        private void BeginChapterRoom()
        {
            if(chapterThreatAdmission!=null)chapterThreatAdmission.Reset();
            chapterThreatAdmission=null;
            ChapterRun.BindRoom(Player.CombatEpoch);chapterEnemies.Clear();chapterSupplier=null;
            DungeonWave=ChapterRoomIndex+1;wavePopulation=Mathf.Max(1,ChapterRun.EnemyCount);objectiveHealedThisWave=false;
            chapterExit=WorldBuilder.MakeRoomExitPortal(world.transform,chapterPlan.Exit);chapterExit.transform.SetParent(world.transform,true);chapterExit.SetActive(ChapterRun.DoorUnlocked);
            if(!WorldTraversal.CanReach(chapterPlan.Entrance,chapterPlan.Exit,.65f)){FailChapter("房间 "+(ChapterRoomIndex+1)+"：入口至出口不可达");return;}
            if(ChapterRun.Objective==RoomObjective.Rest)
            {if(!ChallengeRun)Player.Heal(Player.MaxHealth*.25f);else HealingCharges=Mathf.Min(3,HealingCharges+1);Notify("星台休憩 · 整备后前往出口迎战首领");return;}
            if(ChapterRun.Objective==RoomObjective.Purify||ChapterRun.Objective==RoomObjective.Escape)
            {
                foreach(var point in chapterPlan.Objectives)if(!WorldTraversal.CanReach(chapterPlan.Entrance,point,.65f)){FailChapter("房间 "+(ChapterRoomIndex+1)+"：入口至封印/撤离目标不可达");return;}
                if(ChapterRun.Objective==RoomObjective.Purify)
                {
                    for(int i=0;i<1;i++){var marker=WorldBuilder.MakeRoomObjective(chapterPlan.Objectives[i],true);marker.transform.SetParent(world.transform,true);chapterObjectives[i]=marker;TacticalCaptureVisual.AttachChapter(marker,this,i);}
                    chapterObjective=chapterObjectives[0];
                }
                else {chapterObjective=WorldBuilder.MakeRoomObjective(ChapterObjectivePoint);chapterObjective.transform.SetParent(world.transform,true);TacticalCaptureVisual.Attach(chapterObjective,this);}
            }
            var occupied=new List<Vector3>();
            for(int index=0;index<ChapterRun.EnemyCount;index++)
            {
                bool boss=ChapterRun.Objective==RoomObjective.Boss&&index==0;
                bool crossfire=ActiveChapterNode==ChapterNode.Redrock;
                EnemyKind kind=crossfire?(index<2?EnemyKind.Wisp:index<4?EnemyKind.Guardian:index==4?EnemyKind.Goblin:EnemyKind.Slime):
                    boss?EnemyKind.Guardian:index==0?EnemyKind.Wisp:index%3==1?EnemyKind.Guardian:index%3==2?EnemyKind.Goblin:EnemyKind.Slime;
                Vector3 point;float radius=boss?BossSpawnRadius:kind==EnemyKind.Guardian?.65f:.5f;
                if(Enemies.Count>=ChapterRun.EnemyCount||!TryChapterSpawn(index,crossfire,occupied,radius,out point)||
                    !WorldTraversal.CanReach(chapterPlan.Entrance,point,radius)||!ChapterRun.Register(ChapterRoomIndex,Player.CombatEpoch,index))
                {FailChapter("房间 "+(ChapterRoomIndex+1)+"：敌人 #"+index+" 生成/可达性登记失败");return;}
                occupied.Add(point);SpawnEnemy(kind,DungeonEntryLevel,point,boss);var enemy=Enemies[Enemies.Count-1];
                if(chapterThreatAdmission!=null&&index<4&&!boss&&(kind==EnemyKind.Guardian||kind==EnemyKind.Wisp))
                    enemy.ConfigureThreatAdmission(chapterThreatAdmission,index);
                chapterEnemies.Add(enemy,new ChapterEnemyReceipt{Run=ChapterRun,Room=ChapterRoomIndex,Epoch=Player.CombatEpoch,Index=index});
                if(!Progression.RegisterChapterEnemy(chapterReceipt,ChapterRoomIndex,index,boss))
                {FailChapter("房间 "+(ChapterRoomIndex+1)+"：敌人 #"+index+" 经验预算登记失败");return;}
                if(index==0&&!boss)chapterSupplier=enemy;
                if(crossfire)
                    enemy.ConfigureEscapePost(index==0?EscapeRole.GateSupplier:index==1||index==5?EscapeRole.SideFlanker:index<4?EscapeRole.GateGuard:EscapeRole.Pursuer,point);
                if(ActiveChapterNode==ChapterNode.ForestCourt&&ChapterRoomIndex==0&&(index==4))
                    enemy.ConfigureEscapePost(EscapeRole.GateGuard,point);
                TacticalEnemyVisual.Attach(enemy,this);
                if(boss)LargeExpeditionBoss.ConfigureChapter(enemy,DungeonTier,ChapterSeed);
            }
            ChapterHazards.Configure(this,ActiveChapterNode,ChapterRoomIndex,ChapterSeed,chapterPlan);
        }
        private bool TryChapterSpawn(int index,bool crossfire,List<Vector3> occupied,float radius,out Vector3 point)
        {
            if(index>=6)return ChapterRoomGeometry.TrySpawn(chapterPlan,index,occupied,radius,out point);
            if(!crossfire)return ChapterRoomGeometry.TrySpawn(chapterPlan,index,occupied,radius,out point);
            // The hunt target remains receipt index zero. Two existing casters occupy
            // opposite branches; stone guardians hold the exit rather than chasing across both lanes.
            if(index==2||index==3)
                return ChapterRoomGeometry.TrySpawnAt(chapterPlan,new Vector3(index==2?-2:2,0,11),occupied,radius,out point);
            int slot=index==0?(ChapterRoomIndex==0?0:2):index==1?1:index==4?4:5;
            return ChapterRoomGeometry.TrySpawn(chapterPlan,slot,occupied,radius,out point);
        }
        private void TickChapterRun()
        {
            if(!ChapterActive||ChapterFinished||InputBlocked||Player==null)
            {if(chapterThreatAdmission!=null){if(!ChapterActive||ChapterFinished)chapterThreatAdmission.Reset();else chapterThreatAdmission.Advance(0,true);}return;}
            if(chapterThreatAdmission!=null)chapterThreatAdmission.Advance(Time.deltaTime);
            if(chapterObjective!=null&&!ChapterRun.DoorUnlocked)
            {
                if(ChapterRun.Objective==RoomObjective.Purify)
                {
                    for(int i=0;i<1;i++)ChapterRun.AdvanceSeal(i,Time.deltaTime,true,RoomTacticalRegion.ContainsPlayer(CombatFx.Flat(Player.transform.position-chapterPlan.Objectives[i]).sqrMagnitude),ChapterSealContested(i));
                    chapterFirstSealSeconds=ChapterRun.SealProgress(0);chapterSecondSealSeconds=ChapterRun.SealProgress(1);
                    if(ChapterRoomIndex==0&&ChapterRun.Seals==1&&ChapterRun.LivingRegisteredEnemies>=2)Progression.RecordChapterMastery(chapterReceipt,1);
                }
                else
                {
                    bool contested=false;foreach(var enemy in Enemies)if(IsChapterContesting(enemy)){contested=true;break;}
                    ChapterRun.Advance(Time.deltaTime,true,RoomTacticalRegion.ContainsPlayer(CombatFx.Flat(Player.transform.position-ChapterObjectivePoint).sqrMagnitude),contested);
                }
            }
            FinalizeChapterBoss();
            if(!ChapterFinished&&ChapterRun.DoorUnlocked){if(chapterExit!=null)chapterExit.SetActive(true);}

        }
        public bool IsChapterHuntTarget(EnemyController enemy){return ChapterActive&&!ChapterFinished&&ChapterRun.Objective==RoomObjective.Hunt&&LiveRoomEnemy(enemy)&&enemy==chapterSupplier;}
        private bool ChapterSealContested(int index)
        {foreach(var enemy in Enemies)if(IsChapterContestingPoint(enemy,index))return true;return false;}
        public bool IsChapterContestingPoint(EnemyController enemy,int index)
        {return ChapterActive&&!ChapterFinished&&ChapterRun.Objective==RoomObjective.Purify&&index>=0&&index<1&&!ChapterRun.SealComplete(index)&&LiveRoomEnemy(enemy)&&RoomTacticalRegion.Contests(CombatFx.Flat(enemy.transform.position-chapterPlan.Objectives[index]).sqrMagnitude,enemy.NavigationRadius,true)&&WorldTraversal.HasLineOfSight(enemy.transform.position,chapterPlan.Objectives[index]);}
        public bool TryGetChapterSeal(int index,out float fraction,out bool contested,out bool complete)
        {
            fraction=0;contested=complete=false;
            if(!ChapterActive||ChapterFinished||ChapterRun.Objective!=RoomObjective.Purify||index<0||index>=1)return false;
            complete=ChapterRun.SealComplete(index);fraction=complete?1:ChapterRun.SealProgress(index)/2f;contested=!complete&&ChapterSealContested(index);return true;
        }
        public Vector3 ChapterNextObjectivePoint
        {
            get
            {
                if(!ChapterActive||chapterPlan==null)return Vector3.zero;
                if(ChapterRun.DoorUnlocked)return chapterPlan.Exit;
                if(ChapterRun.Objective!=RoomObjective.Purify)return ChapterObjectivePoint;
                int best=-1;float distance=float.MaxValue;
                for(int i=0;i<1;i++)if(!ChapterRun.SealComplete(i)){float d=Player==null?i:(Player.transform.position-chapterPlan.Objectives[i]).sqrMagnitude;if(d<distance){distance=d;best=i;}}
                return best<0?chapterPlan.Exit:chapterPlan.Objectives[best];
            }
        }
        public bool IsChapterContesting(EnemyController enemy)
        {
            if(ChapterActive&&ChapterRun.Objective==RoomObjective.Purify)return IsChapterContestingPoint(enemy,0)||IsChapterContestingPoint(enemy,1);
            return ChapterActive&&!ChapterFinished&&chapterObjective!=null&&!ChapterRun.DoorUnlocked&&LiveRoomEnemy(enemy)&&RoomTacticalRegion.Contests(CombatFx.Flat(enemy.transform.position-ChapterObjectivePoint).sqrMagnitude,enemy.NavigationRadius,true)&&WorldTraversal.HasLineOfSight(enemy.transform.position,ChapterObjectivePoint);
        }
        private bool ChapterHasSupport {get{return ChapterActive&&!ChapterFinished&&ActiveChapterNode==ChapterNode.ForestCourt&&chapterSupplier!=null&&!chapterSupplier.IsDead&&chapterSupplier.isActiveAndEnabled;}}
        public bool IsChapterSupplier(EnemyController enemy){return ChapterHasSupport&&enemy==chapterSupplier;}
        public float ChapterSupportMultiplier(EnemyController enemy)
        {return ChapterHasSupport&&enemy!=null&&enemy!=chapterSupplier&&!enemy.IsBoss&&!enemy.IsDead&&enemy.isActiveAndEnabled&&RoomTacticalRegion.ReceivesSupport(CombatFx.Flat(enemy.transform.position-chapterSupplier.transform.position).sqrMagnitude,true)&&WorldTraversal.HasLineOfSight(enemy.transform.position,chapterSupplier.transform.position)?.7f:1;}
        private bool IsCurrentChapterBoss(EnemyController enemy)
        {
            ChapterEnemyReceipt receipt;
            return ChapterActive&&!ChapterFinished&&ActiveChapterNode==ChapterNode.StarPlatform&&Player!=null&&enemy!=null&&enemy.IsBoss&&
                chapterEnemies.TryGetValue(enemy,out receipt)&&receipt.Run==ChapterRun&&receipt.Room==ChapterRoomIndex&&receipt.Epoch==Player.CombatEpoch&&receipt.Index==0;
        }
        internal void RecordChapterAnchorExposure(EnemyController enemy)
        {if(IsCurrentChapterBoss(enemy))Progression.RecordChapterMastery(chapterReceipt,8);}
        internal void RecordChapterInterrupt(EnemyController enemy)
        {if(IsCurrentChapterBoss(enemy))Progression.RecordChapterMastery(chapterReceipt,4);}
        private bool RecordChapterDefeat(EnemyController enemy,out int experience)
        {
            experience=0;ChapterEnemyReceipt receipt;
            if(!ChapterActive||Player==null||!chapterEnemies.TryGetValue(enemy,out receipt)||receipt.Run!=ChapterRun||receipt.Epoch!=Player.CombatEpoch||
                !ChapterRun.Defeat(receipt.Room,receipt.Epoch,receipt.Index))return false;
            if(!Progression.TryClaimChapterEnemyExperience(chapterReceipt,receipt.Room,receipt.Index,out experience))
            {FailChapter("敌人 #"+receipt.Index+" 经验收据无法确认");return false;}
            if(ActiveChapterNode==ChapterNode.Redrock&&receipt.Room==0&&receipt.Index==0&&ChapterRun.LivingRegisteredEnemies==5)Progression.RecordChapterMastery(chapterReceipt,2);
            chapterEnemies.Remove(enemy);
            if(enemy.IsBoss)chapterBossDeathFrame=Time.frameCount;
            return true;
        }
        private void FinalizeChapterBoss(){if(ChapterActive&&(ChapterRun.FinishBoss()||ChapterRun.FinishClearedFinalRoom()))FinalizeChapter();}
        private ChapterResultSnapshot CaptureChapterResult(bool failed,string reason)
        {return new ChapterResultSnapshot(ActiveChapterNode,chapterReceipt==null?DungeonTier:chapterReceipt.Tier,chapterEntryPotions,ChallengeRun,ChapterRoomIndex,(chapterFirstSealSeconds>=2?1:0),chapterFirstSealSeconds,chapterSecondSealSeconds,failed,reason,lastDamageSource,lastDamageAmount,Progression.ChapterKillExperienceEarned,MechanismEvidence.Snapshot());}
        private void FailChapter(string reason)
        {if(!ChapterActive||ChapterFinished)return;ChapterResult=CaptureChapterResult(true,reason);ChapterRun.Fail();RunChoices.Reset();Progression.CancelChapterRun();if(!Progression.FinishDungeonRewards())Notify(Progression.LastError);Notify(reason);SuspendInputs();UpdateTimeScale();}
        private void FinalizeChapter()
        {ChapterResult=CaptureChapterResult(false,null);DungeonCleared=true;RetireRemainingEncounterEnemies();chapterEnemies.Clear();TrySettleChapterReward();LastRunSummary=BuildRunSummary(true);UpdateTimeScale();BeginVictoryTransition();GameAudio.Play(SoundCue.Victory);}
        public bool TrySettleChapterReward()
        {
            if(!ChapterRewardPending)return true;
            var source=Progression;var run=ChapterRun;var receipt=chapterReceipt;string slot=source.CurrentSlotId;
            if(!Progression.TryCompleteChapterNode(chapterReceipt)){Notify(Progression.LastError);return false;}
            run.ClaimReward();
            if(Progression!=source||source.CurrentSlotId!=slot||ChapterRun!=run||chapterReceipt!=receipt)return true;
            if(ChapterResult==null)ChapterResult=CaptureChapterResult(false,null);
            ApplyRewardPresentation(receipt.Id);
            var detail=source.GetRewardPresentation(receipt.Id);
            if(detail==null)ChapterResult.RecordSavedUnavailable();
            else ChapterResult.RecordSaved(detail.Materials,detail.FirstCompletion,detail.UnlockedNode,detail.SharedBefore,detail.SharedAfter,detail.Experience,detail.FirstCoreAvailable);
            LastRunSummary=BuildRunSummary(true);
            Notify(detail==null?"章节节点已保存 · 旧回执缺少奖励明细，无法恢复准确数额（不会重复发放）":"章节节点已保存 · 碎片 +"+ChapterResult.Materials);return true;
        }
        public bool EnterNextChapterRoom()
        {
            if(!NearChapterExit||InputBlocked||Player==null||!SaveBeforeLeaving())return false;
            if(!ChapterRun.Exit(true,false))return false;
            if(ChapterFinished){FinalizeChapter();return true;}
            SuspendInputs();changingZone=true;
            try
            {
            AbandonSideEvent();int oldEpoch=Player.CombatEpoch;
            foreach(var enemy in Enemies)if(enemy!=null){enemy.gameObject.SetActive(false);Destroy(enemy.gameObject);}Enemies.Clear();chapterEnemies.Clear();
            foreach(var obj in transientObjects)if(obj!=null){obj.SetActive(false);Destroy(obj);}transientObjects.Clear();
            if(world!=null){world.SetActive(false);Destroy(world);}Player.RetireCombatForWorldTransition();RetireWorldLootReceipts(oldEpoch);
            chapterPlan=ChapterRoomGeometry.Plan(ActiveChapterNode,ChapterRoomIndex,ChapterSeed);DungeonLayout=chapterPlan.Layout;
            world=null;
            StartScenePreparation(PrepareRoom(true),"准备章节下一房间");return true;
            }
            catch(System.Exception error){FailChapter("房间 "+(ChapterRoomIndex+1)+" 生成异常："+error.Message);return false;}
            finally {changingZone=false;}
        }
    }
}

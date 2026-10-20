using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }
        public ProgressionService Progression { get; private set; }
        public PlayerController Player { get; private set; }
        public List<EnemyController> Enemies { get; private set; } = new List<EnemyController>();
        public bool HasStarted { get; private set; }
        public bool Paused { get; private set; }
        public bool InDungeon { get; private set; }
        public bool IsDead { get; private set; }
        public int DungeonWave { get; private set; }
        public int DungeonTier { get; private set; } = 1;
        public int TotalWaves { get { return ChapterActive?ChapterDefinition.RoomCount(ActiveChapterNode):RoomChainRun!=null?5:ModeRun!=null?3:EncounterPlan.FinalWave; } }
        public float ArenaRadius { get { return ArenaSizeRules.Radius(InDungeon?ZoneKind.Dungeon:ZoneKind.Wilderness,DungeonLayout,CurrentHub); } }
        public bool DungeonCleared { get; private set; }
        public int PendingLootCount { get { return pendingLoot.Count; } }
        public string ZoneName { get { return InDungeon ? ModeName + (" · "+DungeonDifficultyName) : HubTravelRules.Name(CurrentHub); } }
        public string Notification
        {
            get
            {
                if (Progression != null && !string.IsNullOrEmpty(Progression.LastError) && Progression.LastError.StartsWith("保存失败"))
                    return Progression.LastError;
                return Time.unscaledTime < notificationUntil ? notification : "";
            }
        }
        public bool InputBlocked { get { return SceneLoading || !HasStarted || Paused || uiBlocking || IsDead || ((ModeFinished||DungeonCleared)&&!FinishedResultDismissed) || (PracticeActive&&PracticeRecord!=null&&PracticeRecord.Finished) || RunChoices.AwaitingChoice || RoomBranchChoiceOpen || DungeonSelectionOpen || pauseState.BackgroundPaused; } }
        public bool PointerOverUI { get { return ui != null && ui.IsPointerOverUI; } }
        public bool CanChangeLoadout { get { return HasStarted && !IsDead && !SceneLoading; } }
        public string Objective
        {
            get
            {
                if (!HasStarted) return "踏入星烬纪元";
                if (IsDead) return "旅途尚未结束 · 返回营地重整旗鼓";
                if(SpecialAdventure)return ModeName+" · "+ModeObjectiveStatus;
                if(wavePreparing)return "第 "+DungeonWave+" 波 · 整波敌人正在集结…";
                if (InDungeon) return DungeonCleared ? "遗迹已肃清 · 前往南侧传送点返回营地" :
                    "肃清遗迹 " + DungeonWave + "/" + TotalWaves + " · 剩余 " + Enemies.Count + " 个敌人";
                if (Progression.Profile.level < 2) return "击败原野怪物，升至 2 级 · 按 K 查看职业技能";
                if (Progression.Profile.skillRanks[0] == 0) return "你已获得技能点！按 K 学习第一个职业技能";
                if(IsNearChapterEntrance)return "按 E 进入星路章节 · 主线副本";
                if (NearPortal()) return "靠近传送门选择冒险 · 三波挑战 / 首领 / 稀有装备";
                return "探索原野收集装备 · 前往北方发光的遗迹传送门";
            }
        }

        private GameObject world;
        private GameUI ui;
        private bool uiBlocking;
        private string equipmentFingerprint;
        private readonly ApplicationPauseState pauseState = new ApplicationPauseState();
        public bool BackgroundPaused { get { return pauseState.BackgroundPaused; } }
        private bool changingZone;
        private float notificationUntil;
        private string notification = "";
        private float respawnTimer;
        private float autosaveTimer;
        private readonly SaveLifecycleGate lifecycleSave=new SaveLifecycleGate();
        private Coroutine waveRoutine;
        private Coroutine wildernessPopulationRoutine;
        private readonly List<GameObject> transientObjects = new List<GameObject>();
        private sealed class PendingLoot
        {
            public ItemData Item;
            public GroundLootPickup Pickup;
            public bool Collecting;
        }
        private readonly Dictionary<string, PendingLoot> pendingLoot = new Dictionary<string, PendingLoot>();
        private readonly HashSet<string> collectedGroundLoot = new HashSet<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
#if UNITY_6000_0_OR_NEWER
            if (FindAnyObjectByType<GameSession>() == null) new GameObject("Emberfall · Game").AddComponent<GameSession>();
#else
            if (FindObjectOfType<GameSession>() == null) new GameObject("Emberfall · Game").AddComponent<GameSession>();
#endif
        }

        private void Awake()
        {
            Debug.Log("Emberfall " + Application.version + " · " + Application.platform);
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Application.targetFrameRate = pauseState.TargetFrameRate(Application.isMobilePlatform, false);
            QualitySettings.vSyncCount = 1;
            QualitySettings.antiAliasing = 4;
            QualitySettings.globalTextureMipmapLimit = 0;
            QualitySettings.resolutionScalingFixedDPIFactor = 1f;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
            if (Application.isMobilePlatform)
            {
                // Mobile pacing uses targetFrameRate; avoid desktop shadow costs at
                // the phone/tablet's native resolution while retaining contact shadows.
                QualitySettings.vSyncCount = 0;
                QualitySettings.shadowDistance = 30;
                QualitySettings.shadowResolution = ShadowResolution.Medium;
                QualitySettings.shadows = ShadowQuality.All;
                QualitySettings.shadowCascades = 1;
                QualitySettings.pixelLightCount = 2;
            }
            ConfigureMobileRendering();
#if UNITY_EDITOR
            string validationDirectory = UnityEditor.SessionState.GetString("Emberfall.ValidationSaveDirectory", "");
            Progression = new ProgressionService(string.IsNullOrEmpty(validationDirectory) ? null : validationDirectory);
#elif EMBERFALL_VISUAL_VALIDATION
            VisualValidationPlayer.EnsureInstalled();
            Progression = new ProgressionService(VisualValidationPlayer.SaveDirectory);
#else
            Progression = new ProgressionService();
#endif
            Application.wantsToQuit += CanQuitSafely;
            Progression.Changed += OnProgressChanged;
            Progression.LeveledUp += OnLevelUp;
            // The title owns an independent backdrop; no camp is built or simulated here.
            ConfigureCamera();
            ui = gameObject.AddComponent<GameUI>();
            ui.Initialize(this);
            gameObject.AddComponent<MobileControls>().Initialize(this);
            Time.timeScale = 0f;
        }

        private void ConfigureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                camera = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            camera.allowMSAA = true;
            camera.allowHDR = !Application.isMobilePlatform && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.DefaultHDR);
            camera.orthographic = false;
            camera.fieldOfView = 48;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 150f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.065f, .1f, .16f);
            camera.transform.position = new Vector3(0, 19, -24);
            camera.transform.rotation = Quaternion.Euler(48f, 0, 0);
            if (camera.GetComponent<AdventureCamera>() == null) camera.gameObject.AddComponent<AdventureCamera>();
        }

        public void StartNew(HeroClass heroClass)
        {
            if (SceneLoading) return;
            // Settle the old character before publishing any new active slot.
            // Otherwise a pending old-run receipt could pay the new character
            // during BeginAdventure's normal zone-change preflight.
            if (!SaveBeforeLeaving()) return;
            string previousEquipment = equipmentFingerprint;
            equipmentFingerprint = null;
            if (!Progression.CreateNewSlot(heroClass)) { equipmentFingerprint = previousEquipment; Notify(Progression.LastError); return; }
            collectedGroundLoot.Clear();
            BeginAdventure();
            Notify("已创建独立存档 · 风语原野");
        }

        public void ContinueGame()
        {
            ContinueAdventure(null);
        }

        public bool ContinueGame(string slotId)
        {
            return ContinueAdventure(slotId);
        }

        public string SaveLoadError { get; private set; }
        private bool loadingSaveSnapshot;

        public bool LoadSaveFromPause(string slotId, bool discardUnsaved, bool alreadySaved = false)
        {
            if (!HasStarted || !Paused) { SaveLoadError = "请先暂停当前冒险。"; return false; }
            return ContinueAdventure(slotId, discardUnsaved, alreadySaved);
        }

        private bool ContinueAdventure(string slotId, bool discardUnsaved = false, bool alreadySaved = false)
        {
            if (SceneLoading) { SaveLoadError = "请等待当前场景准备完成。"; return false; }
            string targetId = slotId ?? Progression.CurrentSlotId ?? "legacy";
            ProgressionService candidate;
            string error;
            if (!SaveSlotTransition.TryStage(Progression, targetId, out candidate, out error))
            { SaveLoadError = error; Notify("存档未能读取：" + error); return false; }
            if (HasStarted && !discardUnsaved && !alreadySaved)
            {
                if (!SaveBeforeLeaving()) { SaveLoadError = Progression.LastError; return false; }
                // Saving and reloading the same role must read the newly saved
                // snapshot, never the preflight's older in-memory version.
                if (targetId == Progression.CurrentSlotId && !SaveSlotTransition.TryStage(Progression, targetId, out candidate, out error))
                { SaveLoadError = error; return false; }
            }
            string loadWarning = candidate.LastError;
            // No old-world callbacks may write into the newly selected character.
            DiscardTransientAdventureForLoad();
            Progression.Changed -= OnProgressChanged;
            Progression.LeveledUp -= OnLevelUp;
            ProgressionService previous = Progression;
            Progression = candidate;
            DiscardForeignSideEventRewards();
            if (ui != null) ui.RebindProgressionNotifications(previous, candidate);
            Progression.Changed += OnProgressChanged;
            Progression.LeveledUp += OnLevelUp;
            autosaveTimer = 0;
            lifecycleSave.Observe(false, false, () => true);
            SaveLoadError = null;
            loadingSaveSnapshot = true;
            try { BeginAdventure(); }
            catch (System.Exception exception)
            {
                DiscardTransientAdventureForLoad();
                SaveLoadError = "营地加载失败，角色存档仍保留。请返回列表重试：" + exception.Message;
                Notify(SaveLoadError);
                return false;
            }
            finally { loadingSaveSnapshot = false; }
            Notify(string.IsNullOrEmpty(loadWarning) ? "已读取「" + GameBalance.ClassName(Progression.Profile.heroClass) +
                " · " + Progression.Profile.level + "级」，返回营地。" : loadWarning);
            return true;
        }

        private void DiscardTransientAdventureForLoad()
        {
            Progression.DiscardDungeonStage();
            EndHubNpcConversation();
            // Only after staging succeeded and the user chose how to handle the
            // current progress. This path intentionally never saves/settles loot.
            HasStarted = false;
            Paused = true;
            uiBlocking = true;
            changingZone = true;
            FloatingNumber.ClearForWorldTransition();
            CancelScenePreparation();
            StopAllCoroutines(); waveRoutine = null; wildernessPopulationRoutine = null; wavePreparing=false;wavePreparationRoutine=null;ClearPreparedWaveActors();
            foreach (PendingLoot loot in pendingLoot.Values)
                if (loot.Pickup != null) loot.Pickup.Retire();
            pendingLoot.Clear(); collectedGroundLoot.Clear();
            foreach (EnemyController enemy in Enemies)
                if (enemy != null) { enemy.gameObject.SetActive(false); Destroy(enemy.gameObject); }
            Enemies.Clear();
            foreach (GameObject obj in transientObjects)
                if (obj != null) { obj.SetActive(false); Destroy(obj); }
            transientObjects.Clear();
            if (Player != null) { Player.gameObject.SetActive(false); Destroy(Player.gameObject); Player = null; }
            if (world != null) { world.SetActive(false); Destroy(world); world = null; }
            // In particular, unclaimed old-mode rewards cannot settle into target.
            ResetExpedition(false);
            InDungeon = false; DungeonCleared = false; IsDead = false; RevivalPending=false;
            equipmentFingerprint = null;
            changingZone = false;
            UpdateTimeScale();
        }

        public bool SaveAsNewSlot()
        {
            if (!HasStarted || IsDead) return false;
            if(ChapterActive&&(!ChapterFinished||ChapterRewardPending)){Notify("请先结束章节挑战，再另存角色。");return false;}
            if (!SaveBeforeLeaving()) return false;
            bool saved = Progression.SaveAsNewSlot();
            Notify(saved ? "已另存为新存档。原进度保留，后续自动保存到新存档。" : Progression.LastError);
            return saved;
        }

        private void BeginAdventure()
        {
            // A checkpoint recovered after interruption still belongs to this character.
            if(!Progression.FinishDungeonRewards()){Notify(Progression.LastError);return;}
            EndHubNpcConversation();
            if(!Progression.CollectGroundSupplies(Progression.Profile.groundGold,Progression.Profile.groundPotions)){Notify(Progression.LastError);return;}
            StartScenePreparation(PrepareAdventure(), "准备进入营地");
        }

        private void Update()
        {
            using (PerformanceDiagnostics.Measure(PerformanceDiagnostics.Section.Session)) UpdateMeasuredSession();
        }

        private void UpdateMeasuredSession()
        {
            UpdateMobileFrameRate();
            if (BackgroundPaused) return;
            if (SceneLoading) return;
            if(!Paused&&VictoryTransitionPresenting)victoryTransitionRemaining=Mathf.Max(0,victoryTransitionRemaining-Time.unscaledDeltaTime);
            if(activeHubNpc!=HubNpcKind.None&&ActiveHubNpc==HubNpcKind.None)EndHubNpcConversation();
            if((ModeFinished||DungeonCleared)&&!FinishedResultDismissed)UpdateTimeScale();
            if(PracticeActive){TickPractice();return;} // Practice owns its guarded potion input too.
            if(HasStarted)TickSideEvent();
            TickWaveTerrain();
            if (InputBlocked) return;
            if(ModeRun!=null){TickArenaRun();if(InputBlocked)return;}
            if(RoomChainRun!=null)TickRoomTactics();
            if(ChapterActive)TickChapterRun();
            if(!InputBlocked&&TryAutoAdvanceRoomPortal())return;
            if (!InputBlocked)
            {
                if(Input.GetKeyDown(KeyCode.E))
                {
                    if(NearDungeonReturn)ui.OpenDungeonExit();
                    else if(NearChapterExit)EnterNextChapterRoom();
                    else if(NearRoomExit)EnterNextRoom();
                    else if(SideEventAvailable)StartSideEvent();
                    else if(!InDungeon)EnterDungeon();
                    if(InputBlocked)return;
                }
                bool touchPotionRequested = MobileControls.ConsumePotion();
                if (Input.GetKeyDown(KeyCode.F) || touchPotionRequested) DrinkPotion();
                if (Input.GetKeyDown(KeyCode.H)) {if(InDungeon&&DungeonReturnAvailable){if(NearDungeonReturn)ui.OpenDungeonExit();else Notify("靠近传送点后交互。");}else ReturnToOrigin();}
            }
            if (!InDungeon && CurrentHub==0)
            {
                respawnTimer -= Time.deltaTime;
                if (respawnTimer <= 0 && Enemies.Count < Mathf.Min(36,26+Progression.Profile.level/8))
                {
                    SpawnWildernessEnemy();
                    respawnTimer = 1.8f;
                }
            }
            if(InDungeon)return;
            autosaveTimer += Time.deltaTime;
            if (autosaveTimer > 25) { autosaveTimer = 0; if(!DungeonRewardPending||TrySettleDungeonReward())Progression.Save(); }
        }

        public void SetPaused(bool value) { Paused = value; if(value)GameAudio.StopNpcGreeting(); UpdateTimeScale(); }
        public void SetUIBlocking(bool value) { uiBlocking = value; if(!value)EndHubNpcConversation(); UpdateTimeScale(); }
        public bool AssignSkill(int hotbarSlot, int skillIndex)
        {
            if (!CanChangeLoadout) { Notify("请先开始冒险，再配置技能快捷栏。"); return false; }
            bool changed = Progression.AssignSkill(hotbarSlot, skillIndex);
            if(!changed&&!string.IsNullOrEmpty(Progression.LastError))Notify(Progression.LastError);
            return changed;
        }
        public bool SetHotbarPage(int page)
        {
            return HasStarted && Progression.SetHotbarPage(page);
        }
        public bool MoveHotbarSkill(int sourceSlot, int targetSlot)
        {
            if (!CanChangeLoadout) return false;
            bool changed = Progression.MoveHotbarSkill(sourceSlot, targetSlot);
            if (!changed&&!string.IsNullOrEmpty(Progression.LastError)) Notify(Progression.LastError);
            return changed;
        }
        private void UpdateMobileFrameRate()
        {
            if (!Application.isMobilePlatform) return;
            int target = ThermalFrameRate(pauseState.TargetFrameRate(true, !InputBlocked));
            if (Application.targetFrameRate != target) Application.targetFrameRate = target;
        }
        private void UpdateTimeScale()
        {
            Time.timeScale = pauseState.CanAdvance(HasStarted, Paused, SceneLoading || uiBlocking || RunChoices.AwaitingChoice || RoomBranchChoiceOpen || DungeonSelectionOpen || (FinishedResultReady&&!FinishedResultDismissed), IsDead&&!RevivalPending) ? 1 : 0;
            UpdateMobileFrameRate();
        }

        public static readonly Vector3 ChapterEntrancePosition=new Vector3(PortalInteractionPolicy.ChapterX,0,PortalInteractionPolicy.EntranceZ);
        public static readonly Vector3 WildernessDungeonEntrancePosition=new Vector3(PortalInteractionPolicy.DungeonX,0,PortalInteractionPolicy.EntranceZ);
        public Vector3 DungeonEntrancePosition {get{return CurrentHub==0?WildernessDungeonEntrancePosition:new Vector3(0,0,11);}}
        public bool IsNearChapterEntrance => !InDungeon&&Player!=null&&PortalInteractionPolicy.IsNear((Player.transform.position-ChapterEntrancePosition).sqrMagnitude);
        public bool IsNearDungeonEntrance {get{return NearPortal();}}
        private bool NearPortal() { return Player != null && PortalInteractionPolicy.IsNear((Player.transform.position-DungeonEntrancePosition).sqrMagnitude); }

        internal void RetreatFromPortal(Vector3 center,float triggerRadius)
        {
            if(Player==null||IsDead||SceneLoading)return;
            Vector3 outward=CombatFx.Flat(Player.transform.position-center).normalized;
            if(outward.sqrMagnitude<.01f)outward=Vector3.back;
            for(float distance=triggerRadius+1f;distance<=triggerRadius+5f;distance+=.5f)
            for(int sample=0;sample<16;sample++)
            {
                int offset=sample==0?0:(sample+1)/2*(sample%2==1?1:-1);
                Vector3 candidate=WorldTraversal.NearestWalkable(center+Quaternion.Euler(0,offset*22.5f,0)*outward*distance,.45f);
                if(CombatFx.Flat(candidate-center).sqrMagnitude<(triggerRadius+.5f)*(triggerRadius+.5f)||
                    !WorldTraversal.CanStand(candidate)||!PlayerBodyPositionClear(candidate))continue;
                Player.RepositionAtOrigin(candidate);MobileControls.ResetInput();return;
            }
        }

        public void EnterDungeon()
        {
            if (!HasStarted || IsDead || InDungeon || DungeonSelectionOpen || InputBlocked) return;
            if (!Progression.CanEnterDungeon) { Notify("先领取营地恢复栏的装备，并为珍贵战利品腾出位置。"); return; }
            if (Progression.Profile.pendingFashionChest || Progression.Profile.pendingChestReveal) { Notify("请先开启上次通关的宝箱。"); return; }
            if(IsNearChapterEntrance){ui.OpenChapterEntrance();return;}
            if (!NearPortal()) return;
            if (Progression.Profile.level < 2) { Notify("遗迹需要 2 级。先在原野战斗，并学习第一个职业技能。"); return; }
            ResetAdventureEntryTiers();
            DungeonSelectionOpen = true;
            SelectedDungeonTier = Mathf.Clamp(SelectedDungeonTier, 1, MaximumDungeonTier);
            UpdateTimeScale();
        }

        public void ReturnToOrigin()
        {
            if (!HasStarted || IsDead || Player == null || InputBlocked) return;
            Vector3 origin = ChapterActive ? chapterPlan.Entrance : InDungeon && RoomChainRun != null
                ? DungeonPoint(TacticalRoomGeometry.Entrance) : DungeonPoint(new Vector3(0, 0, InDungeon ? -9 : -10));
            Player.RepositionAtOrigin(origin);
            var camera = Camera.main;
            if (camera != null)
            {
                var adventureCamera = camera.GetComponent<AdventureCamera>();
                if (adventureCamera != null) adventureCamera.Snap();
            }
        }

        public void ReturnToCamp()
        {
            if(PracticeActive){EndPractice("主动离开 · 记录提前结束");return;}
            if (!HasStarted || IsDead) return;
            if(InCombat){Notify("正在战斗，脱离战斗后可返回营地。");return;}
            bool abandoned = InDungeon && !DungeonCleared;
            if (!ChangeZone(false)) return;
            if(abandoned)Notify("已撤离遗迹，生命恢复。随时可以重新挑战。");
        }

        private bool ChangeZone(bool dungeon)
        {
            if (SceneLoading || Player == null) return false;
            // Camp recovery does not cross a world boundary. Hub travel commits
            // a different destination under loadingSaveSnapshot and must rebuild.
            if (!dungeon && !InDungeon && !loadingSaveSnapshot && world != null && world.activeInHierarchy)
                return RecoverInCurrentCamp();
            if (!CommitZoneState(dungeon)) return false;
            StartScenePreparation(PrepareZone(dungeon), dungeon ? "准备进入副本" : "准备返回营地");
            return true;
        }

        private bool RecoverInCurrentCamp()
        {
            // Keep failed persistence behind the existing death/input gate.
            // This world remains alive, so retain its loot receipt registry too.
            if (!SaveBeforeLeaving()) return false;
            SuspendInputs();
            FloatingNumber.ClearForWorldTransition();
            ResetExpedition(false);
            foreach (EnemyController enemy in Enemies)
                if (enemy != null) enemy.RetireAttackForCampRecovery();
            // Revive before transferring permanent partners onto the same ground.
            // Teleport retires old casts and movement/buff state, preserving skill
            // energy and cooldowns exactly as the old return-to-camp flow did.
            Player.RefreshStats(true);
            Player.Teleport(DungeonPoint(new Vector3(0, 0, -10)));
            var camera = Camera.main;
            if (camera != null)
            {
                var follow = camera.GetComponent<AdventureCamera>();
                if (follow != null) follow.Snap();
            }
            Physics.SyncTransforms();
            return true;
        }

        private bool CommitZoneState(bool dungeon)
        {
            if(!loadingSaveSnapshot&&InDungeon&&!Progression.FinishDungeonRewards()){Notify(Progression.LastError);return false;}
            // Preserve the old adventure before destroying anything. Staged load
            // and committed hub travel already supply a durable target snapshot.
            // Room retry sets this flag only after its own successful save preflight.
            if (!loadingSaveSnapshot && !enteringChapter && !retryingRoomChain && !SaveBeforeLeaving()) return false;
            if(!dungeon&&InDungeon&&!DungeonCleared&&!IsDead&&!ModeFinished)
            {
                if(RoomChainRun!=null)RoomChainRun.Fail(RoomFailureReason.Abandoned);
                if(ModeRun!=null)ModeRun.Fail(ExpeditionModeFailure.Abandoned);
                LastRunSummary=BuildRunSummary(false, "Abandoned");
            }
            changingZone = true;
            FloatingNumber.ClearForWorldTransition();
            int previousCombatEpoch = Player.CombatEpoch;
            if(wildernessPopulationRoutine!=null){StopCoroutine(wildernessPopulationRoutine);wildernessPopulationRoutine=null;}
            if (waveRoutine != null) { StopCoroutine(waveRoutine); waveRoutine = null; }
            if(wavePreparationRoutine!=null){StopCoroutine(wavePreparationRoutine);wavePreparationRoutine=null;}wavePreparing=false;ClearPreparedWaveActors();
            foreach (EnemyController enemy in Enemies) if (enemy != null) { enemy.gameObject.SetActive(false); Destroy(enemy.gameObject); }
            Enemies.Clear();
            foreach (GameObject obj in transientObjects) if (obj != null) { obj.SetActive(false); Destroy(obj); }
            transientObjects.Clear();
            if (world != null) { world.SetActive(false); Destroy(world); }
            Player.RetireCombatForWorldTransition();
            RetireWorldLootReceipts(previousCombatEpoch);
            InDungeon = dungeon;
            DungeonCleared = false;
            DungeonWave = 0;
            DungeonTier = enteringChapter ? chapterReceipt.Tier : Mathf.Clamp(SelectedDungeonTier, 1, MaximumDungeonTier);
            ResetExpedition(dungeon);
            checkpointRoom=-1;
            if(dungeon)Progression.BeginDungeonStage();
            if(ChapterActive){chapterPlan=ChapterRoomGeometry.Plan(ActiveChapterNode,ChapterRoomIndex,ChapterSeed);DungeonLayout=chapterPlan.Layout;}
            world = null;
            return true;
        }

        private System.Collections.IEnumerator PopulateWilderness(GameObject owner,int count)
        {
            // Initial population belongs to the guarded transition, before play.
            yield return null;
            for(int i=0;i<count*3&&Enemies.Count<count;i++)
            {
                while(pauseState.BackgroundPaused&&world==owner&&HasStarted&&!InDungeon)yield return null;
                if(world!=owner||!HasStarted||InDungeon||CurrentHub!=0)break;
                SpawnWildernessEnemy();
                scenePreparation.Report(.80f + .12f * Mathf.Clamp01((i + 1f) / count), "准备原野生物 · " + Enemies.Count + "/" + count);
                yield return null;
            }
            wildernessPopulationRoutine=null;
        }

        private void SpawnWildernessEnemy()
        {
            // Once acquisition is blocked, existing enemies may still drop their
            // own items, but no replacement producers enter this world. At most
            // the already living wilderness population can add retained drops.
            if (pendingLoot.Count > 0) return;
            for(int attempt=0;attempt<20;attempt++)
            {
                Vector3 desired=new Vector3(Random.Range(-28f,28f),0,Random.Range(-21f,28f));
                if(desired.z<-8&&Mathf.Abs(desired.x)<10)continue;
                Vector3 position;
                if(!TrySafeSpawn(desired,.6f,8f,out position)||IsPortalSanctuary(position,.6f))continue;
                EnemyKind kind=position.z>7&&Progression.Profile.level>=3?(Random.value<.4f?EnemyKind.Guardian:EnemyKind.Wisp):
                    position.x< -3?(Random.value<.6f?EnemyKind.Slime:EnemyKind.Goblin):(Random.value<.55f?EnemyKind.Goblin:EnemyKind.Wisp);
                SpawnEnemy(kind,Mathf.Max(1,Progression.Profile.level-1),position,false);return;
            }
        }

        public bool WavePreparing {get{return wavePreparing;}}
        private bool wavePreparing;
        private readonly List<EnemyController> preparedWaveActors=new List<EnemyController>();
        private void ClearPreparedWaveActors()
        {foreach(var enemy in preparedWaveActors)if(enemy!=null)Destroy(enemy.gameObject);preparedWaveActors.Clear();}
        private Coroutine wavePreparationRoutine;
        private void SpawnDungeonWave()
        {
            if(wavePreparationRoutine!=null)StopCoroutine(wavePreparationRoutine);
            ClearPreparedWaveActors();wavePreparing=true;UpdateTimeScale();
            wavePreparationRoutine=StartCoroutine(PrepareWholeDungeonWave());
        }
        private IEnumerator PrepareWholeDungeonWave()
        {
            ResetWaveTerrain();
            var owner=world;
            var plan=EncounterPlan.Create(DungeonTier,DungeonWave,DungeonLayout,runSeed);
            wavePopulation=plan.Count;
            var prepared=new List<EnemyController>(plan.Count);
            double slice=Time.realtimeSinceStartupAsDouble;
            // Build hidden actors over several frames, then expose the entire wave together.
            // Hidden actors reserve positions but do not participate in combat queries.
            foreach(var spawn in plan)
            {
                if(world!=owner||!InDungeon)yield break;
                Vector3 preferred=DungeonPoint(new Vector3(spawn.X,0,spawn.Z)),position;
                float radius=spawn.Boss?BossSpawnRadius:spawn.Kind==EnemyKind.Guardian?.65f:.5f;
                bool placed=TrySafeSpawn(preferred,radius,5.5f,out position);
                if(!placed)placed=TrySafeSpawn(preferred,radius,3.5f,out position,spacing:.15f);
                if(!placed)
                {
                    // Search the full arena rather than deferring enemies until after kills.
                    for(int ring=0;ring<24&&!placed;ring++)
                    {
                        float angle=ring*2.39996f;
                        placed=TrySafeSpawn(new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*(ArenaRadius*.65f),radius,3.5f,out position,spacing:.15f);
                        if(Time.realtimeSinceStartupAsDouble-slice>.004){yield return null;slice=Time.realtimeSinceStartupAsDouble;}
                    }
                }
                if(!placed)
                {
                    Debug.LogError("Whole wave has no safe spawn: "+DungeonWave+" / "+spawn.Kind);
                    Notify("本波出生点不可用，请返回营地重试");
                    FailWavePreparation(prepared);yield break;
                }
                SpawnEnemy(spawn.Kind,DungeonEntryLevel,position,spawn.Boss);
                var enemy=Enemies[Enemies.Count-1];enemy.gameObject.SetActive(false);Enemies.Remove(enemy);prepared.Add(enemy);preparedWaveActors.Add(enemy);
                if(Time.realtimeSinceStartupAsDouble-slice>.004){yield return null;slice=Time.realtimeSinceStartupAsDouble;}
            }
            // A short shared warning precedes the single visible arrival.
            Notify("第 "+DungeonWave+" 波 · "+prepared.Count+" 个敌人即将出现");
            yield return new WaitForSecondsRealtime(.35f);
            while(!SceneLoading&&InputBlocked&&world==owner&&InDungeon)yield return null;
            if(world!=owner||!InDungeon)yield break;
            // The player can move while actors prepare. Revalidate only nearby births.
            foreach(var enemy in prepared)
            {
                if(enemy==null||Player==null||Vector3.Distance(enemy.transform.position,Player.transform.position)>=3.5f)continue;
                Vector3 position;
                if(TrySafeSpawn(enemy.transform.position,enemy.NavigationRadius,3.5f,out position,spacing:.15f))enemy.transform.position=position;
            }
            preparedWaveActors.Clear();
            foreach(var enemy in prepared)if(enemy!=null){Enemies.Add(enemy);enemy.gameObject.SetActive(true);}
            objectiveHealedThisWave=false;wavePreparing=false;wavePreparationRoutine=null;UpdateTimeScale();
        }
        private void FailWavePreparation(List<EnemyController> prepared)
        {
            wavePreparing=false;wavePreparationRoutine=null;ClearPreparedWaveActors();Paused=true;UpdateTimeScale();
        }

        private bool TrySafeSpawn(Vector3 preferred,float radius,float playerDistance,out Vector3 position,Vector3? reservedPosition=null,float reservedClearance=0f,float spacing=1.1f)
        {
            for(int attempt=0;attempt<48;attempt++)
            {
                Vector3 candidate=attempt==0?preferred:preferred+new Vector3(Mathf.Sin(attempt*2.39996f),0,Mathf.Cos(attempt*2.39996f))*(.5f+attempt*.23f);
                candidate=WorldTraversal.NearestWalkable(candidate,radius);
                if(candidate.magnitude>ArenaRadius-radius-1||!WorldTraversal.IsWalkable(candidate,radius))continue;
                if(Player!=null && Vector3.Distance(candidate,Player.transform.position)<playerDistance)continue;
                // A multi-enemy encounter reserves earlier candidates before either enemy is spawned.
                if(reservedPosition.HasValue&&Vector3.Distance(candidate,reservedPosition.Value)<reservedClearance)continue;
                // This deliberately requires a clear traversable approach; it rejects
                // unreachable river banks and sealed pockets, not just visible ground.
                Vector3 approach=InDungeon?Vector3.zero:new Vector3(0,0,-10);
                if(!WorldTraversal.HasGroundPath(candidate,approach,radius)&&WorldTraversal.FindPath(candidate,approach,radius).Count==0)continue;
                bool crowded=false;
                foreach(EnemyController enemy in Enemies)if(enemy!=null&&!enemy.IsDead&&Vector3.Distance(candidate,enemy.transform.position)<radius+enemy.NavigationRadius+spacing){crowded=true;break;}
                if(!crowded)foreach(var enemy in preparedWaveActors)if(enemy!=null&&Vector3.Distance(candidate,enemy.transform.position)<radius+enemy.NavigationRadius+spacing){crowded=true;break;}
                if(crowded)continue;
                position=candidate;return true;
            }
            position=Vector3.zero;return false;
        }

        private Vector3 DungeonPoint(Vector3 point){return InDungeon?ArenaSizeRules.Point(DungeonLayout,point):point;}
        private float BossSpawnRadius {get{return BossScaleRules.SpawnRadius(RoomChainRun!=null&&RoomChainRun.Room.Boss||ChapterActive&&ChapterRun.Objective==RoomObjective.Boss);}}

        private void SpawnEnemy(EnemyKind kind, int level, Vector3 position, bool boss)
        {
            GameObject go = new GameObject(boss ? "Sentinel · Boss" : "Enemy · " + kind);
            go.transform.position = WorldTraversal.NearestWalkable(position, boss ? BossSpawnRadius : BossScaleRules.NavigationRadius(false,false,kind));
            EnemyController enemy = go.AddComponent<EnemyController>();
            using (PerformanceDiagnostics.Measure(PerformanceDiagnostics.Section.EnemySpawn)) enemy.Initialize(this, kind, level, boss);
            Enemies.Add(enemy);
            if (UsesWaveTactics && !boss) enemy.ConfigureDungeonRole(DungeonWaveTactics.Role(CurrentWaveTactic, kind));
        }

        public void OnEnemyKilled(EnemyController enemy)
        {
            if(PracticeActive){if(enemy!=null&&Enemies.Remove(enemy)){PracticeRecord.Defeat(enemy.Kind.ToString(),PracticeRecord.HasSupplier&&enemy.Kind==EnemyKind.Wisp,Enemies.Count==0);enemy.BeginDeath();}return;}
            if (enemy == null || !AdventureResultPolicy.AcceptsKill(HasStarted,CombatEnded,Enemies.Contains(enemy))) return;
            int chapterExperience=0;bool chapterKill=ChapterActive;
            if(chapterKill&&!RecordChapterDefeat(enemy,out chapterExperience))return;
            if(!Enemies.Remove(enemy))return;
            Vector3 position = enemy.transform.position;
            bool boss = enemy.IsBoss;
            OnExpeditionEnemyKilled(enemy);
            RecordArenaDefeat(enemy);
            RecordRoomDefeat(enemy);
            int level = Progression.Profile.level;
            int experience = boss ? 100 + level * 12 : (InDungeon ? 22 : 16) + level * 2;
            int gold = boss ? (int)System.Math.Min(ProgressionService.MaximumGold,85L+(long)DungeonTier*20) : Random.Range(7, 15) + level;
            if(InDungeon&&!boss) { float share=Mathf.Clamp(6f/Mathf.Max(6,wavePopulation),.5f,1f);experience=Mathf.RoundToInt(experience*share);gold=Mathf.Max(1,Mathf.RoundToInt(gold*share)); }
            if(InDungeon)gold=(int)System.Math.Min(ProgressionService.MaximumGold,(double)gold*(1d+.15d*TierRewardBand.Of(DungeonTier)));
            if(chapterKill)experience=chapterExperience;
            // Queue before callbacks or loot delivery can fail; LateUpdate persists
            // the admitted rewards once all combat producers in this frame finish.
            if (!InDungeon) pendingWildernessSave=Progression;
            int potions=InDungeon&&(boss||Random.Range(0,100)<AdventureRewardRules.PotionChance(DungeonTier))?1+TierRewardBand.Of(DungeonTier)/2:0;
            if(InDungeon)
            {
                Progression.RecordDungeonKill(gold,experience,potions);
                runEnemyExperience=(int)System.Math.Min(int.MaxValue,(long)runEnemyExperience+experience);
                runPickupGold=(int)System.Math.Min(int.MaxValue,(long)runPickupGold+gold);
                RunPickupPotions=(int)System.Math.Min(int.MaxValue,(long)RunPickupPotions+potions);
            }
            else Progression.GrantEnemyKillReward(gold,experience,deferSave:true);
            if(!InDungeon)LogSystem("+"+gold+" 金币 · +"+experience+" 经验");
            if (!(InDungeon&&!ChapterActive&&AdventureRewardRules.UsesTierDifficulty(ActiveAdventureMode))&&Random.Range(0,100)<AdventureRewardRules.EnemyEquipmentChance(boss,enemy.Tier!=EnemyController.ThreatTier.Normal))
            {
                ItemData loot = Progression.RollLoot(InDungeon&&!ChapterActive?DungeonEntryLevel:Progression.Profile.level, boss, InDungeon ? DungeonTier : 0, !InDungeon&&enemy.Tier==EnemyController.ThreatTier.Normal);
                DeliverEnemyLoot(loot, position);
            }
            enemy.BeginDeath();
            transientObjects.RemoveAll(go => go == null);
            transientObjects.Add(enemy.gameObject);
            // Coalesce kills from one combat action into one durable snapshot.
            if(ChapterActive)FinalizeChapterBoss();
            if(RoomChainRun!=null)FinalizeRoomChain();
            if(ModeRun!=null)FinalizeArenaResult();
            if (InDungeon && !ChapterActive && ModeRun==null && RoomChainRun==null && !changingZone && !DungeonCleared)
            {
                // The final boss completes the encounter; retiring guards grants no kill rewards.
                if(boss&&DungeonWave==TotalWaves)RetireRemainingEncounterEnemies();
                // Advancing is deferred until the finishing combat action has returned.
                if(Enemies.Count==0 && !wavePreparing) waveRoutine=StartCoroutine(NextWave());
            }
        }

        private ProgressionService pendingWildernessSave;
        private int checkpointRoom=-1;
        private void LateUpdate()
        {
            // Coalesce all kills admitted this frame after combat producers finish.
            // Normal pause, exit and scene-transition saves still flush the live profile.
            if(pendingWildernessSave!=null&&!CombatImpactBatch.InAction)
            {
                var owner=pendingWildernessSave;pendingWildernessSave=null;
                if(object.ReferenceEquals(owner,Progression))owner.Save();
            }
            if(!InDungeon||BackgroundPaused||Progression==null||!Progression.DungeonStageActive)return;
            int completed=ChapterActive&&ChapterRun.DoorUnlocked?ChapterRoomIndex:RoomChainRun!=null&&RoomChainRun.DoorUnlocked?RoomChainRun.Room.Index:-1;
            if(completed<0||completed==checkpointRoom)return;
            checkpointRoom=completed;
            if(!Progression.SaveDungeonCheckpoint())Notify(Progression.LastError);
        }
        private IEnumerator NextWave()
        {
            if (DungeonWave < TotalWaves)
            {
                if (!ChallengeRun) Player.Heal(Player.MaxHealth * .25f);
                else HealingCharges = Mathf.Min(3, HealingCharges + 1);
                // Let the finishing combat action complete before spawning the next wave.
                var owner = Player;
                int epoch = owner.CombatEpoch;
                yield return null;
                while (Player == owner && owner.CombatEpoch == epoch && InDungeon && !IsDead && InputBlocked)
                    yield return null;
                if (Player != owner || owner.CombatEpoch != epoch || !InDungeon || IsDead || changingZone)
                    yield break;
                if(!Progression.SaveDungeonCheckpoint())Notify(Progression.LastError);
                DungeonWave++;
                SpawnDungeonWave();
                Notify(DungeonWave == TotalWaves ? "最终波 · 星蚀巨像" : "第 " + DungeonWave + " 波");
                UpdateTimeScale();
                waveRoutine = null;
                yield break;
            }
            else
            {
                DungeonCleared = true;
                BeginVictoryTransition();GameAudio.Play(SoundCue.Victory);
                QueueDungeonCompletion();
                bool settled=TrySettleDungeonReward();
                LastRunSummary = BuildRunSummary(true);
                Player.Heal(Player.MaxHealth);
                if(!settled)Notify("奖励尚未保存。请重试结算，或打开菜单处理存档。");
            }
            waveRoutine = null;
        }

        public const int DungeonRevivalLimit=3;
        public int DungeonRevivalsUsed {get;private set;}
        public int DungeonRevivalsRemaining => Mathf.Max(0,DungeonRevivalLimit-DungeonRevivalsUsed);
        public bool RevivalPending {get;private set;}
        public bool CanSuspendForRevival => InDungeon&&!DungeonCleared&&!ModeFinished&&!PracticeActive&&DungeonRevivalsRemaining>0;
        private void FinalizeDeathRun()
        {
            RevivalPending=false;
            if(ModeRun!=null)ModeRun.Fail(ExpeditionModeFailure.PlayerDefeated);
            if(RoomChainRun!=null)RoomChainRun.Fail(RoomFailureReason.Death);
            if(ChapterActive)FailChapter("角色倒下：本次章节挑战失败。");
        }
        public void EndDeathBattle(){if(!IsDead||!RevivalPending)return;FinalizeDeathRun();Player.EndGhost();LastRunSummary=BuildRunSummary(false);UpdateTimeScale();}
        public bool ReviveInDungeon()
        {
            if(!IsDead||!RevivalPending||Player==null||!InDungeon)return false;
            if(DungeonRevivalsRemaining<=0){Notify("本次副本复活次数已用完");return false;}
            if(!Progression.ConsumeRevivalStone()){Notify(Progression.LastError);return false;}
            DungeonRevivalsUsed++;
            Player.ReviveAtCurrentPosition();IsDead=false;RevivalPending=false;Paused=false;uiBlocking=false;
            LastRunSummary=null;UpdateTimeScale();return true;
        }
        public void OnPlayerDied()
        {
            if(PracticeActive){PracticeRecord.PlayerDefeated();return;}
            if (IsDead) return;
            IsDead = true;
            RevivalPending=CanSuspendForRevival;
            if(!RevivalPending)FinalizeDeathRun();else Player.BeginGhost();
            DungeonSelectionOpen = false;
            LastRunSummary = BuildRunSummary(false);
            GameAudio.Play(SoundCue.Death);
            // Ground drops belong to this run but are not in the profile yet.
            // Retire them only after checked acquisition/recovery storage succeeds.
            // A failed preservation must retain both the pickup and its save error.
            if (PreserveWorldLoot()) Progression.Save();
            Notify(string.IsNullOrEmpty(Progression.LastError)?string.Empty:Progression.LastError);
            UpdateTimeScale();
        }

        public void Respawn()
        {
            if (!IsDead) return;
            if (!PreserveWorldLoot()) return;
            if(RevivalPending)FinalizeDeathRun();
            if (!ChangeZone(false)) return;
            IsDead = false;
            Paused = false;
            uiBlocking = false;
            UpdateTimeScale();
            Notify("已在营地复苏。生命恢复，可向商人购买药水。");
        }

        public void DrinkPotion()
        {
            if(PracticeActive&&(InputBlocked||PracticeRecord==null||!PracticeRecord.Started||PracticeRecord.Finished))return;
            if (!HasStarted || IsDead || Player == null) return;
            if (Player.PotionCooldownRemaining > 0) { Notify("药剂冷却中，还需 " + Mathf.CeilToInt(Player.PotionCooldownRemaining) + " 秒。"); return; }
            if (Player.Health >= Player.MaxHealth - .5f) return;
            if (ChallengeRun && InDungeon) { if (!TrySpendHealingCharge()) return; }
            else if (!Progression.UsePotion()) { Notify(Progression.LastError); return; }
            Player.StartPotionCooldown();
            Player.Heal(Player.MaxHealth * .5f);
            SpawnFloatingText(Player.transform.position + Vector3.up * 2, "+50% HP", new Color(.4f, 1, .68f));
            // Normal potion consumption already committed once; challenge charges are run-only.
        }

        public void UseHotbarConsumable()
        {
            if (InputBlocked || Player == null || Player.TraversalStartedThisFrame) return;
            DrinkPotion();
        }

        public bool QuitToTitle(bool alreadySaved = false)
        {
            if(!Progression.FinishDungeonRewards()){Notify(Progression.LastError);return false;}
            if (!alreadySaved && !SaveBeforeLeaving()) return false;
            SuspendInputs();
            CancelScenePreparation();
            StopAllCoroutines();
            waveRoutine = null; wildernessPopulationRoutine = null; wavePreparing=false;wavePreparationRoutine=null;ClearPreparedWaveActors();
            HasStarted = false;
            Paused = false;
            uiBlocking = false;
            IsDead = false;
            foreach (EnemyController enemy in Enemies) if (enemy != null) { enemy.gameObject.SetActive(false); Destroy(enemy.gameObject); }
            Enemies.Clear();
            foreach (GameObject obj in transientObjects) if (obj != null) Destroy(obj);
            transientObjects.Clear();
            if (Player != null) { Player.gameObject.SetActive(false); Destroy(Player.gameObject); Player = null; }
            if (world != null) { world.SetActive(false); Destroy(world); }
            InDungeon = false;
            ResetExpedition(false);
            DungeonCleared = false;
            // BeginAdventure builds the camp only after a successful resume/create.
            world = null;
            Camera.main.GetComponent<AdventureCamera>().Snap();
            UpdateTimeScale();
            return true;
        }

        private void OnProgressChanged()
        {
            if(Progression.EnemyRewardWithoutBuildChange)return;
            if (Player != null && HasStarted)
            {
                Player.RefreshStats(false);
                string now=Progression.Profile.weaponId+"|"+Progression.Profile.armorId+"|"+Progression.Profile.relicId;
                if(equipmentFingerprint!=null && now!=equipmentFingerprint) { equipmentFingerprint=now;RecordCombatAction("换装"); }
                equipmentFingerprint=now;
            }
        }
        private void OnLevelUp(int level)
        {
            if(InDungeon)return; // Final settlement owns the level result; no combat heal/VFX burst.
            GameAudio.Play(SoundCue.LevelUp);
            var learned=new System.Collections.Generic.List<string>();
            for(int skill=0;skill<GameBalance.SkillCount;skill++)
                if(GameBalance.SkillRequiredLevels[skill]==level)learned.Add(GameBalance.SkillName(Progression.Profile.heroClass,skill));
            if (Player != null)
            {
                Player.RefreshStats(true);
                Color glow=new Color(.95f,.83f,.4f);
                Vector3 position=Player.transform.position;
                CombatFx.Ring(position,2.1f,glow,.85f,.12f);
                CombatFx.Ring(position,1.25f,new Color(.65f,1f,.84f),.65f,.08f);
                CombatFx.Ring(position,3.4f,new Color(.65f,1f,.84f),1.5f,.16f);
            }
            // One fixed notice owns level feedback; world text would repeat it
            // and reproject while the player or camera moves.
            Notify("等级提升至 Lv."+level+(Player!=null?" · 生命已恢复":"")+(learned.Count>0?"\n习得技能："+string.Join("、",learned):""));
        }
        public void Notify(string message) { notification = message; notificationUntil = Time.unscaledTime + 6; }

        public void SpawnFloatingText(Vector3 position, string value, Color color)
        {
            FloatingNumber.Spawn(position,value,color);
        }

        private void SpawnLootBeacon(Vector3 position, Color color)
        {
            GameObject beacon = WorldBuilder.MakeLootBeacon(position, color);
            transientObjects.RemoveAll(go => go == null);
            transientObjects.Add(beacon);
            Destroy(beacon, 2.5f);
        }

        private void DeliverEnemyLoot(ItemData loot, Vector3 position)
        {
            if(loot==null)return;
            if(InDungeon&&Progression.RecordDungeonLoot(loot)){if(!runItemIds.Contains(loot.id))runItemIds.Add(loot.id);return;}
            SpawnGroundLoot(loot,position);
        }

        public GroundLootPickup SpawnGroundLoot(ItemData item, Vector3 position)
        {
            if (!HasStarted || changingZone || world == null || !world.activeInHierarchy || item == null || string.IsNullOrWhiteSpace(item.id) ||
                !System.Enum.IsDefined(typeof(ItemSlot), item.slot) || !System.Enum.IsDefined(typeof(Rarity), item.rarity) ||
                collectedGroundLoot.Contains(item.id) || Progression.Profile.inventory.Exists(value => value != null && value.id == item.id)) return null;
            PendingLoot existing;
            if (pendingLoot.TryGetValue(item.id, out existing)) return existing.Pickup;
            position.y = 0;
            position = WorldTraversal.NearestWalkable(position, .35f);
            GameObject root = new GameObject("Ground loot · " + item.name);
            if (world != null) root.transform.SetParent(world.transform, false);
            root.transform.position = position;
            GroundLootPickup pickup = root.AddComponent<GroundLootPickup>();
            pendingLoot.Add(item.id, new PendingLoot { Item = item, Pickup = pickup });
            pickup.Initialize(this, item);
            return pickup;
        }

        internal bool IsCurrentGroundLoot(GroundLootPickup pickup)
        {
            PendingLoot pending;
            return HasStarted && !changingZone && world != null && world.activeInHierarchy && pickup != null &&
                pickup.gameObject.activeInHierarchy && !string.IsNullOrEmpty(pickup.ItemId) &&
                pendingLoot.TryGetValue(pickup.ItemId, out pending) && object.ReferenceEquals(pending.Pickup, pickup) &&
                pickup.transform.IsChildOf(world.transform);
        }

        internal bool TryCollectGroundLoot(GroundLootPickup pickup)
        { return IsCurrentGroundLoot(pickup) && TryCollectGroundLoot(pickup.ItemId); }

        public bool TryCollectGroundLoot(string itemId, bool feedback = true)
        {
            if(PracticeActive)return false;
            PendingLoot pending;
            if (string.IsNullOrEmpty(itemId) || !pendingLoot.TryGetValue(itemId, out pending) || pending.Collecting) return false;
            pending.Collecting = true;
            ProgressionService source = Progression;
            bool accepted = false;
            try { accepted = source.CollectLoot(pending.Item); }
            finally
            {
                pending.Collecting = false;
                // An observer can throw after the write committed. Reconcile the
                // exact receipt before propagating; a pre-commit fault stays retryable.
                if (accepted || source.HasCommittedWorldLoot(itemId))
                {
                    PendingLoot current;
                    if (pendingLoot.TryGetValue(itemId, out current) && object.ReferenceEquals(current, pending))
                    {
                        pendingLoot.Remove(itemId);
                        collectedGroundLoot.Add(itemId);
                        if(InDungeon&&!runItemIds.Contains(itemId))runItemIds.Add(itemId);
                    }
                    if (pending.Pickup != null) pending.Pickup.Retire();
                }
            }
            if (!accepted) return false;
            if (feedback)
            {
                GameAudio.Play(SoundCue.Loot);
                LogSystem("拾取 " + pending.Item.name + (string.IsNullOrEmpty(Progression.LastError) ? "" : " · " + Progression.LastError));
            }
            return true;
        }

        public void CollectRemainingDungeonLoot()
        {
            if (Progression == null || pendingLoot.Count == 0) return;
            var ids = new List<string>(pendingLoot.Keys);
            foreach (string id in ids) TryCollectGroundLoot(id, false);
        }

        private bool applicationQuitSavePrepared, applicationQuitSaveAccepted;

        public bool SaveBeforeLeaving()
        {
            if(PracticeActive)
            {try{EndPractice("保存或离开 · 试招结束，恢复原角色");}
             catch(System.Exception exception){Debug.LogException(exception);Notify("试招收尾出现异常，请重试保存退出。");return false;}}
            if (!HasStarted) return true;
            if(!TrySettleSideEventRewards())return false;
            if(DungeonRewardPending&&!TrySettleDungeonReward())return false;
            if(ModeRewardPending&&!TrySettleArenaReward())return false;
            if (!PreserveWorldLoot()) return false;
            // Settlement callbacks can change the live profile. The service checks
            // the complete document and skips only an already durable snapshot.
            if(!Progression.SaveDungeonCheckpoint()){Notify(Progression.LastError);return false;}
            if (!string.IsNullOrEmpty(Progression.LastError)) { Notify(Progression.LastError); return false; }
            return true;
        }

        public bool ExitApplication(bool alreadySaved = false)
        {
#if UNITY_IOS
            return QuitToTitle(alreadySaved);
#else
            if (!alreadySaved && !SaveBeforeLeaving()) return false;
            // One-use token: the in-app confirmation has already made the save
            // durable. The OS callback must not immediately rotate its backup again.
            applicationQuitSavePrepared = true;
            applicationQuitSaveAccepted = false;
#if UNITY_EDITOR
            applicationQuitSavePrepared = false;
            applicationQuitSaveAccepted = true;
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            return true;
#endif
        }

        private bool CanQuitSafely()
        {
            if (applicationQuitSavePrepared)
            {
                applicationQuitSavePrepared = false;
                applicationQuitSaveAccepted = true;
                return true;
            }
            // Alt-F4/window close has no in-app preparation and must still save.
            // A later separate OS close always reaches this check again.
            applicationQuitSaveAccepted = SaveBeforeLeaving();
            return applicationQuitSaveAccepted;
        }

        private void SaveOnApplicationQuit()
        {
            // Some editor/platform shutdown paths omit wantsToQuit. Preserve once
            // there, but never write again after this quit was already accepted.
            if (!applicationQuitSaveAccepted && !applicationQuitSavePrepared) SaveBeforeLeaving();
        }

        private bool PreserveWorldLoot()
        {
            if(Progression!=null&&!Progression.CollectGroundSupplies(Progression.Profile.groundGold,Progression.Profile.groundPotions))return false;
            if(world!=null)foreach(var supply in world.GetComponentsInChildren<GroundSupplyPickup>()){supply.gameObject.SetActive(false);Destroy(supply.gameObject);}
            if (Progression == null || pendingLoot.Count == 0) return true;
            CollectRemainingDungeonLoot();
            if (pendingLoot.Count == 0) return true;
            var items = new List<ItemData>();
            foreach (PendingLoot loot in pendingLoot.Values) items.Add(loot.Item);
            if (!Progression.PreserveGroundLoot(items)) { Notify(Progression.LastError); return false; }
            foreach (PendingLoot loot in pendingLoot.Values)
            {
                collectedGroundLoot.Add(loot.Item.id);
                if (loot.Pickup != null) loot.Pickup.Retire();
            }
            pendingLoot.Clear();
            LogSystem("珍贵地面装备已保存在营地恢复栏");
            return true;
        }

        private void RetireWorldLootReceipts(int previousCombatEpoch)
        {
            bool retired = (world == null || !world.activeInHierarchy) && Enemies.Count == 0 && transientObjects.Count == 0;
            if (Progression != null && Progression.TryRetireWorldLootReceipts(changingZone, pendingLoot.Count,
                retired, Player != null && Player.CombatEpoch != previousCombatEpoch)) collectedGroundLoot.Clear();
        }

        private void OnApplicationPause(bool pause)
        {
            pauseState.SetSuspended(pause);
            GameAudio.SetBackgroundPaused(pauseState.BackgroundPaused);
            if (pause) SuspendInputs();
            lifecycleSave.Observe(pauseState.BackgroundPaused,HasStarted,SaveBeforeLeaving);
            UpdateTimeScale();
        }
        private void OnApplicationFocus(bool focus)
        {
#if EMBERFALL_VISUAL_VALIDATION
            // The isolated performance runner must keep simulating while the
            // developer inspects logs in another window.
            focus = true;
#endif
            pauseState.SetFocus(focus);
            GameAudio.SetBackgroundPaused(pauseState.BackgroundPaused);
            if (!focus) SuspendInputs();
            lifecycleSave.Observe(pauseState.BackgroundPaused,HasStarted,SaveBeforeLeaving);
            UpdateTimeScale();
        }
        private void SuspendInputs()
        {
            MobileControls.ResetInput();
            if (Player != null)
            {
                var targeting = Player.GetComponent<SkillTargetingController>();
                if (targeting != null) targeting.Cancel();
                var charge = Player.GetComponent<SkillChargeController>();
                if (charge != null) charge.Cancel();
            }
            if (ui != null)
            {
                if (BackgroundPaused) ui.CancelBackgroundInput();
                else ui.CancelForegroundInput();
            }
        }
        private void OnApplicationQuit() { SaveOnApplicationQuit(); }
        private void OnDestroy()
        {
            if(PracticeActive)EndPractice("会话关闭");
            if (Instance != this) return;
            EndHubNpcConversation();
            Application.wantsToQuit -= CanQuitSafely;
            PreserveWorldLoot();
            if (Progression != null) { Progression.Changed -= OnProgressChanged; Progression.LeveledUp -= OnLevelUp; }
            Instance = null;
            Time.timeScale = 1;
        }
    }

    [DefaultExecutionOrder(-100)]
    public sealed partial class AdventureCamera : MonoBehaviour
    {
        public const float MinimumPitch = -18f;
        public const float MaximumPitch = 75f;
        private const float DefaultPitch = 25f;
        // At 48 degrees vertical FOV and the existing 25-degree pitch, this
        // frames the hero at roughly 10-12% of the viewport height on all devices.
        private const float DefaultDistance = 18f;
        private const float DistanceScale = 1.2041595f;
        private static AdventureCamera active;
        private readonly CameraOrbitInput orbitInput = new CameraOrbitInput();
        private int inputFrame = -1;
        private float distance = DefaultDistance;
        private float yaw, pitch = DefaultPitch;
        private float smoothYaw, smoothPitch = DefaultPitch, smoothDistance = DefaultDistance;
        private Vector3 lookTarget;
        private float manualCameraUntil, assistTargetSince, assistYawVelocity;
        private EnemyController assistTarget;
        public static bool CancelSkillRequested { get { return !MobileControls.Active && active != null && active.inputFrame == Time.frameCount && active.orbitInput.Clicked; } }
        public static bool IsOrbitDragging { get { return !MobileControls.Active && active != null && active.orbitInput.IsDragging; } }
        public float Pitch { get { return pitch; } }
        public float Yaw { get { return yaw; } }

        private void Awake() { active = this; }
        public void Snap()
        {
            HitFeedback.ClearCamera();
            ResetOrbitInput();
            assistTarget=null;assistYawVelocity=0;
            yaw=smoothYaw=0;pitch=smoothPitch=DefaultPitch;distance=smoothDistance=DefaultDistance;
            manualCameraUntil=Time.unscaledTime+1f;
            lookTarget = DesiredTarget();
            MoveCamera(true);
        }

        public static Vector3 CameraRelativeMovement(Vector2 input, Transform view)
        {
            Vector3 forward = view == null ? Vector3.forward : Vector3.ProjectOnPlane(view.forward, Vector3.up);
            if (forward.sqrMagnitude < .0001f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            return Vector3.ClampMagnitude(right * input.x + forward * input.y, 1f);
        }

        public static Vector3 MobileMovement(Vector2 input)
        {
            return CameraRelativeMovement(input,Camera.main==null?null:Camera.main.transform);
        }

        private Vector3 DesiredTarget()
        {
            GameSession game = GameSession.Instance;
            return game != null && game.Player != null && game.HasStarted ? game.Player.transform.position + Vector3.up * .7f : new Vector3(0, 0, 1);
        }
        private void Update()
        {
            GameSession game = GameSession.Instance;
            bool canContinue = !MobileControls.Active && Application.isFocused && game != null && (!game.InputBlocked||game.RevivalPending&&!game.Paused&&!game.BackgroundPaused);
            bool canStart = canContinue && !game.PointerOverUI;
            ProcessOrbitInput(Input.mousePosition, Input.GetMouseButtonDown(1), Input.GetMouseButton(1), Input.GetMouseButtonUp(1), canStart, canContinue, Input.mouseScrollDelta.y);
        }

        public void ApplyMobileOrbit(float horizontal, float vertical)
        {
            if(!MobileControls.Active||GameSession.Instance==null||GameSession.Instance.InputBlocked)return;
            manualCameraUntil=Time.unscaledTime+1.2f;
            StopCombatAssist(false);
            yaw=Mathf.Repeat(yaw+horizontal,360f);
            ApplyMobilePitch(vertical);
        }

        public void ApplyMobilePitch(float delta) { if(MobileControls.Active&&GameSession.Instance!=null&&!GameSession.Instance.InputBlocked){pitch=MobileCameraGesture.Apply(pitch,delta);} }

        private void ProcessOrbitInput(Vector2 position, bool pressed, bool held, bool released, bool canStart, bool canContinue, float scroll)
        {
            inputFrame = Time.frameCount;
            orbitInput.Advance(position, pressed, held, released, canStart, canContinue);
            if (orbitInput.DragDelta.sqrMagnitude > 0)
            {
                manualCameraUntil=Time.unscaledTime+1.2f;
                yaw = Mathf.Repeat(yaw + orbitInput.DragDelta.x * .22f, 360f);
                pitch = Mathf.Clamp(pitch + orbitInput.DragDelta.y * .18f, MinimumPitch, MaximumPitch);
            }
            if (canStart && canContinue)
            {
                distance = Mathf.Clamp(distance - scroll * .8f, CameraVisibilityRules.MinimumDistance, 25f);
                if(Mathf.Abs(scroll)>.001f)manualCameraUntil=Time.unscaledTime+1.2f;
            }
        }

        private void StopCombatAssist(bool holdView)
        {
            if(holdView)yaw=smoothYaw;
            assistTarget=null;assistYawVelocity=0;
        }
        private static EnemyController ResolveLoneBossCameraTarget(GameSession game)
        {
            if(game==null||!game.InDungeon||game.Player==null)return null;
            EnemyController boss=null;
            foreach(var enemy in game.Enemies)
            {
                if(enemy==null||enemy.IsDead||!enemy.isActiveAndEnabled||!enemy.gameObject.activeInHierarchy)continue;
                if(!enemy.IsBoss||boss!=null)return null;
                boss=enemy;
            }
            if(boss==null)return null;
            Vector3 direction=CombatFx.Flat(boss.transform.position-game.Player.transform.position);
            // Camera guidance is independent of attack line of sight: scenery must
            // not prevent finding the last boss while the player moves around it.
            if(direction.sqrMagnitude<.16f||direction.sqrMagnitude>22f*22f)return null;
            return boss;
        }

        private void AssistCombatView()
        {
            var game=GameSession.Instance;
            bool focused=Application.isFocused;
#if EMBERFALL_VISUAL_VALIDATION
            focused |= VisualValidationPlayer.PerformanceRun;
#endif
            if(!focused||game==null||game.InputBlocked&&!game.RevivalPending||game.Player==null)
            {StopCombatAssist(true);return;}
            // A held orbit gesture owns the camera even when the mouse stops moving.
            if(orbitInput.IsHeld||MobileControls.Active&&MobileControls.CameraLookActive){manualCameraUntil=Time.unscaledTime+1.2f;StopCombatAssist(false);return;}
            if(Time.unscaledTime<manualCameraUntil){StopCombatAssist(false);return;}
            if(!game.HasStarted||game.IsDead||game.CombatEnded||game.RevivalPending){StopCombatAssist(true);return;}
            var targeting=game.Player.GetComponent<SkillTargetingController>();
            if(targeting!=null&&targeting.IsTargeting){StopCombatAssist(true);return;}
            var target=ResolveLoneBossCameraTarget(game);
            if(target==null){StopCombatAssist(true);return;}
            Vector3 direction=CombatFx.Flat(target.transform.position-game.Player.transform.position);
            if(assistTarget!=target){assistTarget=target;assistYawVelocity=0;assistTargetSince=Time.unscaledTime;return;}
            if(Time.unscaledTime-assistTargetSince<.1f)return;
            float desiredYaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
            float error=Mathf.DeltaAngle(yaw,desiredYaw);
            // Damped angular velocity eases into and out of a restrained turn.
            if(Mathf.Abs(error)<=2f){assistYawVelocity=0;return;}
            yaw=Mathf.Repeat(Mathf.SmoothDampAngle(yaw,desiredYaw-Mathf.Sign(error)*2f,ref assistYawVelocity,.35f,110f,Mathf.Min(Time.unscaledDeltaTime,.05f)),360f);
        }

        private void LateUpdate() { using (PerformanceDiagnostics.Measure(PerformanceDiagnostics.Section.Camera)) MoveCamera(false); }
        private void MoveCamera(bool snap)
        {
            if(!snap)AssistCombatView();
            Vector3 desired = DesiredTarget();
            float follow = 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime);
            float orbit = 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime);
            lookTarget = snap ? desired : Vector3.Lerp(lookTarget, desired, follow);
            smoothYaw = snap || MobileControls.Active ? yaw : Mathf.LerpAngle(smoothYaw, yaw, orbit);
            smoothPitch = snap ? pitch : Mathf.Lerp(smoothPitch, pitch, orbit);
            GameSession game=GameSession.Instance;
            float encounterDistance=ArenaSizeRules.CameraDistance(distance,game!=null&&game.InDungeon,game==null?-1:game.DungeonLayout);
            float visibleDistance=CameraVisibilityRules.Zoom(encounterDistance,CameraOcclusionSurface.Nearest(desired));
            smoothDistance = snap ? visibleDistance : Mathf.Lerp(smoothDistance, visibleDistance, orbit);
            Vector3 position = lookTarget + Quaternion.Euler(smoothPitch, smoothYaw, 0) * Vector3.back * (smoothDistance * DistanceScale);
            // Near the horizon the camera approaches the ground; look slightly
            // above the hero so dragging farther can produce a real upward view.
            position.y = Mathf.Max(.45f, position.y);
            transform.position = position;
            transform.LookAt(lookTarget + Vector3.up * (Mathf.Clamp01(-smoothPitch / 18f) * 2.4f));
            transform.position += HitFeedback.CameraOffset;
            UpdateVisibility();
        }

        private void ResetOrbitInput() { orbitInput.Reset(); inputFrame = -1; }
        private void OnApplicationFocus(bool focused) { if (!focused) ResetOrbitInput(); }
        private void OnApplicationPause(bool suspended) { if(suspended)ResetOrbitInput(); }
        private void OnDisable() { ResetOrbitInput();RestoreVisibility(); }
        private void OnDestroy() { RestoreVisibility();if (active == this) active = null; }
    }

}

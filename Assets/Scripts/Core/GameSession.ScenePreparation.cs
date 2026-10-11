using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameSession
    {
        private readonly ScenePreparationState scenePreparation = new ScenePreparationState();
        private Coroutine scenePreparationRoutine;
        private IEnumerator scenePreparationDriver;
        public bool SceneLoading => scenePreparation.Active;
        public float SceneLoadingProgress => scenePreparation.Progress;
        public string SceneLoadingStage => scenePreparation.Stage;
        public string SceneLoadingError => scenePreparation.Error;

        private void StartScenePreparation(IEnumerator work, string label, bool continuing = false)
        {
            if (!continuing) scenePreparation.Begin(label);
            SuspendInputs();
            UpdateTimeScale();
            scenePreparationDriver = DriveScenePreparation(work);
            scenePreparationRoutine = StartCoroutine(scenePreparationDriver);
        }

        private void CancelScenePreparation()
        {
            if (scenePreparationRoutine != null) StopCoroutine(scenePreparationRoutine);
            (scenePreparationDriver as IDisposable)?.Dispose();
            scenePreparationDriver = null; scenePreparationRoutine = null;
            scenePreparation.Cancel();
        }

        // Drive nested enumerators here so any failed asset/build/population stage
        // keeps the action gate closed and shows a recoverable error.
        private IEnumerator DriveScenePreparation(IEnumerator work)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(work);
            bool success = false;
            try
            {
                yield return null; // Let the loading surface paint before heavy work.
                while (stack.Count > 0)
                {
                    if (BackgroundPaused) { yield return null; continue; }
                    bool next = false; object current = null; Exception failure = null;
                    try { next = stack.Peek().MoveNext(); if (next) current = stack.Peek().Current; }
                    catch (Exception error) { failure = error; }
                    if (failure != null)
                    {
                        scenePreparation.Fail("资源或场景准备失败：" + failure.Message);
                        SaveLoadError = scenePreparation.Error;
                        Debug.LogException(failure);
                        yield break;
                    }
                    if (!next) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                    if (current is IEnumerator nested) { stack.Push(nested); continue; }
                    yield return current;
                }
                success = true;
            }
            finally
            {
                while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose();
                scenePreparationRoutine = null;
                scenePreparationDriver = null;
                changingZone = false;
                if (success) scenePreparation.Complete();
                UpdateTimeScale();
            }
        }

        private IEnumerator PrepareAdventure()
        {
            scenePreparation.Report(.03f, "准备人物与怪物模型");
            yield return CombatModel.PrewarmModels(p => scenePreparation.Report(.03f + p * .17f, "准备人物与怪物模型"));
            scenePreparation.Report(.20f, "准备技能实体与材质");
            yield return FilledSkillVfx.Prewarm();
            yield return ElementalCombatVfx.Prewarm();
            yield return PhysicalSkillVfx.PrewarmAssets();
            yield return CombatShaderWarmup.Prewarm(p => scenePreparation.Report(.23f + p * .06f, "准备战斗画面"));
            scenePreparation.Report(.30f, "创建角色与装备");
            HasStarted = true; IsDead = false; Paused = false; uiBlocking = false;
            if (Player != null) { Player.gameObject.SetActive(false); Destroy(Player.gameObject); }
            Player = new GameObject("Hero").AddComponent<PlayerController>();
            Player.Initialize(this, Progression.Profile.heroClass);
            equipmentFingerprint = Progression.Profile.weaponId + "|" + Progression.Profile.armorId + "|" + Progression.Profile.relicId;
            // The create/load entry already made its durable save preflight.
            bool previous = loadingSaveSnapshot; loadingSaveSnapshot = true;
            try { if (!CommitZoneState(false)) throw new InvalidOperationException(Progression.LastError ?? "营地状态准备失败"); }
            finally { loadingSaveSnapshot = previous; }
            yield return PrepareZone(false, .32f);
        }

        private IEnumerator PrepareZone(bool dungeon, float start = .08f)
        {
            scenePreparation.Report(start, "准备技能与场景资源");
            yield return FilledSkillVfx.Prewarm();
            yield return ElementalCombatVfx.Prewarm();
            yield return PhysicalSkillVfx.PrewarmAssets();
            yield return CombatShaderWarmup.Prewarm();
            yield return WorldBuilder.BuildAsync(dungeon ? ZoneKind.Dungeon : ZoneKind.Wilderness,
                DungeonLayout, Progression.HighestAdventureTier, CurrentHub, ChapterSeed,
                root => world = root, (p, stage) => scenePreparation.Report(start + p * (.72f - start), stage));
            if (!dungeon) WorldBuilder.ApplyChapterLandmark(world, Progression.Profile.chapterCompletedMask);
            scenePreparation.Report(.74f, "安置角色与镜头");
            Player.Teleport(ChapterActive ? chapterPlan.Entrance : dungeon && RoomChainRun != null ?
                DungeonPoint(TacticalRoomGeometry.Entrance) : DungeonPoint(new Vector3(0, 0, dungeon ? -9 : -10)));
            Player.RefreshStats(true);
            if (dungeon) Player.ResetCooldownsForDungeonEntry();
            Camera.main.GetComponent<AdventureCamera>().Snap();
            yield return null;
            scenePreparation.Report(.78f, "准备敌人与场景交互");
            if (dungeon)
            {
                DungeonWave = 1;
                if (ChapterActive) BeginChapterRoom();
                else if (RoomChainRun != null) BeginRoomChainScene();
                else if (ModeRun != null) BeginArenaScene();
                else { SpawnDungeonWave(); BuildSideEvent(); }
            }
            else
            {
                if (CurrentHub == 0) yield return PopulateWilderness(world, Mathf.Min(32, 24 + Progression.Profile.level / 8));
                respawnTimer = 2.5f;
            }
            yield return FinishScenePreparation();
        }

        private IEnumerator PrepareRoom(bool chapter)
        {
            yield return WorldBuilder.BuildAsync(ZoneKind.Dungeon, DungeonLayout, Progression.HighestAdventureTier,
                CurrentHub, ChapterSeed, root => world = root,
                (p, stage) => scenePreparation.Report(.08f + p * .62f, stage));
            scenePreparation.Report(.74f, "准备房间敌人与目标");
            Player.Teleport(chapter ? chapterPlan.Entrance : DungeonPoint(TacticalRoomGeometry.Entrance));
            Camera.main.GetComponent<AdventureCamera>().Snap();
            if (chapter) { chapterObjective = null; chapterObjectives[0] = chapterObjectives[1] = null; BeginChapterRoom(); }
            else BeginRoomChainScene();
            yield return FinishScenePreparation();
            Notify(chapter ? ChapterObjectiveStatus : RoomTactics.Name(RoomChainRun.Room.Objective) + " · 房间 " + (RoomChainRun.Room.Index + 1) + " / 5");
        }

        private IEnumerator FinishScenePreparation()
        {
            if (RoomChainRun != null && RoomChainRun.Failed || ChapterRun != null && ChapterRun.Failed)
                throw new InvalidOperationException("房间生成检查失败，请返回首页重新进入。");
            scenePreparation.Report(.94f, "准备远近景与设备画质");
            yield return PrepareGraphicsForWorldAsync(world, p => scenePreparation.Report(.94f + p * .035f, "准备远近景与设备画质"));
            Physics.SyncTransforms();
            yield return null;
            scenePreparation.Report(.98f, "完成画面与交互检查");
            // Render the prepared scene behind the overlay before accepting input.
            yield return null;
            SuspendInputs();
        }

        public void ReturnFromSceneLoadingFailure()
        {
            if (!SceneLoading || string.IsNullOrEmpty(SceneLoadingError)) return;
            DiscardTransientAdventureForLoad();
            scenePreparation.Cancel();
            Paused = false; uiBlocking = false; UpdateTimeScale();
        }
    }
}

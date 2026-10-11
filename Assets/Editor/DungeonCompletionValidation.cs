using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Emberfall.Editor
{
    // Hook for the isolated RuntimeValidation runner. It never opens a player save.
    public static class DungeonCompletionValidation
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private static object Call(object target, string name, params object[] args)
        { return target.GetType().GetMethod(name, Hidden).Invoke(target, args); }

        private static IEnumerator Prepared(GameSession game)
        {
            double deadline = EditorApplication.timeSinceStartup + 45;
            while (game.SceneLoading)
            {
                if (!string.IsNullOrEmpty(game.SceneLoadingError)) throw new InvalidOperationException(game.SceneLoadingError);
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Dungeon completion validation waited 45 seconds for scene preparation.");
                yield return null;
            }
            if (!game.HasStarted || game.Player == null) throw new InvalidOperationException("Prepared dungeon fixture requires a live player.");
            foreach (var enemy in game.Enemies) if (enemy != null) enemy.enabled = false;
            game.Player.enabled = false;
        }

        private static void RequireIsolation(GameSession game)
        {
            string overridden = SessionState.GetString("Emberfall.ValidationSaveDirectory", "");
            if (!Application.isPlaying || !EditorApplication.isPlaying || game == null || game != GameSession.Instance ||
                game.Progression == null || string.IsNullOrEmpty(overridden))
                throw new InvalidOperationException("Dungeon completion validation requires an isolated active Play session.");
            string save = Path.GetFullPath(overridden).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string results = Path.GetDirectoryName(save), runName = Path.GetFileName(results);
            string expectedRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Tests", "TestResults"));
            Guid runId;
            if (Path.GetFileName(save) != "IsolatedSave" || Path.GetDirectoryName(results) != expectedRoot ||
                runName.Length < 33 || runName[runName.Length - 33] != '-' ||
                !Guid.TryParseExact(runName.Substring(runName.Length - 32), "N", out runId) ||
                Path.GetFullPath(game.Progression.SaveDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) != save ||
                Path.GetDirectoryName(Path.GetFullPath(game.Progression.SaveFilePath)) != save)
                throw new InvalidOperationException("Dungeon completion validation refused a save outside its isolated GUID run.");
        }

        private static void Kill(GameSession game, EnemyController enemy)
        { enemy.TakeDamage(100000000f, Vector3.forward, impact: false); }

        private static bool Retired(EnemyController enemy)
        { return enemy == null || !enemy.gameObject.activeInHierarchy; }

        private static IEnumerator PostResultAction(GameSession game, Action<bool, string> check)
        {
            double deadline = EditorApplication.timeSinceStartup + 8;
            while (!game.FinishedResultReady)
            {
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Native result presentation did not finish.");
                yield return null;
            }
            int epoch = game.Player.CombatEpoch;
            game.DismissFinishedResult();
            check(game.FinishedResultDismissed && !game.InputBlocked && !game.CombatEffectsEnded && game.CombatEnded && game.Player.CombatEpoch == epoch + 1,
                "Native dismissed success opens a fresh player action epoch while enemy damage remains terminal");
            epoch = game.Player.CombatEpoch; game.DismissFinishedResult();
            check(game.Player.CombatEpoch == epoch, "Native duplicate result dismissal preserves new casts");

            // Keep construction inside the actual scene/factory and its twelve
            // prop cap. Free one distant optional fixture slot if the scene is full.
            if (DestructibleProp.ActiveCount >= DestructiblePropRules.MaximumProps)
            {
                var spare = UnityEngine.Object.FindObjectsByType<DestructibleProp>(FindObjectsSortMode.None)
                    .OrderByDescending(prop => (prop.transform.position - game.Player.transform.position).sqrMagnitude).First();
                spare.gameObject.SetActive(false); UnityEngine.Object.Destroy(spare.gameObject);
            }
            var world = (GameObject)typeof(GameSession).GetField("world", Hidden).GetValue(game);
            DestructibleProp target = null;
            for (int i = 0; i < 16 && target == null; i++)
            {
                float angle = i * Mathf.PI / 4;
                var at = game.Player.transform.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (i < 8 ? 1.5f : 2.5f);
                at = WorldTraversal.NearestWalkable(at, .4f);
                if (!CombatSight.Area(game.Player.transform.position, at)) continue;
                target = DestructiblePropFactory.Create(world.transform, at, DestructibleKind.Pot, 1, true, PropRecovery.None);
            }
            if (target == null) throw new InvalidOperationException("Native post-result fixture could not place a reachable pot.");
            int kills = game.Progression.Profile.kills, gold = game.Progression.Profile.gold, materials = game.Progression.Profile.mechanicMaterials;
            long rewards = game.Progression.Profile.chapterRewardSequence;
            game.Progression.Profile.skillRanks[0] = 3; game.Player.RestoreSkillEnergy(100);
            check(game.Player.CastImmediateSkill(0), "Native dismissed result accepts the actual learned Vanguard skill");
            Call(game.Player, "AdvanceSkillRelease", .2f);
            check(target.Broken,
                "Native post-result paid skill breaks real scene pottery through production melee contact");
            // Paid contact can queue a visual producer for the next player update.
            // Observe the rendered effect after that update, not within the cast stack.
            yield return null;
            check(UnityEngine.Object.FindObjectsByType<FilledSkillVfx>(FindObjectsSortMode.None).Any() ||
                UnityEngine.Object.FindObjectsByType<BlenderSkillVfx>(FindObjectsSortMode.None).Any(),
                "Native post-result skill creates its real visible mesh effect");
            yield return null;
            check(UnityEngine.Object.FindObjectsByType<FilledSkillVfx>(FindObjectsSortMode.None).Any() ||
                UnityEngine.Object.FindObjectsByType<BlenderSkillVfx>(FindObjectsSortMode.None).Any(),
                "Native post-result mesh effect survives its first update");
            check(game.Progression.Profile.kills == kills && game.Progression.Profile.gold == gold &&
                game.Progression.Profile.mechanicMaterials == materials && game.Progression.Profile.chapterRewardSequence == rewards,
                "Native post-result scenery destruction never creates enemy kills or completion rewards");
        }

        public static IEnumerator Validate(GameSession game, Action<bool, string> check, Action<string> log, bool postResultOnly = false)
        {
            if (check == null) throw new ArgumentNullException(nameof(check));
            // Check the standalone runner's save override before any write.
            RequireIsolation(game);

            log?.Invoke("DUNGEON COMPLETION — native boss-only phases, forest direct recap, retirement and durable retry");
            game.StartNew(HeroClass.Vanguard);
            var prepared = Prepared(game); while (prepared.MoveNext()) yield return prepared.Current;
            RequireIsolation(game);
            try
            {
                if (!postResultOnly) foreach (bool failWrite in new[] { false, true })
                {
                    game.StartNew(HeroClass.Vanguard);
                    prepared = Prepared(game); while (prepared.MoveNext()) yield return prepared.Current;
                    RequireIsolation(game);
                    game.SelectedArenaMode = 2; game.SelectedDungeonTier = 1;
                    check((bool)Call(game, "ChangeZone", true), "Native gauntlet enters through real guarded zone transition");
                    prepared = Prepared(game); while (prepared.MoveNext()) yield return prepared.Current;
                    check(game.ModeRun != null && game.ModeRun.Mode == ExpeditionModeKind.BossGauntlet, "Native gauntlet has real phase state");
                    int epoch = game.Player.CombatEpoch, kills = game.Progression.Profile.kills, ended = 0;
                    string temporary = game.Progression.SaveFilePath + ".tmp"; bool ownsTemporary = false;
                    try
                    {
                        for (int phase = 0; phase < 3; phase++)
                        {
                            check(game.ModeRun.CurrentPhase != null && game.ModeRun.CurrentPhase.PhaseIndex == phase,
                                "Native automatic gauntlet phase begins in order");
                            var boss = game.Enemies.Single(enemy => enemy.IsBoss);
                            var adds = game.Enemies.Where(enemy => !enemy.IsBoss).ToArray();
                            CombatProjectile.Hostile(game, game.Player.transform.position + Vector3.up * 6, Vector3.up, 1, 0,
                                sourceName: "Isolated completion probe", onEnded: () => ended++);
                            if (failWrite && phase == 2)
                            {
                                if (File.Exists(temporary) || Directory.Exists(temporary)) throw new IOException("Completion fault path already exists.");
                                Directory.CreateDirectory(temporary); ownsTemporary = true;
                            }
                            Kill(game, boss);
                            check(game.Enemies.Count == 0 && adds.All(Retired), "Native boss kill immediately removes every surviving add");
                            check(game.Progression.Profile.kills == kills + phase + 1, "Native add retirement never creates kill rewards");
                            check(ended == phase + 1 && game.Player.CombatEpoch == epoch, "Native retirement ends hostile volleys without resetting player casts");
                            int earned = game.Progression.Profile.kills;
                            foreach (var add in adds) game.OnEnemyKilled(add);
                            check(game.Progression.Profile.kills == earned, "Native retired add callbacks cannot award stale kills");
                            if (phase < 2)
                            {
                                check(game.ModeRun.Status == ExpeditionModeStatus.AwaitingSpawn, "Native same-frame boss action cannot hit next phase");
                                double deadline = EditorApplication.timeSinceStartup + 5;
                                while (game.ModeRun.Status == ExpeditionModeStatus.AwaitingSpawn)
                                {
                                    if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Gauntlet automatic phase did not start.");
                                    yield return null;
                                }
                                foreach (var enemy in game.Enemies) if (enemy != null) enemy.enabled = false;
                            }
                        }
                        check(game.DungeonCleared && game.ModeFinished && game.Enemies.Count == 0, "Native gauntlet final boss immediately reaches an empty terminal encounter");
                        if (failWrite)
                        {
                            check(game.ModeRewardPending && !game.ModeRun.RewardReserved, "Native failed final write retains retryable reward ticket");
                            Directory.Delete(temporary); ownsTemporary = false;
                            check(game.TrySettleArenaReward(), "Native same terminal gauntlet receipt retries successfully");
                        }
                        int gold = game.Progression.Profile.gold, materials = game.Progression.Profile.mechanicMaterials;
                        long sequence = game.Progression.Profile.chapterRewardSequence;
                        check(game.TrySettleArenaReward() && game.TrySettleArenaReward() && game.Progression.Profile.gold == gold &&
                            game.Progression.Profile.mechanicMaterials == materials && game.Progression.Profile.chapterRewardSequence == sequence,
                            "Native gauntlet repeated result actions cannot duplicate rewards");
                        var reload = new ProgressionService(game.Progression.SaveDirectory);
                        check(reload.LoadSlot(game.Progression.CurrentSlotId) && reload.Profile.kills == kills + 3,
                            "Native gauntlet persisted only three actual boss kills");
                        var action = PostResultAction(game, check); while (action.MoveNext()) yield return action.Current;
                    }
                    finally { if (ownsTemporary && Directory.Exists(temporary)) Directory.Delete(temporary); }
                }

                game.StartNew(HeroClass.Vanguard);
                prepared = Prepared(game); while (prepared.MoveNext()) yield return prepared.Current;
                game.SelectedArenaMode = -1;
                game.Progression.Profile.level = ChapterProgression.UnlockLevel(ChapterNode.ForestCourt);
                check(game.Progression.SaveDungeonCheckpoint(), "Native forest level gate setup is durable");
                game.SelectedChapterNode = ChapterNode.ForestCourt;
                check(game.ConfirmChapterEnter(), "Native forest enters actual chapter");
                prepared = Prepared(game); while (prepared.MoveNext()) yield return prepared.Current;
                foreach (var enemy in game.Enemies.ToArray()) Kill(game, enemy);
                check(!game.ChapterFinished, "Native forest first-room clear does not skip purification or room exit");
                // Prepare the existing first-room prerequisite through its actual
                // bounded state API; this hook focuses on the terminal clear route.
                for (int seal = 0; seal < 2; seal++) for (int tick = 0; tick < 8; tick++)
                    game.ChapterRun.AdvanceSeal(seal, .25f, true, true, false);
                var plan = (ChapterRoomPlan)typeof(GameSession).GetField("chapterPlan", Hidden).GetValue(game);
                game.Player.Teleport(plan.Exit);
                check(game.EnterNextChapterRoom(), "Native forest still uses its first-room portal transition");
                prepared = Prepared(game); while (prepared.MoveNext()) yield return prepared.Current;
                var finalEnemies = game.Enemies.ToArray();
                Call(game, "SpawnEnemy", EnemyKind.Goblin, game.Progression.Profile.level, game.Player.transform.position + Vector3.right * 3, false);
                var residual = game.Enemies[game.Enemies.Count - 1]; residual.enabled = false;
                int killsBefore = game.Progression.Profile.kills;
                foreach (var enemy in finalEnemies) Kill(game, enemy);
                check(game.ChapterFinished && game.ChapterResultReady && game.DungeonCleared && game.ChapterResult != null && game.ChapterResult.Saved,
                    "Native final forest clear immediately publishes saved recap without touching the final portal");
                check(game.Enemies.Count == 0 && Retired(residual) && game.Progression.Profile.kills == killsBefore + finalEnemies.Length,
                    "Native chapter final result retires residual enemy without awarding its experience");
                int chapterMaterials = game.Progression.Profile.mechanicMaterials;
                check(game.TrySettleChapterReward() && game.Progression.Profile.mechanicMaterials == chapterMaterials,
                    "Native final forest result retry cannot duplicate chapter reward");
                var forestAction = PostResultAction(game, check); while (forestAction.MoveNext()) yield return forestAction.Current;

                if (!postResultOnly)
                {
                game.StartNew(HeroClass.Vanguard);
                prepared = Prepared(game); while (prepared.MoveNext()) yield return prepared.Current;
                game.SelectedArenaMode = 2;
                check((bool)Call(game, "ChangeZone", true), "Native failure fixture enters gauntlet");
                prepared = Prepared(game); while (prepared.MoveNext()) yield return prepared.Current;
                string receiptBefore = game.Progression.Profile.lastModeRewardId;
                int goldBefore = game.Progression.Profile.gold;
                game.ModeRun.Fail(ExpeditionModeFailure.TimeExpired); Call(game, "FinalizeArenaResult");
                check(game.TrySettleArenaReward() && !game.DungeonCleared && game.Progression.Profile.lastModeRewardId == receiptBefore && game.Progression.Profile.gold == goldBefore,
                    "Native failed run cannot issue completion rewards");
                game.ModeRun.Dispose();
                check(game.TrySettleArenaReward() && game.Progression.Profile.lastModeRewardId == receiptBefore && game.Progression.Profile.gold == goldBefore,
                    "Native canceled run cannot issue completion rewards");
                }
            }
            finally
            {
                game.SelectedArenaMode = -1;
                game.StartNew(HeroClass.Vanguard);
            }
            prepared = Prepared(game); while (prepared.MoveNext()) yield return prepared.Current;
            game.Player.enabled = true;
            log?.Invoke("DUNGEON COMPLETION native checks finished; fresh isolated camp restored.");
        }
    }
}

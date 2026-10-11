using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Emberfall.Editor
{
    /// <summary>Prepared real-adapter checks; called only by the isolated Play-mode runner.</summary>
    public static class PersistenceTransitionValidation
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

        public static void Validate(GameSession game, Action<bool, string> check, Action<string> log)
        {
            // Reuse the runner's complete path, GUID, slot-name and Play-mode gate.
            typeof(InventoryUIValidation).GetMethod("RequireIsolatedRuntime", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { game });
            if (check == null) throw new ArgumentNullException(nameof(check));
            if (game.InDungeon || game.IsDead || game.Player == null || game.PendingLootCount != 0 ||
                game.DungeonRewardPending || game.ModeRewardPending || game.DungeonSelectionOpen || game.RoomChainRun != null || game.ModeRun != null)
                throw new InvalidOperationException("Persistence transition checks require a quiet living camp with no pending loot or rewards.");

            ProgressionService progression = game.Progression;
            PlayerController player = game.Player;
            string path = progression.SaveFilePath, temporary = path + ".tmp";
            if (!File.Exists(path) || !File.Exists(path + ".bak") || File.Exists(temporary) || Directory.Exists(temporary))
                throw new InvalidOperationException("Persistence fixture requires its existing isolated primary/backup and an unused temporary path.");
            byte[] primary = File.ReadAllBytes(path), backup = File.ReadAllBytes(path + ".bak");
            DateTime primaryTime = File.GetLastWriteTimeUtc(path), backupTime = File.GetLastWriteTimeUtc(path + ".bak");
            GameProfile originalProfile = progression.Profile;
            string originalError = progression.LastError;
            object lastLoggedError = Field(typeof(ProgressionService), "lastLoggedSaveError").GetValue(progression);
            object notification = Field(typeof(GameSession), "notification").GetValue(game);
            object notificationUntil = Field(typeof(GameSession), "notificationUntil").GetValue(game);
            bool paused = game.Paused, blocking = (bool)Field(typeof(GameSession), "uiBlocking").GetValue(game);
            bool originalEnabled = player.enabled, challenge = game.ChallengeRun, selectedChallenge = game.SelectedChallengeMode;
            int selectedTier = game.SelectedDungeonTier;
            float timeScale = Time.timeScale;
            Vector3 position = player.transform.position;
            Quaternion rotation = player.transform.rotation;
            SkillRuntime originalRuntime = (SkillRuntime)Field(typeof(PlayerController), "skillRuntime").GetValue(player);
            RunChoices originalChoices = game.RunChoices;
            GameObject world = (GameObject)Field(typeof(GameSession), "world").GetValue(game);
            EnemyController[] enemies = game.Enemies.ToArray();
            bool[] enemyActive = Array.ConvertAll(enemies, enemy => enemy != null && enemy.gameObject.activeSelf);
            var sessionReceipts = (HashSet<string>)Field(typeof(GameSession), "collectedGroundLoot").GetValue(game);
            var serviceReceipts = (HashSet<string>)Field(typeof(ProgressionService), "collectedLootIds").GetValue(progression);
            string[] originalSessionReceipts = sessionReceipts.ToArray(), originalServiceReceipts = serviceReceipts.ToArray();
            string receipt = "blocked-transition-" + Guid.NewGuid().ToString("N");
            bool ownsTemporary = false;
            try
            {
                sessionReceipts.Add(receipt); serviceReceipts.Add(receipt);
                log?.Invoke("PERSISTENCE TRANSITIONS — failed preflight preserves the old world, run and skill cooldowns");
                var profile = JsonUtility.FromJson<GameProfile>(JsonUtility.ToJson(originalProfile));
                profile.level = Math.Max(2, profile.level); profile.skillRanks[0] = 1;
                Set(progression, "Profile", profile);
                var runtime = new SkillRuntime(player.HeroClass);
                check(runtime.TryConsume(0, 1), "Persistence fixture seeds a real active skill cooldown once");
                float cooldown = runtime.Remaining(0), energy = runtime.Energy;
                Field(typeof(PlayerController), "skillRuntime").SetValue(player, runtime);
                var choices = new RunChoices(); choices.Prepare(1, player.HeroClass, profile.skillRanks, 17);
                check(choices.Choose(0), "Persistence fixture seeds a distinct run-only state");
                RunBlessing[] active = choices.Active.ToArray();
                Set(game, "RunChoices", choices);
                player.enabled = false;
                game.SetPaused(false); game.SetUIBlocking(false);
                player.transform.position = new Vector3(0, 0, 11);
                game.SelectedChallengeMode = !challenge;
                File.Copy(path, temporary, false); ownsTemporary = true;

                game.EnterDungeon();
                check(game.DungeonSelectionOpen && !game.InDungeon, "Fixture opens the real portal selector without entering");
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    game.ConfirmDungeonSelection();
                    check(game.DungeonSelectionOpen && !game.InDungeon && game.ChallengeRun == challenge,
                        "Failed/repeated dungeon confirmation restores the old challenge flag and retains the selector");
                    check(game.InputBlocked && Time.timeScale == 0 && !string.IsNullOrEmpty(progression.LastError),
                        "Failed entry remains paused with a visible persistence error");
                    check(ReferenceEquals(world, Field(typeof(GameSession), "world").GetValue(game)) && world != null && world.activeSelf &&
                        game.Enemies.SequenceEqual(enemies) && enemies.Select((enemy, i) => (enemy != null && enemy.gameObject.activeSelf) == enemyActive[i]).All(value => value),
                        "Failed entry retains the original scene and enemy membership/activation");
                    check(ReferenceEquals(game.RunChoices, choices) && choices.CompletedWave == 1 && choices.Active.SequenceEqual(active),
                        "Failed entry does not reset the previous run state");
                    check(runtime.Remaining(0) == cooldown && runtime.Energy == energy && player.transform.position == new Vector3(0, 0, 11),
                        "Failed entry neither resets cooldowns nor refills energy nor teleports the player");
                }
                game.CancelDungeonSelection();
                game.SetPaused(true);
                bool changed = (bool)typeof(GameSession).GetMethod("ChangeZone", Hidden).Invoke(game, new object[] { false });
                check(!changed && game.Paused && ReferenceEquals(world, Field(typeof(GameSession), "world").GetValue(game)),
                    "Failed camp transition also stops before teardown and retains the prior pause state");
                check(!game.SaveBeforeLeaving() && game.HasStarted && ReferenceEquals(game.Player, player),
                    "The leave adapter rejects the same failed write without abandoning the adventure");
                int slotCount = progression.GetSaveSlots().Count;
                string currentSlot = progression.CurrentSlotId;
                game.StartNew(player.HeroClass == HeroClass.Vanguard ? HeroClass.Arcanist : HeroClass.Vanguard);
                check(progression.CurrentSlotId == currentSlot && progression.GetSaveSlots().Count == slotCount &&
                    ReferenceEquals(game.Player, player) && ReferenceEquals(game.Progression, progression),
                    "Failed new-character preflight preserves the old character and creates no new save target");
                check(!game.SaveAsNewSlot() && progression.CurrentSlotId == currentSlot && progression.GetSaveSlots().Count == slotCount,
                    "Failed snapshot preflight cannot move pending progress to another autosave target");

                var roomRun = new RoomChainState();
                RoomChainPlan room = roomRun.Room;
                for (int i = 0; i < room.EnemyCount; i++) { roomRun.Register(room, i); roomRun.Defeat(room, i); }
                for(int tick=0;tick<24;tick++)roomRun.Advance(.25f,true,true,false);
                check(roomRun.DoorUnlocked, "Room transition fixture has a genuinely unlocked first door");
                Set(game, "RoomChainRun", roomRun); Set(game, "InDungeon", true);
                game.SetPaused(false); player.transform.position = new Vector3(0, 0, 14);
                check(!game.EnterNextRoom() && ReferenceEquals(roomRun.Room, room) && roomRun.DoorUnlocked &&
                    ReferenceEquals(world, Field(typeof(GameSession), "world").GetValue(game)) && runtime.Remaining(0) == cooldown && runtime.Energy == energy,
                    "Failed room preflight preserves room identity, the old scene, cooldowns and energy");
                Set(game, "RoomChainRun", null); Set(game, "InDungeon", false);
                check(sessionReceipts.Contains(receipt) && serviceReceipts.Contains(receipt) &&
                    sessionReceipts.Count == originalSessionReceipts.Length + 1 && serviceReceipts.Count == originalServiceReceipts.Length + 1,
                    "All failed zone/room/new-character/save attempts preserve both old-world duplicate receipt sets");
                check(File.ReadAllBytes(path).SequenceEqual(primary) && File.ReadAllBytes(path + ".bak").SequenceEqual(backup) &&
                    File.GetLastWriteTimeUtc(path) == primaryTime && File.GetLastWriteTimeUtc(path + ".bak") == backupTime &&
                    File.ReadAllBytes(temporary).SequenceEqual(primary),
                    "Blocked adapters preserve both durable snapshots and the recoverable temporary document exactly");
            }
            finally
            {
                // No successful transition is expected, so restore only fixture
                // state and our own temporary file, never rewrite primary/backup.
                if (ownsTemporary && File.Exists(temporary)) File.Delete(temporary);
                sessionReceipts.Clear(); sessionReceipts.UnionWith(originalSessionReceipts);
                serviceReceipts.Clear(); serviceReceipts.UnionWith(originalServiceReceipts);
                Set(progression, "Profile", originalProfile); Set(progression, "LastError", originalError);
                Field(typeof(ProgressionService), "lastLoggedSaveError").SetValue(progression, lastLoggedError);
                Set(game, "RunChoices", originalChoices); Set(game, "ChallengeRun", challenge);
                Set(game, "RoomChainRun", null); Set(game, "InDungeon", false);
                Set(game, "DungeonSelectionOpen", false);
                game.SelectedChallengeMode = selectedChallenge; game.SelectedDungeonTier = selectedTier;
                Field(typeof(PlayerController), "skillRuntime").SetValue(player, originalRuntime);
                player.transform.position = position; player.transform.rotation = rotation; player.enabled = originalEnabled;
                game.SetPaused(paused); game.SetUIBlocking(blocking); Time.timeScale = timeScale;
                Field(typeof(GameSession), "notification").SetValue(game, notification);
                Field(typeof(GameSession), "notificationUntil").SetValue(game, notificationUntil);
            }
        }

        private static FieldInfo Field(Type type, string name)
        { return type.GetField(name, Hidden) ?? throw new MissingFieldException(type.FullName, name); }
        private static void Set(object target, string name, object value)
        {
            PropertyInfo property = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            MethodInfo setter = property == null ? null : property.GetSetMethod(true);
            if (setter == null) throw new MissingMemberException(target.GetType().FullName, name);
            setter.Invoke(target, new[] { value });
        }
    }
}

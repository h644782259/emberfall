using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Emberfall.Editor
{
    public static class SaveSlotRuntimeValidation
    {
        public static IEnumerator Validate(GameSession game, Action<bool, string> check, Action<string> log)
        {
            string root = Path.GetFullPath(game.Progression.SaveDirectory);
            string testRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Tests", "TestResults")) + Path.DirectorySeparatorChar;
            if (!root.StartsWith(testRoot, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Save-slot runtime validation requires isolated test saves.");
            log("SAVE SLOTS — independent characters, snapshots and explicit session loads");
            game.StartNew(HeroClass.Vanguard);
            check(game.HasStarted, "A new adventure creates its own save slot");
            game.Progression.Profile.gold = 431;
            game.Progression.Save();
            string firstPath = game.Progression.SaveFilePath;
            SaveSlotInfo first = game.Progression.GetSaveSlots().Find(slot => slot.IsCurrent);
            check(first != null && first.CanLoad, "New character appears as the current loadable slot");
            string originalBytes = File.ReadAllText(firstPath);
            check(game.SaveAsNewSlot(), "Explicit snapshot service API creates another independent save");
            check(game.Progression.SaveFilePath != firstPath, "Snapshot becomes the active autosave destination");
            SaveSlotInfo snapshot = game.Progression.GetSaveSlots().Find(slot => slot.IsCurrent);
            game.Progression.Profile.gold = 932;
            game.Progression.Save();
            check(File.ReadAllText(firstPath) == originalBytes, "Saving the snapshot preserves the original character file");
            game.QuitToTitle();
            yield return null;
            check(game.ContinueGame(first.Id) && game.Progression.Profile.gold == 431, "Explicit slot selection loads the original progress rather than the latest snapshot");
            game.StartNew(HeroClass.Arcanist);
            check(game.Player.HeroClass == HeroClass.Arcanist && File.Exists(firstPath), "A different new character keeps previous characters available");
            check(game.ContinueGame(snapshot.Id) && game.Progression.Profile.heroClass == HeroClass.Vanguard && game.Progression.Profile.gold == 932,
                "Selected snapshot replaces the runtime hero and restores its own progress");
            string active = game.Progression.SaveFilePath;
            check(!game.ContinueGame("../outside") && game.Progression.SaveFilePath == active && game.HasStarted,
                "Rejected slot identifiers preserve the current runtime session");
            game.QuitToTitle();
            yield return null;
        }
    }
}

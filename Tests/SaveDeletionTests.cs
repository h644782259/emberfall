// Production-code fixtures. All files live under a unique temporary test directory;
// these tests never inspect Application.persistentDataPath or existing player saves.
using System;
using System.IO;
using System.Linq;
using Emberfall;

public static class SaveDeletionTests
{
    private static string root;
    private static int assertions, cases;
    public static string Run(string testDirectory)
    {
        root = Path.Combine(testDirectory, "deletion-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        assertions = cases = 0;
        ConfirmationAndCancellationAreReadOnly();
        StableIdentitySurvivesSortAndSameClass();
        ActiveAndLastDeletionCannotAutosave();
        BackupOnlyAndDamagedDeletion();
        InvalidAndChangedRequestsDoNotDelete();
        InterruptedDeletionCannotResurrect();
        DirectoryAndLinkTargetsAreNotDeleted();
        StorageGrowthIsBoundedWithoutEviction();
        RepeatedDeletionDoesNotAccumulateMarkers();
        OrphanTemporaryFilesReserveCapacity();
        FailedTemporaryWritesAreCleaned();
        RecoverableTemporaryProgressIsPreserved();
        return "PASS: " + assertions + " save-deletion/storage assertions in " + cases + " isolated scenarios.";
    }

    private static ProgressionService Fresh()
    {
        var service = new ProgressionService(Path.Combine(root, "case-" + (++cases)));
        Check(service.CreateNewSlot(HeroClass.Ranger), "fixture creates a modern character");
        return service;
    }
    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new InvalidOperationException(message);
    }
    private static SaveDeletionRequest Prepare(ProgressionService service, string id)
    {
        SaveDeletionRequest request;
        Check(service.PrepareSaveDeletion(id, out request) && request != null, "prepare stable confirmation request");
        return request;
    }
    private static string DirectorySnapshot(string directory)
    {
        return string.Join("\n", Directory.GetFiles(directory).OrderBy(path => path).Select(path =>
            Path.GetFileName(path) + ":" + File.GetLastWriteTimeUtc(path).Ticks + ":" + Convert.ToBase64String(File.ReadAllBytes(path))));
    }

    private static void ConfirmationAndCancellationAreReadOnly()
    {
        ProgressionService p = Fresh();
        string id = p.CurrentSlotId, before = DirectorySnapshot(p.SaveDirectory);
        var profile = p.Profile;
        int changed = 0;
        p.Changed += () => changed++;
        SaveDeletionRequest request = Prepare(p, id);
        Check(request.Id == id && request.DisplayName.Contains("游侠") && request.WasCurrent && request.SavedAtUtc.Kind == DateTimeKind.Utc,
            "confirmation identifies exact character, full ID, save time and current status");
        // Cancel is deliberately just dropping the request; no service mutation exists.
        request = null;
        Check(DirectorySnapshot(p.SaveDirectory) == before && ReferenceEquals(profile, p.Profile) && changed == 0 && p.HasActiveSave,
            "cancel leaves primary, backup, profile, active ID and notifications unchanged");
        Check(p.LoadSlot(id), "cancelled target remains loadable");
    }

    private static void StableIdentitySurvivesSortAndSameClass()
    {
        ProgressionService p = Fresh();
        string firstId = p.CurrentSlotId, firstPath = p.SaveFilePath;
        SaveDeletionRequest request = Prepare(p, firstId);
        Check(p.CreateNewSlot(HeroClass.Ranger), "same-class second character created after confirmation");
        string secondId = p.CurrentSlotId, secondPath = p.SaveFilePath;
        string secondBytes = File.ReadAllText(secondPath), secondBackup = File.ReadAllText(secondPath + ".bak");
        File.WriteAllText(firstPath + ".notes", "leave related-looking unknown suffix alone");
        File.WriteAllText(Path.Combine(p.SaveDirectory, "other.json"), "unrelated file");
        Directory.CreateDirectory(Path.Combine(p.SaveDirectory, "nested"));
        File.WriteAllText(Path.Combine(p.SaveDirectory, "nested", Path.GetFileName(firstPath)), "nested user backup");
        Check(p.DeleteSaveSlot(request), "delete exact captured ID even after a same-class slot reorders list");
        Check(!File.Exists(firstPath) && !File.Exists(firstPath + ".bak") && !File.Exists(firstPath + ".tmp") && !File.Exists(firstPath + ".delete-pending"),
            "successful deletion removes main, backup, temporary and deletion marker");
        Check(p.CurrentSlotId == secondId && p.HasActiveSave && File.ReadAllText(secondPath) == secondBytes && File.ReadAllText(secondPath + ".bak") == secondBackup,
            "deleting inactive target keeps active character and bytes unchanged");
        Check(File.Exists(firstPath + ".notes") && File.Exists(Path.Combine(p.SaveDirectory, "other.json")) &&
            File.Exists(Path.Combine(p.SaveDirectory, "nested", Path.GetFileName(firstPath))), "no glob, recursive or unknown-file deletion");
        string remaining = DirectorySnapshot(p.SaveDirectory);
        Check(p.DeleteSaveSlot(request) && DirectorySnapshot(p.SaveDirectory) == remaining, "repeated completed request is harmless and idempotent");
        Check(p.GetSaveSlots().Count == 1 && p.GetSaveSlots()[0].Id == secondId, "list refresh contains only remaining ID");
    }

    private static void ActiveAndLastDeletionCannotAutosave()
    {
        ProgressionService p = Fresh();
        string id = p.CurrentSlotId, path = p.SaveFilePath;
        var stale = new ProgressionService(p.SaveDirectory);
        Check(stale.LoadSlot(id), "second service holds the same character before deletion");
        File.WriteAllText(path + ".tmp", File.ReadAllText(path));
        SaveDeletionRequest request = Prepare(p, id);
        int changed = 0; p.Changed += () => changed++;
        Check(p.DeleteSaveSlot(request) && changed == 1, "active last character deletion emits a single refresh");
        Check(!p.HasSave && !p.HasActiveSave && p.CurrentSlotId == null && p.GetSaveSlots().Count == 0, "last delete detaches active destination and empties list");
        p.Save(); p.Save(); p.AddGold(5);
        Check(!p.SaveAsNewSlot() && Directory.GetFiles(p.SaveDirectory).Length == 0, "autosave, progression and snapshot cannot resurrect deleted in-memory character");
        stale.Save();
        Check(!stale.HasActiveSave && Directory.GetFiles(p.SaveDirectory).Length == 0, "stale loaded service cannot recreate deleted files");
        Check(!new ProgressionService(p.SaveDirectory).LoadSlot(id), "new process cannot recover deleted backup");
        Check(p.DeleteSaveSlot(request) && changed == 2, "repeating deletion emits no additional change (AddGold emitted one)");
        Check(p.CreateNewSlot(HeroClass.Summoner) && p.HasActiveSave && p.CurrentSlotId != id && p.Profile.heroClass == HeroClass.Summoner,
            "explicit new character works after last deletion with a new ID");
        Check(!File.Exists(path) && !File.Exists(path + ".bak"), "old files stay absent after new character autosave");
        SaveDeletionRequest again = Prepare(p, p.CurrentSlotId); Check(p.DeleteSaveSlot(again), "delete second fixture");
        p.NewGame(HeroClass.Arcanist);
        Check(p.HasActiveSave && p.CurrentSlotId != again.Id && p.Profile.heroClass == HeroClass.Arcanist, "legacy NewGame API also chooses a fresh ID after deletion");
    }

    private static void BackupOnlyAndDamagedDeletion()
    {
        ProgressionService p = Fresh();
        string path = p.SaveFilePath, id = p.CurrentSlotId;
        File.Delete(path);
        Check(p.GetSaveSlots()[0].RecoveredFromBackup, "backup-only fixture discovered");
        Check(p.DeleteSaveSlot(Prepare(p, id)) && !p.HasSave, "backup-only character permanently deleted");
        Check(p.CreateNewSlot(HeroClass.Vanguard), "create damaged fixture");
        path = p.SaveFilePath; id = p.CurrentSlotId;
        File.WriteAllText(path, "corrupt"); File.WriteAllText(path + ".bak", "also corrupt");
        Check(!p.GetSaveSlots()[0].CanLoad, "damaged character remains selectable for management");
        Check(p.DeleteSaveSlot(Prepare(p, id)) && !p.HasSave, "damaged character deletion works without deserializing it");
        p.NewGame(HeroClass.Vanguard);
        string legacyDirectory = Path.Combine(root, "case-" + (++cases));
        var legacy = new ProgressionService(legacyDirectory); legacy.NewGame(HeroClass.Arcanist);
        Check(legacy.CurrentSlotId == "legacy" && legacy.DeleteSaveSlot(Prepare(legacy, "legacy")) && !legacy.HasSave,
            "legacy primary/backup use the same safe deletion flow");
    }

    private static void InvalidAndChangedRequestsDoNotDelete()
    {
        ProgressionService p = Fresh();
        string id = p.CurrentSlotId, before = DirectorySnapshot(p.SaveDirectory);
        SaveDeletionRequest invalid;
        foreach (string input in new[] { null, "", "..", "../outside", "..\\outside", "legacy/../outside", "LEGACY", id + ".json", Guid.NewGuid().ToString("N") })
            Check(!p.PrepareSaveDeletion(input, out invalid) && invalid == null, "invalid, traversal and missing IDs rejected");
        Check(!p.DeleteSaveSlot(null) && DirectorySnapshot(p.SaveDirectory) == before, "invalid requests do not mutate files");
        SaveDeletionRequest stale = Prepare(p, id);
        p.AddGold(100);
        before = DirectorySnapshot(p.SaveDirectory);
        Check(!p.DeleteSaveSlot(stale) && p.LastError.Contains("已改变") && DirectorySnapshot(p.SaveDirectory) == before,
            "progress changed after prompt requires new confirmation without deleting anything");
        SaveDeletionRequest foreign = Prepare(p, id);
        ProgressionService other = Fresh();
        Check(!other.DeleteSaveSlot(foreign) && DirectorySnapshot(p.SaveDirectory) == before, "confirmation cannot be replayed in another directory");
        SaveDeletionRequest current = Prepare(p, id);
        string path = p.SaveFilePath;
        DateTime timestamp = File.GetLastWriteTimeUtc(path);
        string contents = File.ReadAllText(path);
        File.WriteAllText(path, contents.Replace("游", "流")); // Same length if present, else use a harmless whitespace substitution.
        if (File.ReadAllText(path) == contents) File.WriteAllText(path, contents.Replace("\n", " "));
        File.SetLastWriteTimeUtc(path, timestamp);
        Check(!p.DeleteSaveSlot(current) && File.Exists(path), "content digest rejects changed content even with original timestamp and same length");
    }

    private static void InterruptedDeletionCannotResurrect()
    {
        ProgressionService p = Fresh();
        string path = p.SaveFilePath, id = p.CurrentSlotId;
        string backup = File.ReadAllText(path + ".bak");
        // Simulate power loss after durable deletion marker and before backup removal.
        File.WriteAllText(path + ".delete-pending", "Emberfall confirmed character deletion v1\n");
        File.Delete(path);
        var restarted = new ProgressionService(p.SaveDirectory);
        SaveSlotInfo interrupted = restarted.GetSaveSlots()[0];
        Check(interrupted.Id == id && interrupted.DeletionPending && !interrupted.CanLoad && !interrupted.RecoveredFromBackup,
            "interrupted deletion is visible but cannot recover a backup");
        Check(!restarted.LoadSlot(id) && File.ReadAllText(path + ".bak") == backup, "load never resurrects interrupted deletion or removes remaining valuable bytes");
        p.Save(); Check(!p.HasActiveSave && !File.Exists(path), "pre-deletion active service is also blocked by marker");
        Check(restarted.DeleteSaveSlot(Prepare(restarted, id)) && !restarted.HasSave && Directory.GetFiles(p.SaveDirectory).Length == 0,
            "new explicit confirmation cleans interrupted deletion with no permanent accumulating tombstones");
        // Marker alone is also manageable after power loss between last data file and marker removal.
        File.WriteAllText(path + ".delete-pending", "marker");
        Check(restarted.GetSaveSlots().Count == 1 && restarted.GetSaveSlots()[0].DeletionPending &&
            restarted.DeleteSaveSlot(Prepare(restarted, id)), "orphan marker can be explicitly cleared");
    }

    private static void DirectoryAndLinkTargetsAreNotDeleted()
    {
        ProgressionService p = Fresh();
        string path = p.SaveFilePath, id = p.CurrentSlotId;
        Directory.CreateDirectory(path + ".tmp");
        File.WriteAllText(Path.Combine(path + ".tmp", "precious.txt"), "keep");
        SaveDeletionRequest request;
        Check(!p.PrepareSaveDeletion(id, out request) && request == null && p.HasActiveSave && File.Exists(path) &&
            File.Exists(Path.Combine(path + ".tmp", "precious.txt")), "unexpected directory paths are not traversed or deleted");
        Directory.Delete(path + ".tmp", true); // Only the directory created by this test.
        string outside = Path.Combine(root, "unrelated-" + Guid.NewGuid().ToString("N"));
        File.WriteAllText(outside, "do not delete");
        File.CreateSymbolicLink(path + ".tmp", outside);
        Check(!p.PrepareSaveDeletion(id, out request) && File.ReadAllText(outside) == "do not delete" && File.Exists(path),
            "symlink artifacts cannot cause writes or deletion of a different target");
        File.Delete(path + ".tmp");
    }

    private static void StorageGrowthIsBoundedWithoutEviction()
    {
        ProgressionService p = Fresh();
        string original = p.SaveFilePath;
        for (int i = 0; i < 150; i++) { p.Profile.gold = 100 + i; p.Save(); }
        Check(Directory.GetFiles(p.SaveDirectory).Length == 2 && !File.Exists(original + ".tmp"), "150 autosaves keep one primary and one rotating backup");
        Check(new FileInfo(original).Length <= ProgressionService.MaximumSaveBytes && new FileInfo(original + ".bak").Length <= ProgressionService.MaximumSaveBytes,
            "written primary and backup stay within documented size bound");
        string valuable = File.ReadAllText(original);
        for (int i = 1; i < ProgressionService.MaximumSaveSlots; i++) Check(p.CreateNewSlot(HeroClass.Ranger), "fill explicit character slots");
        int count = Directory.GetFiles(p.SaveDirectory).Length;
        Check(!p.CreateNewSlot(HeroClass.Arcanist) && !p.SaveAsNewSlot() && p.LastError.Contains("上限"), "cap blocks new characters and snapshots without eviction");
        Check(Directory.GetFiles(p.SaveDirectory).Length == count && count == 2 * ProgressionService.MaximumSaveSlots && File.ReadAllText(original) == valuable,
            "slot cap preserves oldest progress byte-for-byte and creates no partial files");
        var freshLegacyWriter = new ProgressionService(p.SaveDirectory);
        freshLegacyWriter.NewGame(HeroClass.Vanguard);
        Check(!File.Exists(Path.Combine(p.SaveDirectory, "emberfall-save.json")) && freshLegacyWriter.LastError.Contains("上限"),
            "legacy new-game writer cannot bypass the new-slot capacity limit");
        string active = p.CurrentSlotId;
        p.Save(); Check(p.LastError == "" && p.CurrentSlotId == active, "full directory still permits existing character saves");
        // Existing manually imported characters above the cap must not be auto-purged.
        string importedId = Guid.NewGuid().ToString("N"), importedPath = Path.Combine(p.SaveDirectory, "emberfall-save-" + importedId + ".json");
        File.Copy(original, importedPath);
        Check(p.GetSaveSlots().Count == ProgressionService.MaximumSaveSlots + 1 && p.LoadSlot(importedId), "over-cap imported character stays readable");
        Check(!p.CreateNewSlot(HeroClass.Summoner) && File.ReadAllText(importedPath) == valuable, "over-cap import is preserved while new creation remains blocked");
        // Oversized damage can be explicitly deleted, never silently discarded.
        File.WriteAllText(importedPath, new string('x', ProgressionService.MaximumSaveBytes + 1));
        Check(!p.GetSaveSlots().Find(s => s.Id == importedId).CanLoad && File.Exists(importedPath), "oversized damaged primary remains present");
        Check(p.DeleteSaveSlot(Prepare(p, importedId)) && !File.Exists(importedPath) && File.ReadAllText(original) == valuable,
            "bounded fingerprint permits explicit removal of oversized damage without affecting other characters");
    }

    private static void RepeatedDeletionDoesNotAccumulateMarkers()
    {
        ProgressionService p = Fresh();
        for (int i = 0; i < ProgressionService.MaximumSaveSlots + 16; i++)
        {
            SaveDeletionRequest request = Prepare(p, p.CurrentSlotId);
            Check(p.DeleteSaveSlot(request) && p.DeleteSaveSlot(request) && Directory.GetFiles(p.SaveDirectory).Length == 0 && !p.HasSave,
                "successful and repeated deletion leave no primary, backup, temporary file or tombstone");
            Check(p.CreateNewSlot(HeroClass.Ranger), "repeated create/delete beyond active-slot cap cannot exhaust slots with old markers");
        }
        Check(p.DeleteSaveSlot(Prepare(p, p.CurrentSlotId)), "remove final fake role before interrupted-marker fixture");
        for (int i = 0; i < ProgressionService.MaximumSaveSlots; i++)
            File.WriteAllText(Path.Combine(p.SaveDirectory, "emberfall-save-" + Guid.NewGuid().ToString("N") + ".json.delete-pending"), "interrupted fake deletion");
        string before = DirectorySnapshot(p.SaveDirectory);
        Check(p.GetSaveSlots().Count == ProgressionService.MaximumSaveSlots && !p.CreateNewSlot(HeroClass.Arcanist) &&
            DirectorySnapshot(p.SaveDirectory) == before, "retained interrupted markers count toward64 and are never automatically purged to make room");
    }

    private static void OrphanTemporaryFilesReserveCapacity()
    {
        ProgressionService p = Fresh();
        string source = File.ReadAllText(p.SaveFilePath);
        Check(p.DeleteSaveSlot(Prepare(p, p.CurrentSlotId)), "clear isolated primary before orphan-temp capacity test");
        for (int i = 0; i < ProgressionService.MaximumSaveSlots; i++)
            File.WriteAllText(Path.Combine(p.SaveDirectory, "emberfall-save-" + Guid.NewGuid().ToString("N") + ".json.tmp"), i % 2 == 0 ? source : "interrupted partial write");
        string before = DirectorySnapshot(p.SaveDirectory);
        Check(!p.HasSave && p.GetSaveSlots().Count == 0, "orphan temporary documents remain non-playable and do not masquerade as characters");
        Check(!p.CreateNewSlot(HeroClass.Vanguard) && p.LastError.Contains("临时恢复文件上限") && DirectorySnapshot(p.SaveDirectory) == before,
            "64 canonical temporary reservations block creation without deleting or reading their contents as saves");
        var legacy = new ProgressionService(p.SaveDirectory); legacy.NewGame(HeroClass.Arcanist);
        Check(legacy.LastError.Contains("临时恢复文件上限") && DirectorySnapshot(p.SaveDirectory) == before,
            "unattached legacy writer cannot bypass temporary-reservation capacity");
        // Only remove our own fixture artifact to model the user's explicit recovery.
        File.Delete(Directory.GetFiles(p.SaveDirectory, "*.tmp")[0]);
        Check(p.CreateNewSlot(HeroClass.Summoner), "one recovered temporary reservation makes exactly one new character possible");
        File.WriteAllText(p.SaveFilePath + ".tmp", source);
        Check(!p.CreateNewSlot(HeroClass.Ranger), "full identity reservations stay capped after an active slot gains a temporary file");
        File.Delete(Directory.GetFiles(p.SaveDirectory, "*.tmp").First(path => path != p.SaveFilePath + ".tmp"));
        Check(p.CreateNewSlot(HeroClass.Ranger), "temporary, primary and backup of the same identity count only once at the exact capacity boundary");
        ProgressionService q = Fresh();
        Check(q.DeleteSaveSlot(Prepare(q, q.CurrentSlotId)), "prepare unknown-suffix fixture");
        for (int i = 0; i < ProgressionService.MaximumSaveSlots + 1; i++)
            File.WriteAllText(Path.Combine(q.SaveDirectory, "emberfall-save-" + Guid.NewGuid().ToString("N") + ".json.tmp.unknown"), "unrelated");
        Check(q.CreateNewSlot(HeroClass.Ranger), "unknown suffixes remain outside recognized generated-file reservations");
        string first = q.SaveFilePath;
        File.WriteAllText(first + ".tmp", source);
        Check(q.CreateNewSlot(HeroClass.Ranger), "one slot with a temporary file does not reserve a second identity");
    }

    private static void RecoverableTemporaryProgressIsPreserved()
    {
        ProgressionService p = Fresh();
        string path = p.SaveFilePath;
        string primary = File.ReadAllText(path), backup = File.ReadAllText(path + ".bak");
        File.WriteAllText(path + ".tmp", primary);
        p.Profile.gold = 987;
        p.Save();
        Check(p.LastError.Contains("可恢复的临时存档") && File.ReadAllText(path) == primary &&
            File.ReadAllText(path + ".bak") == backup && File.ReadAllText(path + ".tmp") == primary,
            "valid interrupted temporary progress is preserved instead of automatically overwritten");
        Check(p.SaveAsNewSlot() && p.Profile.gold == 987 && p.SaveFilePath != path && File.Exists(path + ".tmp"),
            "explicit snapshot unblocks current progress without discarding the recoverable temporary file");
    }

    private static void FailedTemporaryWritesAreCleaned()
    {
        ProgressionService p = Fresh();
        string path = p.SaveFilePath, backup = File.ReadAllText(path + ".bak");
        File.Delete(path); Directory.CreateDirectory(path);
        p.Save();
        Check(p.LastError.StartsWith("保存失败") && !File.Exists(path + ".tmp") && File.ReadAllText(path + ".bak") == backup,
            "failed move removes only this attempt's temporary file and preserves recovery backup");
        Check(Directory.Exists(path), "write failure never deletes a blocking directory");
    }
}

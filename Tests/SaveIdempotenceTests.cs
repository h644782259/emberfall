// Actual production service; every document is created beneath a unique fake-save root.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Emberfall;

public static class SaveIdempotenceTests
{
    private static string root;
    private static int checks, cases;
    public static string Run(string directory)
    {
        root = Path.Combine(directory, "idempotent-save-" + Guid.NewGuid().ToString("N"));
        checks = cases = 0;
        UnchangedSavesPreserveBothDocuments();
        DirectChangesAndExternalReplacementsAreObserved();
        BackupRepair(false); BackupRepair(true);
        PrimaryRepair(false); PrimaryRepair(true);
        AmbiguousDocumentsArePreserved();
        RecoveryAndDeletionGuardsWin();
        CreationCannotDeduplicateAnExistingSlot();
        RewardThenSavePreservesPriorBackup(false);
        RewardThenSavePreservesPriorBackup(true);
        CallbackMutationStillPersists();
        BoundedReadsAndImportedEncoding();
        LinkedStorageArtifactsArePreserved();
        return "PASS: " + checks + " idempotent-save assertions in " + cases + " isolated scenarios";
    }
    private static void Check(bool value, string why)
    { checks++; if (!value) throw new InvalidOperationException(why); }
    private static ProgressionService Fresh()
    {
        var p = new ProgressionService(Path.Combine(root, "case-" + (++cases)));
        Check(p.CreateNewSlot(HeroClass.Ranger), "create isolated role");
        return p;
    }
    private static void Stamp(string path)
    {
        File.SetLastWriteTimeUtc(path, new DateTime(2001, 2, 3, 4, 5, 6, DateTimeKind.Utc));
        File.SetLastWriteTimeUtc(path + ".bak", new DateTime(2002, 3, 4, 5, 6, 7, DateTimeKind.Utc));
    }
    private static string Snapshot(string path)
    {
        return string.Join("\n", new[] { path, path + ".bak", path + ".tmp", path + ".delete-pending" }.Select(p =>
            File.Exists(p) ? p + ":" + File.GetLastWriteTimeUtc(p).Ticks + ":" + Convert.ToBase64String(File.ReadAllBytes(p)) :
            Directory.Exists(p) ? p + ":directory" : p + ":missing"));
    }
    private static void Success(ProgressionService p, string why)
    { p.Save(); Check(string.IsNullOrEmpty(p.LastError), why); }
    private static void UnchangedSavesPreserveBothDocuments()
    {
        var p = Fresh(); string path = p.SaveFilePath;
        Stamp(path); string before = Snapshot(path); int notifications = 0;
        p.Changed += () => notifications++;
        for (int i = 0; i < 20; i++)
        {
            Success(p, "unchanged save succeeds");
            Check(Snapshot(path) == before, "unchanged save preserves primary/backup bytes and timestamps and creates no temp");
        }
        Check(notifications == 0 && Directory.GetFiles(p.SaveDirectory).Length == 2, "no event or file growth from unchanged autosave");
    }
    private static void DirectChangesAndExternalReplacementsAreObserved()
    {
        var p = Fresh(); string path = p.SaveFilePath;
        Stamp(path); string original = File.ReadAllText(path); DateTime beforeTime = File.GetLastWriteTimeUtc(path);
        p.Profile.gold = 61;
        Success(p, "direct profile mutation persists without a dirty flag");
        string expected = File.ReadAllText(path);
        Check(expected != original && File.ReadAllText(path + ".bak") == original && File.GetLastWriteTimeUtc(path) != beforeTime,
            "changed profile replaces primary and rotates exact prior document");
        Stamp(path); string stable = Snapshot(path); Success(p, "next unchanged save succeeds");
        Check(Snapshot(path) == stable, "changed save is followed by a true no-op");
        string replaced = expected.Replace("\"gold\": 61", "\"gold\": 62");
        Check(replaced != expected && Encoding.UTF8.GetByteCount(replaced) == Encoding.UTF8.GetByteCount(expected), "external edit fixture changes content without changing length");
        DateTime time = File.GetLastWriteTimeUtc(path);
        File.WriteAllText(path, replaced, new UTF8Encoding(false)); File.SetLastWriteTimeUtc(path, time);
        Success(p, "same-size same-time replacement is not mistaken for the current profile");
        Check(File.ReadAllText(path) == expected && File.ReadAllText(path + ".bak") == replaced, "actual contents determine whether to write");
    }
    private static void BackupRepair(bool corrupt)
    {
        var p = Fresh(); string path = p.SaveFilePath, primary = File.ReadAllText(path);
        if (corrupt) File.WriteAllText(path + ".bak", "{broken backup"); else File.Delete(path + ".bak");
        Success(p, (corrupt ? "corrupt" : "missing") + " backup prevents dedup and is repaired");
        Check(File.ReadAllText(path) == primary && File.ReadAllText(path + ".bak") == primary, "atomic replacement restores backup from valid primary");
        Stamp(path); string stable = Snapshot(path); Success(p, "repaired pair is reusable");
        Check(Snapshot(path) == stable, "repair ends after one save");
    }
    private static void PrimaryRepair(bool corrupt)
    {
        var p = Fresh(); string path = p.SaveFilePath;
        p.Profile.gold = 777; Success(p, "prepare distinct primary and prior backup");
        string primary = File.ReadAllText(path), backup = File.ReadAllText(path + ".bak");
        Check(primary != backup, "recovery fixture has two revisions");
        if (corrupt) File.WriteAllText(path, "{broken primary"); else File.Delete(path);
        DateTime backupTime = File.GetLastWriteTimeUtc(path + ".bak");
        Success(p, (corrupt ? "corrupt" : "missing") + " primary is repaired from the live profile");
        Check(File.ReadAllText(path) == primary && File.ReadAllText(path + ".bak") == backup && File.GetLastWriteTimeUtc(path + ".bak") == backupTime,
            "primary repair never rotates corrupt data over the usable backup");
    }
    private static void AmbiguousDocumentsArePreserved()
    {
        foreach (bool missingPrimary in new[] { false, true })
        {
            var p = Fresh(); string path = p.SaveFilePath;
            if (missingPrimary) File.Delete(path); else File.WriteAllText(path, "{broken primary");
            File.WriteAllText(path + ".bak", "{broken backup");
            string before = Snapshot(path); var profile = p.Profile;
            Check(!p.BuyPotion() && !string.IsNullOrEmpty(p.LastError), "candidate transaction refuses when neither existing document is trustworthy");
            Check(ReferenceEquals(profile, p.Profile) && Snapshot(path) == before, "both-damaged failure preserves profile identity and every original artifact");
        }
        var freshLegacy = new ProgressionService(Path.Combine(root, "legacy-" + (++cases)));
        freshLegacy.NewGame(HeroClass.Vanguard);
        Check(string.IsNullOrEmpty(freshLegacy.LastError) && File.Exists(freshLegacy.SaveFilePath) && File.Exists(freshLegacy.SaveFilePath + ".bak"), "first legacy save still creates both documents");
    }
    private static void RecoveryAndDeletionGuardsWin()
    {
        var p = Fresh(); string path = p.SaveFilePath;
        File.Copy(path, path + ".tmp"); string before = Snapshot(path);
        p.Save();
        Check(p.LastError.Contains("可恢复的临时存档") && Snapshot(path) == before, "recoverable temp blocks even byte-identical saves without touching any document");
        File.Delete(path + ".tmp"); Directory.CreateDirectory(path + ".tmp"); before = Snapshot(path);
        p.Save(); Check(!string.IsNullOrEmpty(p.LastError) && Snapshot(path) == before, "blocking temp directory cannot be bypassed by dedup");
        Directory.Delete(path + ".tmp");
        Success(p, "recovered unchanged save clears the prior error");
        File.WriteAllText(path + ".delete-pending", "Emberfall confirmed character deletion v1\n"); before = Snapshot(path);
        p.Save(); Check(!string.IsNullOrEmpty(p.LastError) && Snapshot(path) == before, "deletion marker wins over unchanged-document optimization");
        p = Fresh(); path = p.SaveFilePath;
        File.Delete(path); File.Delete(path + ".bak"); p.Save();
        Check(!string.IsNullOrEmpty(p.LastError) && !File.Exists(path) && !File.Exists(path + ".bak"), "removed attached slot is never resurrected as new creation");
    }
    private static void CreationCannotDeduplicateAnExistingSlot()
    {
        var p = Fresh(); string before = Snapshot(p.SaveFilePath);
        MethodInfo method = typeof(ProgressionService).GetMethod("TryWriteProfile", BindingFlags.NonPublic | BindingFlags.Static);
        object[] args = { p.Profile, p.SaveFilePath, true, null };
        Check(!(bool)method.Invoke(null, args) && !string.IsNullOrEmpty((string)args[3]), "create-only collision is rejected even with identical valid primary and backup");
        Check(Snapshot(p.SaveFilePath) == before, "failed creation cannot rewrite either existing document");
    }
    private static void RewardThenSavePreservesPriorBackup(bool mode)
    {
        var p = Fresh(); string path = p.SaveFilePath, prior = File.ReadAllText(path);
        string receipt = Guid.NewGuid().ToString("N");
        Check(mode ? p.TryGrantModeReward(receipt, 240, 0, 3) : p.TryCompleteDungeonRun(receipt, 5, 240, 0), "reward commits exactly once");
        Check(File.ReadAllText(path + ".bak") == prior && File.ReadAllText(path) != prior, "reward commit preserves the pre-reward recovery point");
        Stamp(path); string durable = Snapshot(path);
        for (int i = 0; i < 3; i++) { Success(p, "leave/background save after reward succeeds"); Check(Snapshot(path) == durable, "post-reward save does not rotate away the prior backup"); }
        Check(mode ? p.TryGrantModeReward(receipt, 240, 0, 3) : p.TryCompleteDungeonRun(receipt, 5, 240, 0), "repeated settlement receipt succeeds");
        Check(Snapshot(path) == durable, "repeated receipt remains a file no-op");
    }
    private static void CallbackMutationStillPersists()
    {
        var p = Fresh(); bool fired = false;
        p.Changed += () => { if (!fired) { fired = true; p.Profile.gold += 7; } };
        Check(p.TryGrantModeReward(Guid.NewGuid().ToString("N"), 100, 0, 1), "reward callback fixture commits");
        string reward = File.ReadAllText(p.SaveFilePath); int expected = p.Profile.gold;
        Success(p, "leave saves a direct mutation made by the reward callback");
        Check(File.ReadAllText(p.SaveFilePath) != reward && File.ReadAllText(p.SaveFilePath + ".bak") == reward, "callback mutation requires a real second write rather than a stale prepared flag");
        Check(p.LoadSlot(p.CurrentSlotId) && p.Profile.gold == expected, "all post-commit callback changes survive reload");
    }
    private static void BoundedReadsAndImportedEncoding()
    {
        var p = Fresh(); string path = p.SaveFilePath, primary = File.ReadAllText(path);
        File.WriteAllText(path + ".bak", new string('x', ProgressionService.MaximumSaveBytes + 1));
        Success(p, "oversized backup is rejected as usable and repaired from valid primary");
        Check(File.ReadAllText(path + ".bak") == primary, "backup repair remains bounded");
        File.WriteAllBytes(path, new byte[] { 0xff, 0xfe, 0xff });
        Success(p, "invalid UTF-8 primary cannot be accepted as identical");
        Check(File.ReadAllText(path) == primary, "invalid encoded primary repairs with usable backup retained");
        File.WriteAllText(path, primary, new UTF8Encoding(true));
        Check(p.LoadSlot(p.CurrentSlotId), "UTF-8 BOM import still loads");
        Success(p, "BOM import canonicalizes safely");
        Check(File.ReadAllBytes(path).SequenceEqual(new UTF8Encoding(false).GetBytes(primary)), "write comparisons use the exact canonical UTF-8 document");
        foreach (Encoding encoding in new Encoding[] { Encoding.Unicode, Encoding.BigEndianUnicode, Encoding.UTF32 })
        {
            File.WriteAllText(path, primary, encoding);
            Check(p.LoadSlot(p.CurrentSlotId), "bounded loading preserves prior BOM-detected import compatibility");
            Success(p, "non-UTF-8 import is canonicalized rather than incorrectly deduplicated");
            Check(File.ReadAllBytes(path).SequenceEqual(new UTF8Encoding(false).GetBytes(primary)), "import normalization writes the exact canonical document");
        }
    }
    private static void LinkedStorageArtifactsArePreserved()
    {
        foreach (string suffix in new[] { ".tmp", ".bak", "", ".delete-pending" })
        {
            var p = Fresh(); string path = p.SaveFilePath;
            string external = Path.Combine(root, "unrelated-" + Guid.NewGuid().ToString("N"));
            File.WriteAllText(external, "unrelated document must never be overwritten");
            File.Delete(path + suffix);
            File.CreateSymbolicLink(path + suffix, external);
            string before = Snapshot(path), externalBefore = File.ReadAllText(external);
            p.Profile.gold++;
            p.Save();
            Check(!string.IsNullOrEmpty(p.LastError), "a linked " + suffix + " storage artifact blocks save");
            Check(Snapshot(path) == before && File.ReadAllText(external) == externalBefore,
                "failed save neither follows nor replaces a linked " + suffix + " artifact");
        }
        foreach (string suffix in new[] { ".tmp", ".bak", ".delete-pending" })
        {
            var p = Fresh(); string path = p.SaveFilePath;
            string external = Path.Combine(root, "absent-" + Guid.NewGuid().ToString("N"));
            File.Delete(path + suffix); File.CreateSymbolicLink(path + suffix, external);
            p.Profile.gold++; p.Save();
            Check(!string.IsNullOrEmpty(p.LastError) && !File.Exists(external) &&
                (File.GetAttributes(path + suffix) & FileAttributes.ReparsePoint) != 0,
                "dangling " + suffix + " link is preserved, never used to create an external target");
        }
    }
}

using System;
using System.IO;
using Emberfall;

public static class SafeSaveFlowTests
{
    private static int assertions;
    private static void Check(bool value, string message) { assertions++; if (!value) throw new Exception(message); }
    public static string Run(string directory)
    {
        assertions = 0;
        string root=Path.Combine(directory,"save-flow-"+Guid.NewGuid().ToString("N"));
        var source=new ProgressionService(root);
        Check(source.CreateNewSlot(HeroClass.Ranger),"source fixture created");
        string sourceId=source.CurrentSlotId, sourcePath=source.SaveFilePath;
        source.Profile.gold=321;source.Save();
        Check(source.CreateNewSlot(HeroClass.Arcanist),"target fixture created");
        string targetId=source.CurrentSlotId,targetPath=source.SaveFilePath;
        source.Profile.gold=456;source.Save();
        Check(source.LoadSlot(sourceId),"source active again");
        string disk=File.ReadAllText(sourcePath),backup=File.ReadAllText(sourcePath+".bak"), targetDisk=File.ReadAllText(targetPath);
        source.Profile.gold=987;
        var request=new SafeSaveFlow();int saves=0,loads=0;
        Func<bool> save=()=>{saves++;source.Save();return source.LastError=="";};
        request.RequestManual(sourceId);request.Cancel();
        Check(!request.ConfirmManual(sourceId,save)&&saves==0,"cancel invokes no manual write");
        Check(File.ReadAllText(sourcePath)==disk&&File.ReadAllText(sourcePath+".bak")==backup,"cancel preserves primary and backup bytes");
        request.RequestManual(sourceId);
        Check(!request.ConfirmManual(targetId,save)&&saves==0&&request.NeedsFreshConfirmation,"manual overwrite bound to named source ID");
        Check(!request.ConfirmManual(sourceId,save)&&saves==0,"changed identity requires fresh confirmation even if selection returns");
        request.Cancel();request.RequestManual(sourceId);
        Check(!request.ConfirmManual(sourceId,()=>false)&&request.Open&&!request.Busy,"manual save failure stays open");
        Check(request.ConfirmManual(sourceId,save)&&!request.Open&&saves==1,"confirmed save succeeds exactly once");
        Check(!request.ConfirmManual(sourceId,save)&&saves==1,"double tap cannot repeat manual save");
        var verify=new ProgressionService(root);Check(verify.LoadSlot(sourceId)&&verify.Profile.gold==987&&File.ReadAllText(targetPath)==targetDisk,"only named source is overwritten");

        // Stage the target through actual production code before any world discard.
        source.Profile.gold=11111;int sourceEvents=0;source.Changed+=()=>sourceEvents++;
        ProgressionService candidate;string error;
        string sourceBytes=File.ReadAllText(sourcePath);
        Check(SaveSlotTransition.TryStage(source,targetId,out candidate,out error)&&candidate.Profile.gold==456,
            "target read into isolated service");
        Check(source.CurrentSlotId==sourceId&&source.Profile.gold==11111&&sourceEvents==0&&File.ReadAllText(sourcePath)==sourceBytes,
            "staging cannot mutate current runtime, emit its Changed event, or save source");
        Check(candidate!=source&&candidate.CurrentSlotId==targetId,"staged service has stable selected target ID");
        request.RequestLoad(sourceId,targetId);request.Cancel();
        Check(!request.ConfirmLoad(sourceId,targetId,SaveLoadChoice.DiscardAndLoad,save,_=>{loads++;return true;})&&loads==0,
            "cancel does not switch or discard");
        request.RequestLoad(sourceId,targetId);
        Check(!request.ConfirmLoad(sourceId,sourceId,SaveLoadChoice.DiscardAndLoad,save,_=>{loads++;return true;})&&loads==0&&request.NeedsFreshConfirmation,
            "changed target cannot reuse old confirmation");
        request.Cancel();request.RequestLoad(sourceId,targetId);
        int before=saves;
        Check(request.ConfirmLoad(sourceId,targetId,SaveLoadChoice.DiscardAndLoad,save,discard=>
        {
            Check(discard,"discard intent reaches switch callback");loads++;
            return SaveSlotTransition.TryStage(source,targetId,out candidate,out error);
        })&&saves==before,"discard-and-load invokes no preservation write");
        Check(File.ReadAllText(sourcePath)==sourceBytes&&source.Profile.gold==11111&&candidate.Profile.gold==456,
            "discard route keeps saved source bytes and stages independent target");
        Check(!request.ConfirmLoad(sourceId,targetId,SaveLoadChoice.SaveAndLoad,save,_=>true)&&saves==before,"repeated completion cannot save discarded source afterward");

        request.RequestLoad(sourceId,targetId);
        Check(!request.ConfirmLoad(sourceId,targetId,SaveLoadChoice.SaveAndLoad,()=>false,_=>{loads++;return true;})&&request.Open&&!request.SavedCurrent,
            "failed source save blocks load while keeping request");
        Check(!request.ConfirmLoad(sourceId,targetId,SaveLoadChoice.SaveAndLoad,save,_=>false)&&request.Open&&request.SavedCurrent,
            "rejected load retains successful source preservation");
        before=saves;
        Check(request.ConfirmLoad(sourceId,targetId,SaveLoadChoice.SaveAndLoad,()=>{throw new Exception("duplicate source save");},discard=>!discard),
            "load retry uses already saved source without duplicate backup rotation");
        Check(saves==before,"saved retry performed no additional write");
        request.RequestLoad(sourceId,targetId);
        Check(request.ConfirmLoad(sourceId,targetId,SaveLoadChoice.DiscardAndLoad,save,discard=>
        {
            Check(!request.ConfirmLoad(sourceId,targetId,SaveLoadChoice.DiscardAndLoad,save,_=>true),"reentrant load blocked");
            request.Cancel();Check(request.Open,"busy request cannot be cancelled mid-publish");return true;
        }),"single load transaction accepted");

        // Broken/removed targets do not publish a candidate or touch current bytes.
        string oldTargetBackup=File.ReadAllText(targetPath+".bak");
        File.WriteAllText(targetPath,"broken");File.WriteAllText(targetPath+".bak","also broken");
        sourceBytes=File.ReadAllText(sourcePath);var originalProfile=source.Profile;
        Check(!SaveSlotTransition.TryStage(source,targetId,out candidate,out error)&&candidate==null&&!string.IsNullOrEmpty(error),"bad primary and backup reject staging");
        Check(ReferenceEquals(source.Profile,originalProfile)&&source.CurrentSlotId==sourceId&&File.ReadAllText(sourcePath)==sourceBytes,
            "failed target retains live profile, stable active slot and persisted data");
        File.WriteAllText(targetPath+".bak",oldTargetBackup);
        Check(SaveSlotTransition.TryStage(source,targetId,out candidate,out error)&&candidate.LastError.Contains("备份"),"backup recovery stages its own target without touching source");
        foreach(string invalid in new[]{"../outside", "", null, Guid.NewGuid().ToString("N")})
            Check(!SaveSlotTransition.TryStage(source,invalid,out candidate,out error)&&candidate==null,"invalid/missing target safely rejected");

        // Same-slot reload after a confirmed save must use the newest persisted snapshot.
        source.Profile.gold=2468;request.RequestLoad(sourceId,sourceId);
        Check(request.ConfirmLoad(sourceId,sourceId,SaveLoadChoice.SaveAndLoad,save,discard=>
            SaveSlotTransition.TryStage(source,sourceId,out candidate,out error)&&candidate.Profile.gold==2468),
            "save-and-load same character restores the just-saved state");
        source.Profile.gold=9999;before=saves;request.RequestLoad(sourceId,sourceId);
        Check(request.ConfirmLoad(sourceId,sourceId,SaveLoadChoice.DiscardAndLoad,save,discard=>
            SaveSlotTransition.TryStage(source,sourceId,out candidate,out error)&&candidate.Profile.gold==2468)&&saves==before,
            "discard-and-load same character restores last saved state without resaving unsaved profile");
        // Real disk failure fixture: only this temporary save's write path is blocked.
        string beforeFailure=File.ReadAllText(sourcePath),beforeBackup=File.ReadAllText(sourcePath+".bak");
        Directory.CreateDirectory(sourcePath+".tmp");request.RequestManual(sourceId);
        Check(!request.ConfirmManual(sourceId,save)&&request.Open&&!request.Busy&&source.LastError.StartsWith("保存失败"),
            "actual filesystem save failure keeps named confirmation open");
        Check(File.ReadAllText(sourcePath)==beforeFailure&&File.ReadAllText(sourcePath+".bak")==beforeBackup,
            "failed manual write preserves primary and recovery bytes");
        Directory.Delete(sourcePath+".tmp");
        Check(request.ConfirmManual(sourceId,save),"manual request retries successfully after fake disk blocker clears");
        request.RequestManual(sourceId);
        try{request.ConfirmManual(sourceId,()=>{throw new IOException("simulated failure");});}catch(IOException){}
        Check(request.Open&&!request.Busy,"thrown save exception retains retryable dialog");request.Cancel();
        request.RequestLoad(sourceId,targetId);
        try{request.ConfirmLoad(sourceId,targetId,SaveLoadChoice.SaveAndLoad,()=>true,_=>{throw new IOException("load rejected");});}catch(IOException){}
        Check(request.Open&&!request.Busy&&request.SavedCurrent,"thrown load exception preserves successful source preparation");request.Cancel();
        return "PASS: "+assertions+" manual-save, cancel, stable-load and isolated staging assertions";
    }
}

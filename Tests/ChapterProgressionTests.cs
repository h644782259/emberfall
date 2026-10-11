using System;
using System.IO;
using Emberfall;
public static class ChapterProgressionTests
{
    static int checks;
    static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    static ChapterRunReceipt Begin(ProgressionService p,ChapterNode node,ChapterDifficulty difficulty=ChapterDifficulty.Normal,int tier=1)
    {ChapterRunReceipt receipt;Check(p.TryBeginChapterNode(node,difficulty,tier,out receipt),"begin valid chapter");for(int room=0;room<(node==ChapterNode.StarPlatform?1:2);room++)for(int i=0;i<(node==ChapterNode.StarPlatform?3:6);i++)Check(p.RegisterChapterEnemy(receipt,room,i,node==ChapterNode.StarPlatform&&i==0),"register chapter budget");return receipt;}
    public static string Run(string root)
    {
        var p=new ProgressionService(Path.Combine(root,"chapter-"+Guid.NewGuid().ToString("N")));Check(p.CreateNewSlot(HeroClass.Vanguard),"create");
        ChapterRunReceipt no;
        Check(p.Profile.version==1&&p.Profile.chapterCompletedMask==0&&!p.TryBeginChapterNode(ChapterNode.Redrock,ChapterDifficulty.Normal,5,out no),"new profile starts with only forest");
        Check(!p.TryBeginChapterNode(ChapterNode.ForestCourt,ChapterDifficulty.Hard,5,out no),"normal gates hard");
        Check(!p.TryBeginChapterNode(ChapterNode.ForestCourt,ChapterDifficulty.Normal,2,out no),"service rejects tier above shared authorization");
        var a=Begin(p,ChapterNode.ForestCourt);Check(!p.TryBeginChapterNode(ChapterNode.ForestCourt,ChapterDifficulty.Heroic,1,out no),"failed begin preserves current valid attempt");int initial=p.Profile.mechanicMaterials;string disk=File.ReadAllText(p.SaveFilePath);int events=0;p.Changed+=()=>events++;
        Directory.CreateDirectory(p.SaveFilePath+".tmp");
        Check(!p.TryCompleteChapterNode(a),"actual save failure rejects completion");
        Check(p.Profile.chapterCompletedMask==0&&p.Profile.mechanicMaterials==initial&&events==0&&File.ReadAllText(p.SaveFilePath)==disk,"failed save publishes no rewards unlock or changed event");
        Directory.Delete(p.SaveFilePath+".tmp");Check(p.TryCompleteChapterNode(a),"same receipt retries successfully");
        Check(p.Profile.mechanicMaterials==initial+2&&p.Profile.chapterFirstRewardMask==1&&p.Profile.highestAdventureTier==0,"first forest tier1 pays base1 plus fixed1 with no tier advancement");
        Check(!p.Profile.pendingFirstClearReward&&p.Load()&&!p.Profile.pendingFirstClearReward,"forest alone never enables chapter first-core eligibility");
        int paid=p.Profile.mechanicMaterials;Check(p.TryCompleteChapterNode(a)&&p.Profile.mechanicMaterials==paid,"duplicate receipt no payout");
        var b=Begin(p,ChapterNode.Redrock);Check(p.TryCompleteChapterNode(b)&&p.Profile.highestAdventureTier==0,"redrock only story progress");
        Check(!p.TryCompleteChapterNode(a),"old receipt after newer settlement is rejected");
        Check(!p.Profile.pendingFirstClearReward&&p.Load()&&!p.Profile.pendingFirstClearReward,"redrock alone never enables chapter first-core eligibility");
        var c=Begin(p,ChapterNode.StarPlatform);Check(p.TryCompleteChapterNode(c),"first star completes tier1");
        for(int tier=2;tier<=5;tier++){c=Begin(p,ChapterNode.StarPlatform,ChapterDifficulty.Normal,tier);Check(p.TryCompleteChapterNode(c),"chapter can advance only one authorized tier");}
        Check(p.TryCompleteChapterNode(c)&&p.Profile.highestAdventureTier==5&&p.Profile.pendingFirstClearReward&&!p.Profile.pendingFashionChest,"only star advances shared tier without legacy chest");
        Check(p.Load()&&p.Profile.pendingFirstClearReward&&!p.Profile.pendingFashionChest,"reload preserves chapter core eligibility without legacy chest");
        Check(p.TryCompleteChapterNode(c),"last committed receipt survives reload idempotently");
        Check(p.TryGrantModeReward(Guid.NewGuid().ToString("N"),0,0,0,1)&&p.Load()&&p.Profile.pendingFirstClearReward,"lower tier old mode still qualifies after chapter tier5 and reload");
        var hard=Begin(p,ChapterNode.ForestCourt,ChapterDifficulty.Hard);Check(hard.Materials==1&&p.TryCompleteChapterNode(hard),"hard has no material multiplier or repeated first reward");
        Check(ChapterProgression.CanEnter(p.Profile,ChapterNode.ForestCourt,ChapterDifficulty.Heroic)&&!ChapterProgression.CanEnter(p.Profile,ChapterNode.Redrock,ChapterDifficulty.Heroic),"difficulty unlock belongs to each node");
        var hero=Begin(p,ChapterNode.ForestCourt,ChapterDifficulty.Heroic);Check(hero.Materials==1&&p.TryCompleteChapterNode(hero)&&ChapterProgression.HighestCompletedDifficulty(p.Profile,ChapterNode.ForestCourt)==2,"hero records highest with same reward");
        var abandoned=Begin(p,ChapterNode.ForestCourt);p.CancelChapterRun();Check(!p.TryCompleteChapterNode(abandoned),"canceled callback cannot reward");
        var replaced=Begin(p,ChapterNode.ForestCourt);var current=Begin(p,ChapterNode.Redrock);Check(!p.TryCompleteChapterNode(replaced)&&p.TryCompleteChapterNode(current),"new attempt supersedes old callback");
        var preLoad=Begin(p,ChapterNode.ForestCourt);Check(p.Load()&&!p.TryCompleteChapterNode(preLoad),"reload invalidates unsettled callback");
        Check(!p.TryBeginChapterNode((ChapterNode)9,ChapterDifficulty.Normal,1,out no)&&!p.TryBeginChapterNode(ChapterNode.ForestCourt,(ChapterDifficulty)9,1,out no)&&!p.TryBeginChapterNode(ChapterNode.ForestCourt,ChapterDifficulty.Normal,101,out no),"invalid arguments rejected");
        var legacy=new ProgressionService(Path.Combine(root,"legacy-"+Guid.NewGuid().ToString("N")));legacy.CreateNewSlot(HeroClass.Ranger);legacy.Profile.highestAdventureTier=9;legacy.Save();
        var json=System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(legacy.SaveFilePath));
        var profile=json["profile"]??json;foreach(var key in new[]{"chapterMasteryMask","chapterMasteryTiers","chapterRevision","chapterCompletedMask","chapterFirstRewardMask","chapterHighestDifficulties","chapterRewardSequence","lastChapterRewardId","chapterHighestAdventureTier","chapterPriorAdventureTier"})profile.AsObject().Remove(key);
        File.WriteAllText(legacy.SaveFilePath,json.ToJsonString());
        Check(legacy.Load()&&legacy.Profile.version==1&&legacy.Profile.chapterCompletedMask==0&&ChapterProgression.HighestCompletedDifficulty(legacy.Profile,ChapterNode.ForestCourt)==-1&&legacy.Profile.pendingFirstClearReward,"missing chapter fields in old version1 do not infer progress or erase legacy qualification");
        Check(ChapterDefinition.RoomKind(ChapterNode.ForestCourt,0)==RoomObjective.Purify&&ChapterDefinition.RoomKind(ChapterNode.ForestCourt,1)==RoomObjective.Escape&&ChapterDefinition.RoomKind(ChapterNode.Redrock,0)==RoomObjective.Hunt&&ChapterDefinition.RoomKind(ChapterNode.StarPlatform,0)==RoomObjective.Boss&&ChapterDefinition.RoomCount(ChapterNode.StarPlatform)==1,"definition room sequence is fixed");
        Check(ChapterDefinition.HealthMultiplier(ChapterDifficulty.Normal)==1&&ChapterDefinition.HealthMultiplier(ChapterDifficulty.Hard)==1.2f&&ChapterDefinition.HealthMultiplier(ChapterDifficulty.Heroic)==1.35f&&ChapterDefinition.DamageMultiplier(ChapterDifficulty.Hard)==1.15f&&ChapterDefinition.DamageMultiplier(ChapterDifficulty.Heroic)==1.25f,"difficulty combat multipliers are bounded and explicit");
        for(int n=0;n<3;n++)for(int d=0;d<3;d++)Check(ChapterProgression.MaterialReward((ChapterNode)n,5)==(n==2?3:2),"difficulty independent tierband");
        var mastery=Begin(p,ChapterNode.ForestCourt,ChapterDifficulty.Hard,3);p.RecordChapterMastery(mastery,1);p.RecordChapterMastery(mastery,12);
        Check(p.Profile.chapterMasteryMask==0,"F evidence is attempt-local before successful save");
        Directory.CreateDirectory(p.SaveFilePath+".tmp");Check(!p.TryCompleteChapterNode(mastery)&&p.Profile.chapterMasteryMask==0,"F failed reward write grants no mastery");Directory.Delete(p.SaveFilePath+".tmp");
        Check(p.TryCompleteChapterNode(mastery)&&p.Profile.chapterMasteryMask==1&&p.Profile.chapterMasteryTiers[0]==3,"F successful reward transaction persists only node-scoped evidence and tier");
        Check(p.TryCompleteChapterNode(mastery)&&p.Load()&&p.Profile.chapterMasteryMask==1&&p.Profile.chapterMasteryTiers[0]==3,"F replay and reload are idempotent");
        var bad=Begin(p,ChapterNode.Redrock,ChapterDifficulty.Hard);p.RecordChapterMastery(bad,2);p.CancelChapterRun();Check(!p.TryCompleteChapterNode(bad)&&p.Profile.chapterMasteryMask==1,"F failed attempt never persists staged hunt evidence");
        var normal=Begin(p,ChapterNode.StarPlatform);p.RecordChapterMastery(normal,12);Check(p.TryCompleteChapterNode(normal)&&p.Profile.chapterMasteryMask==1,"F normal completion cannot grant optional hard mastery");
        var interrupt=Begin(p,ChapterNode.StarPlatform,ChapterDifficulty.Hard,2);p.RecordChapterMastery(interrupt,4);Check(p.TryCompleteChapterNode(interrupt)&&p.Profile.chapterMasteryMask==5,"F actual interrupt can persist before anchor achievement");
        var anchorRun=Begin(p,ChapterNode.StarPlatform,ChapterDifficulty.Heroic,4);p.RecordChapterMastery(anchorRun,8);Check(p.TryCompleteChapterNode(anchorRun)&&p.Profile.chapterMasteryMask==13&&p.Profile.chapterMasteryTiers[2]==2&&p.Profile.chapterMasteryTiers[3]==4,"F star two conditions combine across successful runs preserving each tier");
        return "PASS: "+checks+" actual chapter save/receipt/sequence/difficulty/reload assertions";
    }
}

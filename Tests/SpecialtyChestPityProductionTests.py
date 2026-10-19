"""Real reward selection, independent pity, durable claims and source pools."""
from pathlib import Path
import importlib.util, tempfile, subprocess, os
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
program=r'''using System;using System.IO;using System.Reflection;using Emberfall;
class Program{
sealed class FixedRoll:Random{readonly int value;public FixedRoll(int v){value=v;}public override int Next(int max){return max==100?value:0;}}
static int n;static void C(bool v,string m){n++;if(!v)throw new Exception(m);}
static void Qualify(ProgressionService p,bool chapter,int mode,int roll){p.AcknowledgeChestReward();p.Profile.pendingFashionChest=true;p.Profile.pendingAdventureChest=true;p.Profile.pendingChestChapterSource=chapter;p.Profile.pendingChestGemSource=false;p.Profile.pendingChestMode=mode;p.Profile.pendingChestTier=1;p.Profile.pendingChestRulesRevision=3;p.Profile.pendingChestQualificationId=Guid.NewGuid().ToString("N");p.Save();typeof(ProgressionService).GetField("random",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(p,new FixedRoll(roll));}
static void Main(){
C(AdventureRewardRules.DifficultyName(0,3)=="Lv30"&&AdventureRewardRules.DifficultyName(2,3)=="第 3 阶"&&AdventureRewardRules.DifficultyName(3,3)=="第 3 阶","difficulty labels follow reward type");C(AdventureRewardRules.MaximumSelection(0,10,9)==2&&AdventureRewardRules.MaximumSelection(2,10,9)==9&&AdventureRewardRules.MaximumSelection(3,10,9)==9,"level gate applies only to equipment dungeons");C(AdventureRewardRules.EntryEnemyLevel(0,3,16)==30&&AdventureRewardRules.EntryEnemyLevel(2,3,16)==20&&AdventureRewardRules.EntryEnemyLevel(3,8,16)==20,"tier content freezes character-level baseline");
var p=new ProgressionService(Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N")));C(p.CreateNewSlot(HeroClass.Ranger),"create");
var build=typeof(ProgressionService).GetMethod("BuildOneItemChestRoll",BindingFlags.NonPublic|BindingFlags.Static);
foreach(int mode in new[]{2,3}){p.Profile.pendingChestChapterSource=false;p.Profile.pendingChestMode=mode;for(int roll=0;roll<100;roll++){var draw=(ChestReward)build.Invoke(null,new object[]{p.Profile,roll,0,0,Guid.NewGuid().ToString("N")});C(draw.primaryKind!=1,"no equipment in arena/corridor for any random roll");}}
foreach(int source in new[]{0,1,2,3}){
bool chapter=source<2;int mode=source;int target=SpecialtyChestPity.Target(chapter,mode);int missRoll=source==0?99:0;
for(int i=1;i<=10;i++){
Qualify(p,chapter,mode,missRoll);C(p.OpenDungeonChest()!=null,"reward committed");C(p.Profile.lastChestReward.primaryKind==(i==10?target:p.Profile.lastChestReward.primaryKind),"tenth draw guaranteed specialty");C(SpecialtyChestPity.Misses(p.Profile,target,chapter)==(i==10?0:i),"counter increments only on miss and resets on hit");
C(p.LoadSlot(p.CurrentSlotId)&&SpecialtyChestPity.Misses(p.Profile,target,chapter)==(i==10?0:i),"pity survives reload");
} }
C(p.Profile.forestCrystalMisses==0&&p.Profile.redrockReforgeMisses==0&&p.Profile.arenaReforgeMisses==0&&p.Profile.corridorFashionMisses==0,"all independent counters reset");
p.Profile.redrockReforgeMisses=4;p.Profile.arenaReforgeMisses=7;p.Profile.forestCrystalMisses=9;
Qualify(p,true,0,99);int before=p.Profile.basicAttackMaterials;Directory.CreateDirectory(p.SaveFilePath+".tmp");C(p.OpenDungeonChest()==null&&p.Profile.forestCrystalMisses==9&&p.Profile.basicAttackMaterials==before,"save failure preserves pity and inventory");Directory.Delete(p.SaveFilePath+".tmp");C(p.OpenDungeonChest()!=null&&p.Profile.forestCrystalMisses==0&&p.Profile.basicAttackMaterials==before+1,"retry commits guaranteed crystal once");C(p.OpenDungeonChest()==null&&p.Profile.basicAttackMaterials==before+1,"claim cannot duplicate");C(p.Profile.redrockReforgeMisses==4&&p.Profile.arenaReforgeMisses==7,"forest does not change either reforge counter");
p.Profile.forestCrystalMisses=5;Qualify(p,true,0,0);C(p.OpenDungeonChest()!=null&&p.Profile.forestCrystalMisses==0,"early natural reward resets pity");
C(SpecialtyChestPity.Caption(p.Profile,7,true).Contains("6")&&SpecialtyChestPity.Caption(p.Profile,7,false).Contains("3"),"UI selects the correct source counter");
Directory.Delete(p.SaveDirectory,true);Console.WriteLine("PASS "+n+" specialty chest and transaction assertions");}}
'''
with tempfile.TemporaryDirectory(prefix='specialty-chest-') as tmp:
 p=Path(tmp);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 names=['SkillCastingPolicy','ProgressionService.AchievementRewards','SkillRuntime','GameTypes','ProgressionService','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Trading','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow']
 sources=[root/('Assets/Scripts/Core/'+name+'.cs') for name in names]+[root/'Tests/ProgressionTests.cs',root/'Assets/Scripts/UI/CollectionPreviewState.cs']
 project=cv.write_project(p/'project',sources,program)
 subprocess.run(['/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet','run','--project',str(project)],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',MSBuildEnableWorkloadResolver='false'))

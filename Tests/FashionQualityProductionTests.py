"""Actual supply, fashion transactions and preview invalidation against the save service."""
from pathlib import Path
import importlib.util, tempfile, subprocess, os, sys
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
program=r'''using System;using System.IO;using System.Reflection;using Emberfall;
class Program {
static int checks;static void C(bool b,string message){checks++;if(!b)throw new Exception(message);}
static object Call(string name,params object[] args)=>typeof(ProgressionService).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,args);
static void Main(string[] args){
var p=new ProgressionService(args[0]);C(p.CreateNewSlot(HeroClass.Arcanist),"create");
p.Profile.fashionQualityRevision=1;
foreach(FashionSlot slot in Enum.GetValues(typeof(FashionSlot)))for(int q=0;q<4;q++)p.Profile.fashions.Add(new FashionData{id="fashion-"+(int)slot+"-"+q,slot=slot,rarity=Rarity.Legendary,appearanceTier=q,upgradeRank=2});
p.Profile.wingsFashionId="fashion-0-0";p.Profile.weaponFashionId="fashion-1-2";
p.Save();C(p.LoadSlot(p.CurrentSlotId),"reload migration");
C(p.Profile.fashionQualityRevision==2,"quality revision persisted");C(p.Profile.fashions.Count==8,"all appearances retained");
foreach(var f in p.Profile.fashions){C((int)f.rarity==f.appearanceTier,"quality follows collected appearance");C(f.upgradeRank==2,"rank retained");C(f.VisualRarity==f.rarity,"upgrades preserve quality silhouette");C(!ProgressionService.FashionBonus(f).Contains("×"),"no multiplicative crit description");}
C(p.Profile.wingsFashionId=="fashion-0-0"&&p.Profile.weaponFashionId=="fashion-1-2","worn identity preserved");
C(p.LoadSlot(p.CurrentSlotId)&&p.Profile.fashions.Count==8,"migration idempotent");
int[] health={3,5,8,12},crit={2,4,6,9};
for(int q=0;q<4;q++){
 var wings=new FashionData{slot=FashionSlot.Wings,rarity=(Rarity)q};var weapon=new FashionData{slot=FashionSlot.Weapon,rarity=(Rarity)q};
 C(ProgressionService.FashionBaseStat(wings,"生命")==health[q],"four wing tiers");C(ProgressionService.FashionBaseStat(weapon,"暴击率")==crit[q],"four critical tiers");
 p.Profile.fashions.Clear();float basis=p.GetStats().CritChance;p.Profile.fashions.Add(weapon);
 C(Math.Abs(p.GetStats().CritChance-basis-crit[q]/100f)<.0001f,"critical chance adds percentage points");
 weapon.upgradeRank=2;C(Math.Abs(p.GetStats().CritChance-basis-(crit[q]+6)/100f)<.0001f,"rank adds critical points");
}
var profile=new GameProfile{pendingChestMode=3,pendingChestTier=1};int[] counts=new int[4];
for(int roll=0;roll<=40;roll++){
 var r=(ChestReward)Call("BuildOneItemChestRoll",profile,75,0,roll,Guid.NewGuid().ToString("N"));
 C(r.primaryKind==2,"fashion chest");C((bool)Call("ValidOneItemChest",r),"four tier receipt accepted");counts[r.rarityIndex]++;
 var preview=(ChestContentPreview)Call("ChestContent",profile,r);C((int)preview.fashion.rarity==r.rarityIndex&&preview.fashion.appearanceTier==r.rarityIndex,"preview matches receipt");
}
foreach(int count in counts)C(count>0,"each tier obtainable");
int total=0;for(int roll=0;roll<100;roll++)if(ProgressionService.RollFashionRarity(roll).HasValue)total++;C(total==40,"legacy chest overall rate retained");
Console.WriteLine("PASS "+checks+" fashion quality and critical-rate checks");
}}
'''
with tempfile.TemporaryDirectory(prefix='fashion-quality-') as tmp:
 p=Path(tmp);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 names=['SkillCastingPolicy','ProgressionService.AchievementRewards','SkillRuntime','GameTypes','ProgressionService','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Trading','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow']
 sources=[root/('Assets/Scripts/Core/'+name+'.cs') for name in names]+[root/'Tests/ProgressionTests.cs',root/'Assets/Scripts/UI/CollectionPreviewState.cs']
 project=cv.write_project(p/'project',sources,program)
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else '/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet','run','--project',str(project),'--',str(p/'saves')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))

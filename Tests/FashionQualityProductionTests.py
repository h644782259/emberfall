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
float[] health={4.5f,7.5f,12,18},crit={3,6,9,13.5f};
for(int q=0;q<4;q++){
 var wings=new FashionData{slot=FashionSlot.Wings,rarity=(Rarity)q};var weapon=new FashionData{slot=FashionSlot.Weapon,rarity=(Rarity)q};
 C(ProgressionService.FashionBaseStat(wings,"生命")==health[q],"four wing tiers");C(ProgressionService.FashionBaseStat(weapon,"暴击率")==crit[q],"four critical tiers");
 p.Profile.fashions.Clear();float basis=p.GetStats().CritChance;p.Profile.fashions.Add(weapon);
 C(Math.Abs(p.GetStats().CritChance-basis-crit[q]/100f)<.0001f,"critical chance adds percentage points");
 weapon.upgradeRank=2;C(Math.Abs(p.GetStats().CritChance-basis-(crit[q]+9)/100f)<.0001f,"rank adds critical points");
}
var profile=new GameProfile{pendingChestMode=3,pendingChestTier=1};int[] counts=new int[4];int materials=0;
for(int roll=0;roll<100;roll++){
 var r=(ChestReward)Call("BuildOneItemChestRoll",profile,roll,0,17,Guid.NewGuid().ToString("N"));
 C((bool)Call("ValidOneItemChest",r),"receipt accepted");
 if(r.primaryKind!=2){materials++;continue;}counts[r.rarityIndex]++;
 var preview=(ChestContentPreview)Call("ChestContent",profile,r);C((int)preview.fashion.rarity==r.rarityIndex&&preview.fashion.appearanceTier==r.rarityIndex,"preview matches receipt");
}
int[] expected={27,18,12,3};for(int q=0;q<4;q++)C(counts[q]==expected[q],"exact ordinary quality probability");C(materials==40,"remaining material probability");
profile.corridorFashionMisses=9;counts=new int[4];
for(int roll=0;roll<100;roll++){
 var r=(ChestReward)Call("BuildOneItemChestRoll",profile,roll,1,0,Guid.NewGuid().ToString("N"));
 C(r.primaryKind==2&&r.rarityIndex>=2,"pity always epic or legendary");counts[r.rarityIndex]++;
 C((bool)Call("ValidOneItemChest",r),"pity receipt accepted");
}
C(counts[2]==80&&counts[3]==20,"pity quality probability");SpecialtyChestPity.Record(profile,2,Rarity.Epic);C(profile.corridorFashionMisses==0,"epic fashion resets pity");
SpecialtyChestPity.Record(profile,2,Rarity.Common);C(profile.corridorFashionMisses==1,"common continues pity");
SpecialtyChestPity.Record(profile,2,Rarity.Rare);C(profile.corridorFashionMisses==2,"rare continues pity");
SpecialtyChestPity.Record(profile,2,Rarity.Legendary);C(profile.corridorFashionMisses==0,"legendary resets pity");
for(int i=0;i<9;i++)SpecialtyChestPity.Record(profile,3);C(profile.corridorFashionMisses==9,"nine misses enable pity");
int total=0;for(int roll=0;roll<100;roll++)if(ProgressionService.RollFashionRarity(roll).HasValue)total++;C(total==60,"shared fashion rate sixty percent");
float[][] oldBase={new float[]{3,5,8,12},new float[]{2,3,5,8},new float[]{2,4,6,8},new float[]{2,4,6,9},new float[]{2,4,6,9}};
string[] labels={"生命","防御","移动速度","攻击","暴击率"};float[] oldRank={5,4,1,5,3};
for(int q=0;q<4;q++)for(int rank=0;rank<=3;rank++){
 p.Profile.fashions.Clear();var baseline=p.GetStats();
 var w=new FashionData{slot=FashionSlot.Wings,rarity=(Rarity)q,upgradeRank=rank};var f=new FashionData{slot=FashionSlot.Weapon,rarity=(Rarity)q,upgradeRank=rank};
 p.Profile.fashions.Add(w);p.Profile.fashions.Add(f);var actual=p.GetStats();
 foreach(var item in new[]{w,f})foreach(string row in ProgressionService.FashionBonus(item).Split(new[]{" · "},StringSplitOptions.RemoveEmptyEntries)){
 int split=row.LastIndexOf(' ');string stat=row.Substring(0,split);float displayedTotal=float.Parse(row.Substring(split+1).TrimStart('+').TrimEnd('%'));
 C(displayedTotal==ProgressionService.FashionBaseStat(item,stat)+ProgressionService.FashionRankStat(item,stat),"display matches base plus rank with fractional precision");}

 for(int i=0;i<5;i++){var item=i<3?w:f;C(ProgressionService.FashionBaseStat(item,labels[i])==oldBase[i][q]*1.5f,"base increased fifty percent");C(ProgressionService.FashionRankStat(item,labels[i])==oldRank[i]*rank*1.5f,"rank increased fifty percent");}
 C(Math.Abs(actual.MaxHealth/baseline.MaxHealth-1-(oldBase[0][q]+5*rank)*.015f)<.0001f,"live health");
 C(Math.Abs(actual.Armor/baseline.Armor-1-(oldBase[1][q]+4*rank)*.015f)<.0001f,"live armor");
 C(Math.Abs(actual.MoveSpeed/baseline.MoveSpeed-1-(oldBase[2][q]+rank)*.015f)<.0001f,"live speed");
 C(Math.Abs(actual.Damage/baseline.Damage-1-(oldBase[3][q]+5*rank)*.015f)<.0001f,"live attack");
 C(Math.Abs(actual.CritChance-Math.Min(1,baseline.CritChance+(oldBase[4][q]+3*rank)*.015f))<.0001f,"live crit");
 C(Math.Abs(actual.DamageReduction-(1-(1-baseline.DamageReduction)*(1-.03f*Math.Max(0,rank-1))))<.0001f,"live reduction");
 C(Math.Abs(actual.CritDamageBonus-baseline.CritDamageBonus-.075f*Math.Max(0,rank-1))<.0001f,"live crit damage");
}
Console.WriteLine("PASS "+checks+" fashion quality and critical-rate checks");
}}
'''
with tempfile.TemporaryDirectory(prefix='fashion-quality-') as tmp:
 p=Path(tmp);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 names=['SkillCastingPolicy','ProgressionService.AchievementRewards','SkillRuntime','GameTypes','ProgressionService','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Trading','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow']
 sources=[root/('Assets/Scripts/Core/'+name+'.cs') for name in names]+[root/'Tests/ProgressionTests.cs',root/'Assets/Scripts/UI/CollectionPreviewState.cs']
 project=cv.write_project(p/'project',sources,program)
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else '/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet','run','--project',str(project),'--',str(p/'saves')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))

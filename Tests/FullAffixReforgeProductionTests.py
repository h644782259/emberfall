"""Full-affix reforge lock and transactional save regression checks."""
from pathlib import Path
import importlib.util, tempfile, subprocess, os, sys
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
program=r'''using System;using System.IO;using Emberfall;
class Program{static int n;static void C(bool v,string m){n++;if(!v)throw new Exception(m);}static void Main(){
var p=new ProgressionService(Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N")));C(p.CreateNewSlot(HeroClass.Arcanist),"create");
var item=p.Profile.inventory[0];item.rarity=Rarity.Legendary;item.criticalChance=.03f;item.criticalDamageBonus=.12f;item.attackPercent=.08f;p.Profile.affixReforgeStones=20;p.Save();string id=item.id;
int count=p.Profile.affixReforgeCount;var fingerprint=p.BuildStateFingerprint();
C(ProgressionService.EquipmentRandomAffixCount(item)==3&&ProgressionService.HasFullEquipmentAffixes(item),"full count");
C(p.AffixReforgeLockReason(id,true)=="满词条"&&p.PrepareAffixReforge(id,true)==null,"no full quote");
C(!p.ReforgeAffixes(id,true)&&p.Profile.affixReforgeStones==20&&p.Profile.affixReforgeCount==count&&p.BuildStateFingerprint()==fingerprint,"full direct call cannot spend or mutate");
item.attackPercent=0;p.Save();C(!ProgressionService.HasFullEquipmentAffixes(item)&&p.PrepareAffixReforge(id,true)!=null,"two affixes still reforge");
var quote=p.PrepareAffixReforge(id,true);item.attackPercent=.08f;p.Save();C(!p.ApplyAffixReforge(quote,true)&&p.Profile.affixReforgeStones==20,"stale quote cannot reroll full gear");
item.attackPercent=0;item.criticalDamageBonus=0;p.Save();quote=p.PrepareAffixReforge(id,true);
Directory.CreateDirectory(p.SaveFilePath+".tmp");C(!p.ApplyAffixReforge(quote,true)&&p.Profile.affixReforgeStones==20,"save failure spends nothing");Directory.Delete(p.SaveFilePath+".tmp");
C(p.ApplyAffixReforge(quote,true)&&p.Profile.affixReforgeStones==19&&p.Profile.affixReforgeCount==count+1,"success spends once");
item=p.Profile.inventory.Find(x=>x.id==id);C(ProgressionService.EquipmentRandomAffixCount(item)>=1&&ProgressionService.EquipmentRandomAffixCount(item)<=3,"saved affix result");
p.Profile.gold=10000;p.Profile.potions=0;p.Profile.refinementStones=0;
C(ProgressionService.PotionPrice==2000&&ProgressionService.RefinementStonePrice==2000,"unit prices");
var potion=p.PrepareMerchantPurchase(EquipmentMechanic.None,true,Rarity.Common,3);
C(potion!=null&&p.BuyAtMerchant(potion,true)&&p.Profile.gold==4000&&p.Profile.potions==3,"three potions cost 6000");
var stones=p.PrepareRefinementPurchase(true,2);
C(stones!=null&&p.BuyAtMerchant(stones,true)&&p.Profile.gold==0&&p.Profile.refinementStones==2,"two refinement stones cost 4000");
p.Profile.gold=1999;C(p.PrepareRefinementPurchase(true,1)==null&&p.PrepareMerchantPurchase(EquipmentMechanic.None,true,Rarity.Common,1)==null,"cannot buy below 2000");
p.Profile.level=100;p.Profile.masteryRanks=new[]{35,0,29,35};p.Profile.masteryCore=-1;p.Save();
int points=p.Profile.skillPoints;C(p.SelectMasteryCore(MasteryType.Offense,false)&&p.HasMasteryCore(MasteryType.Offense)&&p.Profile.skillPoints==points,"first core enabled outside camp without spending points");
C(!p.SelectMasteryCore(MasteryType.Guard,false)&&p.HasMasteryCore(MasteryType.Offense),"existing core switching stays camp gated");
C(p.SelectMasteryCore(MasteryType.Guard,true)&&p.HasMasteryCore(MasteryType.Guard),"camp core switch");
C(!p.SelectMasteryCore(MasteryType.Vitality,true),"underinvested core remains locked");
Directory.Delete(p.SaveDirectory,true);Console.WriteLine("PASS "+n+" full-affix lock and transaction assertions");}}
'''
with tempfile.TemporaryDirectory(prefix='fashion-refinement-') as tmp:
 p=Path(tmp);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 names=['SkillCastingPolicy','ProgressionService.AchievementRewards','SkillRuntime','GameTypes','ProgressionService','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Trading','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow']
 sources=[root/('Assets/Scripts/Core/'+name+'.cs') for name in names]+[root/'Tests/ProgressionTests.cs',root/'Assets/Scripts/UI/CollectionPreviewState.cs']
 project=cv.write_project(p/'project',sources,program)
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else '/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet','run','--project',str(project),'--',str(p/'saves')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))

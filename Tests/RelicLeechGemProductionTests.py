"""New relic leech gem and legacy vitality preservation."""
from pathlib import Path
import importlib.util, tempfile, subprocess, os, sys
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
program=r'''using System;using System.IO;using System.Linq;using Emberfall;
class Program{static int n;static void C(bool v,string s){n++;if(!v)throw new Exception(s);}static void Main(){
C((int)EquipmentMechanic.RelicVitality==19&&(int)EquipmentMechanic.RelicWard==20,"legacy enum ids stable");
foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass))){var pool=ProgressionService.GemBagPool(hero,ItemSlot.Relic);C(pool.Contains(EquipmentMechanic.RelicLeech)&&!pool.Contains(EquipmentMechanic.RelicVitality)&&pool.Length==5,"new relic pool replaces life gem");C(BuildCatalog.GemsFor(hero).Contains(EquipmentMechanic.RelicVitality),"legacy remains inspectable and upgradeable");}
C(Math.Abs(BuildCatalog.GemAttributeValue(EquipmentMechanic.RelicLeech,Rarity.Legendary,0)-.01f)<.000001f,"legendary base leech one percent");
C(Math.Abs(BuildCatalog.GemAttributeValue(EquipmentMechanic.RelicVitality,Rarity.Legendary,0)-.2f)<.000001f,"legacy life twenty percent");
var p=new ProgressionService(Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N")));p.NewGame(HeroClass.Ranger);p.Profile.attachments.Clear();float hp=p.GetStats().MaxHealth;
p.Profile.attachments.Add(new MechanicAttachment{id="old-life",mechanic=EquipmentMechanic.RelicVitality,rarity=Rarity.Legendary,mounted=true,ascensionRank=0});p.Save();
C(p.Load()&&p.Attachment(EquipmentMechanic.RelicVitality)!=null&&Math.Abs(p.GetStats().MaxHealth-hp*1.2f)<.01f&&p.GetStats().LifeSteal==0,"old save preserves identity health and no leech");
p.Attachment(EquipmentMechanic.RelicVitality).mounted=false;p.Profile.attachments.Add(new MechanicAttachment{id="new-leech",mechanic=EquipmentMechanic.RelicLeech,rarity=Rarity.Legendary,mounted=true,ascensionRank=0});p.Save();
C(p.Load()&&p.Attachment(EquipmentMechanic.RelicLeech)!=null&&Math.Abs(p.GetStats().LifeSteal-.01f)<.000001f&&Math.Abs(p.GetStats().MaxHealth-hp)<.01f,"new gem survives save and grants leech without health");
p.Attachment(EquipmentMechanic.RelicVitality).mounted=true;p.Save();C(Math.Abs(p.GetStats().MaxHealth-hp*1.2f)<.01f&&p.GetStats().LifeSteal>0,"old and new coexist");
Directory.Delete(p.SaveDirectory,true);Console.WriteLine("PASS "+n+" leech gem migration, pool and attribute assertions");}}
'''
with tempfile.TemporaryDirectory(prefix='fashion-refinement-') as tmp:
 p=Path(tmp);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 names=['ProgressionService.GemBags','SkillCastingPolicy','ProgressionService.AchievementRewards','SkillRuntime','GameTypes','ProgressionService','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Trading','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow']
 sources=[root/('Assets/Scripts/Core/'+name+'.cs') for name in names]+[root/'Tests/ProgressionTests.cs',root/'Assets/Scripts/UI/CollectionPreviewState.cs']
 project=cv.write_project(p/'project',sources,program)
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else '/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet','run','--project',str(project),'--',str(p/'saves')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))

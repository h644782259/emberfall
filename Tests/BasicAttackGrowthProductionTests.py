"""Basic attack progression, persistence and fixed chapter loot regression checks."""
from pathlib import Path
import importlib.util, tempfile, subprocess, os, sys
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
program=r'''using System;using System.IO;using System.Reflection;using Emberfall;
class Program{static int n;static void C(bool v,string why){n++;if(!v)throw new Exception(why);}static void Main(){
var p=new ProgressionService(Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N")));C(p.CreateNewSlot(HeroClass.Arcanist),"create");
C(BasicAttackGrowth.Tier(p.Profile,HeroClass.Arcanist)==1,"old/new default tier");
C(!p.UpgradeBasicAttack(),"no free upgrade");p.Profile.basicAttackMaterials=7;p.Save();
Directory.CreateDirectory(p.SaveFilePath+".tmp");C(!p.UpgradeBasicAttack()&&p.Profile.basicAttackMaterials==7&&BasicAttackGrowth.Tier(p.Profile,HeroClass.Arcanist)==1,"failed save rolls back");Directory.Delete(p.SaveFilePath+".tmp");
foreach(int balance in new[]{6,4,0})C(p.UpgradeBasicAttack()&&p.Profile.basicAttackMaterials==balance,"cost 1 2 4");
C(BasicAttackGrowth.Tier(p.Profile,HeroClass.Arcanist)==4&&!p.UpgradeBasicAttack(),"cap");
C(BasicAttackGrowth.Tier(p.Profile,HeroClass.Ranger)==1,"class independence");
var q=new ProgressionService(p.SaveDirectory);C(q.LoadSlot(p.CurrentSlotId)&&BasicAttackGrowth.Tier(q.Profile,HeroClass.Arcanist)==4,"reload persistence");
var build=typeof(ProgressionService).GetMethod("BuildOneItemChestRoll",BindingFlags.NonPublic|BindingFlags.Static);
p.Profile.pendingChestChapterSource=true;p.Profile.pendingChestMode=0;p.Profile.pendingChestTier=1;
for(int roll=0;roll<100;roll++){var chest=(ChestReward)build.Invoke(null,new object[]{p.Profile,roll,0,0,Guid.NewGuid().ToString("N")});C(chest.primaryKind==8&&chest.primaryCount==1&&chest.primaryRarity==Rarity.Legendary,"all rolls fixed legendary material");}
p.Profile.pendingFashionChest=true;p.Profile.pendingChestRulesRevision=3;p.Save();int items=p.Profile.inventory.Count;
C(p.OpenDungeonChest()!=null&&p.Profile.basicAttackMaterials==1&&p.Profile.inventory.Count==items,"chest one material no armor");C(p.Profile.lastChestReward.basicAttackMaterialsDelta==1,"receipt");
C(p.OpenDungeonChest()==null&&p.Profile.basicAttackMaterials==1,"cannot duplicate reward");
for(int t=1;t<=4;t++)C(BasicAttackGrowth.Shots(t)==new[]{1,2,4,8}[t-1],"shot tiers");
for(int t=1;t<=4;t++){
var damageBudget=new BasicAttackHitBudget(t);var enemy=new object();float total=0;
for(int hit=0;hit<BasicAttackGrowth.Shots(t);hit++)total+=BasicAttackGrowth.ProjectileMultiplier(t)*damageBudget.ClaimDamageScale(enemy);
C(Math.Abs(total-new[]{1f,1.2f,1.4f,1.6f}[t-1])<.0001f,"same target damage cap");
if(t>=3){
C(damageBudget.ClaimDamageScale(enemy)<.00001f,"extra hits cannot exceed cap");
C(Math.Abs(damageBudget.ClaimDamageScale(new object())-1f)<.00001f,"other targets retain full projectile damage");
C(BasicAttackGrowth.ProjectileMultiplier(t)>=BasicAttackGrowth.TargetMultiplier(2),"one fan hit at least equals both tier two shots");
C(Math.Abs(new BasicAttackHitBudget(t).ClaimDamageScale(enemy)-1f)<.00001f,"next attack resets target damage budget");}
var budget=new BasicAttackHitBudget(t);int claims=0;for(int hit=0;hit<BasicAttackGrowth.Shots(t);hit++)if(budget.TryClaim())claims++;
C(claims==1&&!budget.TryClaim(),"shared first hit only including delayed hit");
C(new BasicAttackHitBudget().TryClaim(),"next attack independent");}
C(BasicAttackGrowth.Range(4)>BasicAttackGrowth.Range(1)&&BasicAttackGrowth.Angle(4)>BasicAttackGrowth.Angle(1),"sword range grows");
Directory.Delete(p.SaveDirectory,true);Console.WriteLine("PASS "+n+" basic attack progression assertions");}}
'''
with tempfile.TemporaryDirectory(prefix='fashion-refinement-') as tmp:
 p=Path(tmp);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 names=['SkillCastingPolicy','ProgressionService.AchievementRewards','SkillRuntime','GameTypes','ProgressionService','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Trading','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow']
 sources=[root/('Assets/Scripts/Core/'+name+'.cs') for name in names]+[root/'Tests/ProgressionTests.cs',root/'Assets/Scripts/UI/CollectionPreviewState.cs']
 project=cv.write_project(p/'project',sources,program)
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else '/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet','run','--project',str(project),'--',str(p/'saves')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',MSBuildEnableWorkloadResolver='false'))

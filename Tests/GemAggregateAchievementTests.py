"""Verify legacy saves and APIs converge to the fixed class defaults."""
from pathlib import Path
import importlib.util, tempfile, subprocess, os, sys
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
program=r'''using System;using System.Linq;using Emberfall;
class Program{static int n;static void C(bool b,string m){n++;if(!b)throw new Exception(m);}static void Main(string[] args){
var p=new ProgressionService(args[0]);C(p.CreateNewSlot(HeroClass.Arcanist),"create");
var goals=ProgressionService.Achievements.Where(a=>a.Id.StartsWith("gem-goal/")).ToArray();C(goals.Length==27,"27 generic goals");C(goals.All(a=>a.Target<=3),"wearable limit");C(!ProgressionService.Achievements.Any(a=>a.Id.StartsWith("upgrade/")||a.Id.StartsWith("core/")||a.Id.StartsWith("ascend/")||a.Id.StartsWith("variant/")),"no specific gem goals");
var mechanics=BuildCatalog.GemsFor(HeroClass.Vanguard);p.Profile.attachments.Clear();
for(int i=0;i<3;i++){p.Profile.attachments.Add(new MechanicAttachment{mechanic=mechanics[i],upgradeRank=6,ascensionRank=2,variantUnlocked=true});foreach(var a in goals){int expected=a.Id.StartsWith("gem-goal/upgrade/9/")||a.Id.StartsWith("gem-goal/ascend/3/")?0:i+1;C(a.Progress(p.Profile)==expected,"any gems count by threshold");}}
p.Profile.attachments.Clear();p.Profile.achievementReceipts.Clear();p.Profile.achievementReceipts.Add("upgrade/"+(int)mechanics[0]+"/6");p.Profile.achievementReceipts.Add("upgrade/"+(int)mechanics[0]+"/3");
C(p.AchievementClaimed("gem-goal/upgrade/6/1"),"legacy receipt prevents duplicate reward");C(!p.AchievementClaimed("gem-goal/upgrade/6/2"),"same gem only counts once");
p.Profile.achievementReceipts.Add("upgrade/"+(int)mechanics[1]+"/6");C(p.AchievementClaimed("gem-goal/upgrade/6/2"),"second legacy gem");C(!p.AchievementClaimed("gem-goal/upgrade/9/1"),"higher goal stays available");
p.Save();C(p.LoadSlot(p.CurrentSlotId),"reload legacy receipts");C(p.AchievementClaimed("gem-goal/upgrade/6/2"),"legacy claim persists after reload");
Console.WriteLine("PASS "+n+" aggregate gem achievement checks");}}
'''
with tempfile.TemporaryDirectory(prefix='specialization-default-') as tmp:
 p=Path(tmp);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 names=['SkillCastingPolicy','ProgressionService.AchievementRewards','SkillRuntime','GameTypes','ProgressionService','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Trading','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow']
 sources=[root/('Assets/Scripts/Core/'+name+'.cs') for name in names]+[root/'Tests/ProgressionTests.cs',root/'Assets/Scripts/UI/CollectionPreviewState.cs']
 project=cv.write_project(p/'project',sources,program)
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else '/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet','run','--project',str(project),'--',str(p/'saves')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))

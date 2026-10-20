"""Verify legacy saves and APIs converge to the fixed class defaults."""
from pathlib import Path
import importlib.util, tempfile, subprocess, os, sys
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
program=r'''using System;using System.Reflection;using Emberfall;
class Program {static int count;static void C(bool b,string m){count++;if(!b)throw new Exception(m);}static void Main(string[] args){
foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass))){
var p=new ProgressionService(args[0]);C(p.CreateNewSlot(hero),"create class");
p.Profile.specialization=ElementalistSpecialization.Burn;p.Profile.martialSpecialization=MartialSpecialization.Survival;p.Profile.summonerRoute=SummonerRoute.Pack;
p.Save();C(p.LoadSlot(p.CurrentSlotId),"reload legacy selection");
C(p.Profile.specialization==ElementalistSpecialization.None,"default elemental");C(p.Profile.martialSpecialization==MartialSpecialization.Balanced,"default martial");C(p.Profile.summonerRoute==SummonerRoute.Bonded,"default summoner");
if(hero==HeroClass.Arcanist){C(p.SetSpecialization(ElementalistSpecialization.Shatter,true),"legacy setter");C(p.Profile.specialization==ElementalistSpecialization.None,"setter stays default");}
if(hero==HeroClass.Summoner){C(p.SetSummonerRoute(SummonerRoute.Pack,true),"legacy setter");C(p.Profile.summonerRoute==SummonerRoute.Bonded,"setter stays default");}
if(hero==HeroClass.Vanguard||hero==HeroClass.Ranger){C(p.SetMartialSpecialization(MartialSpecialization.Assault,true),"legacy setter");C(p.Profile.martialSpecialization==MartialSpecialization.Balanced,"setter stays default");}
}Console.WriteLine("PASS "+count+" disabled specialization checks");}}
'''
with tempfile.TemporaryDirectory(prefix='specialization-default-') as tmp:
 p=Path(tmp);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 names=['SkillCastingPolicy','ProgressionService.AchievementRewards','SkillRuntime','GameTypes','ProgressionService','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Trading','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow']
 sources=[root/('Assets/Scripts/Core/'+name+'.cs') for name in names]+[root/'Tests/ProgressionTests.cs',root/'Assets/Scripts/UI/CollectionPreviewState.cs']
 project=cv.write_project(p/'project',sources,program)
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else '/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet','run','--project',str(project),'--',str(p/'saves')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))

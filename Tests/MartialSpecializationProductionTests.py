"""Four-class specialization effects, persistence, switching and failed-save regression."""
from pathlib import Path
import importlib.util, tempfile, subprocess, os, sys
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
program=r'''using System;using System.IO;using Emberfall;
class Program{static int n;static void C(bool v,string m){n++;if(!v)throw new Exception(m);}static bool Near(float a,float b)=>Math.Abs(a-b)<.001f;
static void Switch(ProgressionService p,HeroClass hero){var t=p.PrepareClassSwitch(hero,true);C(t!=null&&p.CommitClassSwitch(t,true),"class switch");p.PublishClassSwitch(t);}
static void Main(){var p=new ProgressionService(Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N")));C(p.CreateNewSlot(HeroClass.Vanguard),"new");C(p.Profile.martialSpecialization==MartialSpecialization.Balanced,"old/default profile balanced");
var basis=p.GetStats();C(p.SetMartialSpecialization(MartialSpecialization.Assault,false),"vanguard can choose");var attack=p.GetStats();C(Near(attack.Damage,basis.Damage*1.15f)&&Near(attack.Armor,basis.Armor*.9f),"vanguard assault effects");
C(p.SetMartialSpecialization(MartialSpecialization.Survival,true),"guard choice");var guard=p.GetStats();C(Near(guard.Damage,basis.Damage*.92f)&&Near(guard.Armor,basis.Armor*1.25f)&&Near(guard.MaxHealth,basis.MaxHealth*1.1f),"guard effects");
C(p.LoadSlot(p.CurrentSlotId)&&p.Profile.martialSpecialization==MartialSpecialization.Survival,"saved choice");
Switch(p,HeroClass.Ranger);C(p.Profile.martialSpecialization==MartialSpecialization.Balanced,"new class default independent");basis=p.GetStats();C(p.SetMartialSpecialization(MartialSpecialization.Assault,true),"ranger precision");attack=p.GetStats();C(Near(attack.CritChance,basis.CritChance+.08f)&&Near(attack.CritDamageBonus,basis.CritDamageBonus+.2f)&&Near(attack.MoveSpeed,basis.MoveSpeed*.95f),"precision effects");
C(p.SetMartialSpecialization(MartialSpecialization.Survival,false),"ranger wind");guard=p.GetStats();C(Near(guard.MoveSpeed,basis.MoveSpeed*1.1f)&&Near(guard.CooldownReduction,basis.CooldownReduction+.1f)&&Near(guard.Damage,basis.Damage*.92f),"wind effects");
C(!p.SetMartialSpecialization((MartialSpecialization)99,true),"invalid choice rejected");
Directory.CreateDirectory(p.SaveFilePath+".tmp");C(!p.SetMartialSpecialization(MartialSpecialization.Balanced,true)&&p.Profile.martialSpecialization==MartialSpecialization.Survival,"failed save retains choice");Directory.Delete(p.SaveFilePath+".tmp");
Switch(p,HeroClass.Vanguard);C(p.Profile.martialSpecialization==MartialSpecialization.Survival,"vanguard restored");C(p.ResetMastery(true)&&p.Profile.martialSpecialization==MartialSpecialization.Balanced,"mastery reset");
Switch(p,HeroClass.Ranger);C(p.Profile.martialSpecialization==MartialSpecialization.Survival,"ranger retained across other class reset");
Switch(p,HeroClass.Arcanist);C(!p.SetMartialSpecialization(MartialSpecialization.Assault,true)&&p.SetSpecialization(ElementalistSpecialization.Burn,true),"elemental existing choice");
Switch(p,HeroClass.Summoner);C(!p.SetMartialSpecialization(MartialSpecialization.Assault,true)&&p.SetSummonerRoute(SummonerRoute.Pack,true),"summoner existing choice");
Directory.Delete(p.SaveDirectory,true);Console.WriteLine("PASS "+n+" specialization persistence and effects assertions");}}
'''
with tempfile.TemporaryDirectory(prefix='fashion-refinement-') as tmp:
 p=Path(tmp);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 names=['SkillCastingPolicy','ProgressionService.AchievementRewards','SkillRuntime','GameTypes','ProgressionService','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Trading','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow']
 sources=[root/('Assets/Scripts/Core/'+name+'.cs') for name in names]+[root/'Tests/ProgressionTests.cs',root/'Assets/Scripts/UI/CollectionPreviewState.cs']
 project=cv.write_project(p/'project',sources,program)
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else '/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet','run','--project',str(project),'--',str(p/'saves')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))

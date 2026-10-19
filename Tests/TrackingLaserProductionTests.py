"""Tracking laser legacy ranks, bindings and runtime resource regression."""
from pathlib import Path
import importlib.util, tempfile, subprocess, os, sys
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
program=r'''using System;using System.IO;using System.Reflection;using Emberfall;
class Program{static int n;static void C(bool v,string m){n++;if(!v)throw new Exception(m);}
static void Normalize(GameProfile p){typeof(ProgressionService).GetMethod("ValidateProfile",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{p});}
static void Main(){
var p=new ProgressionService(Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N")));C(p.CreateNewSlot(HeroClass.Arcanist),"create");
C(p.Profile.equippedSkills[8]==10,"default V");
foreach(int level in new[]{19,20,28,38}){p.Profile.level=level;p.Profile.skillRanks=new int[10];p.Profile.equippedSkills[8]=-1;Normalize(p.Profile);C(p.Profile.skillRanks.Length==11,"legacy resize");C(p.Profile.skillRanks[10]==(level<20?0:level<28?1:level<38?2:3),"automatic rank");C(p.Profile.equippedSkills[8]==10,"legacy V migration");}
p.Profile.equippedSkills[8]=0;p.Profile.equippedSkills[0]=-1;Normalize(p.Profile);C(p.Profile.equippedSkills[8]==0,"custom binding preserved");
var runtime=new SkillRuntime(HeroClass.Arcanist);C(runtime.TryConsume(10,3),"cast");C(runtime.Energy==70&&runtime.Remaining(10)>0,"cost cooldown");C(!runtime.TryConsume(10,3)&&runtime.Energy==70,"no double charge");runtime.Advance(30);C(runtime.TryConsume(10,1),"recast");
foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass))){var binds=MobileSkillPolicy.DefaultBindings(hero==HeroClass.Summoner,hero==HeroClass.Arcanist);C(MobileSkillPolicy.ValidBindings(binds),"valid bindings");C(binds[7]==(hero==HeroClass.Arcanist?10:-1)&&binds[8]==9,"mobile final slot");if(hero!=HeroClass.Arcanist){C(!new SkillRuntime(hero).TryConsume(10,3),"exclusive skill");var tx=p.PrepareClassSwitch(hero,true);C(tx!=null&&p.CommitClassSwitch(tx,true),"switch");p.PublishClassSwitch(tx);C(p.Profile.skillRanks[10]==0&&Array.IndexOf(p.Profile.equippedSkills,10)<0,"no skill on other class");}}
Array.Resize(ref p.Profile.classStates[(int)HeroClass.Arcanist].skillRanks,10);Normalize(p.Profile);C(p.Profile.classStates[(int)HeroClass.Arcanist].skillRanks.Length==11,"inactive legacy class migrated");var back=p.PrepareClassSwitch(HeroClass.Arcanist,true);C(back!=null&&p.CommitClassSwitch(back,true),"return to mage");p.PublishClassSwitch(back);C(p.Profile.skillRanks[10]==3,"rank restored");C(p.LoadSlot(p.CurrentSlotId)&&p.Profile.skillRanks[10]==3,"save reload");Directory.Delete(p.SaveDirectory,true);Console.WriteLine("PASS "+n+" tracking laser progression and input assertions");}}
'''
with tempfile.TemporaryDirectory(prefix='fashion-refinement-') as tmp:
 p=Path(tmp);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 names=['SkillCastingPolicy','ProgressionService.AchievementRewards','SkillRuntime','GameTypes','ProgressionService','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Trading','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow']
 sources=[root/('Assets/Scripts/Core/'+name+'.cs') for name in names]+[root/'Tests/ProgressionTests.cs',root/'Assets/Scripts/UI/CollectionPreviewState.cs',root/'Assets/Scripts/Combat/MobileSkillPolicy.cs']
 project=cv.write_project(p/'project',sources,program)
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else '/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet','run','--project',str(project),'--',str(p/'saves')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))

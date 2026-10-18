"""Enemy drop ceilings, chest rarity, ultimate timing and boss move selection."""
from pathlib import Path
import importlib.util, tempfile, subprocess, os, sys
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
program=r'''using System;using Emberfall;
class Program{static int n;static void C(bool v,string m){n++;if(!v)throw new Exception(m);}static void Main(){
for(int tier=1;tier<=100;tier++)for(int roll=0;roll<100;roll++){
 C(TierRewardRules.DropRarity(false,tier,roll)!=Rarity.Legendary,"ordinary no legendary");
 C(TierRewardRules.DropRarity(true,tier,roll)!=Rarity.Legendary,"boss ground no legendary");}
C(AdventureRewardRules.EquipmentRarity(0,1,0)==Rarity.Legendary,"chest keeps legendary");
C(SkillDamageBudgets.ChargeSeconds(HeroClass.Vanguard,9)==0&&SkillDamageBudgets.ChargeSeconds(HeroClass.Ranger,9)==0,"physical ultimates quick cast");
C(SkillDamageBudgets.ChargeSeconds(HeroClass.Arcanist,9)>0&&SkillDamageBudgets.ChargeSeconds(HeroClass.Summoner,9)>0,"spell ultimates retain distinct windups");
C(BossAttackPolicy.Select(2)==BossAttackPolicy.Move.Strike,"near melee");
C(BossAttackPolicy.Select(3)==BossAttackPolicy.Move.Spin,"near spin");
C(BossAttackPolicy.Select(6)==BossAttackPolicy.Move.Charge,"mid straight charge");
C(BossAttackPolicy.Select(2,BossAttackPolicy.Move.Strike,1)==BossAttackPolicy.Move.Spin,"melee alternates spin");
C(BossAttackPolicy.Select(2,BossAttackPolicy.Move.Spin,1)==BossAttackPolicy.Move.Slam,"spin alternates slam");
foreach(BossAttackPolicy.Move move in Enum.GetValues(typeof(BossAttackPolicy.Move))){
 C(!BossAttackPolicy.InRange(move,float.NaN),"nonfinite reject");
 C(BossAttackPolicy.Windup(move,false)>0,"warn every move");}
C(!BossAttackPolicy.InRange(BossAttackPolicy.Move.Strike,3),"no long melee");
C(!BossAttackPolicy.InRange(BossAttackPolicy.Move.Spin,5),"no long spin");
Console.WriteLine("PASS "+n+" combat/reward assertions");}}
'''
with tempfile.TemporaryDirectory(prefix='fashion-refinement-') as tmp:
 p=Path(tmp);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 names=['SkillCastingPolicy','ProgressionService.AchievementRewards','SkillRuntime','GameTypes','ProgressionService','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Trading','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow']
 sources=[root/('Assets/Scripts/Core/'+name+'.cs') for name in names]+[root/'Tests/ProgressionTests.cs',root/'Assets/Scripts/UI/CollectionPreviewState.cs']
 sources += [root/'Assets/Scripts/Combat/BossAttackPolicy.cs']
 project=cv.write_project(p/'project',sources,program)
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else '/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet','run','--project',str(project),'--',str(p/'saves')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))

"""Verify legacy saves and APIs converge to the fixed class defaults."""
from pathlib import Path
import importlib.util, tempfile, subprocess, os, sys
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
program=r'''using System;using System.Reflection;using Emberfall;
class Program{static int n;static void C(bool b,string m){n++;if(!b)throw new Exception(m);}static void Main(string[] args){
var method=typeof(ProgressionService).GetMethod("BuildOneItemChestRoll",BindingFlags.Static|BindingFlags.NonPublic);
for(int mode=-1;mode<=1;mode++)for(int tier=1;tier<=10;tier++){
var p=new GameProfile{pendingChestMode=mode,pendingChestTier=tier};int legendary=0,epic=0,shards=0,refine=0;
for(int roll=0;roll<100;roll++){var r=(ChestReward)method.Invoke(null,new object[]{p,roll,0,17,Guid.NewGuid().ToString("N")});if(r.primaryKind==1){if(r.primaryRarity==Rarity.Legendary)legendary++;else epic++;C(r.primarySlot==(int)AdventureRewardRules.EquipmentSlot(mode,0),"correct slot");}else if(r.primaryKind==3)shards++;else if(r.primaryKind==5)refine++;}
C(legendary==7&&epic==33&&shards==30&&refine==30,"same distribution every mode and tier");p.legendaryEquipmentMisses=29;for(int roll=0;roll<100;roll++){var r=(ChestReward)method.Invoke(null,new object[]{p,roll,0,17,Guid.NewGuid().ToString("N")});C(r.primaryKind==1&&r.primaryRarity==Rarity.Legendary,"30th chest legendary");}}
Console.WriteLine("PASS "+n+" unified dungeon chest checks");}}
'''
with tempfile.TemporaryDirectory(prefix='specialization-default-') as tmp:
 p=Path(tmp);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 names=['SkillCastingPolicy','ProgressionService.AchievementRewards','SkillRuntime','GameTypes','ProgressionService','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Trading','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow']
 sources=[root/('Assets/Scripts/Core/'+name+'.cs') for name in names]+[root/'Tests/ProgressionTests.cs',root/'Assets/Scripts/UI/CollectionPreviewState.cs']
 project=cv.write_project(p/'project',sources,program)
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else '/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet','run','--project',str(project),'--',str(p/'saves')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))

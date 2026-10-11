#!/usr/bin/env python3
"""Actual death/pending collection adapters + actual service/file writes; scene/audio doubles."""
from CastReceiptFixtureSources import include_cast_receipt_source
from pathlib import Path
import tempfile,subprocess,os,sys
root=Path(__file__).resolve().parents[1]
source=(root/'Assets/Scripts/Core/GameSession.cs').read_text()
def method(marker):
 a=source.index(marker);b=source.index('{',a)+1;depth=1
 while depth:depth+=(source[b]=='{')-(source[b]=='}');b+=1
 return source[a:b]
methods='\n'.join(method(m) for m in ['public void OnPlayerDied()','public void Respawn()','private bool PreserveWorldLoot()','public void CollectRemainingDungeonLoot()','public bool TryCollectGroundLoot(string itemId'])
helpers=['GameTypes','CombatBalance','ProgressionService','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Trading','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics']
with tempfile.TemporaryDirectory(prefix='death-loot-') as d:
 p=Path(d)
 for n in helpers:(p/(n+'.cs')).write_text((root/'Assets/Scripts/Core'/(n+'.cs')).read_text())
 for n in ['ProgressionTests','WorldLootReceiptTests','DeathLootPersistenceProductionTests']:(p/(n+'.cs')).write_text((root/'Tests'/(n+'.cs')).read_text())
 # These receipt cases have no engine supply objects. Keep the updated supply
 # preservation call and real service writes; only scene enumeration is empty.
 (p/'CurrentSceneBoundary.cs').write_text('''using System.Collections.Generic;namespace Emberfall{
 public sealed class ReceiptWorldBoundary{public void SetActive(bool active){}public T[] GetComponentsInChildren<T>()=>System.Array.Empty<T>();}
 public sealed class GroundSupplyPickup{public ReceiptWorldBoundary gameObject=new ReceiptWorldBoundary();}
 public partial class GameSession{private ReceiptWorldBoundary world=new ReceiptWorldBoundary();public bool InDungeon;private readonly List<string> runItemIds=new List<string>();private void Destroy(ReceiptWorldBoundary value){value.SetActive(false);}}
 }''')
 adapter=p/'DeathAdapter.cs';original='using System.Collections.Generic;namespace Emberfall{public partial class GameSession{'+methods+'}}';adapter.write_text(original)
 (p/'test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');include_cast_receipt_source(p/'test.csproj');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 cmd=[sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(p/'test.csproj'),'--',str(p/'saves')];env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 subprocess.run(cmd,env=env,check=True)
 adapter.write_text(original.replace('if (PreserveWorldLoot()) Progression.Save();','Progression.Save();'));r=subprocess.run(cmd,env=env,capture_output=True,text=True);assert r.returncode and 'death committed uncollected identity before returning' in r.stdout+r.stderr,r.stdout+r.stderr;print('PASS negative control: old direct death Save loses actual pending identity')

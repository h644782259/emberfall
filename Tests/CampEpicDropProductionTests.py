"""Enumerate actual rarity rolls; camp ordinary epic chance reduced by exactly 70%."""
from pathlib import Path
import tempfile,subprocess,os
root=Path(__file__).resolve().parents[1]
s=(root/'Assets/Scripts/Core/TierRewardRules.cs').read_text();a=s.index('public static Rarity DropRarity(');b=s.index('{',a)+1;n=1
while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
method=s[a:b]
code='''using System;enum Rarity {Common,Rare,Epic,Legendary}
class Program{METHOD
static void C(bool b,string m){if(!b)throw new Exception(m);}
static void Main(){int before=0,after=0,common=0;
for(int roll=0;roll<100;roll++){
if(DropRarity(false,0,roll)==Rarity.Epic)before++;
if(DropRarity(false,0,roll,true)==Rarity.Epic)after++;
if(DropRarity(false,0,roll,true)==Rarity.Common)common++;
C(DropRarity(true,0,roll,true)==DropRarity(true,0,roll),"boss unchanged");
for(int tier=1;tier<=100;tier++)C(DropRarity(false,tier,roll,true)==DropRarity(false,tier,roll),"dungeon unchanged");
C(DropRarity(false,0,roll,true)!=Rarity.Legendary,"no legendary added");}
C(before==10&&after==3&&after*10==before*3,"exact 70 percent reduction");C(common==65,"common unchanged; removed epic weight becomes rare");
Console.WriteLine("PASS camp epic 10% -> 3%; boss and 100 dungeon tiers unchanged");}}
'''.replace('METHOD',method)
host=(root/'Assets/Scripts/Core/GameSession.cs').read_text();assert '!InDungeon&&enemy.Tier==EnemyController.ThreatTier.Normal' in host
with tempfile.TemporaryDirectory(prefix='camp-epic-') as tmp:
 p=Path(tmp);(p/'Program.cs').write_text(code)
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 subprocess.run(['/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet','run','--project',str(p/'Test.csproj')],check=True,env=dict(os.environ,MSBuildEnableWorkloadResolver='false',DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))

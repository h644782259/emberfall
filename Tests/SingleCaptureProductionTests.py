"""Actual room and chapter state machines: one seal, preserved pause/contest rules."""
from pathlib import Path
import tempfile,subprocess,os
root=Path(__file__).resolve().parents[1]
code=r'''using System;using Emberfall;
namespace Emberfall {
public enum ChapterNode{ForestCourt}public enum ChapterDifficulty{Normal}
public static class ChapterDefinition{public static void Get(ChapterNode n){}public static RoomObjective RoomKind(ChapterNode n,int r)=>RoomObjective.Purify;public static int RoomCount(ChapterNode n)=>2;}
}
class Program{
static int n;static void C(bool b,string m){n++;if(!b)throw new Exception(m);}
static void Main(){
for(int seed=0;seed<36;seed+=3){
var r=new RoomChainState(seed);foreach(int i in System.Linq.Enumerable.Range(0,r.Room.EnemyCount))r.Register(r.Room,i);
r.AdvanceSeal(1,.25f,true,true,false);C(r.Seals==0&&r.SealProgress(1)==0,"no second capture");
r.AdvanceSeal(0,.25f,false,true,false);r.AdvanceSeal(0,.25f,true,true,true);C(r.Progress==0,"pause and contest block progress");
for(int i=0;i<4;i++)r.AdvanceSeal(0,.25f,true,true,false);
C(!r.DoorUnlocked&&r.CaptureFraction==.5f,"half capture");
r.AdvanceSeal(0,.25f,true,false,false);C(r.Progress==1,"leaving preserves progress");
for(int i=0;i<4;i++)r.AdvanceSeal(0,.25f,true,true,false);
C(r.Seals==1&&r.DoorUnlocked&&r.CaptureFraction==1,"one zone opens door with enemies alive");
r.AdvanceSeal(0,.25f,true,true,false);C(r.Seals==1,"completion idempotent");
C(!r.Next(false,false),"must approach portal");C(r.Next(true,false),"portal advances after one capture");}
var c=new ChapterCombatRun(ChapterNode.ForestCourt,ChapterDifficulty.Normal,0);c.BindRoom(7);for(int i=0;i<c.EnemyCount;i++)c.Register(0,7,i);
c.AdvanceSeal(1,.25f,true,true,false);C(c.SealProgress(1)==0,"chapter rejects removed seal");
c.AdvanceSeal(0,.25f,true,true,true);C(c.Progress==0,"chapter contested");
for(int i=0;i<7;i++)c.AdvanceSeal(0,.25f,true,true,false);C(!c.DoorUnlocked,"chapter needs full duration");
c.AdvanceSeal(0,.25f,true,true,false);C(c.DoorUnlocked&&c.Seals==1,"chapter one zone completes");
C(c.Exit(true,false),"chapter advances");c.BindRoom(8);C(c.Seals==0&&c.Progress==0,"new room reset");
Console.WriteLine("PASS "+n+" single capture assertions");}}
'''
with tempfile.TemporaryDirectory(prefix='single-capture-') as tmp:
 p=Path(tmp);(p/'Program.cs').write_text(code)
 for name in ['RoomChainState','RoomTactics','ChapterCombatRun']:(p/(name+'.cs')).write_bytes((root/'Assets/Scripts/Core'/(name+'.cs')).read_bytes())
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 subprocess.run(['/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet','run','--project',str(p/'Test.csproj')],check=True,env=dict(os.environ,MSBuildEnableWorkloadResolver='false',DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))

from pathlib import Path
import os, subprocess, tempfile
root=Path(__file__).resolve().parents[1]
s=(root/'Assets/Scripts/UI/GameUI.cs').read_text();a=s.index('private void UpdatePortalProximity()');b=s.index('{',a)+1;depth=1
while depth:depth+=(s[b]=='{')-(s[b]=='}');b+=1
method=s[a:b].replace('private','public',1)
code='''using System;
class PlayerController{}
class Session{public PlayerController Player=new PlayerController();public bool NearDungeonReturn,IsNearChapterEntrance,InDungeon,IsNearDungeonEntrance,InputBlocked,IsDead;public bool HasStarted=true;public int opened;public void EnterDungeon(){opened++;}}
static class MobileControls{public static int resets;public static void ResetInput(){resets++;}}
class UI{enum Panel{None,Other}Panel panel;public Session session=new Session();int proximityPortal;PlayerController proximityPortalPlayer;public bool UITransitionBlocked;public int exits;void CancelHotbarPointer(){}void CancelMobileScroll(){}void OpenDungeonExit(){exits++;}METHOD}
class Program{static int checks;static void C(bool x,string m){checks++;if(!x)throw new Exception(m);}static void Main(){var u=new UI();u.session.IsNearDungeonEntrance=true;u.UpdatePortalProximity();C(u.session.opened==1,"approach opens without button input");u.UpdatePortalProximity();C(u.session.opened==1,"same approach cannot spam even if entry failed");u.session.IsNearDungeonEntrance=false;u.UpdatePortalProximity();u.session.IsNearDungeonEntrance=true;u.UpdatePortalProximity();C(u.session.opened==2,"leaving range rearms approach");u.session.IsNearDungeonEntrance=false;u.UpdatePortalProximity();u.session.IsNearChapterEntrance=true;u.session.InputBlocked=true;u.UpdatePortalProximity();C(u.session.opened==2,"other modal blocks opening");u.session.InputBlocked=false;u.UpdatePortalProximity();C(u.session.opened==3,"chapter portal opens after input becomes available");u.session.IsNearChapterEntrance=false;u.UpdatePortalProximity();u.session.InDungeon=true;u.session.NearDungeonReturn=true;u.UpdatePortalProximity();u.UpdatePortalProximity();C(u.exits==1,"return portal opens once");u.session.Player=new PlayerController();u.UpdatePortalProximity();C(u.exits==2,"new player clears prior portal latch");Console.WriteLine("PASS "+checks+" actual portal proximity assertions");}}
'''.replace('METHOD',method)
with tempfile.TemporaryDirectory() as tmp:
 p=Path(tmp);(p/'Program.cs').write_text(code);(p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 subprocess.run([os.environ.get('DOTNET','/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet'),'run','--project',str(p/'Test.csproj')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli')))

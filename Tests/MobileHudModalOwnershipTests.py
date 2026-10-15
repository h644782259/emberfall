"""Execute the production HUD entry dispatch with a disabled modal underlay."""
from pathlib import Path
import subprocess, sys, tempfile

root = Path(__file__).resolve().parents[1]
source = (root / 'Assets/Scripts/UI/GameUI.Mobile.cs').read_text()
start = source.index('private void DrawMobileHUD()')
end = source.index('            Rect map=', start)
dispatch = source[start:end] + '\n}'
code = '''using System;
static class GUI { public static bool enabled; }
class Layout { public int Inventory=1,SkillsMenu=2,Menu=3,Catalog=4,Shop=5,Smith=6; }
static class MobileControls { public static Layout Layout=new Layout(); }
class GameProfile { public int skillPoints; }
class Progression { public GameProfile Profile=new GameProfile(); }
class Session { public Progression Progression=new Progression(); public void SetPaused(bool p){} }
enum Panel { Inventory,Skills }
enum HubNpcKind { Merchant,Blacksmith }
class Fixture {
Session session=new Session(); bool CanSwitchFunction=true,UITransitionBlocked,HubServicesAvailable=true;
int jade,gold,pale,clicked,actions; void DrawMobileVitals(Layout l){}
bool MobileIcon(int area,string icon,int color) { return GUI.enabled && area==clicked; }
void TogglePanel(Panel p){actions++;} void OpenProgressionGoals(){actions++;}
void OpenHubService(HubNpcKind k){actions++;}
''' + dispatch + '''
static void Main() {
 foreach(int button in new[]{1,2,4,5,6}) {
  var f=new Fixture{clicked=button}; GUI.enabled=false; f.DrawMobileHUD();
  if(f.actions!=0||GUI.enabled)throw new Exception("Modal underlay dispatched button "+button);
  GUI.enabled=true;f.DrawMobileHUD();if(f.actions!=1||!GUI.enabled)throw new Exception("Normal HUD entry unavailable "+button);
  f.UITransitionBlocked=true;f.DrawMobileHUD();if(f.actions!=1)throw new Exception("Transition touch leaked");
 }
 Console.WriteLine("PASS: 15 production HUD modal ownership assertions");
}}
'''
with tempfile.TemporaryDirectory(prefix='emberfall-modal-') as folder:
    p = Path(folder)
    (p/'Program.cs').write_text(code)
    (p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
    (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NuGetAudit>false</NuGetAudit><NoWarn>0649</NoWarn></PropertyGroup></Project>')
    subprocess.run([sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(p/'Test.csproj')],check=True)

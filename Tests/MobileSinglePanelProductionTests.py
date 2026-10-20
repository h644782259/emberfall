"""Single-panel geometry and all saved skill bindings, using production policies."""
from pathlib import Path
import os,subprocess,tempfile
root=Path(__file__).resolve().parents[1]
code='''using System;using Emberfall;
namespace Emberfall{public static class GameBalance{public const int SkillCount=11;}}
class Program{
static int n;static void C(bool b,string m){n++;if(!b)throw new Exception(m);}
static void Main(){
foreach(var size in new[]{new[]{568f,320f,163f,0f},new[]{844f,390f,163f,0f},new[]{2556f,1179f,460f,0f},new[]{1920f,1080f,0f,0f},new[]{1024f,768f,132f,1f},new[]{2360f,1640f,264f,1f},new[]{1136f,640f,326f,1f}}){
var l=new MobileControlLayout(size[0],size[1],size[2],ipad:size[3]>0);var a=l.CombatButtons();
C(l.Potion.X+l.Potion.Width+11.9f<=l.PlayerStatus.X,"potion leaves clear gap before health and energy bars");
float smallBottom=l.Skills[0].Y+l.Skills[0].Height;
C(smallBottom>=l.Height*.5f-.01f,"small row bottom stays within lower-half limit");
float arcTop=l.Skills[3].Y;for(int i=4;i<8;i++)arcTop=Math.Min(arcTop,l.Skills[i].Y);
C(Math.Abs(arcTop-smallBottom-10*l.Skills[0].Width/42)<.01f,"small row remains close to arc on every device");
for(int i=3;i<8;i++)C(l.Skills[i].Y>=l.Height*.5f,"large skills stay below midpoint");
C(l.Skills.Length==9&&a.Length==12,"all skill and action slots");
C(Math.Abs(l.Skills[8].X+l.Skills[8].Width-l.Jump.X-l.Jump.Width)<.01f,"top row right edge aligns with lower buttons");
C(l.Skills[8].X>l.Skills[2].X&&l.Skills[8].Y==l.Skills[0].Y,"ultimate is rightmost in top row");
for(int i=0;i<3;i++)C(l.Skills[i].Y==l.Skills[8].Y&&l.Skills[i].Width==l.Skills[8].Width,"four equal small icons");
C(l.Skills[7].X==l.Skills[6].X&&l.Skills[7].Width==l.Skills[6].Width,"lowest skill aligns left with skill above");
C(l.Skills[3].Y==l.Skills[7].Y&&l.Skills[3].X+l.Skills[3].Width<l.Skills[7].X,"moved skill sits left on lowest row");
C(l.Jump.Y+l.Jump.Height/2<l.Attack.Y+l.Attack.Height/2&&l.Dodge.Y+l.Dodge.Height/2>l.Attack.Y+l.Attack.Height/2,"mobility flanks attack above and below");
C(l.Jump.Width>l.Skills[3].Width&&l.Jump.Width==l.Dodge.Width,"mobility buttons slightly enlarged");
float ringRadius=-1;for(int i=4;i<7;i++){var r=l.Skills[i];float dx=r.X+r.Width/2-l.Attack.X-l.Attack.Width/2,dy=r.Y+r.Height/2-l.Attack.Y-l.Attack.Height/2;float distance=(float)Math.Sqrt(dx*dx+dy*dy);if(ringRadius<0)ringRadius=distance;C(Math.Abs(distance-ringRadius)<.01f&&r.Width>l.Skills[8].Width,"upper arc skills remain on equal radius");}
C(l.SkillPage.Width==0,"no paging target");
for(int i=0;i<a.Length;i++){var r=a[i];C(r.X>=0&&r.Y>=54&&r.X+r.Width<=l.Width&&r.Y+r.Height<=l.Height-5,"safe viewport "+size[0]+"/"+i);
for(int j=0;j<i;j++)C(!r.Overlaps(a[j]),"distinct hitboxes "+size[0]+"/"+i+"/"+j);
foreach(var other in new[]{l.Potion,l.PlayerStatus,l.Interact,l.Menu,l.Inventory,l.SkillsMenu,l.Catalog,l.Shop,l.Smith,l.FocusCommand,l.RecallCommand})C(!r.Overlaps(other),"HUD clearance "+size[0]+"/"+i+" vs "+other.X+","+other.Y);
}
}
foreach(bool summoner in new[]{false,true})foreach(bool arcanist in new[]{false,true}){var b=MobileSkillPolicy.DefaultBindings(summoner,arcanist);C(MobileSkillPolicy.ValidBindings(b),"saved binding order retained");int[] levels={1,4,6,4,10,6,13,20,13,30,20};for(int big=3;big<8;big++)for(int small=0;small<3;small++)if(b[small]>=0)C(levels[b[big]]<levels[b[small]],"lower level skills use the five larger buttons");for(int i=0;i<9;i++)C(MobileSkillPolicy.BindingIndex(i,0)==i,"single panel identity");C(MobileSkillPolicy.BindingIndex(0,1)==-1,"no second page");C(MobileSkillPolicy.SwapBinding(b,6,b[0]),"drag reorder");C(b[8]==9,"ultimate fixed");}
var compact=new MobileControlLayout(568,320,163);var medium=new MobileControlLayout(844,390,163);var large=new MobileControlLayout(1200,800,163);
C(compact.Attack.Width<medium.Attack.Width&&medium.Attack.Width<large.Attack.Width,"buttons grow gradually with available screen space");
C(medium.Skills[3].Width<52&&medium.Attack.Width<76,"phone buttons reduced from fixed sizing");
C(Math.Abs(compact.Skills[3].Width/compact.Attack.Width-large.Skills[3].Width/large.Attack.Width)<.001f,"button size hierarchy preserved");
var mage=MobileSkillPolicy.DefaultBindings(false,true);
int[] orderedButtons={3,7,6,5,4,0,1,2,8},unlock={1,4,6,4,10,6,13,20,13,30,20};
for(int i=1;i<orderedButtons.Length;i++)C(unlock[mage[orderedButtons[i-1]]]<=unlock[mage[orderedButtons[i]]],"visual reading order ascends by unlock level");C(Array.IndexOf(mage,10)>=0,"ninth active available");C(MobileSkillPolicy.ButtonCount==9&&MobileSkillPolicy.PageCount==1,"single page policy");Console.WriteLine("PASS "+n+" single-panel geometry and binding checks");}}
'''
with tempfile.TemporaryDirectory(prefix='single-panel-') as d:
 p=Path(d);(p/'Program.cs').write_text(code)
 for source in ['UI/MobileControlLayout','Combat/MobileSkillPolicy']:(p/(source.split('/')[-1]+'.cs')).write_bytes((root/('Assets/Scripts/'+source+'.cs')).read_bytes())
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>');(p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>')
 subprocess.run(['/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet','run','--project',str(p/'Test.csproj')],check=True,env=dict(os.environ,MSBuildEnableWorkloadResolver='false',DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))

"""Replay the production title draw and iPad-only layout. Managed GUI, not device rendering."""
from pathlib import Path
import sys, tempfile, subprocess, os
root=Path(__file__).resolve().parents[1]
s=(root/'Assets/Scripts/UI/GameUI.Mobile.cs').read_text();a=s.index('private void DrawMobileTitle()');b=s.index('{',a)+1;depth=1
while depth:depth+=(s[b]=='{')-(s[b]=='}');b+=1
method=s[a:b]
assert 'MobileTitleLayout.IsIPad(SystemInfo.deviceModel)' in method
shell=r'''
using System;using System.Collections.Generic;using Emberfall;using UnityEngine;
namespace UnityEngine{
 public struct Rect{public float x,y,width,height;public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;}public bool Contains(float a,float b)=>a>=x&&a<x+width&&b>=y&&b<y+height;}
 public struct Color{public Color(float a,float b,float c,float d=1){}public static Color operator*(Color c,float v)=>c;}
 public enum TextAnchor{MiddleCenter}public static class SystemInfo{public static string deviceModel;}
 public class GUIContent{public static GUIContent none=new GUIContent();}
 public static class GUI{public static float X=-1,Y=-1;public static bool Button(Rect r,GUIContent c,object style)=>r.Contains(X,Y);}
}
namespace Emberfall{
 public enum HeroClass{Vanguard,Arcanist,Ranger,Summoner}
 public static class GameBalance{public static Color ClassColor(HeroClass h)=>new Color();public static string ClassName(HeroClass h)=>h.ToString();}
 public static class MobileControls{public static MobileControlLayout Layout;}
 public class Progression{public string LastError="save error";}public class Session{public Progression Progression=new Progression();}
 public partial class GameUI{
  bool titleCreatingHero=true;void DrawAdventureHome(){}void BlockUITransition(){}bool saveSlotsDirty;List<int> saveSlots=new List<int>{1};Session session=new Session();float width=2000,height=1200;Color pale,muted,gold,jade;object invisibleButton;HeroClass selectedClass;
  enum ButtonRole{Navigation,Primary}public List<Rect> Cards=new List<Rect>(),Icons=new List<Rect>(),Buttons=new List<Rect>();public List<int> Fonts=new List<int>();public int Starts,Loads;
  void RefreshSaveSlots(){}void OpenSaveSelection(){Loads++;}void StartSelectedHero(){Starts++;}
  Rect TouchRect(MobileControlLayout.Area a)=>new Rect(a.X,a.Y,a.Width,a.Height);int TouchFont(float f)=>(int)Math.Round(f);
  void Fill(Rect r,Color c){}void Border(Rect r,Color c,float w){Cards.Add(r);}void DrawCrest(Rect r,HeroClass h,Color c){Icons.Add(r);}
  void Text(Rect r,string t,int font,Color c,bool bold=false,bool wrap=false,TextAnchor anchor=default){Fonts.Add(font);}
  bool DrawButton(Rect r,string t,ButtonRole role,bool enabled=true,string hint=null,int fontSize=0,string controlName=null){Buttons.Add(r);Fonts.Add(fontSize);return enabled&&r.Contains(GUI.X,GUI.Y);}
  METHOD
  static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
  static bool Inside(Rect r,float w,float h)=>r.x>=0&&r.y>=0&&r.x+r.width<=w+.01f&&r.y+r.height<=h+.01f;
  public static void Run(){
   foreach(string model in new[]{"iPad13,1","iPad Simulator","iPhone17,1","iPhone Simulator","",null})
   foreach(var size in new[]{(2420f,1668f),(1668f,2420f),(2048f,1536f),(1024f,768f),(700f,900f),(568f,320f)})
   foreach(float dpi in new[]{163f,264f,326f}){
    var l=MobileControls.Layout=new MobileControlLayout(size.Item1,size.Item2,dpi);SystemInfo.deviceModel=model;GUI.X=GUI.Y=-1;
    bool ipad=model!=null&&model.StartsWith("iPad");var layout=new MobileTitleLayout(l.Width,l.Height,ipad);var ui=new GameUI();ui.DrawMobileTitle();
    Check(ui.Cards.Count==4&&ui.Icons.Count==4&&ui.Buttons.Count==2,"four classes and both actions remain visible");
    foreach(var r in ui.Cards)Check(Inside(r,l.Width,l.Height),"class card stays inside safe area");foreach(var r in ui.Buttons)Check(Inside(r,l.Width,l.Height),"footer stays inside safe area");
    for(int i=0;i<4;i++){
     Check(Math.Abs(ui.Cards[i].width-126*layout.Zoom)<.01f&&Math.Abs(ui.Icons[i].width-74*layout.Zoom)<.01f,"cards and icons use same local zoom");
     if(i>0)Check(ui.Cards[i].x>ui.Cards[i-1].x+ui.Cards[i-1].width,"four cards stay side by side");
     var click=ui.Cards[i];GUI.X=click.x+click.width/2;GUI.Y=click.y+click.height/2;var hit=new GameUI();hit.DrawMobileTitle();Check((int)hit.selectedClass==i&&hit.Starts==0,"enlarged card hitbox selects the correct hero");
    }
    for(int i=0;i<2;i++){var r=ui.Buttons[i];GUI.X=r.x+r.width/2;GUI.Y=r.y+r.height/2;var hit=new GameUI();hit.DrawMobileTitle();Check(i==0?!hit.titleCreatingHero&&hit.Starts==0:hit.Starts==1&&hit.Loads==0,"footer click dispatches exactly the requested action");}
    Check(ui.Fonts[0]==(int)Math.Round(25*layout.Zoom)&&ui.Fonts[6]==(int)Math.Round(15*layout.Zoom),"heading and action typography scale with panel");
    if(!ipad)Check(layout.Zoom==1&&ui.Cards[0].x==(l.Width-528)/2&&ui.Cards[0].y==(l.Height-300)/2+50&&ui.Buttons[0].height==52,"phone layout is unchanged");
    if(ipad&&l.Width>=972&&l.Height>=596)Check(layout.Zoom==1.75f,"roomy iPad uses requested 1.75x enlargement");
    Check(ReferenceEquals(l,MobileControls.Layout),"title never replaces global HUD geometry");
   }
   Console.WriteLine("PASS "+checks+" production title draw/layout/hitbox assertions; managed boundary, not Unity/device rendering");
  }
 }
}
class Program{static void Main(){GameUI.Run();}}
'''
with tempfile.TemporaryDirectory(prefix='ipad-title-') as tmp:
 p=Path(tmp);(p/'Program.cs').write_text(shell.replace('METHOD',method))
 for name in ['MobileTitleLayout.cs','MobileControlLayout.cs']:(p/name).write_text((root/'Assets/Scripts/UI'/name).read_text())
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>0649</NoWarn></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 sdk=sys.argv[1] if len(sys.argv)>1 else 'dotnet';env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 subprocess.run([sdk,'build',str(p/'Test.csproj'),'--configfile',str(p/'NuGet.Config'),'-v:q'],check=True,env=env)
 subprocess.run([sdk,str(p/'bin/Debug/net8.0/Test.dll')],check=True,env=env)

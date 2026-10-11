#!/usr/bin/env python3
"""Exercise production travel cards/cache with GUI boundaries and shipped PNG/import policy."""
from pathlib import Path
import hashlib, os, re, struct, subprocess, sys, tempfile

root = Path(__file__).resolve().parents[1]

def member(source, signature):
    start = source.index(signature)
    end = source.index('{', start) + 1
    depth = 1
    while depth:
        depth += (source[end] == '{') - (source[end] == '}')
        end += 1
    return source[start:end]

hubs = (root / 'Assets/Scripts/UI/GameUI.Hubs.cs').read_text()
paths = re.search(r'TownTravelArtPaths=\{([^}]+)\}', hubs).group(1)
paths = re.findall(r'"([^"]+)"', paths)
assert paths == ['TownTravel/WhisperCamp', 'TownTravel/RedrockOutpost', 'TownTravel/StarwatchCity']
digests, guids = set(), set()
for resource in paths:
    png = root / ('Assets/Resources/' + resource + '.png')
    data = png.read_bytes()
    assert data[:8] == b'\x89PNG\r\n\x1a\n', png
    width, height = struct.unpack('>II', data[16:24])
    assert (width, height) == (1448, 1086), (png, width, height)
    digests.add(hashlib.sha256(data).hexdigest())
    meta = png.with_suffix('.png.meta').read_text()
    guids.add(re.search(r'^guid: ([a-f0-9]{32})$', meta, re.M).group(1))
    for setting in ['enableMipMap: 0', 'isReadable: 0', 'sRGBTexture: 1', 'filterMode: 1',
                    'wrapU: 1', 'wrapV: 1', 'wrapW: 1', 'nPOTScale: 0', 'maxTextureSize: 2048']:
        assert setting in meta, (png, setting)
    iphone = meta.split('buildTarget: iPhone', 1)[1].split('  - serializedVersion:', 1)[0]
    standalone = meta.split('buildTarget: Standalone', 1)[1]
    assert 'overridden: 1' in iphone and 'textureFormat: 50' in iphone, png
    assert 'overridden: 1' in standalone and 'textureCompression: 1' in standalone, png
assert len(digests) == len(guids) == 3, 'Three distinct shipped paintings/import identities'
initialize = member((root / 'Assets/Scripts/UI/GameUI.cs').read_text(), 'public void Initialize')
assert 'PrewarmTownTravelArtwork();' in initialize, 'Preload must run before title/gameplay UI'

draw = member(hubs, 'private void DrawTravelMap')
geometry = member(hubs, 'private static Rect TownTravelRect')
cache_start = hubs.index('        private static readonly string[] TownTravelArtPaths')
cache_end = hubs.index('\n\n        private void DrawExpandedMap', cache_start)
cache = hubs[cache_start:cache_end]
assert 'Resources.Load' not in draw and 'SetPixel' not in cache and 'new Texture2D(' not in cache

fixture = r'''
using System;using System.Collections.Generic;using UnityEngine;
namespace UnityEngine {
 public struct Rect {public float x,y,width,height;public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;}public float xMax=>x+width;public float yMax=>y+height;}
 public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color operator*(Color c,float f)=>new Color(c.r*f,c.g*f,c.b*f,c.a*f);}
 public enum ScaleMode {ScaleAndCrop} public enum TextAnchor {MiddleLeft,MiddleCenter}
 public class Texture2D {public string Path;public Texture2D(string path){Path=path;}}
 public static class Resources {public static int Loads;public static List<string> Paths=new List<string>();public static T Load<T>(string path) where T:class {Loads++;Paths.Add(path);return new Texture2D(path) as T;}}
 public static class GUI {public static Color color=new Color(.2f,.4f,.6f,.8f);public static List<(Rect,Texture2D,ScaleMode,bool,Color)> Images=new List<(Rect,Texture2D,ScaleMode,bool,Color)>();public static void DrawTexture(Rect r,Texture2D t,ScaleMode s,bool alpha){Images.Add((r,t,s,alpha,color));}}
 public static class Mathf {public static float Min(float a,float b)=>Math.Min(a,b);public static int RoundToInt(float v)=>(int)Math.Round(v);}
}
namespace Emberfall {
 static class MobileControls {public static bool Active;public static MobileControlLayout Layout;}
 class Profile {public int unlockedHubMask=7,level=100,clearedRuns=1;}
 class Progression {public Profile Profile=new Profile();public string LastError;}
 class Session {public bool CanOpenTravelMap=true,TravelWorks=true;public int CurrentHub,Trips,Destination=-1;public Progression Progression=new Progression();public bool TravelToHub(int hub){Trips++;Destination=hub;return TravelWorks;}}
 class GameUI {
 float width,height,TouchRatio;bool UITransitionBlocked,travelReturnPause=true;string travelError;int travelMapTab=1,click=-1,Closes;Session session=new Session();
 readonly List<Rect> blockedRects=new List<Rect>();Color jade=new Color(.1f,.8f,.5f),gold=new Color(.9f,.7f,.3f),pale=new Color(.9f,.9f,.9f),muted=new Color(.4f,.4f,.4f),card=new Color(.1f,.1f,.1f);
 List<(Rect,string,bool)> buttons=new List<(Rect,string,bool)>();List<(Rect,string,int)> texts=new List<(Rect,string,int)>();List<Rect> fills=new List<Rect>();Rect frame;
 void Box(Rect r,Color c){frame=r;}void Border(Rect r,Color c){}void Fill(Rect r,Color c){fills.Add(r);}void Text(Rect r,string s,int size,Color c,bool bold=false,bool wrap=false,TextAnchor anchor=TextAnchor.MiddleLeft){texts.Add((r,s,size));}
 void DrawAdventureMapCaption(Rect r,int s,Color c,string prefix,TextAnchor a){}bool PopupCloseButton(Rect r,bool enabled)=>false;bool PauseSidebarTab(Rect r,string s,bool active,float u)=>false;void DrawExpandedMap(Rect r,float u){throw new Exception("unexpected map tab");}
 bool Button(Rect r,string s,Color c,bool enabled=true){int index=buttons.Count;buttons.Add((r,s,enabled));return click==index&&enabled;}void CloseTravelMap(){Closes++;}
 DRAW
 GEOMETRY
 CACHE
 static int checks;static void C(bool okay,string reason){checks++;if(!okay)throw new Exception(reason);}static bool Near(float a,float b)=>Math.Abs(a-b)<.002f;
 static bool Within(Rect child,Rect parent)=>child.x>=parent.x-.002f&&child.y>=parent.y-.002f&&child.xMax<=parent.xMax+.002f&&child.yMax<=parent.yMax+.002f;
 void Render(){buttons.Clear();texts.Clear();fills.Clear();blockedRects.Clear();GUI.Images.Clear();DrawTravelMap();}
 static GameUI Device(float w,float h,float dpi,bool mobile,bool pad){MobileControls.Active=mobile;MobileControls.Layout=new MobileControlLayout(w,h,dpi,0,pad);float scale=HudLogicalScale.For(w,h)*(mobile?MobileControls.Layout.UiZoom:1);return new GameUI{width=w/scale,height=h/scale,TouchRatio=MobileControls.Layout.Scale/scale};}
 public static void Main(){
 C(typeof(TownTravelCardLayout).IsValueType,"draw geometry must not allocate a card object per repaint");
 var original=GUI.color;var preload=new GameUI();Resources.Loads=0;Resources.Paths.Clear();preload.PrewarmTownTravelArtwork();preload.PrewarmTownTravelArtwork();
 C(Resources.Loads==3&&Resources.Paths.Count==3,"initialization preloads exactly three paintings once");C(preload.townTravelArtLoaded,"prewarm commits cache readiness");for(int i=0;i<3;i++)C(preload.townTravelArt[i].Path==TownTravelArtPaths[i],"cached texture identity maps to each hub");
 for(int i=0;i<500;i++)for(int hub=0;hub<3;hub++)preload.DrawTownPostcard(new Rect(4,8,150,210),hub,i%2==0);
 C(Resources.Loads==3,"warm drawing never performs resources loading");C(GUI.Images.Count==1500,"each warm card uses one imported texture draw");C(Near(GUI.color.r,original.r)&&Near(GUI.color.a,original.a),"card drawing restores GUI color");
 foreach(int hub in new[]{-1,3}){GUI.Images.Clear();preload.DrawTownPostcard(new Rect(4,8,150,210),hub,true);C(GUI.Images.Count==0,"invalid hub safely draws fallback without loading");}C(Resources.Loads==3,"fallback also cannot trigger gameplay loading");
 foreach(var device in new[]{(1704f,786f,326f,true,false),(1366f,1024f,264f,true,true),(1920f,1080f,0f,false,false),(1136f,640f,326f,true,false),(1624f,746f,326f,true,false)})
 foreach(bool notice in new[]{false,true})foreach(bool unlocked in new[]{false,true})foreach(int current in new[]{0,1,2}){
 var ui=Device(device.Item1,device.Item2,device.Item3,device.Item4,device.Item5);ui.PrewarmTownTravelArtwork();ui.session.CurrentHub=current;ui.session.Progression.Profile.unlockedHubMask=unlocked?7:1;ui.session.Progression.Profile.level=unlocked?100:1;ui.session.Progression.Profile.clearedRuns=unlocked?1:0;ui.session.CanOpenTravelMap=!notice;int loaded=Resources.Loads;ui.Render();float u=MobileControls.Active?ui.TouchRatio:1.35f;
 C(ui.buttons.Count==3&&GUI.Images.Count==3,"three permanent card actions / paintings");C(Within(ui.frame,new Rect(0,0,ui.width,ui.height)),"centered panel stays inside viewport");C(ui.fills.Count<=16,"painting renderer has bounded fills instead of procedural pixel strips");
 float priorRight=0;for(int hub=0;hub<3;hub++){
 var image=GUI.Images[hub];var action=ui.buttons[hub];bool isUnlocked=unlocked||hub==0;C(Within(image.Item1,ui.frame)&&Within(action.Item1,ui.frame),"art and action stay in panel");C(image.Item1.width>100*u&&image.Item1.height>90*u,"native card area remains readable on compact phone / tablet / PC");C(Near(action.Item1.height,44*u),"travel target keeps its fixed touch height");C(image.Item1.yMax<action.Item1.y&&Near(action.Item1.yMax,ui.frame.yMax-18*u),"button remains below art at fixed footer baseline");C(image.Item1.x>=priorRight,"adjacent image cards cannot overlap");priorRight=image.Item1.xMax;
 C(image.Item2.Path==TownTravelArtPaths[hub]&&image.Item3==ScaleMode.ScaleAndCrop&&image.Item4,"centered artwork uses cached identity / crop without stretch");C(Near(image.Item5.a,original.a*(isUnlocked?1:.5f)),"locked art dims without changing following GUI color");
 string expected=current==hub?"当前城镇":isUnlocked?"前往":"尚未解锁";C(action.Item2==expected&&action.Item3==(isUnlocked&&current!=hub&&!notice),"current / locked / combat admission retained");
 var title=ui.texts.Find(t=>t.Item2==HubTravelRules.Name(hub));C(Within(title.Item1,image.Item1)&&Near(title.Item1.height,36*u),"title overlays art rather than reserving a separate card row");
 var hint=ui.texts.FindAll(t=>t.Item2==HubTravelRules.UnlockHint(hub));C(hint.Count==(isUnlocked?0:1),"only a locked town reserves visible unlock copy");if(!isUnlocked)C(Within(hint[0].Item1,image.Item1),"locked hint overlays the art");
 }
 C(Resources.Loads==loaded,"opening / redrawing a travel panel cannot load assets");C(Near(GUI.color.a,original.a),"whole panel restores alpha state");
 var before=ui.buttons[1].Item1;ui.travelError="保存未完成，请重试。";ui.Render();C(ui.buttons.Count==3&&Near(before.y,ui.buttons[1].Item1.y),"availability / failure notice never moves fixed actions");C(ui.texts.Exists(t=>t.Item2==ui.travelError),"travel failure has visible retry copy");
 }
 var success=Device(1704,786,326,true,false);success.PrewarmTownTravelArtwork();success.click=1;success.Render();C(success.session.Trips==1&&success.session.Destination==1&&success.Closes==1&&!success.travelReturnPause,"accepted travel closes once and clears return pause");
 var failed=Device(1366,1024,264,true,true);failed.PrewarmTownTravelArtwork();failed.click=2;failed.session.TravelWorks=false;failed.session.Progression.LastError="保存失败";failed.Render();C(failed.session.Trips==1&&failed.Closes==0&&failed.travelError=="保存失败","failed travel retains panel and real error for retry");failed.click=-1;failed.Render();C(failed.texts.Exists(t=>t.Item2=="保存失败"),"retained error draws on next repaint");
 foreach(int gate in new[]{0,1,2}){var blocked=Device(1920,1080,0,false,false);blocked.PrewarmTownTravelArtwork();blocked.click=1;if(gate==0)blocked.UITransitionBlocked=true;if(gate==1)blocked.session.CanOpenTravelMap=false;if(gate==2){blocked.session.Progression.Profile.unlockedHubMask=1;blocked.session.Progression.Profile.level=1;blocked.session.Progression.Profile.clearedRuns=0;}blocked.Render();C(blocked.session.Trips==0&&blocked.Closes==0,"transition / combat / unlock gates cannot request travel");}
 Console.WriteLine("PASS "+checks+" actual town painting cache, card rendering, five device/safe-area layouts and travel state assertions; managed GUI boundaries plus shipped PNG/import policy, no native rendering");
 }
 }
}
'''.replace(' DRAW', draw).replace(' GEOMETRY', geometry).replace(' CACHE', cache)

with tempfile.TemporaryDirectory(prefix='town-travel-artwork-') as folder:
    p = Path(folder)
    (p / 'Fixture.cs').write_text(fixture)
    for source in ['UI/TownTravelCardLayout.cs', 'UI/MobilePanelLayout.cs', 'UI/MobileControlLayout.cs', 'Core/HudLogicalScale.cs', 'Core/HubTravelRules.cs']:
        (p / Path(source).name).write_text((root / 'Assets/Scripts' / source).read_text())
    (p / 'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit><NoWarn>0649</NoWarn></PropertyGroup></Project>')
    (p / 'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    subprocess.run([sys.argv[1] if len(sys.argv) > 1 else os.environ.get('DOTNET', 'dotnet'), 'run', '--project', str(p / 'Test.csproj')], env=dict(os.environ, DOTNET_CLI_HOME=str(p / 'cli'), DOTNET_NOLOGO='1'), check=True)

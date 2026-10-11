using System;
using System.Collections.Generic;
using System.Linq;
using Emberfall;
using UnityEngine;

// Input, textures and draw calls are recorded boundaries, not engine rendering.
namespace UnityEngine
{
    public struct Color
    {
        public float r,g,b,a;
        public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}
        public static Color white=>new Color(1,1,1);
        public static Color operator*(Color c,float n)=>new Color(c.r*n,c.g*n,c.b*n,c.a*n);
    }
    public struct Vector2
    {
        public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}
        public static Vector2 operator-(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y);
    }
    public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}}
    public struct Rect
    {
        public float x,y,width,height;
        public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}
        public float xMax=>x+width;public float yMax=>y+height;public Vector2 center=>new Vector2(x+width*.5f,y+height*.5f);
        public bool Contains(Vector2 p)=>width>0&&height>0&&p.x>=x&&p.x<xMax&&p.y>=y&&p.y<yMax;
    }
    public class Texture2D {public string name;public bool Painting;public int width=128;public Texture2D(string n,bool painting=false){name=n;Painting=painting;}}
    public class Matrix4x4 {public Vector3 lossyScale=new Vector3(1,1,1);}
    public enum ScaleMode {ScaleToFit} public enum TextAnchor {MiddleCenter}
    public enum EventType {MouseDown,MouseDrag,MouseUp,KeyDown,Repaint} public enum KeyCode {Escape}
    public enum FocusType {Passive}
    public class Event {public static Event current=new Event();public EventType type=EventType.Repaint;public int button;public Vector2 mousePosition;public KeyCode keyCode;public void Use(){}}
    public static class GUIUtility {public static int hotControl;public static int GetControlID(int n,FocusType f)=>n;}
    public class GUIContent {public string text;public GUIContent(string s){text=s;}}
    public class GUIStyle {public Vector2 CalcSize(GUIContent c)=>new Vector2(c.text.Length*8,12);}
    public static class GUI
    {
        public sealed class Draw {public Rect Rect;public Texture2D Texture;public Color Color;}
        public static List<Draw> Draws=new List<Draw>();public static List<Rect> Groups=new List<Rect>();
        public static Color color=Color.white;public static Matrix4x4 matrix=new Matrix4x4();public static bool enabled=true;
        public static void DrawTexture(Rect r,Texture2D t,ScaleMode s,bool alpha)=>Draws.Add(new Draw{Rect=r,Texture=t,Color=color});
        public static void BeginGroup(Rect r)=>Groups.Add(r);public static void EndGroup(){}
    }
    public static class Screen {public static int height=1000;}
    public static class Time {public static int frameCount;public static float unscaledTime;}
    public static class Mathf
    {
        public static float Max(float a,float b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);
        public static int Max(int a,int b)=>Math.Max(a,b);public static int Min(int a,int b)=>Math.Min(a,b);
        public static float Abs(float n)=>Math.Abs(n);public static float Clamp(float n,float a,float b)=>Math.Clamp(n,a,b);
        public static float Clamp01(float n)=>Math.Clamp(n,0,1);public static int RoundToInt(float n)=>(int)Math.Round(n);
        public static float Sin(float n)=>(float)Math.Sin(n);public static float Repeat(float n,float length)=>n-(float)Math.Floor(n/length)*length;
    }
}
namespace Emberfall
{
    public enum HeroClass {Vanguard,Arcanist,Ranger,Summoner}
    public static class GameBalance
    {
        public const int SkillCount=10,HotbarSize=10,HotbarPotion=-2;
        public static bool IsPassive(int n)=>n==3||n==8;
        public static float EffectiveCooldown(HeroClass h,int n,int rank)=>20;
        public static float SkillEnergyCost(HeroClass h,int n)=>20;
        public static Color ClassColor(HeroClass h)=>new Color(.4f,.7f,1f);
        public static string KeyName(int key)=>key.ToString();
    }
    public static class SkillStockRules {public static int Skill(HeroClass h)=>4;}
    public class GameProfile {public HeroClass heroClass;public int[] skillRanks=new int[10],equippedSkills=new int[20],hotbarKeys=new int[10];public int hotbarPage,potions=3;}
    public class ProgressionStub {public GameProfile Profile=new GameProfile();}
    public struct CombatOpportunityState {public bool Window,Actionable;public string Caption;}
    public class SkillTargetingController {public int Calls;public bool Begin(int skill){Calls++;return true;}}
    public class PlayerStub
    {
        public float Remaining,Energy=100,DodgeCooldown;public int CombatEpoch;public SkillTargetingController Target=new SkillTargetingController();
        public bool IsSkillAvailable(int n)=>Remaining<=0;
        public float SkillCooldownRemaining(int n)=>Remaining;public float CooldownRemaining(int slot)=>Remaining;
        public int SkillCharges(int n)=>2;public float SkillRechargeRemaining(int n)=>0;public float SkillRechargePeriod(int n)=>20;
        public T GetComponent<T>()where T:class=>Target as T;public CombatOpportunityState SkillOpportunityWindow(int n)=>default;
    }
    public class GameSession {public ProgressionStub Progression=new ProgressionStub();public PlayerStub Player=new PlayerStub();public bool InputBlocked,ChallengeRun,InDungeon,PracticeActive;public int HealingCharges;public string ControlFailure(string k)=>"";public void ReportControlFailure(string k,string m){}}
    public static class MobileControls {public static bool Active=true,IsIPad;public static MobileControlLayout Layout=new MobileControlLayout(844,390,163);public static Rect SafeArea=new Rect(0,0,844,390);}
    public static class HudLogicalScale {public static float For(float w,float h)=>1;}
    public static class EffectPreferences {public static float TouchOpacity=1,InterfaceTextScale=1;}
    public static class ProductionGearIconArt {public static Texture2D Refresh(Texture2D t)=>t;}
    public static partial class AuthoredIconArt
    {
        static Dictionary<Texture2D,Texture2D> cooldown=new Dictionary<Texture2D,Texture2D>();
        public static bool IsPainting(Texture2D t)=>t!=null&&t.Painting;
        public static Texture2D ForTint(Texture2D t,Color c)=>t;
        public static Texture2D Cooldown(Texture2D t){if(!cooldown.ContainsKey(t))cooldown[t]=new Texture2D(t.name+"-cooldown",true);return cooldown[t];}
    }
    public static class UIIconAtlas
    {
        static Dictionary<string,Texture2D> textures=new Dictionary<string,Texture2D>();
        public static Texture2D Get(string s,bool art=false){if(!textures.ContainsKey(s))textures[s]=new Texture2D(s,art);return textures[s];}
        public static Texture2D ForDisplay(Texture2D t,float size)=>t;
        public static Texture2D Skill(HeroClass h,int n,int size=48)=>Get("skill-"+h+"-"+n,true);
        public static Texture2D SkillGlyph(HeroClass h,int n,int size=48)=>Skill(h,n,size);
        public static Color SkillColor(HeroClass h,int n)=>new Color(.4f,.7f,1f);
        public static Texture2D ControlDisc()=>Get("disc");public static Texture2D ControlRing(bool glow=false)=>Get(glow?"halo":"ring");
        public static Texture2D SkillPageArrow()=>Get("page");public static Texture2D Utility(string s)=>Get(s);
    }
    public partial class GameUI
    {
        enum Panel {None,Skills,Inventory}Panel panel;
        GameSession session=new GameSession();int mobileSkillPage,mobileSkillPageFrame=-1;int[] mobileBindings=MobileSkillPolicy.DefaultBindings();
        MobileSkillTap mobileTap=new MobileSkillTap();Rect[] hotbarSlots=new Rect[10];List<Rect> blockedRects=new List<Rect>();
        Rect hotbarBounds;float scale=1,width,height;Vector2 guiOffset;static float controlOpacity=1;Color jade=new Color(.2f,1,.8f),gold=new Color(1,.8f,.4f),muted=new Color(.5f,.6f,.7f),pale=Color.white,card=new Color(.1f,.2f,.3f),ink=new Color(.02f,.04f,.06f);
        int hotbarPointerSlot=-1,hotbarPointerControl;bool hotbarPointerConfiguring,hotbarDragging;Vector2 Mouse=>new Vector2(-99,-99);
        string tooltip,tooltipAnchorText;Rect tooltipAnchor;
        void ObserveTouchViewport(Rect safe){}void EnsureMobileBindings(){}
        Vector2 ScreenToUI(Vector2 p)=>p;Rect MobileVisualRect(Rect r)=>r;
        public void CancelMobileCast()=>mobileTap.Cancel();void CancelHotbarPointer(){hotbarPointerSlot=-1;}
        void ContinueHotbarPointer(Vector2 p){}void CompleteHotbarPointer(int slot){}void BeginHotbarPointer(int slot,Vector2 p,bool c){hotbarPointerSlot=slot;hotbarPointerConfiguring=c;}
        void DrawMobileSkillAvailability(Rect r,int skill){}void DrawSkillStock(Rect r,int n,float u){}
        string DesktopBasicOpportunityCaption()=>"";string DesktopSkillOpportunityCaption(int s,bool l,bool e,float c,out bool a){a=false;return "";}
        Texture2D HotbarIcon(GameProfile p,int n)=>UIIconAtlas.Utility("potion");string PotionTooltip(GameProfile p)=>"";string SkillTooltip(GameProfile p,int n,int rank)=>"";
        void SelectSkill(int n){}void TogglePanel(Panel p){}GUIStyle Style(int size,bool bold=false)=>new GUIStyle();
        void Fill(Rect r,Color c){}void Border(Rect r,Color c,float size=1){}void Box(Rect r,Color c,bool unused=true){}
        List<string> texts=new List<string>();void Text(Rect r,string s,int f,Color c,bool b=false,bool wrap=false,TextAnchor a=TextAnchor.MiddleCenter)=>texts.Add(s);
        public static GameUI Create(HeroClass hero,int[] ranks,int page=0)
        {var ui=new GameUI();ui.session.Progression.Profile.heroClass=hero;ui.session.Progression.Profile.skillRanks=(int[])ranks.Clone();ui.mobileBindings=MobileSkillPolicy.DefaultBindings(hero==HeroClass.Summoner);ui.mobileSkillPage=page;return ui;}
        public GameProfile Profile=>session.Progression.Profile;
        public void Layout()=>RefreshLayout();public Rect Slot(int n)=>hotbarSlots[n];public int Bound(int n)=>BoundMobileSkill(n,mobileSkillPage);
        public Rect ExpectedSlot(int n)=>TouchRect(MobileControls.Layout.Skills[n]);
        public bool Begin(int finger,Vector2 p)=>BeginMobileCast(finger,p);public void Release(int finger,Vector2 p,bool cancel=false)=>ContinueMobileCast(finger,p,true,cancel);
        public void SetSlot(int n,Rect r)=>hotbarSlots[n]=r;public int Captured=>mobileTap.Skill;public int Casts=>session.Player.Target.Calls;
        public int[] DrawMobile(){blockedRects.Clear();GUI.Draws.Clear();DrawMobileHotbar();return blockedRects.Select(r=>Array.FindIndex(hotbarSlots,s=>Same(s,r))).Where(n=>n>=0).ToArray();}
        public void DrawDesktop(){GUI.Draws.Clear();DrawHotbar();}public bool DesktopVisible(int slot)=>CombatSlotVisible(Profile,slot);
        public int HoverDesktop(Vector2 p){hotbarPointerSlot=-1;Event.current=new Event{type=EventType.MouseDown,button=0,mousePosition=p};HandleHotbarPointer(hotbarSlots,false);return hotbarPointerSlot;}
        public List<GUI.Draw> RenderState(float remaining,bool ready,float opacity=1)
        {session.Player.Remaining=remaining;GUI.Draws.Clear();GUI.Groups.Clear();texts.Clear();controlOpacity=opacity;GUI.color=new Color(.8f,.7f,.6f,.5f);DrawRecoveringSkill(new Rect(1,2,52,52),UIIconAtlas.Skill(Profile.heroClass,0),0,ready);return GUI.Draws.ToList();}
        public List<string> Texts=>texts;public static bool Same(Rect a,Rect b)=>a.x==b.x&&a.y==b.y&&a.width==b.width&&a.height==b.height;
    }
}
static class CombatSkillHudStateTests
{
    static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
    static bool Near(float a,float b)=>Math.Abs(a-b)<.0001f;
    static void Main()
    {
        var ranks=new[]{2,2,2,2,1,2,1,0,1,0};
        foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass)))foreach(int page in new[]{0,1})
        {
            MobileControls.Active=true;var ui=GameUI.Create(hero,ranks,page);ui.Layout();
            var all=GameUI.Create(hero,Enumerable.Repeat(1,10).ToArray(),page);all.Layout();
            var drawn=ui.DrawMobile();int expected=0;
            for(int button=0;button<5;button++)
            {
                int skill=ui.Bound(button);bool learned=skill>=0&&ranks[skill]>0;
                if(learned){expected++;Check(GameUI.Same(ui.Slot(button),all.Slot(button))&&GameUI.Same(ui.Slot(button),ui.ExpectedSlot(button)),"remaining arc coordinates never move when another skill is hidden");}
                else Check(!ui.Slot(button).Contains(ui.ExpectedSlot(button).center),"unlearned skill has no registered hitbox");
                if(skill>=0)Check(ui.MobileSkillVisible(skill)==learned,"unlearned skill is invisible to opportunity input");
                Check(drawn.Contains(button)==learned,"only learned skills draw and reserve HUD rectangles");
                if(!learned)
                {
                    Check(!ui.Begin(10,ui.ExpectedSlot(button).center)&&ui.Captured<0,"hidden HUD region does not capture skill input");
                    if(skill>=0){ui.SetSlot(button,all.Slot(button));Check(!ui.Begin(10,all.Slot(button).center)&&ui.Captured<0,"stale hidden hitbox never captures a skill touch");ui.Layout();}
                }
                else
                {
                    int before=ui.Casts;Check(ui.Begin(10,ui.Slot(button).center),"learned skill captures its own touch");
                    ui.Release(11,ui.Slot(button).center);Check(ui.Captured==skill&&ui.Casts==before,"other finger never releases the captured skill");
                    ui.Release(10,ui.Slot(button).center);ui.Release(10,ui.Slot(button).center);Check(ui.Casts==before+1&&ui.Captured<0,"one owned release casts once");
                    ui.Begin(10,ui.Slot(button).center);ui.Release(10,new Vector2(-1,-1));Check(ui.Casts==before+1,"dragging away cancels without casting");
                    ui.Begin(10,ui.Slot(button).center);ui.Profile.skillRanks[skill]=0;ui.Release(10,ui.Slot(button).center);Check(ui.Casts==before+1&&ui.Captured<0,"becoming unlearned before release cancels without casting");ui.Profile.skillRanks[skill]=ranks[skill];
                }
            }
            Check(drawn.Length==expected,"only learned skills draw and reserve HUD rectangles");
            Check(GUI.Draws.Count(d=>d.Texture.Painting)==expected,"unlearned icons produce no material draw calls");
            foreach(bool mobile in new[]{false,true})
            {
                MobileControls.Active=mobile;
                var state=ui.RenderState(0,true,.7f);var art=state.Single(d=>d.Texture.Painting);Check(!art.Texture.name.EndsWith("-cooldown")&&Near(art.Color.r,1)&&Near(art.Color.g,1)&&Near(art.Color.b,1)&&Near(art.Color.a,.7f),"ready skill keeps complete source color with opacity applied once");
                Check(Near(GUI.color.r,.8f)&&Near(GUI.color.a,.5f),"material draw restores prior GUI color");
                state=ui.RenderState(0,false);art=state.Single(d=>d.Texture.Painting);Check(art.Texture.name.EndsWith("-cooldown")&&Near(art.Color.r,.35f)&&Near(art.Color.g,.35f)&&Near(art.Color.b,.35f),"temporarily unavailable material is dark and neutral");
                foreach(float seconds in new[]{6.4f,20f})
                {
                    state=ui.RenderState(seconds,false);var dim=state.Single(d=>d.Texture.name.EndsWith("-cooldown"));Check(Near(dim.Color.r,.25f)&&Near(dim.Color.a,1),"cooldown base is dark rather than translucent white");
                    var color=state.Where(d=>d.Texture.Painting&&!d.Texture.name.EndsWith("-cooldown")).ToArray();
                    if(seconds<20){Check(color.Length==1&&Near(color[0].Color.r,.5f)&&Near(color[0].Color.a,1),"cooldown recovery retains a restrained colored intensity");Check(Near(GUI.Groups[0].height,52*(1-seconds/20)),"recovery boundary follows actual remaining cooldown");}
                    else Check(color.Length==0,"fresh cooldown has no ready-color recovery fill");
                    Check(ui.Texts.Contains(seconds>=10?"20":"6.4"),"cooldown remains centered and readable");
                }
            }
            MobileControls.Active=false;ui.Profile.equippedSkills=new[]{0,7,9,-2,-1,1,2,4,5,6,0,7,9,-2,-1,1,2,4,5,6};ui.Layout();
            Check(!ui.DesktopVisible(1)&&!ui.DesktopVisible(2)&&ui.DesktopVisible(3)&&ui.DesktopVisible(4),"desktop hides unlearned bound skills and preserves potion and empty configuration slots");
            Check(ui.HoverDesktop(new Vector2(ui.Slot(0).x+53,ui.Slot(0).center.y))<0,"hidden desktop skill cannot acquire pointer ownership");
            ui.DrawDesktop();Check(!GUI.Draws.Any(d=>d.Texture.name=="skill-"+hero+"-7"||d.Texture.name=="skill-"+hero+"-9"),"desktop hidden skills do not draw their material");
        }
        Check(Near(AuthoredIconArt.DisplayTint(new Texture2D("equipment",true),new Color(.62f,.67f,.73f)).r,1),"global authored equipment tint behavior is unchanged");
        Console.WriteLine("PASS "+checks+" production HUD visibility, state-material and owned-touch assertions; recording GUI only, no rendered or native-touch claim.");
    }
}

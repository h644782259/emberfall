using System;
using System.Collections.Generic;
using Emberfall;

// Only the binding persistence boundary and the layout owner are substituted.
// BoundMobileSkill/MobileOpportunityArea are extracted unchanged from production.
namespace UnityEngine { public static class Mathf { public static int Min(int a,int b){return Math.Min(a,b);} } }
namespace Emberfall
{
    public static class MobileControls { public static MobileControlLayout Layout; public static bool IsIPad,Active; }
    public partial class GameUI
    {
        int mobileSkillPage;
        public GameUI(int[] bindings,int page){mobileBindings=(int[])bindings.Clone();mobileSkillPage=page;}
        public int BoundForLayoutTest(int button){return BoundMobileSkill(button,mobileSkillPage);}
        // Only external session/list state is substituted for the extracted loot getters.
        private enum Panel { None,Other }
        private Panel panel;
        private sealed class LootFixtureChoices { public bool AwaitingChoice; }
        private sealed class LootFixtureSession
        {
            public bool InDungeon=true,IsDead,Paused,PracticeActive,InputBlocked,RoomBranchChoiceOpen;
            public string Notification="";
            public LootFixtureChoices RunChoices=new LootFixtureChoices();
        }
        private LootFixtureSession session=new LootFixtureSession();
        private readonly List<object> lootNotices=new List<object>();
        public void SetLootStateForTest(int count,string notification,int blockedState=-1)
        {
            session=new LootFixtureSession{Notification=notification};panel=Panel.None;
            lootNotices.Clear();for(int i=0;i<count;i++)lootNotices.Add(new object());
            switch(blockedState)
            {
                case 0:session.InDungeon=false;break;case 1:session.IsDead=true;break;
                case 2:session.Paused=true;break;case 3:session.PracticeActive=true;break;
                case 4:panel=Panel.Other;break;case 5:session.InputBlocked=true;break;
                case 6:session.RunChoices.AwaitingChoice=true;break;case 7:session.RoomBranchChoiceOpen=true;break;
            }
        }
        public bool LootVisibleForTest(){return LootNoticesVisible;}
        public bool LootReplacesInformationForTest(){return MobileLootReplacesInformation;}
        public int VisibleLootCountForTest(){return VisibleLootNoticeCount;}
        public int QueuedLootCountForTest(){return lootNotices.Count;}
    }
}

public static class MobileCombatClusterLayoutTests
{
    static int checks;
    static string scenario;
    static void Check(bool value,string message)
    {checks++;if(!value)throw new Exception(message+"; "+scenario);}
    static bool Inside(MobileControlLayout.Area area,MobileControlLayout layout)
    {return area.X>=0&&area.Y>=0&&area.X+area.Width<=layout.Width+.01f&&area.Y+area.Height<=layout.Height+.01f;}
    public static string Run()
    {
        checks=0;
        // One pre-correction runtime snapshot: Screen 1920x883, safeArea (136,44,1648,839), dpi 460.
        // Replay its raw DPI and the corrected native2622x1206/render1920x883 DPI.
        // Only measured dimensions enter this pure fixture; neither replay proves rendered/touch behavior.
        // Other inputs remain native specifications or synthetic logical viewports.
        var devices=new[]{new[]{568f,320f,163f},new[]{844f,375f,163f},new[]{1334f,750f,326f},new[]{2250f,1125f,458f},new[]{2622f,1206f,460f},new[]{1648f,839f,460f},new[]{1648f,839f,MobileControlLayout.RenderDpi(460,1920,883,2622,1206)},new[]{1280f,720f,0f}};
        foreach(var d in devices)foreach(int preset in new[]{-1,0,1})
        {
            var layout=new MobileControlLayout(d[0],d[1],d[2],preset);
            MobileControls.Layout=layout;
            scenario=d[0]+"x"+d[1]+" dpi="+d[2]+" preset="+preset;
            Check(Math.Abs(layout.Width*layout.Scale-d[0])<.01f&&Math.Abs(layout.Height*layout.Scale-d[1])<.01f,"safe viewport and touch scale round-trip once");
            if(d[0]==844)Check(Math.Abs(layout.Height-375)<.01f,"synthetic 375-unit-high phone input retains its logical height");
            if(d[0]==2622)Check(Math.Abs(layout.Scale-460f/163f)<.0001f,"native iPhone 16 Pro specification uses the density rule");
            if(d[0]==1648&&d[2]==460)Check(Math.Abs(layout.Scale-2.621875f)<.0001f&&Math.Abs(layout.Width-628.5578f)<.01f&&Math.Abs(layout.Height-320)<.01f,"raw measured runtime input reproduces its pre-correction logical viewport once");
            if(d[0]==1648&&d[2]!=460)Check(Math.Abs(layout.Scale-d[2]/163f)<.0001f&&Math.Abs(layout.Width-1648f*163f/d[2])<.01f&&Math.Abs(layout.Height-839f*163f/d[2])<.01f,"corrected replay of measured safe dimensions applies render density once");
            checks+=MobileControlLayoutTests.VerifyPhoneArc(layout,preset);
            var actions=new List<MobileControlLayout.Area>{layout.Attack,layout.Dodge,layout.Jump,layout.SkillPage};actions.AddRange(layout.Skills);
            foreach(var action in actions)
            {
                Check(Inside(action,layout),"phone action hitbox stays in safe area");
                Check(action.Width>=44&&action.Height>=44,"compact phone actions retain touch target sizes");
            }
            for(int i=0;i<actions.Count;i++)for(int j=i+1;j<actions.Count;j++)Check(!actions[i].Overlaps(actions[j]),"compact phone action hitboxes never overlap");
            var reserved=new List<MobileControlLayout.Area>(actions){layout.Joystick,layout.Potion,layout.Menu,layout.Inventory,layout.SkillsMenu,layout.Catalog,layout.Shop,layout.Smith,layout.Interact,layout.FocusCommand,layout.RecallCommand,layout.DungeonEntrance,layout.PlayerStatus,layout.Map,layout.AdventureStatus,layout.EncounterText,layout.BossHealth};
            var swapped=MobileSkillPolicy.DefaultBindings();
            Check(MobileSkillPolicy.SwapBinding(swapped,3,0),"real binding policy allows a cross-slot permutation");
            var bindings=new[]{MobileSkillPolicy.DefaultBindings(),MobileSkillPolicy.DefaultBindings(true),swapped};
            for(int configuration=0;configuration<bindings.Length;configuration++)for(int page=0;page<MobileSkillPolicy.PageCount;page++)
            {
                scenario=d[0]+"x"+d[1]+" dpi="+d[2]+" preset="+preset+" bindings="+configuration+" page="+page;
                var ui=new GameUI(bindings[configuration],page);
                var hints=new List<MobileControlLayout.Area>();
                for(int button=0;button<MobileSkillPolicy.ButtonCount;button++)
                {
                    int identity=ui.BoundForLayoutTest(button);
                    if(identity<0)continue;
                    var hint=ui.MobileOpportunityArea(identity);
                    var key=layout.Skills[button];
                    Check(Inside(hint,layout),"visible hint stays in safe area on both skill pages");
                    Check(hint.Width>0&&hint.Height==14,"visible skill identities retain an independent caption hit area");
                    Check(Math.Abs(hint.X-key.X)<.01f&&Math.Abs(hint.Width-key.Width)<.01f,"visible hint follows its actual bound physical slot");
                    foreach(var target in reserved)Check(!hint.Overlaps(target),"visible hint never covers a skill, action or HUD target");
                    Check(!hint.Overlaps(layout.CombatView),"visible hint leaves the central combat window clear");
                    if(configuration==0)
                    {
                        var stored=layout.SkillOpportunities[identity];
                        Check(Math.Abs(stored.X-hint.X)<.01f&&Math.Abs(stored.Y-hint.Y)<.01f&&Math.Abs(stored.Width-hint.Width)<.01f&&Math.Abs(stored.Height-hint.Height)<.01f,"stored caption and actual binding caption use the same geometry");
                    }
                    hints.Add(hint);
                }
                for(int identity=0;identity<10;identity++)
                {
                    bool visible=false;
                    for(int button=0;button<MobileSkillPolicy.ButtonCount;button++)if(ui.BoundForLayoutTest(button)==identity)visible=true;
                    if(!visible){var hidden=ui.MobileOpportunityArea(identity);Check(hidden.Width==0&&hidden.Height==0,"hidden page and passive identities never reserve a caption hitbox");}
                }
                hints.Add(layout.CounterOpportunity);hints.Add(layout.ComboOpportunity);
                foreach(var hint in new[]{layout.CounterOpportunity,layout.ComboOpportunity})
                {
                    Check(Inside(hint,layout),"basic attack captions stay inside safe area");
                    foreach(var target in reserved)Check(!hint.Overlaps(target),"basic attack captions never cover action or HUD targets");
                }
                for(int i=0;i<hints.Count;i++)for(int j=i+1;j<hints.Count;j++)Check(!hints[i].Overlaps(hints[j]),"simultaneous captions on both pages never overlap "+i+"/"+j);
            }
        }
        return "PASS "+checks+" compact phone layout and actual binding-caption assertions; one runtime safe-dimension snapshot plus specification/synthetic inputs, no Unity rendering or touch evidence";
    }
}

public static class MobileLootNoticeVisibilityTests
{
    static int checks;
    static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    public static string Run()
    {
        checks=0;
        foreach(var device in new[]{new[]{568f,320f,163f},new[]{1648f,839f,460f},new[]{844f,375f,163f}})
        {
            MobileControls.Layout=new MobileControlLayout(device[0],device[1],device[2]);
            var ui=new GameUI(MobileSkillPolicy.DefaultBindings(),0);
            bool fallback=MobileControls.Layout.LootNotice(0).Overlaps(MobileControls.Layout.AdventureStatus);
            if(device[0]==568)Check(fallback,"compact phone pickup uses the shared information slot");
            if(device[0]==844)Check(!fallback,"wide phone pickup retains a separate information slot");
            foreach(bool active in new[]{true,false})foreach(int count in new[]{0,1,3})foreach(string notification in new[]{"","notification"})
            {
                MobileControls.Active=active;ui.SetLootStateForTest(count,notification);
                bool visible=!active||!fallback||notification.Length==0;
                Check(ui.LootVisibleForTest()==visible,"notification has priority only over a shared mobile pickup slot");
                Check(ui.LootReplacesInformationForTest()==(active&&visible&&count>0&&fallback),"empty pickup queue never hides objectives and visible shared pickups replace them");
                Check(ui.QueuedLootCountForTest()==count,"visibility getters preserve the queued pickups");
                int shown=ui.VisibleLootCountForTest();
                Check(shown>=0&&shown<=Math.Min(count,active?2:3),"actual visible pickup count respects platform limits");
                if(active)for(int i=0;i<shown;i++)Check(MobileControls.Layout.LootNotice(i).Width==174&&MobileControls.Layout.LootNotice(i).Height==44,"visible mobile pickups keep complete content dimensions");
                if(!active)Check(shown==Math.Min(count,3),"desktop pickup count keeps its established limit");
                if(active&&fallback)Check(shown==Math.Min(count,1),"shared information slot exposes one full pickup card");
            }
            foreach(bool active in new[]{true,false})for(int blocked=0;blocked<8;blocked++)
            {
                MobileControls.Active=active;ui.SetLootStateForTest(3,"",blocked);
                Check(!ui.LootVisibleForTest(),"actual session block prevents pickup display");
                Check(!ui.LootReplacesInformationForTest(),"blocked pickup display never hides objectives");
                Check(ui.QueuedLootCountForTest()==3,"session visibility block preserves queue data");
            }
        }
        return "PASS "+checks+" actual loot visibility getter assertions; external session/list boundaries substituted, no draw/timer/touch evidence";
    }
}

class Program
{
    static void Main(string[] args)
    {
        if(args.Length>0&&args[0]=="phone-controls"){Console.WriteLine(MobileControlLayoutTests.RunPhone());return;}
        if(args.Length>0&&args[0]=="all-controls"){Console.WriteLine(MobileControlLayoutTests.Run());return;}
        if(args.Length>0&&args[0]=="loot-visibility"){Console.WriteLine(MobileLootNoticeVisibilityTests.Run());return;}
        if(args.Length>0&&args[0]=="render-dpi"){Console.WriteLine(MobileControlLayoutTests.RunDpi());return;}
        Console.WriteLine(MobileCombatClusterLayoutTests.Run());
    }
}

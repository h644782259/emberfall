using System;
using System.Collections.Generic;
using Emberfall;
public static class MobileControlLayoutTests
{
    static int checks;
    public const float AcceptedPhoneSkillDiameter=52;
    const float AcceptedPhoneAttackInset=143;
    const float AcceptedPadJumpTopInset=199.9f;
    static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    static bool Near(float a,float b){return Math.Abs(a-b)<.01f;}
    static void Center(MobileControlLayout.Area area,float x,float y,float diameter,string name)
    {Check(Near(area.X+area.Width*.5f,x)&&Near(area.Y+area.Height*.5f,y)&&Near(area.Width,diameter)&&Near(area.Height,diameter),name+" keeps its accepted phone center and touch diameter");}
    public static int VerifyPhoneArc(MobileControlLayout layout,int preset=0)
    {
        int before=checks;
        float chord=0;
        for(int i=0;i<4;i++)
        {
            var key=layout.Skills[i];
            Check(Near(key.Width,AcceptedPhoneSkillDiameter)&&Near(key.Height,AcceptedPhoneSkillDiameter),"ordinary phone skills retain the original platform touch diameter");
            if(i==0)continue;
            var previous=layout.Skills[i-1];float dx=key.X+key.Width*.5f-previous.X-previous.Width*.5f,dy=key.Y+key.Height*.5f-previous.Y-previous.Height*.5f;
            Check(dx>0&&dy<0,"four ordinary phone skills follow the restored smooth arc");
            float spacing=(float)Math.Sqrt(dx*dx+dy*dy);
            if(i==1)chord=spacing;else Check(Near(spacing,chord),"ordinary phone arc preserves equal adjacent chord spacing");
        }
        float shift=preset<0?-Math.Min(8,Math.Max(0,layout.Height-320)):0;
        for(int i=0;i<4;i++)
        {
            double angle=i*Math.PI/6;
            Center(layout.Skills[i],layout.Width-108-144*(float)Math.Cos(angle),layout.Height-44-144*(float)Math.Sin(angle)+shift,AcceptedPhoneSkillDiameter,"restored ordinary arc slot "+i);
        }
        Center(layout.Attack,layout.Width-AcceptedPhoneAttackInset,layout.Height-67,76,"main-platform basic attack");
        Center(layout.Dodge,layout.Width-48,layout.Height-42+shift,48,"dodge");
        Center(layout.Jump,layout.Width-48,layout.Height-97+shift,48,"jump");
        Center(layout.SkillPage,layout.Width-48,layout.Height-150+shift,44,"page switch");
        Center(layout.Skills[4],layout.Width-48,layout.Height-211.2f+shift,62.4f,"fixed ultimate inside the compact right control group");
        Check(Near(layout.Interact.X,layout.Width-156)&&Near(layout.Interact.Y,56)&&Near(layout.Interact.Width,48)&&Near(layout.Interact.Height,48),"phone contextual action clears the restored compact arc");
        var actions=new List<MobileControlLayout.Area>{layout.Attack,layout.Dodge,layout.Jump,layout.SkillPage};actions.AddRange(layout.Skills);
        foreach(var action in actions)Check(action.X>=layout.Width-252-AcceptedPhoneSkillDiameter*.5f-.01f&&action.Y>=layout.Height-242.4f+shift-.01f,"phone controls remain inside the accepted compact arc bounds");
        return checks-before;
    }
    public static string RunPhone(){return Run(true);}
    public static string RunDpi()
    {
        checks=0;
        float expected=460f*883f/1206f;
        Check(Near(MobileControlLayout.RenderDpi(460,1920,883,2622,1206),expected),"lower render resolution converts native DPI to the render grid");
        Check(Near(MobileControlLayout.RenderDpi(460,1920,883,1206,2622),expected),"native portrait dimensions rotate into the landscape render orientation");
        Check(Near(MobileControlLayout.RenderDpi(460,883,1920,2622,1206),expected),"native landscape dimensions rotate into the portrait render orientation");
        Check(Near(MobileControlLayout.RenderDpi(460,2622,1206,2622,1206),460),"native-size rendering keeps the reported DPI");
        foreach(var invalid in new[]{new[]{0f,1206f},new[]{2622f,0f},new[]{-1f,1206f}})Check(Near(MobileControlLayout.RenderDpi(460,1920,883,invalid[0],invalid[1]),460),"missing native resolution keeps the advisory DPI");
        foreach(var invalid in new[]{new[]{0f,883f},new[]{1920f,0f}})Check(Near(MobileControlLayout.RenderDpi(460,invalid[0],invalid[1],2622,1206),460),"missing render resolution keeps the advisory DPI");
        foreach(float dpi in new[]{0f,119f,701f})Check(Near(MobileControlLayout.RenderDpi(dpi,1920,883,2622,1206),dpi),"unusable native DPI remains available to the existing density fallback");
        Check(Near(MobileControlLayout.RenderDpi(460,1920,1080,2622,1206),460),"inconsistent display aspect ratio retains the reported DPI");
        Check(Near(MobileControlLayout.RenderDpi(460,500,247,1000,500),460f*.494f),"near-uniform render-grid rounding uses the conservative axis ratio");
        Check(Near(MobileControlLayout.RenderDpi(460,500,230,1000,500),460),"nonuniform desktop-like windows do not change native DPI");
        var corrected=new MobileControlLayout(1648,839,expected);
        Check(Near(corrected.Scale,expected/163f)&&Near(corrected.Width,1648f*163f/expected)&&Near(corrected.Height,839f*163f/expected),"corrected runtime safe dimensions use render DPI once");
        return checks+" pure actual RenderDpi boundary assertions passed; layout getter, rendering and touch not executed";
    }
    public static string Run(bool phoneOnly=false)
    {
        checks=0;
        Console.WriteLine(phoneOnly?"SCOPE: phone geometry only; one runtime safe-dimension snapshot plus specification/synthetic inputs, no rendering or touch evidence":"SCOPE: phone and existing iPad geometry; one runtime safe-dimension snapshot plus specification/synthetic inputs, no rendering or touch evidence");
        // Screen 1920x883, safeArea (136,44,1648,839), reported dpi 460 was measured
        // before the render-DPI correction. Replay both that raw input and the corrected
        // native 2622x1206/render 1920x883 density; neither is a new render/touch observation.
        float[][] devices={new[]{568f,320f,163f},new[]{844f,375f,163f},new[]{1334f,750f,326f},new[]{2208f,1080f,401f},new[]{2250f,1125f,458f},new[]{2340f,1080f,460f},new[]{2622f,1206f,460f},new[]{1648f,839f,460f},new[]{1648f,839f,MobileControlLayout.RenderDpi(460,1920,883,2622,1206)},new[]{2048f,1536f,264f},new[]{2388f,1668f,264f},new[]{2732f,2048f,264f},new[]{1280f,720f,0f}};
        foreach(var preset in new[]{-1,0,1})foreach(var d in devices)
        {
            var l=new MobileControlLayout(d[0],d[1],d[2],preset);
            if(d[0]==1648&&d[2]==460)Check(Near(l.Scale,2.621875f)&&Near(l.Width,628.5578f)&&Near(l.Height,320),"raw measured runtime input reproduces its pre-correction logical viewport once");
            if(d[0]==1648&&d[2]!=460)Check(Near(l.Scale,d[2]/163f)&&Near(l.Width,1648f*163f/d[2])&&Near(l.Height,839f*163f/d[2]),"corrected replay of measured safe dimensions applies render density once");
            var targets=new List<MobileControlLayout.Area>{l.Joystick,l.Attack,l.Dodge,l.Potion,l.Jump,l.Menu,l.Inventory,l.SkillsMenu,l.Catalog,l.Interact,l.FocusCommand,l.RecallCommand,l.SkillPage};targets.AddRange(l.Skills);
            foreach(var r in targets)
            {
                Check(r.Width>=(r.Width==44?44:48)&&r.Height>=(r.Width==44?44:48),"minimum 48 action targets; compact navigation and page controls retain 44");
                Check(r.X>=0&&r.Y>=0&&r.X+r.Width<=l.Width+.01f&&r.Y+r.Height<=l.Height+.01f,"safe-area contained controls");
            }
            foreach(var service in new[]{l.Shop,l.Smith})
            {
                Check(service.X>=0&&service.Y>=0&&service.X+service.Width<=l.Width&&service.Y+service.Height<=l.Height,"service icon remains within safe area");
                foreach(var target in targets)Check(!service.Overlaps(target),"camp service icon clears combat/navigation targets");
                Check(!service.Overlaps(l.Map)&&!service.Overlaps(l.DungeonEntrance),"service icons clear map and dungeon entry");
            }
            var navigation=new[]{l.Shop,l.Smith,l.Catalog,l.SkillsMenu,l.Inventory,l.Menu};
            for(int i=0;i<navigation.Length;i++){Check(navigation[i].Y==l.Menu.Y,"navigation is one row");if(i>0)Check(Math.Abs(navigation[i].X-navigation[i-1].X-46)<.01,"compact equal navigation spacing");}
            Check(!l.Shop.Overlaps(l.Smith),"shop and smith have independent hit targets");
            for(int i=0;i<targets.Count;i++)for(int j=i+1;j<targets.Count;j++)Check(!targets[i].Overlaps(targets[j]),"non-overlapping touch hitboxes "+i+"/"+j+" at "+d[0]);
            Check(Near(l.PlayerStatus.X,l.Width*.5f-80)&&Near(l.PlayerStatus.Y,l.Height-63)&&Near(l.PlayerStatus.Width,l.PlayerHealth.Width)&&Near(l.PlayerStatus.Height,36),"phone vitals reserve the two actually drawn rows with numbers inside their bars");
            Check(l.PlayerHealth.Width>=80&&l.PlayerHealth.Width<=160&&Near(l.PlayerHealth.Height,16)&&Near(l.PlayerEnergy.Width,l.PlayerHealth.Width)&&Near(l.PlayerEnergy.Height,16),"health and energy have equal readable bar widths");
            Check(Near(l.PlayerHealth.X,l.PlayerStatus.X)&&Near(l.PlayerHealth.Y,l.PlayerStatus.Y)&&Near(l.PlayerEnergy.X,l.PlayerStatus.X)&&Near(l.PlayerEnergy.Y,l.PlayerStatus.Y+20),"health and energy remain separate inside the vitals bounds");
            Check(!l.PlayerHealth.Overlaps(l.PlayerEnergy),"health and energy rows never overlap");
            foreach(var target in targets)Check(!l.PlayerStatus.Overlaps(target),"vitals clear all action targets");
            foreach(var hint in l.SkillOpportunities)Check(!l.PlayerStatus.Overlaps(hint),"vitals clear opportunity captions");
            Check(!l.PlayerStatus.Overlaps(l.CounterOpportunity)&&!l.PlayerStatus.Overlaps(l.ComboOpportunity),"vitals clear attack feedback");
            Check(l.CombatView.Width>=96&&l.CombatView.Height>=64,"Explicit hero/melee feedback clear window");
            foreach(var target in targets)Check(!l.CombatView.Overlaps(target),"Clear window cannot cover any action hitbox");
            foreach(var overlay in new[]{l.MoveZone,l.Notice,l.AdventureStatus,l.BossHealth,l.EncounterText,l.PlayerStatus,l.Map})Check(!l.CombatView.Overlaps(overlay),"Clear window avoids HUD and joystick zone");
            Check((l.CombatView.X+48)/l.Width>=.25f&&(l.CombatView.X+48)/l.Width<=.75f&&(l.CombatView.Y+32)/l.Height>=.25f&&(l.CombatView.Y+32)/l.Height<=.75f,"Anchor remains central without blind pan");
            Check(l.DungeonEntrance.Y==54&&l.DungeonEntrance.Height==44&&Math.Abs(l.DungeonEntrance.X+l.DungeonEntrance.Width/2-l.Width/2)<.01,"dungeon entrance is a compact top-center safe-area control");
            foreach(var target in targets)Check(!l.DungeonEntrance.Overlaps(target),"entrance does not overlap any fixed or skill control");
            VerifyPhoneArc(l,preset);
            Check(Near(l.Attack.Width,76)&&Near(l.Attack.Height,76),"basic attack retains the original phone touch diameter");
            Check(Near(l.Dodge.Width,48)&&Near(l.Dodge.Height,48)&&Near(l.Jump.Width,48)&&Near(l.Jump.Height,48),"dodge and jump retain their original phone touch diameter");
            Check(Near(l.SkillPage.Width,44)&&Near(l.SkillPage.Height,44),"phone page switch retains the original touch diameter");
            Check(l.Skills.Length==5,"four paged slots plus one fixed ultimate; passives excluded");
            Check(Near(l.Height-l.Potion.Y-l.Potion.Height,20)&&Near(l.Potion.Width,52)&&Near(l.Potion.Height,52),"potion keeps its touch size and clears the bottom XP rail");
            Check(Near(l.Potion.X+l.Potion.Width+2,l.PlayerStatus.X)&&Math.Abs(l.Potion.Y+l.Potion.Height/2-l.PlayerStatus.Y-l.PlayerStatus.Height/2)<=1.01f,"potion stays beside two-row vitals without sharing their hit area");
            Check(l.AdventureStatus.X==12&&l.AdventureStatus.Y>=l.Map.Y+l.Map.Height,"objectives follow upper-left minimap");
            Check(l.Interact.X>=l.Width-156&&!l.Interact.Overlaps(l.Potion),"context stays in its accepted right-side position away from potion");
            foreach(var hint in l.SkillOpportunities)if(hint.Width>0&&hint.Height>0)Check(hint.X>=0&&hint.Y>=0&&hint.X+hint.Width<=l.Width+.01f&&hint.Y+hint.Height<=l.Height+.01f,"phone skill hints stay inside safe-area coordinates");
            for(int h=0;h<l.SkillOpportunities.Length;h++)foreach(var target in targets)Check(!l.SkillOpportunities[h].Overlaps(target),"opportunity affordance stays outside action targets");
            Check(l.Attack.X>l.Width/2&&l.Joystick.X<l.Width/2,"separate thumb zones");
            Check(l.Cancel.X==l.Jump.X&&l.Cancel.Y==l.Jump.Y,"cancel replaces jump without additional overlap");
            foreach(var feedback in new[]{l.EncounterText,l.BossHealth,l.Notice,l.AdventureStatus})
            {
                Check(feedback.X>=0&&feedback.Y>=0&&feedback.X+feedback.Width<=l.Width&&feedback.Y+feedback.Height<=l.Height,"encounter feedback stays in safe area");
                foreach(var target in targets)Check(!feedback.Overlaps(target),"wave/boss feedback cannot cover a skill or action target");
            }
            Check(!l.AdventureStatus.Overlaps(l.BossHealth)&&!l.AdventureStatus.Overlaps(l.EncounterText),"objective card cannot cover boss bar/label");
            Check(!l.AdventureStatus.Overlaps(l.PlayerStatus)&&!l.AdventureStatus.Overlaps(l.Map),"objective remains below minimap and outside status");
            Check(!l.AdventureStatus.Overlaps(l.MoveZone)&&l.AdventureStatus.X==l.Notice.X&&l.AdventureStatus.Y==l.Notice.Y,"notices reuse the left information slot and never cover movement");
            Check(l.AdventureStatus.Width>=188&&l.AdventureStatus.Height==76,"four lines fit compact phone/iPad objective card");
            Check(!l.EncounterText.Overlaps(l.BossHealth),"wave label and boss health remain separate");
            Check(!l.Notice.Overlaps(l.MoveZone)&&l.Notice.Width>=100&&l.Notice.Height>=48,"full-message touch target stays outside the entire movement zone");
        }
        if(!phoneOnly)foreach(var d in new[]{new[]{2048f,1536f,264f},new[]{2388f,1668f,264f},new[]{2732f,2048f,264f},new[]{2266f,1488f,326f}})
        {
            var l=new MobileControlLayout(d[0],d[1],d[2],0,true);
            Check(Math.Abs(l.Map.Width-158.4f)<.01&&Math.Abs(l.Map.Height-122.4f)<.01,"iPad map dimensions are 1.8x");
            Check(Math.Abs(l.Skills[0].Width-62.4f)<.01,"iPad skill diameter is 1.3x");
            Check(Near(l.Menu.Width,52.8f)&&Near(l.Potion.Width,62.4f),"iPad navigation and potion retain their existing touch sizes");
            Check(l.Skills[4].Y+l.Skills[4].Height+9.9f<=l.SkillPage.Y,"iPad ultimate sits above page switch with a gap");
            Check(Math.Abs(l.Skills[4].X+l.Skills[4].Width*.5f-l.SkillPage.X-l.SkillPage.Width*.5f)<.01f,"iPad ultimate and switch share right column");
            Check(l.SkillPage.Width>68,"iPad page switch has enlarged hit area");
            Check(l.PlayerHealth.Width>=120&&l.PlayerHealth.Width<=184&&Near(l.PlayerHealth.Height,16),"iPad health bar remains readable while clearing its lowest skill");
            Check(Near(l.PlayerStatus.Width,l.PlayerHealth.Width)&&Near(l.PlayerStatus.Height,36)&&Near(l.PlayerEnergy.Width,l.PlayerHealth.Width)&&Near(l.PlayerEnergy.Height,16)&&Near(l.PlayerEnergy.Y-l.PlayerHealth.Y,20),"iPad vitals reserve only their two actually drawn rows");
            Check(Near(l.Jump.X,l.Width-112.2f)&&Near(l.Jump.Y,l.Height-AcceptedPadJumpTopInset)&&Near(l.Jump.Width,62.4f)&&Near(l.Jump.Height,62.4f)&&Near(l.Cancel.X,l.Jump.X)&&Near(l.Cancel.Y,l.Jump.Y),"iPad jump retains its platform placement and cancel replacement");
            Check(Near(l.FocusCommand.X,l.Potion.X+l.Potion.Width+8)&&Near(l.RecallCommand.X,l.FocusCommand.X)&&Near(l.FocusCommand.Width,48)&&Near(l.RecallCommand.Width,48),"iPad companion commands retain their touch size beside the potion");
            Check(l.Width-l.Dodge.X-l.Dodge.Width>=30,"iPad combat cluster has right inset");
            var targets=new List<MobileControlLayout.Area>{l.Attack,l.Dodge,l.Jump,l.Potion,l.SkillPage,l.Menu,l.Inventory,l.SkillsMenu,l.Catalog,l.Shop,l.Smith,l.Interact,l.Joystick,l.FocusCommand,l.RecallCommand};targets.AddRange(l.Skills);
            for(int i=0;i<targets.Count;i++)
            {
                var r=targets[i];Check(r.X>=0&&r.Y>=0&&r.X+r.Width<=l.Width&&r.Y+r.Height<=l.Height,"iPad targets stay in safe area");
                for(int j=i+1;j<targets.Count;j++)Check(!r.Overlaps(targets[j]),"iPad targets do not overlap "+i+"/"+j+" at "+d[0]);
                foreach(var hud in new[]{l.Map,l.PlayerStatus,l.AdventureStatus,l.DungeonEntrance})Check(!r.Overlaps(hud),"iPad shared HUD clears controls "+i+" at "+d[0]);
                // Shop/Smith are camp-only. Wave text and the boss bar belong to
                // dungeon combat, so they never share a visible context with these services.
                if(i!=9&&i!=10)foreach(var combatHud in new[]{l.EncounterText,l.BossHealth})Check(!r.Overlaps(combatHud),"iPad combat HUD clears controls "+i+" at "+d[0]);
            }
        }
        foreach(bool ipad in phoneOnly?new[]{false}:new[]{false,true})foreach(var d in devices)
        {
            if(ipad&&(d[0]<2000||d[0]/d[1]>1.65f))continue;
            var l=new MobileControlLayout(d[0],d[1],d[2],0,ipad);
            var reserved=new List<MobileControlLayout.Area>{l.Map,l.EncounterText,l.BossHealth,l.Menu,l.Inventory,l.SkillsMenu,l.Catalog,l.Shop,l.Smith,l.Interact,l.Attack,l.Dodge,l.Jump,l.SkillPage,l.PlayerStatus,l.Potion};reserved.AddRange(l.Skills);
            Check(l.LootNotice(0).Width>0,"at least one pickup notice fits at "+d[0]+" ipad="+ipad);
            for(int i=0;i<2;i++)
            {
                var notice=l.LootNotice(i);if(notice.Width<=0)continue;
                foreach(var r in reserved)Check(!notice.Overlaps(r),"loot notice clears wave, map and controls");
                Check(notice.X>=0&&notice.Y>=0&&notice.X+notice.Width<=l.Width+.01f&&notice.Y+notice.Height<=l.Height+.01f,"pickup notice stays in the safe viewport");
                if(!ipad)
                {
                    Check(Near(notice.Width,174)&&Near(notice.Height,44),"mobile pickup retains the full content card");
                    foreach(var r in new[]{l.MoveZone,l.Joystick,l.FocusCommand,l.RecallCommand,l.CombatView,l.CounterOpportunity,l.ComboOpportunity,l.DungeonEntrance})Check(!notice.Overlaps(r),"phone pickup clears movement, companion commands and combat feedback");
                    foreach(var r in l.SkillOpportunities)Check(!notice.Overlaps(r),"phone pickup clears every skill caption");
                    if(notice.Overlaps(l.AdventureStatus))
                    {
                        Check(i==0&&Near(notice.X,12)&&Near(notice.Y,92)&&notice.Y+notice.Height<=l.AdventureStatus.Y+l.AdventureStatus.Height,"compact pickup owns only the shared left information slot");
                        Check(l.LootNotice(1).Width==0,"shared information slot admits one full pickup card at a time");
                    }
                    else Check(!notice.Overlaps(l.Notice),"separate pickup leaves the message information slot clear");
                }
                Check(notice.Y>=l.Map.Y+l.Map.Height+12,"pickup notices stay below minimap");
                if(i>0){var previous=l.LootNotice(i-1);Check(notice.X==previous.X&&notice.Y>=previous.Y+previous.Height+8,"queued notices stack downward");}
            }
            if(!ipad&&d[0]==568)Check(l.LootNotice(0).Overlaps(l.AdventureStatus),"compact 568 phone uses the information replacement fallback");
            if(!ipad&&d[0]==844)Check(!l.LootNotice(0).Overlaps(l.AdventureStatus),"wide phone has a separate pickup position");
        }
        Check(MobileControlLayout.DeadZone(.1f)==0,"deadzone rejects drift");
        Check(MobileControlLayout.DeadZone(1)==1&&MobileControlLayout.DeadZone(-2)==-1,"movement saturates safely");
        return checks+(phoneOnly?" phone-only control geometry assertions passed":" phone and iPad control geometry assertions passed");
    }
}

using System;

namespace Emberfall
{
    /// <summary>Pure geometry in density-independent touch units. No gameplay state or device APIs.</summary>
    public sealed class MobileControlLayout
    {
        public struct Area
        {
            public float X, Y, Width, Height;
            public Area(float x, float y, float w, float h) { X=x;Y=y;Width=w;Height=h; }
            public bool Contains(float x,float y) { return x>=X&&x<X+Width&&y>=Y&&y<Y+Height; }
            public bool Overlaps(Area r) { return X<r.X+r.Width&&X+Width>r.X&&Y<r.Y+r.Height&&Y+Height>r.Y; }
        }
        public readonly float Scale, Width, Height, UiZoom;
        public readonly bool Tablet;
        private readonly bool padControls;
        public readonly Area Joystick, MoveZone, Attack, Dodge, Potion, Jump, Cancel, Menu, Inventory, SkillsMenu, Catalog, Interact, SkillPage, DungeonEntrance, Shop, Smith;
        public readonly Area EncounterText, BossHealth, Notice, AdventureStatus, FocusCommand, RecallCommand, CombatView, PlayerStatus, PlayerHealth, PlayerEnergy, Map;
        public readonly Area[] Skills = new Area[5];
        public readonly Area[] SkillOpportunities = new Area[10];
        public readonly Area CounterOpportunity, ComboOpportunity;
        public MobileControlLayout(float pixelWidth,float pixelHeight,float dpi,int positionPreset=0, bool ipad=false, float[] skillOffsets=null)
        {
            pixelWidth=Math.Max(1,pixelWidth);pixelHeight=Math.Max(1,pixelHeight);
            padControls=ipad;
            Tablet=pixelWidth/pixelHeight<1.65f;
            float fallback=pixelHeight/(Tablet?768f:390f);
            // Screen.dpi is advisory; reject missing/implausible values and constrain
            // density so 320-point compact phones still fit the full combat cluster.
            float density=dpi>=120&&dpi<=700?dpi/163f:fallback;
            float nativeScale=Math.Max(.25f,Math.Min(density,Math.Min(pixelHeight/320f,pixelWidth/568f)));
            // One device factor for rendering, fonts and input. Preserve the minimum
            // landscape viewport instead of enlarging controls beyond its edges.
            UiZoom=ipad?Math.Max(1f,Math.Min(1.4f,Math.Min(pixelWidth/(568f*nativeScale),pixelHeight/(320f*nativeScale)))):1f;
            Scale=nativeScale*UiZoom;
            Width=pixelWidth/Scale;Height=pixelHeight/Scale;
            Joystick=Centered(90,Height-86,128);
            float moveTop=Math.Max(164,Height-172);
            MoveZone=new Area(12,moveTop,175,Height-12-moveTop);
            Attack=Centered(Width-125,Height-55,76);
            Dodge=Centered(Width-30,Height-30,48);
            Jump=Centered(Width-30,Height-99,48);Cancel=Jump;
            float bottomLift=Width<700?36:0;
            Potion=Centered(Width*.5f-108,Height-30,52);
            // Width already excludes Screen.safeArea insets. Never subtract them again.
            Menu=Centered(Width-28,32,44);Inventory=Centered(Width-166,32,44);
            SkillsMenu=Centered(Width-74,32,44);Catalog=Centered(Width-120,32,44);
            Shop=Centered(Width-258,32,44);Smith=Centered(Width-212,32,44);
            Interact=ipad?new Area(Width-54,92,48,48):new Area(Width-156,56,48,48);
            DungeonEntrance=new Area(Width*.5f-58,54,116,44);
            float shift=positionPreset<0?-Math.Min(8,Math.Max(0,Height-320)):positionPreset>0?0:0;
            // Equal-size skills follow a 30-degree arc with one shared chord length.
            const float radius=132;float diameter=ipad?48:52;
            float chord=2*radius*(float)Math.Sin(Math.PI/12);
            for(int i=0;i<4;i++)
            {
                double angle=i*Math.PI/6;
                Skills[i]=Centered(Width-78-radius*(float)Math.Cos(angle),Height-30-radius*(float)Math.Sin(angle)+shift-bottomLift,diameter);
            }
            Skills[4]=Centered(Width-78-radius-chord,Height-30+shift-bottomLift,diameter);
            SkillPage=Centered(Width-24,Height-158,44);
            if(ipad)
            {
                Attack=ScalePadCombat(Attack);Dodge=ScalePadCombat(Dodge);Jump=ScalePadCombat(Jump);
                // Separate the existing iPad jump hitbox from the end of its skill arc.
                Jump=new Area(Jump.X,Jump.Y+8,Jump.Width,Jump.Height);Cancel=Jump;
                // Give the left skill arc more breathing room; keep a 30.9-unit gap to the right controls.
                Attack=new Area(Math.Min(Attack.X+12,Dodge.X-Attack.Width-30.9f),Attack.Y,Attack.Width,Attack.Height);
                Area page=ScalePadCombat(SkillPage);
                SkillPage=Centered(Width-45,page.Y+page.Height*.5f,68.64f);
                // Move the arc inward as one unit, preserving equal button spacing.
                for(int i=0;i<Skills.Length;i++)
                {
                    Area skill=ScalePadCombat(Skills[i]);skill.X+=10;Skills[i]=skill;
                }
                // Ultimate stays fixed above the page switch, freeing the bottom HUD.
                Skills[4]=Centered(SkillPage.X+SkillPage.Width*.5f,SkillPage.Y-10-31.2f,62.4f);
                Menu=Centered(Width-38,38,52.8f);Inventory=Centered(Width-203.6f,38,52.8f);
                SkillsMenu=Centered(Width-93.2f,38,52.8f);Catalog=Centered(Width-148.4f,38,52.8f);
                Smith=Centered(Width-258.8f,38,52.8f);Shop=Centered(Width-314,38,52.8f);
                DungeonEntrance=new Area(Width*.5f-58,76,116,44);
                Potion=Centered(Width*.5f-151,Height-34,62.4f);
            }
            // Align the lowest iPad skill with the bottom edge of the energy strip.
            if(ipad)
            {
                float lift=Height-18-(Skills[0].Y+Skills[0].Height);
                for(int i=0;i<4;i++){Area skill=Skills[i];skill.Y+=lift;Skills[i]=skill;}
            }
            else
            {
                // Ordinary skills curve around the upper/left side of attack.
                // The compact right column keeps ultimate, page, jump and dodge
                // together at the lower-right without the old narrow-phone lift.
                Attack=Centered(Width-125,Height-55,76);
                Dodge=Centered(Width-30,Height-30+shift,48);
                Jump=Centered(Width-30,Height-85+shift,48);Cancel=Jump;
                SkillPage=Centered(Width-30,Height-138+shift,44);
                const float phoneRadius=144,phoneDiameter=52;
                for(int i=0;i<4;i++)
                {
                    double angle=i*Math.PI/6;
                    Skills[i]=Centered(Width-90-phoneRadius*(float)Math.Cos(angle),Height-32-phoneRadius*(float)Math.Sin(angle)+shift,phoneDiameter);
                }
                Skills[4]=Centered(Width-30,Height-199.2f+shift,62.4f);
            }
            // Keep the complete combat cluster clear of right and bottom edges.
            const float edgeInset=18, bottomInset=12;
            Attack=new Area(Attack.X-edgeInset,Attack.Y-bottomInset,Attack.Width,Attack.Height);
            Dodge=new Area(Dodge.X-edgeInset,Dodge.Y-bottomInset,Dodge.Width,Dodge.Height);
            Jump=new Area(Jump.X-edgeInset,Jump.Y-bottomInset,Jump.Width,Jump.Height);Cancel=Jump;
            SkillPage=new Area(SkillPage.X-edgeInset,SkillPage.Y-bottomInset,SkillPage.Width,SkillPage.Height);
            for(int i=0;i<Skills.Length;i++){var skill=Skills[i];Skills[i]=new Area(skill.X-edgeInset,skill.Y-bottomInset,skill.Width,skill.Height);}
            // iPad basic attack sits slightly lower/right inside the surrounding skill arc.
            Attack=new Area(Attack.X+(ipad?8:0),Math.Min(Attack.Y+(ipad?20:6),Height-20-Attack.Height),Attack.Width,Attack.Height);
            if(ipad)
            {
                var ultimate=Skills[4];
                Skills[4]=Centered(ultimate.X+ultimate.Width*.5f,ultimate.Y+ultimate.Height*.5f,ultimate.Width*1.15f);
            }
            int[] opportunityIdentities={0,1,2,4,5,6,7,9};
            for(int index=0;index<opportunityIdentities.Length;index++)
            {
                int skill=opportunityIdentities[index],button=index==7?4:index%4;
                SkillOpportunities[skill]=SkillOpportunityForButton(button);
            }
            CounterOpportunity=new Area(Attack.X,Attack.Y+Attack.Height+1,Attack.Width/2,13);
            ComboOpportunity=new Area(Attack.X+Attack.Width/2,Attack.Y+Attack.Height+1,Attack.Width/2,13);
            float commandLift=Width<700?22:0;
            float commandX=ipad?Potion.X+Potion.Width+8:204;
            FocusCommand=new Area(commandX,Height-170-commandLift,48,48);RecallCommand=new Area(commandX,Height-120-commandLift,48,48);
            Map=new Area(12,12,ipad?158.4f:88,ipad?122.4f:68);
            // Reserve the bottom XP rail even on devices with no bottom safe-area inset.
            Potion=new Area(Potion.X,Math.Min(Potion.Y,Height-20-Potion.Height),Potion.Width,Potion.Height);
            // A compact phone can otherwise put the potion four units into the thumb disc.
            if(!ipad&&Joystick.Overlaps(Potion))Joystick=Centered(Math.Min(90,Potion.X-68),Height-86,128);
            AdventureStatus=new Area(12,ipad?140.4f:86,188,76);
            // Transient notices replace this left-side information slot, never the battlefield.
            Notice=AdventureStatus;
            float phoneHealthWidth=Math.Min(160,Math.Max(80,Math.Min(Attack.X-58,Skills[0].X-8)-(Width*.5f-80)));
            float vitalX=Width*.5f-(ipad?112:80),vitalY=Height-(ipad?58:63);
            // Numbers stay inside two full-width bars. Compact iPads can lack
            // the bottom gap; use the free strip above movement in that case.
            float vitalWidth=ipad?Math.Min(184,Math.Max(120,Skills[0].X-8-vitalX)):phoneHealthWidth;
            if(ipad&&new Area(vitalX,vitalY,vitalWidth,36).Overlaps(Skills[0]))
            {
                vitalX=Math.Max(12,FocusCommand.X-vitalWidth-8);vitalY=MoveZone.Y-44;
                if(vitalY<AdventureStatus.Y+AdventureStatus.Height+8)
                {vitalX=AdventureStatus.X+AdventureStatus.Width+8;vitalY=DungeonEntrance.Y+DungeonEntrance.Height+8;}
            }
            PlayerStatus=new Area(vitalX,vitalY,vitalWidth,36);
            PlayerHealth=new Area(PlayerStatus.X,PlayerStatus.Y,vitalWidth,16);
            PlayerEnergy=new Area(PlayerStatus.X,PlayerStatus.Y+20,vitalWidth,16);
            // Keep wave text above the boss bar and clear of the raised skill arc.
            EncounterText=new Area(Map.X+Map.Width+12,12,164,18);float bossWidth=Math.Min(280,Math.Max(48,Math.Min(Width*.3f,2*(Catalog.X-Width*.5f-12))));
            BossHealth=new Area(Width*.5f-bossWidth*.5f,32,bossWidth,6);
            if(skillOffsets!=null&&skillOffsets.Length==2)
            {
                float dx=skillOffsets[0]*Width,dy=skillOffsets[1]*Height;
                if(!float.IsNaN(dx)&&!float.IsInfinity(dx)&&!float.IsNaN(dy)&&!float.IsInfinity(dy)&&(dx!=0||dy!=0))
                {
                    var bounds=CombatButtonBounds();
                    dx=Math.Max(8-bounds.X,Math.Min(Width-8-bounds.X-bounds.Width,dx));
                    dy=Math.Max(8-bounds.Y,Math.Min(Height-20-bounds.Y-bounds.Height,dy));
                    for(int i=0;i<Skills.Length;i++)Skills[i]=Translate(Skills[i],dx,dy);
                    Attack=Translate(Attack,dx,dy);Dodge=Translate(Dodge,dx,dy);Jump=Translate(Jump,dx,dy);Cancel=Jump;
                    SkillPage=Translate(SkillPage,dx,dy);
                    CounterOpportunity=Translate(CounterOpportunity,dx,dy);ComboOpportunity=Translate(ComboOpportunity,dx,dy);
                    for(int i=0;i<opportunityIdentities.Length;i++)SkillOpportunities[opportunityIdentities[i]]=SkillOpportunityForButton(i==7?4:i%4);
                }
            }
            CombatView=ChooseCombatView(Width*.5f-12,Height*.3f-32,96,64);
        }

        private static Area Translate(Area area,float dx,float dy)
        {return new Area(area.X+dx,area.Y+dy,area.Width,area.Height);}
        public Area[] CombatButtons()
        {return new[]{Skills[0],Skills[1],Skills[2],Skills[3],Skills[4],Attack,Dodge,Jump,SkillPage};}
        public Area CombatButtonBounds()
        {
            float x=Width,y=Height,right=0,bottom=0;
            foreach(var r in CombatButtons()){x=Math.Min(x,r.X);y=Math.Min(y,r.Y);right=Math.Max(right,r.X+r.Width);bottom=Math.Max(bottom,r.Y+r.Height);}
            return new Area(x,y,right-x,bottom-y);
        }
        public bool CanPlaceCombatButtons()
        {
            foreach(var r in CombatButtons())
                foreach(var fixedArea in new[]{Potion,PlayerStatus,Map,AdventureStatus,Menu,Inventory,SkillsMenu,Catalog,Shop,Smith,Interact,FocusCommand,RecallCommand,MoveZone,DungeonEntrance})
                    if(r.Overlaps(fixedArea))return false;
            return true;
        }

        // Screen.dpi describes native pixels even when mobile rendering is reduced.
        // Convert it to the current render grid before measuring touch/UI geometry.
        public static float RenderDpi(float nativeDpi,float renderWidth,float renderHeight,float nativeWidth,float nativeHeight)
        {
            if(nativeDpi<120||nativeDpi>700||renderWidth<=0||renderHeight<=0||nativeWidth<=0||nativeHeight<=0)return nativeDpi;
            if((renderWidth>renderHeight)!=(nativeWidth>nativeHeight))
            {float swap=nativeWidth;nativeWidth=nativeHeight;nativeHeight=swap;}
            float widthRatio=renderWidth/nativeWidth,heightRatio=renderHeight/nativeHeight;
            // A desktop window or inconsistent display report is not a scaled render grid.
            if(Math.Abs(widthRatio-heightRatio)>Math.Max(widthRatio,heightRatio)*.05f)return nativeDpi;
            return nativeDpi*Math.Min(widthRatio,heightRatio);
        }

        private Area ScalePadCombat(Area r)
        {return new Area(Width-24-(Width-r.X)*1.3f,Height-36-(Height-r.Y)*1.3f,r.Width*1.3f,r.Height*1.3f);}

        public Area SkillOpportunityForButton(int button)
        {
            if(button<0||button>=Skills.Length)return new Area();
            Area key=Skills[button];float y=key.Y-(button==0?14:15);
            // A high arc button keeps its caption below, clear of top HUD.
            if(button==3&&y<140)y=key.Y+key.Height+1;
            return new Area(key.X,y,key.Width,14);
        }

        private readonly Area[] lootNoticeAreas=new Area[2];
        private bool lootNoticesMeasured;
        public Area LootNotice(int index)
        {
            if(index<0||index>=lootNoticeAreas.Length)return new Area();
            if(lootNoticesMeasured)return lootNoticeAreas[index];
            lootNoticesMeasured=true;
            var placed=new System.Collections.Generic.List<Area>();
            float w=Math.Min(174,Width*.35f);
            for(float y=Math.Max(Map.Y+Map.Height,Math.Max(EncounterText.Y+EncounterText.Height,BossHealth.Y+BossHealth.Height))+12;y+44<Height-70;y+=8)
            for(float x=Width-w-12;x>=Math.Max(Map.X+Map.Width+12,Width*.3f);x-=12)
            {
                if(placed.Count>0&&(x!=placed[0].X||y<placed[placed.Count-1].Y+placed[placed.Count-1].Height+8))continue;
                Area candidate=new Area(x,y,w,44);bool blocked=!ClearLootNotice(candidate,false);
                if(!blocked)foreach(var r in placed)if(candidate.Overlaps(new Area(r.X-4,r.Y-4,r.Width+8,r.Height+8))){blocked=true;break;}
                if(blocked)continue;
                placed.Add(candidate);lootNoticeAreas[placed.Count-1]=candidate;if(placed.Count==lootNoticeAreas.Length)return lootNoticeAreas[index];
            }
            if(placed.Count==0&&!padControls)
            {
                // On compact phones the full-size pickup card temporarily owns
                // the information slot below the map instead of covering combat.
                Area fallback=new Area(AdventureStatus.X,Math.Max(AdventureStatus.Y,Map.Y+Map.Height+12),w,44);
                if(fallback.Y+fallback.Height<=AdventureStatus.Y+AdventureStatus.Height&&ClearLootNotice(fallback,true))lootNoticeAreas[0]=fallback;
            }
            return lootNoticeAreas[index];
        }

        private bool ClearLootNotice(Area candidate,bool replacesInformation)
        {
            if(candidate.X<0||candidate.Y<0||candidate.X+candidate.Width>Width||candidate.Y+candidate.Height>Height)return false;
            foreach(var r in new[]{Map,EncounterText,BossHealth,Menu,Inventory,SkillsMenu,Catalog,Shop,Smith,Interact,Attack,Dodge,Jump,SkillPage,PlayerStatus,Potion})if(candidate.Overlaps(r))return false;
            foreach(var r in Skills)if(candidate.Overlaps(r))return false;
            if(!padControls)
            {
                foreach(var r in new[]{MoveZone,Joystick,FocusCommand,RecallCommand,CombatView,CounterOpportunity,ComboOpportunity,DungeonEntrance})if(candidate.Overlaps(r))return false;
                foreach(var r in SkillOpportunities)if(candidate.Overlaps(r))return false;
                if(!replacesInformation&&(candidate.Overlaps(AdventureStatus)||candidate.Overlaps(Notice)))return false;
            }
            return true;
        }

        private Area ChooseCombatView(float x,float y,float w,float h)
        {
            Area best=new Area(x+(w-96)/2,y+(h-64)/2,96,64);float bestScore=float.MaxValue;
            // The compact gap between the entrance and companion controls can
            // be narrower than the coarse search step, so test its exact edge.
            Area compactGap=new Area(FocusCommand.X+FocusCommand.Width,DungeonEntrance.Y+DungeonEntrance.Height,96,64);
            if(!padControls&&ClearView(compactGap))best=compactGap;
            for(int i=-1;i<121;i++)
            {
                float cx=i<0?best.X+48:Width*(.25f+(i%11)*.05f),cy=i<0?best.Y+32:Height*(.25f+(i/11)*.05f);
                // Preserve the HUD clearance on compact 320-unit-high viewports.
                // The lowest grid row otherwise starts at Y=48 and is rejected,
                // even though moving its top to Y=56 leaves a valid central gap.
                cy=Math.Max(88,cy);
                Area candidate=new Area(cx-48,cy-32,96,64);
                if(!ClearView(candidate))continue;
                float dx=cx/Width-.5f,dy=cy/Height-.5f,score=dx*dx+dy*dy;
                if(score<bestScore){best=candidate;bestScore=score;}
            }
            return best;
        }
        private bool ClearView(Area area)
        {
            if(area.X<0||area.Y<56||area.X+area.Width>Width||area.Y+area.Height>Height)return false;
            foreach(var control in new[]{MoveZone,Attack,Dodge,Potion,Jump,Menu,Inventory,SkillsMenu,Catalog,Interact,FocusCommand,RecallCommand,Notice,BossHealth,EncounterText,AdventureStatus,PlayerStatus,Map,SkillPage,DungeonEntrance,Shop,Smith})
                if(area.Overlaps(control))return false;
            foreach(var skill in Skills)if(area.Overlaps(skill))return false;
            foreach(var hint in SkillOpportunities)if(area.Overlaps(hint))return false;
            if(area.Overlaps(CounterOpportunity)||area.Overlaps(ComboOpportunity))return false;
            return true;
        }
        private static Area Centered(float x,float y,float size) { return new Area(x-size/2,y-size/2,size,size); }
        public static float DeadZone(float value,float dead=.14f)
        { float magnitude=Math.Abs(value);return magnitude<=dead?0:Math.Sign(value)*Math.Min(1,(magnitude-dead)/(1-dead)); }
    }
}

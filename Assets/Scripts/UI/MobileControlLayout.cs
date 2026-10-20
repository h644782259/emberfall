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
        public readonly Area[] Skills = new Area[9];
        public readonly Area[] SkillOpportunities = new Area[11];
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
            // Four small skills in ascending order (ultimate last), with five full-size
            // skills following a shared arc around the primary attack button.
            // Fit the whole cluster to the usable viewport, rather than enlarging all
            // tablet controls or using a fixed phone size. Scale hit areas together
            // with their artwork so neighboring buttons remain distinct.
            float group=Math.Max(.68f,Math.Min(1.1f,Math.Min(Height/528f,(Width*.38f-20)/285f)));
            float attackX=Width-112*group,attackY=Height-106*group;
            Attack=Centered(attackX,attackY,76*group);
            for(int i=0;i<5;i++)
            {
                double angle=(-40-i*45)*Math.PI/180;
                Skills[3+i]=Centered(attackX+85*group*(float)Math.Cos(angle),attackY+85*group*(float)Math.Sin(angle),52*group);
            }
            // The two leftmost skills form a straight column at the bottom.
            Skills[7]=new Area(Skills[6].X,Skills[7].Y,Skills[7].Width,Skills[7].Height);
            // Move the former upper-right skill beside the lowest skill.
            Skills[3]=new Area(Skills[7].X-62*group,Skills[7].Y,Skills[7].Width,Skills[7].Height);
            // Keep the small row attached to the combat arc. The midpoint is an
            // upper travel limit for its bottom edge, not a fixed row anchor.
            float arcTop=Skills[3].Y;
            for(int i=4;i<8;i++)arcTop=Math.Min(arcTop,Skills[i].Y);
            float topRowBottom=Math.Max(Height*.5f,arcTop-10*group);
            float topRowY=topRowBottom-21*group;
            Skills[8]=Centered(Width-56*group,topRowY,42*group);
            for(int i=0;i<3;i++)Skills[i]=Centered(Width-(230-i*58)*group,topRowY,42*group);
            Jump=Centered(Width-45*group,attackY-43*group,54*group);
            Dodge=Centered(Width-45*group,attackY+30*group,54*group);Cancel=Jump;
            // Align the top row with the outer edge of the mobility buttons.
            float topRowShift=Jump.X+Jump.Width-(Skills[8].X+Skills[8].Width);
            Skills[8]=Translate(Skills[8],topRowShift,0);
            for(int i=0;i<3;i++)Skills[i]=Translate(Skills[i],topRowShift,0);
            SkillPage=new Area(); // Retained as an empty compatibility field; no hit target.
            float potionSize=ipad?62.4f:52;
            Potion=Centered(Width*.5f-(ipad?112:92)-12-potionSize*.5f,Height-34,potionSize);
            Menu=Centered(Width-28,32,44);Inventory=Centered(Width-166,32,44);
            SkillsMenu=Centered(Width-74,32,44);Catalog=Centered(Width-120,32,44);
            Shop=Centered(Width-258,32,44);Smith=Centered(Width-212,32,44);
            Interact=new Area(Skills[3].X-56,Attack.Y-56,48,48);
            DungeonEntrance=new Area(Width*.5f-58,54,116,44);
            int[] opportunityIdentities={0,1,2,4,5,6,7,10,9};
            for(int index=0;index<opportunityIdentities.Length;index++)
            {
                int skill=opportunityIdentities[index],button=index;
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
            float phoneHealthWidth=Math.Min(160,Math.Max(80,Math.Min(Attack.X-58,Skills[1].X-8)-(Width*.5f-80)));
            float vitalX=Width*.5f-(ipad?112:92),vitalY=Height-(ipad?58:63);
            // Keep the vital bars clear of the two new lower-left skills.
            float vitalWidth=Math.Min(ipad?184:phoneHealthWidth,Skills[3].X-8-vitalX);
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
                    for(int i=0;i<opportunityIdentities.Length;i++)SkillOpportunities[opportunityIdentities[i]]=SkillOpportunityForButton(i);
                }
            }
            CombatView=ChooseCombatView(Width*.5f-12,Height*.3f-32,96,64);
        }

        private static Area Translate(Area area,float dx,float dy)
        {return new Area(area.X+dx,area.Y+dy,area.Width,area.Height);}
        public Area[] CombatButtons()
        {var buttons=new Area[Skills.Length+3];Array.Copy(Skills,buttons,Skills.Length);buttons[Skills.Length]=Attack;buttons[Skills.Length+1]=Dodge;buttons[Skills.Length+2]=Jump;return buttons;}
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

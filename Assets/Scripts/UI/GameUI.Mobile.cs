using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameUI
    {
        private readonly MobileSkillTap mobileTap=new MobileSkillTap();
        private int mobileCastFinger {get{return mobileTap.Finger;}}
        private float TouchRatio { get { return MobileControls.Layout.Scale/scale; } }
        private Rect TouchRect(MobileControlLayout.Area a) { float r=TouchRatio;return new Rect(a.X*r,a.Y*r,a.Width*r,a.Height*r); }
        private Rect TouchRect(float x,float y,float w,float h) { return TouchRect(new MobileControlLayout.Area(x,y,w,h)); }
        private int TouchFont(float size) { return Mathf.RoundToInt(size*TouchRatio); }
        private bool MobileButton(MobileControlLayout.Area area,string label,Color color)
        { Rect r=TouchRect(area);blockedRects.Add(r);return Button(r,label,color); }
        private bool MobileIcon(MobileControlLayout.Area area,string icon,Color color)
        {
            Rect r=TouchRect(area);blockedRects.Add(r);
            float size=28*TouchRatio;DrawIcon(new Rect(r.center.x-size*.5f,r.center.y-size*.5f,size,size),UIIconAtlas.Utility(icon),color);
            Badge(r,icon=="inventory"?NewEquipmentAttention||Attention.LootPending:icon=="skills"?Attention.Skills:icon=="achievement"?CachedAchievementAttention:false);
            return GUI.Button(r,GUIContent.none,invisibleButton);
        }

        private int mobileSkillPage, mobileSkillPageFrame=-1;
        public bool MobileSkillVisible(int skill)
        {if(session==null||!CombatSkillLearned(session.Progression.Profile,skill))return false;for(int button=0;button<MobileSkillPolicy.ButtonCount;button++)if(BoundMobileSkill(button,mobileSkillPage)==skill)return true;return false;}
        public void CancelMobileCast(){mobileTap.Cancel();}
        private bool BeginMobileCast(int finger,Vector2 screen)
        {
            if(session.InputBlocked||panel!=Panel.None||session.Player==null)return false;
            Vector2 p=ScreenToUI(screen);
            if(TouchRect(MobileControls.Layout.SkillPage).Contains(p)){if(mobileSkillPageFrame!=Time.frameCount){CancelMobileCast();mobileSkillPage=(mobileSkillPage+1)%MobileSkillPolicy.PageCount;mobileSkillPageFrame=Time.frameCount;}return true;}
            if(mobileTap.Active)return false;
            for(int i=0;i<MobileSkillPolicy.ButtonCount;i++)
            {
                if(!hotbarSlots[i].Contains(p))continue;
                int skill=BoundMobileSkill(i,mobileSkillPage);
                if(!CombatSkillLearned(session.Progression.Profile,skill))return false;
                mobileTap.Begin(finger,skill);return true;
            }
            return false;
        }
        private void ContinueMobileCast(int finger,Vector2 screen,bool ended,bool cancelled)
        {
            if(finger!=mobileTap.Finger)return;
            if(cancelled||!MobileControls.SafeArea.Contains(screen)||session.InputBlocked||panel!=Panel.None||session.Player==null)
            {CancelMobileCast();return;}
            if(!ended)return;
            bool inside=false;Vector2 point=ScreenToUI(screen);
            for(int i=0;i<MobileSkillPolicy.ButtonCount;i++)if(hotbarSlots[i].Contains(point)&&BoundMobileSkill(i,mobileSkillPage)==mobileTap.Skill&&CombatSkillLearned(session.Progression.Profile,mobileTap.Skill))inside=true;
            int skill;
            if(mobileTap.Release(finger,inside,false,out skill))
            {var targeting=session.Player.GetComponent<SkillTargetingController>();
                if(targeting!=null&&!targeting.Begin(skill))
                {if(string.IsNullOrEmpty(session.ControlFailure("skill"+skill)))session.ReportControlFailure("skill"+skill,"暂不可用");}}
        }
        public bool MobileDungeonEntranceVisible {get{return session!=null&&!session.PracticeActive&&!session.InDungeon&&!session.InputBlocked&&!session.DungeonSelectionOpen&&!session.NearChapterExit&&!session.NearRoomExit&&!session.SideEventAvailable&&(session.IsNearDungeonEntrance||session.IsNearChapterEntrance)&&session.Progression.CanEnterDungeon;}}
        public MobileControlLayout.Area MobileInteractionArea
        {
            get
            {
                var layout=MobileControls.Layout;
                if(MobileDungeonEntranceVisible)return layout.DungeonEntrance;
                if(session!=null&&(session.NearRoomExit||session.NearChapterExit))return new MobileControlLayout.Area(layout.Width*.5f-90,52,180,44);
                if(session!=null&&(session.SideEventAvailable||session.NearDungeonReturn))return new MobileControlLayout.Area(layout.Width*.5f-66,layout.Height*.62f-22,132,44);
                return layout.Interact;
            }
        }
        public bool MobileInteractionVisible {get{return session!=null&&!session.PracticeActive&&(session.NearDungeonReturn||session.NearChapterExit||session.NearRoomExit||session.SideEventAvailable||MobileDungeonEntranceVisible);}}
        private bool CanMobileInteract {get{return session!=null&&!session.PracticeActive&&!session.InputBlocked&&!session.DungeonSelectionOpen&&(session.NearDungeonReturn||session.NearChapterExit||session.NearRoomExit||session.SideEventAvailable||session.InDungeon||(session.IsNearDungeonEntrance||session.IsNearChapterEntrance));}}
        public void ActivateMobileInteraction(int triggeringFinger=TouchReleaseLatch.AnyPointer)
        {
            if(!CanMobileInteract||UITransitionBlocked)return;
            try
            {
                if(MobileDungeonEntranceVisible)session.EnterDungeon();
                else if(session.NearDungeonReturn)OpenDungeonExit();
                else if(session.NearChapterExit)session.EnterNextChapterRoom();
                else if(session.NearRoomExit)session.EnterNextRoom();
                else if(session.SideEventAvailable)session.StartSideEvent();
                else if(session.InDungeon)session.Notify("靠近返营传送点后交互。");
                else session.EnterDungeon();
            }
            finally{BlockUITransitionForFinger(triggeringFinger);}
        }
        private void LeaveMobilePauseForCamp()
        {
            bool paused=session.Paused, practice=session.PracticeActive, completed=false;
            var player=session.Player;
            int epoch=player==null?0:player.CombatEpoch;
            try
            {
                session.ReturnToCamp();
                completed=session.Player!=player || player!=null&&player.CombatEpoch!=epoch || practice&&!session.PracticeActive;
            }
            finally { session.SetPaused(completed?false:paused); }
        }
        private void LeaveMobilePauseForDungeon()
        {
            bool paused=session.Paused, completed=false;
            try
            {
                session.SetPaused(false);
                session.EnterDungeon();
                completed=session.DungeonSelectionOpen;
            }
            finally { if(!completed)session.SetPaused(paused); }
        }
        private void DrawMobileHUD()
        {
            var l=MobileControls.Layout;GameProfile p=session.Progression.Profile;
            DrawMobileVitals(l);
            bool hudEnabled=GUI.enabled;GUI.enabled=hudEnabled&&CanSwitchFunction&&!UITransitionBlocked;
            if(MobileIcon(l.Inventory,"inventory",jade))TogglePanel(Panel.Inventory);
            if(MobileIcon(l.SkillsMenu,"skills",p.skillPoints>0?gold:jade))TogglePanel(Panel.Skills);
            GUI.enabled=hudEnabled;
            if(MobileIcon(l.Menu,"settings",pale))session.SetPaused(true);
            GUI.enabled=hudEnabled&&CanSwitchFunction&&!UITransitionBlocked;
            if(MobileIcon(l.Catalog,"achievement",gold))OpenProgressionGoals();
            if(HubServicesAvailable)
            {
                if(MobileIcon(l.Shop,"shop",gold))OpenHubService(HubNpcKind.Merchant);
                if(MobileIcon(l.Smith,"smith",jade))OpenHubService(HubNpcKind.Blacksmith);
            }
            GUI.enabled=hudEnabled;
            Rect map=TouchRect(l.Map);blockedRects.Add(map);DrawMinimapTerrain(map);
            if(!session.InDungeon){MapDot(map,session.DungeonEntrancePosition,jade,4*TouchRatio);MapDot(map,GameSession.ChapterEntrancePosition,new Color(.8f,.5f,1),4*TouchRatio);for(int npc=0;npc<3;npc++)MapDot(map,GameSession.HubNpcPosition(npc),gold,3*TouchRatio);}
            else if(session.DungeonReturnAvailable)MapDot(map,session.DungeonReturnPosition,jade,4*TouchRatio);
            if(session.Player!=null)MapDot(map,session.Player.transform.position,jade,4*TouchRatio);
            foreach(var enemy in session.Enemies)if(enemy!=null&&!enemy.IsDead)MapDot(map,enemy.transform.position,enemy.IsBoss?gold:new Color(1,.4f,.3f),2*TouchRatio);
            if(GUI.Button(map,GUIContent.none,invisibleButton))OpenTravelMap();
            string growthTitle,growthStep;
            if(string.IsNullOrEmpty(session.Notification)&&!MobileLootReplacesInformation)
            {
                if(session.ChapterActive||session.SpecialAdventure)DrawMobileModeStatus(TouchRect(l.AdventureStatus));
                else if(!(session.IsNearDungeonEntrance||session.IsNearChapterEntrance)&&TryGrowthHudHint(out growthTitle,out growthStep))
                {
                    Rect goal=TouchRect(l.AdventureStatus);float y=goal.y;
                    DrawMobileObjectiveText(goal,ref y,growthTitle,11,gold,true,true);
                    DrawMobileObjectiveText(goal,ref y,growthStep,10,pale);
                    DrawObjectiveDirection(new Rect(goal.x,y,110*TouchRatio,24*TouchRatio),TouchRatio);
                }
            }
            if(session.InDungeon&&!session.SpecialAdventure)
            {blockedRects.Add(TouchRect(l.EncounterText));Text(TouchRect(l.EncounterText),session.DungeonCleared?"遗迹肃清":"第 "+session.DungeonWave+" / "+session.TotalWaves+" 波",TouchFont(12),pale,true,false,TextAnchor.MiddleCenter);}
            DrawMobileHotbar();
            DrawCompanionCommands();

            string interaction=MobileDungeonEntranceVisible?(session.IsNearChapterEntrance?"星路章节":"普通副本"):session.NearDungeonReturn?"传送点":session.NearChapterExit?"沿星路前进":session.NearRoomExit?"进入下一间":session.SideEventAvailable?"开启晶核挑战":session.InDungeon?"返回营地":(session.IsNearDungeonEntrance||session.IsNearChapterEntrance)?"进入副本":"靠近入口";
            if(MobileInteractionVisible)
            {
            Rect interact=TouchRect(MobileInteractionArea);blockedRects.Add(interact);
            // One pointer owner handles real touches and simulated/attached mice.
            // This is presentation only: a second IMGUI Button here would dispatch
            // again after a room transition changed the context on pointer release.
            Box(interact,CanMobileInteract?gold:muted,false);
            Text(interact,interaction,TouchFont(11),CanMobileInteract?gold:muted,true,false,TextAnchor.MiddleCenter);

            }
            var target=session.Player==null?null:session.Player.AimTarget;
            if(session.InDungeon)DrawDungeonStatus();
            else if(target!=null&&!target.IsDead)DrawEnemyTargetPanel(target);
            var targeting=session.Player==null?null:session.Player.GetComponent<SkillTargetingController>();
            var charge=session.Player==null?null:session.Player.GetComponent<SkillChargeController>();
            if(charge!=null&&charge.IsCharging)Bar(TouchRect(26,l.Height-40,128,5),charge.Progress,gold);
        }
        private bool OtherMobilePageReady()
        {
            if(session.Player==null)return false;
            int otherPage=(mobileSkillPage+1)%MobileSkillPolicy.PageCount;
            for(int button=0;button<MobileSkillPolicy.ButtonCount;button++)
            {
                int skill=BoundMobileSkill(button,otherPage);
                // Shared buttons (such as the ultimate) are already visible on this page.
                if(skill>=0&&!MobileSkillVisible(skill)&&session.Player.IsSkillAvailable(skill))return true;
            }
            return false;
        }
        private void DrawMobileHotbar()
        {
            float priorOpacity=controlOpacity;controlOpacity=EffectPreferences.TouchOpacity;
            var l=MobileControls.Layout;GameProfile p=session.Progression.Profile;
            for(int i=0;i<MobileSkillPolicy.ButtonCount;i++)
            {
                int skill=BoundMobileSkill(i,mobileSkillPage);if(!CombatSkillLearned(p,skill))continue;
                Rect hit=hotbarSlots[i];blockedRects.Add(hit);Rect r=MobileVisualRect(hit,combatButton:true);
                bool ready=session.Player!=null&&session.Player.IsSkillAvailable(skill);
                bool pressed=mobileTap.Skill==skill&&mobileTap.Active;
                Texture2D skillArt=UIIconAtlas.SkillGlyph(p.heroClass,skill,48);
                float iconSize=Mathf.Min(r.width,r.height)*1f*(pressed?.95f:1f);
                Rect icon=new Rect(r.center.x-iconSize*.5f,r.center.y-iconSize*.5f,iconSize,iconSize);
                // Solid backing keeps the identity readable against bright terrain.
                DrawRecoveringSkill(icon,skillArt,skill,ready);
                DrawMobileSkillAvailability(r,skill);
            }
            Rect pageHit=TouchRect(l.SkillPage);blockedRects.Add(pageHit);
            float pageIcon=(MobileControls.IsIPad?28.8f:24f)*TouchRatio;
            DrawIcon(new Rect(pageHit.center.x-pageIcon*.5f,pageHit.center.y-pageIcon*.5f,pageIcon,pageIcon),UIIconAtlas.SkillPageArrow(),Color.white);
            if(OtherMobilePageReady())
            {
                float pulse=.5f+.5f*Mathf.Sin(Time.unscaledTime*4);
                Rect aura=new Rect(pageHit.center.x-pageIcon*.65f,pageHit.center.y-pageIcon*.65f,pageIcon*1.3f,pageIcon*1.3f);
                DrawIcon(aura,UIIconAtlas.ControlRing(true),new Color(.25f,1f,.85f,.35f+.45f*pulse));
                float flow=Mathf.Repeat(Time.unscaledTime*.55f,1);
                DrawIcon(new Rect(aura.x+flow*aura.width-3*TouchRatio,aura.yMax-5*TouchRatio,6*TouchRatio,6*TouchRatio),UIIconAtlas.Utility("gem"),jade);
            }
            controlOpacity=priorOpacity;
        }
        private void DrawMobileControlSurface(Rect r,bool ready,bool pressed)
        {
            DrawIcon(r,UIIconAtlas.ControlDisc(),new Color(.025f,.045f,.065f,pressed&&ready?.28f:.18f));
            if(ready)DrawIcon(r,UIIconAtlas.ControlRing(true),new Color(.32f,.88f,1f,.12f));
            DrawIcon(r,UIIconAtlas.ControlRing(),ready?new Color(.35f,1f,.76f,.85f):new Color(.8f,.88f,.94f,.48f));
        }
        private void DrawMobileVitals(MobileControlLayout layout)
        {
            float hp=session.Player==null?0:session.Player.Health,max=session.Player==null?1:session.Player.MaxHealth;
            blockedRects.Add(TouchRect(layout.PlayerStatus));
            DrawHudVital(TouchRect(layout.PlayerHealth),hp/Mathf.Max(1,max),new Color(.035f,.30f,.14f),Mathf.CeilToInt(hp)+" / "+Mathf.CeilToInt(max));
            DrawHudVital(TouchRect(layout.PlayerEnergy),session.Player==null?0:session.Player.Energy/Mathf.Max(1,session.Player.MaxEnergy),new Color(.045f,.20f,.40f),session.Player==null?"0 / 100":Mathf.FloorToInt(session.Player.Energy)+" / "+Mathf.RoundToInt(session.Player.MaxEnergy));
        }
        // Objective text is informational; touching it must not open travel or click through.
        private void DrawMobileObjectiveText(Rect bounds,ref float y,string value,int size,Color tint,bool bold=false,bool locate=false)
        {
            if(string.IsNullOrEmpty(value))return;
            string content=PlatformText(value);var style=Style(TouchFont(size),bold,true);
            float w=Mathf.Min(bounds.width,style.CalcSize(new GUIContent(content)).x+2*TouchRatio);
            float h=style.CalcHeight(new GUIContent(content),w);
            Rect line=new Rect(bounds.x,y,w,h);
            Text(new Rect(line.x+TouchRatio,line.y+TouchRatio,line.width,line.height),content,TouchFont(size),new Color(0,0,0,.9f),bold,true);
            Text(line,content,TouchFont(size),tint,bold,true);
            blockedRects.Add(line);
            y+=h+2*TouchRatio;
        }
        private string mobileNoticeDetail;
        private Vector2 mobileNoticeScroll;
        private void DrawMobileBattleNotice()
        {
            var area=MobileControls.Layout.Notice;
            Rect bounds=TouchRect(area);
            if(session.Notification.StartsWith("等级提升"))
                bounds.width=Mathf.Min(280*TouchRatio,(MobileControls.Layout.Width*.45f-area.X)*TouchRatio);
            float y=bounds.y;
            DrawMobileObjectiveText(bounds,ref y,session.Notification,12,pale);
        }
        private void OpenMobileNoticeFromPause()
        {
            if(!session.Paused)return;
            var detail=new System.Text.StringBuilder();
            if(!string.IsNullOrEmpty(session.Notification))detail.AppendLine(PlatformText(session.Notification));
            if(!string.IsNullOrEmpty(session.Progression.LastError))detail.AppendLine(session.Progression.LastError);
            foreach(var message in session.SystemMessages)detail.AppendLine(PlatformText(message.Text));
            mobileNoticeDetail=detail.Length==0?"暂无系统提示":detail.ToString();mobileNoticeScroll=Vector2.zero;
            panel=Panel.Notice;session.SetUIBlocking(true);session.SetPaused(false);CancelMobileScroll();BlockUITransition();
        }
        private void DrawMobileNotice()
        {
            var layout=DrawMobileDialogChrome("冒险提示",gold);
            float contentWidth=layout.Body.Width-18;
            float contentHeight=MeasureMobileParagraph(mobileNoticeDetail,contentWidth-16,16)+16;
            mobileNoticeScroll=BeginTouchScroll("mobile-notice",MobilePanelRect(new MobilePanelLayout.Area(layout.Body.X,layout.Body.Y,layout.Body.Width,layout.Body.Height+56)),mobileNoticeScroll,
                new Rect(0,0,contentWidth*TouchRatio,Mathf.Max(layout.Body.Height+56,contentHeight)*TouchRatio));
            DrawMobileParagraph(8,8,contentWidth-16,mobileNoticeDetail,16,pale);
            EndTouchScroll();
            if(NavigationButton(MobilePanelRect(layout.FooterButton(0,1)),"返回设置",jade))
            {ClosePanel();session.SetPaused(true);BlockUITransition();}
        }
        private void DrawMobileTitle()
        {
            if(saveSlotsDirty)RefreshSaveSlots();
            if(!titleCreatingHero){DrawAdventureHome();return;}
            var l=MobileControls.Layout;
            // Title backdrop covers the full viewport before safe-area content.
            var title=new MobileTitleLayout(l.Width,l.Height,MobileTitleLayout.IsIPad(SystemInfo.deviceModel));
            Rect TitleRect(float x,float y,float w,float h) => TouchRect(title.Rect(x,y,w,h));
            int TitleFont(float size) => TouchFont(size*title.Zoom);
            Text(TitleRect(0,0,528,30),"选择职业",TitleFont(25),pale,true);
            Text(TitleRect(0,31,528,16),"初选职业可在安全营地自由切换",TitleFont(11),muted);
            for(int i=0;i<4;i++)
            {
                var hero=(HeroClass)i;Color tint=GameBalance.ClassColor(hero);Rect r=TitleRect(i*134,50,126,166);
                Fill(r,new Color(.055f,.09f,.13f));Border(r,selectedClass==hero?gold:tint*.45f,selectedClass==hero?2:1);
                DrawHeroPortrait(TitleRect(i*134+7,58,112,102),hero,"class-"+i);
                Text(TitleRect(i*134+5,166,116,31),GameBalance.ClassName(hero),TitleFont(18),pale,true,false,TextAnchor.MiddleCenter);
                if(GUI.Button(r,GUIContent.none,invisibleButton))selectedClass=hero;
            }
            if(DrawButton(TitleRect(0,237,254,52),"返回首页",ButtonRole.Navigation,fontSize:TitleFont(15))){titleCreatingHero=false;BlockUITransition();return;}
            if(DrawButton(TitleRect(274,237,254,52),"开始冒险",ButtonRole.Primary,fontSize:TitleFont(15)))StartSelectedHero();
            if(!string.IsNullOrEmpty(session.Progression.LastError))Text(TitleRect(0,291,528,22),session.Progression.LastError,TitleFont(11),gold);
        }
        private void DrawMobileSaveSelection()
        {
            var l=MobileControls.Layout;float panelWidth=MobileControls.IsIPad?Mathf.Min(900,l.Width-32):520;float x=(l.Width-panelWidth)/2,y=12;
            // Title backdrop covers the full viewport before safe-area content.
            Text(TouchRect(x,y,panelWidth,30),"角色存档  ·  "+saveSlots.Count,TouchFont(21),pale,true);
            Rect viewport=TouchRect(x,y+42,panelWidth,l.Height-135);float ratio=TouchRatio;
            if(mobileSaveSelectionIssue!=saveSelectionError)
            {mobileSaveSelectionIssue=saveSelectionError;if(!string.IsNullOrEmpty(saveSelectionError)){CancelMobileScroll();saveSelectionScroll=Vector2.zero;}}
            float issueHeight=string.IsNullOrEmpty(saveSelectionError)?0:MeasureMobileParagraph(saveSelectionError,panelWidth-42,14,true)+16;
            saveSelectionScroll=BeginTouchScroll("mobile-saves",viewport,saveSelectionScroll,new Rect(0,0,(panelWidth-20)*ratio,Mathf.Max(viewport.height,(issueHeight+saveSlots.Count*96)*ratio)));
            if(issueHeight>0)DrawMobileParagraph(10,6,panelWidth-42,saveSelectionError,14,gold,true);
            for(int i=0;i<saveSlots.Count;i++)
            {
                var slot=saveSlots[i];Rect r=new Rect(0,(issueHeight+i*96)*ratio,(panelWidth-22)*ratio,88*ratio);
                Fill(r,card);Border(r,selectedSaveId==slot.Id?gold:jade*.35f);
                if(slot.CanLoad&&r.yMax>=saveSelectionScroll.y&&r.y<=saveSelectionScroll.y+viewport.height)DrawHeroPortrait(new Rect(4*ratio,r.y+3*ratio,68*ratio,82*ratio),slot.HeroClass,"save-"+slot.Id,slot);
                Text(new Rect(82*ratio,r.y+17*ratio,(panelWidth-210)*ratio,23*ratio),slot.DisplayName,TouchFont(16),pale,true);
                Text(new Rect(82*ratio,r.y+49*ratio,(panelWidth-110)*ratio,19*ratio),(slot.SavedAtUtc==System.DateTime.MinValue?"时间未知":slot.SavedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm"))+(slot.DeletionPending?"  删除未完成":""),TouchFont(12),muted);
                if(GUI.Button(r,GUIContent.none,invisibleButton)){selectedSaveId=slot.Id;saveSelectionError=null;}
            }
            EndTouchScroll();
            float bottom=l.Height-62;float footerUnit=(panelWidth-32)/3;
            if(NavigationButton(TouchRect(x,bottom,footerUnit,48),"返回",jade))ClosePanel();
            DrawDeleteSaveButton(TouchRect(x+footerUnit+16,bottom,footerUnit,48));
            if(PrimaryButton(TouchRect(x+2*(footerUnit+16),bottom,footerUnit,48),"读取角色",gold,saveSlots.Exists(a=>a.Id==selectedSaveId&&a.CanLoad)))ContinueSelectedSave();
        }
        private string mobileSaveSelectionIssue;
        private void DrawMobileGuide()
        {
            var l=MobileControls.Layout;float x=(l.Width-510)/2,y=(l.Height-300)/2;
            Box(TouchRect(x-12,y-6,534,312),jade,false);
            Text(TouchRect(x,y,510,30),"触屏操作",TouchFont(22),pale,true);
            string[] tips={"左侧拖动移动 · 右下按住普攻，可同时操作", "主动技能分两页，每页最多4个；大招固定，被动自动生效", "轻点技能自动瞄准并施放，无须圈选或二次确认", "右屏空白拖动转镜头；松手保持当前视角", "蓄力自动完成；点取消或闪避可中断", "灰色技能尚未学会；到技能学习后直接可用"};
            for(int i=0;i<tips.Length;i++)Text(TouchRect(x,y+43+i*32,510,28),tips[i],TouchFont(14),i==2?jade:pale);
            if(controlsReturnPause&&NavigationButton(TouchRect(x,y+250,510,48),"返回设置",jade))ClosePanel();
        }
        private bool PauseSidebarTab(Rect hit,string caption,bool selected,float u)
        {
            if(selected)
            {
                Fill(hit,new Color(jade.r,jade.g,jade.b,.10f));
                Fill(new Rect(hit.x,hit.y+8*u,3*u,hit.height-16*u),gold);
            }
            Text(new Rect(hit.x+14*u,hit.y,hit.width-20*u,hit.height),caption,Mathf.RoundToInt(14*u),selected?gold:muted,selected,false,TextAnchor.MiddleLeft);
            bool clicked=GUI.Button(hit,GUIContent.none,invisibleButton);
            if(clicked)GameAudio.Play(SoundCue.UI);
            return clicked;
        }
        private int mobilePausePage;
        private readonly Vector2[] mobilePauseScroll = new Vector2[3];
        private void DrawMobilePause()
        {
            if(mobileBindingEditor){DrawMobileBindingEditor();return;}
            var layout = MobileControls.Layout;
            float panelWidth=Mathf.Min(720,layout.Width-24),x=(layout.Width-panelWidth)*.5f,y=12;
            float titleWidth=panelWidth-52;
            float headerHeight=Mathf.Max(44,Style(TouchFont(23),true).CalcHeight(new GUIContent("设置"),titleWidth*TouchRatio)/TouchRatio+8);
            Box(TouchRect(x-4,4,panelWidth+8,Mathf.Min(layout.Height,height/TouchRatio)-8),jade,false);
            Text(TouchRect(x,y,titleWidth,headerHeight), "设置", TouchFont(23), pale, true);
            Rect close=TouchRect(x+panelWidth-44,y,44,44);

            if(PopupCloseButton(close)){session.SetPaused(false);BlockUITransition();return;}
            string[] tabs = { "存档", "声音与画面", "按键设置" };
            int[] tabOrder={0,1,2};
            float sidebarWidth=120,bodyY=y+headerHeight+8;
            float bodyHeight=Mathf.Max(48,Mathf.Min(layout.Height,height/TouchRatio)-bodyY-72);
            float footerY=Mathf.Min(layout.Height,height/TouchRatio)-60,exitButtonWidth=panelWidth-136;
            if(session.InDungeon)
            {
                float actionWidth=(exitButtonWidth-8)*.5f;
                DrawPauseDungeonExitButton(TouchRect(x+136,footerY,actionWidth,48));
                if(PrimaryButton(TouchRect(x+144+actionWidth,footerY,actionWidth,48),"保存并返回主菜单",jade))RequestExit(true);
            }
            else if(PrimaryButton(TouchRect(x+136,footerY,exitButtonWidth,48),"保存并返回主菜单",jade))RequestExit(true);
            Fill(TouchRect(x,bodyY,sidebarWidth,bodyHeight),new Color(.025f,.05f,.065f,.65f));
            for (int i=0;i<tabs.Length;i++)
                if (PauseSidebarTab(TouchRect(x,bodyY+i*52,sidebarWidth,48),tabs[tabOrder[i]],mobilePausePage==tabOrder[i],TouchRatio) && mobilePausePage!=tabOrder[i])
                { mobilePausePage=tabOrder[i]; }
            float contentX=x+sidebarWidth+16,bodyWidth=panelWidth-sidebarWidth-16,contentWidth=bodyWidth-18;
            string notice=string.IsNullOrEmpty(session.Notification)?"":PlatformText(session.Notification);
            float noticeHeight=string.IsNullOrEmpty(notice)?0:Mathf.Max(32,Style(TouchFont(11),false,true).CalcHeight(new GUIContent(notice),contentWidth*TouchRatio)/TouchRatio+8);
            float contentHeight=mobilePausePage==0?174+noticeHeight:mobilePausePage==2?174:132+MobileGraphicsSettingsHeight(contentWidth);
            mobilePauseScroll[mobilePausePage]=BeginTouchScroll("mobile-pause-"+mobilePausePage,TouchRect(contentX,bodyY,bodyWidth,bodyHeight),mobilePauseScroll[mobilePausePage],new Rect(0,0,contentWidth*TouchRatio,Mathf.Max(bodyHeight,contentHeight)*TouchRatio));
            try { DrawMobilePauseBody(contentWidth,notice,noticeHeight); }
            finally { EndTouchScroll(); }
        }
        private void DrawMobilePauseBody(float contentWidth,string notice,float noticeHeight)
        {
            if(mobilePausePage==2){DrawMobileControlPreferences(0,0,contentWidth);if(NavigationButton(TouchRect(0,116,contentWidth,48),"操作指南",jade))OpenControls();return;}
            if(mobilePausePage==0)
            {
                if(PrimaryButton(TouchRect(0,0,contentWidth,48),"保存",gold))RequestManualSave();
                if(NavigationButton(TouchRect(0,58,contentWidth,48),"读取存档",jade))OpenSaveSelection();
                if(NavigationButton(TouchRect(0,116,contentWidth,48),"存档位置 / 迁移",jade))
                {saveReturnPause=true;panel=Panel.SaveLocation;session.SetUIBlocking(true);session.SetPaused(false);}
            if (!string.IsNullOrEmpty(notice))
                Text(TouchRect(0,174,contentWidth,noticeHeight),notice,TouchFont(11),gold,false,true,TextAnchor.MiddleCenter);
                return;
            }
            if (mobilePausePage == 1)
            {
                float column=(contentWidth-12)*.5f;
                string[] extra = { "声音：" + (GameAudio.Muted ? "关" : "开"), "飘字：" + (EffectPreferences.CombatTextScale > 1.5f ? "大" : "标准"),
                    "镜头反馈：" + (EffectPreferences.CameraShake ? "开" : "关"), "特效：" + (EffectPreferences.ReducedEffects ? "精简" : "完整") };
                for (int i = 0; i < extra.Length; i++)
                    if (Button(TouchRect((i%2)*(column+12),(i/2)*58,column,48),extra[i],jade))
                    {
                        if (i == 0) GameAudio.Muted = !GameAudio.Muted;
                        else if (i == 1) EffectPreferences.CombatTextScale = EffectPreferences.CombatTextScale > 1.5f ? 1.25f : 1.8f;
                        else if (i == 2) EffectPreferences.CameraShake = !EffectPreferences.CameraShake;
                        else if (i == 3) EffectPreferences.EffectsScale = EffectPreferences.ReducedEffects ? 1f : .35f;
                    }
                DrawMobileGraphicsSettings(0,132,contentWidth);
                return;
            }
        }
    }
}

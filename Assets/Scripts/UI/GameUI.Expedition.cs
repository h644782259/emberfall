using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameUI
    {
        private int selectedBlessing = -1, campTab;
        private float lastBlessingClick=-10;
        private void ClickBlessing(int index)
        {
            if(!GUI.enabled||UITransitionBlocked)return;
            bool twice=selectedBlessing==index&&Time.unscaledTime-lastBlessingClick<.45f;
            selectedBlessing=index;lastBlessingClick=Time.unscaledTime;
            if(twice&&session.ConfirmBlessing(index))
            {selectedBlessing=-1;lastBlessingClick=-10;CancelMobileScroll();BlockUITransition();}
        }
        private bool systemHistory;
        public void CancelForegroundInput()
        {
            CancelHotbarPointer(); rebindingSlot = -1;
            // IMGUI buttons have their own hotControl, unrelated to our hotbar.
            GUIUtility.hotControl=0;GUIUtility.keyboardControl=0;
            lastBlessingClick=mobileBlessingTapAt=-10;
            BlockUITransition();
        }
        public void CancelBackgroundInput()
        {
            CancelForegroundInput();
            // Only focus/suspension and viewport invalidation require every
            // pointer to lift. Room transitions retain their initiating finger.
            lifecycleRelease.Block(Time.unscaledTime);
        }

        private void DrawDungeonSelection(){DrawArenaSelection();}

        private Vector2 branchScroll;
        private void DrawRoomBranchChoice()
        {
            float u=MobileControls.Active?TouchRatio:1;
            Rect w=Modal(Mathf.Min(width-24,900*u),Mathf.Min(height-24,520*u),"第三房 · 选择侧廊","只进入所选路线；第四房星泉汇合，第五房首领。无额外材料奖励。");
            Rect viewport=new Rect(w.x+20*u,w.y+105*u,w.width-40*u,Mathf.Max(60*u,w.height-175*u));
            float cw=(viewport.width-16*u)*.5f;
            branchScroll=BeginTouchScroll("room-branch",viewport,branchScroll,new Rect(0,0,viewport.width,280*u));
            for(int i=0;i<2;i++)
            {
                RoomBranch branch=i==0?RoomBranch.Seal:RoomBranch.Supply;
                Rect c=new Rect(i*(cw+16*u),0,cw,270*u);Fill(c,card);Border(c,jade);
                Text(new Rect(c.x+12*u,c.y+12*u,c.width-24*u,196*u),GameSession.RoomBranchDescription(branch),Mathf.RoundToInt(16*u),pale,false,true);
                if(PrimaryButton(new Rect(c.x+12*u,c.y+218*u,c.width-24*u,42*u), "进入这条侧廊", gold))
                {session.ConfirmRoomBranch(branch);BlockUITransition();}
            }
            EndTouchScroll();
            if(NavigationButton(new Rect(w.x+20*u,w.yMax-58*u,w.width-40*u,40*u), "返回第二房 · 暂不选择", jade))
            {session.CancelRoomBranchChoice();BlockUITransition();}
        }

        private void DrawBlessingChoice()
        {
            if(MobileControls.Active){DrawMobileBlessingChoice();return;}
            RunBlessing[] offer = session.RunChoices.Offer;
            ReconcileBlessingOffer(offer);
            float cardHeight=184;
            foreach(var blessing in offer)
                cardHeight=Mathf.Max(cardHeight,Style(23,true,true).CalcHeight(new GUIContent(RunChoices.Name(blessing)),260)+Style(12,false,true).CalcHeight(new GUIContent(RunChoices.Association(blessing,session.Progression.Profile,false)),260)+Style(16,false,true).CalcHeight(new GUIContent(RunChoices.Description(blessing)),260)+96);
            Rect w = Modal(1000, cardHeight+210, "星烬祝福", BlessingSubtitle(false));
            for (int i=0;i<offer.Length;i++)
            {
                Rect cardRect = new Rect(w.x+28+i*322,w.y+117,300,cardHeight);
                bool chosen = i == selectedBlessing;
                Fill(cardRect,chosen ? new Color(.12f,.2f,.21f):card); Border(cardRect,chosen?gold:jade*.5f);
                float nameHeight=Style(23,true,true).CalcHeight(new GUIContent(RunChoices.Name(offer[i])),260);
                float associationHeight=Style(12,false,true).CalcHeight(new GUIContent(RunChoices.Association(offer[i],session.Progression.Profile,false)),260);
                Text(new Rect(cardRect.x+20,cardRect.y+16,260,nameHeight),RunChoices.Name(offer[i]),23,chosen?gold:pale,true,true);
                bool compatible = RunChoices.IsCompatible(offer[i],session.Progression.Profile.heroClass,RunChoices.UsableRanks(session.Progression.Profile,false));
                Text(new Rect(cardRect.x+20,cardRect.y+24+nameHeight,260,associationHeight),RunChoices.Association(offer[i],session.Progression.Profile,false),12,compatible?jade:muted,false,true);
                Text(new Rect(cardRect.x+20,cardRect.y+36+nameHeight+associationHeight,260,cardHeight-88-nameHeight-associationHeight),RunChoices.Description(offer[i]),16,pale,false,true);
                if(GUI.Button(cardRect,GUIContent.none,invisibleButton))
                ClickBlessing(i);
                Text(new Rect(cardRect.x+20,cardRect.y+202,260,24),chosen?"再点击确认":"点击预览 · 双击确认",13,chosen?gold:muted,true,false,TextAnchor.MiddleCenter);
            }
            if(PrimaryButton(new Rect(w.x+310,w.y+cardHeight+137,380,46), session.RoomChainRun!=null?"确认祝福并继续":"确认并进入下一波", gold, selectedBlessing>=0&&selectedBlessing<offer.Length, null, true))
            { if(session.ConfirmBlessing(selectedBlessing)){selectedBlessing=-1;lastBlessingClick=-10;CancelMobileScroll();BlockUITransition();} }
        }

        // Dedicated utility surfaces remain reachable from their own entries.
        private void DrawHubUtility()
        {
            if(merchantExchangeOpen){DrawAttachmentWorkshop();return;}
            if(DrawClassSwitchSurface()||DrawReforgeSurface()||DrawProgressionGoalSurface()||DrawBuildPlanSurface())return;
            if(campTab==4){DrawTownActivitySurface();return;}
            ClosePanel();
        }

        private void DrawComboCounter()
        {
            int count=session.ComboHitCount;
            if(count<=0||panel!=Panel.None||session.Paused||session.IsDead)return;
            float u=MobileControls.Active?TouchRatio:1;
            Rect r=new Rect(width-244*u,Mathf.Clamp(height*.46f,30*u,height-90*u),208*u,54*u);
            if(MobileControls.Active)
            {
                var layout=MobileControls.Layout;
                // Keep combo feedback in the open field left of the upper skill column.
                // A viewport anchor stays below navigation even on compact phones.
                float centerX=Mathf.Min(layout.Width*.74f,layout.Skills[4].X-96);
                r=new Rect((centerX-80)*u,(layout.Height*.4f-27)*u,160*u,54*u);
            }
            var previous=GUI.matrix;
            GUIUtility.RotateAroundPivot(-7,r.center);
            var style=new GUIStyle(Style(Mathf.RoundToInt(30*u),true,false));
            style.fontStyle=FontStyle.BoldAndItalic;style.alignment=MobileControls.Active?TextAnchor.MiddleCenter:TextAnchor.MiddleRight;
            style.padding=new RectOffset(0,Mathf.RoundToInt(8*u),0,0);
            string combo="连击 x"+count;
            while(style.fontSize>Mathf.RoundToInt(12*u)&&style.CalcSize(new GUIContent(combo)).x>r.width-16*u)style.fontSize--;
            style.clipping=TextClipping.Clip;
            style.normal.textColor=new Color(.12f,.05f,.015f,.95f);
            GUI.Label(new Rect(r.x+2*u,r.y+2*u,r.width,r.height),combo,style);
            style.normal.textColor=gold;GUI.Label(r,combo,style);
            GUI.matrix=previous;
        }

        private void DrawPauseDungeonExitButton(Rect exit)
        {
            if(!session.InDungeon||!session.Paused||session.IsDead)return;
            if(DangerButton(exit,"退出副本",gold,!session.InCombat,"脱离战斗后可退出副本"))
            {
                session.ReturnToCamp();
                if(!session.InDungeon){session.SetPaused(false);panel=Panel.None;session.SetUIBlocking(false);}
                BlockUITransition();
            }
        }

        public void OpenDungeonExit()
        {
            if(!session.NearDungeonReturn||session.InputBlocked)return;
            panel=Panel.DungeonExit;session.SetUIBlocking(true);BlockUITransition();
        }
        private void DrawDungeonExit()
        {
            if(!session.NearDungeonReturn){ClosePanel();return;}
            float u=MobileControls.Active?TouchRatio:1;
            Rect w=Modal(Mathf.Min(width-24,500*u),Mathf.Min(height-24,250*u),"传送点","");
            if(PopupCloseButton(new Rect(w.xMax-54*u,w.y+12*u,44*u,44*u))){ClosePanel();return;}
            float x=w.x+24*u,y=w.y+106*u,bw=(w.width-56*u)*.5f;

            if(Button(new Rect(x,y,w.width-48*u,44*u),"再次挑战当前副本",gold,session.CanRepeatCurrentDungeon))
            {
                session.SetUIBlocking(false);
                if(session.RepeatCurrentDungeon())panel=Panel.None;else session.SetUIBlocking(true);
                BlockUITransition();return;
            }
            y+=54*u;
            bool showNextTier=!session.ChapterActive||session.ActiveChapterDifficulty==ChapterDifficulty.Heroic;
            if(showNextTier&&Button(new Rect(x,y,bw,44*u),"挑战下一阶",gold,session.CanChallengeNextTier))
            {
                session.SetUIBlocking(false);
                if(session.ChallengeNextTier())panel=Panel.None;
                else session.SetUIBlocking(true);
                BlockUITransition();return;
            }
            if(Button(new Rect(showNextTier?x+bw+8*u:x,y,showNextTier?bw:w.width-48*u,44*u),"返回营地",jade))
            {
                session.SetUIBlocking(false);session.ReturnToCamp();
                if(!session.InDungeon)panel=Panel.None;else session.SetUIBlocking(true);
                BlockUITransition();
            }
        }

        private void DrawExpeditionHUD()
        {
            if(session.NearDungeonReturn){Rect exit=new Rect((width-240)*.5f,height-225,240,48);blockedRects.Add(exit);if(Button(exit,"传送点 [E]",gold))OpenDungeonExit();}
            else if(session.NearChapterExit){Rect next=new Rect((width-300)*.5f,height-225,300,48);blockedRects.Add(next);if(Button(next,"沿星路前进 [E]",gold))session.EnterNextChapterRoom();}
            else if(session.NearRoomExit){Rect next=new Rect((width-300)*.5f,height-225,300,48);blockedRects.Add(next);if(Button(next,"进入下一间 [E]",gold))session.EnterNextRoom();}
            else if(!session.InDungeon&&(session.IsNearDungeonEntrance||session.IsNearChapterEntrance)&&!session.InputBlocked&&!session.DungeonSelectionOpen)
            {Rect entrance=new Rect((width-300)*.5f,height-225,300,48);blockedRects.Add(entrance);if(Button(entrance,session.IsNearChapterEntrance?"进入星路章节 [E]":"进入普通副本 [E]",gold))session.EnterDungeon();}
            if(session.SideEventAvailable)
            { Rect r=new Rect((width-410)*.5f,height-260,410,48);blockedRects.Add(r);
              if(Button(r,"开启晶核挑战 [E] · 2名守卫",gold,true,"额外一名遗迹守卫与一名魔灵；全部击败才获得1碎片和补给。"))session.StartSideEvent(); }

            DrawSystemLog();
        }
        // Read live encounter state: no message queue, duplicate logs or stale warnings.
        private void DrawBossCombatWarning()
        {
            if(panel!=Panel.None||session.InputBlocked||!session.InDungeon||session.Player==null||session.IsDead)return;
            string message=null;float remaining=0,progress=0;bool opportunity=false;
            foreach(var enemy in session.Enemies)
            {
                if(enemy==null||!enemy.IsBoss||enemy.IsDead||!enemy.enabled||!enemy.gameObject.activeInHierarchy)continue;
                if(enemy.IsBossParalyzed){message="首领瘫痪 · 受到伤害 +35%";opportunity=true;progress=enemy.BossParalysisRemaining/EnemyController.BossParalysisDuration;break;}
                var mechanic=enemy.GetComponent<LargeExpeditionBoss>();
                var state=mechanic==null?null:mechanic.State;
                if(state!=null&&state.Phase==LargeBossPhase.Windup)
                {message=state.IsFollowup?"追加扫射蓄力 · 用控制技能打断":"环流扫射蓄力 · 打断或摧毁供能锚";remaining=state.Remaining;progress=1-remaining/LargeBossPhaseState.WindupSeconds;break;}
                if(state!=null&&state.Phase==LargeBossPhase.Beam)
                {message=state.LiveAnchorMask!=0?"环流扫射 · 避开光束，摧毁供能锚":"追加扫射 · 避开光束";remaining=state.Remaining;progress=remaining/LargeBossPhaseState.BeamSeconds;break;}
                if(enemy.IsPreparingAttack)
                {message=enemy.CanBeSkillInterrupted?"首领正在蓄力 · 控制技能可打断":"首领正在蓄力 · 避开地面预警";remaining=enemy.AttackWindupRemaining;progress=enemy.AttackWindupProgress;break;}
                if(state!=null&&state.Phase==LargeBossPhase.Exposed)
                {message="核心暴露 · 伤害提高35%，集中攻击";remaining=state.Remaining;progress=remaining/LargeBossPhaseState.ExposureSeconds;opportunity=true;}
            }
            if(string.IsNullOrEmpty(message))return;
            bool mobile=MobileControls.Active;
            float w=mobile?Mathf.Min(360*TouchRatio,width*.50f):Mathf.Min(460,width-620);
            if(w<180)return;
            int font=mobile?TouchFont(12):15;
            string label=message;
            float h=Mathf.Max(mobile?36*TouchRatio:38,Style(font,true,true,TextAnchor.MiddleCenter).CalcHeight(new GUIContent(label),w-20)+24);
            float top=mobile?TouchRect(MobileControls.Layout.BossHealth).yMax+12*TouchRatio:100;
            // Notifications and registered HUD controls have priority over this strip.
            Rect notice=mobile?TouchRect(MobileControls.Layout.Notice):new Rect((width-550)*.5f,26,550,
                string.IsNullOrEmpty(session.Notification)?0:Mathf.Max(40,Style(14,true,true,TextAnchor.MiddleCenter).CalcHeight(new GUIContent(PlatformText(session.Notification)),522)+12));
            for(float y=top;y+h<height*.40f;y+=8)
            {
                Rect r=new Rect((width-w)*.5f,y,w,h);bool overlaps=!string.IsNullOrEmpty(session.Notification)&&r.Overlaps(notice);
                foreach(var occupied in blockedRects)if(r.Overlaps(occupied)){overlaps=true;break;}
                if(overlaps)continue;
                Color tint=opportunity?jade:gold;
                Fill(r,new Color(.025f,.035f,.05f,.82f));Fill(new Rect(r.x,r.y,3,r.height),tint);
                Text(new Rect(r.x+10,r.y+6,r.width-20,r.height-24),label,font,tint,true,true,TextAnchor.MiddleCenter);
                Bar(new Rect(r.x+10,r.yMax-12,r.width-20,5),progress,tint);
                return;
            }
        }

        private void DrawSystemLog()
        {
            if(panel!=Panel.None || session.InputBlocked)return;
            var messages=session.SystemMessages;
            if(messages.Count==0)return;
            int show=systemHistory?Mathf.Min(8,messages.Count):Mathf.Min(3,messages.Count);
            float panelHeight=AdventureSelectionLayout.LogHeight(messages.Count,systemHistory);
            float bottom=height-(MobileControls.Active?220:54);
            Rect r=new Rect(16,bottom-panelHeight,344,panelHeight);
            Fill(r,new Color(.025f,.045f,.065f,.88f));
            Text(new Rect(r.x+10,r.y+6,228,20),"系统信息",12,jade,true);
            if(Button(new Rect(r.xMax-68,r.y+3,59,25),systemHistory?"收起":"记录",muted))systemHistory=!systemHistory;
            for(int i=0;i<show;i++)
            {
                Rect row=new Rect(r.x+10,r.y+31+i*39,r.width-20,38);
                Text(row,messages[messages.Count-show+i].Text,13,pale,false,true);
                if(row.Contains(Mouse))tooltip=messages[messages.Count-show+i].Text;
            }
            blockedRects.Add(r);
        }

        private void DrawRunSummary()
        {
            if(DrawStructuredRunRecap(false))CloseSettlement();
        }

        private void DrawAccessibilityStrip(Rect r)
        {
            if(Button(new Rect(r.x,r.y,127,35),"飘字 "+EffectPreferences.CombatTextScale.ToString("0.00")+"×",jade))
                EffectPreferences.CombatTextScale=EffectPreferences.CombatTextScale>=1.79f?1f:Mathf.Min(1.8f,EffectPreferences.CombatTextScale+.25f);
            if(Button(new Rect(r.x+408,r.y,127,35),"字号 "+EffectPreferences.InterfaceTextSizeName,jade))EffectPreferences.CycleInterfaceTextScale();
            if(Button(new Rect(r.x+136,r.y,127,35),EffectPreferences.CameraShake?"震动：开":"震动：关",jade))EffectPreferences.CameraShake=!EffectPreferences.CameraShake;
            if(Button(new Rect(r.x+272,r.y,127,35),EffectPreferences.ReducedEffects?"低动态效果":"完整效果",jade))EffectPreferences.EffectsScale=EffectPreferences.ReducedEffects?1f:.3f;
        }
    }
}

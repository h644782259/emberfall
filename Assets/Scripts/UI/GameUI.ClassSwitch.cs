using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private bool classSwitchOpen;
        private ProgressionService classSwitchOwner;
        private PlayerController classSwitchHero;
        private ProgressionService.ClassSwitchTransaction classSwitchPreview;
        private string classSwitchFingerprint,classSwitchMessage;
        private Vector2 classSwitchScroll,classSelectionScroll;
        internal bool ClassSwitchHasPendingEdit {get{return allocationDraft!=null||reforgeOwner!=null;}}
        private void OpenClassSwitch()
        {
            panel=Panel.Skills;skillSection=2;classSwitchMessage=null;CancelMobileScroll();
        }
        private void DrawClassSelectionTab()
        {
            float u=MobileControls.Active?TouchRatio:1;Rect body;
            if(MobileControls.Active)
            {
                var l=MobilePanelGeometry();
                if(DrawMobilePanelChrome(l,"切换职业","",showNotice:false,headerRightReserve:276))return;
                DrawSkillTabs(TouchRect(l.Close.X-276,8,264,44));
                body=MobilePanelRect(new MobilePanelLayout.Area(l.Body.X,l.Body.Y,l.Body.Width,l.Height-l.Body.Y-12));
            }
            else
            {
                Rect w=Modal(1160,660,"切换职业","");
                if(PopupCloseButton(new Rect(w.xMax-69,w.y+20,44,32))){ClosePanel();return;}
                DrawSkillTabs(new Rect(w.x+330,w.y+20,264,36));
                body=new Rect(w.x+24,w.y+108,w.width-48,w.height-132);
            }
            if(skillSection!=2)return;
            string[] roles={"近战 · 斩击 · 耐久","法术 · 控制 · 爆发","远程 · 灵活 · 射击","召唤 · 协同 · 灵兽"};
            int columns=body.width/u<600?2:4,rows=(4+columns-1)/columns;
            float gap=12*u,cardWidth=(body.width-18*u-(columns-1)*gap)/columns;
            float cardHeight=columns==2?240*u:Mathf.Min(350*u,body.height-48*u);
            string reason=session.ClassSwitchLockReason();
            string notice=!string.IsNullOrEmpty(classSwitchMessage)?classSwitchMessage:!string.IsNullOrEmpty(reason)?reason:"选择职业后确认切换";
            float noticeHeight=Mathf.Max(32*u,Style(Mathf.RoundToInt(13*u),false,true).CalcHeight(new GUIContent(notice),body.width-18*u));
            float total=rows*(cardHeight+gap)+noticeHeight+8*u;
            classSelectionScroll=BeginTouchScroll("class-selection-models",body,classSelectionScroll,new Rect(0,0,body.width-18*u,Mathf.Max(body.height,total)));
            float top=0;
            for(int i=0;i<4;i++)
            {
                var hero=(HeroClass)i;bool current=hero==session.Progression.Profile.heroClass;
                Rect choice=new Rect((i%columns)*(cardWidth+gap),top+(i/columns)*(cardHeight+gap),cardWidth,cardHeight);
                Color accent=GameBalance.ClassColor(hero);
                Fill(choice,new Color(.055f,.09f,.13f));Border(choice,current?gold:accent*.45f,current?2:1);
                DrawHeroPortrait(new Rect(choice.x+8*u,choice.y+24*u,choice.width-16*u,cardHeight-96*u),hero,"switch-"+i);
                Text(new Rect(choice.x+4*u,choice.y+5*u,choice.width-8*u,20*u),current?"当前职业":"",Mathf.RoundToInt(11*u),gold,true,false,TextAnchor.MiddleCenter);
                Text(new Rect(choice.x+4*u,choice.yMax-66*u,choice.width-8*u,30*u),GameBalance.ClassName(hero),Mathf.RoundToInt(18*u),pale,true,false,TextAnchor.MiddleCenter);
                if(cardHeight>180*u)Text(new Rect(choice.x+4*u,choice.yMax-34*u,choice.width-8*u,24*u),roles[i],Mathf.RoundToInt(11*u),muted,false,false,TextAnchor.MiddleCenter);
                if(GUI.Button(choice,GUIContent.none,invisibleButton)&&!current)
                {
                    if(!string.IsNullOrEmpty(reason))classSwitchMessage=reason;
                    else {classSwitchOwner=session.Progression;classSwitchHero=session.Player;classSwitchOpen=true;PreviewClassSwitch(hero);BlockUITransition();EndTouchScroll();return;}
                }
            }
            Text(new Rect(0,rows*(cardHeight+gap),body.width-18*u,noticeHeight),notice,Mathf.RoundToInt(13*u),gold,false,true,TextAnchor.MiddleCenter);
            EndTouchScroll();
        }
        private bool CloseClassSwitchSurface()
        {
            if(!classSwitchOpen||(panel!=Panel.HubUtility&&panel!=Panel.Skills))return false;
            classSwitchOpen=false;classSwitchPreview=null;classSwitchScroll=Vector2.zero;
            CancelMobileScroll();BlockUITransition();return true;
        }
        private void ReconcileClassSwitchSurface()
        {
            if(classSwitchOpen&&((panel!=Panel.HubUtility&&panel!=Panel.Skills)||session.Progression!=classSwitchOwner||session.Player!=classSwitchHero))
            {classSwitchOpen=false;classSwitchPreview=null;classSwitchOwner=null;classSwitchHero=null;}
        }
        internal void OnClassSwitched()
        {
            classSwitchOpen=false;classSwitchPreview=null;classSwitchOwner=null;classSwitchHero=null;
            RebindProgressionNotifications(session.Progression,session.Progression);
            CancelForegroundInput();CancelMobileCast();CancelMobileScroll();ResetMobileSkillNavigation();
            skillDevelopmentScroll=Vector2.zero;saveSlotsDirty=true;BlockUITransition();
        }
        private void PreviewClassSwitch(HeroClass target)
        {
            classSwitchPreview=session.Progression.PrepareClassSwitch(target,session.IsInCamp);
            classSwitchFingerprint=session.Progression.BuildStateFingerprint();
            classSwitchMessage=classSwitchPreview==null?session.Progression.LastError:null;
            CancelMobileScroll();
        }
        private bool DrawClassSwitchSurface()
        {
            ReconcileClassSwitchSurface();
            if(!classSwitchOpen)return false;
            float unit=MobileControls.Active?TouchRatio:1;var layout=new MobileDialogLayout(width/unit,height/unit);
            blockedRects.Add(new Rect(0,0,width,height));
            Box(BuildPlanRect(layout.Frame,unit),gold,false);Text(BuildPlanRect(layout.Header,unit),"确认切换职业",Mathf.RoundToInt(21*unit),pale,true);
            float bodyWidth=layout.Body.Width-18,bodyHeight=DrawClassSwitchContent(bodyWidth,unit,false);
            classSwitchScroll=BeginTouchScroll("class-switch",BuildPlanRect(layout.Body,unit),classSwitchScroll,new Rect(0,0,bodyWidth*unit,Mathf.Max(layout.Body.Height,bodyHeight)*unit));
            DrawClassSwitchContent(bodyWidth,unit,true);EndTouchScroll();
            if(NavigationButton(BuildPlanRect(layout.FooterButton(0,2),unit), "取消 · 返回技能", jade)){CloseClassSwitchSurface();return true;}
            bool fresh=classSwitchPreview!=null&&classSwitchFingerprint==session.Progression.BuildStateFingerprint();
            string reason=session.ClassSwitchLockReason();
            string caption=classSwitchPreview==null?"选择另一职业":!fresh?"重新核对":"确认切换为"+GameBalance.ClassName(classSwitchPreview.Target);
            if(DrawButton(BuildPlanRect(layout.FooterButton(1,2),unit), caption, ButtonRole.Primary, classSwitchPreview!=null&&string.IsNullOrEmpty(reason)))
            {
                if(!fresh)PreviewClassSwitch(classSwitchPreview.Target);
                else if(!session.TrySwitchClass(classSwitchPreview.Target))classSwitchMessage=session.ClassSwitchError;
                CancelMobileScroll();BlockUITransition();
            }
            return true;
        }
        private float DrawClassSwitchContent(float bodyWidth,float unit,bool draw)
        {
            float y=4;var p=session.Progression;
            BuildPlanParagraph(ref y,bodyWidth,unit,"当前："+GameBalance.ClassName(p.Profile.heroClass),gold,draw,true);
            BuildPlanParagraph(ref y,bodyWidth,unit,"共享等级、装备与地图进度。每个职业独立保留配点、路线和快捷栏。首次切换按技能位置保留合法配点，不复制教程。",muted,draw);
            BuildPlanParagraph(ref y,bodyWidth,unit,"装备与宝石保留，武器名称随职业更新；各职业的宝石镶嵌分别恢复。切换不会重置生命比例、能量和冷却。",muted,draw);
            BuildPlanParagraph(ref y,bodyWidth,unit,classSwitchMessage,gold,draw,true);
            BuildPlanParagraph(ref y,bodyWidth,unit,session.ClassSwitchLockReason(),gold,draw);
            if(classSwitchPreview!=null)
            {
                BuildPlanParagraph(ref y,bodyWidth,unit,"切换后："+GameBalance.ClassName(classSwitchPreview.Target),gold,draw,true);

            }
            return y;
        }
    }
}

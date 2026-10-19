using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private Vector2 chapterScroll,chapterResultScroll;
        private object chapterResultScrollOwner;
        private bool chapterStoryExpanded,chapterRulesExpanded;
        private GameProfile chapterSelectionOwner;
        private string chapterEntryError;
        private bool ChapterSelectionIsCurrent()
        {return (panel==Panel.Chapter||session.DungeonSelectionOpen&&adventureChapterSelected)&&session.OpenChapterSelectionAllowed&&ReferenceEquals(chapterSelectionOwner,session.Progression.Profile);}
        public bool OpenChapterEntrance(){return OpenChapterSelection();}
        private bool OpenChapterSelection()
        {
            if(UITransitionBlocked||!session.OpenChapterSelectionAllowed)return false;
            chapterSelectionOwner=session.Progression.Profile;chapterEntryError=null;chapterScroll=Vector2.zero;chapterStoryExpanded=false;chapterRulesExpanded=false;
            if(!ChapterProgression.IsUnlocked(chapterSelectionOwner,session.SelectedChapterNode))session.SelectedChapterNode=ChapterNode.ForestCourt;
            session.SelectedChapterTier=Mathf.Clamp(session.SelectedChapterTier,1,session.Progression.UnlockedChapterTier(session.SelectedChapterNode));
            CancelHotbarPointer();CancelMobileScroll();panel=Panel.Chapter;session.SetUIBlocking(true);BlockUITransition();return true;
        }
        private bool SelectChapterNode(ChapterNode node)
        {
            if(!ChapterSelectionIsCurrent()||!ChapterProgression.IsUnlocked(chapterSelectionOwner,node))return false;
            session.SelectedChapterNode=node;session.SelectedChapterTactic=-1;
            chapterScroll=Vector2.zero;chapterStoryExpanded=false;chapterRulesExpanded=false;chapterEntryError=null;CancelMobileScroll();return true;
        }
        private void ChangeChapterTier(int delta)
        {
            if(!ChapterSelectionIsCurrent())return;
            session.SelectedChapterTier=Mathf.Clamp((int)System.Math.Max(1,System.Math.Min(int.MaxValue,(long)session.SelectedChapterTier+(delta<0?-1:delta>0?1:0))),1,session.Progression.UnlockedChapterTier(session.SelectedChapterNode));
            // The stepper stays in place; only its values change, with no screen transition.
        }
        private void SetChapterLimitedHealing(bool limited)
        {if(!ChapterSelectionIsCurrent())return;session.SelectedChapterLimitedHealing=limited;}
        private bool ConfirmSelectedChapter()
        {
            if(!ChapterSelectionIsCurrent()||!ChapterProgression.CanEnter(chapterSelectionOwner,session.SelectedChapterNode))return false;
            if(!session.ConfirmChapterEnter())
            {chapterEntryError=string.IsNullOrEmpty(session.Progression.LastError)?"暂时无法进入，请确认营地状态后重试。":session.Progression.LastError;BlockUITransition();return false;}
            panel=Panel.None;chapterSelectionOwner=null;chapterEntryError=null;CancelMobileScroll();session.SetUIBlocking(false);BlockUITransition();return true;
        }
        private bool CloseChapterSelection()
        {
            if(panel!=Panel.Chapter)return false;
            if(session.IsNearChapterEntrance)session.RetreatFromPortal(GameSession.ChapterEntrancePosition,PortalInteractionPolicy.EntranceRadius);
            panel=Panel.None;chapterSelectionOwner=null;chapterEntryError=null;CancelMobileScroll();session.SetUIBlocking(false);BlockUITransition();return true;
        }
        private bool RetryChapterSettlement()
        {if(!session.ChapterFinished||!session.ChapterRewardPending)return false;bool saved=session.TrySettleChapterReward();BlockUITransition();return saved;}
        private void ReturnFromChapter()
        {if(!session.ChapterFinished)return;if(session.IsDead)session.Respawn();else session.ReturnToCamp();BlockUITransition();}
        private bool ReturnAndSelectNextChapter()
        {
            if(!session.ChapterFinished||session.ChapterRewardPending||session.ChapterRun.Failed)return false;
            int next=(int)session.ActiveChapterNode+1;
            if(next>=3||!ChapterProgression.IsUnlocked(session.Progression.Profile,(ChapterNode)next))return false;
            session.ReturnToCamp();
            if(!session.OpenChapterSelectionAllowed){BlockUITransition();return false;}
            session.SelectedChapterNode=(ChapterNode)next;
            return OpenChapterSelection();
        }
        private void DrawChapterSelection()
        {
            if(!ChapterSelectionIsCurrent()){CloseChapterSelection();return;}
            bool mobile=MobileControls.Active;float u=mobile?TouchRatio:1;
            var layout=ChapterPanelGeometry();
            Box(ChapterRect(new MobilePanelLayout.Area(8,4,layout.Width-16,layout.Height-8),u),jade,false);
            Text(ChapterRect(new MobilePanelLayout.Area(28,24,112,40),u),"星路章节",Mathf.RoundToInt(20*u),pale,true);
            var profile=session.Progression.Profile;var node=session.SelectedChapterNode;
            if(PopupCloseButton(ChapterRect(new MobilePanelLayout.Area(layout.Width-64,24,44,44),u)))
            {CloseChapterSelection();return;}
            float cardWidth=(layout.Width-236)/3;
            for(int i=0;i<3;i++)
            {
                var choice=(ChapterNode)i;bool unlocked=ChapterProgression.IsUnlocked(profile,choice);
                Rect card=ChapterRect(new MobilePanelLayout.Area(148+i*cardWidth,24,cardWidth-8,44),u);
                if(TabButton(card,ChapterDefinition.Get(choice).Name,choice==node,unlocked)){SelectChapterNode(choice);return;}
            }
            DrawChapterEntryDetails(ChapterRect(new MobilePanelLayout.Area(28,88,layout.Width-56,layout.Height-184),u),u);
            DrawChapterEntryControls(ChapterRect(new MobilePanelLayout.Area(28+0*((layout.Width-56)/3),layout.Height-76,(layout.Width-56)/3-8,48),u),u);
            if(PrimaryButton(ChapterRect(new MobilePanelLayout.Area(28+2*((layout.Width-56)/3),layout.Height-76,(layout.Width-56)/3-8,48),u),"进入 "+ChapterDefinition.Get(node).Name,gold,ChapterProgression.CanEnter(profile,node)))
            {ConfirmSelectedChapter();return;}
        }
        private void DrawChapterEntryDetails(Rect body,float u)
        {
            var node=session.SelectedChapterNode;int mode=(int)node;float w=body.width/u-18;
            string mechanic=ChapterDefinition.Get(node).Mechanic+"\n"+ChapterDefinition.CombatMechanic(node);
            // All three entrances use the same typography for this viewport.
            // Content length only affects scrolling, never font size or weight.
            bool compact=MobileControls.Active&&body.height/u<420&&w>=340;
            int descriptionFont=compact?12:14;
            float descriptionY=compact?28:52,textWidth=w-24;
            float descriptionHeight=Style(Mathf.RoundToInt(descriptionFont*u),false,true).CalcHeight(new GUIContent(mechanic),textWidth*u)/u+(compact?0:12);
            float rewardWidth=w,rewardX=0,rewardY=descriptionY+descriptionHeight+(compact?2:0);
            float rewardsHeight=DrawEntryRewardPreviews(rewardWidth,u,mode,session.SelectedChapterTier,true,false,compact);
            float total=rewardY+rewardsHeight,errorHeight=string.IsNullOrEmpty(chapterEntryError)?0:48;
            total+=errorHeight;
            bool overflow=total*u>body.height;
            if(overflow)chapterScroll=BeginTouchScroll("chapter-entry",body,chapterScroll,new Rect(0,0,w*u,total*u));
            else {chapterScroll=Vector2.zero;GUI.BeginGroup(body);}
            if(compact)
            {
                DrawDungeonEntryArtwork(new Rect(12*u,2*u,32*u,24*u),5+(int)node);
                Text(new Rect(52*u,0,(w-60)*u,28*u),ChapterDefinition.Get(node).Name+" · 第"+session.SelectedChapterTier+"阶",Mathf.RoundToInt(15*u),pale,true,false,TextAnchor.MiddleLeft);
            }
            else
            {
                DrawDungeonEntryArtwork(new Rect(12*u,4*u,64*u,40*u),5+(int)node);
                Text(new Rect(86*u,4*u,(w-94)*u,40*u),ChapterDefinition.Get(node).Name+" · 第"+session.SelectedChapterTier+"阶",Mathf.RoundToInt(20*u),pale,true,false,TextAnchor.MiddleLeft);
            }
            Text(new Rect(12*u,descriptionY*u,textWidth*u,descriptionHeight*u),mechanic,Mathf.RoundToInt(descriptionFont*u),muted,false,true);
            entryRewardViewport=body;entryRewardContentOrigin=new Vector2(body.x+rewardX*u-chapterScroll.x,body.y+rewardY*u-chapterScroll.y);
            GUI.BeginGroup(new Rect(rewardX*u,rewardY*u,rewardWidth*u,rewardsHeight*u));DrawEntryRewardPreviews(rewardWidth,u,mode,session.SelectedChapterTier,true,true,compact);GUI.EndGroup();
            if(!string.IsNullOrEmpty(chapterEntryError))Text(new Rect(8*u,(total-48)*u,(w-16)*u,48*u),chapterEntryError,Mathf.RoundToInt(13*u),gold,false,true);
            if(overflow)EndTouchScroll();else GUI.EndGroup();
        }
        private void DrawChapterEntryControls(Rect area,float u)
        {
            var node=session.SelectedChapterNode;
            int tier=session.SelectedChapterTier,maximum=session.Progression.UnlockedChapterTier(node);
            float button=Mathf.Min(44*u,area.width*.24f),label=Mathf.Max(0,area.width-button*2);
            float x=area.x;
            if(NavigationButton(new Rect(x,area.y,button,area.height),"−",jade,tier>1))ChangeChapterTier(-1);
            Text(new Rect(x+button,area.y,label,area.height),"第 "+tier+" 阶",Mathf.RoundToInt(13*u),gold,true,false,TextAnchor.MiddleCenter);
            if(NavigationButton(new Rect(x+button+label,area.y,button,area.height),"+",jade,tier<maximum))ChangeChapterTier(1);
        }
        private void PrepareInlineChapterSelection()
        {
            if(ReferenceEquals(chapterSelectionOwner,session.Progression.Profile))return;
            chapterSelectionOwner=session.Progression.Profile;chapterEntryError=null;chapterScroll=Vector2.zero;
            if(!ChapterProgression.IsUnlocked(chapterSelectionOwner,session.SelectedChapterNode))session.SelectedChapterNode=ChapterNode.ForestCourt;
            session.SelectedChapterTier=Mathf.Clamp(session.SelectedChapterTier,1,session.Progression.UnlockedChapterTier(session.SelectedChapterNode));
        }
        private void DrawInlineChapterEntry(Rect area,float u)
        {PrepareInlineChapterSelection();DrawChapterEntryDetails(area,u);}
        private MobilePanelLayout ChapterPanelGeometry()
        {return MobileControls.Active?MobilePanelGeometry():new MobilePanelLayout(Mathf.Min(960,width),Mathf.Min(660,height));}
        private Rect ChapterRect(MobilePanelLayout.Area area,float u)
        {var layout=ChapterPanelGeometry();float x=MobileControls.Active?0:(width-layout.Width*u)*.5f,y=MobileControls.Active?0:(height-layout.Height*u)*.5f;
            return new Rect(x+area.X*u,y+area.Y*u,area.Width*u,area.Height*u);}
        private void DrawChapterFrame(MobilePanelLayout layout,float u,string title,string subtitle)
        {

            Rect frame=ChapterRect(new MobilePanelLayout.Area(0,0,layout.Width,layout.Height),u);
            if(!MobileControls.Active){Fill(frame,new Color(.025f,.045f,.068f,1));Border(frame,new Color(.24f,.37f,.44f,.55f));}
            Box(ChapterRect(new MobilePanelLayout.Area(8,4,layout.Width-16,layout.Height-8),u),jade,false);
            DrawChapterSymbol(ChapterRect(new MobilePanelLayout.Area(16,16,14,14),u),session.ChapterFinished?session.ActiveChapterNode:session.SelectedChapterNode,gold);
            Text(ChapterRect(new MobilePanelLayout.Area(36,10,layout.Width-52,28),u),title,Mathf.RoundToInt(22*u),pale,true);
            Text(ChapterRect(new MobilePanelLayout.Area(16,40,layout.Width-32,18),u),subtitle,Mathf.RoundToInt(12*u),muted);
        }
        // Shared procedural marks avoid platform font dependencies: tree, stepped rock, star.
        private void DrawChapterSymbol(Rect r,ChapterNode node,Color color)
        {
            if(node==ChapterNode.ForestCourt)
            {Fill(new Rect(r.x+r.width*.45f,r.y,r.width*.1f,r.height),color);Fill(new Rect(r.x,r.y+r.height*.25f,r.width,r.height*.15f),color);Fill(new Rect(r.x+r.width*.15f,r.y+r.height*.55f,r.width*.7f,r.height*.15f),color);}
            else if(node==ChapterNode.Redrock)
            {Fill(new Rect(r.x,r.y+r.height*.4f,r.width,r.height*.6f),color);Fill(new Rect(r.x+r.width*.25f,r.y,r.width*.5f,r.height*.5f),color);}
            else
            {Fill(new Rect(r.x+r.width*.4f,r.y,r.width*.2f,r.height),color);Fill(new Rect(r.x,r.y+r.height*.4f,r.width,r.height*.2f),color);}
        }
        private void DrawChapterResult()
        {
            if(!ReferenceEquals(chapterResultScrollOwner,session.ChapterRun))
            {chapterResultScrollOwner=session.ChapterRun;chapterResultScroll=Vector2.zero;CancelMobileScroll();}
            float u=MobileControls.Active?TouchRatio:1;var layout=ChapterPanelGeometry();
            if(!session.ChapterResultReady)
            {
                // Preserve the battlefield while the already-dead boss's actual visual retires.
                Rect badge=new Rect((width-300*u)*.5f,14*u,300*u,42*u);
                Fill(badge,new Color(.025f,.06f,.08f,.85f));Text(badge,session.ChapterRewardPending?"节点完成 · 奖励待保存":"节点完成 · 奖励已保存",Mathf.RoundToInt(14*u),jade,true);
                if(NavigationButton(new Rect((width-180*u)*.5f,height-62*u,180*u,48*u), "继续 · 查看结果", jade)){session.ContinueChapterResult();BlockUITransition();}
                return;
            }
            bool failed=session.ChapterRun==null||session.ChapterRun.Failed,pending=session.ChapterRewardPending;
            DrawChapterFrame(layout,u,failed?"本次星路止步":pending?"结算待保存":"星路线索已记录",ChapterDefinition.Get(session.ActiveChapterNode).Name);
            string copy=MobileControls.Active?ChapterEntryPresentation.Result(session.ChapterResult):ChapterEntryPresentation.DesktopResult(session.ChapterResult);
            if(!string.IsNullOrEmpty(session.Progression.LastError))copy=session.Progression.LastError+"\n\n"+copy;
            bool rewardCards=!MobileControls.Active&&!failed&&!pending&&session.ChapterResult!=null&&!session.ChapterResult.RewardDetailsUnavailable;
            float rewardHeight=rewardCards?92*u:0;
            float h=Style(Mathf.RoundToInt(16*u),false,true).CalcHeight(new GUIContent(copy),(layout.Body.Width-26)*u)+16*u+rewardHeight;
            chapterResultScroll=BeginTouchScroll("chapter-result",ChapterRect(layout.Body,u),chapterResultScroll,new Rect(0,0,(layout.Body.Width-16)*u,Mathf.Max(layout.Body.Height*u,h)));
            if(rewardCards)
            {
                float cw=(layout.Body.Width-42)*.5f;var result=session.ChapterResult;
                for(int i=0;i<2;i++)
                {
                    Rect r=new Rect((8+i*(cw+10))*u,8*u,cw*u,74*u);Fill(r,card);
                    DrawRewardToken(new Rect(r.x+10*u,r.y+6*u,r.width-20*u,36*u),i==0?1:3,i==0?result.Materials:result.KillExperience+result.CompletionExperience,u);
                    Text(new Rect(r.x+10*u,r.y+46*u,r.width-20*u,24*u),i==0?"星烬碎片":"经验",Mathf.RoundToInt(14*u),muted);
                }
            }
            Text(new Rect(8*u,8*u+rewardHeight,(layout.Body.Width-26)*u,h-rewardHeight),copy,Mathf.RoundToInt(16*u),pale,false,true);EndTouchScroll();
            if(pending&&Button(ChapterRect(layout.FooterButton(0,2),u),"重试保存结算",gold)){RetryChapterSettlement();return;}
            if(failed&&Button(ChapterRect(layout.FooterButton(0,2),u),"原条件重试",gold,session.CanRetryChapter)){session.RetryFailedChapter();BlockUITransition();return;}
            if(session.IsDead)
            {if(Button(ChapterRect(layout.FooterButton(1,2),u),"复活",jade)){ReturnFromChapter();return;}}
            else
            {
                if(PopupCloseButton(ChapterRect(layout.Close,u))||Button(ChapterRect(layout.FooterButton(pending||failed?1:0,pending||failed?2:1),u),"继续拾取",jade))
                {session.DismissFinishedResult();BlockUITransition();return;}
            }
        }
    }
}

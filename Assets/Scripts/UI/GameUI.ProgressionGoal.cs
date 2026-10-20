using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private int achievementCategory=-1;
        private Vector2 achievementScroll;
        private sealed class AchievementRow
        {
            internal AchievementDefinition Definition;internal int Progress;internal bool Claimed,Ready;
            internal System.Collections.Generic.List<EntryRewardPreview> Rewards;
        }
        private GameProfile achievementProfile;
        private readonly System.Collections.Generic.List<AchievementRow> achievementRows=new System.Collections.Generic.List<AchievementRow>();
        private readonly System.Collections.Generic.List<AchievementRow> visibleAchievementRows=new System.Collections.Generic.List<AchievementRow>();
        private readonly bool[] achievementReadyCategories=new bool[6];
        private int achievementCompleted,achievementClaimable,achievementCachedCategory=-99;
        private float achievementAttentionAt;
        private bool achievementAttention;
        private bool CachedAchievementAttention
        {
            get
            {
                if(progressionGoalsOpen){RefreshAchievementCache();return achievementClaimable>0;}
                if(Time.unscaledTime>=achievementAttentionAt){achievementAttentionAt=Time.unscaledTime+1;achievementAttention=session.Progression.ClaimableAchievements>0;}
                return achievementAttention;
            }
        }
        private void RefreshAchievementCache()
        {
            var p=session.Progression;
            if(achievementProfile!=p.Profile)
            {
                achievementProfile=p.Profile;achievementRows.Clear();System.Array.Clear(achievementReadyCategories,0,6);achievementCompleted=achievementClaimable=0;
                foreach(var a in p.AvailableAchievements)
                {
                    bool claimed=p.AchievementClaimed(a.Id);int progress=Mathf.Min(a.Target,a.Progress(p.Profile));bool ready=!claimed&&progress>=a.Target;
                    achievementRows.Add(new AchievementRow{Definition=a,Progress=progress,Claimed=claimed,Ready=ready});
                    if(claimed||progress>=a.Target||a.Id.StartsWith("revival/")&&progress>0)achievementCompleted++;
                    if(ready){achievementClaimable++;achievementReadyCategories[0]=true;if(a.Category>=0&&a.Category<5)achievementReadyCategories[a.Category+1]=true;}
                }
                achievementRows.Sort((a,b)=>{int x=a.Claimed?2:a.Ready?0:1,y=b.Claimed?2:b.Ready?0:1;return x!=y?x.CompareTo(y):string.CompareOrdinal(a.Definition.Id,b.Definition.Id);});
                achievementCachedCategory=-99;
            }
            if(achievementCachedCategory==achievementCategory)return;
            achievementCachedCategory=achievementCategory;visibleAchievementRows.Clear();
            foreach(var row in achievementRows)if(achievementCategory<0||row.Definition.Category==achievementCategory)visibleAchievementRows.Add(row);
        }
        private float AchievementTitleHeight(AchievementDefinition a,float width,float u)
        {return Mathf.Max(26,Style(Mathf.RoundToInt(14*u),true,true).CalcHeight(new GUIContent(a.Title),width-20*u)/u);}
        private float AchievementTileHeight(AchievementDefinition a,float width,float u)
        {
            int rewards=(a.Gold>0?1:0)+(a.Shards>0?1:0)+(a.Refinement>0?1:0)+(a.Reforge>0?1:0)+(a.RevivalStones>0?1:0)+(a.GearSlot>=0?1:0)+(a.FashionSlotIndex>=0?1:0);
            return 76+AchievementTitleHeight(a,width,u)-26+Mathf.Max(1,Mathf.CeilToInt(rewards/3f))*32;
        }
        private void DrawAchievements(Rect area,float u)
        {
            RefreshAchievementCache();var p=session.Progression;string[] filters={"全部","成长","战斗","探索","收集","打造"};
            float filterWidth=Mathf.Min(96*u,area.width/6);
            for(int i=0;i<filters.Length;i++)
            {
                Rect tab=new Rect(area.x+i*filterWidth,area.y,filterWidth-4*u,32*u);
                if(TabButton(tab,filters[i],achievementCategory==i-1)&&achievementCategory!=i-1){achievementCategory=i-1;achievementScroll=Vector2.zero;}
                bool ready=achievementReadyCategories[i];
                if(ready)Text(new Rect(tab.xMax-13*u,tab.y,12*u,16*u),"●",Mathf.RoundToInt(10*u),new Color(1,.22f,.2f),true);
            }
            RefreshAchievementCache();var entries=visibleAchievementRows;
            Rect viewport=new Rect(area.x,area.y+42*u,area.width,area.height-42*u);
            int columns=area.width/u>=720?2:1;float gap=10*u,cw=(viewport.width-18*u-(columns-1)*gap)/columns,total=0;
            for(int i=0;i<entries.Count;i+=columns){float height=0;for(int col=0;col<columns&&i+col<entries.Count;col++)height=Mathf.Max(height,AchievementTileHeight(entries[i+col].Definition,cw,u));total+=(height+8)*u;}
            achievementScroll=BeginTouchScroll("achievements",viewport,achievementScroll,new Rect(0,0,viewport.width-18*u,Mathf.Max(viewport.height,total)));
            float tileTop=0,rowSize=0;
            for(int i=0;i<entries.Count;i++)
            {
                if(i%columns==0){if(i>0)tileTop+=rowSize+8*u;rowSize=0;for(int col=0;col<columns&&i+col<entries.Count;col++)rowSize=Mathf.Max(rowSize,AchievementTileHeight(entries[i+col].Definition,cw,u)*u);}
                Rect tile=new Rect((i%columns)*(cw+gap),tileTop,cw,rowSize);
                if(tile.yMax<achievementScroll.y||tile.y>achievementScroll.y+viewport.height)continue;
                var row=entries[i];var a=row.Definition;bool claimed=row.Claimed,ready=row.Ready;int progress=row.Progress;
                Fill(tile,card);Border(tile,ready?gold:muted*.3f);
                float titleHeight=AchievementTitleHeight(a,cw,u),titleExtra=titleHeight-26;
                Text(new Rect(tile.x+10*u,tile.y+6*u,cw-20*u,titleHeight*u),a.Title,Mathf.RoundToInt(14*u),claimed?muted:pale,true,true);
                float barWidth=cw-136*u;
                Fill(new Rect(tile.x+10*u,tile.y+(43+titleExtra)*u,barWidth,5*u),muted*.3f);Fill(new Rect(tile.x+10*u,tile.y+(43+titleExtra)*u,barWidth*progress/a.Target,5*u),jade);
                Text(new Rect(tile.x+10*u,tile.y+(49+titleExtra)*u,barWidth,18*u),progress+" / "+a.Target,Mathf.RoundToInt(10*u),muted);
                var rewards=row.Rewards??(row.Rewards=AchievementRewardPreviews(a));
                float rw=(cw-20*u)/3;
                for(int reward=0;reward<rewards.Count;reward++)
                {
                    var item=rewards[reward];Rect hit=new Rect(tile.x+10*u+(reward%3)*rw,tile.y+(72+titleExtra)*u+(reward/3)*32*u,rw-4*u,30*u);
                    DrawEntryRewardIcon(new Rect(hit.x,hit.y,26*u,26*u),item,u);
                    Text(new Rect(hit.x+33*u,hit.y,rw-37*u,30*u),item.Quantity>0?item.Quantity.ToString():"×1",Mathf.RoundToInt(11*u),item.QualityColor,true);
                    InspectRewardItem(hit,item,inspectionKey:"achievement:"+a.Id+":"+reward);
                }
                if(PrimaryButton(new Rect(tile.xMax-110*u,tile.y+(35+titleExtra)*u,100*u,32*u),claimed?"已领取":ready?"领取":"未完成",gold,ready))
                    Feedback(p.ClaimAchievement(a.Id),"成就奖励已领取");
            }
            EndTouchScroll();
        }
        private bool progressionGoalsOpen;
        private Vector2 progressionGoalScroll,progressionGoalHeaderScroll;
        private ProgressionService progressionGoalOwner;
        private string progressionGoalCharacter;
        private void OpenProgressionGoals()
        {
            if(!CanSwitchFunction)return;PrepareFunctionSwitch();
            panel=Panel.HubUtility;session.SetUIBlocking(true);
            achievementProfile=null;progressionGoalsOpen=true;progressionGoalOwner=session.Progression;
            progressionGoalCharacter=session.Progression.CurrentSlotId;progressionGoalScroll=progressionGoalHeaderScroll=Vector2.zero;
            CancelMobileScroll();BlockUITransition();
        }
        private void ReconcileProgressionGoalSurface()
        {
            ReconcileReforgeSurface();
            if(progressionGoalsOpen&&(panel!=Panel.HubUtility||progressionGoalOwner!=session.Progression||progressionGoalCharacter!=session.Progression.CurrentSlotId))
                progressionGoalsOpen=false;
        }
        private bool CloseProgressionGoalSurface()
        {
            if(CloseReforgeSurface())return true;
            if(!progressionGoalsOpen)return false;
            progressionGoalsOpen=false;panel=Panel.None;session.SetUIBlocking(false);CancelMobileScroll();BlockUITransition();return true;
        }
        private string CurrentProgressionGoalStatus(int runMaterials=0)
        {return session.Progression.ProgressionGoalStatus(runMaterials,session.IsInCamp);}
        private bool DrawProgressionGoalSurface()
        {
            ReconcileProgressionGoalSurface();if(!progressionGoalsOpen)return false;
            float u=MobileControls.Active?TouchRatio:1;var l=new MobileDialogLayout(width/u,height/u);
            blockedRects.Add(new Rect(0,0,width,height));
            Box(BuildPlanRect(l.Frame,u),jade,false);
            RefreshAchievementCache();
            Rect heading=BuildPlanRect(l.Header,u);heading.width=Mathf.Min(heading.width,(l.Frame.X+l.Frame.Width-234)*u-heading.x);
            Text(heading,"成就  ·  已达成 "+achievementCompleted+" / "+achievementRows.Count,Mathf.RoundToInt(18*u),pale,true);
            int available=achievementClaimable;
            if(DrawButton(new Rect((l.Frame.X+l.Frame.Width-222)*u,(l.Frame.Y+12)*u,154*u,32*u),"一键领取"+(available>0?" · "+available:""),ButtonRole.Primary,available>0,fontSize:Mathf.RoundToInt(15*u)))Feedback(session.Progression.ClaimAllAchievements(),"成就奖励已领取");
            DrawAchievements(new Rect(l.Body.X*u,l.Body.Y*u,l.Body.Width*u,(l.Frame.YMax-l.Body.Y-16)*u),u);
            if(PopupCloseButton(new Rect((l.Frame.X+l.Frame.Width-52)*u,(l.Frame.Y+12)*u,40*u,32*u)))CloseProgressionGoalSurface();
            return true;
        }
        private void GoalNode(ref float y,float w,float u,string title,string reason,bool draw)
        {
            GoalParagraph(ref y,w,u,title,16,gold,true,draw);
            GoalParagraph(ref y,w,u,reason,13,muted,false,draw);y+=8;
        }
        private void GoalParagraph(ref float y,float w,float u,string text,int size,Color color,bool bold,bool draw)
        {
            int font=Mathf.RoundToInt(size*u);
            float h=Mathf.Ceil(Style(font,bold,true).CalcHeight(new GUIContent(text),(w-16)*u)/u)+8;
            if(draw)Text(new Rect(8*u,y*u,(w-16)*u,h*u),text,font,color,bold,true);
            y+=h;
        }
        private static string GoalItemTitle(ItemData item)
        {return item.name+" · Lv"+item.level+" · "+GameBalance.RarityName(item.rarity);}
        private void GoalUnavailable(ref float y,float w,float u,string text,bool draw)
        {if(draw)Text(new Rect(8*u,y*u,(w-16)*u,40*u),text,Mathf.RoundToInt(13*u),muted,false,true);y+=48;}
    }
}

using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private void DrawAttachmentWorkshop()
        {merchantMode=1;DrawMerchantService();}
        private void DrawGrowthHudCard()
        {
            if(MobileControls.Active||session.PracticeActive||session.InDungeon||systemHistory)return;
            var goal=session.Progression.SelectedProgressionGoal(session.IsInCamp);
            Rect r=new Rect(18,Mathf.Min(height-156,height-110-AdventureSelectionLayout.LogHeight(session.SystemMessages.Count,systemHistory)-98),282,98);blockedRects.Add(r);Box(r,jade,false);
            DrawIcon(new Rect(r.x+12,r.y+14,24,24),UIIconAtlas.Utility("confirm"),jade);
            Text(new Rect(r.x+46,r.y+10,r.width-58,25),goal.Title,14,pale,true);
            Text(new Rect(r.x+12,r.y+43,r.width-24,38),goal.Step,12,muted,false,true);
            if(GUI.Button(r,GUIContent.none,invisibleButton))NavigateProgressionGoal(goal);
        }
        private void NavigateProgressionGoal(ProgressionGoalState goal)
        {
            if(!CanSwitchFunction)return;
            PrepareFunctionSwitch();
            if(!session.IsInCamp)
            {
                session.ReturnToCamp();
                if(!session.IsInCamp)return;
            }
            if(goal.Identity.StartsWith("main/chapter/")||goal.Done)
            {
                if(goal.Identity.StartsWith("main/chapter/"))session.SelectedChapterNode=(ChapterNode)int.Parse(goal.Identity.Substring("main/chapter/".Length));
                OpenChapterSelection();
            }
            else if(goal.RequiredAdventureTier>0)
            {
                session.Player.Teleport(WorldTraversal.NearestWalkable(new Vector3(0,0,11),.45f));
                session.SelectedArenaMode=-1;adventureChapterSelected=false;session.EnterDungeon();
                session.SelectedDungeonTier=Mathf.Min(Mathf.Max(1,goal.RequiredAdventureTier),session.MaximumDungeonTier);
            }
            BlockUITransition();
        }
    }
}

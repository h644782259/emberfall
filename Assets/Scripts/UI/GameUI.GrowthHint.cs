using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private void DrawObjectiveDirection(Rect r,float u)
        {
            Vector3 destination;
            if(!session.TryMainObjectivePosition(out destination)||session.Player==null)return;
            Vector3 delta=Vector3.ProjectOnPlane(destination-session.Player.transform.position,Vector3.up);
            float yaw=Camera.main==null?session.Player.transform.eulerAngles.y:Camera.main.transform.eulerAngles.y;
            float angle=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg-yaw;
            Rect arrow=new Rect(r.x,r.y,24*u,24*u);var matrix=GUI.matrix;
            GUIUtility.RotateAroundPivot(angle,arrow.center);Text(arrow,"▲",Mathf.RoundToInt(16*u),gold,true,false,TextAnchor.MiddleCenter);GUI.matrix=matrix;
            Text(new Rect(r.x+28*u,r.y,r.width-28*u,24*u),delta.magnitude<3?"已到达":Mathf.CeilToInt(delta.magnitude)+" 米",Mathf.RoundToInt(11*u),gold,true,false,TextAnchor.MiddleLeft);
        }
        private bool TryGrowthHudHint(out string title,out string detail)
        {
            if (!MobileControls.Active && !session.InDungeon && !session.SpecialAdventure && !session.IsDead && !session.PracticeActive)
            {
                var goal = session.Progression.SelectedProgressionGoal(session.IsInCamp);
                title = goal.Title;
                detail = goal.Step + " · 点击查看与定位";
                return true;
            }
            return ProgressionHudHint.TryGet(session.Progression,session.IsInCamp?Attention:null,
                session.InDungeon||session.SpecialAdventure||session.IsDead,session.IsInCamp,ProgressionHudHint.TutorialUsable(session.Progression.Profile,MobileControls.Active,session.ClassTutorialVisible),out title,out detail);
        }
    }
}

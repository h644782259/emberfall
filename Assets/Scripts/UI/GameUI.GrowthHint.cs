using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private float objectiveArrowAngle;
        private float objectiveArrowTarget,objectiveArrowVelocity;
        private int objectiveArrowFrame=-1;
        private Vector3 objectiveArrowDestination;
        private void DrawObjectiveDirection(Rect r,float u)
        {
            Vector3 destination;
            if(!session.TryMainObjectivePosition(out destination)||session.Player==null)return;
            Vector3 delta=Vector3.ProjectOnPlane(destination-session.Player.transform.position,Vector3.up);
            float yaw=Camera.main==null?session.Player.transform.eulerAngles.y:Camera.main.transform.eulerAngles.y;
            float angle=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg-yaw;
            if(objectiveArrowFrame<0||(destination-objectiveArrowDestination).sqrMagnitude>4)
            {objectiveArrowTarget=objectiveArrowAngle=Mathf.Round(angle/15f)*15f;objectiveArrowVelocity=0;}
            if(objectiveArrowFrame!=Time.frameCount)
            {
                // Stable compass sectors with hysteresis: tiny bearing changes
                // cannot keep a 24-pixel arrow rotating between raster samples.
                if(delta.sqrMagnitude>1&&Mathf.Abs(Mathf.DeltaAngle(objectiveArrowTarget,angle))>10.5f)
                    objectiveArrowTarget=Mathf.Round(angle/15f)*15f;
                objectiveArrowAngle=Mathf.SmoothDampAngle(objectiveArrowAngle,objectiveArrowTarget,ref objectiveArrowVelocity,.12f,360f,Mathf.Min(Time.unscaledDeltaTime,.05f));
                if(Mathf.Abs(Mathf.DeltaAngle(objectiveArrowAngle,objectiveArrowTarget))<.15f)
                {objectiveArrowAngle=objectiveArrowTarget;objectiveArrowVelocity=0;}
                objectiveArrowFrame=Time.frameCount;objectiveArrowDestination=destination;
            }
            Rect arrow=new Rect(r.x,r.y,24*u,24*u);var matrix=GUI.matrix;
            DrawIcon(arrow,UIIconAtlas.ObjectiveDirectionRing(),gold);
            // Rotate in the objective card's GUI coordinates, before the screen-scale transform.
            Vector3 pivot=new Vector3(arrow.center.x,arrow.center.y,0);
            GUI.matrix=matrix*Matrix4x4.Translate(pivot)*Matrix4x4.Rotate(Quaternion.Euler(0,0,objectiveArrowAngle))*Matrix4x4.Translate(-pivot);
            // Filtered coverage keeps diagonal edges smooth at HUD scale.
            // Only the arrow rotates; the antialiased rim stays on the same pixels.
            DrawIcon(arrow,UIIconAtlas.ObjectiveDirection(),gold);
            GUI.matrix=matrix;
            Text(new Rect(r.x+28*u,r.y,r.width-28*u,24*u),delta.magnitude<3?"已到达":Mathf.CeilToInt(delta.magnitude)+" 米",Mathf.RoundToInt(11*u),gold,true,false,TextAnchor.MiddleLeft);
        }
        private bool TryGrowthHudHint(out string title,out string detail)
        {
            if (!MobileControls.Active && !session.InDungeon && !session.SpecialAdventure && !session.IsDead && !session.PracticeActive)
            {
                var goal = session.Progression.SelectedProgressionGoal(session.IsInCamp);
                if(goal.Identity=="main/complete"){title=detail=null;return false;}
                title = goal.Title;
                detail = goal.Step + " · 点击查看与定位";
                return true;
            }
            return ProgressionHudHint.TryGet(session.Progression,session.IsInCamp?Attention:null,
                session.InDungeon||session.SpecialAdventure||session.IsDead,session.IsInCamp,ProgressionHudHint.TutorialUsable(session.Progression.Profile,MobileControls.Active,session.ClassTutorialVisible),out title,out detail);
        }
    }
}

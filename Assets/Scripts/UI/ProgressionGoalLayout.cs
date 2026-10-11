using System;
namespace Emberfall
{
    public sealed class ProgressionGoalLayout
    {
        public readonly MobilePanelLayout.Area Status,Action,Candidates;
        public ProgressionGoalLayout(MobilePanelLayout.Area body,float measuredStatus,bool action)
        {
            float actionHeight=action?48:0,gaps=action?16:8;
            float status=Math.Max(0,Math.Min(measuredStatus,body.Height-48-actionHeight-gaps));
            Status=new MobilePanelLayout.Area(body.X,body.Y,body.Width,status);
            Action=new MobilePanelLayout.Area(body.X,Status.YMax+8,body.Width,actionHeight);
            float next=action?Action.YMax+8:Status.YMax+8;
            Candidates=new MobilePanelLayout.Area(body.X,next,body.Width,Math.Max(0,body.YMax-next));
        }
    }
}

using System;
using Emberfall;
public static class BlenderPilotPoseTests
{
    public static string Run()
    {
        int count=0;
        Action<bool,string> check=(ok,label)=>{count++;if(!ok)throw new Exception(label);};
        // Test the existing basic timeline's committed contact at several actual intervals.
        foreach(float interval in new[]{.18f,.23f,.46f,.92f})
        {
            float duration=BasicActionTimeline.Duration(false,interval);
            float contact=BasicActionTimeline.Contact(false);
            var atCommit=BlenderPilotPosePolicy.Select(10,2,1,1,true,true,contact,0);
            check(atCommit.Clip==BlenderPilotClip.Basic,"committed basic wins over move/hit");
            check(Math.Abs(atCommit.NormalizedTime-.52f)<.00001f,"no hidden windup at commit");
            float previous=contact;
            for(int i=0;i<=30;i++)
            {
                float age=duration*contact+duration*(1-contact)*i/30;
                var sample=BlenderPilotPosePolicy.Select(10,2,1,1,true,true,age/duration,0);
                check(sample.NormalizedTime>=previous-.00001f&&sample.NormalizedTime<=1,"monotonic bounded recovery");previous=sample.NormalizedTime;
            }
        }
        var pausedA=BlenderPilotPosePolicy.Select(2,2,1.2f,1,true,false,.31f,1);
        var pausedB=BlenderPilotPosePolicy.Select(900,2,5,1,true,false,.31f,1);
        check(pausedA.Clip==BlenderPilotClip.Skill&&pausedA.NormalizedTime==pausedB.NormalizedTime,"action sampling ignores wall time");
        var move=BlenderPilotPosePolicy.Select(100,2,(float)Math.PI,1,false,false,0,1);
        check(move.Clip==BlenderPilotClip.Move&&Math.Abs(move.NormalizedTime-.5f)<.00001f,"accepted gait phase drives movement");
        check(BlenderPilotPosePolicy.Select(4.5f,2,0,0,false,false,0,1).NormalizedTime==.25f,"isolated preview time");
        check(BlenderPilotPosePolicy.Select(0,2,0,1,false,false,0,.11f).Clip==BlenderPilotClip.Hit,"actual hurt recovery overrides move");
        foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity,-1,2})
        {
            var result=BlenderPilotPosePolicy.Select(0,2,0,0,true,true,invalid,1);
            check(!float.IsNaN(result.NormalizedTime)&&result.NormalizedTime>=0&&result.NormalizedTime<=1,"invalid phase bounded");
        }
        return count+" pilot production pose-clock assertions; no Unity rendering claim";
    }
}

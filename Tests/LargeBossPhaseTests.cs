using System;
using Emberfall;

public static class LargeBossPhaseTests
{
    private static int checks;
    private static void Check(bool okay,string why){checks++;if(!okay)throw new Exception(why);}
    private static void Step(LargeBossPhaseState state,float seconds)
    {for(int i=0;i<(int)Math.Ceiling(seconds/.05f);i++)state.Advance(.05f,true,true);}
    public static string Run()
    {
        checks=0;
        foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity,0,-1})
        {var s=new LargeBossPhaseState();Check(!s.TryBegin(invalid,true),"invalid/dead health cannot begin");s.Advance(invalid,true,true);Check(s.Phase==LargeBossPhase.Combat,"invalid time does not progress");}
        var state=new LargeBossPhaseState();
        Check(!state.TryBegin(.71f,true)&&!state.TryBegin(.7f,false),"threshold and paused gate");
        Check(state.TryBegin(.7f,true)&&state.Interruptible&&state.PhaseNumber==1,"first visible phase");
        Check(!state.TryBegin(.2f,true)&&!state.CommitAnchors(8),"no duplicate phase or invalid mask");
        state.Advance(10,true,true);Check(state.Remaining==LargeBossPhaseState.WindupSeconds,"no attack before placement commitment");
        Check(state.CommitAnchors(7)&&!state.CommitAnchors(7),"placement commits exactly once");
        float before=state.Remaining;for(int i=0;i<20;i++)state.Advance(.1f,false,true);
        Check(before==state.Remaining&&!state.DamagePulse,"menus/background freeze clock and pulse");
        Check(!state.DestroyAnchor(2,0)&&!state.DestroyAnchor(1,3),"stale phase or bad index ignored");
        Step(state,2.15f);Check(state.Phase==LargeBossPhase.Windup,"full 2.2 second readable warning");
        Step(state,.1f);Check(state.Phase==LargeBossPhase.Beam&&!state.Interruptible&&!state.DamagePulse,"beam only after warning, not skill interruptible");
        Check(!state.InterruptWindup(),"beam cannot bypass control policy by interruption");
        Step(state,.4f);Check(!state.DamagePulse,"no immediate damage burst");
        int pulses=0;for(int i=0;i<40;i++){state.Advance(.05f,true,true);if(state.DamagePulse)pulses++;}
        Check(pulses>=2&&pulses<=4&&state.BeamAngle>40&&state.BeamAngle<80,"bounded sweep speed and tick cadence");
        Check(state.DestroyAnchor(1,0)&&!state.DestroyAnchor(1,0),"anchor resolves once");
        Check(state.DestroyAnchor(1,2)&&state.Phase==LargeBossPhase.Beam,"remaining anchor sustains beam");
        Check(state.DestroyAnchor(1,1)&&state.Phase==LargeBossPhase.Exposed&&!state.DamagePulse,"last anchor stops damage immediately");
        Check(state.IncomingMultiplier==1.35f&&state.OwnsAttacks,"exposure is bonus damage, not invulnerability");
        Step(state,6.1f);Check(state.Phase==LargeBossPhase.Combat&&state.IncomingMultiplier==1,"bonus expires");
        Check(!state.TryBegin(.3f,true),"no chained mechanics without recovery");
        Step(state,4.1f);Check(state.TryBegin(.35f,true)&&state.PhaseNumber==2,"second health threshold");
        Check(state.BeamAngle==0,"second windup resets direction before warning instead of snapping at beam release");
        Check(state.CommitAnchors(3)&&state.InterruptWindup()&&!state.InterruptWindup(),"one real windup interruption ends phase");
        Step(state,11);Check(!state.TryBegin(.1f,true),"exactly two health phases");
        foreach(int mask in new[]{0,1,3,7})
        {
            var s=new LargeBossPhaseState();s.TryBegin(.5f,true);s.CommitAnchors(mask);
            if(mask==0)Check(s.Phase==LargeBossPhase.Exposed,"blocked placement fails open");
            else {Step(s,16.4f);Check(s.Phase==LargeBossPhase.Recovery&&!s.DamagePulse,"ignored anchors time out, no permanent lock");}
            s.Advance(.1f,false,false);Check(s.Phase==LargeBossPhase.Finished&&!s.DamagePulse&&!s.OwnsAttacks,"death terminates even when paused");
            s.Dispose();s.Advance(100,true,true);Check(!s.TryBegin(.1f,true)&&!s.DestroyAnchor(1,0)&&!s.InterruptWindup(),"disposed callbacks cannot revive hazard");
        }
        var hitch=new LargeBossPhaseState();hitch.TryBegin(.5f,true);hitch.CommitAnchors(1);hitch.Advance(1000,true,true);
        Check(hitch.Phase==LargeBossPhase.Windup&&hitch.Remaining>2,"long frame cannot skip warning");
        return "Large boss phase checks: "+checks;
    }
}

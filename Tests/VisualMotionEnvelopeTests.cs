using System;
using Emberfall;
public static class VisualMotionEnvelopeTests
{
    public static string Run()
    {
        int n=0;Action<bool,string> check=(ok,msg)=>{n++;if(!ok)throw new Exception(msg);};
        check(VisualMotionEnvelope.RecoveryWeight(0)==1&&VisualMotionEnvelope.RecoveryWeight(.12f)==0,"settle keeps initial pose and finishes in bounded cosmetic interval");
        float prev=1;
        for(int i=0;i<=120;i++){float w=VisualMotionEnvelope.RecoveryWeight(i*.001f);check(w<=prev&&w>=0&&w<=1,"monotone bounded recovery");prev=w;}
        var left=new VisualMotionEnvelope();var right=new VisualMotionEnvelope();
        for(int i=0;i<90;i++)
        {
            left.Advance(-3,-1,1,1f/60);right.Advance(3,1,1,1f/60);
            check(Math.Abs(left.Turn+right.Turn)<.00001f&&Math.Abs(left.SideLoad+right.SideLoad)<.00001f,"left/right responses mirror");
            check(Math.Abs(left.Turn)<=1&&Math.Abs(left.SideLoad)<=1&&Math.Abs(left.ForwardDrag)<=1,"bounded impulses");
        }
        float before=right.Turn;right.Advance(-80,-1,-1,0);check(right.Turn==before,"pause does not advance inertia");
        for(int i=0;i<180;i++)right.Advance(0,0,0,1f/60);
        check(Math.Abs(right.Turn)<.00001f&&Math.Abs(right.SideLoad)<.00001f,"settles after input stops");
        right.Reset();right.Advance(170,0,0,1f/60);check(right.Turn==0,"orientation teleport not interpreted as spin");
        right.Advance(float.NaN,0,0,1f/60);check(!float.IsNaN(right.Turn),"invalid yaw isolated");
        return "PASS: "+n+" cosmetic recovery/inertia invariants (not an animation playback test)";
    }
}

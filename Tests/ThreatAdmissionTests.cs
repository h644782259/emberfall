using System;
using Emberfall;
public static class ThreatAdmissionTests
{
    static int count;
    static void Check(bool value,string label){count++;if(!value)throw new Exception(label);}
    public static string Run()
    {
        for(int n=0;n<3;n++)for(int d=0;d<3;d++)for(int r=0;r<3;r++)
            Check(ThreatAdmissionPolicy.IsPilot(true,n,d,r)==(n==1&&d==1&&r==0),"pilot scope");
        Check(!ThreatAdmissionPolicy.IsPilot(false,1,1,0),"non chapter excluded");
        var p=new ThreatAdmissionPolicy(123);var twin=new ThreatAdmissionPolicy(123);
        Check(p.Request(0)&&twin.Request(0),"first starts");
        Check(p.LastGap>=.35f&&p.LastGap<=.6f&&p.LastGap==twin.LastGap,"seed gap");
        Check(!p.Request(1),"same frame stagger");
        for(int i=0;i<12;i++){p.Advance(.05f);p.Request(1);}
        Check(p.ActiveCount==2,"two admitted");
        Check(!p.Request(2),"cap");p.Release(0);
        for(int i=0;i<12;i++){p.Advance(.05f);p.Request(2);}
        Check(p.ActiveCount==2,"death releases");
        var end=p.LaunchVolley(1,2);Check(!p.Request(1),"owner cannot overlap flying volley");
        p.Advance(99);Check(p.ActiveCount==2,"elapsed time cannot retire live projectiles");
        p.Release(1);Check(p.ActiveCount==2,"source death keeps in-flight danger");
        end();Check(p.ActiveCount==2,"one of two bolts still alive");
        end();Check(p.ActiveCount==1,"last bolt releases volley");
        p.Release(2);Check(p.ActiveCount==0,"true windup interrupt releases");
        p.Reset();p.Request(1);var stale=p.LaunchVolley(1,1);p.Reset();p.Request(1);stale();
        Check(p.ActiveCount==1,"old room callback cannot release new lease");
        p.Reset();Check(p.Request(7),"reset accepts");Check(!p.Request(8),"waiting queued");
        p.Advance(99,true);Check(p.WaitingCount==0&&p.ActiveCount==1,"pause clears only waiting; telegraph retained");
        Check(!p.Request(8),"pause did not consume gap");p.Withdraw(8);Check(p.WaitingCount==0,"ineligible withdraw");
        p.Reset();Check(p.ActiveCount==0&&p.WaitingCount==0,"leave/retry cleanup");
        var served=new int[4];
        for(int tick=0;tick<2400;tick++){
            p.Advance(.05f);
            for(int id=0;id<4;id++)if(p.Request(id)){served[id]++;p.Finish(id);Check(p.LastGap>=.35f&&p.LastGap<=.6f,"gap bounds");}
        }
        foreach(int times in served)Check(times>40,"no starvation under stable update order");
        Check(Math.Abs(served[0]-served[3])<=1,"FIFO rotation");
        p.Reset();p.Request(0);p.Request(1);p.Advance(1);p.Advance(.01f);p.Finish(0);Check(p.Request(2),"stale waiter cannot block after missing a complete simulation pass");
        return "Threat admission: "+count+" checks PASS";
    }
}

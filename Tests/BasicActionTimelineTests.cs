using System;
using Emberfall;
public static class BasicActionTimelineTests
{
    private static int checks;
    private static void Check(bool ok,string message) { checks++; if(!ok) throw new Exception(message); }
    public static string Run()
    {
        checks=0;
        foreach(bool bow in new[]{false,true})
        foreach(float interval in new[]{.18f,.272f,.34f,.46f,.52f,1.2f})
        {
            float contact=BasicActionTimeline.Contact(bow),duration=BasicActionTimeline.Duration(bow,interval);
            Check(Math.Abs(duration*(1-contact)-interval)<.00001f,"recovery ends at actual next attack, including high speed");
            if(bow)
            {
                Check(!BasicActionTimeline.ArrowVisible(contact,true),"arrow gone in projectile emission frame");
                Check(Math.Abs(BasicActionTimeline.BowDraw(contact)-1)<.00001f,"bow starts releasing from drawn string");
                Check(BasicActionTimeline.BowDraw(BasicActionTimeline.BowSettled)==0,"released bow settles");
            }
            foreach(float dt in new[]{1f/60,.2f,.5f})
            {
                float age=contact*duration,previous=contact;
                for(int frame=0;frame<150;frame++)
                {
                    float t=age/duration;
                    Check(t>=previous && t<=1.000001f,"monotonic bounded recovery across hitches");
                    if(bow && t<BasicActionTimeline.ArrowReload) Check(!BasicActionTimeline.ArrowVisible(t,true),"no duplicate held arrow during release");
                    previous=t;age=Math.Min(duration,age+dt);
                }
                Check(Math.Abs(age-duration)<.00001f,"no stuck action after recovery");
            }
        }
        foreach(float invalid in new[]{0f,-1f,float.NaN,float.PositiveInfinity})
            Check(BasicActionTimeline.Duration(true,invalid)>0 && !float.IsInfinity(BasicActionTimeline.Duration(true,invalid)),"invalid duration bounded");
        Check(BasicActionTimeline.ArrowVisible(1,false),"idle has replacement arrow");
        return "PASS: "+checks+" basic action timeline assertions (no rendered-frame validation)";
    }
}

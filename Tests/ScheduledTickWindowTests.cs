using System;
using System.Collections.Generic;
using Emberfall;

public static class ScheduledTickWindowTests
{
    private static int checks;
    private static void Check(bool condition,string why){checks++;if(!condition)throw new Exception(why);}
    private static int RunStatus(float life,float interval,float delta)
    {
        var state=new ScheduledTickWindow(life,interval,interval);int total=0,frames=0;
        while(!state.Complete&&frames++<10000){int due=state.Advance(delta,true);Check(due>=0&&due<=8,"status catch-up bounded");total+=due;}
        Check(state.Complete,"finite status eventually drains");Check(state.Advance(delta,true)==0,"expired status never ticks again");return total;
    }
    private static int RunArea(float startup,float duration,float spacing,float delta)
    {
        float next=startup,age=0;int total=0,frames=0;
        while(!ScheduledTickWindow.Drained(next,startup+duration)&&frames++<10000)
        {age+=delta;int due=ScheduledTickWindow.Collect(ref next,age,startup+duration,spacing);Check(due>=0&&due<=8,"area catch-up bounded");total+=due;}
        Check(ScheduledTickWindow.Drained(next,startup+duration),"area drains before finisher or disposal");return total;
    }
    private static void InterruptedArea(float delta)
    {
        float age=0,next=.25f,end=4.25f;int frames=0,delivered=0,pauses=0;
        bool pause=false,finished=false;var batch=new ScheduledImpactBatch<int>();
        var targets=new List<int>{0,1,2};var impacts=new int[3];
        while(!finished&&frames++<200)
        {
            if(pause)
            {
                float frozenNext=next,frozenAge=age;
                Check(batch.Pending,"area retains the partially delivered original pulse during choice pause");
                Check(next==frozenNext&&age==frozenAge,"paused frame does not reserve another area tick");
                pause=false;continue;
            }
            age+=delta;
            for(int tick=0;tick<ScheduledTickWindow.MaximumCatchUp;tick++)
            {
                if(!batch.Pending)
                {
                    if(ScheduledTickWindow.Collect(ref next,age,end,.7f,1)==0)break;
                    batch.Begin(targets,true);
                }
                int target;
                while(batch.TryTake(out target))
                {
                    impacts[target]++;delivered++;
                    if((delivered==1||delivered==8)&&pauses<2){pause=true;pauses++;break;}
                }
                if(pause)break;
            }
            finished=age>=end&&!batch.Pending&&ScheduledTickWindow.Drained(next,end);
        }
        Check(finished&&pauses==2,"interrupted finite area eventually reaches its finisher");
        Check(delivered==18&&impacts[0]==6&&impacts[1]==6&&impacts[2]==6,
            "normal and severe-hitch areas deliver six ticks to each original target despite mid-pulse pauses");
    }
    public static string Run()
    {
        checks=0;
        InterruptedArea(.2f);InterruptedArea(5f);
        foreach(float delta in new[]{1f/60,.2f,.5f,5f})
        {
            Check(RunStatus(4,.75f,delta)==5,"poison 4s has five .75s ticks at every frame rate");
            Check(RunStatus(.7f,.75f,delta)==0,"short poison cannot tick beyond expiry");
            Check(RunStatus(.75f,.75f,delta)==1,"exact poison endpoint tick kept");
            Check(RunStatus(3,.5f,delta)==6,"3s burn has six ticks including endpoint");
            Check(RunStatus(2,.5f,delta)==4,"2s burn preserves intended total damage");
            Check(RunArea(.25f,4,.7f,delta)==6,"summoner poison field startup/end ordering");
            Check(RunArea(.3f,6,.4f,delta)==16,"ranger field catch-up survives 500ms frame");
            Check(RunArea(.2f,4.5f,.5f,delta)==10,"arcanist field includes startup and last tick");
            Check(RunArea(0,3.6f,.45f,delta)==9,"vanguard field final boundary stable");
            Check(RunArea(.55f,0,1,delta)==1,"one delayed impact remains exactly once");
            Check(RunArea(0,2.5f,.5f,delta)==6,"gravity schedules six pulses before 2.6s finisher");
        }
        foreach(bool reverse in new[]{false,true})
        {
            var original=new List<string>{"first","middle","last"};
            var batch=new ScheduledImpactBatch<string>();batch.Begin(original,reverse);
            string hit;Check(batch.TryTake(out hit)&&hit==(reverse?"last":"first"),"impact begins in the controller's original iteration order");
            original.RemoveAt(reverse?2:0);original.Add("new wave");
            Check(batch.Pending,"pause after a kill retains the current pulse");
            bool refused=false;try{batch.Begin(original);}catch(InvalidOperationException){refused=true;}
            Check(refused,"another due tick cannot overwrite an unfinished pulse");
            Check(batch.TryTake(out hit)&&hit=="middle","resuming targets the next original enemy, not a shifted list entry");
            Check(batch.TryTake(out hit)&&hit==(reverse?"first":"last"),"remaining original target is delivered exactly once");
            Check(!batch.TryTake(out hit)&&!batch.Pending,"new-wave additions cannot be hit by an already-claimed pulse");
            batch.Begin(original,reverse);batch.Clear();Check(!batch.Pending&&!batch.TryTake(out hit),"stale owner disposal drops the retained snapshot");
            batch.Begin(new List<string>(),reverse);Check(!batch.TryTake(out hit)&&!batch.Pending,"empty pulse drains without sticking a finisher");
        }
        var paused=new ScheduledTickWindow(3,.5f,.5f);
        Check(paused.Advance(2,false)==0&&paused.Remaining==3,"menu/background freeze does not spend lifetime");
        Check(paused.Advance(.2f,true)==0&&paused.Refresh(3),"refresh while active accepted");
        Check(paused.Advance(.3f,true)==1,"refresh preserves original tick phase");
        float remaining=paused.Remaining;Check(!paused.Refresh(float.NaN)&&paused.Remaining==remaining,"invalid refresh rejected");
        var backlog=new ScheduledTickWindow(3,.1f,.1f);
        Check(backlog.Advance(10,true)==8&&!backlog.Complete&&backlog.Remaining==0,"expiry retains bounded backlog");
        int drained=8;while(!backlog.Complete)drained+=backlog.Advance(.016f,true);Check(drained==30,"no hitch ticks dropped or invented");
        foreach(float delta in new[]{.5f,5f})
        {
            var interrupted=new ScheduledTickWindow(3,.5f,.5f);int actual=0;
            Check(interrupted.Elapse(delta,true),"runtime clock can advance before applying individual impacts");
            Check(interrupted.TryTakeDueTick(true),"first due event can open a choice menu");actual++;
            float frozenRemaining=interrupted.Remaining;
            for(int pauseFrame=0;pauseFrame<10;pauseFrame++)
                Check(!interrupted.Elapse(1,false)&&!interrupted.TryTakeDueTick(false)&&interrupted.Remaining==frozenRemaining,
                    "choice pause neither advances time nor consumes a queued damage event");
            int frames=0;
            while(!interrupted.Complete&&frames++<100)
            {
                interrupted.Elapse(.5f,true);
                for(int tick=0;tick<ScheduledTickWindow.MaximumCatchUp&&interrupted.TryTakeDueTick(true);tick++)actual++;
            }
            Check(interrupted.Complete&&actual==6,"resuming interrupted catch-up delivers every original event exactly once");
            Check(!interrupted.TryTakeDueTick(true),"drained incremental schedule cannot repeat the endpoint");
        }
        var twoEffects=new[]{new ScheduledTickWindow(3,.5f,.5f),new ScheduledTickWindow(3,.75f,.75f)};
        foreach(var effect in twoEffects)effect.Elapse(5,true);
        Check(twoEffects[0].TryTakeDueTick(true),"burn may open the menu before poison is applied");
        Check(twoEffects[1].Remaining==0&&!twoEffects[1].TryTakeDueTick(false),"poison still records elapsed combat time without spending paused backlog");
        int burn=1,poison=0;while(twoEffects[0].TryTakeDueTick(true))burn++;while(twoEffects[1].TryTakeDueTick(true))poison++;
        Check(burn==6&&poison==4,"two independent status clocks retain the full interrupted frame");
        var cleared=new ScheduledTickWindow(3,.1f,.1f);cleared.Advance(10,true);cleared.Clear();
        Check(cleared.Complete&&cleared.Advance(1,true)==0&&!cleared.TryTakeDueTick(true)&&!cleared.Elapse(1,true)&&!cleared.Refresh(3),"death/disposal clears queued ticks irrevocably");
        foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity,-1,0})
        {var safe=new ScheduledTickWindow(3,.5f,.5f);Check(safe.Advance(invalid,true)==0&&safe.Remaining==3,"invalid time cannot corrupt schedule");}
        float cursor=0;Check(ScheduledTickWindow.Collect(ref cursor,100,1,.1f)==8,"static scheduler bound");
        Check(!ScheduledTickWindow.Drained(cursor,1),"caller must keep final backlog alive");
        float saved=cursor;Check(ScheduledTickWindow.Collect(ref cursor,float.NaN,1,.1f)==0&&cursor==saved,"invalid static input cannot move cursor");
        return "Scheduled tick checks: "+checks;
    }
}

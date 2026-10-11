using System;
using Emberfall;
public static class AndroidLifecycleTests
{
    public static string Run()
    {
        int n=0;
        Action<bool,string> check=(ok,message)=>{n++;if(!ok)throw new Exception(message);};
        // A viewport change invalidates ownership even without a new safe rect.
        for(int key=0;key<9;key++)
        {
            var viewport=new TouchViewportState();
            float[] v={568,320,0,0,568,320,1,160,1};
            Func<bool> observe=()=>viewport.Observe(v[0],v[1],v[2],v[3],v[4],v[5],v[6],v[7],(int)v[8]);
            check(!observe(),"first viewport has no stale pointer");
            check(!observe(),"stable viewport");v[key]+=1;
            check(observe(),"every coordinate key invalidates including raw DPI and orientation");
            check(!observe(),"duplicate geometry notification is idle");
            var release=new TouchReleaseLatch();release.Block(1);
            check(release.IsBlocked(60,true,true),"held old pointer quarantined after arbitrary delay");
            check(!release.IsBlocked(60,false,false),"lift clears geometry quarantine");
        }
        // Enumerate all eight-callback focus/suspend histories, including repeats
        // and stale-looking pause(false) while still unfocused.
        for(int sequence=0;sequence<65536;sequence++)
        {
            var pause=new ApplicationPauseState();var audio=new AudioLifecycleGate();var saves=new SaveLifecycleGate();
            bool focus=true,suspended=false,wasBackground=false;int savesExpected=0,savesActual=0;
            int encoded=sequence;
            for(int step=0;step<8;step++)
            {
                int callback=encoded&3;encoded>>=2;
                if(callback<2){focus=callback==1;pause.SetFocus(focus);}else{suspended=callback==3;pause.SetSuspended(suspended);}
                bool background=!focus||suspended;
                var expected=background==wasBackground?AudioLifecycleTransition.None:background?AudioLifecycleTransition.Suspend:AudioLifecycleTransition.Resume;
                check(audio.Observe(pause.BackgroundPaused)==expected,"audio transitions follow merged state only");
                check(audio.BackgroundPaused==background,"merged audio state");
                if(background&&!wasBackground)savesExpected++;
                saves.Observe(background,true,()=>{savesActual++;return true;});
                check(savesActual==savesExpected,"one successful save per actual background episode");
                check(!pause.CanAdvance(true,true,false,false),"resume preserves manual menu pause");
                check(pause.CanAdvance(true,false,false,false)==!background,"only foreground unpaused simulation advances");
                wasBackground=background;
            }
        }
        var retry=new SaveLifecycleGate();int attempts=0;
        check(!retry.Observe(true,true,()=>{attempts++;return false;}),"failed lifecycle save remains retryable");
        check(retry.Observe(true,true,()=>{attempts++;return true;})&&attempts==2,"duplicate callback retries failed save");
        check(retry.Observe(true,true,()=>{attempts++;return true;})&&attempts==2,"successful retry deduplicates later callback");
        return "PASS: "+n+" Android lifecycle state assertions (65536 callback sequences)";
    }
}

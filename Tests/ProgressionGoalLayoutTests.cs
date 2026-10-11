using System;
using Emberfall;
public static class ProgressionGoalLayoutTests
{
    public static string Run()
    {
        int n=0;
        foreach(float width in new[]{568,667,800,1024,1366})foreach(float height in new[]{320,375,540,768})foreach(float measured in new[]{18,48,120,400,2000})foreach(bool action in new[]{false,true})
        {
            var dialog=new MobileDialogLayout(width,height);var l=new ProgressionGoalLayout(dialog.Body,measured,action);
            Action<bool,string> check=(b,w)=>{n++;if(!b)throw new Exception(w);};
            check(l.Status.Y>=dialog.Body.Y&&l.Candidates.YMax<=dialog.Body.YMax+.001f,"fixed current section and candidates inside body");
            check(l.Candidates.Height>=48,"at least one touch-sized candidate viewport");
            check(!l.Status.Overlaps(l.Candidates)&&(!action||!l.Action.Overlaps(l.Candidates)&&!l.Status.Overlaps(l.Action)),"progress/action cannot scroll over candidates");
            check(!action||l.Action.Height==48,"fixed action keeps 48-unit touch height");
            check(l.Status.Height<=measured,"long requirements measured once and readable in dedicated header scroll");
        }
        return "PASS: "+n+" fixed goal header/action and scrolling candidate geometry checks";
    }
}

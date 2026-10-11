using System;
using Emberfall;
public static class CameraVisibilityTests
{
    static int checks;
    static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    public static string Run()
    {
        checks=0;
        foreach(float dt in new[]{.001f,.016f,.033f,.1f,1f})
        {
            float alpha=1;
            for(int frame=0;frame<180;frame++){float next=CameraVisibilityRules.FadeStep(alpha,true,dt);Check(next<=alpha&&next>=.2f,"Fade is bounded/monotonic");alpha=next;}
            for(int frame=0;frame<180;frame++){float next=CameraVisibilityRules.FadeStep(alpha,false,dt);Check(next>=alpha&&next<=1,"Restoration monotonic/bounded");alpha=next;}
        }
        foreach(float dt in new[]{0,-1,float.NaN,float.PositiveInfinity})Check(CameraVisibilityRules.FadeStep(.7f,true,dt)==.7f,"Invalid time cannot corrupt alpha");
        for(int i=0;i<=100;i++)
        {
            float anchor=i/100f,projection=CameraVisibilityRules.Projection(anchor);
            Check(projection>=-.5f&&projection<=.5f,"Finite camera bias bound");
            Check(Math.Abs((1-projection)/2-Math.Max(.25f,Math.Min(.75f,anchor)))<.00001f,"Projection places hero at requested safe anchor");
        }
        foreach(float normal in new[]{13f,19f,25f})
        {Check(CameraVisibilityRules.Zoom(normal,2)>=13&&CameraVisibilityRules.Zoom(normal,2)<=normal,"Near-building zoom has lower bound");Check(CameraVisibilityRules.Zoom(normal,5)==normal,"Distant scenery leaves normal zoom");}
        Check(CameraVisibilityRules.MaximumFaded==32&&CameraVisibilityRules.MaximumSurfaces==256,"Resource budgets finite");
        return checks+" camera fade/zoom/projection rule assertions passed";
    }
}

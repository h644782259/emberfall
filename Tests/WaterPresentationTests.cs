using System;
using Emberfall;
public static class WaterPresentationTests
{
    public static string Run()
    {
        int n=0;Action<bool,string> check=(ok,msg)=>{n++;if(!ok)throw new Exception(msg);};
        foreach(WaterEnvironment environment in Enum.GetValues(typeof(WaterEnvironment)))foreach(float width in new[]{float.NaN,float.PositiveInfinity,-1f,0,.5f,2.4f,3.2f,8,100})
        {
            var p=new WaterPresentation(environment,width);
            check(p.Width>=.5f&&p.Width<=8,"bounded finite width");
            check(p.DeepWidth<p.Width&&p.DeepWidth>p.Width*.7f,"visible shallow margins around deeper center");
            check(p.CurrentWidth>=p.Width*.25f&&p.CurrentWidth<=p.Width*.34f,"broad current instead of thin decorative line");
            check(Math.Abs(p.CurrentOffset)+p.CurrentWidth*.5f<p.DeepWidth*.5f,"current stays inside deep strip");
            check(p.CurrentBrightness>=.88f&&p.CurrentBrightness<=1,"shared restrained palette");
        }
        var brook=new WaterPresentation(WaterEnvironment.Brook,3.2f);var yard=new WaterPresentation(WaterEnvironment.Courtyard,3.2f);var tactical=new WaterPresentation(WaterEnvironment.Tactical,3.2f);
        check(brook.CurrentWidth!=yard.CurrentWidth&&yard.CurrentWidth!=tactical.CurrentWidth,"same palette different environment parameters");
        check(WaterPresentation.SurfaceLayers==3&&WaterPresentation.ContactStrips==4,"bounded visual geometry budget");
        return "PASS: "+n+" water depth/current profile invariants (no shader or visual validation)";
    }
}

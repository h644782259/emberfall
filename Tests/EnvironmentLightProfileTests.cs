using System;
using Emberfall;
public static class EnvironmentLightProfileTests
{
    public static string Run()
    {
        int n=0;Action<bool,string> check=(ok,msg)=>{n++;if(!ok)throw new Exception(msg);};
        foreach(int hub in new[]{int.MinValue,0,1,2,int.MaxValue})foreach(bool dungeon in new[]{false,true})
        {
            var p=new EnvironmentLightProfile(dungeon,hub);
            check(p.Hub>=0&&p.Hub<=2,"hub index bounded");
            check(p.KeyIntensity>=1.1f&&p.KeyIntensity<=1.35f&&p.FillIntensity>=.28f&&p.FillIntensity<=.37f,"readable bounded key/fill");
            check(p.AmbientScale>=.92f&&p.FogDensity>=.008f&&p.FogDensity<=.016f,"ground readability preserved");
            if(dungeon)check(p.KeyIntensity==1.1f&&p.FillIntensity==.28f&&p.FogDensity==.016f,"dungeon baseline preserved");
        }
        var caravan=new EnvironmentLightProfile(false,0);var quarry=new EnvironmentLightProfile(false,1);var astral=new EnvironmentLightProfile(false,2);
        check(caravan.KeyElevation!=quarry.KeyElevation&&quarry.KeyElevation!=astral.KeyElevation,"three hub light directions differentiated");
        check(EnvironmentLightProfile.TownAccentLights==2&&EnvironmentLightProfile.AccentRange<EnvironmentLightProfile.PortalRange,"fixed local accents and distinct portal reach");
        return "PASS: "+n+" environment lighting policy checks (no rendered-frame or device performance validation)";
    }
}

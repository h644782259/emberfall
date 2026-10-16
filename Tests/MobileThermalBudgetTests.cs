using System;
using Emberfall;
public static class MobileThermalBudgetTests
{
    public static string Run()
    {
        int checks=0;Action<bool,string> c=(value,why)=>{checks++;if(!value)throw new Exception(why);};
        var p=new MobileThermalBudget();
        c(p.FrameRate(60)==60,"normal combat stays smooth");
        p.Observe(1,0);c(!p.Limited && p.ReducedRendering && p.FrameRate(60)==60,"fair reduces rendering before sacrificing input cadence");
        p.Observe(2,2);c(p.FrameRate(60)==30,"serious heat limits combat");
        c(p.FrameRate(15)==15&&p.FrameRate(30)==30,"thermal limit never raises background/menu budget");
        p.Observe(0,4);p.Observe(0,63);c(p.Limited,"no early recovery");
        p.Observe(1,64);p.Observe(0,66);p.Observe(0,124);c(p.Limited,"fair heat resets cooling window");
        p.Observe(-1,0);p.Observe(0,200);c(p.Limited,"background or unknown state cannot count as cooling");
        p.Observe(0,260);c(!p.Limited&&p.FrameRate(60)==60,"sustained nominal temperature restores combat");
        p.Observe(3,262);c(p.Limited && p.FrameRate(60)==15,"critical state immediately caps at 15");
        p.Observe(1,264);c(p.FrameRate(60)==15,"falling heat cannot prematurely restore load");
        p.Observe(0,266);p.Observe(0,326);c(!p.ReducedRendering,"sustained cooling restores lighting as well as cadence");
        p.Observe(1,328);p.Observe(-1,330);c(p.ReducedRendering,"unknown reading preserves early protection");
        p.Observe(0,400);p.Observe(0,459);c(p.ReducedRendering,"early protection also requires full cooldown");
        p.Observe(0,460);c(!p.ReducedRendering,"early protection recovers after continuous nominal readings");
        foreach(var size in new[]{new[]{2622,1206},new[]{1206,2622},new[]{2732,2048},new[]{1280,720},new[]{1920,1080}})
        {
            int w,h;MobileThermalBudget.RenderSize(size[0],size[1],out w,out h);
            c(w<=size[0]&&h<=size[1]&&Math.Max(w,h)<=1920,"never upscale; cap phone and tablet pixels");
            c(Math.Abs((double)w/h-(double)size[0]/size[1])<.003,"preserve screen aspect and orientation");
        }
        var idle=new MobileIdleFrameBudget();
        c(idle.FrameRate(60,true,true,100)==60,"touch immediately restores activity");
        c(idle.FrameRate(60,true,false,109)==60,"brief stops retain smoothness");
        c(idle.FrameRate(60,true,false,110)==60,"stationary foreground retains full cadence");
        c(idle.FrameRate(60,true,false,160)==60,"long foreground idle stays smooth");
        c(idle.FrameRate(60,true,true,161)==60,"touch, movement or combat restores full cadence");
        c(idle.FrameRate(60,false,false,300)==60,"dungeon and practice never use camp idle budget");
        c(idle.FrameRate(15,false,false,400)==15,"never raises a background cap");
        c(idle.FrameRate(60,true,false,401)==60,"resume does not inherit time spent in background");
        return "PASS: "+checks+" mobile thermal/render budget assertions";
    }
}

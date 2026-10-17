using System;
using Emberfall;
public static class MobileSkillPositionTests
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static bool Near(float a,float b){return Math.Abs(a-b)<.01f;}
    public static void Run()
    {
        foreach(bool ipad in new[]{false,true})
        {
            var original=new MobileControlLayout(2420,1668,264,0,ipad);
            var reset=new MobileControlLayout(2420,1668,264,0,ipad,new float[2]);
            var baseline=original.CombatButtons();
            var moved=new MobileControlLayout(2420,1668,264,0,ipad,new[]{-.03f,-.02f});
            var buttons=moved.CombatButtons();
            for(int i=0;i<buttons.Length;i++)
            {
                Check(Near(baseline[i].X,reset.CombatButtons()[i].X)&&Near(baseline[i].Y,reset.CombatButtons()[i].Y),"reset preserves default");
                Check(Near(buttons[i].X-baseline[i].X,-original.Width*.03f)&&Near(buttons[i].Y-baseline[i].Y,-original.Height*.02f),"every combat button translates together");
                Check(Near(buttons[i].Width,baseline[i].Width),"hitbox size unchanged");
            }
            Check(Near(moved.Potion.X,original.Potion.X)&&Near(moved.PlayerHealth.Y,original.PlayerHealth.Y),"potion and vitals stay fixed");
            Check(Near(moved.Cancel.Y,moved.Jump.Y),"cancel follows jump");
            Check(Near(moved.CounterOpportunity.Y-original.CounterOpportunity.Y,moved.Attack.Y-original.Attack.Y),"attack hints follow attack");
            Check(Near(moved.SkillOpportunities[0].X,moved.Skills[0].X)&&Near(moved.SkillOpportunities[5].X,moved.Skills[0].X),"both pages follow moved slots");
            foreach(float offset in new[]{-100f,100f})
            {
                var clamped=new MobileControlLayout(2420,1668,264,0,ipad,new[]{offset,offset});var b=clamped.CombatButtonBounds();
                Check(b.X>=7.99f&&b.Y>=7.99f&&b.X+b.Width<=clamped.Width-7.99f&&b.Y+b.Height<=clamped.Height-19.99f,"whole group clamped inside viewport");
                Check(Near(clamped.Attack.X-clamped.Skills[0].X,original.Attack.X-original.Skills[0].X),"clamping preserves relative positions");
            }
            var invalid=new MobileControlLayout(2420,1668,264,0,ipad,new[]{float.NaN,float.PositiveInfinity});
            Check(Near(invalid.Attack.X,original.Attack.X),"invalid settings ignored");
        }
        Console.WriteLine("Combat group position checks passed");
    }
}

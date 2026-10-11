using System;
using Emberfall;
public static class CombatOpportunityTests
{
    static int n;static void Check(bool value,string reason){n++;if(!value)throw new Exception(reason);}
    public static string Run()
    {
        n=0;Check(CombatOpportunityPresentation.Vanguard(0)=="","Expired counter has no stale notice");
        Check(CombatOpportunityPresentation.Vanguard(.5f).Contains("0.5"),"Actual remaining counter");
        Check(CombatOpportunityPresentation.Arcanist(false,false,false,false)=="","No target effects means no opportunity");
        Check(CombatOpportunityPresentation.Arcanist(true,true,false,true)=="霜痕 · 可碎冰","Only one prioritized message");
        Check(CombatOpportunityPresentation.Arcanist(true,false,false,false)=="目标霜痕","Locked or cooldown shatter does not claim cast ready");
        Check(CombatOpportunityPresentation.Arcanist(false,true,false,false)=="目标灼烧","Burn is real current target state");
        Check(CombatOpportunityPresentation.Arcanist(false,true,true,true)=="灼烧 · 陨星续燃","Burn route offers actual meteor refresh");
        Check(CombatOpportunityPresentation.Arcanist(true,true,true,true)=="灼烧 · 陨星续燃","Unrelated frost cannot mask Burn opportunity");
        Check(CombatOpportunityPresentation.Arcanist(true,true,true,false)=="目标灼烧","Blocked Burn route keeps its relevant current status");
        Check(CombatOpportunityPresentation.Arcanist(false,true,false,true)=="当前落点可碎冰","Actual AoE shatter can be available beyond the displayed target");
        Check(CombatOpportunityPresentation.Arcanist(true,false,true,true)=="目标霜痕","Burn route cannot claim shatter or refresh absent burn");
        Check(CombatOpportunityPresentation.Arcanist(false,false,true,true)=="","No burning target means no refresh opportunity");
        Check(CombatOpportunityPresentation.MeteorReady(true,0,20,20,false),"Exact sufficient energy is ready");
        foreach(var unavailable in new[]{
            CombatOpportunityPresentation.MeteorReady(false,0,100,20,false),
            CombatOpportunityPresentation.MeteorReady(true,1,100,20,false),
            CombatOpportunityPresentation.MeteorReady(true,.001f,100,20,false),
            CombatOpportunityPresentation.MeteorReady(true,0,19,20,false),
            CombatOpportunityPresentation.MeteorReady(true,0,100,20,true)})
        {
            Check(!unavailable,"Locked/CD including sub-display remainder/noenergy/charge or airborne suppress ready");
            Check(CombatOpportunityPresentation.Arcanist(true,true,true,unavailable)=="目标灼烧","Unavailable spell cannot advertise refresh");
        }
        foreach(bool frost in new[]{false,true})foreach(bool burn in new[]{false,true})foreach(bool route in new[]{false,true})foreach(bool ready in new[]{false,true})
        {
            string caption=CombatOpportunityPresentation.Arcanist(frost,burn,route,ready);
            Check(!caption.Contains("\n")&&!(caption.Contains("碎冰")&&caption.Contains("续燃")),"Exactly one class opportunity line");
        }
        Check(CombatOpportunityPresentation.Ranger(0,false)=="","No poison no notice");
        Check(CombatOpportunityPresentation.Ranger(3,true)=="三层毒 · 易伤","Vulnerable and three stacks on one line");
        Check(CombatOpportunityPresentation.Summoner(0,float.PositiveInfinity,0)=="暂无伙伴","No stale roster");
        Check(CombatOpportunityPresentation.Summoner(2,float.PositiveInfinity,0)=="伙伴 2 · 常驻","Permanent roster no fake lifetime");
        Check(CombatOpportunityPresentation.Summoner(3,4.2f,0).Contains("5秒"),"Shortest live contract rounded up");
        Check(CombatOpportunityPresentation.Summoner(3,4.2f,6).Contains("契机"),"Command opportunity takes single line priority");
        Check(CombatOpportunityPresentation.Summoner(3,4.2f,6).Contains("5秒"),"Contract lifetime stays visible beside command opportunity");
        return n+" combat opportunity presentation assertions passed";
    }
}

using System;
using Emberfall;
public static class DestructiblePropTests
{
    private static int checks;
    private static void Check(bool v,string m){checks++;if(!v)throw new Exception(m);}
    public static string Run()
    {
        checks=0;
        foreach(DestructibleKind kind in Enum.GetValues(typeof(DestructibleKind)))
        {
            float previous=0;
            for(int level=1;level<=100;level++)
            {
                var prop=new DestructiblePropRules(kind,level);
                Check(prop.Health>previous&&prop.Health<=2000,"level health finite monotonic bounded");previous=prop.Health;
                Check(!prop.TryClaimRecovery(),"unbroken prop not rewarded");
                var hit=prop.Hit(1,prop.MaxHealth*.2f);Check(hit.Applied&&!hit.Broke,"real partial impact");
                float health=prop.Health;
                Check(!prop.Hit(1,prop.MaxHealth*.2f).Applied&&prop.Health==health,"same cast equal tick cannot multiply damage");
                prop.Hit(1,prop.MaxHealth*.4f);Check(Math.Abs(prop.Health-prop.MaxHealth*.6f)<.001f,"strong finisher raises cast contribution only to maximum");
                Check(!prop.Hit(1,1).Applied,"weaker repeated projectile ignored");
                var fatal=prop.Hit(2,float.MaxValue);Check(fatal.Broke&&prop.Health==0,"new attack can break once with finite clamp");
                Check(prop.TryClaimRecovery()&&!prop.TryClaimRecovery(),"exactly one recovery claim");
                Check(!prop.Hit(3,9999).Applied,"broken target cannot retrigger");prop.Dispose();Check(!prop.TryClaimRecovery(),"disposed target cannot reward");
            }
        }
        var state=new DestructiblePropRules(DestructibleKind.Rubble,100);
        foreach(float value in new[]{0,-1,float.NaN,float.PositiveInfinity,float.NegativeInfinity})Check(!state.Hit(1,value).Applied,"invalid damage rejected");
        Check(!state.Hit(0,10).Applied&&!state.Hit(-1,10).Applied,"invalid cast rejected");
        for(int cast=1;cast<=200;cast++)Check(state.Hit(cast,.01f).Applied,"bounded recent-cast memory accepts new casts");
        Check(!state.Hit(1,50).Applied,"evicted stale cast cannot resurrect damage");
        Check(state.Hit(200,50).Applied,"recent finisher still valid");
        state.Dispose();Check(!state.Hit(201,1).Applied,"disposed hit rejected");
        var min=new DestructiblePropRules(DestructibleKind.Pot,int.MinValue);var max=new DestructiblePropRules(DestructibleKind.Pot,int.MaxValue);
        Check(min.MaxHealth==15&&max.MaxHealth==312,"invalid levels clamp");
        Check(Math.Abs(PropImpactGeometry.EntryFraction(0,0,10,0,5,0,1)-.4f)<.0001f,"swept projectile enters near surface");
        Check(PropImpactGeometry.EntryFraction(0,0,10,0,5,3,1)>1,"projectile miss");
        Check(PropImpactGeometry.EntryFraction(5,0,5,0,5,0,1)==0,"starts inside hit");
        Check(PropImpactGeometry.EntryFraction(0,0,0,0,5,0,1)>1,"stationary miss");
        Check(PropImpactGeometry.EntryFraction(0,0,10,0,5,1,1)==.5f,"tangent contact");
        Check(PropImpactGeometry.EntryFraction(float.NaN,0,10,0,5,0,1)>1,"nonfinite sweep rejected");
        Check(PropImpactGeometry.EntryFraction(0,0,10,0,5,0,-1)>1,"negative radius rejected");
        float near=PropImpactGeometry.EntryFraction(0,0,10,0,3,0,.5f),far=PropImpactGeometry.EntryFraction(0,0,10,0,7,0,.5f);
        Check(near<far,"front prop/enemy collision can be resolved first");
        Check(DestructiblePropRules.MaximumProps==12&&DestructiblePropRules.RememberedCasts<=64,"explicit registry/history budgets");
        return "PASS: "+checks+" destructible rule/impact assertions";
    }
}

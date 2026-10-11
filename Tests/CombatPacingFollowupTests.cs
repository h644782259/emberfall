using System;
using Emberfall;

public static class CombatPacingFollowupTests
{
    private static int checks;
    private static void Check(bool okay,string why){checks++;if(!okay)throw new Exception(why);}
    public static string Run()
    {
        checks=0;
        foreach(float dt in new[]{1f/60,.2f,.5f})foreach(bool progressing in new[]{false,true})
        {
            var budget=new BossAdvanceBudget();float elapsed=0,distance=15;int frames=0;
            while(!budget.FallbackActive&&frames++<200)
            {elapsed+=dt;if(progressing)distance-=dt*2;budget.Advance(dt,distance,true);}
            Check(budget.FallbackActive&&elapsed>=1.49f&&elapsed<=2.01f,"stalled and moving approach both release far attack gate within two seconds");
            Check(!budget.Advance(dt,distance,true),"fallback stays open until next real attack/reset");
            budget.Reset();Check(!budget.FallbackActive&&budget.Advance(dt,12,true),"new attack gets its own approach budget");
            Check(!budget.Advance(dt,7,false)&&!budget.FallbackActive,"reaching charge range clears chase budget");
        }
        foreach(float distance in new[]{8.501f,9f,9.5f,12f,16f})
        {
            var approach=new BossAdvanceBudget();
            for(int frame=0;frame<12;frame++)approach.Advance(.2f,distance,true);
            var selected=BossAttackPolicy.Select(distance,BossAttackPolicy.Move.Fan,1);
            Check(BossAttackPolicy.AfterAdvanceBudget(selected,distance,approach.FallbackActive,true)==BossAttackPolicy.Move.Fan,
                "expired approach chooses a legal visible fan even in the anti-repeat charge band");
            Check(BossAttackPolicy.AfterAdvanceBudget(selected,distance,false,true)==selected,
                "unexpired approach keeps the existing attack preference");
            Check(BossAttackPolicy.AfterAdvanceBudget(selected,distance,true,false)==selected,
                "timeout does not grant ranged attacks through solid cover");
        }
        foreach(float invalidDistance in new[]{-1f,16.01f,float.NaN,float.PositiveInfinity})
            Check(BossAttackPolicy.AfterAdvanceBudget(BossAttackPolicy.Move.Charge,invalidDistance,true,true)==BossAttackPolicy.Move.Charge,
                "fallback cannot widen the real engagement range");
        Check(SummonerDamageRules.MarkTicks==6&&Math.Abs(SummonerDamageRules.MarkTicks*SummonerDamageRules.MarkTickCoefficient+SummonerDamageRules.MarkFinisherCoefficient-4.4f)<.0001f,"gravity runtime budget is six pulses plus finisher");
        for(int rank=1;rank<=3;rank++)
            Check(SummonerDamageRules.ThornTicks(rank)==(rank==1?6:rank==2?8:9),"thorn event count comes from production duration/interval");
        for(int form=0;form<3;form++)
        {
            Check(CompanionRules.AttackCoefficient(form)>0&&CompanionRules.AttackInterval(form)>0,"pet cadence and coefficient available to schedule harness");
            Check(CompanionRules.ContractLifetime(form,3,false)==(form==2?16:24),"temporary partner lifetime is shared with runtime");
        }
        var invalid=new BossAdvanceBudget();Check(!invalid.Advance(float.NaN,12,true)&&!invalid.FallbackActive,"invalid frame cannot force ranged fallback");
        for(int rank=1;rank<=3;rank++)
        {
            float life=CompanionRules.RefreshPackLifetime(.1f,rank);
            Check(life==8+rank*2,"full pack is refreshed to its existing rank duration");
            for(int i=0;i<20;i++)life=CompanionRules.RefreshPackLifetime(life,rank);
            Check(life==8+rank*2,"recasts never add unlimited lifetime");
            Check(CompanionRules.PreserveRecastHealth(35,100)==35,"recast cannot heal living pack");
            Check(CompanionRules.PreserveRecastHealth(35,120)==35,"higher rank max health does not refill current HP");
            Check(CompanionRules.PreserveRecastHealth(35,20)==20,"lower cap clamps health safely");
        }
        var opportunity=new CompanionCommandOpportunity();opportunity.Grant(10);
        Check(opportunity.IsProtected(10)&&opportunity.IsProtected(12.99f)&&!opportunity.IsProtected(13),"companion protection lasts exactly three combat seconds");
        Check(Math.Abs(CompanionRules.DamageTaken(100,false,false,true)-50)<.001,"dodge grants 50% pet mitigation");
        Check(Math.Abs(CompanionRules.DamageTaken(100,true,false,true)-27.5f)<.001,"AOE protection composes multiplicatively");
        Check(CompanionRules.DamageTaken(100,false,true,true)==50,"recall and dodge protection cannot double stack");
        Check(opportunity.Remaining(22)==4&&opportunity.TryConsume(22),"token survives full twelve-second contract cooldown");
        Check(!opportunity.TryConsume(22)&&!opportunity.TryConsume(23),"next command token consumes once");
        opportunity.Grant(30);Check(!opportunity.TryConsume(46),"expired token cannot be consumed");
        opportunity.Grant(float.NaN);Check(opportunity.Remaining(47)==0,"invalid grant ignored");
        return "Combat pacing/contract checks: "+checks;
    }
}

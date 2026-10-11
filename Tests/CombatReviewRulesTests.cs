using System;
using Emberfall;
public static class CombatReviewRulesTests
{
    private static int checks;
    private static void Check(bool value,string why) { checks++;if(!value)throw new Exception(why); }
    public static string Run()
    {
        checks=0;
        foreach(float initial in new[]{6f,10f,2f}) foreach(float dt in new[]{1f/60,.2f,.5f})
        {
            var budget=new BossAdvanceBudget();float elapsed=0;
            bool blocked=initial==2;
            while(elapsed<3&&!budget.FallbackActive)
            { elapsed+=dt; budget.Advance(dt,initial,BossAttackPolicy.PreferredApproach(1,initial,BossAttackPolicy.Move.Slam,0,!blocked)); }
            Check(budget.FallbackActive && elapsed<=BossAdvanceBudget.MaximumApproach+dt+.001f,"heavy retreat/pillar/pointblank obstruction cannot force unbounded close approach");
            var selected=BossAttackPolicy.LegalFallback(BossAttackPolicy.Move.Slam,initial,true,true,true);
            Check(selected==(initial<=8.5f?BossAttackPolicy.Move.Charge:BossAttackPolicy.Move.Fan),"expiry chooses range-legal fallback");
            Check(BossAttackPolicy.LegalFallback(BossAttackPolicy.Move.Slam,initial,true,false,true)==BossAttackPolicy.Move.Fan,"blocked ground may use visible volley");
        }
        Check(!BossAttackPolicy.PreferredApproach(1,2,BossAttackPolicy.Move.Slam,0,true),"reachable close slam has no artificial delay");
        Check(!BossAttackPolicy.InRange(BossAttackPolicy.Move.Charge,10),"charge cannot be scheduled beyond its reach");
        object locked=new object(),bystander=new object();
        Check(!LockedImpactMarkPolicy.ShouldApply(locked,bystander,true,2,.2f),"pierced bystander never inherits locked mark");
        Check(!LockedImpactMarkPolicy.ShouldApply(locked,locked,false,2,.2f),"dead locked target never receives mark");
        Check(!LockedImpactMarkPolicy.ShouldApply(locked,locked,true,0,.2f),"zero accepted impact cannot mark");
        Check(!LockedImpactMarkPolicy.ShouldApply<object>(null,bystander,true,2,.2f),"unlocked projectile cannot retarget mark");
        Check(LockedImpactMarkPolicy.ShouldApply(locked,locked,true,2,.2f),"living actual lock hit carries unchanged mark");
        for(int rank=1;rank<=3;rank++)
        {
            int steps=SkillDamageBudgets.AdvancedSteps(HeroClass.Vanguard,9,rank);float sum=0;
            for(int step=0;step<steps;step++)sum+=SkillDamageBudgets.AdvancedImpact(HeroClass.Vanguard,9,rank,step);
            Check(Math.Abs(sum-(8+(steps-1)*2.2f))<.0001f,"reordering preserves total ultimate budget");
            Check(SkillDamageBudgets.AdvancedImpact(HeroClass.Vanguard,9,rank,0)==8,"main judgment is first");
            Check(SkillDamageBudgets.AdvancedFirstEvent(HeroClass.Vanguard,9)==.15f,"judgment lands .15 seconds after charge release");
        }
        Check(BasicActionTimeline.BlocksBasic(false,.52f,1)&&!BasicActionTimeline.BlocksBasic(false,.66f,1),"basic yields only during important skill pose");
        Check(!BasicActionTimeline.BlocksBasic(false,0,0),"dodge/death cancellation clears priority");
        foreach(SoundCue cue in (SoundCue[])Enum.GetValues(typeof(SoundCue))) for(int busy=0;busy<256;busy++)
        {
            int selected=AudioVoicePolicy.Select(cue,busy,3);
            Check(selected>=-1&&selected<8,"bounded source count");
            if(AudioVoicePolicy.IsCritical(cue))Check(selected>=6,"critical feedback always has reserved voice");
            else Check(selected<6&&(selected<0||(busy&(1<<selected))==0),"ordinary cues cannot steal reserved or occupied voice");
        }
        return "PASS: "+checks+" combat review rules assertions";
    }
}

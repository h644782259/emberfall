using System;
using Emberfall;

public static class CompanionRulesTests
{
    private static int assertions;
    private static void Check(bool value, string message)
    {
        assertions++;
        if (!value) throw new Exception("FAILED: " + message);
    }
    private static bool Near(float a, float b) { return Math.Abs(a - b) < .0001f; }
    public static string Run()
    {
        assertions = 0;
        Check(!CompanionRules.CoordinatedTarget(false, false), "coincidental autonomous attacks cannot activate cooperation");
        Check(CompanionRules.CoordinatedTarget(true, false), "actual player focus authorizes same-target cooperation");
        Check(CompanionRules.CoordinatedTarget(false, true), "a living commanded partner lets another type follow its command target");
        Check(CompanionRules.CoordinatedTarget(true, true), "overlapping focus and command count as one eligible target");
        Check(CompanionRules.CanTransfer(true, 10, true), "living permanent partner can cross encounter boundary");
        Check(!CompanionRules.CanTransfer(false, 10, true), "temporary partners cannot carry into next encounter");
        Check(!CompanionRules.CanTransfer(true, 0, true), "teleport cannot resurrect a defeated permanent partner");
        Check(!CompanionRules.CanTransfer(true, 10, false), "dismissed permanent partners cannot be revived by transfer");
        Check(!CompanionRules.CanTransfer(true, float.NaN, true) && !CompanionRules.CanTransfer(true, float.PositiveInfinity, true), "invalid companion health cannot transfer");
        Check(!CompanionRules.ShouldCreatePartner(true), "living contract partner is commanded, never replaced");
        Check(CompanionRules.ShouldCreatePartner(false), "dead or absent contract partner may be resummoned");
        foreach (bool pack in new[] { false, true })
        {
            Check(CompanionRules.PermanentPartner(true, 0, pack), "base wolf stays permanent in either route");
            Check(!CompanionRules.PermanentPartner(false, 2, pack), "tree ultimate keeps a finite lifetime");
        }
        Check(CompanionRules.PermanentPartner(false, 1, false), "bonded spirit remains permanent");
        Check(!CompanionRules.PermanentPartner(false, 1, true), "pack spirit has an upkeep lifetime");
        Check(CompanionRules.NormalCapacity(false) == 4 && CompanionRules.NormalCapacity(true) == 2, "normal pet budget is four versus two, including foundation");
        Check(Near(CompanionRules.DamageMultiplier(true), 1.6f) && Near(CompanionRules.HealthMultiplier(true), 1.2f), "two-pet equipment has real individual damage and survival compensation");
        Check(Near(CompanionRules.DamageMultiplier(false), 1f) && Near(CompanionRules.HealthMultiplier(false), 1f), "ordinary gear does not inherit twin multipliers");
        Check(Near(4 * CompanionRules.DamageMultiplier(false), 4f) && Near(2 * CompanionRules.DamageMultiplier(true), 3.2f), "two pets do not accidentally gain four-pet aggregate base DPS");
        Check(CompanionRules.RankPower(0) < CompanionRules.RankPower(1), "starter remains weaker than learned command rank");
        for (int learned = 0; learned <= 3; learned++)
        {
            Check(CompanionRules.EffectiveRank(0, true, true, 3, learned, 3) == learned,
                "permanent foundation follows its actual learned wolf rank");
            Check(CompanionRules.EffectiveRank(1, false, true, 3, 3, learned) == learned,
                "bonded spirit follows downgraded or upgraded learned rank without recast");
            Check(CompanionRules.EffectiveRank(1, false, false, 2, 3, learned) == 2 &&
                CompanionRules.EffectiveRank(2, false, false, 2, 3, learned) == 2,
                "timed spirit/tree preserve their cast rank");
            foreach (bool empowered in new[] { false, true })
                Check(Near(CompanionRules.ActiveCommandMultiplier(learned, empowered),
                    CompanionRules.CommandMultiplier(learned) * (empowered ? CompanionRules.EmpoweredCommandMultiplier : 1)),
                    "active command changes rank contribution without losing its paid empowerment");
        }
        Check(CompanionRules.EffectiveRank(1, false, true, 3, 3, -7) == 0 &&
            CompanionRules.EffectiveRank(1, false, true, 1, 3, 99) == 3, "effective rank clamps invalid investment");
        float retainedHealth = 44;
        for (int repeat = 0; repeat < 32; repeat++)
        {
            retainedHealth = CompanionRules.PreserveRecastHealth(retainedHealth, 50);
            retainedHealth = CompanionRules.PreserveRecastHealth(retainedHealth, 79.2f);
            Check(retainedHealth == 44, "rank/health-gear toggles and repeated preset refresh never heal a damaged body");
        }
        Check(CompanionRules.PreserveRecastHealth(60, 50) == 50 && CompanionRules.PreserveRecastHealth(0, 80) == 0,
            "lower maximum only clamps excess HP and cannot revive a dead partner");
        for (int rank = 1; rank <= 3; rank++)
        {
            Check(Near(CompanionRules.RankPower(rank), 1 + (rank - 1) * .3f), "three learned ranks retain power scaling " + rank);
            Check(Near(CompanionRules.PackLifetime(rank), 8 + rank * 2), "reinforcement duration scales with rank " + rank);
            Check(CompanionRules.CommandMultiplier(rank) > 1f, "living partner gets a meaningful command buff " + rank);
            for (int count = 0; count <= 6; count++)
            foreach (bool twin in new[] { false, true })
            {
                int extra = CompanionRules.PackReinforcements(count, rank, twin);
                Check(extra >= 0 && extra <= (rank == 3 ? 3 : 2), "pack cast has a finite rank event budget");
                Check(count + extra <= Math.Max(count, CompanionRules.NormalCapacity(twin)), "pack cast never exceeds available pet slots");
            }
            for (int form = 0; form < 3; form++)
            {
                Check(CompanionRules.HealthFraction(form, rank, false) > 0, "every form has positive health");
                Check(CompanionRules.HealthFraction(form, rank, false) >= CompanionRules.HealthFraction(form, rank - 1, false), "ranks never lower pet max health");
            }
        }
        Check(Near(GameBalance.EffectiveCooldown(HeroClass.Summoner,2,1),14f),"reviewed base wolf contract really has a fourteen-second cooldown");
        for(int rank=1;rank<=3;rank++)
        {
            float wolfCooldown=GameBalance.EffectiveCooldown(HeroClass.Summoner,2,rank);
            var opportunity=new CompanionCommandOpportunity();opportunity.Grant(10f);
            Check(opportunity.Remaining(10f+wolfCooldown)>=2f,
                "perfect-dodge contract token keeps at least two seconds after the actual wolf cooldown at rank "+rank);
            Check(opportunity.TryConsume(10f+wolfCooldown)&&!opportunity.TryConsume(10f+wolfCooldown),
                "the ready wolf command can consume exactly one live token at rank "+rank);
        }
        Check(Near(CompanionRules.DamageTaken(100, false, false), 100), "ordinary direct attacks are not silently negated");
        Check(Near(CompanionRules.DamageTaken(100, true, false), 55), "AOE mitigation protects pets against unavoidable group damage");
        Check(Near(CompanionRules.DamageTaken(100, false, true), 50), "recall has explicit temporary mitigation");
        Check(Near(CompanionRules.DamageTaken(100, true, true), 27.5f), "recall plus AOE mitigates multiplicatively but never grants invulnerability");
        Check(CompanionRules.DamageTaken(float.NaN, true, true) == 0 && CompanionRules.DamageTaken(float.PositiveInfinity, true, true) == 0, "invalid damage cannot corrupt companion health");

        object alpha = new object(), beta = new object();
        var tracker = new CompanionCooperationTracker<object>();
        Check(!tracker.RegisterHit(alpha, 0, 0), "one wolf hit alone cannot cooperate");
        Check(!tracker.RegisterHit(alpha, 0, .4f) && !tracker.RegisterHit(alpha, 0, .8f), "two or more wolves do not qualify as different types");
        Check(!tracker.RegisterHit(beta, 1, .9f), "a spirit hitting a different enemy does not cooperate");
        Check(tracker.RegisterHit(alpha, 1, 1f), "wolf and spirit hitting same target inside window cooperate");
        Check(!tracker.RegisterHit(alpha, 2, 1.1f) && !tracker.RegisterHit(alpha, 0, 1.2f), "third type and repeated impacts cannot recurse during shared cooldown");
        Check(!tracker.RegisterHit(beta, 0, 2.5f), "stale spirit mark cannot trigger later hit");
        Check(!tracker.RegisterHit(alpha, 0, 4f), "expired target marks are purged before cooldown reopens");
        Check(tracker.RegisterHit(alpha, 2, 4.1f), "wolf and tree are also a valid different-type pair");
        Check(!tracker.RegisterHit(null, 1, 5f) && !tracker.RegisterHit(alpha, -1, 5f) && !tracker.RegisterHit(alpha, 3, 5f), "invalid targets or forms never count");
        Check(!tracker.RegisterHit(alpha, 1, float.NaN) && !tracker.RegisterHit(alpha, 1, float.PositiveInfinity) && !tracker.RegisterHit(alpha, 1, -1), "invalid or backwards timestamps cannot bypass proc gate");
        tracker.Clear();
        Check(!tracker.RegisterHit(alpha, 0, 10f) && !tracker.RegisterHit(alpha, 1, 11.51f), "different-type hits outside exact 1.5-second window cannot cooperate");
        tracker.Clear();
        Check(!tracker.RegisterHit(alpha, 0, 20f) && tracker.RegisterHit(alpha, 1, 21.5f), "exact window endpoint is accepted");
        tracker.Clear();
        Check(!tracker.RegisterHit(alpha, 1, 30f), "owner combat epoch reset clears all previous marks");
        Check(!tracker.RegisterHit(alpha, 1, 30f), "same-time duplicate type hit cannot trigger a proc");
        Check(tracker.RegisterHit(alpha, 0, 30f), "two real simultaneous different-type impacts remain valid");
        for (int i = 0; i < 300; i++)
        {
            float time = 40f + i * .01f;
            Check(!tracker.RegisterHit(new object(), i % 3, time), "large crowd with only one hit per target never creates cross-target resonance");
        }
        return "PASS: " + assertions + " companion rules assertions";
    }
}

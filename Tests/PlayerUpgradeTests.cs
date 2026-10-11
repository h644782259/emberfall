using System;
using Emberfall;

public static class PlayerUpgradeTests
{
    private static int assertions;
    private static void Check(bool value, string message)
    {
        assertions++;
        if (!value) throw new Exception("FAILED: " + message);
    }
    private static bool Near(float a, float b, float tolerance = .001f) { return Math.Abs(a - b) < tolerance; }

    public static string Run()
    {
        assertions = 0;
        Check(Near(PlayerUpgradeRules.FindSafeBlinkDistance(4.8f, d => true, d => true), 4.8f), "clear blink keeps full distance");
        float beforeWall = PlayerUpgradeRules.FindSafeBlinkDistance(4.8f, d => d < 2.13f, d => true);
        Check(beforeWall < 2.13f && beforeWall > 2.12f, "solid endpoint shortens to final safe boundary");
        Check(Near(PlayerUpgradeRules.FindSafeBlinkDistance(4.8f, d => d < .2f, d => true), 0), "in-place blink never consumes cooldown");
        Check(Near(PlayerUpgradeRules.FindSafeBlinkDistance(4.8f, d => d < 1.3f || d > 2f, d => true), 1.3f, .001f), "never searches beyond first solid even when endpoint is clear");
        Check(Near(PlayerUpgradeRules.FindSafeBlinkDistance(4.8f, d => true, d => d < 1f || d > 4f), 4.8f), "water can be crossed when far bank is safe");
        float bank = PlayerUpgradeRules.FindSafeBlinkDistance(4.8f, d => true, d => d < 1.2f);
        Check(bank >= 1.1f && bank < 1.2f, "unsafe water landing shortens onto near bank");
        Check(Near(PlayerUpgradeRules.FindSafeBlinkDistance(4.8f, d => d < 2f, d => d < 1f || d > 3f), .96f, .06f), "solid mid-river cannot be crossed to later bank");
        Check(Near(PlayerUpgradeRules.FindSafeBlinkDistance(4.8f, d => d <= 3.1f, d => true), 3.1f, .001f), "arena edge shortens with safe path boundary");
        Check(Near(PlayerUpgradeRules.FindSafeBlinkDistance(float.NaN, d => true, d => true), 0), "invalid distance is rejected");
        Check(Near(PlayerUpgradeRules.FindSafeBlinkDistance(float.PositiveInfinity, d => true, d => true), 0), "infinite distance is rejected");
        Check(Near(PlayerUpgradeRules.FindSafeBlinkDistance(4.8f, d => true, d => d > .1f), 0), "invalid starting ground cannot teleport into safety");

        Check(PlayerUpgradeRules.CanBufferDodge(.1f, 0, 0), "last 160ms of cooldown accepts a dodge buffer");
        Check(PlayerUpgradeRules.CanBufferDodge(0, .15f, .15f), "near-finished traversal can buffer");
        Check(!PlayerUpgradeRules.CanBufferDodge(.17f, 0, 0), "early cooldown inputs are not saved indefinitely");
        Check(!PlayerUpgradeRules.CanBufferDodge(0, 0, .4f), "midair inputs expire instead of surprising player at landing");
        Check(!PlayerUpgradeRules.CanBufferDodge(float.NaN, 0, 0), "invalid buffer inputs cannot pass");
        Check(!PlayerUpgradeRules.CanBufferDodge(0, -1, 0), "negative readiness cannot pass");

        var casts = new RecentCastGate();
        Check(!casts.TryEnter(0) && !casts.TryEnter(-1), "untagged damage cannot trigger combo consumptions");
        Check(casts.TryEnter(1) && !casts.TryEnter(1), "one meteor cast can consume a target only once");
        Check(casts.TryEnter(2) && !casts.TryEnter(1), "interleaved impacts do not reopen prior cast");
        for (int i = 3; i <= 32; i++) Check(casts.TryEnter(i), "independent casts remain eligible " + i);
        Check(!casts.TryEnter(1), "recent queue retains first cast across 32 in-flight identities");
        casts.Clear();
        Check(casts.TryEnter(1), "new owner or combat epoch starts a fresh gate");

        var proc = new CombatProcCooldown();
        Check(proc.TryTrigger(1.5f) && !proc.TryTrigger(1.5f), "proc cannot recurse within same impact");
        proc.Advance(0); proc.Advance(-1); proc.Advance(float.NaN); proc.Advance(float.PositiveInfinity);
        Check(Near(proc.Remaining, 1.5f), "pause and invalid time preserve cooldown");
        proc.Advance(1.49f);
        Check(!proc.TryTrigger(1.5f), "proc remains gated until exact cooldown");
        proc.Advance(.02f);
        Check(proc.TryTrigger(3f), "proc becomes available after elapsed combat time");
        proc.Advance(100);
        Check(!proc.TryTrigger(0) && !proc.TryTrigger(float.NaN), "invalid cooldown cannot enable permanent proc spam");

        Check(Near(PlayerUpgradeRules.ShatterMultiplier(ElementalistSpecialization.None), .5f), "base nova meteor combo exists without specialization");
        Check(Near(PlayerUpgradeRules.ShatterMultiplier(ElementalistSpecialization.Shatter), 1f), "shatter specialization doubles combo bonus");
        Check(Near(PlayerUpgradeRules.ShatterMultiplier(ElementalistSpecialization.Burn), 0), "burn cannot also benefit from shatter specialization");
        Check(Near(PlayerUpgradeRules.MeteorDirectMultiplier(ElementalistSpecialization.None, false), 1), "base meteor unchanged");
        Check(Near(PlayerUpgradeRules.MeteorDirectMultiplier(ElementalistSpecialization.Shatter, false), .85f), "shatter pays 15 percent direct damage tradeoff");
        Check(Near(PlayerUpgradeRules.MeteorDirectMultiplier(ElementalistSpecialization.Burn, false), .8f), "burn pays 20 percent direct damage tradeoff");
        Check(Near(PlayerUpgradeRules.MeteorDirectMultiplier(ElementalistSpecialization.Shatter, true), .68f), "cinder trail and specialization have explicit multiplicative tradeoffs");

        var frost = new OpeningFrostCounter();
        Check(!frost.RecordHit(10, true, false) && !frost.RecordHit(10, true, false), "early mage first two actual hits do not control");
        Check(frost.RecordHit(10, true, false), "third hit on same living target applies early frost");
        Check(!frost.RecordHit(10, true, false), "frost counter resets after proc");
        Check(!frost.RecordHit(11, true, false) && !frost.RecordHit(11, true, false), "changing targets starts a fresh three-hit count");
        Check(!frost.RecordHit(11, false, false), "dead target cannot trigger signature");
        Check(!frost.RecordHit(11, true, false) && !frost.RecordHit(11, true, false), "dead hit also invalidates queued count");
        Check(frost.RecordHit(11, true, true), "learning nova retains three-hit rhythm for the upgraded frost-mark effect");
        Check(!frost.RecordHit(0, true, false), "unconfirmed target does not count");
        frost.Clear();
        Check(!frost.RecordHit(11, true, false), "teleport clears opening hit sequence");

        Check(Near(PlayerUpgradeRules.CounterAfterAttack(0,1.8f,true),1.8f), "new returning-blade bonus survives current swing");
        Check(Near(PlayerUpgradeRules.CounterAfterAttack(2,0,true),0), "old counter consumed by successful swing");
        Check(Near(PlayerUpgradeRules.CounterAfterAttack(2,0,false),2), "whiff preserves remaining old counter");
        var eligible = new RecentCastGate();
        Check(!eligible.TryEnterEligible(10,false), "unfrozen first meteor does not spend reaction eligibility");
        Check(eligible.TryEnterEligible(10,true), "later same-cast meteor may react after nova");
        Check(!eligible.TryEnterEligible(10,true), "one actual reaction per target and cast");
        Check(eligible.TryEnterEligible(11,true), "new cast remains eligible");
        Check(PlayerUpgradeRules.IsInsideArea(1,1,4,true), "player inside circle hit independently");
        Check(PlayerUpgradeRules.IsInsideArea(-2,1,4,true), "companion elsewhere inside same circle also hit");
        Check(!PlayerUpgradeRules.IsInsideArea(4,0,4,true), "outer radius excluded consistently");
        Check(!PlayerUpgradeRules.IsInsideArea(1,0,4,false), "blocked area path protected");
        Check(!PlayerUpgradeRules.IsInsideArea(float.NaN,0,4,true), "invalid geometry cannot hit");
        CombatDamage ordinary = 50f;
        Check(Near(ordinary.Amount, 50) && !ordinary.IsCritical, "plain DoT and proc amounts never get a cosmetic critical flag");
        CombatDamage critical = CombatDamage.Roll(50, .2f, .1f);
        Check(Near(critical.Amount, 82.5f) && critical.IsCritical, "critical metadata matches actual 1.65 multiplier");
        Check(Near(critical.WithoutCritical().Amount,50)&&!critical.WithoutCritical().IsCritical,"DoT strips crit multiplier and flag together");
        Check((critical*.3f).IsCritical&&Near((critical*.3f).Amount,24.75f),"direct sequence budget scaling preserves critical metadata");
        CombatDamage normal = CombatDamage.Roll(50, .2f, .2f);
        Check(Near(normal.Amount, 50) && !normal.IsCritical, "roll threshold preserves existing strict comparison");
        Check(critical.IsCritical && !normal.IsCritical && Near(critical.Amount, 82.5f), "later rolls cannot overwrite queued earlier hit metadata");
        CombatDamage finisher = CombatDamage.Roll(100, .2f, .8f);
        Check(critical.IsCritical && !finisher.IsCritical, "independent finisher roll does not inherit main area critical");
        for (int i = 0; i < 100; i++)
        {
            float roll = i / 100f;
            CombatDamage packet = CombatDamage.Roll(10, .25f, roll);
            Check(packet.IsCritical == (i < 25), "critical display matches actual roll " + i);
            Check(Near(packet.Amount, i < 25 ? 16.5f : 10), "critical damage is multiplied exactly once " + i);
        }
        return "PASS: " + assertions + " player upgrade assertions";
    }
}

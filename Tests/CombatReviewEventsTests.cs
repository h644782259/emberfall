using System;
using Emberfall;
public static class CombatReviewEventsTests
{
    public static string Run()
    {
        int observed=0;
        Action<CombatReviewEvents.Entry> fail=e=>{throw new Exception("sink failure");};
        Action<CombatReviewEvents.Entry> record=e=>{if(e.kind!="hit"||e.actorId!="18446744073709551615"||e.targetId!="18446744073709551614"||e.amount!=5||e.skill!=2||e.detail!="actual")throw new Exception("payload changed");observed++;};
        if(CombatReviewEvents.Enabled)throw new Exception("leaked listener");
        CombatReviewEvents.Emit("unused","0");
        CombatReviewEvents.Observed+=fail;CombatReviewEvents.Observed+=record;
        try {CombatReviewEvents.Emit("hit","18446744073709551615","18446744073709551614",5,2,"actual");if(observed!=1)throw new Exception("sink failure interrupted delivery");}
        finally {CombatReviewEvents.Observed-=fail;CombatReviewEvents.Observed-=record;}
        if(CombatReviewEvents.Enabled)throw new Exception("unsubscribe failed");
        var configs=CombatReviewConfigurations.Create();
        if(configs.Length!=5)throw new Exception("five builds required");
        var ids=new System.Collections.Generic.HashSet<string>();
        foreach(var c in configs){if(c.level!=50||c.skillRanks.Length!=10||!ids.Add(c.id))throw new Exception("invalid review config");}
        configs[0].skillRanks[0]=9;
        if(CombatReviewConfigurations.Create()[0].skillRanks[0]!=1)throw new Exception("mutable shared config");
        return "PASS: review bus payload, disabled path, sink isolation, unsubscribe and five independent level-50 configs";
    }
}

using System;
namespace Emberfall
{
    // Opt-in real-event transport. No subscriber means no recording or retained history.
    public static class CombatReviewEvents
    {
        public struct Entry
        {
            public string kind, detail, actorId, targetId;
            public int skill;
            public float amount;
        }
        public static event Action<Entry> Observed;
        public static bool Enabled { get { return Observed != null; } }
        public static void Emit(string kind, string actorId, string targetId = "0", float amount = 0, int skill = -1, string detail = "")
        {
            var listener = Observed;
            if (listener == null) return;
            // Review instrumentation must never break combat or leak into saves.
            foreach (Action<Entry> sink in listener.GetInvocationList())
                try { sink(new Entry { kind=kind, actorId=actorId, targetId=targetId, amount=amount, skill=skill, detail=detail }); }
                catch { }
        }
    }
}

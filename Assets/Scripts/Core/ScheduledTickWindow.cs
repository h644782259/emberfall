using System;
using System.Collections.Generic;

namespace Emberfall
{
    public static class StatusTickRates
    { public const float Poison=.75f, Burn=.5f; }

    /// <summary>A claimed area event keeps its unvisited targets across a pause.
    /// The snapshot prevents a newly spawned wave from joining an older pulse.</summary>
    internal sealed class ScheduledImpactBatch<T>
    {
        private readonly List<T> targets=new List<T>();
        private int cursor, direction;
        public bool Pending { get; private set; }
        public void Begin(IList<T> current,bool reverse=false)
        {
            if(Pending)throw new InvalidOperationException("Finish the pending impact before claiming another tick");
            targets.Clear();if(current!=null)for(int i=0;i<current.Count;i++)targets.Add(current[i]);
            direction=reverse?-1:1;cursor=reverse?targets.Count-1:0;Pending=true;
        }
        public bool TryTake(out T target)
        {
            target=default(T);
            if(!Pending)return false;
            if(cursor<0||cursor>=targets.Count){Clear();return false;}
            target=targets[cursor];cursor+=direction;return true;
        }
        public void Clear(){targets.Clear();cursor=0;Pending=false;}
    }

    /// <summary>Finite combat-time tick schedule. Expiry never erases an already-due
    /// tick; callers drain at most eight events per frame, then clear on disposal.</summary>
    public sealed class ScheduledTickWindow
    {
        public const int MaximumCatchUp = 8;
        private const double Epsilon = .00001;
        private double age, end, next;
        private readonly double interval, first;
        private bool cleared;
        public float Remaining { get { return (float)Math.Max(0,end-age); } }
        public bool Complete { get { return cleared || age + Epsilon >= end && next > end + Epsilon; } }
        public ScheduledTickWindow(float lifetime,float spacing,float firstTick)
        {
            if(!Finite(lifetime)||!Finite(spacing)||!Finite(firstTick)||lifetime<=0||spacing<=0||firstTick<0)
                throw new ArgumentOutOfRangeException("Tick schedule must be finite and positive");
            end=lifetime;interval=spacing;first=firstTick;next=first;
        }
        public int Advance(float delta,bool combatActive)
        {
            if(!Elapse(delta,combatActive))return 0;
            return Collect(ref next,age,end,interval,MaximumCatchUp);
        }
        // Runtime impacts may open a choice/pause synchronously. Advance the clock
        // once, then claim each event only when it is actually going to be applied.
        public bool Elapse(float delta,bool combatActive)
        {
            if(cleared||!combatActive||!Finite(delta)||delta<=0)return false;
            age=Math.Min(end,age+delta);return true;
        }
        public bool TryTakeDueTick(bool combatActive)
        {
            return !cleared&&combatActive&&Collect(ref next,age,end,interval,1)==1;
        }
        public bool Refresh(float lifetime)
        {
            if(cleared||!Finite(lifetime)||lifetime<=0)return false;
            if(Complete){age=0;next=first;end=lifetime;}
            else end=Math.Max(end,age+lifetime);
            return true;
        }
        public bool HasDueTicks {get{return !cleared&&next<=Math.Min(age,end)+Epsilon;}}
        // Only future, unclaimed discrete events can be converted. Due events must be delivered first.
        public int ClaimFutureTicks(float horizon)
        {
            if(cleared||HasDueTicks||!Finite(horizon)||horizon<=0)return 0;
            double through=Math.Min(end,age+horizon);
            if(next>through+Epsilon)return 0;
            int count=(int)Math.Min(int.MaxValue,Math.Floor((through+Epsilon-next)/interval)+1);
            next+=count*interval;
            if(next>end+Epsilon)Clear();
            return count;
        }
        public void Clear(){cleared=true;age=end;next=end+interval;}
        public static int Collect(ref float nextTick,float age,float lifetime,float interval,int maximum=MaximumCatchUp)
        {
            if(!Finite(nextTick)||!Finite(age)||!Finite(lifetime)||!Finite(interval)||age<0||lifetime<0||interval<=0||nextTick<0)return 0;
            double next=nextTick;int count=Collect(ref next,age,lifetime,interval,Math.Max(0,Math.Min(MaximumCatchUp,maximum)));
            nextTick=(float)next;return count;
        }
        public static bool Drained(float nextTick,float lifetime)
        {return Finite(nextTick)&&Finite(lifetime)&&nextTick>lifetime+Epsilon;}
        private static int Collect(ref double next,double age,double end,double interval,int maximum)
        {
            int count=0;double through=Math.Min(age,end);
            while(count<maximum&&next<=through+Epsilon){next+=interval;count++;}
            return count;
        }
        private static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
    }
}

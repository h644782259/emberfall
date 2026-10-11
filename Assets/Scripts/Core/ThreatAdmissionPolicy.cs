using System;
using System.Collections.Generic;
namespace Emberfall
{
    // One room-local queue. Clock advances only during active simulation, never wall time.
    public sealed class ThreatAdmissionPolicy
    {
        public const int MaximumActive = 2;
        public const float MinimumGap = .35f, MaximumGap = .6f;
        private readonly List<int> waiting = new List<int>();
        private sealed class Lease {public int Remaining;}
        private readonly Dictionary<int,Lease> active = new Dictionary<int,Lease>();
        private readonly Dictionary<int,long> seen = new Dictionary<int,long>();
        private uint random;
        private float clock, nextStart;
        private long simulationStep;
        public int ActiveCount {get{return active.Count;}}
        public int WaitingCount {get{return waiting.Count;}}
        public float LastGap {get;private set;}
        public ThreatAdmissionPolicy(int seed){random=unchecked((uint)seed)^0x9e3779b9u;}
        public static bool IsPilot(bool chapterActive, int node, int difficulty, int room)
        {return chapterActive && node==1 && difficulty==1 && room==0;}
        public void Advance(float delta, bool paused=false)
        {
            if(paused){ClearWaiting();return;}
            if(float.IsNaN(delta)||float.IsInfinity(delta)||delta<=0)return;
            clock+=delta;simulationStep++;
            // One Advance per active host Update. Keep identity across a long frame;
            // retire only a member that missed a complete request pass. This works
            // whether the host Update runs before or after enemy Updates.
            for(int i=waiting.Count-1;i>=0;i--)if(simulationStep-seen[waiting[i]]>1){seen.Remove(waiting[i]);waiting.RemoveAt(i);}
        }
        public bool Request(int id,float simulationTime=float.NaN)
        {
            // The host may Update after this enemy. Use this frame's scaled Unity
            // time when supplied, so a previous long frame cannot shorten spacing.
            float now=float.IsNaN(simulationTime)?clock:simulationTime;
            if(float.IsInfinity(now))return false;
            // A projectile volley still in flight cannot overlap another attack by its owner.
            if(active.ContainsKey(id))return false;
            if(!seen.ContainsKey(id))waiting.Add(id);
            seen[id]=simulationStep;
            if(active.Count>=MaximumActive||now<nextStart||waiting[0]!=id)return false;
            waiting.RemoveAt(0);seen.Remove(id);active.Add(id,new Lease());
            random=unchecked(random*1664525u+1013904223u);
            LastGap=MinimumGap+(MaximumGap-MinimumGap)*((random>>8)/16777215f);
            nextStart=now+LastGap;return true;
        }
        public Action LaunchVolley(int id,int count)
        {
            Lease lease;if(count<=0||!active.TryGetValue(id,out lease))return null;
            lease.Remaining=count;
            return ()=>{Lease current;if(--lease.Remaining==0&&active.TryGetValue(id,out current)&&ReferenceEquals(current,lease))active.Remove(id);};
        }
        public void Finish(int id){Release(id);}
        public void Withdraw(int id){waiting.Remove(id);seen.Remove(id);}
        // Death/interrupt ends a windup; already launched danger owns its independent lease.
        public void Release(int id){Withdraw(id);Lease lease;if(active.TryGetValue(id,out lease)&&lease.Remaining==0)active.Remove(id);}
        public void ClearWaiting(){waiting.Clear();seen.Clear();}
        public void Reset(){ClearWaiting();active.Clear();nextStart=clock=0;simulationStep=0;}
    }
}

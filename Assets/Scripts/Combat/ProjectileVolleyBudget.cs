using System;using System.Collections.Generic;
namespace Emberfall
{
    /// <summary>One finite cast object shared by its arrows and their explosions.</summary>
    internal sealed class ProjectileVolleyBudget<T> where T:class
    {
        private sealed class Target { public float Used;public int Direct,Secondary; }
        private readonly Dictionary<T,Target> targets=new Dictionary<T,Target>();
        private readonly float maximum;
        internal int TrackedTargets {get{return targets.Count;}}
        public ProjectileVolleyBudget(float attack,float capCoefficient)
        { maximum=Finite(attack)&&Finite(capCoefficient)?Math.Max(0,Math.Min(100000000,attack*capCoefficient)):0; }
        public CombatDamage Apply(T target,CombatDamage raw,bool secondary)
        {
            if(target==null||maximum<=0||!Finite(raw.Amount)||raw.Amount<=0)return new CombatDamage(0,false);
            Target state;
            if(!targets.TryGetValue(target,out state)){if(targets.Count>=64)return new CombatDamage(0,false);targets.Add(target,state=new Target());}
            int previous=secondary?state.Secondary++:state.Direct++;
            float basis=raw.WithoutCritical().Amount*SkillDamageBudgets.RepeatedVolleyMultiplier(previous);
            float granted=Math.Min(Math.Max(0,maximum-state.Used),basis);state.Used+=granted;
            return new CombatDamage(granted*(raw.IsCritical?raw.CriticalMultiplier:1),raw.IsCritical,raw.CriticalMultiplier);
        }
        private static bool Finite(float v){return !float.IsNaN(v)&&!float.IsInfinity(v);}
    }
}

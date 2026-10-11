using System;
using System.Collections.Generic;

namespace Emberfall
{
    public enum DestructibleKind { Crate, Pot, Rubble }
    public enum PropRecovery { None, Energy, Health }
    public struct PropHitResult
    {
        public readonly bool Applied, Broke;
        public readonly float Damage;
        internal PropHitResult(bool applied,bool broke,float damage){Applied=applied;Broke=broke;Damage=damage;}
    }
    public sealed class DestructiblePropRules : IDisposable
    {
        public const int MaximumProps=12, RememberedCasts=64;
        public readonly float MaxHealth;
        public float Health {get;private set;}
        public bool Broken {get{return Health<=0;}}
        public bool Disposed {get;private set;}
        public bool RecoveryClaimed {get;private set;}
        private readonly Dictionary<long,float> casts=new Dictionary<long,float>();
        private long discardedThrough;
        public DestructiblePropRules(DestructibleKind kind,int level)
        {
            if(!Enum.IsDefined(typeof(DestructibleKind),kind))throw new ArgumentOutOfRangeException(nameof(kind));
            level=Math.Max(1,Math.Min(100,level));
            MaxHealth=kind==DestructibleKind.Pot?12+level*3:kind==DestructibleKind.Rubble?36+level*18:18+level*6;
            Health=MaxHealth;
        }
        public PropHitResult Hit(long cast,float damage)
        {
            if(Disposed||Broken||cast<=0||cast<=discardedThrough||damage<=0||float.IsNaN(damage)||float.IsInfinity(damage))return new PropHitResult();
            float earlier;casts.TryGetValue(cast,out earlier);
            if(damage<=earlier)return new PropHitResult();
            // Repeated pellets/ticks do not multiply damage against scenery. A later
            // stronger finisher may raise the same cast's contribution to its maximum.
            if(!casts.ContainsKey(cast)&&casts.Count>=RememberedCasts)
            {
                long oldest=long.MaxValue;foreach(long key in casts.Keys)if(key<oldest)oldest=key;
                if(cast<=oldest)return new PropHitResult();
                casts.Remove(oldest);discardedThrough=Math.Max(discardedThrough,oldest);
            }
            casts[cast]=damage;
            float applied=Math.Min(Health,damage-earlier);Health=Math.Max(0,Health-applied);
            return new PropHitResult(true,Broken,applied);
        }
        public bool TryClaimRecovery()
        {if(Disposed||!Broken||RecoveryClaimed)return false;RecoveryClaimed=true;return true;}
        public void Dispose(){Disposed=true;casts.Clear();}
    }
    public static class PropImpactGeometry
    {
        // Earliest swept-circle contact, or 2 for no hit. All math is managed and
        // finite-input checked; targets behind a wall are rejected by the host LOS.
        public static float EntryFraction(float ax,float az,float bx,float bz,float cx,float cz,float radius)
        {
            if(!Finite(ax)||!Finite(az)||!Finite(bx)||!Finite(bz)||!Finite(cx)||!Finite(cz)||!Finite(radius)||radius<=0)return 2;
            double dx=(double)bx-ax,dz=(double)bz-az,x=(double)ax-cx,z=(double)az-cz;
            double c=x*x+z*z-(double)radius*radius;if(c<=0)return 0;
            double a=dx*dx+dz*dz;if(a<1e-12)return 2;
            double b=2*(x*dx+z*dz),discriminant=b*b-4*a*c;if(discriminant<0)return 2;
            double t=(-b-Math.Sqrt(discriminant))/(2*a);
            return t>=0&&t<=1?(float)t:2;
        }
        private static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
    }
}

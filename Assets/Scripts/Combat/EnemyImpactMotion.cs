using System;
namespace Emberfall
{
    // Authoritative vertical contact height. Analytic integration is independent
    // of render rate; the grounded navigation proxy still resolves horizontal walls.
    public struct EnemyImpactMotion
    {
        public const float Gravity=9.81f;
        public float Height {get;private set;}
        public float Velocity {get;private set;}
        public bool Airborne => Height>0 || Velocity>0;
        public bool CanLift(float contactHeight) => !Airborne || Velocity<=0&&Height<=contactHeight;
        public void Launch(float apex)
        {
            if(float.IsNaN(apex)||float.IsInfinity(apex)||apex<=Height)return;
            Velocity=(float)Math.Sqrt(2*Gravity*(apex-Height));
        }
        public bool Advance(float delta)
        {
            if(!Airborne||delta<=0||float.IsNaN(delta)||float.IsInfinity(delta))return false;
            Height+=Velocity*delta-Gravity*delta*delta*.5f;Velocity-=Gravity*delta;
            if(Height>0)return false;
            Height=Velocity=0;return true;
        }
        public void PressDown(){Velocity=Math.Min(Velocity,-2f);}
        public static float KnockbackTravel(float velocity,float delta)
        {return delta<=0?0:velocity*(1-(float)Math.Exp(-5*delta))/5;}
        public static float KnockbackDecay(float delta) => (float)Math.Exp(-5*Math.Max(0,delta));
        public const float FrostContactHeight=.7f;
    }
}

using System;
namespace Emberfall
{
    public struct LargeBossPose
    {
        public float Opening,Height,Pitch,CoreScale,Brace;
        public LargeBossPose(float opening,float height,float pitch,float core,float brace)
        {Opening=opening;Height=height;Pitch=pitch;CoreScale=core;Brace=brace;}
    }
    // Decorative angular momentum only: encounter phase and beam direction stay authoritative.
    public sealed class LargeBossMotion
    {
        public float FirstAngle {get;private set;}
        public float SecondAngle {get;private set;}
        public float FirstVelocity {get;private set;}=22;
        public float SecondVelocity {get;private set;}=-17;
        public void Advance(float delta,LargeBossPhase phase)
        {
            if(float.IsNaN(delta)||float.IsInfinity(delta)||delta<=0||phase==LargeBossPhase.Finished)return;
            float dt=Math.Min(delta,.1f),first=22,second=-17;
            switch(phase){case LargeBossPhase.Windup:first=55;second=-38;break;case LargeBossPhase.Beam:first=38;second=-26;break;
                case LargeBossPhase.Exposed:first=7;second=-5;break;case LargeBossPhase.Recovery:first=12;second=-9;break;}
            float v=FirstVelocity;FirstAngle=Step(FirstAngle,ref v,first,dt);FirstVelocity=v;
            v=SecondVelocity;SecondAngle=Step(SecondAngle,ref v,second,dt);SecondVelocity=v;
        }
        private static float Step(float angle,ref float velocity,float target,float dt)
        {
            const float response=5;float decay=(float)Math.Exp(-response*dt);
            // Analytic integral of exponential velocity easing: no age * newly selected speed.
            angle+=target*dt+(velocity-target)*(1-decay)/response;
            velocity=target+(velocity-target)*decay;return (angle%360+360)%360;
        }
        public static LargeBossPose Pose(LargeBossPhase phase,float remaining)
        {
            if(float.IsNaN(remaining)||float.IsInfinity(remaining))remaining=0;
            float progress=1-Math.Max(0,Math.Min(1,remaining/LargeBossPhaseState.WindupSeconds));
            switch(phase)
            {
                case LargeBossPhase.Windup:return new LargeBossPose(.16f,1.45f-.24f*progress,-10*progress,.9f+.15f*progress,.9f);
                case LargeBossPhase.Beam:return new LargeBossPose(.42f,1.18f,-6,1.12f,1);
                case LargeBossPhase.Exposed:return new LargeBossPose(1,1.32f,8,1.32f,.5f);
                case LargeBossPhase.Recovery:
                    float t=1-Math.Max(0,Math.Min(1,remaining/2));
                    return new LargeBossPose(.45f*(1-t),1.18f+.27f*t,-4*(1-t),1.12f-.12f*t,1-t);
                default:return new LargeBossPose(0,1.45f,0,1,0);
            }
        }
    }
}

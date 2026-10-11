using System;
namespace Emberfall
{
    // Only accepted walking displacement enters this state, never whole-frame root motion.
    public sealed class LocomotionPoseState
    {
        public float Phase { get; private set; }
        public float Speed { get; private set; }
        public float Forward { get; private set; }
        public float Side { get; private set; }
        public float CloakPitch { get; private set; }
        public float CloakSide { get; private set; }
        public float JumpTuck { get; private set; }
        public float Landing { get; private set; }
        public bool Airborne { get; private set; }
        public float JumpProgress { get; private set; }
        private bool wasAirborne;
        private float previousTarget;
        public void Reset() { Phase=Speed=Forward=Side=CloakPitch=CloakSide=JumpTuck=Landing=previousTarget=0;wasAirborne=Airborne=false;JumpProgress=0; }
        private static bool Finite(float value) { return !float.IsNaN(value)&&!float.IsInfinity(value); }
        private static float Clamp(float value,float min,float max) { return Math.Max(min,Math.Min(max,value)); }
        public void Advance(float localX,float localZ,float delta,float referenceSpeed,bool walking,bool airborne,float jumpProgress)
        {
            if(!Finite(delta)||delta<=0||!Finite(localX)||!Finite(localZ)||!Finite(referenceSpeed)||referenceSpeed<=0)return;
            if(!walking||airborne)localX=localZ=0;
            float distance=(float)Math.Sqrt((double)localX*localX+(double)localZ*localZ);
            // At most a normal-speed frame is accepted; discontinuities are excluded by the adapter too.
            distance=Math.Min(distance,referenceSpeed*delta*2);
            Phase=(Phase+distance*2.6f)%(float)(Math.PI*2);
            float target=Clamp(distance/(referenceSpeed*delta),0,1);
            float blend=1-(float)Math.Exp(-delta*12);
            float side=Clamp(localX/(referenceSpeed*delta),-1,1),forward=Clamp(localZ/(referenceSpeed*delta),-1,1);
            Speed+=(target-Speed)*blend;Forward+=(forward-Forward)*blend;Side+=(side-Side)*blend;
            float acceleration=Clamp((target-previousTarget)/delta,-4,4);previousTarget=target;
            float inertia=1-(float)Math.Exp(-delta*6);
            CloakPitch+=(Clamp(target*10+acceleration*3,-14,22)-CloakPitch)*inertia;
            CloakSide+=(-side*12-CloakSide)*inertia;
            Airborne=airborne;JumpProgress=Clamp(Finite(jumpProgress)?jumpProgress:0,0,1);
            JumpTuck=airborne?(float)Math.Sin(Math.PI*JumpProgress):0;
            Landing=wasAirborne&&!airborne?1:Math.Max(0,Landing-delta*5);
            wasAirborne=airborne;
        }
    }
    public enum EnemyPosePhase { Idle, Windup, Recovery, ActiveCharge }
    public static class EnemyActionPose
    {
        public static EnemyPosePhase Select(bool preparing,bool activeCharge,float recovery)
        {return activeCharge?EnemyPosePhase.ActiveCharge:preparing?EnemyPosePhase.Windup:recovery>0?EnemyPosePhase.Recovery:EnemyPosePhase.Idle;}
        public static float Charge(EnemyPosePhase phase){return phase==EnemyPosePhase.ActiveCharge?1:0;}
        public static float Windup(EnemyPosePhase phase,float progress)
        { float t=Math.Max(0,Math.Min(1,progress));return phase==EnemyPosePhase.Windup?t*t*(3-2*t):0; }
        public static float Contact(EnemyPosePhase phase,float progress)
        { float t=Math.Max(0,Math.Min(1,progress));return phase==EnemyPosePhase.Recovery?(1-t)*(1-t):0; }
    }
}

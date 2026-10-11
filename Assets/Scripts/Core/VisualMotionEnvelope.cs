using System;
namespace Emberfall
{
    // Cosmetic state only: never queried by attack, collision or traversal code.
    public sealed class VisualMotionEnvelope
    {
        public float Turn { get; private set; }
        public float SideLoad { get; private set; }
        public float ForwardDrag { get; private set; }
        public static float RecoveryWeight(float age)
        {float t=Math.Max(0,Math.Min(1,age/.12f));return 1-t*t*(3-2*t);}
        public void Advance(float yawDelta,float side,float forward,float delta)
        {
            if(delta<=0||float.IsNaN(delta)||float.IsInfinity(delta))return;
            delta=Math.Min(delta,.1f);
            if(float.IsNaN(yawDelta)||float.IsInfinity(yawDelta))yawDelta=0;
            // Teleports/orientation resets are not impulses.
            float turn=Math.Abs(yawDelta)>100?0:Math.Max(-1,Math.Min(1,yawDelta/delta/240));
            float blend=1-(float)Math.Exp(-delta*9);
            Turn+=(turn-Turn)*blend;
            SideLoad+=(Math.Max(-1,Math.Min(1,side))-SideLoad)*blend;
            ForwardDrag+=(Math.Max(-1,Math.Min(1,forward))-ForwardDrag)*blend;
        }
        public void Reset(){Turn=SideLoad=ForwardDrag=0;}
    }
}

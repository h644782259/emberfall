using UnityEngine;

namespace Emberfall
{
    // A presentation impulse only. Navigation, damage, control timers and attack
    // clocks remain controller-owned; the solver has no per-frame allocations.
    internal static class ActorImpactSpring
    {
        internal static bool Finite(Vector3 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
                !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
                !float.IsNaN(value.z) && !float.IsInfinity(value.z);
        }

        internal static void Impulse(ref Vector3 velocity, Vector3 direction, float strength)
        {
            velocity = Vector3.ClampMagnitude(velocity + direction * strength * 3.2f, 4.5f);
        }

        internal static void Advance(ref Vector3 offset, ref Vector3 velocity, float delta)
        {
            if (delta <= 0 || float.IsNaN(delta) || float.IsInfinity(delta)) return;
            const float damping = 7.5f, stiffness = 95f, frequency = 6.2249498f;
            float decay = Mathf.Exp(-damping * delta), sine = Mathf.Sin(frequency * delta), cosine = Mathf.Cos(frequency * delta);
            Vector3 original = offset;
            offset = (original * cosine + (velocity + original * damping) * (sine / frequency)) * decay;
            velocity = (velocity * cosine - (velocity * damping + original * stiffness) * (sine / frequency)) * decay;
            // Even layered repeated impacts cannot detach the render body from its collider.
            offset = Vector3.ClampMagnitude(offset, .22f);
            if (offset.sqrMagnitude < .00000001f && velocity.sqrMagnitude < .00000001f)
                offset = velocity = Vector3.zero;
        }
    }
}

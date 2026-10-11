using System;
using System.Collections.Generic;

namespace Emberfall
{
    // Engine-independent gates shared by the actual combat implementations.
    public sealed class RecentCastGate
    {
        private readonly HashSet<int> seen = new HashSet<int>();
        private readonly Queue<int> order = new Queue<int>();
        public bool TryEnter(int castId)
        {
            if (castId <= 0 || !seen.Add(castId)) return false;
            order.Enqueue(castId);
            if (order.Count > 32) seen.Remove(order.Dequeue());
            return true;
        }
        public bool TryEnterEligible(int castId, bool eligible) { return eligible && TryEnter(castId); }
        public void Clear() { seen.Clear(); order.Clear(); }
    }

    public sealed class CombatProcCooldown
    {
        public float Remaining { get; private set; }
        public bool TryTrigger(float cooldown)
        {
            if (Remaining > 0 || !Valid(cooldown) || cooldown <= 0) return false;
            Remaining = cooldown;
            return true;
        }
        public void Advance(float delta)
        {
            if (Valid(delta) && delta > 0) Remaining = Math.Max(0, Remaining - delta);
        }
        private static bool Valid(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    }

    public sealed class OpeningFrostCounter
    {
        private int target, hits;
        public void Clear() { target = hits = 0; }
        public bool RecordHit(int targetId, bool livingTarget, bool novaLearned)
        {
            if (targetId == 0 || !livingTarget) { Clear(); return false; }
            if (target != targetId) { target = targetId; hits = 0; }
            if (++hits < 3) return false;
            hits = 0;
            return true;
        }
    }

    public static class PlayerUpgradeRules
    {
        public const float DodgeBufferWindow = .16f;
        public const float PerfectDodgeEnergy = 12f;
        public const float CounterWindow = 2f;
        public const float FocusDuration = 1.8f;
        public const float FocusRange = 14f;
        public const float BasicPoisonCoefficient=.14f, BasicPoisonDuration=4f, PoisonDetonationTicks=3f;
        public const float BasicFrostMarkDuration=3f, BasicFrostProcCooldown=1.2f;
        public static float NovaFreezeDuration(int rank){return 1.5f+Math.Max(1,Math.Min(3,rank))*.25f;}

        public static float MeteorDirectMultiplier(ElementalistSpecialization specialization, bool cinderTrail)
        {
            return (specialization == ElementalistSpecialization.Shatter ? .85f : specialization == ElementalistSpecialization.Burn ? .8f : 1f) * (cinderTrail ? BuildCatalog.CinderDirectMultiplier : 1f);
        }
        public static float ShatterMultiplier(ElementalistSpecialization specialization)
        { return specialization == ElementalistSpecialization.Shatter ? 1f : specialization == ElementalistSpecialization.Burn ? 0f : .5f; }
        public static float FindSafeBlinkDistance(float distance, Func<float, bool> pathClear, Func<float, bool> canLand)
        {
            if (float.IsNaN(distance) || float.IsInfinity(distance) || distance <= 0 || pathClear == null || canLand == null || !pathClear(0) || !canLand(0)) return 0;
            int samples = Math.Max(1, (int)Math.Ceiling(distance / .06f));
            float previous = 0, landing = 0;
            for (int i = 1; i <= samples; i++)
            {
                float candidate = distance * i / samples;
                if (!pathClear(candidate))
                {
                    float clear = previous, blocked = candidate;
                    for (int step = 0; step < 8; step++)
                    {
                        float probe = (clear + blocked) * .5f;
                        if (pathClear(probe)) clear = probe; else blocked = probe;
                    }
                    if (canLand(clear)) landing = clear;
                    break;
                }
                if (canLand(candidate)) landing = candidate;
                previous = candidate;
            }
            return landing >= .35f ? landing : 0;
        }

        public static float CounterAfterAttack(float previous, float newlyGranted, bool hit)
        { return hit ? Math.Max(0, newlyGranted) : Math.Max(previous, newlyGranted); }

        public static bool IsInsideArea(float dx, float dz, float radius, bool unobstructed)
        {
            return unobstructed && !float.IsNaN(dx) && !float.IsNaN(dz) && !float.IsInfinity(dx) && !float.IsInfinity(dz)
                && !float.IsNaN(radius) && !float.IsInfinity(radius) && radius > 0 && dx*dx+dz*dz < radius*radius;
        }

        public static bool CanBufferDodge(float cooldown, float movementLock, float landingTime)
        {
            return cooldown >= 0 && movementLock >= 0 && landingTime >= 0 &&
                cooldown <= DodgeBufferWindow && movementLock <= DodgeBufferWindow && landingTime <= DodgeBufferWindow;
        }
    }
}

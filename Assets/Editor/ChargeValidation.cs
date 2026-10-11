using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Emberfall.Editor
{
    public static class ChargeValidation
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        // Run inside an isolated, cleared, fully learned class fixture. The caller
        // permits intentional pause for this enumerator. Null means another frame.
        public static IEnumerator Validate(GameSession game, Action<bool, string> check, Action<string> log)
        {
            typeof(InventoryUIValidation).GetMethod("RequireIsolatedRuntime", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { game });
            PlayerController player = game.Player;
            SkillRuntime runtime = Read<SkillRuntime>(player, "skillRuntime");
            SkillChargeController charge = player.GetComponent<SkillChargeController>();
            SkillTargetingController targeting = player.GetComponent<SkillTargetingController>();
            HeroClass hero = player.HeroClass;
            check(charge != null, hero + " charge component is attached to the real player");
            int chargeCount = 0;
            foreach (HeroClass type in (HeroClass[])Enum.GetValues(typeof(HeroClass)))
                for (int skill = 0; skill < GameBalance.SkillCount; skill++)
                    if (SkillChargeController.Duration(type, skill) > 0) chargeCount++;
            check(chargeCount == 7, "Only seven of the thirty-two active class skills require charging");

            charge.Cancel(); targeting.Cancel(); runtime.Advance(200); runtime.FillEnergy();
            int nextFrame = Time.frameCount;
            while (Time.frameCount == nextFrame) yield return null;
            Vector3 chosen = player.transform.position + new Vector3(3, 0, 2);
            Begin(targeting, 9, chosen, check);
            check(charge.IsCharging && charge.SkillIndex == 9 && charge.Progress == 0, hero + " confirmed ultimate starts its charge at zero progress");
            check(player.Energy == 100 && player.SkillCooldownRemaining(9) == 0, hero + " starting a charge spends no energy and starts no cooldown");
            Component chargeEffect = Read<Component>(charge, "chargeEffect");
            check(chargeEffect != null && chargeEffect.gameObject.activeInHierarchy, hero + " pending charge creates visible owned gathering energy");
            CombatModel model = player.GetComponentInChildren<CombatModel>();
            model.Animate(0, 0, false);
            Transform rightArm = Read<Transform>(model, "rightArm");
            Quaternion restingArm = rightArm.localRotation;
            model.AnimateCharge(.75f);
            check(Quaternion.Angle(restingArm, rightArm.localRotation) > 10, hero + " charge visibly raises the weapon or draws the bow");
            check(!targeting.Begin(0) && !charge.Begin(9), hero + " a pending charge rejects other skills and duplicate starts");
            check(Mathf.Abs(player.MovementMultiplier - .4f) < .001f, hero + " charge applies the actual player movement multiplier");
            float attackBefore = Read<float>(player, "attackCooldown");
            Invoke(player, "BasicAttack");
            check(Read<float>(player, "attackCooldown") == attackBefore, hero + " ordinary attacks cannot commit during a charge");
            Invoke(charge, "Advance", SkillChargeController.Duration(hero, 9) * .5f);
            check(Mathf.Abs(charge.Progress - .5f) < .001f && player.Energy == 100, hero + " half-complete charge is still uncommitted");

            game.SetPaused(true);
            try
            {
                double until = EditorApplication.timeSinceStartup + .15;
                while (EditorApplication.timeSinceStartup < until) yield return null;
                Invoke(charge, "Advance", 10f);
                check(charge.IsCharging && Mathf.Abs(charge.Progress - .5f) < .001f && player.Energy == 100,
                    hero + " real paused frames and positive manual deltas cannot advance a paused charge");
            }
            finally { game.SetPaused(false); }
            charge.Cancel();
            check(chargeEffect == null || !chargeEffect.gameObject.activeInHierarchy, hero + " cancelling immediately hides owned charge geometry");
            check(!charge.IsCharging && charge.TargetEnemy == null && charge.CancelledThisFrame && player.Energy == 100 && player.SkillCooldownRemaining(9) == 0,
                hero + " cancellation consumes the input frame without charging its budget");
            check(!targeting.Begin(0), hero + " cancel input cannot accidentally become a same-frame ordinary skill");

            nextFrame = Time.frameCount;
            while (Time.frameCount == nextFrame) yield return null;
            Begin(targeting, 9, chosen, check);
            double deadline = EditorApplication.timeSinceStartup + 3;
            while (charge.Progress <= 0 && EditorApplication.timeSinceStartup < deadline) yield return null;
            check(charge.IsCharging && charge.Progress > 0 && player.Energy == 100, hero + " native player-loop frames advance a live charge before its resource commit");
            charge.Cancel();

            nextFrame = Time.frameCount;
            while (Time.frameCount == nextFrame) yield return null;
            runtime.Advance(200); runtime.FillEnergy();
            Begin(targeting, 9, chosen, check);
            Vector3 locked = charge.TargetPoint;
            Vector3 direction = player.transform.forward;
            Set(player, "aimPoint", player.transform.position - Vector3.forward * 8);
            player.transform.rotation = Quaternion.Euler(0, 180, 0);
            player.transform.position += Vector3.right * 2;
            Invoke(charge, "Advance", SkillChargeController.Duration(hero, 9));
            float energy = 100 - GameBalance.SkillEnergyCost(hero, 9);
            float cooldown = GameBalance.EffectiveCooldown(hero, 9, game.Progression.Profile.skillRanks[9]);
            check(!charge.IsCharging && charge.TargetEnemy == null && Mathf.Abs(player.Energy - energy) < .001f && Mathf.Abs(player.SkillCooldownRemaining(9) - cooldown) < .001f,
                hero + " exact completion commits the real cast budget once");
            check(Vector3.Distance(player.AimPoint, locked) < .001f && Vector3.Angle(player.transform.forward, direction) < .01f,
                hero + " release retains its original point and direction after movement and aim changes");
            Invoke(charge, "Advance", 10f);
            check(!targeting.Begin(0) && Mathf.Abs(player.Energy - energy) < .001f && Mathf.Abs(player.SkillCooldownRemaining(9) - cooldown) < .001f,
                hero + " duplicate completion and same-frame input cannot spend twice");

            nextFrame = Time.frameCount;
            while (Time.frameCount == nextFrame) yield return null;
            runtime.Advance(200); runtime.FillEnergy();
            Begin(targeting, 9, chosen, check);
            check(runtime.TryConsume(7, 3), hero + " resource revalidation fixture changes the shared budget during pending cast");
            energy = player.Energy;
            Invoke(charge, "Advance", 10f);
            check(!charge.IsCharging && charge.TargetEnemy == null && Mathf.Abs(player.Energy - energy) < .001f && player.SkillCooldownRemaining(9) == 0,
                hero + " insufficient energy at completion safely discards the pending cast");

            nextFrame = Time.frameCount;
            while (Time.frameCount == nextFrame) yield return null;
            runtime.Advance(200); runtime.FillEnergy();
            check(runtime.TryConsume(0, 3), hero + " teleport preservation fixture has an existing independent cooldown");
            energy = player.Energy;
            float originalCooldown = player.SkillCooldownRemaining(0);
            Begin(targeting, 9, chosen, check);
            player.Teleport(player.transform.position + Vector3.left);
            Invoke(charge, "Advance", 10f);
            check(!charge.IsCharging && charge.TargetEnemy == null && Mathf.Abs(player.Energy - energy) < .001f && player.SkillCooldownRemaining(9) == 0 && Mathf.Abs(player.SkillCooldownRemaining(0) - originalCooldown) < .001f,
                hero + " teleport cancels only the pending charge and preserves committed resources");

            nextFrame = Time.frameCount;
            while (Time.frameCount == nextFrame) yield return null;
            runtime.Advance(200); runtime.FillEnergy();
            Begin(targeting, 9, chosen, check);
            float originalHealth = player.Health;
            try
            {
                typeof(PlayerController).GetProperty("Health").SetValue(player, 0f, null);
                Invoke(charge, "Advance", 10f);
                check(!charge.IsCharging && charge.TargetEnemy == null && player.Energy == 100 && player.SkillCooldownRemaining(9) == 0, hero + " dead-owner validation prevents a pending cast from releasing");
            }
            finally { typeof(PlayerController).GetProperty("Health").SetValue(player, originalHealth, null); }
            runtime.Advance(200); runtime.FillEnergy();
            log("CHARGE " + hero + " passed real entry, frame progression, pause, fixed aim, cancellation, exact commit, energy revalidation and invalid-owner checks.");
        }

        private static void Begin(SkillTargetingController targeting, int skill, Vector3 chosen, Action<bool, string> check)
        {
            check(targeting.Begin(skill) && targeting.IsTargeting, "Charge fixture enters real ground preview");
            targeting.SetTarget(chosen);
            check(targeting.Confirm() && !targeting.IsTargeting, "Ground confirmation routes into the charge controller");
        }
        private static T Read<T>(object target, string name) { return (T)target.GetType().GetField(name, Private).GetValue(target); }
        private static void Set(object target, string name, object value) { target.GetType().GetField(name, Private).SetValue(target, value); }
        private static object Invoke(object target, string name, params object[] values) { return target.GetType().GetMethod(name, Private).Invoke(target, values); }
    }
}

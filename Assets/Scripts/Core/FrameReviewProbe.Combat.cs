using UnityEngine;

namespace Emberfall
{
    internal sealed partial class FrameReviewProbe
    {
        // Explicit device review only. Practice owns a temporary progression copy;
        // normal launch never drives input or changes the player's adventure.
        private bool combatReview, combatInputOwned, savedInputSimulation;
        private CampPracticeRecord observedPractice;
        private int combatRounds, combatCasts, nextCombatSkill;
        private double combatSeconds, nextCombatAction;
        private float combatDamage, combatDamageTaken;

        private void TickCombatReview(GameSession session)
        {
            if (!combatReview || session == null || !session.HasStarted || session.SceneLoading) return;
            if (!combatInputOwned)
            {
                savedInputSimulation = MobileControls.SimulationEnabled;
                MobileControls.SimulationEnabled = true;
                combatInputOwned = true;
            }
            var record = session.PracticeRecord;
            if (observedPractice != null && observedPractice.Finished)
            {
                combatRounds++;
                combatSeconds += observedPractice.Elapsed;
                combatDamage += observedPractice.ActualDamage;
                combatDamageTaken += observedPractice.DamageTaken;
                foreach (var count in observedPractice.SkillCasts.Values) combatCasts += count;
                observedPractice = null;
            }
            if (!session.PracticeActive)
            {
                MobileControls.ResetInput();
                if (Time.realtimeSinceStartupAsDouble < nextCombatAction || session.InputBlocked) return;
                nextCombatAction = Time.realtimeSinceStartupAsDouble + 1;
                if (session.BeginPractice(CampPracticeScenario.GuardAndWispPressure, 60))
                {
                    observedPractice = session.PracticeRecord;
                    session.StartPractice();
                    // Let enemy AI engage before the high-level hero attacks.
                    nextCombatAction = Time.realtimeSinceStartupAsDouble + 2;
                }
                return;
            }
            if (session.InputBlocked || session.Player == null || record == null || record.Finished) return;
            var controls = session.GetComponent<MobileControls>();
            if (record.Elapsed >= 4 && controls != null && !MobileControls.AttackHeld)
                controls.ProcessPointer(-812, TouchPhase.Began, controls.ControlScreenPoint("attack"));
            if (Time.realtimeSinceStartupAsDouble < nextCombatAction) return;
            nextCombatAction = Time.realtimeSinceStartupAsDouble + .25;
            // Begin enforces learned skills, energy, cooldown, and mobile aiming.
            var targeting = session.Player.GetComponent<SkillTargetingController>();
            if (targeting != null) targeting.Begin(nextCombatSkill++ % GameBalance.SkillCount);
        }

        private void WriteCombatReview(Report report)
        {
            if (!combatReview) return;
            report.combatReview = true;
            report.combatRounds = combatRounds;
            report.combatActiveSeconds = combatSeconds;
            report.combatDamage = combatDamage;
            report.combatDamageTaken = combatDamageTaken;
            report.combatCasts = combatCasts;
            if (observedPractice == null) return;
            report.combatActiveSeconds += observedPractice.Elapsed;
            report.combatDamage += observedPractice.ActualDamage;
            report.combatDamageTaken += observedPractice.DamageTaken;
            foreach (var count in observedPractice.SkillCasts.Values) report.combatCasts += count;
        }

        private void DisposeCombatReview()
        {
            if (!combatInputOwned) return;
            MobileControls.ResetInput();
            var session = GameSession.Instance;
            if (session != null && session.PracticeActive) session.EndPractice("性能验证结束");
            MobileControls.SimulationEnabled = savedInputSimulation;
            combatInputOwned = false;
        }
    }
}

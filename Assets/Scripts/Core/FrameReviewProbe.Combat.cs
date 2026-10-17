using UnityEngine;

namespace Emberfall
{
    internal sealed partial class FrameReviewProbe
    {
        // Explicit device review only. Practice owns a temporary progression copy;
        // normal launch never drives input or changes the player's adventure.
        private bool combatReview, combatInputOwned, savedInputSimulation;
        private bool combatCrowdReview, preparingCombatCrowd;
        private int combatPeakEnemies;
        private double combatCrowdedSeconds, combatEnemySeconds;
        private CampPracticeRecord observedPractice;
        private int combatRounds, combatCasts, nextCombatSkill;
        private double combatSeconds, nextCombatAction;
        private float combatDamage, combatDamageTaken;

        private void TickCombatReview(GameSession session)
        {
            if (!combatReview || session == null || !session.HasStarted || session.SceneLoading) return;
            if (preparingCombatCrowd) return;
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
                    if (combatCrowdReview) StartCoroutine(PrepareCombatCrowd(session));
                    else session.StartPractice();
                    // Let enemy AI engage before the high-level hero attacks.
                    nextCombatAction = Time.realtimeSinceStartupAsDouble + 2;
                }
                return;
            }
            if (session.InputBlocked || session.Player == null || record == null || record.Finished) return;
            int living = 0;
            foreach (var enemy in session.Enemies) if (enemy != null && !enemy.IsDead) living++;
            combatPeakEnemies = Mathf.Max(combatPeakEnemies, living);
            combatEnemySeconds += living * Time.deltaTime;
            if (living >= 12) combatCrowdedSeconds += Time.deltaTime;
            var controls = session.GetComponent<MobileControls>();
            if (record.Elapsed >= 4 && controls != null && !MobileControls.AttackHeld)
                controls.ProcessPointer(-812, TouchPhase.Began, controls.ControlScreenPoint("attack"));
            if (Time.realtimeSinceStartupAsDouble < nextCombatAction) return;
            nextCombatAction = Time.realtimeSinceStartupAsDouble + .25;
            // Begin enforces learned skills, energy, cooldown, and mobile aiming.
            var targeting = session.Player.GetComponent<SkillTargetingController>();
            if (targeting != null) targeting.Begin(nextCombatSkill++ % GameBalance.SkillCount);
        }

        private System.Collections.IEnumerator PrepareCombatCrowd(GameSession session)
        {
            preparingCombatCrowd = true;
            try
            {
                var prepare = session.PreparePerformanceReviewCrowd();
                while (prepare.MoveNext()) yield return prepare.Current;
                if (!session.PracticeActive || session.Enemies.Count != 24 || !session.StartPractice())
                {
                    session.EndPractice("压力验证未能准备完整怪群");
                    // A partial wave must not silently become an easier benchmark.
                    autoContinue = false; combatReview = false;
                    Debug.LogError("Crowd performance review aborted: expected24 real enemies");
                    yield break;
                }
                nextCombatAction = Time.realtimeSinceStartupAsDouble + 2;
            }
            finally { preparingCombatCrowd = false; }
        }

        private void WriteCombatReview(Report report)
        {
            if (!combatReview) return;
            report.combatReview = true;
            report.combatCrowdReview = combatCrowdReview;
            report.combatPeakEnemies = combatPeakEnemies;
            report.combatCrowdedSeconds = combatCrowdedSeconds;
            report.combatEnemySeconds = combatEnemySeconds;
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

    public sealed partial class GameSession
    {
        // Only a temporary practice owner under the exact explicit review tag can
        // create this wave. Ordinary practice and saved adventures are unchanged.
        internal System.Collections.IEnumerator PreparePerformanceReviewCrowd()
        {
            if (System.Environment.GetEnvironmentVariable("EMBERFALL_PERF_REVIEW") != "performance-soak-combat-crowd" ||
                !PracticeActive || Progression == null || !Progression.IsPracticeOnly ||
                PracticeRecord == null || PracticeRecord.Started || !PracticeRecord.UsesEnemyAI) yield break;
            for (int index = Enemies.Count; index < 24; index++)
            {
                if (!PracticeActive || PracticeRecord.Started) yield break;
                var kind = (EnemyKind)(index % 4);
                float angle = index * 2.39996f;
                var preferred = new Vector3(Mathf.Sin(angle) * (4 + index * .16f), 0, -4 + Mathf.Cos(angle) * (4 + index * .16f));
                if (!TrySafeSpawn(preferred, BossScaleRules.NavigationRadius(false, false, kind), 3, out var position, spacing: .2f)) yield break;
                SpawnEnemy(kind, Progression.Profile.level, position, false);
                Enemies[Enemies.Count - 1].ConfigurePracticeTarget();
                // Avoid a synchronous creation spike masquerading as combat cost.
                yield return null;
            }
        }
    }
}

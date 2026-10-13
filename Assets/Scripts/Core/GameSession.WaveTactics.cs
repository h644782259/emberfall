using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameSession
    {
        public bool UsesWaveTactics => InDungeon && !ChapterActive && ModeRun == null && RoomChainRun == null;
        public DungeonTactic CurrentWaveTactic => DungeonWaveTactics.Select(runSeed, DungeonLayout, DungeonWave);
        private EnemyAttackTelegraph waveFault;
        private GameObject waveFaultAnchor;
        private float nextWaveFault, waveFaultAt;
        private Vector3 waveFaultPosition;
        private int announcedTacticWave = -1;

        private void ResetWaveTerrain()
        {
            if (waveFault != null) Destroy(waveFault.gameObject);
            if (waveFaultAnchor != null) Destroy(waveFaultAnchor);
            waveFault = null; waveFaultAnchor = null;
            nextWaveFault = Time.time + 6; announcedTacticWave = -1;
        }
        private void TickWaveTerrain()
        {
            if (!HasStarted || !UsesWaveTactics || DungeonCleared || IsDead || changingZone)
            { if (waveFaultAnchor != null) ResetWaveTerrain(); return; }
            if (SceneLoading || InputBlocked || Paused || Player == null) return;
            if (announcedTacticWave != DungeonWave)
            {
                announcedTacticWave = DungeonWave;
                Notify(DungeonWaveTactics.Name(CurrentWaveTactic) + " · " + DungeonWaveTactics.Hint(CurrentWaveTactic));
            }
            if (CurrentWaveTactic != DungeonTactic.Upheaval || Enemies.Count == 0)
            {
                if(waveFault!=null)Destroy(waveFault.gameObject);
                if(waveFaultAnchor!=null)Destroy(waveFaultAnchor);
                waveFault=null;waveFaultAnchor=null;nextWaveFault=Time.time+6;return;
            }
            if (waveFaultAnchor == null && Time.time >= nextWaveFault)
            {
                waveFaultPosition = WorldTraversal.NearestWalkable(Player.transform.position, .5f);
                waveFaultAnchor = new GameObject("Dungeon fault origin");
                waveFaultAnchor.transform.position = waveFaultPosition;
                waveFault = EnemyAttackTelegraph.Circle(waveFaultPosition, 3.5f, waveFaultAnchor.transform);
                waveFault.SetControlWarning(true);
                waveFaultAt = Time.time + 1.8f;
                return;
            }
            if (waveFault == null) return;
            waveFault.SetProgress(1 - (waveFaultAt - Time.time) / 1.8f);
            if (Time.time < waveFaultAt) return;
            // One warned footprint owns both visuals and damage; cover protects both sides.
            if (EnemyImpactRegion.Contains(waveFaultPosition, waveFaultPosition, Player.transform.position, 3.5f))
                Player.TakeDamageFrom(CombatBalance.EnemyDamage(DungeonEntryLevel, 1, false), "遗迹地脉爆发",waveFaultPosition,PlayerHitReaction.Launch);
            var targets = Enemies.ToArray(); // Bounded to the live encounter, only once per eruption.
            foreach (var enemy in targets)
                if (enemy != null && !enemy.IsDead && EnemyImpactRegion.Contains(waveFaultPosition, waveFaultPosition, enemy.transform.position, 3.5f))
                    enemy.TakeDamage(enemy.MaxHealth * (enemy.IsBoss ? .02f : .18f), Vector3.up, impact:false);
            PhysicalSkillVfx.Impact(waveFaultPosition, Vector3.up, new Color(.75f,.40f,.16f), 1.3f);
            Destroy(waveFault.gameObject); Destroy(waveFaultAnchor);
            waveFault = null; waveFaultAnchor = null; nextWaveFault = Time.time + 7;
        }
        internal float WaveSupportMultiplier(EnemyController target)
        {
            if (!UsesWaveTactics || target.DungeonRole == DungeonEnemyRole.Protector || target.IsBoss) return 1;
            foreach (var enemy in Enemies)
                if (enemy != null && !enemy.IsDead && !enemy.IsStunned && enemy.DungeonRole == DungeonEnemyRole.Protector &&
                    (enemy.transform.position - target.transform.position).sqrMagnitude <= 36 && WorldTraversal.HasLineOfSight(enemy.transform.position, target.transform.position)) return .7f;
            return 1;
        }
    }
}

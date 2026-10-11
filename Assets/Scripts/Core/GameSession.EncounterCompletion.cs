namespace Emberfall
{
    public sealed partial class GameSession
    {
        // A phase/encounter completion is not an enemy kill. Remove admission
        // first so deactivation callbacks cannot create loot or experience.
        private void RetireRemainingEncounterEnemies()
        {
            var remaining = Enemies.ToArray();
            Enemies.Clear();
            foreach (var enemy in remaining)
            {
                if (enemy == null) continue;
                enemy.gameObject.SetActive(false);
                Destroy(enemy.gameObject);
            }
            CombatProjectile.RetireHostileForEncounter(this);
        }
    }
}

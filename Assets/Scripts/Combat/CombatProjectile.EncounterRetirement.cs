namespace Emberfall
{
    internal sealed partial class CombatProjectile
    {
        internal static void RetireHostileForEncounter(GameSession owner)
        {
            if (owner == null) return;
            // OnDisable releases callbacks synchronously; work on a snapshot so
            // callbacks that end another volley cannot invalidate enumeration.
            var remaining = hostileProjectiles.ToArray();
            foreach (var projectile in remaining)
            {
                if (projectile == null || projectile.session != owner || !projectile.hostile) continue;
                projectile.gameObject.SetActive(false);
                Destroy(projectile.gameObject);
            }
        }
    }
}

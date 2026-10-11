"""Wiring checks only; managed transform behavior is executed by FilledVfxAllocationTests.py."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
effects=(root/'Assets/Scripts/Combat/CombatEffects.cs').read_text()
links=(root/'Assets/Scripts/Combat/WeaponVisualLinks.cs').read_text()
shot=effects[effects.index('public static void BasicShot'):effects.index('public static void Hostile')]
assert shot.index('CanLaunchFromMuzzle(')<shot.index('projectile.transform.position=muzzle;')<shot.index('projectile.AlignBodyFlight();')<shot.index('projectile.BindVisualOrigin(player);')
make=effects[effects.index('private static CombatProjectile Make'):effects.index('        private void Update()',effects.index('private static CombatProjectile Make'))]
assert 'GameObject obj = new GameObject("Projectile simulation root")' in make
assert 'body.transform.SetParent(obj.transform,false)' in make
assert 'body.AddComponent<TrailRenderer>()' in make and 'obj.AddComponent<CombatProjectile>()' in make
assert 'BindVisualOrigin(companionSource==null?player:null)' in effects
assert 'BowArrowRest:WeaponVisualAnchor.StaffCore' in links and 'BowNock' not in links
assert 'simulation.position=' not in links and 'simulation.rotation=' not in links
assert 'WeaponVisualAnchor.SwordRoot' in links and 'WeaponVisualAnchor.SwordTip' in links and 'side=model.WeaponSwingSide' in links
assert 'CombatReviewEvents.Emit' not in links and 'TakeDamage' not in links
print('PASS: weapon visual-child/logic-root and endpoint wiring contracts (not rendered validation)')

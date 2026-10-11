"""Production wiring checks; does not execute Unity transforms or rendering."""
from pathlib import Path
root = Path(__file__).resolve().parents[1]
m = (root/'Assets/Scripts/Combat/CombatModel.cs').read_text()
a = (root/'Assets/Scripts/Combat/CombatModel.WeaponRig.cs').read_text()
recoil = m[m.index('private void ApplyRecoil()'):m.index('private void LateUpdate()')]
assert 'body.localRotation *= Quaternion.Euler' in recoil
assert 'ActorImpactSpring.Advance' in recoil
hero=m[m.index('private void AnimateHero('):m.index('public void AnimateCharge(')]
assert hero.count('ApplyRecoil(dt)') == 2
assert 'bodyRestRotation' not in recoil
animate = m[m.index('public void Animate(float'):]
assert animate.index('body.localRotation = bodyRestRotation') < animate.index('else if (floating)') < animate.index('else if (treantCompanion)') < animate.rindex('ApplyRecoil();')
assert 'weaponStructure.StaffShaftCenter' in m and 'weaponStructure.StaffShaftHalfLength' in m
assert '1.3f * growth' not in m
assert 'WeaponAnchorLocal(WeaponVisualAnchor.StaffCore)' in m
assert m.count('WeaponAnchorLocal(WeaponVisualAnchor.BowUpperTip)') == 2
assert m.count('WeaponAnchorLocal(WeaponVisualAnchor.BowLowerTip)') == 2
assert 'side * weaponStructure.BowReach' in m
assert 'rig.TransformPoint(WeaponAnchorLocal(anchor))' in a
assert 'arrowRig.localPosition' in a and 'float direction = WeaponSwingSide;' in m
for name in ('Damage', 'Physics.', 'CombatSight', 'TakeDamage', 'Projectile.Create'):
    assert name not in a
for charged in ('true', 'false'):
    assert f'SkillDamageBudgets.SkillPoseDuration(heroClass, skill, {charged})' in m
    assert f'SkillDamageBudgets.SkillPoseStart(heroClass, skill, {charged})' in m
print('PASS: weapon rig/pose layering source contracts; logical origin untouched by anchor helper')

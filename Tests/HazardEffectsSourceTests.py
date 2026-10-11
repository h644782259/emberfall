from pathlib import Path
r=Path(__file__).resolve().parent.parent
read=lambda f:(r/'Assets/Scripts'/f).read_text()
t=read('Combat/EnemyAttackTelegraph.cs'); c=read('Combat/CombatEffects.cs');e=read('Combat/ElementalCombatVfx.cs')
assert 'EnemyImpactRegion.Contains(from, impactCenter, point, impactRadius)' in t and 'FitsCircle(from, point' in t
assert 'WorldTerrain.Ground(points[i]' in t and 'body.GetComponent<Renderer>()' in t
assert 'interruptCore.gameObject.SetActive(value)' in t and 'root.AddComponent<WorldResources>()' in t
assert 'PhysicalSkillMotion.ClearFlight' in c and 'CombatSight.Area(transform.position,enemy.transform.position)' in c
assert 'CombatSight.FillAreaBoundary(circle' in read('Combat/SkillTargetingController.cs')
assert 'CombatSight.VisualFootprint' in read('Combat/FilledSkillVfx.cs')
assert 'CombatSight.VisualFootprint' in read('Combat/ElementalFieldVisual.cs')
assert 'CombatSight.VisualFootprint' in read('Combat/CoveredAreaParticles.cs')
assert 'DecorationBudget.Particles(EffectPreferences.EffectsScale)' in e and 'rate * EffectPreferences.EffectsScale' in e
assert 'DecorationLease.Attach(obj,0)' in e and 'main.maxParticles = DecorationBudget.Particles' in e
assert 'DecorationBudget.DeathMotes' in read('Combat/EnemyDeathDissolve.cs')
assert 'private void OnDisable(){Release();}' in read('Combat/ElementalFieldVisual.cs')
assert 'private void OnDisable(){if(budget!=null)budget.Release(ref leased);}' in read('Combat/DecorationLease.cs')
assert 'fire.gameObject.SetActive(false)' in e and 'StopEmittingAndClear' in e
assert 'DecorationLease' not in t
print('PASS: 15 hazard/effects source contracts (soil coverage/lifecycle behavior is executed by EnemyImpactContourTests)')

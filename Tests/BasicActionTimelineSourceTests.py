#!/usr/bin/env python3
"""Production wiring; no Unity or animation acceptance is claimed."""
from pathlib import Path
root=Path(__file__).resolve().parent.parent
read=lambda n:(root/'Assets/Scripts/Combat'/n).read_text()
p=read('PlayerController.cs');m=read('CombatModel.cs');fx=read('CombatEffects.cs')
b=p[p.index('private void BasicAttack()'):p.index('private bool Melee(')]
assert b.index('attackCooldown = Mathf.Max(.18f, attackCooldown)') < b.index('model.PlayAction(-1,true,attackCooldown)') < b.index('Melee(')
assert 'CombatProjectile.BasicShot(' in b and 'StartCoroutine' not in b and 'Invoke(' not in b
assert 'BasicActionTimeline.Contact(heroClass == HeroClass.Ranger)' in m
assert 'Time.frameCount != actionStartedFrame' in m
assert 'BasicActionTimeline.ArrowVisible(t, acting)' in m and 'BasicActionTimeline.BowDraw(t)' in m
assert all('CancelCombatPose();' in tail[:150] for tail in p.split('CombatEpoch++;')[1:])
assert 'session.IsDead || session.CombatEffectsEnded || hostile && session.CombatEnded || session.Player.CombatEpoch != epoch' in fx
assert 'if (IsDead) return;' in p and 'if (session.InputBlocked)' in p
print('PASS: 8 basic action timing wiring contracts')

assert 'model.BasicActionBlocked' not in p
assert 'TraversalStartedThisFrame || SkillReleaseInputBlocked || skillBasicRecovery.Blocked' in b
assert b.index('model.PlayAction(-1,true,attackCooldown)') < b.index('GameAudio.PlayBasicAttack(HeroClass)') < b.index('BasicAttackMuzzle(ranger)') < b.index('CombatProjectile.BasicShot(')
assert 'new Vector3(aimPoint.x,muzzle.y,aimPoint.z)' in b
assert 'skillBasicRecovery.Advance(dt);' in p
cancel=p[p.index('internal void CancelCombatPose()'):]
cancel=cancel[:cancel.index('\n')]
assert 'skillBasicRecovery.Clear();' in cancel
cast=p[p.index('private void CastSkillNow'): ] if 'private void CastSkillNow' in p else p[p.index('if (!skillRuntime.TryConsume(slot, rank'):]
assert cast.index('skillRuntime.TryConsume(')<cast.index('skillBasicRecovery.Begin(HeroClass, slot, executingChargedSkill)')
assert '!charge.IsCharging && !charge.ConsumedThisFrame' in p
assert 'if (attackCooldown <= 0) BasicAttack();' in p
hit=p[p.index('internal void OnBasicAttackHitTarget'):p.index('private void',p.index('internal void OnBasicAttackHitTarget'))]
assert hit.index('!confirmedLivingHit) return;')<hit.index('skillRuntime.RestoreEnergy(')
print('PASS: gameplay recovery independent of visual pose; confirmed-hit energy and cancellation wiring')

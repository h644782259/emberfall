from pathlib import Path
r=Path(__file__).resolve().parent.parent/'Assets/Scripts'
read=lambda f:(r/f).read_text()
s=read('Combat/AdvancedSkillSequence.cs');fx=read('Combat/CombatEffects.cs');p=read('Combat/PlayerController.cs');m=read('Combat/CombatModel.cs');a=read('Core/GameAudio.cs')
assert '.StatusEffects.Mark(' not in s
assert 'markTarget:lockedTarget,markStrength:.08f+rank*.04f' in s
# Brace-delimit the production Update; companion callbacks may legitimately be a block.
start=fx.index('private void Update()',fx.index('internal sealed class CombatProjectile'))
end=fx.index('{',start)+1;depth=1
while depth:
    depth+=(fx[end]=='{')-(fx[end]=='}');end+=1
update=fx[start:end]
hit=update[update.index('hitTargets.Add(enemy);'):]
assert 'companionSource.OnConfirmedHit(enemy)' in hit
assert hit.index('LockedImpactMarkPolicy.ShouldApply') < hit.index('enemy.StatusEffects.Mark(4f, impactMarkStrength)') < hit.index('enemy.TakeDamage(')
assert 'if (!CombatSight.Direct(previous, enemy.transform.position)) continue;' in fx
assert 'if (TraversalStartedThisFrame || skillBasicRecovery.Blocked) return;' in p and 'CancelCombatPose();' in p[p.index('private bool TryBlinkCore'):p.index('private void AdvanceJump')]
assert 'SkillDamageBudgets.SkillPoseDuration(heroClass, skill, true)' in m and 'SkillDamageBudgets.SkillPoseStart(heroClass, skill, true)' in m
assert 'if (step == 0) GameAudio.Play(SoundCue.Judgment);' in s and 'rank==3 && step==steps-1) SpawnTail(target,5.2f*range' in s
assert 'AudioVoicePolicy.Select' in a and 'now - lastImpact < .1f' in a
assert 'CombatReviewEvents.Emit("takendamage"' in p and 'healthBeforeHit-Health' in p
assert 'CombatReviewEvents.Emit("chargecancel"' in read('Combat/SkillChargeController.cs')
print('PASS: 10 combat review source contracts')

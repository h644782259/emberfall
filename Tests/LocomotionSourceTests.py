from pathlib import Path
r=Path(__file__).resolve().parent.parent/'Assets/Scripts/Combat'
p=(r/'PlayerController.cs').read_text();e=(r/'EnemyController.cs').read_text();m=(r/'CombatModel.cs').read_text();cloth=(r/'TailoredCloth.cs').read_text()
assert 'walkingDisplacement = CombatFx.Flat(transform.position - walkingStart);' in p
assert 'walkingDisplacement += CombatFx.Flat(bounded-beforeBoundary)' in p
assert 'transform.InverseTransformDirection(walkingDisplacement)' in p and '!TraversalStartedThisFrame,jumping,jumpAge/.55f' in p
assert 'model.ResetLocomotion();' in p
assert 'knockVelocity * dt, NavigationRadius)' in e and 'Vector3 actual=CombatFx.Flat(next-transform.position);' in e and 'walkingDisplacement += actual;' in e
assert e.index('if (windup <= 0) ResolveAttack();')<e.index('AnimateModel(0,preparing?.95f:attackAnimation,hurtTime>0);')
assert 'model.SetEnemyAttackPose(EnemyActionPose.Select(preparing,activeChargePose,attackAnimation)' in e
assert 'speed = smoothedSpeed = locomotion.Speed' in m and 'gaitPhase = locomotion.Phase' in m
assert 'EnemyActionPose.Contact(enemyActionPhase,enemyActionProgress)' in m and 'enemyPose = Mathf.Lerp' not in m
assert 'inertiaSide*.004f' in cloth and 'length*length' in cloth
print('PASS: 10 locomotion and guardian phase wiring contracts')

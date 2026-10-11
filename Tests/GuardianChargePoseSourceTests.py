from pathlib import Path
r=Path(__file__).resolve().parent.parent
read=lambda p:(r/p).read_text()
e=read('Assets/Scripts/Combat/EnemyController.cs');m=read('Assets/Scripts/Combat/CombatModel.cs')
assert 'EnemyActionPose.Select(preparing,activeChargePose,attackAnimation)' in e
resolve=e[e.index('private void ResolveAttack'):e.index('private void FinishAttack')]
assert 'activeChargePose = chargeTime > .001f;' in resolve
def member(source,signature):
 start=source.index(signature);end=source.index('{',start)+1;depth=1
 while depth:
  depth+=(source[end]=='{')-(source[end]=='}');end+=1
 return source[start:end]
assert 'activeChargePose = false;' in member(e,'private void FinishAttack')
assert 'activeChargePose = false;' in member(e,'private void CancelAttack')
charge=e[e.index('            if (chargeTime > 0)'):e.index('            else if (preparing)')]
assert charge.index('DamageTarget(damage*1.35f)')<charge.index('AnimateModel(1,.6f,hurtTime>0)')<charge.index('FinishAttack()')
assert 'walkingDisplacement +=' not in charge
assert 'EnemyActionPose.Charge(enemyActionPhase)' in m and 'chargeBrace*23f' in m and 'chargeBrace*58f' in m
assert 'CancelAttack();' in member(e,'private void OnDisable()')
print('PASS: 8 Guardian active-charge production wiring contracts')

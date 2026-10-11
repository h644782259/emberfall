from pathlib import Path
r=Path(__file__).resolve().parent.parent/'Assets/Scripts/Combat'
p=(r/'PlayerController.cs').read_text()
assert 'wasDodgeCounter && lastMeleeDamagedEnemy) session.RecordClassTutorial(HeroClass.Vanguard)' in p
assert 'counterTime = perfectDodgeCounterTime = PlayerUpgradeRules.CounterWindow' in p
for block in p.split('CombatEpoch++;')[1:]: assert 'perfectDodgeCounterTime = 0;' in block[:100]
assert p.index('status.TryShatter(this, castId)')<p.index('session.RecordClassTutorial(HeroClass.Arcanist)')
assert p.index('status.ConsumePoison(this, castId, out bonus)')<p.index('session.RecordClassTutorial(HeroClass.Ranger)')
assert 'if (enemy.Health < healthBefore) lastMeleeDamagedEnemy = true;' in p
assert 'if (CombatReviewEvents.Enabled && !lastMeleeDamagedEnemy) CombatReviewEvents.Emit(basic ? "basicmiss" : "spellmiss"' in p
assert 'session.ReportControlFailure("skill"+skill,"空中")' in p and 'session.ReportControlFailure("skill"+skill,"施法中")' in p
assert 'session.ReportControlFailure("dodge",dodgeCooldown>0?"冷却":jumping?"空中":"位移中")' in p
print('PASS: 9 real-opportunity and refusal wiring contracts (no PlayMode execution)')

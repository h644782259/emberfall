from pathlib import Path
r=Path(__file__).resolve().parent.parent
read=lambda p:(r/'Assets/Scripts'/p).read_text()
rig=read('Combat/LargeBossRig.cs');boss=read('Combat/LargeExpeditionBoss.cs');state=read('Core/LargeBossPhaseState.cs')
assert 'motion.FirstAngle' in rig and 'motion.SecondAngle' in rig and 'age*(charging' not in rig
assert 'LargeBossMotion.Pose(phase,remaining)' in rig and 'encounter.BeamWorldAngle' in rig
assert 'Quaternion.Euler(0,BeamWorldAngle,0)' in boss
assert 'Remaining = WindupSeconds; BeamAngle = 0;' in state
assert 'Radial beam emitter' in rig and 'float swing=' in rig and '*(1-brace)' in rig
fx=read('Combat/EnemySkillPhysicalVfx.cs')
assert 'EnemySweepPhysicalVfx.Create(transform, BeamDangerRadius)' in boss
assert 'sweepVisual.Draw(from, to, progress, live, boss.CanBeSkillInterrupted)' in boss
assert 'core.gameObject.SetActive(!live && interruptible)' in fx
assert 'WorldTerrain.Ground(point, rise * .2f)' in fx and 'PhysicalSkillVfx.Impact' in fx
assert 'LineRenderer' not in boss and 'LineRenderer' not in fx
print('PASS: 10 boss motion/actual sweep alignment/terrain body/interrupt core source contracts')

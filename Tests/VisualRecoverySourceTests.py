from pathlib import Path
root=Path(__file__).resolve().parents[1]
read=lambda f:(root/'Assets/Scripts/Combat'/f).read_text()
m=read('CombatModel.cs');r=read('CombatModel.Recovery.cs');mtn=read('CombatModel.Motion.cs');cloth=read('TailoredCloth.cs')
assert 'CancelAction() { BeginVisualRecovery(); weaponActionId++; actionAge = actionDuration = 0; }' in m
assert 'if (actionDuration > 0 && actionAge < actionDuration || recoveryAge < .12f) BeginVisualRecovery(false);' in m
assert 'ApplyVisualRecovery(dt);' in m and 'AdvanceVisualMotion(dt);' in m
assert 'WeaponVisualAnchor.BowGrip' in r and 'WeaponVisualAnchor.BowNock' in r
assert 'if(recoveryJoints==null)' in r and 'recoveryRotations=new Quaternion[recoveryJoints.Length]' in r
assert 'visualMotion.Turn*10' in mtn and 'Mathf.Max(0,-load)*11' in mtn and 'Mathf.Max(0,load)*11' in mtn
assert 'targetPitch=Mathf.Clamp(pitch,-14,22)' in cloth and 'targetSide=Mathf.Clamp(side,-12,12)' in cloth
assert 'inertiaPitch=Mathf.Lerp' in cloth and 'inertiaSide=Mathf.Lerp' in cloth
for token in ['TakeDamage','Physics.','cooldown','BasicInterval','SkillPoseDuration','transform.position =']:
    assert token not in r+mtn
print('PASS: cosmetic recovery/turn/cloth source wiring; no combat timing or collision writes')

assert "recoveryCancellation||recoveryJoints[i]==headRig||recoveryJoints[i]==cloak" in r
assert "if(delta<=0||visualMotionFrame==Time.frameCount)return" in r

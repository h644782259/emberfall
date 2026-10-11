#!/usr/bin/env python3
"""Focused text admission/read-only view wiring checks; no engine execution."""
from pathlib import Path
root = Path(__file__).resolve().parent.parent
number = (root / 'Assets/Scripts/Combat/FloatingNumber.cs').read_text()
feedback = (root / 'Assets/Scripts/Core/GameSession.Feedback.cs').read_text()
session = (root / 'Assets/Scripts/Core/GameSession.cs').read_text()
fixture = (root / 'Assets/Editor/FeedbackValidation.cs').read_text()

def body(source, signature):
    start = source.index('{', source.index(signature)); depth = 1
    for end in range(start + 1, len(source)):
        depth += (source[end] == '{') - (source[end] == '}')
        if depth == 0: return source[start + 1:end]
    raise AssertionError(signature)

checks = []
def check(value, message):
    if not value: raise AssertionError(message)
    checks.append(message)

select = body(number, 'private static bool TrySelectAdmission')
check('MobileControls.Active?20:36' in select, 'Admission uses actual desktop/mobile display limits')
check(not any(token in select.replace('new CombatTextMetrics(', 'CombatTextMetrics(') for token in ('new ', '=>', '.Retire()', '.Remove', '.Add(')),
      'Admission scanning creates no explicit heap objects and does not mutate visible text')
check('if(!isCritical)return false' in select and '!number.critical&&!number.mechanism' in select,
      'Only critical text can replace an ordinary number at the cap')
check('number==replacement' in select and 'nearby>=(isMechanism?4:isCritical?6:4)' in select,
      'Local admission evaluates the prospective replacement and original local limits')
check('camera.WorldToScreenPoint(origin)' in select and '<110*Density' in select and '<2f' in select,
      'Screen-space proximity and camera-free fallback remain unchanged')
spawn = body(number, 'public static FloatingNumber Spawn')
check(spawn.count('TrySelectAdmission(') == 1 and spawn.index('TrySelectAdmission(') < spawn.index('new GameObject('),
      'Factory performs one complete admission scan before object creation')
check('InitializeAdmitted(' in spawn and '.Initialize(' not in spawn,
      'Factory consumes its immediate local admission without rescanning')
initialize = body(number, 'public void Initialize(')
check('if(counted)return' in initialize and 'TrySelectAdmission(' in initialize and 'InitializeAdmitted(' in initialize,
      'Existing direct initialization remains guarded and uses the same policy')
accepted = body(number, 'private void InitializeAdmitted')
check(accepted.index('replacement.Retire()') < accepted.index('ActiveCount++'),
      'An accepted replacement frees exactly one count before registration')
for source, method in ((session, 'public void SpawnFloatingText'), (feedback, 'public void SpawnCombatDamage')):
    caller = body(source, method)
    check('FloatingNumber.Spawn(' in caller and 'new GameObject(' not in caller and 'ActiveCount' not in caller,
          method + ' delegates admission and creation to the common factory')
check('MechanismCount>=CombatTextLayout.MechanismLimit' in select and 'ActiveCount-MechanismCount>=globalLimit' in select,
      'Independent mechanism reserve does not consume damage capacity')
check('isMechanism:true' in feedback and 'SpawnMechanismText' in feedback,
      'Mechanism captions use an explicit admission channel')
check('CombatTextLayout.CandidateCount' in number and 'box.Overlaps(number.bounds)' in number and 'RequestCharactersInTexture' in number and 'localBounds.size' in number,
      'Bounded slot search uses glyph metrics and actual renderer bounds')
check('OnDisable(){ReleaseCount();}' in number and 'MechanismCount=Mathf.Max(0,MechanismCount-1)' in number,
      'Disable immediately returns both allocation budgets')
check('systemMessagesView ?? (systemMessagesView = systemMessages.AsReadOnly())' in feedback,
      'System log caches one read-only live wrapper rather than allocating per read')
check('systemMessages.Count >= 32' in feedback and 'systemMessages.RemoveAt(0)' in feedback,
      'System log retains its existing bounded append/eviction behavior')
check('RequireIsolatedRuntime' in fixture and 'FloatingNumber.ActiveCount != 0' in fixture,
      'Prepared feedback fixture requires an isolated quiet scene')
check('MobileControls.SimulationEnabled = simulation' in fixture and 'stored.AddRange(previousMessages)' in fixture,
      'Prepared feedback fixture restores simulation and prior log contents')
dense = fixture[fixture.index('// Reject replacement only'):fixture.index('objects = Objects().Length;', fixture.index('// Reject replacement only'))]
check(dense.index('RetireAll();') < dense.index('for (int i = 0; i < 6; i++)') < dense.index('FloatingNumber distantOrdinary = null;')
      and 'FloatingNumber.Spawn(cluster, "99", Color.yellow, true)' in dense,
      'Dense rejection fixture replaces its mixed group with six critical entries before ordinary distant fillers')
print('PASS:', len(checks), 'feedback admission/allocation source contracts')

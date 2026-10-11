from pathlib import Path
r=Path(__file__).resolve().parents[1]
a=r/'Assets/Scripts/Core/CombatReviewObjectId.cs'
for p in (r/'Assets').rglob('*.cs'):
    if p!=a: assert 'GetInstanceID(' not in p.read_text(), str(p)
s=a.read_text()
assert 'EntityId.ToULong(value.GetEntityId()).ToString(CultureInfo.InvariantCulture)' in s
assert s.index('#else') < s.index('GetInstanceID()') < s.index('#endif')
print('PASS: runtime/editor IDs route through the compatible full-width adapter')

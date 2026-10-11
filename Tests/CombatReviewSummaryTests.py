"""Synthetic parser input only, not gameplay evidence; all files are temporary."""
import importlib.util
import json
from pathlib import Path
import tempfile
spec=importlib.util.spec_from_file_location('review',Path(__file__).resolve().parents[1]/'Tools/summarize-combat-review.py')
m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
with tempfile.TemporaryDirectory() as d:
    p=Path(d)/'synthetic-parser-input.jsonl'
    rows=[dict(kind='damage',frame=4,amount=12),dict(kind='projectilehit',frame=4,amount=12),dict(kind='observed_enemy_hp_delta',frame=5,amount=12)]
    rows += [dict(kind='projectileend',frame=5,detail='3:expired:hits=0'),dict(kind='projectileend',frame=5,detail='4:retired:hits=0'),dict(kind='projectileend',frame=5,detail='5:terrain:hits=1')]
    p.write_text('\n'.join(json.dumps(r) for r in rows))
    out=m.summarize(p)
    assert out['recorded_damage_amount']==12
    assert out['event_counts']['projectilehit']==1 and 'death' in out['unobserved_event_kinds']
    assert out['derived_projectile_no_enemy_hit']==1
    assert out['first_frame']==4 and out['last_frame']==5
    p.write_text('{}')
    try:m.summarize(p)
    except ValueError:pass
    else:raise AssertionError('malformed rows accepted')
print('PASS: summary avoids duplicate damage and invented missing events; rejects malformed input')

# Values differing only below a double's precision remain distinct, including ulong max.
assert m.normalize_object_id("18446744073709551615") != m.normalize_object_id("18446744073709551614")
assert m.normalize_object_id(-2147483648) == "-2147483648"
assert m.normalize_object_id(0) == m.normalize_object_id("0") == "0"
for bad in (1.0, True, "18446744073709551616", "01", "-2147483649"):
    try: m.normalize_object_id(bad)
    except ValueError: pass
    else: raise AssertionError(f'Lossy/invalid ID accepted: {bad!r}')
print('PASS: lossless 64-bit identity and legacy/null ID parser compatibility')

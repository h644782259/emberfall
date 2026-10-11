from pathlib import Path
import importlib.util
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('capture',root/'Tests/CombatReviewInstrumentationTests.py')
probe=importlib.util.module_from_spec(spec);spec.loader.exec_module(probe)
assert len(probe.production_statements())==17
fx=(root/'Assets/Scripts/Combat/CombatEffects.cs').read_text()
assert 'detail:"blocked_muzzle;prop_may_have_been_hit");CombatFx.Ring(' in fx
print('PASS: 17 production capture sites guard ID/event construction; blocked-muzzle gameplay effect remains outside telemetry guard')

"""Validate committed buffers, budgets and the narrow presentation-only hook diff."""
from pathlib import Path
import struct, math, hashlib, json, subprocess
root=Path(__file__).resolve().parents[2];report={}
for p in sorted((root/'Assets/Resources/BlenderVfx').glob('*.bytes')):
 b=p.read_bytes();magic,n,k=struct.unpack_from('<Iii',b);assert magic==0x45464D31 and 0<n<=4096 and 0<k<=12288 and k%3==0 and len(b)==12+n*32+k*4
 for i in range(n):
  v=struct.unpack_from('<8f',b,12+i*32);assert all(math.isfinite(x) for x in v);assert max(abs(x) for x in v[:3])<=16;assert .9<=sum(x*x for x in v[3:6])<=1.1,(p,i,v)
 ids=struct.unpack_from('<%di'%k,b,12+n*32);assert all(0<=x<n for x in ids)
 report[p.name]={'vertices':n,'triangles':k//3,'bytes':len(b),'sha256':hashlib.sha256(b).hexdigest()}
assert len(report)==4
source=(root/'Assets/Scripts/Combat/BlenderSkillVfx.cs').read_text()
assert 'TakeDamage(' not in source and 'HitArea(' not in source and 'CombatArea.Spawn' not in source
assert 'AuthoredActorMeshes.Decode(data.bytes' in source
assert 'owner.CombatEpoch!=epoch' in source and 'session.InputBlocked||Time.deltaTime<=0' in source
assert 'ReducedEffects?5:Application.isMobilePlatform?8:12' in source
print(json.dumps({'result':'PASS','validation':'binary geometry + source contract, NOT Unity runtime','assets':report},indent=2))

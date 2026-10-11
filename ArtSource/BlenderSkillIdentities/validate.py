from pathlib import Path
import struct,math,json,hashlib
root=Path(__file__).resolve().parents[2];records={}
for p in sorted((root/'Assets/Resources/BlenderSkillIdentities').glob('*.bytes')):
 b=p.read_bytes();magic,n,k=struct.unpack_from('<Iii',b);assert magic==0x45464D31 and 0<n<=256 and 0<k<=1200 and k%3==0 and len(b)==12+n*32+k*4
 for i in range(n):
  v=struct.unpack_from('<8f',b,12+i*32);assert all(math.isfinite(x) for x in v);assert max(abs(x) for x in v[:3])<1.5;assert .9<=sum(x*x for x in v[3:6])<=1.1
 assert all(0<=i<n for i in struct.unpack_from('<%di'%k,b,12+n*32));records[p.stem]={'vertices':n,'triangles':k//3,'bytes':len(b),'sha256':hashlib.sha256(b).hexdigest()}
assert set(records)=={'BladeSlices','ForkPulse','ContractSigil','ProtectionCage'}
assert len(set(r['sha256'] for r in records.values()))==4
print(json.dumps({'result':'PASS','scope':'4 distinct authored data buffers, finite/unit normals/index/mesh budgets; NOT Unity','assets':records,'totalRuntimeBytes':sum(r['bytes'] for r in records.values())},indent=2))

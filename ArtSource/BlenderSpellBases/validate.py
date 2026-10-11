from pathlib import Path
import struct, math, json
root=Path(__file__).resolve().parents[2];results={};expected={'Crescent','Crystal','Flame','Sword','Lightning','Rupture','ArcaneShard','Arrow','Vine','IcePrimary','FirePrimary'}
for p in sorted((root/'Assets/Resources/BlenderSpellBases').glob('*.bytes')):
 b=p.read_bytes();magic,n,k=struct.unpack_from('<Iii',b);assert magic==0x45464D31 and 0<n<=4096 and 0<k<=12288 and k%3==0 and len(b)==12+n*32+k*4
 for i in range(n):
  v=struct.unpack_from('<8f',b,12+i*32);assert all(math.isfinite(x) for x in v);assert max(abs(x) for x in v[:3])<2;assert .9<=sum(x*x for x in v[3:6])<=1.1,(p,i,v)
 if p.stem=='Crescent':
  assert all(abs(struct.unpack_from('<f',b,12+i*32+28)[0]-(1 if i%4==0 else 0 if i%4==2 else .6))<1e-6 for i in range(n)), 'preserved cross-section bright rim UV'
 if p.stem=='Flame':
  assert all(abs(struct.unpack_from('<f',b,12+i*32+28)[0]-(i//8)/8)<1e-6 for i in range(n)), 'flame normalized height UV'
 assert all(0<=i<n for i in struct.unpack_from('<%di'%k,b,12+n*32));results[p.stem]={'vertices':n,'triangles':k//3,'bytes':len(b)}
assert set(results)==expected
s=(root/'Assets/Scripts/Combat/FilledSkillVfx.cs').read_text()
for name in expected-{'IcePrimary','FirePrimary'}:assert 'AuthoredSpellBases.Load("'+name+'")??Mesh(FilledVfxRecipes.'+name+'()' in s
assert 'ReducedEffects?FilledVfxRecipes.ReducedParts:Application.isMobilePlatform?10:FilledVfxRecipes.MaximumParts' in s
assert 'AnchoredImpactMesh.Create(mesh,transform,at,dimensions*1.15f,rotation)' in s
assert max(x['triangles'] for x in results.values())<=300
print(json.dumps({'result':'PASS','coverage':'11 buffers, unit normals, strict envelope, per-asset fallback wiring, unchanged mobile cap and anchored clipping source contract; NOT Unity validation','assets':results,'runtimeBytes':sum(x['bytes'] for x in results.values())},indent=2))

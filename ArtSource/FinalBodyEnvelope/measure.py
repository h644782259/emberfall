"""Actual same-height profile plus triangle crossings. No mismatched shoulder extrema."""
from pathlib import Path
import sys,json,math,hashlib
import numpy as np
from intersections import crossings
actors=json.loads(Path(sys.argv[1]).read_text());effects=json.loads(Path(sys.argv[2]).read_text());rows=[];hits=[]
# Discriminating analytic oracle checks: crossing segment versus an actual finite triangle, then translated disjoint triangles.
a=[[0,0,0],[1,0,0],[0,1,0]];b=[[.2,.2,-1],[.2,.2,1],[.8,.2,0]]
assert crossings(a,[0,1,2],b,[0,1,2]);assert not crossings(a,[0,1,2],[[x+4,y,z] for x,y,z in b],[0,1,2])
unique={}
for e in effects:
 key=hashlib.sha256(json.dumps([{'vertices':p['vertices'],'triangles':p['triangles']} for p in e['objects']],sort_keys=True).encode()).hexdigest();unique.setdefault(key,{'objects':e['objects'],'states':[]})['states'].append(e['phase'])
for key,e in unique.items():
 fx=[v for p in e['objects'] for v in p['vertices']];edges=[]
 for p in e['objects']:
  tris=np.asarray(p['vertices'])[np.asarray(p['triangles']).reshape(-1,3)]
  edges.extend(np.stack([tris,np.roll(tris,-1,axis=1)],axis=2).reshape(-1,2,3))
 edges=np.asarray(edges);a=edges[:,0];b=edges[:,1];cache={}
 def limits(y):
  if y not in cache:
   selected=(np.minimum(a[:,1],b[:,1])<=y)&(np.maximum(a[:,1],b[:,1])>=y)&(np.abs(a[:,1]-b[:,1])>1e-10)
   aa=a[selected];bb=b[selected];x=aa[:,0]+(bb[:,0]-aa[:,0])*(y-aa[:,1])/(bb[:,1]-aa[:,1]);cache[y]=(float(min(x)),float(max(x))) if len(x) else None
  return cache[y]
 for c in actors:
  parts=[p for p in c['parts'] if not p['category'].startswith('external-')];head=[v for p in parts if p['category']=='head' for v in p['vertices']];shoulders=[v for p in parts if p['category']=='shoulder' for v in p['vertices']];body=[v for p in parts for v in p['vertices']]
  assert head and shoulders and body
  profile=[]
  for p in parts:
   if p['category'] in ['head','shoulder']:
    for v in p['vertices']:
     lr=limits(v[1]);profile.append(min(v[0]-lr[0],lr[1]-v[0]) if lr else -999)
  vertices=[];triangles=[];owners=[]
  for p in parts:
   offset=len(vertices);vertices.extend(p['vertices']);triangles.extend(t+offset for t in p['triangles']);owners.extend([p]*(len(p['triangles'])//3))
  for surface in e['objects']:
   grouped={}
   for pair in crossings(vertices,triangles,surface['vertices'],surface['triangles']):grouped.setdefault(id(owners[pair[0]]),[]).append(pair)
   for pairs in grouped.values():
    part=owners[pairs[0][0]];hits.append(dict(hero=c['name'],tier=c['tier'],phase=c['phase'],skill=c['skill'],part=part['name'],category=part['category'],effectStates=e['states'],crossings=len(pairs),actorTrianglePairs=pairs[:12]))
  crown=max(v[1] for v in fx)-max(v[1] for v in head)
  rows.append(dict(hero=c['name'],tier=c['tier'],phase=c['phase'],skill=c['skill'],effectStates=e['states'],sameHeightHeadShoulderLateralMargin=min(profile),crownClearance=crown,bodyBounds=[[min(v[a] for v in body),max(v[a] for v in body)] for a in range(3)],includedParts=[p['name'] for p in parts],excludedParts=[{'name':p['name'],'reason':p['category']} for p in c['parts'] if p['category'].startswith('external-')]))
report={'effectGeometryHashes':list(unique),'rows':rows,'triangleCrossings':hits};Path(sys.argv[3]).write_text(json.dumps(report,indent=2)+'\n')
print('MEASURE',len(actors),'actual combined bodies/actions,',len(effects),'true protection triggers;',len(hits),'intersecting body/ornament samples',flush=True)
for h in hits[:5]:print(h,flush=True)
assert not hits,'actual protection triangles cross assembled body/head/shoulder ornaments'
assert min(r['sameHeightHeadShoulderLateralMargin'] for r in rows)>0,'local head/shoulder extends past actual protection profile'
assert min(r['crownClearance'] for r in rows)>.15,'actual head ornaments have insufficient crown clearance'
print('PASS actual combined geometry: no sampled noncoplanar triangle crossings; positive local head/shoulder profile margin; crown >.15. External weapon/shield/wings remain rendered and contact-tested, not cage-size inputs.')

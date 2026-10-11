"""Check actual exported static construction/module use and wolf leg overlap; not animation acceptance."""
from pathlib import Path
import json,hashlib,sys
geometry=Path(sys.argv[1]);destination=Path(sys.argv[2]) if len(sys.argv)>2 else Path(__file__).parent
destination.mkdir(parents=True,exist_ok=True)
cases=json.loads(geometry.read_text());report=[]
for c in cases:
 names={}
 for part in c['parts']:
  mesh=part.get('mesh','')
  if mesh and mesh.startswith('Blender actor '):names[mesh]=names.get(mesh,0)+1
 assert names, 'no actual module applied in '+str(c.get('name',c.get('hero')))
 if 'hero' in c and c['tier']==-1:
  assert 'Blender actor '+('Cuirass' if c['hero']==0 else 'ClothTorso') in names
 if c.get('name')=='Companion-2':assert 'Blender actor BarkTorso' in names
 if c.get('name')=='LargeBoss':assert 'Blender actor BossHousing' in names and names.get('Blender actor BossPlate')==4
 report.append({'case':c.get('name',f"Hero-{c.get('hero')}-equipment-{c.get('tier')}"),'authored_module_instances':names,'total_scene_triangles':sum(len(x['triangles'])//3 for x in c['parts'])})
wolf=next(x for x in cases if x.get('name')=='Companion-0')
body=next(p for p in wolf['parts'] if p['name']=='Spirit wolf torso')
def inside_mesh(q,body):
 heights=[];v=body['vertices'];t=body['triangles']
 for i in range(0,len(t),3):
  a,c,d=[v[j] for j in t[i:i+3]]
  x,z=q[0],q[2];den=(c[2]-d[2])*(a[0]-d[0])+(d[0]-c[0])*(a[2]-d[2])
  if abs(den)<1e-10:continue
  u=((c[2]-d[2])*(x-d[0])+(d[0]-c[0])*(z-d[2]))/den
  vv=((d[2]-a[2])*(x-d[0])+(a[0]-d[0])*(z-d[2]))/den
  if min(u,vv,1-u-vv)>=-1e-7:heights.append(u*a[1]+vv*c[1]+(1-u-vv)*d[1])
 return bool(heights) and min(heights)<=q[1]<=max(heights)
overlap=[]
for paw in (p for p in wolf['parts'] if p['name']=='Paw'):
 top=max(v[1] for v in paw['vertices']);points=[v for v in paw['vertices'] if v[1]>top-.001]
 count=sum(inside_mesh(q,body) for q in points);assert count>0,'detached wolf leg cap'
 overlap.append({'top_world_y':top,'inside_torso_vertices':count,'sampled_cap_vertices':len(points)})
assert len(overlap)==4
head=next(p for p in wolf['parts'] if p['name']=='Wolf head')
eye_overlap=[]
for eye in (p for p in wolf['parts'] if p['name']=='Luminous eye'):
 count=sum(inside_mesh(q,head) for q in eye['vertices']);assert count>0,'detached wolf eye'
 eye_overlap.append(count)
assert len(eye_overlap)==2
(destination/'construction-inventory.json').write_text(json.dumps({'source_geometry_sha256':hashlib.sha256(geometry.read_bytes()).hexdigest(),'scope':'Actual factory construction and decoder with managed Unity TRS doubles; static Blender preview, not Unity','cases':report,'wolf_static_attachment_overlap':overlap,'wolf_eye_overlap_vertices':eye_overlap},indent=2)+'\n')
print('PASS actual loaded modules across',len(cases),'constructed actors. Correct cloth/metal/bark dispatch; boss 4 articulated shell plates.')
print('PASS all 4 wolf cap surfaces overlap actual transformed torso:',overlap)
print('PASS both eye surfaces overlap actual transformed head:',eye_overlap)
print('Scope: static mesh/TRS geometry only; no Unity rendering, locomotion or gameplay collision validation.')

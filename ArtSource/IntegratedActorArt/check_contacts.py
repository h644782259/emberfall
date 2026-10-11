"""Actual emitted vertex checks for treant contacts in the combined actual-factory sample. No raster claim."""
import json,sys,math
from pathlib import Path
cases=json.loads(Path(sys.argv[1]).read_text());report=[]
def inside(q,part):
 heights=[];v=part['vertices'];tri=part['triangles']
 for i in range(0,len(tri),3):
  a,b,c=[v[j] for j in tri[i:i+3]];x,z=q[0],q[2];den=(b[2]-c[2])*(a[0]-c[0])+(c[0]-b[0])*(a[2]-c[2])
  if abs(den)<1e-10:continue
  u=((b[2]-c[2])*(x-c[0])+(c[0]-b[0])*(z-c[2]))/den;w=((c[2]-a[2])*(x-c[0])+(a[0]-c[0])*(z-c[2]))/den
  if min(u,w,1-u-w)>=-1e-7:heights.append(u*a[1]+w*b[1]+(1-u-w)*c[1])
 return bool(heights) and min(heights)-1e-6<=q[1]<=max(heights)+1e-6
for c in cases:
 if c['name']!='Treant':continue
 body=next(p for p in c['parts'] if p['name']=='Breastplate');head=next(p for p in c['parts'] if p['name']=='Head')
 shoulders=[sum(inside(v,body) for v in p['vertices'])+sum(inside(v,p) for v in body['vertices']) for p in c['parts'] if p['name']=='Pauldrons']
 eyes=[sum(inside(v,head) for v in p['vertices']) for p in c['parts'] if p['name'].startswith('Eyes')]
 assert len(shoulders)==2 and min(shoulders)>0,('tree shoulder contact',c['pose'],shoulders)
 assert len(eyes)==2 and min(eyes)>0,('tree face contact',c['pose'],eyes)
 report.append({'pose':c['pose'],'bidirectional_shoulder_body_contact_vertex_samples':shoulders,'eye_vertices_inside_head':eyes})
Path(sys.argv[2]).write_text(json.dumps(report,indent=2)+'\n');print('PASS treant final combined pose: two shoulder-shell intersections (bidirectional vertex samples) and both eye/head contacts preserved',report)

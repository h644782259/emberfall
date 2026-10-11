"""Actual world-space triangle/triangle crossing check, no bounds-only inference."""
import numpy as np

def crossings(vertices,triangles,effect_vertices,effect_triangles):
 a=np.asarray(vertices,dtype=float)[np.asarray(triangles).reshape(-1,3)]
 b=np.asarray(effect_vertices,dtype=float)[np.asarray(effect_triangles).reshape(-1,3)]
 amin=a.min(axis=1);amax=a.max(axis=1);hits=[]
 def segments(tri,other):
  # Both directed triangle edge sets are tested, finite segment [0,1].
  orig=tri;direc=np.roll(tri,-1,axis=0)-tri;e1=other[1]-other[0];e2=other[2]-other[0]
  h=np.cross(direc,e2);det=h@e1;valid=np.abs(det)>1e-10;inv=np.divide(1,det,out=np.zeros_like(det),where=valid)
  s=orig-other[0];u=inv*np.einsum('ij,ij->i',s,h);q=np.cross(s,e1);v=inv*np.einsum('ij,ij->i',direc,q);t=inv*(q@e2)
  return bool(np.any(valid&(u>=-1e-8)&(v>=-1e-8)&(u+v<=1+1e-8)&(t>=-1e-8)&(t<=1+1e-8)))
 for j,triangle in enumerate(b):
  idx=np.where(np.all(amax>=triangle.min(axis=0)-1e-8,axis=1)&np.all(amin<=triangle.max(axis=0)+1e-8,axis=1))[0]
  for i in idx:
   if segments(a[i],triangle) or segments(triangle,a[i]):hits.append([int(i),j])
 return hits
if __name__=='__main__':
 import json,sys
 c=json.load(open(sys.argv[1]));e=json.load(open(sys.argv[2]))[0]['objects'][0];rows=[]
 for actor in c:
  for p in actor['parts']:
   if p['category'].startswith('external-'):continue
   hits=crossings(p['vertices'],p['triangles'],e['vertices'],e['triangles'])
   if hits:rows.append({'hero':actor['name'],'tier':actor['tier'],'phase':actor['phase'],'skill':actor['skill'],'part':p['name'],'crossings':len(hits),'trianglePairs':hits[:12]})
 print(json.dumps(rows,indent=2));json.dump(rows,open(sys.argv[3],'w'),indent=2)

"""Export hand-edited canonical meshes without rebuilding the source split.
blender -b ArtSource/WeaponModules/WeaponModules.blend --python ArtSource/WeaponModules/export_blend.py
Object layout translations are review-only; mesh coordinates define module frames.
"""
import bpy,bmesh,struct,json
from pathlib import Path
root=Path(__file__).resolve().parents[2]
# Canonical editable runtime modules; rebuild measurement output on export.
budget={'modules':[{'name':name} for name in ['StarBlade', 'StarGuard', 'StarGrip', 'StarPommel', 'BowJoiner', 'StaffShaft', 'MechanicalHoop', 'Facet']]}
for entry in budget['modules']:
 o=bpy.data.objects[entry['name']];m=o.data;bm=bmesh.new();bm.from_mesh(m);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(m);bm.free();m.calc_loop_triangles();values=[];indices=[]
 for t in m.loop_triangles:
  for i in t.vertices:
   v=m.vertices[i].co;n=t.normal;indices.append(len(values));values.append((v.x,v.z,-v.y,n.x,n.z,-n.y,v.x+.5,v.z))
 assert len(indices)//3<=512
 data=struct.pack('<III',0x45464d31,len(values),len(indices))+b''.join(struct.pack('<8f',*v) for v in values)+struct.pack('<%di'%len(indices),*indices);(root/'Assets/Resources/WeaponModules'/(o.name+'.bytes')).write_bytes(data);entry.update(triangles=len(indices)//3,bytes=len(data))
budget['total_bytes']=sum(x['bytes'] for x in budget['modules']);(root/'ArtSource/WeaponModules/budget.json').write_text(json.dumps(budget,indent=2)+'\n');print('PASS exported 8 editable meshes with <=512 triangle cap')

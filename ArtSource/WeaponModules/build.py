"""Split the existing original StarcoreSword .blend; reuse palette, no atlas export."""
import bpy,bmesh,math,struct,json,hashlib
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];SRC=ROOT/'ArtSource/WeaponModules';OUT=ROOT/'Assets/Resources/WeaponModules';OUT.mkdir(exist_ok=True)
original=ROOT/'ArtSource/BlenderPilot/Emberfall-Pilot-Props.blend';bpy.ops.wm.open_mainfile(filepath=str(original));sword=bpy.data.objects['StarcoreSword'];mesh=sword.data
anchors={o.name:list(o.location-sword.location) for o in bpy.data.objects if o.type=='EMPTY' and o.get('asset')=='StarcoreSword'}
assert len(anchors)==6
neighbors={i:set() for i in range(len(mesh.vertices))}
for e in mesh.edges:a,b=e.vertices;neighbors[a].add(b);neighbors[b].add(a)
remaining=set(neighbors);groups=[]
while remaining:
 todo=[next(iter(remaining))];component=set()
 while todo:
  i=todo.pop()
  if i in component:continue
  component.add(i);todo.extend(neighbors[i]-component)
 remaining-=component;groups.append(component)
modules={k:[] for k in ['StarBlade','StarGuard','StarGrip','StarPommel']}
for g in groups:
 lo=[min(mesh.vertices[i].co[a] for i in g) for a in range(3)];hi=[max(mesh.vertices[i].co[a] for i in g) for a in range(3)]
 key='StarBlade' if hi[2]>1.25 else 'StarGuard' if lo[0]<-.24 else 'StarPommel' if lo[2]<-.19 else 'StarGrip' if lo[2]>=-.136 and hi[2]<=.131 else None
 if key:modules[key].append(g)
objects=[]
for key,gs in modules.items():
 ids=set.union(*gs);ids=sorted(ids);remap={j:i for i,j in enumerate(ids)};verts=[tuple(mesh.vertices[j].co) for j in ids];faces=[tuple(remap[j] for j in p.vertices) for p in mesh.polygons if set(p.vertices)<=set(ids)]
 m=bpy.data.meshes.new(key);m.from_pydata(verts,[],faces);m.update();o=bpy.data.objects.new(key,m);bpy.context.collection.objects.link(o);objects.append(o)
 # Canonical primitive envelope: y-up Unity. Blade rooted at y=0; cylinder grip has height2.
 lo=[min(v.co[a] for v in m.vertices) for a in range(3)];hi=[max(v.co[a] for v in m.vertices) for a in range(3)]
 for v in m.vertices:
  for a in range(3):v.co[a]=(v.co[a]-lo[a])/(hi[a]-lo[a])-(0 if key=='StarBlade' and a==2 else .5)
  if key=='StarGrip':v.co.z*=2
 o['origin']='Split connected source components from existing StarcoreSword .blend, no new purchase/asset'
for o in list(bpy.data.objects):
 if o not in objects:bpy.data.objects.remove(o,do_unlink=True)
def rings(name,rows,n):
 vs=[(r*math.cos(i*math.tau/n),r*math.sin(i*math.tau/n),y) for y,r in rows for i in range(n)];fs=[]
 for j in range(len(rows)-1):
  for i in range(n):a=j*n+i;b=j*n+(i+1)%n;fs.append((a,b,b+n,a+n))
 fs += [tuple(reversed(range(n))),tuple((len(rows)-1)*n+i for i in range(n))]
 m=bpy.data.meshes.new(name);m.from_pydata(vs,[],fs);o=bpy.data.objects.new(name,m);bpy.context.collection.objects.link(o);objects.append(o)
rings('BowJoiner',[(-1,.17),(0,.5),(1,.14)],6)
rings('StaffShaft',[(-1,.5),(1,.5)],6)
# Same mechanical hoop identity/envelope as CostumeRecipes.Ring, lower segments for tiny weapon ornaments.
vs=[];fs=[]
for i in range(12):
 for j in range(3):
  a=i*math.tau/12;b=j*math.tau/3;r=.8+math.cos(b)*.055;vs.append((math.cos(a)*r,-math.sin(b)*.055,math.sin(a)*r))
for i in range(12):
 for j in range(3):
  a=i*3+j;b=((i+1)%12)*3+j;c=((i+1)%12)*3+(j+1)%3;d=i*3+(j+1)%3;fs.append((a,b,c,d))
m=bpy.data.meshes.new('MechanicalHoop');m.from_pydata(vs,[],fs);o=bpy.data.objects.new('MechanicalHoop',m);bpy.context.collection.objects.link(o);objects.append(o)
vs=[(-.5,0,0),(.5,0,0),(0,-.5,0),(0,.5,0),(0,0,-.5),(0,0,.5)];fs=[(a,b,c) for a,b in [(0,2),(2,1),(1,3),(3,0)] for c in [4,5]]
m=bpy.data.meshes.new('Facet');m.from_pydata(vs,[],fs);o=bpy.data.objects.new('Facet',m);bpy.context.collection.objects.link(o);objects.append(o)
budget=[]
for o in objects:
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-7);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free();o.data.update();o.data.calc_loop_triangles();data=[];indices=[]
 for t in o.data.loop_triangles:
  n=t.normal
  for i in t.vertices:
   v=o.data.vertices[i].co;indices.append(len(data));data.append((v.x,v.z,-v.y,n.x,n.z,-n.y,v.x+.5,v.z))
 assert 0<len(indices)//3<=512,(o.name,len(indices)//3)
 binary=struct.pack('<III',0x45464d31,len(data),len(indices))+b''.join(struct.pack('<8f',*v) for v in data)+struct.pack('<%di'%len(indices),*indices);(OUT/(o.name+'.bytes')).write_bytes(binary)
 o.data.materials.clear();o.location=(objects.index(o)*1.8,0,0)
 budget.append(dict(name=o.name,triangles=len(indices)//3,bytes=len(binary)))
assert len(objects)==8
(SRC/'budget.json').write_text(json.dumps(dict(source=str(original.relative_to(ROOT)),source_sha256=hashlib.sha256(original.read_bytes()).hexdigest(),source_anchors_blender=anchors,modules=budget,texture_bytes=0,new_materials=0,total_bytes=sum(x['bytes'] for x in budget)),indent=2)+'\n')
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'WeaponModules.blend'));print(json.dumps(budget))

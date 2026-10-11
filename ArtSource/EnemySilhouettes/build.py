"""Original F2 enemy/anchor rigid modules. Blender 4.3.2; no external assets/textures.
blender -b --factory-startup --python ArtSource/EnemySilhouettes/build.py
"""
import bpy,bmesh,math,struct,json,hashlib
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Assets/Resources/EnemySilhouettes';SRC=Path(__file__).resolve().parent;OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
assets=[]
def make(name,v,f):
 mesh=bpy.data.meshes.new(name);mesh.from_pydata([(x,-z,y) for x,y,z in v],[],f);mesh.update();bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free();obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj);assets.append(obj);return obj

def ring_data(rows,sides=8,angle=math.pi/4):
 v=[];f=[]
 for y,rx,rz,cx,cz in rows:
  for i in range(sides):
   a=2*math.pi*i/sides+angle;v.append((cx+rx*math.cos(a),y,cz+rz*math.sin(a)))
 for j in range(len(rows)-1):
  for i in range(sides):
   a=j*sides+i;b=j*sides+(i+1)%sides;f.append((a,b,b+sides,a+sides))
 f.extend([tuple(reversed(range(sides))),tuple((len(rows)-1)*sides+i for i in range(sides))]);return v,f

def rings(name,rows,sides=8,angle=0):return make(name,*ring_data(rows,sides,angle))
def profile_data(points,depth=1,bevel=.06):
 v=[];f=[];n=len(points)
 for z,scale in [(-depth*.5,1-bevel),(-depth*(.5-bevel),1),(depth*(.5-bevel),1),(depth*.5,1-bevel)]:
  v.extend((x*scale,y*scale,z) for x,y in points)
 for j in range(3):
  for i in range(n):f.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
 f.extend([tuple(reversed(range(n))),tuple(3*n+i for i in range(n))]);return v,f

def profile(name,points,depth=1,bevel=.06):return make(name,*profile_data(points,depth,bevel))
# Spear-shaped folded ears retain the original capsule origin/2-unit Y envelope.
rings('GoblinEar',[(-1,.16,.20,0,-.06),(-.63,.38,.35,0,-.03),(-.14,.50,.50,.0,0),(.36,.28,.29,-.08,.03),(1,.018,.03,-.13,.04)],4,math.pi/4)
# Low leather cap with a projecting lower rim; the crown leans rather than resembling a metal helmet.
rings('GoblinCap',[(-.5,.45,.48,0,0),(-.35,.50,.50,0,0),(-.23,.45,.43,0,-.035),(.04,.40,.37,-.04,-.035),(.33,.29,.29,-.09,-.035),(.5,.12,.15,-.12,-.035)],12)
profile('GoblinKnife',[(-.13,-.5),(.13,-.5),(.12,-.22),(.36,-.14),(.48,.04),(.36,.27),(.03,.5),(-.10,.13),(-.18,-.13),(-.13,-.22)],1,.045)
# Articulated stone plates stay inside the original box envelope and reuse the original steel palette.
profile('GuardianChest',[(-.31,-.5),(.31,-.5),(.50,-.16),(.50,.22),(.36,.50),(-.36,.50),(-.50,.22),(-.50,-.16)],1,.065)
# Bottom corners are retained for the already validated down-pose shoulder contact.
v,f=profile_data([(-.5,-.5),(.5,-.5),(.5,.1),(.31,.5),(-.31,.5),(-.5,.1)],1,.055)
v=[(x/(1-.055) if abs(z)>.49 and y<-.4 else x,-.5 if y<-.4 else y,z) for x,y,z in v]
make('GuardianShoulder',v,f)
# Open crown band plus five stone teeth: face and original glowing center remain visible.
v=[];f=[]
def add_box(at,size):
 start=len(v);x,y,z=at;dx,dy,dz=[n*.5 for n in size];v.extend([(x+a*dx,y+b*dy,z+c*dz) for a,b,c in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)] ]);f.extend(tuple(start+i for i in face) for face in [(0,3,2,1),(4,5,6,7),(0,1,5,4),(3,7,6,2),(0,4,7,3),(1,2,6,5)])
add_box((0,-.28,-.43),(1,.44,.14));add_box((0,-.28,.43),(1,.44,.14));add_box((-.43,-.28,0),(.14,.44,.72));add_box((.43,-.28,0),(.14,.44,.72))
for x,z,height in [(-.40,.40,.72),(.40,.40,.72),(-.40,-.40,.62),(.40,-.40,.62),(0,-.43,.85)]:
 vv,ff=profile_data([(-.10,-.5),(.10,-.5),(.08,.12),(0,.5),(-.08,.12)],.14,.045);start=len(v);v.extend((a+x,b*height+.5-height*.5,c+z) for a,b,c in vv);f.extend(tuple(start+i for i in face) for face in ff)
make('GuardianCrown',v,f)
rings('AnchorPlinth',[(-1,.47,.47,0,0),(-.72,.5,.5,0,0),(-.47,.5,.5,0,0),(-.22,.37,.37,0,0),(.48,.37,.37,0,0),(.72,.45,.45,0,0),(1,.45,.45,0,0)],10)
rings('AnchorCrystal',[(-.5,.22,.22,0,0),(-.30,.40,.40,0,0),(.08,.50,.50,0,0),(.32,.30,.30,.03,0),(.5,.015,.025,.06,0)],6,math.pi/6)
rings('AnchorClaw',[(-1,.26,.27,0,-.14),(-.58,.46,.38,0,-.10),(-.10,.50,.50,0,0),(.35,.28,.36,0,.10),(1,.035,.05,0,.24)],6)
report={}
for obj in assets:
 mesh=obj.data;mesh.calc_loop_triangles();v=[];n=[];uv=[];ids=[]
 for tri in mesh.loop_triangles:
  for vi in tri.vertices:
   p=mesh.vertices[vi].co;normal=tri.normal;v.append((p.x,p.z,-p.y));n.append((normal.x,normal.z,-normal.y));uv.append((p.x+.5,p.z*.5+.5));ids.append(len(ids))
 assert len(ids)//3<=512,obj.name
 data=struct.pack('<III',0x45464D31,len(v),len(ids))+b''.join(struct.pack('<8f',*a,*b,*c) for a,b,c in zip(v,n,uv))+struct.pack('<%di'%len(ids),*ids)
 path=OUT/(obj.name+'.bytes');path.write_bytes(data);meta=path.with_suffix('.bytes.meta')
 if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+hashlib.sha256(('emberfall.enemy-silhouettes.v1/'+obj.name).encode()).hexdigest()[:32]+'\nTextScriptImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
 report[obj.name]={'triangles':len(ids)//3,'bytes':len(data),'bounds':[[min(p[k] for p in v) for k in range(3)],[max(p[k] for p in v) for k in range(3)]],'sha256':hashlib.sha256(data).hexdigest()}
 obj.location=((len(report)-1)%3*2.5,(len(report)-1)//3*3,1)
bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'EnemySilhouettes.blend'),compress=True)
(SRC/'budget.json').write_text(json.dumps({'source':'Original Blender script; no external inputs','blender':bpy.app.version_string,'assets':report,'total_source_bytes':sum(x['bytes'] for x in report.values()),'new_textures':0,'new_runtime_materials':0},indent=2)+'\n')
print('PASS',len(assets),'modules',sum(x['bytes'] for x in report.values()),'source bytes; each <=512 triangles; 0 textures/materials')

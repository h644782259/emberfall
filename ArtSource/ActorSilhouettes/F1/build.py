"""Original F1 rigid identity art, Blender 4.3.2, no external inputs.
blender -b --factory-startup --python ArtSource/ActorSilhouettes/F1/build.py
"""
import bpy,bmesh,math,struct,json,hashlib
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[3];OUT=ROOT/'Assets/Resources/ActorSilhouettes/F1';SRC=Path(__file__).parent
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);assets=[]
def mesh(name,v,f):
 m=bpy.data.meshes.new(name);m.from_pydata([(x,-z,y) for x,y,z in v],[],f);m.update();bm=bmesh.new();bm.from_mesh(m);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(m);bm.free();o=bpy.data.objects.new(name,m);bpy.context.collection.objects.link(o);assets.append(o);return o
def profile(name,rows,n=8):
 v=[]
 for y,rx,rz,cx,cz in rows:
  for i in range(n):a=i*2*math.pi/n;v.append((cx+rx*math.cos(a),y,cz+rz*math.sin(a)))
 f=[]
 for j in range(len(rows)-1):
  for i in range(n):a=j*n+i;b=j*n+(i+1)%n;f.append((a,b,b+n,a+n))
 f.extend([tuple(reversed(range(n))),tuple((len(rows)-1)*n+i for i in range(n))]);return mesh(name,v,f)
def normalize(o,size=(1,1,1)):
 m=o.data
 for axis,extent in enumerate((size[0],size[2],size[1])):
  lo=min(v.co[axis] for v in m.vertices);hi=max(v.co[axis] for v in m.vertices)
  for v in m.vertices:v.co[axis]=((v.co[axis]-lo)/(hi-lo)-.5)*extent
 m.update();return o
# Long separated cloth ribbons have a scalloped/pointed hem and a shallow folded section.
v=[]
for y,width in [(-.5,.2),(-.38,.5),(.0,.43),(.5,.46)]:
 for x in [-width,0,width]:v.append((x,y,.18 if x else .5))
v+= [(x,y,z-.22) for x,y,z in v];f=[]
for j in range(3):
 for i in range(2):a=j*3+i;f.extend([(a,a+1,a+4,a+3),(a+12,a+15,a+16,a+13)])
for chain in [[0,3,6,9],[2,5,8,11],[0,1,2],[9,10,11]]:
 for a,b in zip(chain,chain[1:]):f.append((a,b,b+12,a+12))
normalize(mesh('RobePanel',v,f))
# Open-faced hood, outer and inner layers joined only at aperture/back rim.
v=[];rows=[(-.5,.45,.44),(-.1,.5,.5),(.25,.43,.44),(.5,.07,.10)];n=11
for inset in [0,.07]:
 for y,rx,rz in rows:
  for i in range(n):a=math.radians(-145+i*29);v.append((max(.035,rx-inset)*math.sin(a),y,-max(.025,rz-inset)*math.cos(a)))
f=[];layer=n*len(rows)
for k in range(2):
 for j in range(3):
  for i in range(n-1):a=k*layer+j*n+i;f.append((a,a+1,a+n+1,a+n))
for j in range(3):
 for i in [0,n-1]:a=j*n+i;f.append((a,a+n,a+n+layer,a+layer))
for j in [0,3]:
 for i in range(n-1):a=j*n+i;f.append((a,a+1,a+1+layer,a+layer))
normalize(mesh('Hood',v,f))
# Hollow quiver mouth with a raised lip; original cylinder y-height two retained.
rows=[(-1,.32,.32,0,0),(-.8,.43,.43,0,0),(.70,.46,.46,0,0),(.82,.5,.5,0,0),(1,.5,.5,0,0),(1,.36,.36,0,0),(.68,.35,.35,0,0)]
profile('Quiver',rows,12)
# Retain the arrow shaft in the same socket; paired tapering vanes at upper third.
o=profile('ArrowFeather',[(-1,.3,.3,0,0),(.25,.3,.3,0,0),(.45,1.75,.22,0,0),(.78,1.3,.22,0,0),(1,.3,.3,0,0)],6)
profile('Antler',[(-1,.36,.34,0,0),(-.55,.42,.42,-.04,.04),(-.12,.35,.31,.04,0),(.3,.22,.25,-.1,-.04),(.66,.16,.15,-.28,-.08),(1,.018,.018,-.5,-.08)],7)
# Crescent crown, open central void; 12 arc sections, extruded cross-section.
v=[];n=13
for z in [-.35,.35]:
 for radius in [.5,.33]:
  for i in range(n):a=math.radians(30+i*25);v.append((radius*math.cos(a),radius*math.sin(a),z))
f=[]
for i in range(n-1):
 f.extend([(i,i+1,n+i+1,n+i),(2*n+i,3*n+i,3*n+i+1,2*n+i+1),(i,2*n+i,2*n+i+1,i+1),(n+i,n+i+1,3*n+i+1,3*n+i)])
f.extend([(0,n,3*n,2*n),(n-1,3*n-1,4*n-1,2*n-1)]);normalize(mesh('SpiritCrown',v,f),(2.32,1.52,.6))
# Three flowing feather lobes joined into one rigid wing silhouette.
v=[];f=[]
for j in range(3):
 base=len(v);cx=(j-1)*.22;bottom=-1+j*.12;top=1-j*.22
 pts=[(cx-.16,bottom,0),(cx+.16,bottom,.0),(cx+.25,top-.35,.02),(cx+.09,top,.02),(cx-.10,top-.25,.02)]
 v+= [(x,y,z-.22) for x,y,z in pts]+[(x,y,z+.22) for x,y,z in pts]
 f.extend([tuple(base+i for i in reversed(range(5))),tuple(base+5+i for i in range(5))])
 for i in range(5):k=(i+1)%5;f.append((base+i,base+k,base+5+k,base+5+i))
normalize(mesh('SpiritWing',v,f),(1.7777777778,2,1))
# Angular canopy clusters; shoulders and limb origins remain untouched.
v=[];f=[]
for cx,cy,cz,rx,ry,rz in [(-.25,.02,0,.32,.32,.36),(.24,.04,0,.33,.34,.35),(0,.23,-.13,.33,.30,.30),(0,-.12,.24,.37,.23,.30)]:
 bm=bmesh.new();bmesh.ops.create_icosphere(bm,subdivisions=1,radius=1);bm.verts.ensure_lookup_table();bm.verts.index_update();base=len(v)
 v.extend([(cx+p.co.x*rx,cy+p.co.y*ry,cz+p.co.z*rz) for p in bm.verts]);f.extend([tuple(base+p.index for p in face.verts) for face in bm.faces]);bm.free()
normalize(mesh('Canopy',v,f))
normalize(profile('BarkLimb',[(-1,.30,.34,0,0),(-.65,.4,.4,.05,.05),(-.2,.31,.35,-.035,.02),(.3,.46,.40,.025,-.03),(.7,.40,.5,-.05,0),(1,.36,.36,0,0)],7),(1,2,1))
normalize(profile('BarkHand',[(-.5,.34,.48,0,.03),(-.30,.5,.5,0,.04),(.03,.43,.35,0,0),(.3,.32,.29,.05,0),(.5,.30,.29,0,0)],7))
normalize(profile('RootFoot',[(-.5,.5,.5,0,.05),(-.25,.48,.5,0,.04),(-.04,.33,.36,0,-.08),(.2,.28,.25,0,-.14),(.5,.30,.27,0,-.14)],7))
normalize(profile('BarkFace',[(-.5,.26,.3,0,0),(-.2,.38,.4,-.03,.02),(.15,.5,.45,0,0),(.4,.41,.42,.02,-.04),(.5,.30,.31,0,-.05)],9))
report={}
for o in assets:
 m=o.data;m.calc_loop_triangles();vs=[];inds=[]
 for t in m.loop_triangles:
  for vi in t.vertices:
   v=o.matrix_world@m.vertices[vi].co;n=(o.matrix_world.to_3x3().inverted().transposed()@t.normal).normalized();vs.append((v.x,v.z,-v.y,n.x,n.z,-n.y,v.x+.5,v.z+.5));inds.append(len(inds))
 assert len(inds)//3<=512
 blob=struct.pack('<III',0x45464D31,len(vs),len(inds))+b''.join(struct.pack('<8f',*v) for v in vs)+struct.pack('<%dI'%len(inds),*inds)
 path=OUT/(o.name+'.bytes');path.write_bytes(blob);meta=path.with_suffix('.bytes.meta')
 if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+hashlib.sha256(('emberfall.f1.silhouettes/'+o.name).encode()).hexdigest()[:32]+'\nTextScriptImporter:\n  externalObjects: {}\n')
 report[o.name]={'triangles':len(inds)//3,'bytes':len(blob),'sha256':hashlib.sha256(blob).hexdigest(),'bounds':[[min(v[k] for v in vs) for k in range(3)],[max(v[k] for v in vs) for k in range(3)]]}
 for p in m.polygons:p.use_smooth=False
for i,o in enumerate(assets):o.location=((i%4)*2.8,(i//4)*3,1.2)
bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'ActorSilhouettes-F1.blend'))
(SRC/'budget.json').write_text(json.dumps({'source':'Original Blender scripted geometry, no external inputs','assets':report,'total_bytes':sum(x['bytes'] for x in report.values()),'triangles':sum(x['triangles'] for x in report.values()),'added_materials':0,'added_textures':0,'added_renderers':0},indent=2)+'\n');print('F1 BUILT',len(assets),'modules',sum(x['bytes'] for x in report.values()),'bytes')

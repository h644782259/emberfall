"""Blender 4.3+: blender --background --python ArtSource/BlenderVfx/build.py"""
import bpy, math, json, hashlib, struct
from pathlib import Path
root=Path(__file__).resolve().parents[2]; out=root/'Assets/Resources/BlenderVfx'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
budget={}
def mesh(name,v,f):
 m=bpy.data.meshes.new(name);m.from_pydata(v,[],f);m.update();o=bpy.data.objects.new(name,m);bpy.context.collection.objects.link(o)
 # Explicit conversion Blender Z-up -> Unity Y-up; imported geometry has no object transforms.
 lines=['# Emberfall original authored geometry; BlenderVfx/build.py']
 lines += ['v %.7f %.7f %.7f'%(x,z,-y) for x,y,z in v]
 lines += ['vt %.7f %.7f'%(i/max(1,len(v)-1),i%4/3) for i in range(len(v))]
 tris=[]
 for face in f:
  for i in range(1,len(face)-1):tris.append((face[0],face[i],face[i+1]))
 lines += ['f '+' '.join('%d/%d'%(i+1,i+1) for i in t) for t in tris]
 p=root/'ArtSource/BlenderVfx'/(name+'.obj');p.write_text('\n'.join(lines)+'\n');budget[name]={'vertices':len(v),'triangles':len(tris),'materials':1,'textures':0,'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
 payload=bytearray(struct.pack('<Iii',0x45464D31,len(v),len(tris)*3))
 for i,vert in enumerate(m.vertices):
  x,y,z=vert.co;nx,ny,nz=vert.normal;payload.extend(struct.pack('<8f',x,z,-y,nx,nz,-ny,i/max(1,len(v)-1),i%4/3))
 for t in tris:payload.extend(struct.pack('<3i',*t))
 binary=out/(name+'.bytes');binary.write_bytes(payload);budget[name]['runtimeBytes']=len(payload);budget[name]['runtimeSha256']=hashlib.sha256(payload).hexdigest()
 mat=bpy.data.materials.get('Amber') or bpy.data.materials.new('Amber');mat.diffuse_color=(1,.45,.08,1);m.materials.append(mat)
 return o
v=[];f=[]
for i in range(25):
 t=i/24;a=math.radians(118)*t;w=.14*math.sin(math.pi*t)**.7+.002
 for r,z in [(1,0),(1-w*.35,.027*math.sin(math.pi*t)),(1-w,0),(1-w*.35,-.021*math.sin(math.pi*t))]:v.append((r*math.sin(a),r*math.cos(a),z))
for i in range(24):
 for k in range(4):f.append((i*4+k,(i+1)*4+k,(i+1)*4+(k+1)%4,i*4+(k+1)%4))
f.extend([(3,2,1,0),(96,97,98,99)])
mesh('Crescent',v,f)
core=[]
for x,y,z in v:
 r=math.sqrt(x*x+y*y);nr=1-(1-r)*.15;core.append((x/r*nr,y/r*nr,z*.4))
mesh('CrescentCore',core,f)
mesh('Shard',[(-1,-1,0),(1,-1,0),(1,1,0),(-1,1,0),(.15,0,1)],[(0,3,2,1),(0,1,4),(1,2,4),(2,3,4),(3,0,4)])
mesh('Fault',[(-.12,0,0),(-.07,0,.035),(.08,-.2,.02),(.13,-.2,.055),(-.03,-.5,0),(.02,-.5,.035)],[(0,2,3,1),(2,4,5,3)])
bpy.ops.wm.save_as_mainfile(filepath=str(root/'ArtSource/BlenderVfx/Vanguard-Skills.blend'))
(root/'ArtSource/BlenderVfx/budget.json').write_text(json.dumps({'source':'Original Emberfall geometry, adapted from approved render_vfx_comparison.py optimized()','assets':budget,'runtime':'No textures; one shared material; maximum 12 desktop / 8 mobile / 5 reduced renderers per cast; <= 2400 source triangles per cast; 1.15s visual tail'},indent=2)+'\n')
print('BLENDER_VFX_BUILD_OK',json.dumps(budget))

"""Four distinct skill visual families. Original geometry, no gameplay baked into assets."""
import bpy,math,struct,json,hashlib
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Assets/Resources/BlenderSkillIdentities';SRC=Path(__file__).resolve().parent
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
records={}
class Shape:
 def __init__(self):self.v=[];self.f=[];self.uv=[]
 def tube(self,points,width):
  start=len(self.v)
  for i,p in enumerate(points):
   p=Vector(p);direction=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(0,i-1)]);direction.normalize();u=direction.cross(Vector((0,0,1)) if abs(direction.z)<.9 else Vector((1,0,0))).normalized();v=direction.cross(u);r=width*(1-.72*i/(len(points)-1))
   for j in range(4):self.v.append(tuple(p+r*(u*math.cos(j*math.tau/4)+v*math.sin(j*math.tau/4))));self.uv.append((i/(len(points)-1),1 if j%2==0 else .3))
  for i in range(len(points)-1):
   for j in range(4):self.f.append((start+i*4+j,start+i*4+(j+1)%4,start+(i+1)*4+(j+1)%4,start+(i+1)*4+j))
  self.f.extend([(start+3,start+2,start+1,start),tuple(start+(len(points)-1)*4+j for j in range(4))])
 def diamond(self,x,y,z,w,h):
  n=len(self.v);self.v.extend([(x,y,z),(x-w,y+h*.5,z),(x,y+h*.5,z+w*.3),(x+w,y+h*.5,z),(x,y+h*.5,z-w*.3),(x,y+h,z)]);self.uv.extend([(0,0),(0,.3),(.5,1),(1,.3),(.5,1),(1,1)])
  for i in range(4):self.f.extend([(n,n+1+i,n+1+(i+1)%4),(n+5,n+1+(i+1)%4,n+1+i)])
 def export(self,name):
  m=bpy.data.meshes.new(name);m.from_pydata([(x,-z,y) for x,y,z in self.v],[],self.f);m.update();m.calc_loop_triangles();o=bpy.data.objects.new(name,m);bpy.context.collection.objects.link(o);ids=[i for t in m.loop_triangles for i in t.vertices];b=bytearray(struct.pack('<Iii',0x45464D31,len(self.v),len(ids)))
  for i,v in enumerate(m.vertices):x,y,z=v.co;nx,ny,nz=v.normal;u,w=self.uv[i];b.extend(struct.pack('<8f',x,z,-y,nx,nz,-ny,u,w))
  b.extend(struct.pack('<%di'%len(ids),*ids));p=OUT/(name+'.bytes');p.write_bytes(b);records[name]={'vertices':len(self.v),'triangles':len(ids)//3,'bytes':len(b),'sha256':hashlib.sha256(b).hexdigest(),'materials':1,'textures':0}
  meta=Path(str(p)+'.meta')
  if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+hashlib.md5(('emberfall-identities-v1/'+name).encode()).hexdigest()+'\n')
s=Shape()
# Three separated planar blade slices with closed diamond sections; the gaps remain visible.
for ring in range(3):
 start=len(s.v);radius=1-ring*.15
 for i in range(16):
  t=i/15;a=math.radians(-78+156*t+ring*11);width=(.035+.12*math.sin(t*math.pi))*(1-ring*.17)
  for k,(r,h) in enumerate([(radius,0),(radius-width*.35,.032),(radius-width,0),(radius-width*.35,-.027)]):s.v.append((math.sin(a)*r,h*(math.sin(t*math.pi)+.05)+ring*.06,math.cos(a)*r));s.uv.append((t,1 if k==0 else 0 if k==2 else .6))
 for i in range(15):
  for k in range(4):s.f.append((start+i*4+k,start+(i+1)*4+k,start+(i+1)*4+(k+1)%4,start+i*4+(k+1)%4))
 s.f.extend([tuple(start+j for j in reversed(range(4))),tuple(start+60+j for j in range(4))])
s.export('BladeSlices')
s=Shape();s.tube([(0,0,0),(-.08,.26,0),(.05,.48,0),(0,.72,0)],.055)
for sign in [-1,1]:
 s.tube([(0,.18,0),(sign*.26,.39,.025),(sign*.15,.58,.015),(sign*.38,.88,0)],.05)
 s.tube([(sign*.24,.4,.025),(sign*.42,.48,.08),(sign*.49,.66,.1)],.032)
s.export('ForkPulse')
s=Shape();s.diamond(0,.12,0,.19,.64)
for sign in [-1,1]:
 s.tube([(sign*.12,.04,0),(sign*.43,.19,0),(sign*.43,.65,0),(sign*.23,.81,0)],.045)
 s.diamond(sign*.43,.74,0,.075,.18)
s.tube([(-.22,.04,-.3),(0,.04,-.42),(.22,.04,-.3)],.035);s.export('ContractSigil')
s=Shape()
for arm in range(4):
 a=arm*math.tau/4;points=[]
 for i in range(9):t=i/8;r=.58*math.cos(t*math.pi/2);points.append((math.cos(a)*r,.08+math.sin(t*math.pi/2)*1.05,math.sin(a)*r))
 s.tube(points,.035)
s.diamond(0,1.10,0,.11,.26);s.export('ProtectionCage')
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'Four-Skill-Identities.blend'));(SRC/'budget.json').write_text(json.dumps(records,indent=2)+'\n');print('IDENTITIES_BUILD_OK',json.dumps(records))

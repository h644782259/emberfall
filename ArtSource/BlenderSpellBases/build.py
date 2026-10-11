"""Original low-poly spell volumes. Blender owns editable meshes; Unity owns motion/cover/gameplay."""
import bpy, math, struct, json, hashlib
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Assets/Resources/BlenderSpellBases';SRC=Path(__file__).resolve().parent
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
records={}
class Shape:
 def __init__(self):self.v=[];self.f=[];self.uv=[]
 def beam(self,a,b,width,sides=4,endwidth=None):
  a,b=Vector(a),Vector(b);d=(b-a).normalized();u=d.cross(Vector((0,0,1)) if abs(d.z)<.9 else Vector((1,0,0))).normalized();w=d.cross(u);start=len(self.v)
  for center,r in [(a,width),(b,width if endwidth is None else endwidth)]:
   for j in range(sides):self.v.append(tuple(center+r*(u*math.cos(j*math.tau/sides)+w*math.sin(j*math.tau/sides))))
  for j in range(sides):self.f.append((start+j,start+(j+1)%sides,start+sides+(j+1)%sides,start+sides+j))
  self.f.extend([tuple(start+j for j in reversed(range(sides))),tuple(start+sides+j for j in range(sides))])
 def export(self,name):
  if name=="Rupture":self.v=[(x,y,max(-.77246,z)) for x,y,z in self.v]
  # Geometry authored in Unity coordinates then converted only for the editable Blender scene.
  m=bpy.data.meshes.new(name);m.from_pydata([(x,-z,y) for x,y,z in self.v],[],self.f);m.update();m.calc_loop_triangles()
  o=bpy.data.objects.new(name,m);bpy.context.collection.objects.link(o)
  indices=[i for t in m.loop_triangles for i in t.vertices];data=bytearray(struct.pack('<Iii',0x45464D31,len(self.v),len(indices)))
  for i,vert in enumerate(m.vertices):
   x,y,z=vert.co;nx,ny,nz=vert.normal;u,v=self.uv[i] if self.uv else ((x+1)*.5,max(0,min(1,z)));data.extend(struct.pack('<8f',x,z,-y,nx,nz,-ny,u,v))
  data.extend(struct.pack('<%di'%len(indices),*indices));p=OUT/(name+'.bytes');p.write_bytes(data)
  records[name]={'vertices':len(self.v),'triangles':len(indices)//3,'materials':1,'textures':0,'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest()}
  return o
s=Shape()
for i in range(25):
 t=i/24;a=math.radians(-108+216*t);taper=.015+math.sin(t*math.pi)**.7
 for j in range(4):
  r=1.04 if j==0 else 1-.37*taper if j==2 else 1-.15*taper;y=(.09 if j==1 else -.075 if j==3 else 0)*taper+.055*math.sin(a);s.v.append((math.sin(a)*r,y,math.cos(a)*r));s.uv.append((t,1 if j==0 else 0 if j==2 else .6))
for i in range(24):
 for j in range(4):s.f.append((i*4+j,(i+1)*4+j,(i+1)*4+(j+1)%4,i*4+(j+1)%4))
s.f.extend([(3,2,1,0),(96,97,98,99)]);s.export('Crescent')
s=Shape()
for y,r,x,z in [(0,.04,0,0),(.18,.48,0,0),(.65,.26,.06,.04),(1,.005,.12,.07)]:
 for i in range(6):s.v.append((x+r*math.cos(i*math.tau/6),y,z+r*math.sin(i*math.tau/6)));s.uv.append((i/6,1 if y==1 else .55 if y==.65 else 0))
for ring in range(3):
 for i in range(6):s.f.append((ring*6+i,ring*6+(i+1)%6,(ring+1)*6+(i+1)%6,(ring+1)*6+i))
s.f.extend([tuple(reversed(range(6))),tuple(range(18,24))]);s.export('Crystal')
s=Shape()
for i in range(9):
 t=i/8;r=.006+.46*math.sin(t*math.pi)**.65*(1-.5*t)
 for j in range(8):a=j*math.tau/8;s.v.append((math.cos(a)*r+.26*t*t,t,math.sin(a)*r+.11*math.sin(t*5)*t));s.uv.append((j/8,t))
for i in range(8):
 for j in range(8):s.f.append((i*8+j,i*8+(j+1)%8,(i+1)*8+(j+1)%8,(i+1)*8+j))
s.f.extend([tuple(reversed(range(8))),tuple(range(64,72))]);s.export('Flame')
s=Shape();s.v=[(0,0,0)]
for y,width in [(.2,.20),(.82,.22),(.94,.17)]:s.v.extend([(-width,y,0),(0,y,.075),(width,y,0),(0,y,-.075)])
for j in range(4):s.f.append((0,1+j,1+(j+1)%4))
for ring in range(2):
 for j in range(4):s.f.append((1+ring*4+j,1+ring*4+(j+1)%4,5+ring*4+(j+1)%4,5+ring*4+j))
s.f.append((9,10,11,12));s.beam((-.42,.98,0),(.42,.98,0),.065);s.beam((0,1.02,0),(0,1.3,0),.055);s.export('Sword')
s=Shape();pts=[(0,0,0),(-.16,.24,0),(.13,.46,0),(-.2,.73,0),(.1,1.02,0),(0,1.4,0)]
for i in range(5):s.beam(pts[i],pts[i+1],.038,endwidth=.018)
s.beam(pts[3],(-.52,.58,.12),.024,endwidth=.016);s.beam((-.52,.58,.12),(-.42,.3,.2),.018,endwidth=.004);s.beam(pts[2],(.46,.31,-.12),.024,endwidth=.004);s.export('Lightning')
s=Shape();s.beam((-.28,0,0),(.28,.38,0),.032,endwidth=.026);s.beam((.28,.38,0),(.28,.7,.18),.028,endwidth=.008);s.export('ArcaneShard')
s=Shape()
for arm in range(3):
 a=arm*math.tau/3;dx,dz=math.cos(a),math.sin(a);last=(0,.025,0)
 for i in range(1,4):
  x=dx*i*.28-dz*(-.09 if i%2==0 else .09);z=dz*i*.28+dx*(-.09 if i%2==0 else .09);n=(x,.025,z);s.beam(last,n,.027,endwidth=.012 if i==3 else .027);last=n
s.export('Rupture')
s=Shape();s.beam((0,.08,0),(0,1.7,0),.022)
# Filled diamond arrowhead and feather fins remain in the old envelope.
s.v.extend([(-.13,.25,0),(0,0,0),(.13,.25,0),(0,.22,.024),(0,.22,-.024)]);n=len(s.v)-5;s.f.extend([(n,n+1,n+3),(n+1,n+2,n+3),(n,n+4,n+1),(n+1,n+4,n+2),(n,n+3,n+2,n+4)])
s.beam((0,1.38,0),(-.14,1.64,0),.018);s.beam((0,1.38,0),(.14,1.64,0),.018);s.export('Arrow')
s=Shape();last=(0,.04,0)
for i in range(1,8):
 nx,nz=math.sin(i*.9)*.55,i*.25;n=(nx,.04,nz);s.beam(last,n,.035,endwidth=.027);last=n
 if i%2:s.beam(n,(nx+(.3 if i%4==1 else -.3),.14,nz+.18),.024,endwidth=.006)
s.export('Vine')
# Dedicated primary silhouettes: repeated ornaments and beam/thrust meshes remain unchanged.
s=Shape()
for y,r,x,z in [(0,.05,0,0),(.18,.29,0,0),(.7,.19,.035,.02),(1,.006,.08,.04)]:
 for j in range(6):s.v.append((x+r*math.cos(j*math.tau/6),y,z+r*math.sin(j*math.tau/6)));s.uv.append((j/6,y))
for ring in range(3):
 for j in range(6):s.f.append((ring*6+j,ring*6+(j+1)%6,(ring+1)*6+(j+1)%6,(ring+1)*6+j))
s.f.extend([tuple(reversed(range(6))),tuple(range(18,24))])
for j in range(4):
 a=j*math.tau/4+.35;cx,cz=math.cos(a)*.28,math.sin(a)*.28;n=len(s.v)
 for k in range(4):s.v.append((cx+math.cos(k*math.tau/4)*.12,.055,cz+math.sin(k*math.tau/4)*.12));s.uv.append((k/4,0))
 s.v.append((cx*1.16,.43+(j%2)*.13,cz*1.16));s.uv.append((.5,1))
 s.f.append((n+3,n+2,n+1,n))
 for k in range(4):s.f.append((n+k,n+(k+1)%4,n+4))
s.export('IcePrimary')
s=Shape()
for offset,height,lean,width in [(0,1,.22,.19),(-.2,.73,-.105,.105),(.21,.64,.115,.10)]:
 n=len(s.v)
 for i in range(6):
  t=i/5;r=.004+width*math.sin(t*math.pi)**.7
  for j in range(6):
   a=j*math.tau/6;s.v.append((offset+lean*t*t+r*math.cos(a),height*t,.02+math.sin(t*4)*.035+r*math.sin(a)));s.uv.append((j/6,t))
 for i in range(5):
  for j in range(6):s.f.append((n+i*6+j,n+i*6+(j+1)%6,n+(i+1)*6+(j+1)%6,n+(i+1)*6+j))
 s.f.extend([tuple(n+j for j in reversed(range(6))),tuple(n+30+j for j in range(6))])
s.export('FirePrimary')
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'Reusable-Spell-Volumes.blend'))
(SRC/'budget.json').write_text(json.dumps(records,indent=2)+'\n');print('SPELL_BASES_BUILD_OK',json.dumps(records))

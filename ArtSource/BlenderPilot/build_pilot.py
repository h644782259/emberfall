"""Original Emberfall art pilot. Blender 4.3+, no external assets or services.
Run: blender -b --factory-startup --python ArtSource/BlenderPilot/build_pilot.py
"""
import bpy, math, json, os, random
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/Resources/BlenderPilot'; SOURCE=ROOT/'ArtSource/BlenderPilot'
PREVIEW=Path(os.environ.get('PILOT_PREVIEW','/workspace/scratch/blender-pilot-preview'))
for p in (OUT,SOURCE,PREVIEW): p.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
random.seed(7081)
COLORS=[('Obsidian',(.055,.095,.14)),('Steel',(.28,.43,.52)),('Edge',(.69,.82,.83)),('Brass',(.64,.35,.105)),('Amber',(.95,.32,.045)),('Cloth',(.13,.28,.31)),('Linen',(.65,.55,.35)),('Leather',(.19,.09,.047)),('Wood',(.29,.13,.056)),('Endgrain',(.48,.25,.09)),('Stone',(.17,.21,.24)),('Ember',(.95,.095,.015))]
# One authored 1024 atlas. Subtle repeatable pigment/grain, padded swatch UVs.
img=bpy.data.images.new('EmberfallPilot_Atlas',width=1024,height=1024)
pixels=[]
for y in range(1024):
 for x in range(1024):
  idx=min(11,(y//256)*4+x//256); c=COLORS[idx][1]
  grain=1+.028*math.sin(x*.37+y*.15)+.018*math.sin(x*.081-y*.24)
  pixels.extend([min(1,v*grain) for v in c]+[1])
img.pixels.foreach_set(pixels); img.filepath_raw=str(OUT/'EmberfallPilot_Atlas.png'); img.file_format='PNG'; img.save()
mat=bpy.data.materials.new('Pilot_Atlas_Standard'); mat.use_nodes=True
bs=mat.node_tree.nodes.get('Principled BSDF'); bs.inputs['Roughness'].default_value=.62
tex=mat.node_tree.nodes.new('ShaderNodeTexImage'); tex.image=img; mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
# Standard-compatible metallic (R) / smoothness (A) mask, linear sampling.
mask=bpy.data.images.new('EmberfallPilot_MetallicSmoothness',width=1024,height=1024,alpha=True)
mask.colorspace_settings.name='Non-Color'; mp=[]
for y in range(1024):
 for x in range(1024):
  idx=min(11,(y//256)*4+x//256); metal=.78 if idx in (1,2,3) else .05; smooth=.60 if idx in (1,2,3) else .18
  mp.extend([metal,metal,metal,smooth])
mask.pixels.foreach_set(mp);mask.filepath_raw=str(OUT/'EmberfallPilot_MetallicSmoothness.png');mask.file_format='PNG';mask.save()
mt=mat.node_tree.nodes.new('ShaderNodeTexImage');mt.image=mask
mat.node_tree.links.new(mt.outputs['Color'],bs.inputs['Metallic'])
invert=mat.node_tree.nodes.new('ShaderNodeMath');invert.operation='SUBTRACT';invert.inputs[0].default_value=1
mat.node_tree.links.new(mt.outputs['Alpha'],invert.inputs[1]);mat.node_tree.links.new(invert.outputs[0],bs.inputs['Roughness'])
assets={}; current=[]
def surface(o,swatch):
 o.data.materials.clear(); o.data.materials.append(mat)
 if not o.data.uv_layers: o.data.uv_layers.new(name='UVMap')
 # face-local projection maps each face to its palette tile, with generous padding
 for poly in o.data.polygons:
  n=poly.normal; drop=max(range(3),key=lambda i:abs(n[i])); axes=[i for i in range(3) if i!=drop]
  coords=[o.data.vertices[o.data.loops[k].vertex_index].co for k in poly.loop_indices]
  lo=[min(c[a] for c in coords) for a in axes]; hi=[max(c[a] for c in coords) for a in axes]
  for k,c in zip(poly.loop_indices,coords):
   uv=[.08+.84*(c[a]-l)/max(.00001,h-l) for a,l,h in zip(axes,lo,hi)]
   o.data.uv_layers.active.data[k].uv=((swatch%4+uv[0])/4,(swatch//4+uv[1])/4)
 current.append(o); return o

def mesh(name,verts,faces,swatch):
 m=bpy.data.meshes.new(name); m.from_pydata(verts,[],faces); m.update(); o=bpy.data.objects.new(name,m); bpy.context.collection.objects.link(o); return surface(o,swatch)
def bevel(o,width=.02,segments=1):
 mod=o.modifiers.new('Authored edge bevel','BEVEL'); mod.width=width; mod.segments=segments
 bpy.context.view_layer.objects.active=o; bpy.ops.object.modifier_apply(modifier=mod.name); return o
def box(name,pos,size,s=0,b=.02):
 x,y,z=[v/2 for v in size]
 v=[(-x,-y,-z),(x,-y,-z),(x,y,-z),(-x,y,-z),(-x,-y,z),(x,-y,z),(x,y,z),(-x,y,z)]
 o=mesh(name,v,[(0,3,2,1),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)],s); o.location=pos
 if b: bevel(o,b)
 return o
def rod(name,a,b,r,s=3,n=8,r2=None):
 a,b=Vector(a),Vector(b); d=b-a; q=d.to_track_quat('Z','Y'); v=[]
 for z,rr in [(0,r),(d.length,r if r2 is None else r2)]:
  v.extend([a+q@Vector((rr*math.cos(i*2*math.pi/n),rr*math.sin(i*2*math.pi/n),z)) for i in range(n)])
 faces=[tuple(range(n-1,-1,-1)),tuple(range(n,n*2))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
 return mesh(name,v,faces,s)
def outline(name,points,depth,s):
 # x/z sculpted profile, front faces toward negative Y
 n=len(points); verts=[(x,y,z) for y in [-depth/2,depth/2] for x,z in points]
 return mesh(name,verts,[tuple(range(n-1,-1,-1)),tuple(range(n,n*2))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)],s)
def begin():
 global current; current=[]
def finish(name):
 objs=current[:]; assets[name]=objs
 for o in objs: o['asset']=name
 return objs

def sword():
 begin()
 # The blade is a diamond-section tapered mesh, not a stretched cube.
 rings=[(.20,.095,.022),(.29,.115,.025),(.88,.082,.02),(1.12,.057,.013),(1.30,0,0)]
 verts=[]
 for z,w,t in rings: verts +=[(-w,0,z),(0,-t,z),(w,0,z),(0,t,z)]
 faces=[]
 for k in range(len(rings)-1):
  for i in range(4): faces.append((k*4+i,k*4+(i+1)%4,(k+1)*4+(i+1)%4,(k+1)*4+i))
 blade=mesh('Sword_ForgedDiamondBlade',verts,faces,2)
 # recessed-looking central fuller split around inset amber core
 for side in [-1,1]:
  o=outline('Sword_DarkFuller', [(-.018,.34),(.018,.34),(.012,1.01),(0,1.08),(-.012,1.01)],.002,0); o.location.y=side*.024
 guard=outline('Sword_SweptStarGuard',[(-.25,.115),(-.24,.20),(-.12,.225),(0,.18),(.12,.225),(.24,.20),(.25,.115),(.18,.135),(.09,.17),(0,.145),(-.09,.17),(-.18,.135)],.07,3); bevel(guard,.009)
 core=outline('Sword_AmberCore',[(0,.255),(.045,.19),(0,.145),(-.045,.19)],.085,4)
 rod('Sword_LeatherGrip',(0,0,-.135),(0,0,.13),.036,7,8)
 for i in range(7): rod('Sword_Wrap',(-.03,-.034,-.115+i*.035),(.03,-.034,-.091+i*.035),.006,6,5)
 pom=outline('Sword_Pommel',[(-.052,-.13),(0,-.20),(.052,-.13),(0,-.10)],.062,3); bevel(pom,.007)
 for name,z in [('Grip',0),('Guard',.16),('BladeRoot',.20),('Tip',1.30),('Pommel',-.17),('Emission',.23)]:
  o=bpy.data.objects.new('Anchor_'+name,None); bpy.context.collection.objects.link(o); o.location=(0,0,z); current.append(o)
 return finish('StarcoreSword')

def tent():
 begin()
 # Sagged canopy panels with an asymmetric open entrance and stitched bindings.
 for side in [-1,1]:
  verts=[]
  for j in range(9):
   y=-1.04+j*.26
   for i in range(7):
    u=i/6; x=side*(.065+1.0*u); z=1.68*(1-u)+.19*u-.13*math.sin(math.pi*u)+.045*math.cos(y*3)*(u*.6+.4)
    verts.append((x,y,z))
  faces=[(j*7+i,j*7+i+1,(j+1)*7+i+1,(j+1)*7+i) for j in range(8) for i in range(6)]
  o=mesh('Tent_TensionedCanvas',verts,faces,5)
  sol=o.modifiers.new('Canvas thickness','SOLIDIFY'); sol.thickness=.013; bpy.context.view_layer.objects.active=o; bpy.ops.object.modifier_apply(modifier=sol.name)
  for y in [-1.04,1.04]:
   for i in range(6): rod('Tent_BoundHem',verts[(0 if y<0 else 8)*7+i],verts[(0 if y<0 else 8)*7+i+1],.017,6,5)
  for j in range(16):
   y=-.98+j*.125; x=side*.60; z=.69+.035*math.cos(y*3)
   rod('Tent_Stitch',(x-.015,y,z),(x+.015,y+.025,z),.0045,6,4)
 # partially folded entrance; intentional dark interior
 for side in [-1,1]:
  mesh('Tent_FoldedDoor',[(side*.08,-1.055,1.66),(side*.98,-1.055,.20),(side*.67,-1.11,.26),(side*.41,-1.08,.77)],[(0,1,2,3)],6)
  rod('Tent_DoorTie',(side*.60,-1.125,.43),(side*.72,-1.125,.55),.022,3,6)
 for y in [-1.1,1.1]:
  rod('Tent_RidgePost',(0,y,0),(0,y,1.81),.04,8)
  rod('Tent_Finial',(0,y,1.79),(0,y,1.94),.062,3,6,r2=0)
 rod('Tent_RidgePole',(0,-1.16,1.71),(0,1.16,1.71),.04,8)
 for x in [-1,1]:
  for y in [-.85,.85]:
   rod('Tent_Guyline',(x*.88,y,.41),(x*1.32,y*1.35,.05),.009,6,5)
   rod('Tent_Peg',(x*1.32,y*1.35,0),(x*1.32-.035,y*1.35,.16),.02,3,6)
 badge=outline('Tent_StarBanner',[(0,0),(.11,-.13),(0,-.28),(-.11,-.13)],.014,3); badge.location=(0,-1.09,1.4)
 return finish('WayfarerTent')

def crate():
 begin()
 for i in range(5):
  x=(i-2)*.166
  box('Crate_FrontPlank',(x,-.393,.5),(.158,.055,.93),8,0)
  box('Crate_BackPlank',(x,.393,.5),(.158,.055,.93),8,0)
  box('Crate_LidPlank',(x,0,1.00),(.158,.80,.058),9,.012)
 for x in [-.398,.398]:
  for i in range(4): box('Crate_SidePlank',(x,(i-1.5)*.19,.5),(.055,.18,.93),8,.01)
 for z in [.16,.82]:
  box('Crate_FrontStrap',(0,-.427,z),(.86,.035,.065),0,.008)
  box('Crate_BackStrap',(0,.427,z),(.86,.035,.065),0,.008)
  for x in [-.431,.431]: box('Crate_SideStrap',(x,0,z),(.035,.85,.065),0,.006)
 for x in [-.31,.31]:
  for z in [.16,.82]: rod('Crate_Rivet',(x,-.447,z),(x,-.458,z),.019,3,6)
 # latch and inset stamped star give a readable functional front
 box('Crate_Latch',(0,-.45,.85),(.11,.04,.20),3,.01)
 o=outline('Crate_StarSeal',[(0,.68),(.08,.56),(.04,.47),(0,.43),(-.04,.47),(-.08,.56)],.015,3); o.location.y=-.435
 for x in [-.447,.447]:
  rod('Crate_Handle',(x,-.13,.58),(x,.13,.58),.018,3,6)
 return finish('SupplyCrate')

def fire():
 begin()
 for i in range(10):
  a=i*math.tau/10; x,y=.55*math.cos(a),.55*math.sin(a)
  o=box('Fire_RingStone',(x,y,.12),(.27,.21,.23),10,.065); o.rotation_euler.z=a+.13
 for i in range(4):
  a=i*math.pi/2+.2; a1=(-.40*math.cos(a),-.40*math.sin(a),.15+i*.018); b1=(.40*math.cos(a),.40*math.sin(a),.15+i*.018)
  rod('Fire_CharredLog',a1,b1,.08,7,7); d=(Vector(b1)-Vector(a1)).normalized()*.006
  rod('Fire_Endgrain',Vector(a1)-d,Vector(a1),.067,9,7)
 for i in range(7):
  a=i*2.4; x,y=.20*math.cos(a),.20*math.sin(a); h=.45+.15*math.sin(i*1.7)
  # angular curled flames with offset centres, not stacked cones
  verts=[]
  for z,r,dx,dy in [(0,.105,0,0),(h*.4,.09,.04,0),(h*.78,.04,-.015,.02),(h,0,.06,.03)]:
   verts +=[(x+dx+r*math.cos(k*math.tau/5),y+dy+r*math.sin(k*math.tau/5),.24+z) for k in range(5)]
  faces=[(j*5+k,j*5+(k+1)%5,(j+1)*5+(k+1)%5,(j+1)*5+k) for j in range(3) for k in range(5)]
  mesh('Fire_FoldedFlame',verts,faces,4 if i%2 else 11)
 return finish('StarEmberCampfire')

sword(); tent(); crate(); fire()
# Apply transforms, ensure UV valid after bevel, join each asset to one mesh.
for name,objs in assets.items():
 meshes=[o for o in objs if o.type=='MESH']; empties=[o for o in objs if o.type=='EMPTY']; bpy.ops.object.select_all(action='DESELECT')
 for o in meshes:o.select_set(True)
 bpy.context.view_layer.objects.active=meshes[0]; bpy.ops.object.join(); meshes[0].name=name
 assets[name]=[meshes[0]]+empties
 bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 # export before display layout. Blender Z up -> Unity +Y, unit metres.
 bpy.ops.object.select_all(action='DESELECT')
 for o in assets[name]:o.select_set(True)
 bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,add_leaf_bones=False,bake_anim=False,path_mode='STRIP')

# Native review stage, excluded from exports; single elevated orthographic camera.
positions={'StarcoreSword':(-2.55,0,.15),'WayfarerTent':(.1,.5,0),'SupplyCrate':(2.2,-.4,0),'StarEmberCampfire':(-1.5,-2.1,0)}
for name,objs in assets.items():
 for o in objs:o.location+=Vector(positions[name])
# Sword float on a dark display plinth (not part of runtime asset).
begin(); box('Preview_SwordPlinth',(-2.55,0,.06),(.85,.75,.12),0,.025)
box('Preview_Ground',(0,0,-.10),(200,200,.15),0,.01)
world=bpy.context.scene.world; world.use_nodes=True; world.node_tree.nodes['Background'].inputs[0].default_value=(.10,.15,.21,1); world.node_tree.nodes['Background'].inputs[1].default_value=.5
for name,loc,power,size,col in [('Key',(-3,-4,7),1000,6,(1,.83,.64)),('Fill',(4,-2,5),700,5,(.56,.78,1)),('Rim',(0,4,6),1300,4,(1,.54,.22))]:
 d=bpy.data.lights.new(name,'AREA'); d.energy=power; d.shape='DISK'; d.size=size; d.color=col; o=bpy.data.objects.new(name,d); bpy.context.collection.objects.link(o); o.location=loc; o.rotation_euler=(Vector((0,0,.5))-o.location).to_track_quat('-Z','Y').to_euler()
d=bpy.data.cameras.new('ReviewCamera'); cam=bpy.data.objects.new('ReviewCamera',d); bpy.context.collection.objects.link(cam); cam.location=(6,-10,8); cam.rotation_euler=(Vector((0,-.35,.65))-cam.location).to_track_quat('-Z','Y').to_euler(); d.type='ORTHO'; d.ortho_scale=7.7
sc=bpy.context.scene; sc.camera=cam; sc.render.engine='CYCLES'; sc.cycles.device='CPU'; sc.cycles.samples=48; sc.cycles.use_denoising=False; sc.render.resolution_x=1600; sc.render.resolution_y=1200; sc.render.resolution_percentage=100
sc.view_settings.view_transform='AgX'; sc.render.image_settings.file_format='PNG'; sc.render.filepath=str(PREVIEW/'Emberfall-Pilot-Props.png')
report={}
for name,objs in assets.items():
 m=next(o for o in objs if o.type=='MESH'); m.data.calc_loop_triangles(); report[name]={'triangles':len(m.data.loop_triangles),'vertices':len(m.data.vertices),'materials':len(m.data.materials),'dimensions_blender_xyz_m':list(m.dimensions),'uv_layers':len(m.data.uv_layers),'fbx_bytes':(OUT/(name+'.fbx')).stat().st_size}
(SOURCE/'prop-budget.json').write_text(json.dumps(report,indent=2))
img.pack();mask.pack();bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Emberfall-Pilot-Props.blend'))
bpy.ops.render.render(write_still=True)
print('PILOT_BUDGET',json.dumps(report))

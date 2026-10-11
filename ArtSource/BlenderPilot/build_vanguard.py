"""Original skinned Vanguard pilot; deterministic authoring, Blender 4.3."""
from pathlib import Path
from mathutils import Matrix
# Reuse authored mesh/UV functions; do not execute prop build/render phase.
exec((Path(__file__).parent/'build_pilot.py').read_text().split('sword(); tent(); crate(); fire()')[0])
begin()
weights={}
blended_weights={}
def bind(o,bone): weights[o.name]=bone; return o
def shell(name,rings,s,bone,n=8):
 verts=[]
 for z,rx,ry,cx,cy in rings:
  verts +=[(cx+rx*math.cos((i+.5)*math.tau/n),cy+ry*math.sin((i+.5)*math.tau/n),z) for i in range(n)]
 faces=[tuple(range(n-1,-1,-1)),tuple(range((len(rings)-1)*n,len(rings)*n))]+[(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(len(rings)-1) for i in range(n)]
 return bind(mesh(name,verts,faces,s),bone)
def plate(name,points,depth,s,pos,bone):
 o=outline(name,points,depth,s); o.location=pos; bevel(o,.012); return bind(o,bone)
def continuous_sleeve(suf,side):
 # Closed continuous cloth from torso under the shoulder armor to the glove.
 # Intermediate rings share vertices across joints; at most two existing bones
 # influence each vertex, so bending cannot split two separately capped rods.
 S='Spine';U='UpperArm.'+suf;F='Forearm.'+suf;H='Hand.'+suf
 rings=[((side*.28,0,1.60),.095,((S,.8),(U,.2))),
        ((side*.37,0,1.62),.115,((S,.25),(U,.75))),
        ((side*.43,-.004,1.56),.119,((U,1),)),
        ((side*.50,-.011,1.42),.099,((U,1),)),
        ((side*.535,-.014,1.355),.094,((U,.65),(F,.35))),
        ((side*.548,-.023,1.31),.090,((U,.2),(F,.8))),
        ((side*.557,-.047,1.20),.087,((F,1),)),
        ((side*.57,-.075,1.085),.080,((F,.35),(H,.65)))]
 n=10;verts=[];groups=[]
 for j,(center,radius,influences) in enumerate(rings):
  before=Vector(rings[max(0,j-1)][0]);after=Vector(rings[min(len(rings)-1,j+1)][0]);q=(after-before).to_track_quat('Z','Y')
  for k in range(n):
   verts.append(Vector(center)+q@Vector((radius*math.cos(k*math.tau/n),radius*math.sin(k*math.tau/n),0)));groups.append(influences)
 faces=[tuple(range(n-1,-1,-1)),tuple(range((len(rings)-1)*n,len(rings)*n))]
 faces += [(j*n+k,j*n+(k+1)%n,(j+1)*n+(k+1)%n,(j+1)*n+k) for j in range(len(rings)-1) for k in range(n)]
 o=bind(mesh('Vanguard_ContinuousSleeve_'+suf,verts,faces,5),U);blended_weights[o.name]=groups
 for f in o.data.polygons:f.use_smooth=len(f.vertices)==4
 return o

def folded_mantle():
 # Closed thin volume with three long folds and bowed side profile. Shape is
 # authored rather than simulated; the existing Spine/Mantle hinge is retained.
 cols=9;rows=[0,.16,.35,.58,.80,1];verts=[]
 for layer in [-1,1]:
  for t in rows:
   width=.28+.048*math.sin(t*math.pi*.75)
   for c in range(cols):
    u=-1+2*c/(cols-1);fold=(.010+.032*t)*math.cos(3*math.pi*u)*(1-.25*abs(u))
    x=u*width;y=.235+.12*t+.026*math.sin(math.pi*t)+fold+layer*.009
    z=1.67-.68*t+.083*math.exp(-u*u*22)*t**6+.022*u*u*t
    verts.append((x,y,z))
 size=cols*len(rows);faces=[]
 for j in range(len(rows)-1):
  for c in range(cols-1):
   a=j*cols+c;b=a+1;d=a+cols;e=d+1
   faces.extend([(a,d,e,b),(a+size,b+size,e+size,d+size)])
 perimeter=list(range(cols))+[j*cols+cols-1 for j in range(1,len(rows))]+list(range(size-2,size-cols-1,-1))+[j*cols for j in range(len(rows)-2,0,-1)]
 for a,b in zip(perimeter,perimeter[1:]+perimeter[:1]):faces.append((a,b,b+size,a+size))
 o=bind(mesh('Vanguard_BackMantle',verts,faces,5),'Mantle')
 # Preserve fold ridges in the silhouette; shared normals soften only the broad cloth panels.
 for f in o.data.polygons[:2*(len(rows)-1)*(cols-1)]:f.use_smooth=True
 return o

# Tailored silhouette: broad left pauldron, tapered chest, split tabard, articulated greaves.
shell('Vanguard_PaddedTorso',[(1.04,.25,.16,0,0),(1.31,.31,.20,0,0),(1.64,.40,.22,0,0),(1.75,.30,.17,0,0)],5,'Spine')
shell('Vanguard_Breastplate',[(1.19,.235,.173,0,-.035),(1.43,.34,.24,0,-.012),(1.66,.37,.23,0,0),(1.73,.27,.17,0,0)],1,'Spine')
plate('Vanguard_ChestKeel',[(-.17,0),(0,-.23),(.17,0),(.10,.11),(-.10,.11)],.043,3,(0,-.246,1.54),'Spine')
plate('Vanguard_StarHeart',[(0,.065),(.047,0),(0,-.085),(-.047,0)],.061,4,(0,-.278,1.52),'Spine')
shell('Vanguard_WaistBelt',[(1.08,.265,.18,0,0),(1.18,.27,.18,0,0)],7,'Pelvis')
bind(box('Vanguard_BeltBuckle',(0,-.187,1.13),(.135,.043,.105),3,.015),'Pelvis')
for side in [-1,1]:
 suf='L' if side<0 else 'R'; x=side*.19
 shell('Vanguard_Thigh_'+suf,[(.62,.105,.12,x,0),(.90,.14,.14,x,0),(1.10,.145,.145,x,0)],7,'Thigh.'+suf)
 shell('Vanguard_Greave_'+suf,[(.16,.115,.12,x,0),(.26,.10,.105,x,0),(.53,.125,.125,x,0),(.64,.10,.115,x,0)],1,'Shin.'+suf)
 plate('Vanguard_KneeShield_'+suf,[(-.115,.02),(0,-.14),(.115,.02),(.085,.13),(-.085,.13)],.10,3,(x,-.12,.62),'Shin.'+suf)
 # chamfered toe profile, heel distinct from toe; planted sole at z0
 shell('Vanguard_Boot_'+suf,[(.035,.12,.21,x,-.09),(.14,.13,.24,x,-.10),(.24,.11,.14,x,0)],0,'Foot.'+suf)
 plate('Vanguard_ToeCap_'+suf,[(-.10,0),(.10,0),(.09,.05),(-.09,.05)],.15,1,(x,-.275,.10),'Foot.'+suf)
 # arm rests in a shallow A pose; sword in right hand
 shoulder=(side*.39,0,1.64); elbow=(side*.54,-.015,1.34); hand=(side*.57,-.075,1.08)
 continuous_sleeve(suf,side)
 bind(rod('Vanguard_Vambrace_'+suf,(side*.545,-.021,1.31),hand,.113,1,8,r2=.09),'Forearm.'+suf)
 shell('Vanguard_Glove_'+suf,[(.99,.09,.09,hand[0],hand[1]),(1.15,.10,.10,hand[0],hand[1])],7,'Hand.'+suf)
 # overlaid segmented asymmetric pauldron, readable from top camera
 for i in range(3 if side<0 else 2):
  z=1.72-i*.065; w=.19-i*.018
  shell('Vanguard_Pauldron_'+suf+str(i),[(z-.10,w,.20,side*(.40+i*.035),0),(z,w*.94,.195,side*(.40+i*.035),0),(z+.045,w*.65,.15,side*(.40+i*.035),0)],3 if i==0 else 1,'UpperArm.'+suf)
 # split cloth panel with geometry crease and pointed hem; two cloth bones
 plate('Vanguard_Tabard_'+suf,[(-.125,0),(.12,0),(.14,-.38),(.01,-.50),(-.13,-.39)],.024,5,(side*.145,-.17,1.10),'Tabard.'+suf)
 plate('Vanguard_TabardBorder_'+suf,[(-.12,-.38),(.01,-.50),(.14,-.38),(.13,-.34),(.01,-.46),(-.115,-.34)],.027,6,(side*.145,-.17,1.10),'Tabard.'+suf)
# enclosed helmet sculpted as successive angular sections, not primitive head with painted visor
shell('Vanguard_Gorget',[(1.71,.20,.16,0,0),(1.81,.18,.15,0,0)],3,'Spine')
shell('Vanguard_Helmet',[(1.80,.17,.145,0,0),(1.93,.225,.20,0,0),(2.12,.23,.19,0,0),(2.25,.11,.13,0,.015)],1,'Head',10)
plate('Vanguard_VisorSlot',[(-.19,.024),(0,-.005),(.19,.024),(.18,-.04),(0,-.065),(-.18,-.04)],.028,0,(0,-.191,2.035),'Head')
for side in [-1,1]:
 plate('Vanguard_EyeGlow',[(side*.025,0),(side*.15,.018),(side*.15,-.002),(side*.025,-.017)],.032,4,(0,-.207,2.026),'Head')
 plate('Vanguard_Cheek',[(0,.08),(side*.14,.11),(side*.18,-.015),(side*.055,-.13),(0,-.095)],.06,1,(0,-.19,1.94),'Head')
plate('Vanguard_Brow',[(-.21,0),(0,.04),(.21,0),(.19,-.035),(0,.008),(-.19,-.035)],.045,3,(0,-.21,2.09),'Head')
# swept crest: wedge volume in YZ, oriented along forward-back, rises to legacy 2.535 crest height
crest=mesh('Vanguard_Crest',[(-.042,-.14,2.20),(.042,-.14,2.20),(-.033,-.03,2.535),(.033,-.03,2.535),(-.027,.24,2.34),(.027,.24,2.34),(-.03,.19,2.18),(.03,.19,2.18)],[(0,2,4,6),(1,7,5,3),(0,1,3,2),(2,3,5,4),(4,5,7,6),(6,7,1,0)],5); bind(crest,'Head')
# back mantle with centre crease and bottom notch, rigged separately
folded_mantle()
hero=current[:]
# Build deform rig in Blender coordinates (+Z up, -Y forward).
bones=[('Root',(0,0,0),(0,0,.2),None),('Pelvis',(0,0,1.05),(0,0,1.25),'Root'),('Spine',(0,0,1.25),(0,0,1.73),'Pelvis'),('Head',(0,0,1.78),(0,0,2.21),'Spine'),('Mantle',(0,.23,1.67),(0,.25,1.0),'Spine')]
for side in [-1,1]:
 s='L' if side<0 else 'R'; x=side*.19
 bones += [('Thigh.'+s,(x,0,1.05),(x,0,.62),'Pelvis'),('Shin.'+s,(x,0,.62),(x,0,.20),'Thigh.'+s),('Foot.'+s,(x,0,.20),(x,-.27,.09),'Shin.'+s),('UpperArm.'+s,(side*.39,0,1.64),(side*.54,-.015,1.34),'Spine'),('Forearm.'+s,(side*.54,-.015,1.34),(side*.57,-.075,1.08),'UpperArm.'+s),('Hand.'+s,(side*.57,-.075,1.08),(side*.57,-.075,.99),'Forearm.'+s),('Tabard.'+s,(side*.145,-.17,1.10),(side*.145,-.18,.65),'Pelvis')]
arm=bpy.data.armatures.new('Vanguard_Rig'); rig=bpy.data.objects.new('Vanguard_Rig',arm); bpy.context.collection.objects.link(rig); bpy.context.view_layer.objects.active=rig; rig.select_set(True); bpy.ops.object.mode_set(mode='EDIT')
for name,h,t,parent in bones:
 b=arm.edit_bones.new(name); b.head=h; b.tail=t
 if parent:b.parent=arm.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT')
# rigid armour pieces intentionally follow individual bones; cloth gets a graded two-bone hinge.
for o in hero:
 bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active=o; bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 b=weights[o.name]; g=o.vertex_groups.new(name=b)
 if o.name in blended_weights:
  for index,influences in enumerate(blended_weights[o.name]):
   for bone,value in influences:
    group=o.vertex_groups.get(bone) or o.vertex_groups.new(name=bone);group.add([index],value,'REPLACE')
 elif 'Tabard' in o.name or 'BackMantle' in o.name:
  parent='Pelvis' if 'Tabard' in o.name else 'Spine'; p=o.vertex_groups.new(name=parent)
  top=1.10 if parent=='Pelvis' else 1.67
  for v in o.data.vertices:
   w=max(.15,min(1,(top-v.co.z)/.25)); g.add([v.index],w,'REPLACE'); p.add([v.index],1-w,'REPLACE')
 else:g.add(range(len(o.data.vertices)),1,'REPLACE')
 o.parent=rig; mod=o.modifiers.new('Vanguard_Skin','ARMATURE'); mod.object=rig
# Five explicit authoring/runtime part groups share one rig and atlas.
def part_group(o):
 n=o.name
 if any(x in n for x in ('Helmet','Visor','EyeGlow','Cheek','Brow','Crest')):return 'Head'
 if 'BackMantle' in n:return 'Back'
 if any(x in n for x in ('Tabard','Sleeve')):return 'Clothes'
 if any(x in n for x in ('PaddedTorso','Thigh','Glove','Boot','WaistBelt')):return 'Body'
 return 'Armor'
group_members={g:[o for o in hero if part_group(o)==g] for g in ('Body','Clothes','Armor','Head','Back')}
skins=[]
for group in ('Body','Clothes','Armor','Head','Back'):
 members=group_members[group]
 bpy.ops.object.select_all(action='DESELECT')
 for o in members:o.select_set(True)
 bpy.context.view_layer.objects.active=members[0];bpy.ops.object.join();part=members[0];part.name='Vanguard_'+group;skins.append(part)
# Only the group's live mesh references are used after join.
skin=skins[0]
# Sword is separate object, parented to right hand with exportable sockets.
sword(); sword_objects=assets['StarcoreSword']
# Keep each sword part rigidly skinned to Hand.R for one final sword draw call.
meshes=[o for o in sword_objects if o.type=='MESH']; anchors=[o for o in sword_objects if o.type=='EMPTY']
bpy.ops.object.select_all(action='DESELECT')
for o in meshes:o.select_set(True)
bpy.context.view_layer.objects.active=meshes[0]; bpy.ops.object.join(); sw=meshes[0]; sw.name='Vanguard_Sword'
# sword points upward at rest, grip at closed hand
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
sword_rotation=Matrix.Rotation(math.radians(32),4,'X')
for v in sw.data.vertices:v.co=sword_rotation@v.co+Vector((.57,-.075,1.075))
g=sw.vertex_groups.new(name='Hand.R'); g.add(range(len(sw.data.vertices)),1,'REPLACE'); sw.parent=rig; mod=sw.modifiers.new('SwordHandBinding','ARMATURE'); mod.object=rig
for o in anchors:
 world=sword_rotation@o.location.copy()+Vector((.57,-.075,1.075)); o.parent=rig; o.parent_type='BONE'; o.parent_bone='Hand.R'; bpy.context.view_layer.update(); o.matrix_world.translation=world
# Controlled clips: all bones keyed, no root translation, no gameplay animation events.
rig.animation_data_create(); clips={}
def reset():
 for b in rig.pose.bones:b.rotation_mode='XYZ'; b.rotation_euler=(0,0,0); b.location=(0,0,0); b.scale=(1,1,1)
def rot(name,x=0,y=0,z=0):rig.pose.bones[name].rotation_euler=[math.radians(v) for v in (x,y,z)]
for name,last in [('Idle',100),('Move',40),('Basic',25),('Hit',20),('Skill',50)]:
 act=bpy.data.actions.new('Pilot_'+name); act.use_fake_user=True; rig.animation_data.action=act
 frames=sorted(set([0,last]+list(range(0,last+1,2))+([13] if name=='Basic' else [])))
 for f in frames:
  reset(); t=f/last; wave=math.sin(t*math.tau)
  if name=='Idle':rot('Spine',wave*.9); rot('Mantle',2+wave*1.3)
  if name=='Move':
   for s,sign in [('L',1),('R',-1)]:
    w=wave*sign; rot('Thigh.'+s,w*22); rot('Shin.'+s,max(0,-w)*30); rot('Foot.'+s,-w*12-max(0,-w)*18); rot('UpperArm.'+s,-w*13); rot('Tabard.'+s,w*10)
   rot('Spine',5,0,wave*2); rot('Mantle',12+abs(wave)*5)
  if name=='Basic':
   # Contact pose exactly 13/25=.52; postcommit recovery never inserts windup.
   keys=[(0,0),(.30,-1),(.52,1),(1,0)]
   a=0
   for (t0,v0),(t1,v1) in zip(keys,keys[1:]):
    if t0<=t<=t1:a=v0+(v1-v0)*(t-t0)/(t1-t0);break
   rot('Spine',0,a*8,a*-23); rot('UpperArm.R',a*-55,a*17,a*45); rot('Forearm.R',a*-28,0,a*20); rot('Hand.R',a*8,0,a*-20); rot('UpperArm.L',-8,0,-a*15); rot('Mantle',abs(a)*12)
  if name=='Hit':
   a=math.sin(math.pi*t)*(1-t);rot('Spine',-28*a,0,8*a);rot('Head',15*a);rot('UpperArm.L',-20*a);rot('UpperArm.R',-20*a)
  if name=='Skill':
   a=math.sin(math.pi*t);rot('Spine',-12*a,0,-28*a);rot('UpperArm.R',-100*a,0,22*a);rot('Forearm.R',-35*a);rot('UpperArm.L',-30*a,0,-30*a);rot('Mantle',25*a)
  for b in rig.pose.bones:
   b.keyframe_insert('rotation_euler',frame=f,group=b.name);b.keyframe_insert('location',frame=f,group=b.name);b.keyframe_insert('scale',frame=f,group=b.name)
 for fc in act.fcurves:
  for kp in fc.keyframe_points:kp.interpolation='LINEAR'
 clips[name]=act
rig.animation_data.action=clips['Idle']; bpy.context.scene.render.fps=50; bpy.context.scene.frame_set(0)
bpy.ops.object.select_all(action='DESELECT')
for o in [rig,sw]+skins+anchors:o.select_set(True)
bpy.context.view_layer.objects.active=rig
# One verified export path for authoring builds and source-preserving re-exports.
import importlib.util
_export_spec=importlib.util.spec_from_file_location('vanguard_fbx_export',Path(__file__).with_name('export_vanguard_fbx.py'))
_export_module=importlib.util.module_from_spec(_export_spec);_export_spec.loader.exec_module(_export_module)
_export_module.export_current_scene(OUT/'Vanguard.fbx')
# studio is not exported
begin(); box('Preview_Ground',(0,0,-.075),(200,200,.10),0,.01)
world=bpy.context.scene.world; world.use_nodes=True; world.node_tree.nodes['Background'].inputs[0].default_value=(.1,.15,.21,1); world.node_tree.nodes['Background'].inputs[1].default_value=.5
for name,loc,power,size,col in [('Key',(-3,-4,7),1000,6,(1,.83,.64)),('Fill',(4,-2,5),700,5,(.56,.78,1)),('Rim',(0,4,6),1300,4,(1,.54,.22))]:
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size;d.color=col;o=bpy.data.objects.new(name,d);bpy.context.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
d=bpy.data.cameras.new('ReviewCamera');cam=bpy.data.objects.new('ReviewCamera',d);bpy.context.collection.objects.link(cam);cam.location=(4,-7,5);cam.rotation_euler=(Vector((0,0,1.2))-cam.location).to_track_quat('-Z','Y').to_euler();d.type='ORTHO';d.ortho_scale=3.8
sc=bpy.context.scene;sc.camera=cam;sc.render.engine='CYCLES';sc.cycles.device='CPU';sc.cycles.samples=32;sc.cycles.use_denoising=False;sc.render.resolution_x=1000;sc.render.resolution_y=1000;sc.render.resolution_percentage=100;sc.view_settings.view_transform='AgX';sc.render.image_settings.file_format='PNG';sc.render.filepath=str(PREVIEW/'Emberfall-Pilot-Vanguard.png')
for o in skins+[sw]:o.data.calc_loop_triangles()
report={'hero_triangles':sum(len(o.data.loop_triangles) for o in skins),'part_groups':[o.name for o in skins],'sword_triangles':len(sw.data.loop_triangles),'bones':len(arm.bones),'max_weights':max(len(v.groups) for o in skins for v in o.data.vertices),'clips':{n:{'frames':list(a.frame_range),'seconds':(a.frame_range.y-a.frame_range.x)/50} for n,a in clips.items()},'basic_contact_frame':13,'basic_contact_normalized':.52,'root_motion':False}
(SOURCE/'vanguard-budget.json').write_text(json.dumps(report,indent=2))
img.pack();mask.pack();bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Emberfall-Pilot-Vanguard.blend'))
if os.environ.get('PILOT_SKIP_RENDER')!='1':bpy.ops.render.render(write_still=True)
print('PILOT_HERO_BUDGET',json.dumps(report))

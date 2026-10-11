"""Original Blender 4.3.2 E05 tactical fittings. No downloaded inputs.
blender -b --factory-startup --python ArtSource/TacticalAttachments/build.py
Unity coordinates x/right y/up z/front, measured in torso-bound fractions.
"""
import bpy, math, struct, json, hashlib
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'Assets/Resources/TacticalAttachments'; SRC=Path(__file__).parent
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
objects=[]; pieces=[]
def cube(name,pos,scale):
 bpy.ops.mesh.primitive_cube_add(size=1,location=(pos[0],-pos[2],pos[1]));o=bpy.context.object;o.name=name;o.scale=(scale[0],scale[2],scale[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);pieces.append(o);return o
def gem(name,pos,scale):
 bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=1,location=(pos[0],-pos[2],pos[1]));o=bpy.context.object;o.name=name;o.scale=(scale[0],scale[2],scale[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);pieces.append(o)
def finish(name,color):
 bpy.ops.object.select_all(action='DESELECT')
 for o in pieces:o.select_set(True)
 bpy.context.view_layer.objects.active=pieces[0];bpy.ops.object.join();o=bpy.context.object;o.name=name;bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
 bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
 mat=bpy.data.materials.new(name);mat.diffuse_color=(*color,1);o.data.materials.clear();o.data.materials.append(mat);objects.append(o);pieces.clear()
# Rear twin reservoirs and raised fork make supplier identifiable from elevated gameplay view.
cube('braced frame',(0,.08,-.54),(.58,.66,.15))
for x in [-.23,.23]:
 gem('energy reservoir',(x,.25,-.70),(.14,.39,.16));cube('retaining foot',(x,-.18,-.68),(.23,.12,.30));cube('over shoulder feed',(x,.53,-.30),(.09,.11,.53))
cube('core bridge',(0,.24,-.72),(.48,.09,.08));finish('SupplierBackpack',(.92,.64,.12))
# Forward central pierced cross badge, clean silhouette distinct from the rear energy rig.
for x in [-1,1]:
 o=cube('hunt blade',(x*.14,.03,.60),(.10,.47,.10));o.rotation_euler[1]=x*-.52
cube('badge collar',(0,.27,.59),(.36,.065,.13));gem('target gem',(0,.025,.69),(.10,.15,.09));finish('HuntBadge',(.93,.25,.12))
# Side shoulder fins enclose the torso without covering the hunt badge or the rear supply frame.
for x in [-1,1]:
 gem('ward shoulder',(x*.55,.30,.02),(.15,.23,.36));cube('ward side clasp',(x*.56,-.01,.17),(.075,.27,.12))
finish('SupportMantle',(.12,.72,.84))
report={}
for o in objects:
 m=o.data;m.calc_loop_triangles();data=[];idx=[]
 for tri in m.loop_triangles:
  for vi in tri.vertices:
   v=o.matrix_world @ m.vertices[vi].co;n=(o.matrix_world.to_3x3().inverted().transposed() @ tri.normal).normalized();data.append((v.x,v.z,-v.y,n.x,n.z,-n.y,v.x+.5,v.z+.5));idx.append(len(idx))
 blob=struct.pack('<III',0x45464D31,len(data),len(idx))+b''.join(struct.pack('<8f',*v) for v in data)+struct.pack('<%dI'%len(idx),*idx)
 f=OUT/(o.name+'.bytes');f.write_bytes(blob);guid=hashlib.sha256(('emberfall.tactical-attachments.v1/'+o.name).encode()).hexdigest()[:32]
 f.with_suffix('.bytes.meta').write_text('fileFormatVersion: 2\nguid: '+guid+'\nTextScriptImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
 report[o.name]={'triangles':len(idx)//3,'bytes':len(blob),'materials':1,'textures':0,'sha256':hashlib.sha256(blob).hexdigest()}
(SRC/'budget.json').write_text(json.dumps(report,indent=2)+'\n')
# Three panels show fitted construction; gray torso is a review prop, not shipped geometry.
for i,o in enumerate(objects):
 o.location.x=(i-1)*2.0
 bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=6,radius=1,location=(o.location.x,0,0));b=bpy.context.object;b.scale=(.5,.5,.5);mat=bpy.data.materials.new('review torso');mat.diffuse_color=(.12,.15,.19,1);b.data.materials.append(mat)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=False
scene.world.color=(.25,.25,.25)
bpy.ops.object.light_add(type='AREA',location=(0,-4,7));bpy.context.object.data.energy=1100;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=7
bpy.ops.object.camera_add(location=(4,-6,4));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,.1))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=6.7;scene.camera=cam
scene.render.resolution_x=1200;scene.render.resolution_y=620;scene.render.resolution_percentage=100
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'TacticalAttachments.blend'))
scene.render.filepath=str(SRC/'Fittings-front.png');bpy.ops.render.render(write_still=True)
cam.location=(4,6,4);cam.rotation_euler=(Vector((0,0,.1))-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(SRC/'Fittings-back.png');bpy.ops.render.render(write_still=True)
print(json.dumps(report))

# Combined state checks the non-overlapping rear/front/lateral arrangement.
for o in list(scene.objects):
 if o.type=='MESH':o.hide_render=True
for o in objects:
 c=o.copy();c.data=o.data;scene.collection.objects.link(c);c.location=(0,0,0);c.hide_render=False
bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=6,radius=.5);b=bpy.context.object;b.data.materials.append(bpy.data.materials['review torso'])
cam.location=(3,-5,3);cam.rotation_euler=(Vector((0,0,.1))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=3.4
scene.render.filepath=str(SRC/'Fittings-combined.png');bpy.ops.render.render(write_still=True)

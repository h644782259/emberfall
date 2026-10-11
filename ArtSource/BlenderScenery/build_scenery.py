"""Original Emberfall scenery; Blender 4.3.2 background build, no external assets."""
import bpy, math, json, hashlib
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]; SRC=ROOT/'ArtSource/BlenderScenery'; OUT=ROOT/'Assets/Resources/BlenderScenery'
bpy.ops.wm.read_factory_settings(use_empty=True)
def mat(name,color):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True;m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(*color,1);m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.9;return m
clay=mat('Clay',(.49,.27,.18));gold=mat('Ochre',(.64,.43,.22));stone=mat('Stone',(.35,.40,.43));bark=mat('Bark',(.27,.20,.14));leaf=mat('Leaf',(.18,.36,.26))
def mesh(name,v,f,m):
 d=bpy.data.meshes.new(name);d.from_pydata(v,[],f);d.update();o=bpy.data.objects.new(name,d);bpy.context.collection.objects.link(o);d.materials.append(m);return o
def vessel():
 # Closed cross-section with true recessed mouth; 16 sides, no alpha/overdraw.
 profile=[(.0,.02),(.22,.02),(.32,.14),(.37,.44),(.30,.67),(.17,.79),(.17,.89),(.23,.91),(.23,.96),(.15,.96),(.13,.87),(.13,.78),(.25,.64),(.29,.44),(.24,.16),(.0,.16)]
 v=[(r*math.cos(i*math.tau/16),r*math.sin(i*math.tau/16),z) for r,z in profile for i in range(16)]
 f=[(j*16+i,j*16+(i+1)%16,(j+1)*16+(i+1)%16,(j+1)*16+i) for j in range(len(profile)-1) for i in range(16)]
 o=mesh('Clay',v,f,clay);o.data.materials.append(gold)
 for p in o.data.polygons:p.material_index=1 if p.index//16 in (5,6,7) else 0
 return [o]
def rubble():
 objs=[]
 for i,(x,y,z,s) in enumerate([(-.32,0,.23,.37),(.25,.10,.27,.42),(0,-.18,.19,.30)]):
  bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=1,location=(x,y,z));o=bpy.context.object;o.name='Stone';o.scale=(s,s*.78,s*.79);o.rotation_euler=(i*.3,i*.22,i*.9);o.data.materials.append(stone);objs.append(o)
 return objs
def limb(a,b,r):
 a,b=Vector(a),Vector(b);d=b-a;bpy.ops.mesh.primitive_cone_add(vertices=7,radius1=r,radius2=r*.62,depth=d.length,location=(a+b)/2);o=bpy.context.object;o.name='Bark';o.rotation_euler=d.to_track_quat('Z','Y').to_euler();o.data.materials.append(bark);return o
def tree():
 objs=[limb((0,0,0),(.08,0,2.65),.17)]
 for i in range(5):
  a=i*2.39996;level=1.65+i*.30;r=.66+(i%2)*.12;x,y=math.cos(a)*r,math.sin(a)*r
  objs.extend([limb((.05,0,level-.55),(x,y,level),.06),limb((x,y,level),(x*1.32,y*1.32,level+.34),.033)])
  # Three asymmetric faceted lobes per branch, with real volume and open sky gaps.
  # These replace the old scalloped fan recipe rather than re-exporting it.
  for k in range(3):
   t=a+(k-1)*.9;extent=.24 if k!=1 else .46
   bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=1,location=(x+math.cos(t)*extent,y+math.sin(t)*extent,level+.27+(k%2)*.16))
   o=bpy.context.object;o.name='Leaf';o.scale=(.58-i*.025,.43-i*.016,.16 if k!=1 else .22);o.rotation_euler=(.08*k,.12*i,t);o.data.materials.append(leaf);objs.append(o)
 return objs
assets={};report={}
for name,fn in [('WayfarerPot',vessel),('FracturedRubble',rubble),('OpenCanopyTree',tree)]:
 objs=fn();bpy.ops.object.select_all(action='DESELECT')
 for o in objs:o.select_set(True)
 bpy.context.view_layer.objects.active=objs[0];bpy.ops.object.join();o=objs[0];o.name=name;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 o.data.calc_loop_triangles();assert len(o.data.loop_triangles)<1024
 bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,path_mode='STRIP')
 report[name]={'triangles':len(o.data.loop_triangles),'vertices':len(o.data.vertices),'materials':len(o.data.materials),'textures':0,'dimensions_blender_xyz_m':list(o.dimensions),'fbx_bytes':(OUT/(name+'.fbx')).stat().st_size,'sha256':hashlib.sha256((OUT/(name+'.fbx')).read_bytes()).hexdigest()};assets[name]=o
assets['WayfarerPot'].location=(-1.6,-.3,0);assets['FracturedRubble'].location=(0,-.5,0);assets['OpenCanopyTree'].location=(1.7,.4,0)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.04));bpy.context.object.data.materials.append(mat('Review floor',(.055,.075,.09)))
world=bpy.data.worlds.new('Studio');bpy.context.scene.world=world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.14,.19,.25,1);world.node_tree.nodes['Background'].inputs[1].default_value=.5
for name,pos,energy,col in [('Key',(-3,-4,7),1200,(1,.85,.65)),('Fill',(4,-1,5),800,(.65,.8,1)),('Rim',(1,4,6),1000,(1,.67,.35))]:
 d=bpy.data.lights.new(name,'AREA');d.energy=energy;d.size=5;d.color=col;o=bpy.data.objects.new(name,d);bpy.context.collection.objects.link(o);o.location=pos;o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
d=bpy.data.cameras.new('ReviewCamera');o=bpy.data.objects.new('ReviewCamera',d);bpy.context.collection.objects.link(o);o.location=(6,-9,6);o.rotation_euler=(Vector((0,0,1.3))-o.location).to_track_quat('-Z','Y').to_euler();d.type='ORTHO';d.ortho_scale=6.8
s=bpy.context.scene;s.camera=o;s.render.engine='CYCLES';s.cycles.samples=24;s.cycles.use_denoising=False;s.render.resolution_x=1200;s.render.resolution_y=900;s.render.resolution_percentage=100;s.render.filepath=str(SRC/'Scenery-New-Preview.png');s.view_settings.view_transform='AgX'
report['_validation']={'blender':bpy.app.version_string,'unity_import_validated':False,'texture_bytes_added':0,'max_triangles_per_asset':1024,'low_tier':'Same bounded opaque meshes; no particles, alpha cards, tessellation or additional textures'}
(SRC/'budget.json').write_text(json.dumps(report,indent=2)+'\n');bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'Emberfall-Scenery.blend'));bpy.ops.render.render(write_still=True)
print(json.dumps(report,indent=2))

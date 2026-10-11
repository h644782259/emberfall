"""Original Emberfall rigid actor modules; Blender 4.3.2, no external assets.
Run from any cwd: blender -b --factory-startup --python <this-file>.
Blender mesh source -> little-endian Unity-axis .bytes, no runtime topology recipe.
"""
import bpy, bmesh, math, struct, json, hashlib
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
SRC=ROOT/'ArtSource/ActorModules'; OUT=ROOT/'Assets/Resources/ActorModules'
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
assets=[]
# Every module keeps the original primitive's bounding box, origin and forward axis.
# Ring profiles are deliberately different by role; Unity axes x/right y/up z/front.
def rings(name, rows, sides=8, height=1, depth=1, angular=0, face_push=0):
    verts=[]
    for y,rx,rz,front in rows:
        for i in range(sides):
            a=2*math.pi*i/sides+angular
            x=math.cos(a)*rx
            z=math.sin(a)*rz+front
            verts.append((x,-z,y))
    faces=[]
    for j in range(len(rows)-1):
        for i in range(sides):
            a=j*sides+i;b=j*sides+(i+1)%sides
            faces.append((a,b,b+sides,a+sides))
    faces.extend([tuple(reversed(range(sides))),tuple((len(rows)-1)*sides+i for i in range(sides))])
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
    # Bounds match original scales including capsule's 2-unit height.
    lo=[min(v.co[k] for v in mesh.vertices) for k in range(3)]
    hi=[max(v.co[k] for v in mesh.vertices) for k in range(3)]
    for v in mesh.vertices:
        for k,extent in enumerate((1,depth,height)):
            v.co[k]=((v.co[k]-lo[k])/(hi[k]-lo[k])-.5)*extent
    bpy.context.view_layer.objects.active=obj;obj.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT')
    obj.select_set(False);assets.append(obj)
    return obj
# Armoured chest: narrow waist, broad upper rib, recessed neck; retains capsule envelope.
rings('Cuirass',[(-1,.22,.23,0),(-.75,.34,.30,0),(-.18,.45,.38,.02),(.48,.50,.46,.02),(.85,.39,.33,0),(1,.27,.23,0)],10,height=2)
# Stepped pauldron with flattened lower rim and layered central ridge.
rings('Pauldron',[(-.5,.28,.28,0),(-.32,.48,.40,0),(-.20,.50,.48,0),(.04,.47,.48,0),(.26,.37,.38,0),(.5,.12,.18,0)],10)
rings('Helmet',[(-.5,.34,.40,.03),(-.34,.47,.47,.03),(.10,.5,.5,0),(.35,.39,.41,0),(.5,.17,.23,-.03)],12)
# Boots and paws show a forward toe and pronounced narrow ankle.
rings('Boot',[(-.5,.44,.50,0),(-.35,.50,.50,0),(-.16,.44,.43,.01),(.02,.34,.26,-.15),(.5,.29,.24,-.15)],8)
# Wolf silhouette v2: longitudinal skull, tapered muzzle and pointed ears.
# All original sockets and capsule/cube envelopes retained; only wolf geometry changes.
def wolf_longitudinal(name,rows,sides=8):
    o=rings(name,rows,sides)
    # longitudinal ring axis y becomes forward z; previous z becomes vertical y.
    for v in o.data.vertices:
        y,z=v.co.z,-v.co.y;v.co.z=z;v.co.y=-y
    bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free();o.data.update()
    return o
wolf_longitudinal('WolfHead',[(-.5,.20,.23,.04),(-.31,.43,.43,.03),(-.02,.5,.49,0),(.24,.38,.36,-.06),(.5,.22,.18,-.14)],10)
rings('WolfTorso',[(-1,.17,.19,0),(-.8,.40,.41,0),(-.47,.45,.47,0),(-.12,.32,.33,.06),(.20,.41,.45,.015),(.56,.5,.5,0),(.83,.40,.48,-.04),(1,.23,.30,-.05)],12,height=2)
# Lower leg narrows at ankle; forward toe and broad shoulder produce a real four-leg silhouette.
rings('Paw',[(-1,.40,.48,.06),(-.80,.48,.5,.055),(-.60,.26,.32,-.07),(-.20,.23,.24,-.12),(.2,.37,.30,-.11),(.62,.5,.42,-.02),(1,.44,.43,0)],8,height=2)
rings('WolfHindLeg',[(-1,.36,.49,.04),(-.8,.46,.5,.04),(-.59,.23,.28,-.1),(-.26,.24,.24,-.14),(.10,.43,.40,.015),(.51,.5,.5,.05),(1,.38,.43,0)],8,height=2)
wolf_longitudinal('WolfMuzzle',[(-.5,.49,.42,0),(-.27,.5,.5,0),(.15,.39,.33,-.04),(.5,.23,.21,-.03)],8)
rings('WolfEar',[(-.5,.43,.42,0),(-.20,.5,.5,0),(.05,.39,.32,0),(.50,.015,.035,-.04)],4,angular=math.pi/4)
rings('WolfTail',[(-1,.045,.045,0),(-.60,.24,.31,0),(-.13,.5,.5,0),(.36,.43,.40,0),(1,.20,.23,0)],8,height=2)
# Hand-cut gem silhouette instead of round orb; top/bottom retain socket centre.
rings('SpiritCore',[(-.5,.035,.035,0),(-.30,.27,.27,0),(.02,.50,.50,0),(.26,.36,.36,0),(.5,.06,.06,0)],8)
# Low irregular gelatin shoulders; bottom remains flattened at original floor envelope.
rings('Slime',[(-.5,.23,.23,0),(-.39,.43,.44,0),(-.12,.50,.50,0),(.17,.46,.42,0),(.38,.32,.28,0),(.5,.10,.08,0)],14)
rings('Hammer',[(-.5,.36,.36,0),(-.37,.50,.50,0),(-.25,.50,.50,0),(-.16,.40,.40,0),(.16,.40,.40,0),(.25,.50,.50,0),(.37,.50,.50,0),(.5,.36,.36,0)],8,angular=math.pi/4)
rings('Totem',[(-.5,.33,.28,0),(-.35,.50,.45,0),(-.10,.35,.32,0),(.12,.46,.50,0),(.32,.34,.28,0),(.5,.48,.38,0)],6)
# Boss chassis is a stepped octagonal pressure housing (original cylinder y envelope).
rings('BossHousing',[(-1,.32,.32,0),(-.70,.46,.46,0),(-.46,.50,.50,0),(.32,.50,.50,0),(.46,.40,.40,0),(.85,.40,.40,0),(1,.31,.31,0)],12,height=2)
rings('BossPlate',[(-1,.20,.20,0),(-.65,.38,.33,.02),(-.08,.5,.45,.04),(.45,.45,.5,0),(.83,.33,.30,-.03),(1,.14,.13,-.03)],8,height=2)
rings('SwordGuard',[(-.5,.30,.32,0),(-.22,.42,.46,0),(0,.5,.5,0),(.2,.40,.45,0),(.5,.24,.26,0)],8,angular=math.pi/4)
rings('BowLimb',[(-1,.22,.23,0),(-.65,.36,.34,0),(-.1,.5,.5,0),(.55,.38,.32,0),(1,.19,.19,0)],8,height=2)
rings('StaffCrown',[(-.5,.33,.33,0),(-.15,.45,.45,0),(.05,.5,.5,0),(.32,.27,.27,0),(.5,.04,.04,0)],6)
rings('ClothTorso',[(-1,.23,.24,0),(-.70,.34,.32,0),(-.25,.37,.37,0),(.26,.48,.43,0),(.63,.50,.46,0),(1,.29,.24,0)],14,height=2)
rings('BarkTorso',[(-1,.29,.31,0),(-.75,.46,.43,0),(-.35,.34,.33,0),(.08,.47,.41,0),(.48,.50,.50,0),(.8,.39,.36,0),(1,.31,.27,0)],7,height=2)
rings('CuirassPlate',[(-.5,.25,.25,0),(-.28,.38,.36,0),(.15,.50,.50,0),(.39,.46,.38,0),(.5,.28,.22,0)],10)
rings('ClothShoulder',[(-.5,.29,.29,0),(-.23,.50,.48,0),(.03,.48,.5,0),(.25,.37,.40,0),(.5,.14,.20,0)],14)
# Export flat corner normals to keep the sculpted facets; one existing material per renderer.
report={}
for obj in assets:
    mesh=obj.data;mesh.calc_loop_triangles()
    verts=[];norms=[];uv=[];indices=[]
    for tri in mesh.loop_triangles:
        for vi in tri.vertices:
            v=mesh.vertices[vi].co;n=tri.normal
            verts.append((v.x,v.z,-v.y));norms.append((n.x,n.z,-n.y));uv.append((v.x+.5,v.z*.5+.5));indices.append(len(indices))
    data=struct.pack('<III',0x45464D31,len(verts),len(indices))
    for v,n,t in zip(verts,norms,uv):data+=struct.pack('<8f',*v,*n,*t)
    data+=struct.pack('<%dI'%len(indices),*indices)
    path=OUT/(obj.name+'.bytes');path.write_bytes(data)
    guid=hashlib.sha256(('emberfall.actor-modules.v1/'+obj.name).encode()).hexdigest()[:32]
    meta=path.with_suffix(path.suffix+'.meta')
    if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+guid+'\nTextScriptImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
    report[obj.name]={'triangles':len(indices)//3,'vertices':len(verts),'materials_added':0,'textures_added':0,'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest(),'bounds_unity':[[min(v[k] for v in verts) for k in range(3)],[max(v[k] for v in verts) for k in range(3)]]}
# Contact sheet is a native Blender authoring screenshot, never an engine capture.
mat=bpy.data.materials.new('Preview jade bronze');mat.diffuse_color=(.18,.48,.42,1);mat.use_nodes=True
mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.18,.48,.42,1)
mat.node_tree.nodes['Principled BSDF'].inputs['Metallic'].default_value=.3
mat.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.38
for i,obj in enumerate(assets):
    obj.location=((i%5)*2.5,(i//5)*3,1.1);obj.data.materials.append(mat)
    bpy.ops.object.text_add(location=(obj.location.x-.75,obj.location.y-.95,.05));text=bpy.context.object;text.data.body=obj.name;text.data.size=.20
bpy.ops.object.camera_add(location=(8,-12,18));cam=bpy.context.object
cam.rotation_euler=(Vector((5,3,.5))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=15.5;bpy.context.scene.camera=cam
for location,energy,size in [((2,-4,12),1800,8),((9,6,10),1400,7)]:
    bpy.ops.object.light_add(type='AREA',location=location);lamp=bpy.context.object;lamp.data.energy=energy;lamp.data.shape='DISK';lamp.data.size=size
    lamp.rotation_euler=(Vector((5,3,0))-lamp.location).to_track_quat('-Z','Y').to_euler()
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=False
scene.world.color=(.15,.15,.15);scene.render.resolution_x=1500;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.render.filepath=str(SRC/'ActorModules-Blender.png')
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'Emberfall-ActorModules.blend'))
(SRC/'inventory.json').write_text(json.dumps({'blender':bpy.app.version_string,'format':'EFM1 little endian position/normal/uv/indices in Unity axes','source':'Original scripted geometry; no third party inputs','assets':report,'total_runtime_source_bytes':sum(r['bytes'] for r in report.values()),'total_triangles':sum(r['triangles'] for r in report.values()),'texture_bytes':0},indent=2)+'\n')
bpy.ops.render.render(write_still=True)
print('ACTOR_MODULES_OK',len(report),'assets',sum(r['bytes'] for r in report.values()),'bytes')

"""Sample the REIMPORTED FBX skeleton and skins against authored Blender motion.
This validates Blender's FBX roundtrip, never Unity's importer or clip bindings.
"""
import bpy,json,math,hashlib
from pathlib import Path
from mathutils.kdtree import KDTree
ROOT=Path(__file__).resolve().parents[2];SRC=ROOT/'ArtSource/BlenderPilot';FBX=ROOT/'Assets/Resources/BlenderPilot/Vanguard.fbx'
CLIPS=('Idle','Move','Basic','Hit','Skill');GROUPS=('Body','Clothes','Armor','Head','Back','Sword');SOCKETS=('Grip','Guard','BladeRoot','Tip','Pommel','Emission')
def capture():
 dg=bpy.context.evaluated_depsgraph_get();meshes={}
 for name in GROUPS:
  o=bpy.data.objects['Vanguard_'+name].evaluated_get(dg);m=o.to_mesh();meshes[name]=[tuple(o.matrix_world@v.co) for v in m.vertices];o.to_mesh_clear()
  assert all(math.isfinite(x) for v in meshes[name] for x in v)
 anchors={n:tuple(bpy.data.objects['Anchor_'+n].matrix_world.translation) for n in SOCKETS}
 return meshes,anchors
bpy.ops.wm.open_mainfile(filepath=str(SRC/'Emberfall-Pilot-Vanguard.blend'));rig=bpy.data.objects['Vanguard_Rig'];reference={}
for name in CLIPS:
 a=bpy.data.actions['Pilot_'+name];rig.animation_data.action=a;reference[name]=[]
 for frame in range(int(a.frame_range.x),int(a.frame_range.y)+1):
  bpy.context.scene.frame_set(frame);reference[name].append(capture())
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(FBX));rig=next(o for o in bpy.data.objects if o.type=='ARMATURE')
assert len(rig.data.bones)==19
report={'engine':'Blender '+bpy.app.version_string,'sampled_asset':'REIMPORTED Vanguard.fbx','fbx_sha256':hashlib.sha256(FBX.read_bytes()).hexdigest(),'unity_import_validated':False,'clips':{}}
for name in CLIPS:
 a=next(a for a in bpy.data.actions if a.name.endswith('Pilot_'+name));rig.animation_data.action=a
 frames=reference[name];assert int(a.frame_range.y-a.frame_range.x)+1==len(frames)
 maxmesh=0.;maxanchor=0.;moving=0.;first=None
 for idx,(expected,expectedanchors) in enumerate(frames):
  bpy.context.scene.frame_set(int(a.frame_range.x)+idx);actual,anchors=capture()
  for group in GROUPS:
   assert len(actual[group])==len(expected[group]),(name,group,'vertex count')
   # Symmetric nearest-surface-vertex distance tolerates FBX vertex reordering.
   for left,right in ((actual[group],expected[group]),(expected[group],actual[group])):
    tree=KDTree(len(right))
    for j,p in enumerate(right):tree.insert(p,j)
    tree.balance()
    maxmesh=max(maxmesh,max(tree.find(p)[2] for p in left))
  for socket in SOCKETS:
   delta=sum((anchors[socket][j]-expectedanchors[socket][j])**2 for j in range(3))**.5;maxanchor=max(maxanchor,delta)
  if first is None:first=anchors['Grip']
  moving=max(moving,sum((anchors['Grip'][j]-first[j])**2 for j in range(3))**.5)
 assert maxmesh<.0001,(name,'mesh roundtrip deviation',maxmesh)
 assert maxanchor<.0001,(name,'socket roundtrip deviation',maxanchor)
 assert moving>.0001,(name,'frozen imported action')
 report['clips'][name]={'frames_sampled':len(frames),'imported_frame_range':list(a.frame_range),'max_deformed_vertex_error_m':maxmesh,'max_socket_error_m':maxanchor,'grip_motion_from_first_m':moving}
report['total_frames']=sum(v['frames_sampled'] for v in report['clips'].values());report['pass']=True
(SRC/'validation-fbx-motion.json').write_text(json.dumps(report,indent=2)+'\n');print('FBX_MOTION_PASS',json.dumps(report))

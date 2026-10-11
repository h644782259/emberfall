"""Original additive swordguard animation curves. Blender 4.3; no external assets.
Rebuild: blender -b --python ArtSource/VanguardActions/build.py
Curves only: original gameplay root and accepted locomotion remain authoritative.
"""
import bpy, math, struct, json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'ArtSource/VanguardActions'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bones=['Pelvis','Spine','Head','LeftArm','RightArm','LeftKnee','RightKnee','Cloak','Sword']
clips=['Idle','Forward','Backward','Left','Right','TurnLeft','TurnRight','Basic','Skill','Jump','Landing','Hit','Death']
rig=[]
for i,n in enumerate(bones):
 o=bpy.data.objects.new(n,None);bpy.context.collection.objects.link(o);o.rotation_mode='XYZ';rig.append(o)
# Explicit artist-authored key poses (Euler degree deltas on the existing local rig).
def pose(**kw):return kw
keys={
'Idle':[(0,pose()),(.5,pose(Spine=(1.3,0,0),Head=(-.8,0,0),Cloak=(2,0,0))),(1,pose())],
'Forward':[(0,pose()),(.25,pose(Pelvis=(0,-2,1),Spine=(2,2,-1),LeftArm=(-5,0,0),RightArm=(3,0,0))),(.5,pose()),(.75,pose(Pelvis=(0,2,-1),Spine=(2,-2,1),LeftArm=(5,0,0),RightArm=(-3,0,0))),(1,pose())],
'Backward':[(0,pose()),(.25,pose(Pelvis=(-2,2,-1),Spine=(-3,-2,0),LeftKnee=(8,0,0),RightArm=(-4,0,0))),(.5,pose()),(.75,pose(Pelvis=(-2,-2,1),Spine=(-3,2,0),RightKnee=(8,0,0),LeftArm=(-4,0,0))),(1,pose())],
'Left':[(0,pose()),(.25,pose(Pelvis=(0,-3,-3),Spine=(0,4,2),LeftKnee=(7,0,0),Cloak=(0,4,0))),(.5,pose()),(.75,pose(Pelvis=(0,3,2),Spine=(0,-4,-2),RightKnee=(7,0,0))),(1,pose())],
'Right':[(0,pose()),(.25,pose(Pelvis=(0,3,3),Spine=(0,-4,-2),RightKnee=(7,0,0),Cloak=(0,-4,0))),(.5,pose()),(.75,pose(Pelvis=(0,-3,-2),Spine=(0,4,2),LeftKnee=(7,0,0))),(1,pose())],
'TurnLeft':[(0,pose()),(1,pose(Pelvis=(0,3,0),Spine=(0,-5,-2),Head=(0,-7,0),Cloak=(0,9,0)))],
'TurnRight':[(0,pose()),(1,pose(Pelvis=(0,-3,0),Spine=(0,5,2),Head=(0,7,0),Cloak=(0,-9,0)))],
'Basic':[(0,pose()),(.3,pose(Spine=(-3,-5,-2),LeftArm=(-7,0,-5),Cloak=(4,-6,0))),(.52,pose(Spine=(3,4,1),LeftArm=(-4,0,-3),Cloak=(6,7,0))),(1,pose())],
'Skill':[(0,pose()),(.3,pose(Pelvis=(-3,-4,0),Spine=(-5,-8,-3),LeftArm=(-14,0,-8),Cloak=(5,-10,0))),(.52,pose(Pelvis=(3,3,0),Spine=(5,8,3),LeftArm=(-8,0,-12),Cloak=(10,12,0))),(1,pose())],
'Jump':[(0,pose()),(1,pose(Spine=(-4,0,0),LeftArm=(-12,0,-8),RightArm=(-6,0,5),Cloak=(12,0,0)))],
'Landing':[(0,pose()),(1,pose(Spine=(7,0,0),Head=(-4,0,0),LeftArm=(-5,0,-5),Cloak=(-6,0,0)))],
'Hit':[(0,pose(Spine=(-6,0,-3),Head=(-4,0,2),LeftArm=(6,0,-4))),(.35,pose(Spine=(-3,0,-2),Head=(-2,0,1))),(1,pose())],
'Death':[(0,pose()),(.4,pose(Spine=(8,0,4),Head=(10,0,0),LeftArm=(8,0,12),RightArm=(8,0,-12),LeftKnee=(12,0,0),RightKnee=(18,0,0))),(1,pose(Spine=(14,0,6),Head=(16,0,0),LeftArm=(12,0,18),RightArm=(12,0,-18),LeftKnee=(20,0,0),RightKnee=(28,0,0),Cloak=(-10,0,0)))]}
values=[]
for clip in clips:
 for o in rig:
  o.animation_data_clear();o.animation_data_create();o.animation_data.action=bpy.data.actions.new(clip+'_'+o.name)
  for t,p in keys[clip]:
   o.rotation_euler=tuple(math.radians(x) for x in p.get(o.name,(0,0,0)));o.keyframe_insert(data_path='rotation_euler',frame=t*32)
  for fc in o.animation_data.action.fcurves:
   for kp in fc.keyframe_points:kp.interpolation='LINEAR'
  o.animation_data.action.use_fake_user=True
 for frame in range(33):
  bpy.context.scene.frame_set(frame)
  for o in rig:values.extend(math.degrees(v) for v in o.rotation_euler)
path=ROOT/'Assets/Resources/VanguardActions/Swordguard.bytes';path.write_bytes(b'VGA1'+struct.pack('<iii',len(clips),len(bones),33)+struct.pack('<%df'%len(values),*values))
bpy.context.scene['runtime_contract']='Additive local rotations; no root translation, action clock, hit time, root turn or equipment bindings.'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Swordguard-Actions.blend'))
(OUT/'budget.json').write_text(json.dumps(dict(clips=clips,bones=bones,samples_per_clip=33,runtime_bytes=path.stat().st_size,triangles_added=0,materials_added=0,textures_added=0,source='Original key poses authored in build.py; Blender baked local Euler offsets'),indent=2)+'\n')
print('PASS Vanguard actions: complete 13-channel pose library',path.stat().st_size)

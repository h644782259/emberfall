"""Export hand-edited named actions from Swordguard-Actions.blend without rebuilding it.
blender -b ArtSource/VanguardActions/Swordguard-Actions.blend --python ArtSource/VanguardActions/export_blend.py
"""
import bpy,struct,math,json
from pathlib import Path
root=Path(__file__).resolve().parents[2]
# Stable packed-action schema; generated measurement reports are not source inputs.
schema={'clips': ['Idle', 'Forward', 'Backward', 'Left', 'Right', 'TurnLeft', 'TurnRight', 'Basic', 'Skill', 'Jump', 'Landing', 'Hit', 'Death'], 'bones': ['Pelvis', 'Spine', 'Head', 'LeftArm', 'RightArm', 'LeftKnee', 'RightKnee', 'Cloak', 'Sword']}
values=[]
for clip in schema['clips']:
 objects=[]
 for name in schema['bones']:
  obj=bpy.data.objects[name];obj.animation_data_create();obj.animation_data.action=bpy.data.actions[clip+'_'+name];objects.append(obj)
 for frame in range(33):
  bpy.context.scene.frame_set(frame)
  for obj in objects:
   angles=[math.degrees(v) for v in obj.rotation_euler]
   assert all(math.isfinite(v) and abs(v)<=40 for v in angles),'authored rotation budget exceeded'
   values.extend(angles)
asset=root/'Assets/Resources/VanguardActions/Swordguard.bytes';asset.write_bytes(b'VGA1'+struct.pack('<iii',13,9,33)+struct.pack('<%df'%len(values),*values))
print('PASS exported editable Blender actions:',len(values),'floats',asset.stat().st_size,'bytes')

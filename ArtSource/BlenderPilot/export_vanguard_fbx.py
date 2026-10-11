"""Bounded per-clip FBX sampling for unchanged Vanguard source, Blender 4.3.2 only."""
import bpy,inspect,json,sys
from pathlib import Path
SCHEDULE={'Idle':1.0,'Move':1.0,'Basic':0.0625,'Hit':0.5,'Skill':0.5}
GROUPS={'Vanguard_'+n for n in ('Rig','Body','Clothes','Armor','Head','Back','Sword')}|{'Anchor_'+n for n in ('Grip','Guard','BladeRoot','Tip','Pommel','Emission')}
def export_current_scene(path):
 path=Path(path)
 if path.suffix.lower()!='.fbx':raise ValueError('Output must have .fbx suffix')
 if bpy.data.filepath and path.resolve()==Path(bpy.data.filepath).resolve():raise ValueError('Never overwrite the source blend')
 from io_scene_fbx import export_fbx_bin
 if bpy.app.version!=(4,3,2):raise RuntimeError('Vanguard sampler verified only with Blender 4.3.2')
 native=export_fbx_bin.fbx_animations_do
 expected=('scene_data','ref_id','f_start','f_end','start_zero','objects','force_keep')
 if tuple(inspect.signature(native).parameters)!=expected:raise RuntimeError('Unsupported FBX exporter signature')
 if {o.name for o in bpy.context.selected_objects}!=GROUPS:raise RuntimeError('Select only Vanguard rig, six meshes and six anchors')
 calls=[]
 def sample_clip(scene_data,ref_id,*args,**kwargs):
  if not isinstance(ref_id,tuple) or len(ref_id)!=2:raise RuntimeError('Expected one object/action export reference')
  act=ref_id[1];name=act.name.removeprefix('Pilot_')
  if name not in SCHEDULE or act.name!='Pilot_'+name:raise RuntimeError('Unexpected Vanguard action: '+act.name)
  if not hasattr(scene_data,'_replace') or not hasattr(scene_data.settings,'_replace'):raise RuntimeError('Unsupported FBX settings type')
  calls.append({'action':act.name,'step':SCHEDULE[name]})
  return native(scene_data._replace(settings=scene_data.settings._replace(bake_anim_step=SCHEDULE[name])),ref_id,*args,**kwargs)
 export_fbx_bin.fbx_animations_do=sample_clip
 try:
  result=bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH','ARMATURE','EMPTY'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,bake_anim_step=1.0,path_mode='STRIP')
  if result!={'FINISHED'}:raise RuntimeError('FBX exporter did not finish')
  if len(calls)!=5 or {x['action'] for x in calls}!={'Pilot_'+n for n in SCHEDULE}:raise RuntimeError('Expected all five Vanguard actions exactly once')
 finally:export_fbx_bin.fbx_animations_do=native
 return calls

def export_blend(blend,output):
 bpy.ops.wm.open_mainfile(filepath=str(blend));rig=bpy.data.objects['Vanguard_Rig'];rig.animation_data.action=bpy.data.actions['Pilot_Idle'];bpy.context.scene.frame_set(0)
 bpy.ops.object.select_all(action='DESELECT')
 for name in GROUPS:bpy.data.objects[name].select_set(True)
 bpy.context.view_layer.objects.active=rig
 return export_current_scene(output)
if __name__=='__main__':
 import argparse
 parser=argparse.ArgumentParser();parser.add_argument('--blend',required=True);parser.add_argument('--out',required=True);args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
 print(json.dumps({'output':args.out,'calls':export_blend(args.blend,args.out)}))

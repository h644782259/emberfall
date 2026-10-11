"""Read final FBX exports back with Blender; not a Unity import assertion."""
import bpy,json,math
from pathlib import Path
root=Path(__file__).resolve().parents[2];out=root/'Assets/Resources/BlenderScenery';report={}
for p in sorted(out.glob('*.fbx')):
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(p));objects=[o for o in bpy.data.objects if o.type=='MESH'];assert len(objects)==1
 o=objects[0];o.data.calc_loop_triangles();assert len(o.data.loop_triangles)<1024
 coords=[o.matrix_world@v.co for v in o.data.vertices];low=[min(v[i] for v in coords) for i in range(3)];high=[max(v[i] for v in coords) for i in range(3)]
 assert all(math.isfinite(v) for pt in coords for v in pt);assert low[2]>=-.10 # Original rubble may sit slightly embedded in the ground.
 names=[m.name for m in o.data.materials];assert all(n in ('Clay','Ochre','Stone','Bark','Leaf') for n in names)
 assert len(o.data.polygons)>0 and all(p.material_index<len(names) for p in o.data.polygons)
 report[p.name]={'triangles':len(o.data.loop_triangles),'materials':names,'world_bounds_blender_z_up':[low,high],'mesh_count':len(objects),'animation_count':len(bpy.data.actions)}
report['evidence_scope']='Blender 4.3.2 FBX round-trip only. Unity importer/rendering/device performance pending.'
(root/'ArtSource/BlenderScenery/validation.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))

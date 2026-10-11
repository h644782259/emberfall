from pathlib import Path
root=Path(__file__).resolve().parents[1]
def read(path):return (root/'Assets/Scripts'/path).read_text()
model=read('Combat/CombatModel.cs');costume=read('Combat/CombatModel.Costumes.cs');library=read('Combat/CostumeMeshLibrary.cs')
assert 'model.BuildClassCostume()' in model and 'BuildFashionWingShape(wings,color)' in model
assert 'if(heroClass!=HeroClass.Vanguard){BuildClassEquipmentArmor(look);return;}' in model
assert 'heroClass==HeroClass.Ranger&&side==1' in costume
assert 'Long robe front panel' in costume and 'Leaf ritual mantle' in costume and 'Bound spirit totem' in costume
assert 'CostumeMeshLibrary.Get(recipe)' in costume and 'Mesh[] meshes=new Mesh[3]' in library and 'Object.Destroy(meshes[i])' in library
assert 'angle+Time.deltaTime*16f' in library and 'Time.time *' not in library
assert 'AddComponent<Collider' not in costume and 'AddComponent<Rigidbody' not in costume
print('PASS: class-specific equipment, three cached wing meshes, bounded integrated orbit, cosmetic-only shapes')

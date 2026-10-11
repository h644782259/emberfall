from pathlib import Path
root=Path(__file__).resolve().parents[1]
m=(root/'Assets/Scripts/Combat/CombatModel.cs').read_text()
c=(root/'Assets/Scripts/Combat/CombatModel.Costumes.cs').read_text()
l=(root/'Assets/Scripts/Combat/CombatModel.CostumeLayers.cs').read_text()
mesh=(root/'Assets/Scripts/Combat/CostumeMeshLibrary.cs').read_text()
assert m.index('model.BuildClassCostume();')<m.index('model.CaptureBaseCostume();')
assert 'SetBaseCostumeVisible(armor == null);' in m
for item in ['equipmentArmor','equipmentLeftShoulder','equipmentRightShoulder']:
    assert f'{item}.gameObject.SetActive(false); Destroy({item}.gameObject)' in m
assert 'part.gameObject.activeSelf && CostumeLayers.IsBaseOuter(part.name)' in l
assert 'part.SetActive(visible)' in l
assert 'Color cloth=GameBalance.ClassColor(heroClass)*.72f;' in c
assert 'BuildClassUpgradeGeometry(look);' in c
for ornament in ['Stitched star-chart point','Astrolabe chest frame','Master chart collar fin','Ranger clasp','Layered ranger arm guard','Master ranger feather crest','Contract branch ring','Forked contract bough','Master contract leaf']:
    assert ornament in l
assert 'GlowingPart' not in l
assert 'BuildWeaponFashionShape(weapon)' in m and 'RefreshWeaponFashion();' in m
assert 'Weapon aura"' not in m
assert 'WeaponVisualAnchor.SwordGuard' in l and 'WeaponVisualAnchor.StaffCore' in l and 'weaponStructure.BowReach' in l
assert 'if(style==WingSilhouette.Crystal)' in mesh and 'faceted[i]=vertices[triangles[i]];indices[i]=i;' in mesh
assert mesh.index('vertices=faceted;triangles=indices;')<mesh.index('mesh.RecalculateNormals()')
print('PASS: costume swap/milestone/fashion/faceted-normal production source contracts')

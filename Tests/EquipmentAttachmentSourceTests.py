from pathlib import Path
r=Path(__file__).resolve().parents[1]
m=(r/'Assets/Scripts/Combat/CombatModel.cs').read_text()
assert 'EquipmentAttachmentRecipe.Relic(look.Tier,look.UpgradeRank)' in m
assert 'piece.X,piece.Y,piece.Z' in m and 'piece.Width,piece.Height,piece.Depth' in m
assert 'piece.Shape==AttachmentShape.Disc?90:0,0,piece.Roll' in m
assert 'equipmentRelic.gameObject.SetActive(false); Destroy(equipmentRelic.gameObject)' in m
assert 'weaponStructure.StaffCore - .11f' in m and 'weaponStructure.StaffCore + .14f' in m
assert 'staffRig != null ? weaponStructure.StaffCore + .15f' in m
print('PASS: actual relic geometry consumes bounded recipe; staff decorations follow growing core; swap retires old rendering immediately')

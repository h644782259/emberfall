from pathlib import Path
root=Path(__file__).resolve().parents[1]
helper=(root/'Assets/Scripts/UI/GameFont.cs').read_text()
assert '#if !UNITY_ANDROID && !UNITY_IOS' in helper
assert 'if(ownsShared&&shared!=null)Object.Destroy(shared)' in helper
assert 'public static void Release(ref Font reference){reference=null;}' in helper
for name in ['UI/GameUI.cs','World/GroundLootPickup.cs','Core/AdventureCamera.Visibility.cs','Combat/FloatingNumber.cs']:
 text=(root/'Assets/Scripts'/name).read_text()
 assert 'GameFont.Shared' in text,name
 assert 'CreateDynamicFontFromOSFont' not in text,name
 assert 'Destroy(font)' not in text and 'Destroy(occlusionFont)' not in text,name
assert 'GameFont.WorldLabels' in (root/'Assets/Scripts/World/WorldBuilder.cs').read_text()
for path in (root/'Assets/Scripts').rglob('*.cs'):
 if path.name!='GameFont.cs':assert 'CreateDynamicFontFromOSFont' not in path.read_text(),path
print('PASS: all runtime Chinese text uses resolver; mobile has no OS fallback; consumers do not destroy shared fonts')

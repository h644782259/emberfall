#!/usr/bin/env python3
"""Static asset/configuration checks. Does not run Unity, import assets or build players."""
from pathlib import Path
import hashlib,re,struct,json
root=Path(__file__).resolve().parent.parent
icon=root/'Assets/Art/EmberfallIcon.png';data=icon.read_bytes()
assert data[:8]==b'\x89PNG\r\n\x1a\n'
w,h,depth,color=struct.unpack('>IIBB',data[16:26]);assert(w,h,depth,color)==(1024,1024,8,2),'Opaque RGB 1024x1024 master required'
meta=icon.with_suffix('.png.meta').read_text();guid=re.search(r'^guid: (\w+)',meta,re.M)[1]
settings=(root/'ProjectSettings/ProjectSettings.asset').read_text();assert guid in settings and 'bundleVersion: 0.4.0' in settings
for size in [256,128,64,48,32,16]:assert 'm_Width: '+str(size) in settings
assert 'enableMipMap: 0' in meta and 'alphaSource: 0' in meta
for source in (root/'Assets').rglob('*.cs'):
 text=source.read_text();assert not re.search(r'^(<<<<<<< |=======|>>>>>>> )',text,re.M),str(source)
 assert source.with_suffix('.cs.meta').is_file(),'Missing meta: '+str(source)
assert (root/'Assets/Editor/AppIconSetup.cs').is_file()
if(root/'Assets/Editor/IOSBuild.cs').exists():
 assert 'm_Width: 1024' in settings and 'm_SubKind: App Store' in settings
 block=settings.split('  m_BuildTargetPlatformIcons:',1)[1].split('  m_BuildTargetBatching:',1)[0]
 assert block.count(guid)==19 and 'm_Textures: []' not in block
 assert 'iPhoneAndiPad' in (root/'Assets/Editor/IOSBuild.cs').read_text()
 assert 'GameFont.Shared' in (root/'Assets/Scripts/UI/GameUI.cs').read_text()
print(json.dumps({'passed':True,'scope':'PNG and source/config checks only','iconBytes':len(data),'iconSha256':hashlib.sha256(data).hexdigest(),'iconDimensions':[w,h],'rgbOpaque':True,'guid':guid},indent=2))

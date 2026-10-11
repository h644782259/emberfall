from pathlib import Path
import re
root=Path(__file__).resolve().parents[1]
def read(p):return (root/'Assets/Scripts'/p).read_text()
checks=0
def check(ok,label):
 global checks
 assert ok,label
 checks+=1
model=read('Combat/CombatModel.cs')
for name,surface in [('Cuirass','Metal'),('Shoulder shell','Metal'),('Bow Limb','Wood'),('Leaf crown','Foliage')]:
 check(re.search(r'(?:Part|model.Part)\("'+name+r'".*?VisualSurface\.'+surface,model,re.S),name+' explicit surface')
check('0,5,1,0,1,7 }, color, VisualSurface.Metal)' in model,'forged blade explicit metal')
npc=read('World/WorldBuilder.Hubs.cs')
check('VisualSurface.Skin' in npc and npc.count('VisualSurface.Cloth')>=2,'NPC material categories')
atlas=read('UI/UIIconAtlas.cs')
for skill,draw in [(0,'ink.Arrow'),(1,'ink.Ring'),(2,'ink.Polygon'),(5,'ink.Shield')]:
 match=re.search(r'if \(skill == '+str(skill)+r'\)\s*\{(.*?)\n                \}',atlas,re.S)
 check(match and draw in match.group(1),'summoner icon '+str(skill))
for path in (root/'Assets/Scripts').rglob('*.cs'):
 for line in path.read_text().splitlines():
  if 'CombatArea.Spawn(' in line:check('visual:' in line,'explicit area recipe '+str(path))
for path in ['Combat/CombatEffects.cs','Combat/AdvancedSkillSequence.cs']:
 source=read(path)
 check(not re.search(r'(?:tint|element)\.[rgb]\s*[<>]',source),'no palette inferred recipe '+path)
for path in ['UI/GameUI.cs','UI/GameUI.Mobile.cs']:
 check('UIIconAtlas.Skill' in read(path) or ('DrawSkillIdentity' in read(path) and 'UIIconAtlas.Skill' in read('UI/GameUI.SkillIcons.cs')),'shared skill icon atlas '+path)
print('PASS:',checks,'visual identity source contracts (no rendered validation)')

"""UI wiring contracts, not engine rendering or touch-device acceptance."""
from pathlib import Path
r=Path(__file__).resolve().parents[1]/'Assets/Scripts/UI'
s={p.name:p.read_text() for p in r.glob('*.cs')}
checks=0
def check(ok,message):
 global checks
 checks+=1
 assert ok,message
skills=s['GameUI.MobileSkills.cs'];nav=s['GameUI.MobileSkillNavigation.cs'];atlas=s['UIIconAtlas.cs'];inv=s['GameUI.MobileInventory.cs']
check('split ? layout.BodyLeft : layout.Body' in skills and 'split ? layout.BodyRight : layout.Body' in skills,'narrow panels take full body; tablet retains split')
check('bool showList = split || !mobileSkillDetail' in skills,'narrow list/detail mutually exclusive')
check('MobileSkillRowClicked(node)' in skills and 'touchScrollSuppressed != Time.frameCount' in nav,'production tree node routes through drag-release gate')
check('mobileSkillListScroll =' not in nav and 'mobileSkillDetailScroll =' not in nav,'back keeps scroll anchors')
check('panel != Panel.Skills' in nav and 'SkillIconPresentation.SideBySide' in nav,'navigation excludes unrelated and tablet panels')
check('progression.LearnSkill(selectedSkill)' in skills and 'progression.SkillLockReason(selectedSkill)' in skills,'real progression mutation and refusal retained')
check(skills.count('DrawSkillIdentity(')==2,'list and detail share actual identity')
check('DrawSkillIdentity(' in s['GameUI.DesktopSkills.cs'] and 'DrawSkillIdentity(' in s['GameUI.Mobile.cs'],'desktop detail/mobile combat share identity')
check('Fill(r,' not in s['GameUI.MobileFeedback.cs'] and 'Fill(caption,' not in s['GameUI.MobileFeedback.cs'],'state captions do not blanket glyph')
check('GameBalance.SkillPrerequisites[skill]' in skills and 'SkillTreeColumn(skill)' in skills and 'SkillTreeRow(skill)' in skills,'mobile tree renders the actual prerequisite graph')
check('SkillIconPresentation.RasterSize(requestedSize)' in atlas and 'GetPixels' not in atlas,'bounded icon sizes without non-readable texture access')
check('Apply(false,true)' in atlas.replace(' ',''),'raster releases CPU backing storage')
check('评分不含机制价值' in inv and 'MobileEquipmentScore(item)' in inv,'equipment score remains actual and excludes unscored mechanic')
check('MechanicBadgePresentation.Title(item,session.Progression.Profile.heroClass),width-26,13,true' in inv,'row height includes same badge measurement')
check('EquipmentComparisonPresentation.Description' in inv and 'EquipmentComparisonPresentation.Changes' in inv,'short badges retain complete description and gained/lost comparison')
print(f'PASS: {checks} skill readability UI wiring contracts (not Unity execution)')

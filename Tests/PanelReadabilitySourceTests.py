"""UI wiring only: geometry/font drawing still require Unity visual acceptance."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
def read(name): return (root/'Assets/Scripts/UI'/name).read_text()
skills=read('GameUI.DesktopSkills.cs')
modes=read('GameUI.Modes.cs')
camp=read('GameUI.MobileWorkshop.cs')
log=read('GameUI.Expedition.cs')
checks=0
def check(ok,message):
    global checks
    checks+=1
    assert ok,message
check('CalcHeight' in skills and 'descriptionHeight=DesktopParagraphHeight' in skills and 'prerequisiteHeight=DesktopParagraphHeight' in skills,'desktop long paragraphs use rendered font measurement')
check('evolutionHeight=Mathf.Max' in skills and 'DesktopParagraphHeight(evolutions[stage-1]' in skills,'rank cards use highest measured text rather than fixed clipping')
check(skills.index('EndTouchScroll();')<skills.index('float actionX')<skills.index('HandleHotbarPointer'),'learning and loadout stay outside scroll content')
check('desktopDetailSkill != skill' in skills,'changing skill resets only its detail scroll')
check('AdventureSelectionLayout' in modes and 'BeginTouchScroll("arena-entry"' not in modes,'all adventure entries use the bounded three-row production layout')
check('AdventureSelectionLayout.WorkshopY' in log and 'AdventureSelectionLayout.LogHeight' in log,'workshop clearance uses the actual log size rule')
check('if(panel!=Panel.None || session.InputBlocked)return;' in log,'system history is not interactive behind a modal')
check('mobileWorkshopScroll[campTab] = Vector2.zero' not in camp,'workshop mutations preserve position')
check('MobileWorkshopParagraph(ref y, width, mobileWorkshopStatus' not in camp and 'Feedback(!mobileWorkshopFailed, mobileWorkshopStatus)' in camp,'feedback stays in fixed header instead of shifting content')
print(f'PASS: {checks} panel readability source contracts (no Unity rendering)')

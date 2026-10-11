#!/usr/bin/env python3
"""Regression contracts for responsive routing and explicit-action persistence.

These inspect production integration; they do not execute Unity GUI or JsonUtility.
"""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
def read(path): return (root / path).read_text()
ui = read('Assets/Scripts/UI/GameUI.cs')
expedition = read('Assets/Scripts/UI/GameUI.Expedition.cs')
rewards = read('Assets/Scripts/UI/GameUI.Rewards.cs')
blessings = read('Assets/Scripts/UI/GameUI.MobileBlessings.cs')
shared = read('Assets/Scripts/UI/GameUI.MobilePanels.cs')
service = read('Assets/Scripts/Core/ProgressionService.cs')
session = read('Assets/Scripts/Core/GameSession.cs')
checks = 0
def check(ok, message):
    global checks
    checks += 1
    assert ok, message
def method(source, signature):
    begin = source.index('{', source.index(signature)); end = begin + 1; depth = 1
    while depth:
        depth += (source[end] == '{') - (source[end] == '}'); end += 1
    return source[begin:end]

for source, entry, mobile in [
    (ui, 'DrawInventory', 'DrawMobileInventory'), (ui, 'DrawFashion', 'DrawMobileFashion'),
    (ui, 'DrawSkills', 'DrawMobileSkills'), (rewards, 'DrawChests', 'DrawMobileChests'),
    (expedition, 'DrawBlessingChoice', 'DrawMobileBlessingChoice'),
    (expedition, 'DrawCampWorkshop', 'DrawMobileCampWorkshop')]:
    body = method(source, 'private void ' + entry + '(')
    check('if(MobileControls.Active){' + mobile + '();return;}' in body,
          entry + ' routes to its responsive view before desktop geometry')
check('Style(TouchFont(fontSize),bold,true' in shared and '.CalcHeight(' in shared,
      'wrapped paragraphs use actual font measurements')
check('ClosePanel();BlockUITransition();return true;' in shared,
      'shared close retains existing lifecycle and release latch')
check('bool canClose=true,bool pauseInstead=false' in shared and 'canClose||pauseInstead' in shared,
      'mandatory reward/choice views can retain their dismissal policy')
check('mobileBlessingOffer.Length!=offer.Length' in blessings and 'offer[i]!=mobileBlessingOffer[i]' in blessings,
      'defensive offer copies compare values, not reference identity')
check('mobileBlessingOwner!=session.Player' in blessings and 'mobileBlessingEpoch!=session.Player.CombatEpoch' in blessings and
      'mobileBlessingWave!=session.RunChoices.CompletedWave' in blessings,
      'new player/run/wave resets selection even if random cards repeat')
check('Mathf.Max(cardHeight,name+detail+association+82)' in blessings and 'BeginTouchScroll(' in blessings,
      'long blessing descriptions grow and scroll without shrinking controls')
check('session.ConfirmBlessing(selectedBlessing)' in blessings and 'selectedBlessing>=0&&selectedBlessing<count' in blessings,
      'blessing confirm uses actual selected index and shared gameplay service')
check('session.SetPaused(true)' in blessings, 'mandatory choice allows pause/save access')
gui = method(ui, 'private void OnGUI()')
check(gui.index('if (session.Paused) DrawPause();') < gui.index('else if(PauseUtilityVisible)') <
      gui.index('else if (session.IsDead)') < gui.index('else if (session.DungeonSelectionOpen)') <
      gui.index('else if (session.RunChoices.AwaitingChoice)'),
      'pause and return-to-pause utility surfaces render before blocking gameplay states')
update = method(ui, 'private void Update()')
check('session.IsDead && !session.Paused && !PauseUtilityVisible' in update and
      '(session.DungeonSelectionOpen || session.RunChoices.AwaitingChoice) && !session.Paused && !PauseUtilityVisible' in update,
      'dead/pending-state cleanup cannot erase a pause utility surface')
for name in ['EquipFashion', 'UnequipFashion', 'SetSpecialization', 'SetItemLocked', 'SetAutoSell',
             'LearnSkill', 'AssignSkill', 'AssignConsumable', 'MoveHotbarSkill', 'SetHotbarPage',
             'SetHotbarKey', 'UsePotion']:
    body = method(service, 'public bool ' + name + '(')
    check('Snapshot()' in body and 'CommitCandidate(candidate)' in body and 'Commit();' not in body,
          name + ' commits detached state and reports write failure')
commit = method(service, 'private bool CommitCandidate(')
check(commit.index('TryWriteAttachedProfile(candidate') < commit.index('Profile = candidate') < commit.index('RaiseChanged()'),
      'active profile and listeners only update after durable write succeeds')
potion = method(session, 'public void DrinkPotion()')
check('Notify(Progression.LastError)' in potion and 'Progression.Save()' not in potion,
      'potion error remains accurate and successful consumption is not saved twice')
check(potion.index('Progression.UsePotion()') < potion.index('Player.Heal('),
      'failed consumption cannot heal before its transaction')
check('bindingReturnPanel==Panel.Controls&&controlsReturnPause' in ui,
      'bindings opened through pause-origin guide retain their utility priority')
check('MobilePanelOwnsNotification' in method(ui, 'private void DrawNotification()') and 'string notice=session.Notification;' in shared,
      'responsive feedback stays in reserved header instead of overlapping active tabs')
check('panel == Panel.Chests && (!chestDetails || session.Paused)' in update,
      'Escape opens or resumes pause without consuming an unresolved chest')
check('"菜单",jade)' in rewards.replace(" ","") and 'session.SetPaused(true);BlockUITransition();return;' in rewards.replace(" ",""),
      'desktop chest has a recovery menu even when storage cannot accept its reward')
recap = read('Assets/Scripts/UI/GameUI.RunRecap.cs')
check('RecapStatusHeight(layout,unit)' in recap and 'contentHeight=RecapContentHeight(layout,data)+statusHeight' in recap,
      'long recap storage failures reserve measured scrolling space')
check('recapError=latestError;if(!string.IsNullOrEmpty(recapError))recapScroll=Vector2.zero;' in recap,
      'new settlement error is immediately visible at top of recap')
check('if(MobileControls.Active){DrawMobileSaveLocation();return;}' in method(ui, 'private void DrawSaveLocation()'),
      'mobile save-location utility cannot fall through to tiny desktop geometry')
check('.delete-pending 删除标记' in ui and '可恢复 .tmp' in ui,
      'desktop migration preserves interruption markers and recoverable documents')
print(f'PASS: {checks} responsive panel/persistence source contracts (not Unity execution)')

#!/usr/bin/env python3
"""Production UI wiring contracts. No Unity GUI, touch device, or rendered UX is executed."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
read=lambda name:(root/'Assets/Scripts/UI'/name).read_text()
checks=0
def check(value,message):
    global checks
    checks+=1
    assert value,message
def method(source,signature):
    begin=source.index('{',source.index(signature));end=begin+1;depth=1
    while depth:
        depth+=(source[end]=='{')-(source[end]=='}');end+=1
    return source[begin:end]
ui=read('GameUI.cs');mobile=read('GameUI.Mobile.cs');modes=read('GameUI.Modes.cs')
controls=read('MobileControls.cs');flow=read('GameUI.SaveFlow.cs');inventory=read('GameUI.MobileInventory.cs');hubs=read('GameUI.Hubs.cs')
hud=method(mobile,'private void DrawMobileHUD()')
check('TouchRect(l.EncounterText)' in hud and 'TouchRect(l.BossHealth)' in hud,'wave and boss rendering use tested control-safe geometry')
check('blockedRects.Add(r)' in method(modes,'private void DrawMobileModeStatus('),'objective card excludes camera gestures')
check('DrawMobileBattleNotice();return;' in method(ui,'private void DrawNotification()') and 'panel!=Panel.None||session.InputBlocked' in ui,'battle toast does not cover modal actions')
notice=method(mobile,'private void DrawMobileBattleNotice()')
check('MobileControls.Layout.Notice' in notice and 'GUI.Button' not in notice and 'blockedRects.Add' not in notice,'battle toast is presentation only and cannot consume controls or open a modal')
check('panel=' not in notice and 'SetUIBlocking' not in notice,'battle feedback cannot suspend gameplay')
manual=method(mobile,'private void OpenMobileNoticeFromPause()')
check('if(!session.Paused)return;' in manual and manual.index('if(!session.Paused)return;')<manual.index('panel=Panel.Notice'),'full notice can only open through an explicit paused action')
check('session.SystemMessages' in manual and 'session.Progression.LastError' in manual,'manual detail preserves generation diagnostics and save errors')
full_notice=method(mobile,'private void DrawMobileNotice()')
check('MeasureMobileParagraph' in full_notice and 'BeginTouchScroll' in full_notice and full_notice.index('EndTouchScroll()')<full_notice.index('layout.FooterButton'),'full feedback remains measured, scrollable and separate from return action')
check('if (MobileControls.Active) return DrawMobileSaveFlowConfirmation();' in method(flow,'private bool DrawSaveFlowConfirmation()'),'mobile save confirmation cannot use fixed desktop warning/error rectangles')
confirm=method(flow,'private bool DrawMobileSaveFlowConfirmation()')
check('DrawMobileDialogChrome' in confirm and 'DrawMobileSaveFlowContent(contentWidth,manual,problem,false)' in confirm,'confirmation uses production dialog bounds and measured content')
check(confirm.index('EndTouchScroll();')<confirm.index('layout.FooterButton') and 'count=manual?2:3' in confirm,'cancel/save/discard remain fixed outside scroll body')
check('mobileSaveFlowScroll=Vector2.zero' in confirm and 'CancelMobileScroll()' in confirm,'fresh storage failure returns to the top instead of staying hidden')
content=method(flow,'private float DrawMobileSaveFlowContent(')
check(content.index('width,problem')<content.index('saveFlowSourceName') and 'saveFlow.SourceId' in content and 'saveFlow.TargetId' in content,'failure has its own measured space before complete stable identities')
check('session.ModeRewardPending' in content and '自动保存仍持续写入' in content and '已自动保存的内容不会回滚' in content,'confirmation retains consequential autosave/discard distinctions')
selection=method(mobile,'private void DrawMobileSaveSelection()')
check('MeasureMobileParagraph(saveSelectionError' in selection and '(issueHeight+i*68)' in selection and
      selection.index('DrawMobileParagraph(10,6,478,saveSelectionError')<selection.index('for(int i=0;i<saveSlots.Count;i++)'),
      'load/delete failure owns measured space before save rows instead of covering list controls')
check('mobileSaveSelectionIssue!=saveSelectionError' in selection and 'saveSelectionScroll=Vector2.zero' in selection,
      'new save-list error resets scroll so recovery information is visible')
npc=method(hubs,'private void OpenNearbyHubNpc()')
check('mobileInventoryNpcRequest=kind' in npc,'NPC explicitly requests the corresponding mobile inventory destination')
inv=method(inventory,'private void DrawMobileInventory()')
check(inv.index('mobileInventoryProfile !=')<inv.index('mobileInventoryNpcRequest!=') and 'mobileInventoryTab=smith?1:2' in inv and 'mobileInventoryDetail=smith' in inv,'first-open profile reset cannot erase merchant supply or blacksmith detail routing')
row=method(inventory,'private bool DrawMobileInventoryList(')
check('availableRarity=levelLocked?' in row and '需 ' in row and 'ReviewEquipment(' in row,'unavailable rows stay dim, labelled and inspectable')
actions=method(inventory,'private void DrawMobileEquipmentActions(')
check('ProgressionAttention.LevelEligible' in actions and 'canEquip' in actions,'dimmed appearance never replaces actual equip eligibility gate')
cast=method(mobile,'private bool BeginMobileCast(')
check('MobileSkillPolicy.ButtonCount' in cast and 'MobileSkillPolicy.SkillAtButton(i,mobileSkillPage)' in cast and 'hotbarPage' not in cast,'mobile skill pages are independent from desktop paging')
release=method(mobile,'private void ContinueMobileCast(')
check('mobileTap.Release' in release and 'targeting.Begin(skill)' in release,'single committed touch release dispatches the real targeting/cast path')
pointer=method(controls,'public bool ProcessPointer(')
check('cameraGesture.Begin(finger,screen.x,screen.y)' in pointer and 'ui.IsScreenPointOverUI(screen)' in pointer,'camera ownership begins only outside UI')
check('moveFinger == -1000' in pointer and 'Role.Skill' in pointer and 'Role.Attack' in pointer and 'ActivateMobileInteraction(finger)' in pointer,'movement, skill, attack and contextual action keep independent touch ownership')
hotbar=method(mobile,'private void DrawMobileHotbar(')
check('UIIconAtlas.ControlDisc()' not in hotbar and 'Border(' not in hotbar and 'DrawSkillIdentity(' not in hotbar and 'Lv.' not in hotbar,'floating battle glyphs have no square tile frame or rank label')
check('UIIconAtlas.Skill(p.heroClass,skill,48)' in hotbar and 'ready?Color.white' in hotbar and 'new Color(.38f,.42f,.46f,.58f)' in hotbar,'availability changes the whole transparent glyph brightness')
check('"免费"' not in read('GameUI.CombatOpportunities.cs'),'companion commands do not expose ambiguous free label')
check('"刷新"' not in selection,'mobile saves refresh through normal entry lifecycle without a manual refresh button')
print(f'PASS: {checks} mobile interaction/dialog source contracts (not Unity execution)')

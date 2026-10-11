"""Save-location touch integration contracts; no clipboard, file or Unity action executes."""
from pathlib import Path
root = Path(__file__).resolve().parents[1]
source = (root / 'Assets/Scripts/UI/GameUI.MobileSaveLocation.cs').read_text()
checks = 0
def check(value, message):
    global checks
    checks += 1
    assert value, message

check('MobilePanelGeometry()' in source and 'DrawMobilePanelChrome(' in source, 'save location uses touch panel geometry')
check('MeasureMobileParagraph' in source and 'DrawMobileParagraph' in source and 'CalcSize' in source, 'path wrapping and body height use the actual font')
check('MobileSavePathText.Wrap(path, contentWidth - 16, measure)' in source, 'complete path receives measured display wrapping')
check('GUIUtility.systemCopyBuffer = path;' in source and 'systemCopyBuffer = mobileSaveDisplayPath' not in source, 'clipboard receives the untouched complete original path')
check('private Vector2 mobileSaveLocationScroll' in source and 'BeginTouchScroll("mobile-save-location"' in source and 'EndTouchScroll();' in source, 'long paths and help use shared drag scrolling')
check(source.index('EndTouchScroll();') < source.index('layout.FooterButton(0, buttons)'), 'copy/back remain outside scrolling content')
check('#if UNITY_IOS || UNITY_ANDROID\n            const int buttons = 2;' in source, 'mobile platform exposes fixed copy/back actions')
desktop = source[source.index('#if !UNITY_IOS && !UNITY_ANDROID'):source.index('#endif', source.index('#if !UNITY_IOS && !UNITY_ANDROID'))]
check('Application.OpenURL' in desktop and 'Directory.Exists(path)' in desktop, 'desktop directory request is isolated to supported desktop branch')
check(source.count('Application.OpenURL') == 1, 'iOS/Android never attempts a desktop file URL')
check('已请求系统打开存档目录' in source, 'UI reports a system request rather than claiming external file-manager success')
check('saveReturnPause ? "返回暂停菜单" : "返回冒险"' in source and 'ClosePanel(); BlockUITransition();' in source, 'back preserves existing pause-origin lifecycle')
check('mobileSaveLocationStatus' in source and 'CancelMobileScroll(); mobileSaveLocationScroll = Vector2.zero;' in source, 'full failures are visible in measured body after capture reset')
check('.delete-pending' in source and '.tmp' in source, 'migration guidance preserves recoverable temporary files and deletion markers with saves and backups')
check('if (identityChanged) mobileSaveLocationStatus = null;' in source, 'rotation remeasures the path without discarding an action error')
check('session.Progression.HasActiveSave ? mobileSaveDisplayFile' in source, 'detached deleted slot is not presented as an available character file')
check(all(action not in source for action in ['File.Write', 'File.Delete', 'Directory.Create', 'Progression.Save(', 'SaveAsNewSlot(']), 'location utility cannot mutate saves or add another save route')
print(f'PASS: {checks} mobile save-location source contracts (not Unity execution)')

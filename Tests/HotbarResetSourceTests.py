from pathlib import Path
root=Path(__file__).resolve().parents[1]
s=(root/'Assets/Scripts/UI/GameUI.cs').read_text()
block=s[s.index('private void DrawBindings()'):s.index('private void DrawPause()')]
assert 'Feedback(session.Progression.ResetHotbarKeys(), "已恢复默认技能按键")' in block
assert 'SetHotbarKey(' not in block
print('PASS: default-key reset dispatches one production transaction')

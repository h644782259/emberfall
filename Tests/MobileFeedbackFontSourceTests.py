from pathlib import Path
r=Path(__file__).resolve().parents[1]
s=(r/'Assets/Scripts/UI/MobileControls.Feedback.cs').read_text()
assert 'font=GameFont.Shared' in s
assert (r/'Assets/Resources/Fonts/NotoSansSC-Regular.otf').is_file()
print('PASS: mobile Chinese availability labels route through bundled iOS GameFont')

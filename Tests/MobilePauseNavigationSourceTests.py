from pathlib import Path
root=Path(__file__).resolve().parents[1]
ui=(root/'Assets/Scripts/UI/GameUI.cs').read_text()
update=ui.split('private void Update()',1)[1].split('private void OnDestroy()',1)[0]
# Exit confirmation and active panel/save/other modal ownership retain priority;
# returning from pause subpages happens before gameplay cancellation or unpause.
assert update.index('if(exitRequest.Open)') < update.index('if (ReturnToMobilePauseRoot()) return;')
assert update.index('backConsumedFrame=Time.frameCount') < update.index('if (ReturnToMobilePauseRoot()) return;')
assert update.index('if (ReturnToMobilePauseRoot()) return;') < update.index('if (hotbarPointerSlot >= 0)') < update.index('charge.Cancel();')
assert update.index('if (ReturnToMobilePauseRoot()) return;') < update.index('else session.SetPaused(!session.Paused);')
print('PASS: mobile pause Back preserves modal and frame-consumption priorities')

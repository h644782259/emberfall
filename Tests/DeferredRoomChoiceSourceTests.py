from pathlib import Path
r=Path(__file__).resolve().parent.parent
read=lambda p:(r/'Assets/Scripts/Core'/p).read_text()
t=read('GameSession.RoomTactics.cs');c=read('GameSession.RoomChain.cs');g=read('GameSession.cs')
gate=t[t.index('private void OpenRoomGate()'):]
assert 'pendingRoomChoice.Request' in gate and 'PrepareRoomChoice' not in gate and 'UpdateTimeScale' not in gate
assert 'if(TryOpenPendingRoomChoice())return;' in t
assert 'Time.frameCount,valid,!InputBlocked)' in t and 'Player.CombatEpoch' in t
assert 'InputBlocked||pendingRoomChoice.Pending' in c
assert 'pendingRoomChoice.Cancel();' in c[c.index('private void ResetRoomChain'):c.index('private void BeginRoomChainScene')]
assert 'pendingRoomChoice.Cancel();' in c[c.index('private void FinalizeRoomChain'):]
assert 'pendingRoomChoice.Cancel();' in g[g.index('public void OnPlayerDied'):g.index('public void Respawn')]
print('PASS: 7 deferred room-choice production host wiring contracts')

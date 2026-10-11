from pathlib import Path
root=Path(__file__).resolve().parents[1]
def read(path):return (root/'Assets/Scripts'/path).read_text()
room=read('Core/GameSession.RoomChain.cs');tactics=read('Core/GameSession.RoomTactics.cs');player=read('Combat/PlayerController.cs')
assert 'PrepareRoomChoice(2' in room and 'PrepareRoomChoice(1' in tactics
assert 'Room.Index==0&&RunChoices.CompletedWave==0' in tactics
confirm=room[room.index('private bool ConfirmRoomInterlude'):room.index('private void FinalizeRoomChain')]
assert 'RoomChainRun.Finished' in confirm and 'IsDead' in confirm and 'Paused' in confirm and '!RunChoices.Choose(index)' in confirm
travel=room[room.index('public bool EnterNextRoom'):room.index('private bool ConfirmRoomInterlude')]
assert 'InputBlocked' in travel and 'ResetCooldown' not in travel and 'RetireCombatForWorldTransition' in travel
retire=player[player.index('internal void RetireCombatForWorldTransition'):player.index('public void ResetCooldownsForDungeonEntry')]
assert 'ResetCooldown' not in retire and 'Reset()' not in retire
for f in ['UI/GameUI.Expedition.cs','UI/GameUI.MobileBlessings.cs']:
 assert 'RunChoices.Association' in read(f) and 'RunChoices.UsableRanks' in read(f)
for f in ['UI/GameUI.Expedition.cs','UI/GameUI.MobileWorkshop.cs']:assert 'CampRouteCards.Describe' in read(f)
print('PASS: room checkpoint lifecycle, cooldown preservation and desktop/mobile route wiring')

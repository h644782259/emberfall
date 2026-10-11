from pathlib import Path
root=Path(__file__).resolve().parents[1]
s=(root/'Assets/Scripts/Core/GameSession.Expedition.cs').read_text()
r=(root/'Assets/Scripts/Core/GameSession.RoomChain.cs').read_text()
available=s[s.index('public bool SideEventAvailable'):s.index('private int runSeed')]
assert 'DoorUnlocked' not in available and 'RoomTactics.EventRoom(runSeed)' in available
start=s[s.index('public bool StartSideEvent()'):]
assert 'SideEventRun.HasRoomCapacity(Enemies.Count)' in start
assert 'roomEnemies.Add' not in start and 'RoomChainRun.Register' not in start
assert 'sideEventRun.Register(guardian)' in start and 'sideEventRun.Register(wisp)' in start
assert start.index('TrySafeSpawn')<start.index('sideEventRun=new SideEventRun')
assert 'TryGrantSideEventReward(receipt,out newlyCommitted)' in s and 'if(already||!newlyCommitted)continue;' in s
assert 'Player.CombatEpoch==pending.Epoch' in s and 'object.ReferenceEquals(pending.Source,Progression)' in s
assert 'AbandonSideEvent();' in r
assert 'pendingSideRewards.Clear' not in r and 'pendingSideRewards.Clear' not in s
assert '全灭：1材料+补给' in (root/'Assets/Scripts/World/WorldBuilder.TacticalRooms.cs').read_text()
print('PASS: optional first-contact, bounded separate roster, durable receipt and epoch-scoped supply wiring (source contracts only)')

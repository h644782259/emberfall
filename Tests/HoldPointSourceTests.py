from pathlib import Path
r=Path(__file__).resolve().parent.parent
read=lambda p:(r/p).read_text()
s=read('Assets/Scripts/Core/GameSession.Modes.cs');a=read('Assets/Scripts/World/ArenaHazards.cs');ui=read('Assets/Scripts/UI/GameUI.Modes.cs');mobile=read('Assets/Scripts/UI/GameUI.Mobile.cs')
assert 'ExpeditionModeState.ContestsHoldPoint' in s and 'enemy.NavigationRadius' in s and 'sqrMagnitude<36' not in s
assert 'ExpeditionModeState.InsideHoldPoint' in s and 'Circle(rings[2],Vector3.zero,ExpeditionModeState.HoldPointRadius)' in a
assert 'session.ModeRun.HoldStateLabel' in ui and 'session.ModeRun.Mode==ExpeditionModeKind.HoldPoint?TouchRect(l.AdventureStatus)' in mobile
assert 'var objective=session.RoomObjectiveView;' in ui and 'RoomObjectivePresentation.Create' in read('Assets/Scripts/Core/GameSession.RoomTactics.cs')
print('PASS: 4 hold-point host/geometry/mobile wiring contracts')

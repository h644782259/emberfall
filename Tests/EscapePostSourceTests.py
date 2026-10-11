from pathlib import Path
root=Path(__file__).resolve().parents[1]
room=(root/'Assets/Scripts/Core/GameSession.RoomChain.cs').read_text()
enemy=(root/'Assets/Scripts/Combat/EnemyController.cs').read_text()
assert 'bool escape=plan.Objective==RoomObjective.Escape;' in room
assert 'escape?EscapeRoomFormation.TrySpawn(runSeed,index,occupied,out point)' in room
assert 'if(escape)enemy.ConfigureEscapePost(EscapeRoomFormation.Role(index),point);' in room
assert 'index==1||index==3?EnemyKind.Guardian:index==2||index==4?EnemyKind.Goblin:EnemyKind.Slime' in room
assert 'ReturnToEscapePost(dt,effectiveSpeed,combatTargetPosition)' in enemy
assert enemy.index('ReturnToEscapePost(dt,effectiveSpeed,combatTargetPosition)')<enemy.index('else if (aggro)')
start=enemy.index('private bool ReturnToEscapePost');end=enemy.index('{',start)+1;depth=1
while depth:
    depth+=(enemy[end]=='{')-(enemy[end]=='}');end+=1
body=enemy[start:end]
assert 'escapePost==null' in body and 'route.Direction(transform.position,escapePostPosition,NavigationRadius)' in body
assert 'CancelAttack();companionTarget=null;' in body
assert 'ConfigureEscapePost' not in (root/'Assets/Scripts/Core/GameSession.Modes.cs').read_text()
print('PASS: Escape-only roles/spawns and post-return controller routing (source contracts, not Unity behavior)')

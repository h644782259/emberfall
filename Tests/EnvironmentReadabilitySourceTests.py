from pathlib import Path
root=Path(__file__).resolve().parents[1]
read=lambda p:(root/'Assets/Scripts'/p).read_text()
a=read('World/ArenaHazards.cs');enemy=read('Combat/EnemyAttackTelegraph.cs');camera=read('Core/AdventureCamera.Visibility.cs');fade=read('Core/CameraOcclusionSurface.cs')
assert 'ThreatVisualStyle.Material()' in a and 'ThreatVisualStyle.Material()' in enemy
assert 'ArenaPulseRules.Radius' in a and 'ArenaPulseRules.Contains' in a
assert 'CombatSight.FillAreaBoundary(outline' in a and 'CombatSight.Area(centers[i],session.Player.transform.position)' in a
assert 'ArenaPulseRules.Progress(phase)' in a and 'clocks[i].positionCount=count' in a
assert all(x in a for x in ['Thorn source glyph','Heat source glyph','Eclipse source glyph'])
assert 'ArenaPulseRules.HoldSegments(session.ModeRun.ObjectiveProgress)' in a and 'ExpeditionModeState.HoldPointRadius-.22f' in a
assert 'Vector3.up*1.35f' in camera and 'Vector3.up*.18f' in camera and 'game.Player.AimTarget' in camera
assert 'Protects(bounds,camera,torso)' in fade and 'Protects(bounds,camera,feet)' in fade and 'protectTarget&&Protects(bounds,camera,target)' in fade
assert 'ReserveGroup(ref available,needed)' in fade and 'admittedGroups.Contains(surface.group)' in fade
building=read('Core/BuildingOcclusionGroup.cs')
assert 'renderer.bounds.max.y<=center.y+.5f' in building and 'renderer.GetComponent<TextMesh>()!=null' in building
assert 'size.x*.5f' in building and 'size.y*.5f' in building and 'WorldTraversal.' not in building
label=read('World/WorldLabelPresentation.cs')
assert 'visual.localBounds.size.y' in label and 'WorldLabelReadability.Scale(pixels,lines,density)'  in label and 'GameFont.Apply(label)' in label
assert 'transform.rotation=camera.transform.rotation' in label
print('PASS: hazard shared boundary/clock/source glyphs, hold state, camera points/group caps, footprint and label wiring; root owns label/group creation hooks')

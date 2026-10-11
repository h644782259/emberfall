#!/usr/bin/env python3
"""Lifecycle/budget/input wiring checks; not shader or camera render tests."""
from pathlib import Path
root=Path(__file__).resolve().parent.parent
fade=(root/'Assets/Scripts/Core/CameraOcclusionSurface.cs').read_text()
camera=(root/'Assets/Scripts/Core/AdventureCamera.Visibility.cs').read_text()
controls=(root/'Assets/Scripts/UI/MobileControls.cs').read_text()
hud=(root/'Assets/Scripts/UI/GameUI.Mobile.cs').read_text()
checks=0
def check(ok,message):
    global checks
    assert ok,message
    checks+=1
check('surfaces.Count<CameraVisibilityRules.MaximumSurfaces' in fade,'Registration is bounded')
check(fade.index('needed>CameraVisibilityRules.MaximumFaded-fadedCount')<fade.index('new Material(source)'), 'Fading refuses resource admission before cloning')
check('if(surface.requested)LastOccluders++' in fade and fade.index('if(surface.requested)LastOccluders++')<fade.index('ReserveGroup(ref available,needed)'), 'Position-marker signal survives exhausted fade slots')
check('current[j]=saved[j]' in fade and 'Destroy(owned)' in fade, 'Original material restored and owned clone destroyed')
check('private void OnDisable(){surfaces.Remove(this);Restore();}' in fade, 'Disable returns registry/material slots immediately')
check('private void OnDestroy(){surfaces.Remove(this);Restore();}' in fade, 'Destruction cleanup is idempotent')
check('original.color=' not in fade and 'FindObjects' not in fade, 'No mutation of shared world material and no global renderer scan')
check('CameraOcclusionSurface.LastHeroOccluders==0' in camera and 'GUI.Label(marker,"角色"' in camera, 'Independent player-position fallback marker')
check('EffectPreferences.TouchOpacity' not in camera, 'Occlusion marker cannot be hidden by button-opacity setting')
check('MobileControls.Active&&game!=null&&game.HasStarted' in camera, 'Title/character selection retains its original camera framing')
check('ResetProjectionMatrix()' in camera and 'layout.CombatView' in camera, 'Projection uses verified clear region and resets on teardown')
check('ScreenPointToRay' not in camera and 'Physics.' not in camera and 'Input.' not in camera, 'Visibility adjustment does not replace aiming or touch input')
check('Rect hit=hotbarSlots[i];blockedRects.Add(hit);Rect r=MobileVisualRect(hit)' in hud, 'Visual size and interaction size remain separate')
check('cachedPosition!=EffectPreferences.TouchPosition' in controls, 'Layout preferences invalidate shared input/render geometry')
print('PASS:',checks,'camera visibility/resource/input source contracts')

check('surface.requested=surface.heroRequested||surface.targetRequested' in fade,'Hero and target keep separate demand with a shared fade union')
check('if(surface.heroRequested)LastHeroOccluders++' in fade and 'if(surface.targetRequested)LastTargetOccluders++' in fade,'Separate counters survive clone admission failure')
check(fade.count('LastHeroOccluders=0;LastTargetOccluders=0;')==3,'Reset, tick and restore clear both causes')
arena=(root/'Assets/Scripts/World/WorldBuilder.ChallengeArenas.cs').read_text()
check('BuildingOcclusionGroup.Configure(cover.transform,p,new Vector2(5,1.2f));WorldTraversal.AddBox(p,new Vector2(5,1.2f))' in arena,'Cover grouping uses unchanged solid navigation footprint')
check('Primitive(cover.transform,"Quarry opaque footing"' in arena and 'Ribbon(parent,r,"Molten fracture"' in arena,'Opaque foot survives group fade; molten hazard is outside the group')
print('PASS:',checks,'including independent obstruction causes and lava cover classification')

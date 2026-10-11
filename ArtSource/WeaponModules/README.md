# Weapon modules

The original `ArtSource/BlenderPilot/Emberfall-Pilot-Props.blend` contains the joined **StarcoreSword**. `build.py` reads that editable source, splits its connected blade / swept guard / wrapped grip / pommel components, normalizes their mesh frames and exports four rigid modules into editable `WeaponModules.blend`. The source `.blend`, FBX, atlas and GUIDs are retained.

Actual default path: `CombatModel.Hero` → `ApplyWeaponArt(null)` and `ApplyEquipment` → `ApplyWeaponArt(weapon)`. A null/base weapon or the exact unupgraded common level-1 初行长剑 uses all four source pieces. They reuse existing renderers, wrist hierarchy and palette. Missing/malformed any one piece gives the complete procedural sword. Other gear retains its tier blade and guard; this does not enable the incomplete imported character. `WeaponModules.Enabled=false` plus the normal refresh restores saved original meshes/scales. Shared new meshes are cache-owned, original owned meshes retain their owner, and repeated equipment changes do not own/destroy shared resources.

Blade length remains `WeaponStructure.SwordTip - SwordRoot`; tip/root are not inferred from FBX bounds. Public sword anchors (pommel, grip, guard, root, tip) and ribbon root/tip attachment remain unchanged. The source's sixth `Anchor_Emission` remains in the editable source: the public runtime enum has five sword anchors and the ribbon consumes root/tip; no new emission gameplay/attachment API is introduced. Bow string, arrow nock, draw timing, caster wrist/core/prong identity and both caster classes are retained. Held-arrow mesh simplification is only inside the Bow rig; flying projectiles are untouched.

The remaining modules are a bowed six-sided joint, a shared six-sided shaft/ornament profile, a faceted gem and a mechanical hoop. Existing names, part counts, placement, tier/upgrade rules and material palette are retained. T4 sword blade and guard remain procedural. Mechanical weapon ornaments keep their ring identity; back wings and fashion catalog are unchanged. Module exports retain the 512-triangle cap and weapon sets the 800-triangle cap, without added textures, materials or renderers. No damage, range, collision, progression or timing code is edited.

## Rebuild and maintained checks

From repository root:

```sh
blender -b -t 2 --python ArtSource/WeaponModules/build.py
# Or export hand-edited canonical meshes, retaining meta GUIDs:
blender -b -t 2 ArtSource/WeaponModules/WeaponModules.blend --python ArtSource/WeaponModules/export_blend.py
python3 Tests/WeaponModulesProductionTests.py /path/to/dotnet
python3 Tests/WeaponContactProductionTests.py /path/to/dotnet
```

The production checks exercise the actual factory, resource decoder and adapter, including class/tier/fashion combinations, bounds, anchors, geometry caps and missing-resource fallback. Contact checks exercise production AnimateHero/AimArm, caster pose code and ribbon sampling, including bow string/nock draw agreement. Legacy equipment/fashion suites disable the visual adapter to exercise fallback construction.

Managed checks do not establish Unity importer, rendering, device FPS, Frame Debugger behavior or compressed player package size. In-game appearance and device cost require Unity/device review.

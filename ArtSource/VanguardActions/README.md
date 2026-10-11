# Swordguard complete rig / authored local motion layer

The default combat presentation uses the existing articulated body plus Blender ActorModules. Equipment, upgrades, fashions and tactics remain separate objects on that rig. The imported five-clip whole-body pilot is a bounded preview; it is not a complete combat replacement.

The authored layer supplies 13 original Blender-baked additive pose channels, sampled on the actual existing rigid joints: idle; forward/back/left/right gait; left/right turning; basic; first active; jump; landing; hit; death terminal pose. It **enhances the existing complete actions**, not 13 newly authored full-skeleton animations. Later active skills and charge keep their existing complete poses. No hitbox, displacement, damage, action duration, contact time or control time is changed. Accepted gait phase, local velocity, turn load, jump tuck, landing impulse and existing action progress remain authoritative. Hit is a short additive recoil, never an action cancellation. Death's existing root fall remains PlayerController-owned; local joints apply a static authored terminal pose even when the death screen freezes scaled time; this is not a continuous animation during pause.

`CombatModel.VanguardArt` selects the complete binary library once when building the character. Unsupported classes, missing joints, disabled authored modules, missing or invalid binary choose the complete existing motion path. Equipment replacement never changes that decision or swaps the character body. The partial applies before existing visual recovery so recovery retains final priority.

Original source: `build.py` key poses, no external asset/license/purchase. `Swordguard-Actions.blend` retains individually named editable actions with fake users. Bake with Blender 4.3.2 (or export hand-edited named actions from the saved .blend with `export_blend.py`):

```
blender -b --python ArtSource/VanguardActions/build.py
python3 Tests/VanguardActionsProductionTests.py /path/to/dotnet
python3 ArtSource/VanguardActions/export_actions.py /tmp/vanguard-actions /path/to/dotnet
blender -b ArtSource/VanguardActions/Swordguard-Actions.blend --python ArtSource/VanguardActions/export_blend.py
python3 Tests/VanguardRecoveryIntegrationTests.py /path/to/dotnet
```

Resource contract: one binary contains 13 channels × 33 samples × 9 local Vector3 rotations plus its header. The layer adds no triangles, materials, textures or renderers and performs no runtime curve parsing or per-frame allocation. Sampling uses bounded local rotation additions. Decoder rejects truncated, oversized, schema-mismatched, non-finite or over-budget data as a whole. Stable resource `.meta` GUIDs are checked in. Source animations use linear interpolation; runtime resamples linearly.

The maintained production checks cover the real decoder and adapter, routed poses, stable body/joint identity, interpolation and missing-library fallback. The recovery integration suite joins factory construction, authored sampling, action cancellation, equipment changes, visual recovery and paused-death ownership. Managed transform/UI/save boundaries are explicit; native animation, rendering and device acceptance remain separate.

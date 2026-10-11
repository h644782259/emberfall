# Four skill visual families

Four independent identity silhouettes are original and editable in `Four-Skill-Identities.blend`; `build.py` deterministically exports strict Unity-axis buffers with stable GUIDs. They reuse the existing shared material, gameplay calls, cover clipping, tier limits and lifecycle. No textures or external art sources.

| Family | Geometry | Runtime integration |
|---|---|---|
| BladeSlices | Three separated closed blade slices | Existing `Crescent` release/advanced finale uses one clipped primary instead of two broad crescents. Vanguard pending charge uses this identity. Starting whirlwind/groundshock retain their separately implemented presentation. |
| ForkPulse | Central bolt with two side forks and secondary branches | Lightning `Impact` primary; advanced chain-lightning charge; actual enemy endpoint only after `Health` decreases. The dynamically calculated lightning line remains. No-target cast does not emit a fake hit endpoint. |
| ContractSigil | Central diamond, mirrored open brackets, terminal diamonds | Summoner first active release and confirmed enemy contact; actual companion contract emergence and mark finisher through `Impact(Summon)`; pending contract charges. |
| ProtectionCage | Four open arches and a crown diamond | Healing preparation and actual heal ticks. Replaces generic beam decorations after `Heal` executes; final-rank energy restoration continues. No extra healing or damage occurs. |

The four shared meshes use the existing single shared material and no textures. Blade primary uses one renderer; other families use one identity renderer at contact/preparation. Reduced mode retains the same identity primary; 14/10/7 part limits and global lease limits remain authoritative. Runtime cover subdivision stays under its existing independent caps.

Preparation is owned by the existing cancellable charge/Rune parent. `SkillChargeController` clears that parent on cancel and after actual release; no cosmetic timer triggers damage. Family motion is visual only: blade contracts after release; contract settles over .08 seconds; fork opacity pulses within its .6-second contact lifetime; protection settles over .1 seconds. Anchored families only contract radially, retaining certified cover rays. Dynamic damage boundaries and beam endpoints remain runtime calculations. Missing identity data returns to the previous charge or impact mesh; missing direct contact identity allows the original caller presentation.

The contact gates use actual health reduction for lightning and the Summoner first-active enemy feedback. Their initial release visuals are ActionBody priority; confirmed enemy contacts and heal-event cues use RealContact priority. Contract summon-emergence requires an actual partner. No cooldown, rank, energy, damage, stun, healing amount or scheduled event time is changed.

## Rebuild and maintained checks

From repository root:

```sh
blender --background --python ArtSource/BlenderSkillIdentities/build.py
python3 ArtSource/BlenderSkillIdentities/validate.py
python3 Tests/AuthoredSpellIntegrationTests.py /path/to/dotnet
python3 Tests/SkillIdentityCallsiteProductionTests.py /path/to/dotnet
```

`Tests/AuthoredSpellIntegrationTests.py` loads real resources through the production decoder, component, clipping and lease code. It checks identity families in preparation/release/hold/fade, mobile/reduced and near-wall/open conditions, missing-data fallback and source-mesh ownership. Mutation controls check resource loading, reduced budgets and clipping.

`Tests/SkillIdentityCallsiteProductionTests.py` executes the production `Healing`, `SkillChargeController` and `AdvancedSkillVfx` paths with loaded identity meshes. Checks cover healing and rank-3 energy continuation; Begin, pause, cancel, release, epoch cancellation and component disable; terminal effect/lease cleanup and single release commitment. It also exercises actual `ChainLightning` and Summoner slot-0 bodies across damaging hit, immunity, zero input damage, corpse, killing blow and no-target cases. Only actual health reduction routes a RealContact identity; dynamic lightning may retain its calculated line without a confirmed damage endpoint. Mutation controls check effect cleanup and health-decrease gates.

These are managed/API-boundary checks. Shader appearance, depth sorting, device allocation/performance and gameplay composition require Unity/device review.

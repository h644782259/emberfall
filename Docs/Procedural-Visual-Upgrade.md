# Procedural visual refinement

## Implemented

- Shared authored bevelled boxes and rounded sphere/capsule/cylinder meshes replace raw primitive components in character and environment construction. Visual creation does not create a collider. Original primitive envelopes and existing traversal registrations remain separate.
- Cloth, skin, wood, stone, metal, and crystal have distinct smoothness/metalness/emission responses. Metal edges now catch light; crystal accents retain their colour. Materials are reused within a character or world resource owner.
- The hero cloak is a curved, double-sided 7×7 drape with a pinned shoulder seam, authored folds, speed-responsive trailing, and smooth secondary movement. It uses reusable vertex buffers rather than cloth physics.
- Guardians have a broader layered chest/shoulder silhouette and an ember focus; goblins gain brows/nose detail. Humanoid enemies have separate elbows, knees, spine, pelvis, and neck. Their visual anticipation/recovery follows the existing controller signals without changing warning/damage timing. Wolf companions use four articulated legs with diagonal gait and a separate swaying tail.
- Smooth, deformed boulders and rounded evergreen crowns replace box rocks and stacked seven-sided cones. Camp props gain a domed provision coffer, bronze bands/clasp, and gently animated flames. Tri-light ambient colours, a modest cool silhouette fill, and tuned main-light shadows add depth.
- Sword arcs use tapered curved ribbons and eased expansion. Projectiles use rounded lit bodies and smoother trails. Meteors use the shared irregular boulder mesh with a warm emissive surface and accelerating visual fall.
- A hit fan uses one mesh/renderer instead of five to twelve separate spark renderers. Actual critical-hit flags still drive the longer golden fan and ring. No cosmetic random roll changes damage or rarity.

## Bounds and ownership

- Four shared primitive meshes plus one shared boulder mesh; no external textures, models, packages, or licensed assets were added.
- Default mesh sizes: bevelled box 216 vertices/300 triangles; sphere 221/384; capsule 238/416; cylinder 102/96; boulder 117/192.
- The single hero cloth uses 98 vertices and 144 triangles. Arrays are allocated once; only positions/normals/bounds update while scaled game time advances.
- Hit fans retain the 24-effect simultaneous cap and use at most 136 vertices per critical fan. Transient rings/ribbons are capped at 32 on mobile, 48 on desktop. Persistent area boundaries and enemy telegraphs are not suppressed by that transient cap.
- Decorative point lights have no shadows and use vertex lighting. Only the main directional light casts shadows; the cool fill does not.
- Reduced-effects settings shorten projectile trails, reduce hit rays/reach and remove the critical halo. No full-screen flash was introduced. Existing camera-shake preference remains respected.
- Actor/world-owned materials and transient meshes are destroyed by their owner. The fixed shared mesh cache is reset at Unity subsystem registration.

## Verification

Managed production recipe tests cover deterministic geometry, primitive bounds, finite/unit normals, valid indices, outward triangle winding, clamped allocation limits, and cloth pinning/bounds/continuity. The test uses a managed vector/math shim, not Unity graphics execution.

API compilation does not establish Unity Play Mode, shader/frame output, GPU profiling, device behavior or real-device readability.

## Follow-up visual acceptance in a runnable Editor

1. Compare all four heroes at normal gameplay camera distance, walking, turning, attacking, charging, being hit, and changing equipment/fashion. Check cloak seam, clipping, metal highlights, and weapon trails.
2. View guardian windup/release, ordinary goblin gait, wisp/slime motion, and four-legged wolf motion. Verify no visual anticipatory strike is mistaken for a damage-frame change.
3. Traverse both environments and the bridge; inspect rounded objects, ground contact, shadows, portal light and labels. Traversal/attack warnings must retain clear contrast.
4. Test simultaneous normal/critical hits at full and reduced effects, pause/resume, and room reload. Confirm no growth in retained meshes/materials and no lost persistent area boundaries.
5. Profile representative populated rooms on the lowest supported iOS device. Check MSAA and shadow settings from the integration's rendering profile; tune from measured GPU cost rather than these geometry bounds alone.

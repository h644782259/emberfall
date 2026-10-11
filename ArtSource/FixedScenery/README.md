# F4 fixed scenery

27 original Blender 4.3.2 modules are authored by `build.py`. `Emberfall-Fixed-Scenery.blend` retains the editable source meshes. No external models, textures, rigs or materials are used. Assets have one submesh and use the existing palette. Low quality uses the same bounded meshes; the loader adds no material instances.

Coverage:

1. Pillar: stepped chamfered base, fluted taper, capital, collar, band. Original five objects, sizes, 1.4 navigation box and two occlusion marks remain.
2. Portal: segmented cut-stone fixed frame and tiered plinth. Jade moving inner light, crystals, motes, focus and interaction remain original runtime builders.
3. Workshop: ten raised tile courses, shaped eaves and ridge, recessed chimney cap. Original seven roof pieces remain on original BuildingOcclusionGroup. Observatory dome/spire already have a useful silhouette and are retained.
4. Town facilities: inset shelves, waisted anvil and stump, lectern, concentric observatory dais. Original forge gantry and live astronomical crown retained. NPC station furniture uses the same modules, no duplicate artwork.
5. Four facilities: one shared pedestal and four small identifying fixed crests beneath original animated progression crystals. Original progress, palette, names and navigation unchanged.
6. Three NPC roles: shared faceted tunic, sleeve, boots and head, merchant brim/crown and smith apron. Existing arm transforms, idle components, hammer and chart animation unchanged. No face or skeleton replacement.

Production wiring is confined to three calls in WorldBuilder: primitive mesh substitution by exact existing name; Gate frame mesh with original line fallback; optional fixed crest. Meshes are cached and destroyed on subsystem reset. `Enabled=false`, missing data or rejected decoding retains original meshes; missing crests are simply absent. Existing transforms, material ownership and registry markings remain authoritative. Stable checked-in resource `.meta` GUIDs must be preserved when editing or rebuilding assets. No navigation, collision, damage, timing or interaction fields are changed.

## Rebuild and checks

Run from the repository root:

```sh
blender -b -t 2 --python ArtSource/FixedScenery/build.py
python3 Tests/FixedSceneryProductionTests.py /path/to/dotnet
python3 Tests/SceneryPresentationProductionTests.py /path/to/dotnet
```

The maintained checks cover the actual decoder/resources and factory recipes, enabled/disabled navigation and occlusion membership, malformed data fallback, mesh attachment and collider isolation. The scene tests use explicit managed Unity boundaries; native rendering, device performance and on-device occlusion require separate engine acceptance. Generated results remain in ignored local output directories.

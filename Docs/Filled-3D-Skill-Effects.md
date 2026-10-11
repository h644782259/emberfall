# Filled, evolving 3D skill effects

## Visual reference and assets

The supplied 4×4 character reference was inspected directly. Its useful motion language is a thick blue crescent changing into a directional thrust, then a widening ice eruption. The character drawing is not copied or imported. The new effect foundation is authored world-space geometry with thickness, facet normals, colour gradients and evolving volume silhouettes.

## What changed

- Ordinary slash: closed diamond-section crescent with a broad cutting face, tapered tips, a secondary offset surface and released chips. The volume sweeps and thins over 0.34 seconds.
- Ice: individually delayed, faceted spears erupt from the floor, spread through the actual area, hold their shape briefly, then sink/fade. This is a surface-filled eruption, not a set of line segments.
- Fire: curved tapered tongues expand upward and outward, twist and dissolve over 0.8 seconds, with a filled impact wave.
- Summon: six upright curved shell petals rise and open away from the real partner's position, revealing the companion. No extra summon or damage is created.
- Thrust/advanced beam: a tapering filled volume and a leading spear follow the actual world-space path. It replaces the old zig-zag line combination.
- Charge/buff envelope: three low-opacity curved surfaces rotate and contract around the caster/target. The owning charge controller or area can destroy it immediately on cancellation. It is visually distinct from a damage impact.

`AdvancedSkillVfx` no longer constructs line renderers or its former stacks of runic rings/glyphs. Ground range markers and enemy telegraphs remain separate semantic boundaries. Their presence does not serve as the primary skill art.

## Timing and ownership

The area controller creates its impact volume on the first actual damage tick, after startup. Fire/poison/lightning field emission now also waits for that tick. Frost nova, meteor impact and advanced elemental sequence hits have explicit hooks. Summon emergence occurs only when `CastContract` returns a real partner. The advanced falling-blade event now shows its descending strike and ground rupture immediately at the hit event, rather than visually arriving after damage already landed.

Effects perform no damage rolls, physics queries, enemy callbacks, colliders or gameplay timing changes. They use scaled combat time, freeze for input-blocking menus/background pause, and terminate on death, changed hero/epoch or terminal mode. They do not persist across a room transition.

## Resource and readability limits

Three immutable shared meshes (crescent 148 vertices, crystal 36 vertices with hard facets, flame 110 vertices) and one shared material are reused. Default geometry is at most 300/12/216 triangles respectively. No per-frame mesh rebuild or texture animation is required. The envelope and part transforms evolve using fixed buffers and a reused property block.

At most 20 filled effects on desktop or 12 on mobile. Each has at most 14 pieces, reduced to 10 on mobile and seven with reduced effects. Typical crescent uses six pieces; an impact uses at most twelve. These are allocation/draw-call bounds, not measured performance claims. Disabled effects release their active cap immediately.

The shader uses depth testing, no depth writes and a lower render queue than enemy danger boundaries. Reduced effects preserve the filled silhouette while lowering piece count and opacity. No new screen-wide flash or camera shake is added.

## Reusable validation

Production geometry checks cover finite/bounded vertices, valid UVs/indices, nondegenerate faces, outward positive volume and closed crescent/flame topology. Timing checks cover envelopes, terminal opacity and invalid inputs; source checks cover integration, run-combat and large-boss hooks. Runtime C# API compilation is a separate check.

Unity validation must cover shader import/compilation, rendered colour and occlusion, timing feel, small-screen readability, GPU overdraw and device frame times. Managed checks do not establish those visual or device results.

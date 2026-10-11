# Natural skill materials

Generated with the built-in imagegen tool on 2026-10-11. Both images are used on three-dimensional meshes; flame transparency is preserved. Palette and surface contrast are matched to the soft light and muted foliage of the existing camp.

- `Assets/Resources/SkillMatter/NaturalMatterAtlas.png`: steel, charred volcanic rock, wet mud, ice, wood, leather, fur and vine tissue.
- `Assets/Resources/SkillMatter/NaturalFlame.png`: turbulent transparent combustion detail for solid flame tongues and projectile shells.

The generated 1254×1254 atlas has four columns and a row boundary at pixel 627 (normalized y=0.5), verified from the source pixels. The material shader uses inset per-tile UV rectangles, continuous mirrored repetition and derivatives from unwrapped coordinates to avoid tile bleeding and repeat-seam grids. Detail coordinates and blend weights are in object space, so textures rotate and move with the body.

## Atlas prompt

Use case: stylized-concept
Asset type: production game material texture atlas for three-dimensional combat spell objects in a Unity fantasy game.
Primary request: Generate one square image with EXACTLY 4 columns and 2 rows of equal square material tiles, edge-to-edge, no borders or gutters. Top row left to right: 1 aged forged steel with fine longitudinal scratches and sparse pitting, neutral silver-gray; 2 charcoal-black volcanic crust with irregular dim orange ember fissures; 3 saturated wet swamp mud with tiny olive moss fragments, granular dark brown green soil and scattered water sheen; 4 cloudy translucent blue-gray glacial ice with small internal bubbles and irregular veins. Bottom row left to right: 5 weathered warm brown wood grain, coarse fibers; 6 matte brown leather with subtle creases and small pores; 7 short gray-brown wolf fur, visible individual clumped fibers; 8 dark olive vine bark and leaf tissue, organic ridges and mottled chlorophyll.
Style/medium: believable physically grounded natural surface scans, slightly simplified detail to suit a softly lit low-poly fantasy forest camp with muted yellow-green grass, gray-blue rocks and brown tree trunks. Materials must read as real matter on solid 3D meshes, never painted symbols.
Composition/framing: each tile is a close-up flat surface, orthographic, uniform diffuse light, no perspective, no object silhouette. All eight tiles tile seamlessly within themselves. Keep average brightness moderate and color natural. 2048x2048 preferred.
Constraints: no cast shadows, no studio lighting hotspots, no text, no numbers, no labels, no magical runes, no geometric ornament, no framing, no checkerboard. EXACT 4x2 equal tiles.

## Flame prompt

Use case: stylized-concept
Asset type: alpha texture for a solid three-dimensional flame mesh in a Unity fantasy game, supporting surface detail rather than a flat billboard.
Primary request: one natural turbulent flame plume, seen close-up from the side, upright, cropped at its base. Real flame combustion detail with interleaving translucent amber tongues, deep orange lower folds, small pale yellow hot pockets and softly dissipating smoky tips. Flame fills the center 85 percent width and 95 percent height; irregular silhouette.
Style/medium: realistic flame, subdued saturation and softly lit edges so it belongs in a muted low-poly forest game. No illustrative brushwork.
Background: genuinely transparent alpha outside the flame and in gaps; no floor or scene.
Constraints: no black rectangular background, no glow halo, no sparks, no text, no lines, no rings, no symbols, no weapon or person. Keep the orange translucent flame body richly detailed instead of an opaque yellow blob.


## Living leaf surface

`Assets/Resources/WorldArt/LivingLeafSurface.png` was generated with the built-in imagegen tool and copied into both projects. It supplies real leaf-vein surface detail to closed, curved leaf meshes with individual UVs; no billboard or plane is used. The ground moss atlas remains a separate surface.

Use case: stylized-concept
Asset type: seamless natural foliage surface texture for closed three-dimensional leaf meshes in a softly lit Unity fantasy forest. This image will be used as material detail on real meshes, not as a sprite or billboard.
Primary request: a square edge-to-edge macro surface of living broadleaf tissue. Subtle natural branching leaf veins, waxy olive-green skin, fine translucent cell texture, small mottled age marks and muted sage variation. The whole image is green plant tissue; no isolated leaf silhouette, no background. Leaf vein details should vary organically and feel like real photographed matter.
Style: physically plausible natural material scan with moderate contrast, matching muted yellow-green grass, brown trees and gray-blue stones. Soft diffuse light, no baked shadows or painted brush strokes. Scale of detail should suit individual leaves 20-35 cm long.
Constraints: tile seamlessly, no panels, no grid, no border, no symbols, no text, no perspective, no plants or full tree, no repeated big diagonal lines, no neon saturated colors.

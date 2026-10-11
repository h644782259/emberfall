# Refined icon paintings

Generated with the built-in ImageGen tool using current 3D photographs for silhouette/subject identity and the earlier icon paintings for lighting/material style. Exact prompts are in prompts.json and navigation-prompt.json.

- skills-*.png: 5 columns x 2 rows, ten ordered skill identities per class.
- gear-*.png: 4 rarity columns x 5 rows: weapon, armor, relic, crystal wings, ceremonial weapon. ImageGen placed rows at unequal heights; Tools/slice-icon-rows.swift uses reviewed row boundaries and preserves aspect ratio.
- navigation.png: camp return and achievement, two cells.

Runtime images live in Assets/Resources/IconArt/refined-* as 128px RGBA. Skill recovery variants are deterministic luminance copies. No black background or enclosing circular frame is baked into the art. PC draws a square slot; mobile draws a thin identity-colored circle. Original paintings and exact generation prompts are retained here.

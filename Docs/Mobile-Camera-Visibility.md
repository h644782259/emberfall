# Mobile controls and foreground visibility

The 568×320 layout uses all ten skill buttons simultaneously, with independent
48×48 logical-unit hit areas. Icons can shrink to 86% without shrinking input.
Free focus/recall sit in a horizontal pair outside the ten skills. Layout presets
move the skill group within checked limits; the notification card adjusts with
the lower preset. Each tested preset leaves a 96×64 HUD-free rectangle for the
hero and nearby action. Wider layouts prefer a clear anchor near screen center.

The mobile pause menu cycles through ordinary settings and a touch-layout page.
Position, opacity (50/70/100%) and visual size are device preferences, separate
from character saves. Shared layout caching updates both input and drawing.
Buttons remain independently hit-tested even when their visual surface shrinks.
The hero marker and important encounter text do not inherit button opacity.

The camera still follows and looks at the hero. A bounded off-axis projection
places the hero in the verified clear rectangle; it does not modify aim planes,
ray construction or touch conversion. User pitch/orbit rules stay in place.
Marked scenery near the hero smoothly reduces the requested camera distance,
with the existing minimum distance retained. This is not a camera collision solver.

Only explicitly marked world primitives participate in foreground fading:
large rocks, tree crowns, columns and walls, plus building/roof marks supplied by
the town builder. Actors, target markers, hazard warnings and effects are not
scanned or faded. Registry capacity is 256 surfaces; at most 32 can own a fading
material at once. Fade materials are copies, restored/destroyed as the obstruction
clears or the surface disables. Shared originals are never edited. A persistent
screen-space player-position label remains available whenever registered scenery
intersects the camera-to-player line, including when all fade slots are occupied.

The target alpha is 20%. The existing Standard material blend state is used;
there is no new shader or external art dependency. Expanded renderer bounds are
a conservative obstruction approximation, so some scenery can fade before its
visible triangles cover the hero. Very dense scenery beyond the registration cap
is not included. This bounded behavior still needs rendered review.

Validation commands:

```bash
python3 Tests/Run-CombatFeedbackTests.py --dotnet /path/to/dotnet
python3 Tests/CameraVisibilitySourceTests.py
```

The runner includes all three position presets over phone/tablet/high-DPI cases,
clear-region intersections, fade monotonicity/resource constants, zoom bounds and
projection arithmetic. Source contracts check cleanup, material ownership,
preallocation limits and separate visual/input geometry. Reference-API compilation
checks the Unity calls. No Unity process, material/shader rendering, true camera
screenshots, device touches or performance profiling has been executed for this
change; those remain necessary for a visual-quality conclusion.

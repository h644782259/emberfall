# Current terrain on the minimap

The previous minimap cached only wilderness/dungeon plus layout number. Changing hub, breaking a prop or returning to the same layout could retain obsolete obstacles; hard-coded forest river marks were also drawn over the new towns.

The current map tracks the shared traversal revision and arena radius. It rasterizes actual ground, open water and solid geometry, including the actual bridge opening. Broken prop navigation updates invalidate the image. The original forest's road overlay is limited to that forest; invented universal river/corridor/crystal markers were removed from unrelated maps.

One96×96 texture and one reusable pixel buffer are owned by the UI and updated only after a geometry revision. It is destroyed on UI teardown. There is no growing map snapshot collection. Managed tests exercise river/bridge/solid classifications and reset/removal invalidation; source tests verify adapter wiring. GPU output and in-game orientation still require real Unity visual acceptance.

Travel gates now retain the same jade interaction colour in every hub/dungeon instead of inheriting each town's decoration colour. The compact mobile map marks the actual northern dungeon entrance or the ordinary dungeon's southern exit; NPCs remain gold. This is source-verified navigation wiring, not a rendered colour-contrast test.

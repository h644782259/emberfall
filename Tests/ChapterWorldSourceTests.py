"""Render/navigation and landmark ownership contracts; no frame/render assertions."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
r=lambda f:(root/'Assets/Scripts'/f).read_text()
w=r('World/WorldBuilder.cs');c=r('World/WorldBuilder.ChapterRooms.cs');g=r('World/ChapterRoomGeometry.cs')
assert 'int chapterSeed = 0' in w
assert w.index('if(ChapterRoomGeometry.IsChapterLayout(dungeonLayout))')<w.index('else if(dungeonLayout>=20)')
assert 'ChapterRoomGeometry.FromLayout(dungeonLayout,chapterSeed)' in w
assert 'ChapterRoomGeometry.Register(plan);' in c and 'foreach(var obstacle in plan.Obstacles)' in c
assert 'obstacle.Radius*2,obstacle.Height*.5f,obstacle.Radius*2' in c
assert 'new Vector3(obstacle.Size.x,obstacle.Height,obstacle.Size.y)' in c
assert 'obstacle.Center+Vector3.up*obstacle.Height*.5f' in c
assert 'Tree(' not in c and 'Rock(' not in c # no hidden secondary obstacle registration
assert 'MaximumEnemies=6' in g and 'index>=MaximumEnemies' in g
assert 'NearestWalkable' not in g # reject invalid authored arrivals; no silent teleport through solids
assert 'WorldResources owned=root.AddComponent<WorldResources>();' in c
assert 'previous.gameObject.SetActive(false);Object.Destroy(previous.gameObject)' in c
assert 'child.name==stateName&&child.gameObject.activeSelf' in c
assert 'ChapterRoomGeometry.StarMapPieces(mask)' in c
assert 'node<3' in g and 'segment<5' in g
landmark=c[c.index('public static void ApplyChapterLandmark'):]
for forbidden in ['WorldTraversal.','GameSession.Instance','Destroy(baseRenderer','Resources.Load','AddComponent<Collider']:
    assert forbidden not in landmark
print('PASS: chapter dispatch/shared geometry/6-spawn cap and bounded camp landmark ownership source contracts')

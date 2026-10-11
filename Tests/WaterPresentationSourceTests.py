from pathlib import Path
root=Path(__file__).resolve().parents[1]
r=lambda f:(root/'Assets/Scripts/World'/f).read_text()
w=r('WorldBuilder.cs');linked=r('WorldBuilder.LinkedRooms.cs');water=r('WorldBuilder.WaterSurface.cs')
tactical=r('WorldBuilder.TacticalRooms.cs')
assert 'WaterEnvironment.Brook' in w and 'WaterEnvironment.Courtyard' in linked and 'WaterEnvironment.Tactical' in tactical
assert 'BuildBridgeWaterContact(parent,r,new Rect((bridge.xMin+bridge.xMax)*.5f-2.7f,-3,5.4f,6),.14f)' in tactical
assert 'new Color(.42f,.28f,.16f),false,VisualSurface.Wood' in tactical
assert water.count('Ribbon(parent,r,')==3
assert 'profile.Width,height,shallows' in water and 'profile.DeepWidth,height+.0015f,depth' in water
assert 'profile.CurrentWidth,height+.003f,current' in water
# Three retained opaque depth bands plus one bounded animated current material.
assert water[:water.index('WaterFlowBands.Create')].count('false,VisualSurface.Water')==3
assert water.count('false,VisualSurface.Water')==4
assert 'WaterFlowBands.Create(parent,flow,profile.CurrentWidth*.72f,height+.004f,environment,' in water
assert 'BuildBridgeWaterContact(lowland,r,new Rect(-1.9f,-2.975f,3.8f,3.86f),.049f)' in w
assert 'BuildBridgeWaterContact(parent,r,new Rect(-3.4f,-2.5f,6.8f,6),.07f)' in linked
for token in ['WorldTraversal.','Collider','AddComponent<Light','Time.','Update()']:
    assert token not in water
print('PASS: shared brook/courtyard/tactical depth-current and bridge-contact source wiring')

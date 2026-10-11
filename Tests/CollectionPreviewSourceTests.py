from pathlib import Path
r=Path(__file__).resolve().parents[1]
read=lambda f:(r/'Assets/Scripts/UI'/f).read_text()
p=read('CollectionModelPreview.cs');ui=read('GameUI.CollectionPreview.cs');base=read('GameUI.cs')
assert 'CombatModel.Hero(' in p and 'model.ApplyEquipment(' in p and 'model.ApplyFashion(' in p
assert 'finally {UnityEngine.Random.state=random;}' in p
assert 'c.enabled=false' in p and 'behaviour.enabled=false' in p
assert 'camera.enabled=false' in p and 'camera.cullingMask=1<<PreviewLayer' in p and 'light.cullingMask=1<<PreviewLayer' in p
assert 'new RenderTextureDescriptor(allocated.Width,allocated.Height' in p and 'state.ShouldRender(true,Time.frameCount)' in p and 'texture.IsCreated()' in p
assert 'texture.Release()' in p and 'stage.SetActive(false)' in p
assert 'ReleaseCollectionPreview();' in base and 'private void OnDisable(){ReleaseCollectionModel();}' in ui
assert 'ReleaseCollectionModel();collectionViewing.Reset();' in ui and 'collectionViewing.Yaw' in ui
assert 'EquipmentComparisonPresentation.Receipt(reward)' in ui
assert 'DrawChestRewardModel(' in read('GameUI.Rewards.cs') and 'DrawChestRewardModel(' in read('GameUI.MobileRewards.cs')
assert 'EquipmentComparisonPresentation.Changes(' in base and 'EquipmentComparisonPresentation.Changes(' in read('GameUI.MobileInventory.cs')
assert 'DrawPersistentMechanismDetail(' in base and 'CalcHeight(' in read('GameUI.EquipmentComparison.cs')
assert 'owned&&!current' in ui and 'owned&&!equipped' in ui
assert 'StrongestFashion(slot)' in ui and '属性来源' in ui
assert '.EquipFashion' not in p and 'ProgressionService' not in p and '.Save(' not in p
print('PASS: 14 real-model, isolation, cleanup, bounded-render and persistent-comparison wiring contracts')

assert 'surfaceFrame!=Time.frameCount' in p and 'GetRenderTextureSupportedMSAASampleCount' in p
assert 'Event.current==null||Event.current.type!=EventType.Repaint' in p
assert 'CollectionPreviewFraming.Size(' in p and 'renderer.gameObject.activeInHierarchy' in p
assert 'finally' in p and 'RenderSettings.ambientMode=ambientMode' in p and 'sceneLights[i].cullingMask=sceneMasks[i]' in p
assert 'shadowMaterial!=null' in p and 'ringMaterial!=null' in p and 'shadowTexture!=null' in p
assert 'camera.targetTexture=null' in p and 'sceneLights=null' in p
assert 'model.ConfigurePreview()' in p and 'model.SamplePreview(motion.Time,motion.Action,motion.Progress,motion.OrbitYaw)' in p and 'motion.Advance(Time.unscaledDeltaTime,Time.frameCount)' in p
assert 'if(framingDirty){FrameModel();framingDirty=false;}' in p and 'model.PlayAction(' not in p
assert 'collectionModel.Play((CollectionPreviewAction)i)' in ui
print('PASS: composition, bounded surface, isolated local-clock mannequin, lighting restoration and owned-resource cleanup source contracts')

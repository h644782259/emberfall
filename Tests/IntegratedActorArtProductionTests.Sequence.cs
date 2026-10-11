// Appended inside the actual factory partial. No gameplay/state simulation replacement.
public void IntegratedSequence(System.Action<bool,string> check){
 isolatedPreview=false;Time.time=4;Time.deltaTime=0;Time.frameCount++;
 var originalSpine=spine;var originalHead=headRig;var library=vanguardArt;
 if(isHero){
  if(heroClass==HeroClass.Vanguard)check(library!=null,"real Vanguard library loaded");
  var walkingMeshes=GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh).ToArray();
  for(int i=0;i<4;i++){locomotion.Advance(.04f,.08f,.05f,2,true,false,0);Time.frameCount++;AnimateHero(locomotion.Speed,0,false,.05f);}
  check(locomotion.Speed>.5f&&walkingMeshes.SequenceEqual(GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh)),"real locomotion keeps authored body meshes");
  locomotion.Reset();visualMotion.Reset();AnimateHero(0,0,false,0);
  for(int stage=0;stage<4;stage++){
   int tier=stage==0?1:4;
   var gear=new ItemData{id="integrated-"+stage,name=stage==0?"初行长剑":"Integrated high tier",slot=ItemSlot.Weapon,level=tier==1?1:76,rarity=tier==1?Rarity.Common:Rarity.Legendary,upgradeLevel=tier==1?0:10};
   ApplyEquipment(stage==3?null:gear,stage==3?null:new ItemData{id="armor",slot=ItemSlot.Armor,level=gear.level,rarity=gear.rarity,upgradeLevel=gear.upgradeLevel},null);
   ApplyFashion(stage==2?new FashionData{id="fashion-0-3",slot=FashionSlot.Wings,rarity=Rarity.Legendary}:null,stage==2?new FashionData{id="fashion-1-3",slot=FashionSlot.Weapon,rarity=Rarity.Legendary}:null);
   UnityEngine.Object.Flush();check(spine==originalSpine&&headRig==originalHead&&library==vanguardArt,"equipment preserves body and action library");
   if(heroClass==HeroClass.Vanguard&&stage==0)check(GetComponentsInChildren<MeshFilter>(false).Count(f=>f.sharedMesh.name.StartsWith("Blender weapon Star"))==4,"F3 starter complete");
   if(heroClass!=HeroClass.Vanguard)check(GetComponentsInChildren<MeshFilter>(false).Any(f=>f.sharedMesh.name.StartsWith("F1 silhouette")),"F1 hero visible");
   var identity=GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh).ToArray();
   PlayAction(0,true,.46f);check(Math.Abs(actionAge/actionDuration-BasicActionTimeline.Contact(heroClass==HeroClass.Ranger))<.00001f,"original basic contact clock");
   var committed=spine.localRotation;
   if(heroClass==HeroClass.Vanguard){var expected=Pose(Vector3.zero,new Vector3(-8,WeaponSwingSide*-29f,-7),new Vector3(10,WeaponSwingSide*34f,9),.52f)*Quaternion.Euler(vanguardArt.Sample(VanguardArtPose.Basic,1,.52f));check(Math.Abs(System.Numerics.Quaternion.Dot(expected.q,committed.q))>.999999f,"real Vanguard authored contact");}
   Vector3 tip;var anchor=heroClass==HeroClass.Vanguard?WeaponVisualAnchor.SwordTip:heroClass==HeroClass.Ranger?WeaponVisualAnchor.BowGrip:WeaponVisualAnchor.StaffTop;
   check(TryGetWeaponVisualAnchor(anchor,out tip),"real contact anchor available");
   ContactGeometry(check);
   CancelAction();AnimateHero(0,0,false,0);check(Math.Abs(System.Numerics.Quaternion.Dot(committed.q,spine.localRotation.q))>.999999f,"cancel preserves torso contact");
   Vector3 cancelled;TryGetWeaponVisualAnchor(anchor,out cancelled);check((tip-cancelled).magnitude<.0002f,"cancel preserves world-space contact anchor");
   ContactGeometry(check);
   Time.frameCount++;AnimateHero(0,0,false,.06f);ContactGeometry(check);check(Math.Abs(recoveryAge-.06f)<.00001f,"full recovery half clock");
   Time.frameCount++;AnimateHero(0,0,false,.06f);check(recoveryAge>=.12f,"full recovery finished");
   PlayAction(0,false);CancelAction();AnimateHero(0,0,false,0);PlayAction(0,true,.46f);check(!recoveryCancellation&&actionDuration>0,"replacement commits new contact");
   check(identity.SequenceEqual(GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh)),"actions never swap body or equipment meshes");
   CancelAction();Time.frameCount++;AnimateHero(0,0,false,.12f);
  }
  // Final visible example is the highest existing gear/fashion during real basic contact.
  ApplyEquipment(new ItemData{id="final",slot=ItemSlot.Weapon,level=76,rarity=Rarity.Legendary,upgradeLevel=10},new ItemData{id="finalarmor",slot=ItemSlot.Armor,level=76,rarity=Rarity.Legendary,upgradeLevel=10},null);
  ApplyFashion(new FashionData{id="fashion-0-3",slot=FashionSlot.Wings,rarity=Rarity.Legendary},new FashionData{id="fashion-1-3",slot=FashionSlot.Weapon,rarity=Rarity.Legendary});UnityEngine.Object.Flush();PlayAction(0,true,.46f);
 }else{
  var identity=GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh).ToArray();
  for(int pose=0;pose<4;pose++){
   if(articulatedEnemy){enemyActionPhase=pose==2?EnemyPosePhase.Recovery:EnemyPosePhase.Windup;enemyActionProgress=.5f;}
   if(treantCompanion||floating){SetCompanionAttackPreparation(pose==2?.5f:0);SetCompanionRecall(pose==3?.5f:0);}
   Time.deltaTime=.016f;Time.frameCount++;Animate(pose==1?1:0,pose==2?.5f:0,false);
   check(identity.SequenceEqual(GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh)),"companion/enemy real Animate keeps mesh identity");
  }
  if(treantCompanion||floating){SetCompanionRecall(0);SetCompanionAttackPreparation(.5f);Animate(0,.5f,false);}
 }
}

private void ContactGeometry(System.Action<bool,string> check){
 if(bowRig!=null){check((bowRig.TransformPoint(bowstring.points[1])-bowRig.TransformPoint(arrowRig.localPosition)).magnitude<.00001f,"loaded bow string/nock contact");check((bowRig.localPosition+bowRig.localRotation*WeaponAnchorLocal(WeaponVisualAnchor.BowGrip)-new Vector3(0,-.23f,.04f)).magnitude<.00001f,"loaded bow grip remains on hand socket");}
 if(swordRig!=null){var blade=GetComponentsInChildren<MeshFilter>(false).First(f=>f.transform.name=="Tiered blade"||f.sharedMesh.name=="Blender weapon StarBlade");var v=blade.sharedMesh.vertices.Select(x=>swordRig.InverseTransformPoint(blade.transform.TransformPoint(x))).ToArray();check(Math.Abs(v.Min(x=>x.y)-weaponStructure.SwordRoot)<.0001f&&Math.Abs(v.Max(x=>x.y)-weaponStructure.SwordTip)<.0001f,"loaded blade geometry matches contact endpoints");}
}

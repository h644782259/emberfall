public void EnvelopePose(float progress,bool skill){
 Time.time=4;Time.deltaTime=0;Time.frameCount=1;isolatedPreview=false;
 if(actionDuration==0)PlayAction(0,!skill,.46f);
 actionAge=actionDuration*progress;actionBasic=!skill;actionSkill=0;actionStartedFrame=Time.frameCount;
 AnimateHero(0,0,false,0);
 ContactGeometry((ok,label)=>{if(!ok)throw new Exception(label);});
 if(heroClass==HeroClass.Vanguard&&Math.Abs(progress-.52f)<.0001f){var expected=Pose(Vector3.zero,new Vector3(-8,WeaponSwingSide*-29f,-7),new Vector3(10,WeaponSwingSide*34f,9),.52f)*Quaternion.Euler(vanguardArt.Sample(VanguardArtPose.Basic,1,.52f));if(Math.Abs(System.Numerics.Quaternion.Dot(expected.q,spine.localRotation.q))<.999999f)throw new Exception("actual Vanguard layer changes contact");}
}
public string EnvelopeCategory(Transform t){
 for(var p=t;p!=null;p=p.parent){if(p==swordRig||p==staffRig||p==bowRig||p==equipmentWeapon||p==fashionWeapon||p==castingOrb||p==arrowRig)return "external-weapon";if(p==fashionWings)return "external-fashion-wings";}
 for(var p=t;p!=null;p=p.parent)if(p==headRig)return "head";
 string n=t.name.ToLowerInvariant();if(n.Contains("shield"))return "external-shield";
 if(n.Contains("shoulder")||n.Contains("pauldron")||n.Contains("armor horn")||n.Contains("raised left guard")||n=="sleeve")return "shoulder";
 if(n.Contains("head")||n.Contains("helmet")||n.Contains("wizard hat")||n.Contains("hood")||n.Contains("feather")||n.Contains("spirit antler")||n.Contains("contract crown")||n.Contains("circlet"))return "head";
 return "body";
}

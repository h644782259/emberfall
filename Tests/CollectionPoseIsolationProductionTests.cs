using System;using UnityEngine;using Emberfall;
namespace Emberfall {public sealed partial class CombatModel {public bool PreviewArrow=>arrowRig.gameObject.active;public float PreviewNock=>arrowRig.localPosition.z;public Vector3 PreviewSpine=>spine.localPosition;public Quaternion PreviewDecoration=>decoration.localRotation;
 public Quaternion PreviewLeg=>leftLeg.localRotation;public Vector3 PreviewRoot=>transform.localPosition;
 public Transform InstallPreviewOrbit(){var orbit=new Transform();fashionWings=new Transform{childName="Mechanical star-ring orbit",namedChild=orbit};ConfigurePreview();return orbit;}} }
public static class CollectionPoseIsolationProductionTests
{
 static int n;static void Check(bool ok,string why){n++;if(!ok)throw new Exception(why);}
 static bool Same(Quaternion a,Quaternion b)=>Quaternion.Difference(a,b)<.00001f;
 public static string Run()
 {
  foreach(var hero in new[]{HeroClass.Vanguard,HeroClass.Arcanist,HeroClass.Ranger,HeroClass.Summoner})
  {
   var model=new CombatModel(hero);model.PlayAction(1,false);var prior=model.Arm;int id=model.Identity;model.SamplePreview(7,CollectionPreviewAction.Attack,.3f);Check(Same(prior,model.Arm)&&model.Identity==id,"live model rejects preview sampling before isolation opt-in");
   model.ConfigurePreview();
   foreach(var action in new[]{CollectionPreviewAction.Idle,CollectionPreviewAction.Attack,CollectionPreviewAction.Cast,CollectionPreviewAction.Move})foreach(float progress in new[]{0f,.2f,.5f,.8f,1f})
   {
    Time.time=10;Time.frameCount=20;Time.deltaTime=.5f;model.SamplePreview(2.25f,action,progress);var arm=model.Arm;var weapon=model.Weapon;var spine=model.PreviewSpine;var decoration=model.PreviewDecoration;
    Time.time=900;Time.frameCount=1000;Time.deltaTime=0;model.SamplePreview(2.25f,action,progress);
    Check(Same(arm,model.Arm)&&Same(weapon,model.Weapon)&&Math.Abs(spine.y-model.PreviewSpine.y)<.000001f&&Same(decoration,model.PreviewDecoration),"preview pose is independent of world clock pause and frames");
    Check(model.Identity==id,"preview pose never dispatches an action identity");
   }
   model.SamplePreview(0,CollectionPreviewAction.Move,.125f);var movingLeg=model.PreviewLeg;
   Check(model.PreviewRoot.z>0&&model.Identity==id,"move is bounded mannequin translation without live action identity");
   model.SamplePreview(0,CollectionPreviewAction.Idle,1);Check(!Same(movingLeg,model.PreviewLeg)&&Math.Abs(model.PreviewRoot.z)<.00001f,"short move has visible leg pose and returns to idle origin");
   model.SamplePreview(0,CollectionPreviewAction.Idle,1);float before=model.PreviewSpine.y;model.SamplePreview(.3f,CollectionPreviewAction.Idle,1);Check(Math.Abs(before-model.PreviewSpine.y)>.00001f,"local idle clock actually animates model joints");
  }
  var archer=new CombatModel(HeroClass.Ranger);archer.ConfigurePreview();
  archer.SamplePreview(0,CollectionPreviewAction.Attack,0);float resting=archer.PreviewNock;Check(archer.PreviewArrow,"preview arrow visible before draw");
  archer.SamplePreview(0,CollectionPreviewAction.Attack,.16f);Check(archer.PreviewArrow&&archer.PreviewNock<resting-.1f,"preview arrow stays nocked during actual pullback");
  archer.SamplePreview(0,CollectionPreviewAction.Attack,.32f);Check(!archer.PreviewArrow,"preview arrow disappears at release");
  archer.SamplePreview(0,CollectionPreviewAction.Attack,.60f);Check(!archer.PreviewArrow&&Math.Abs(archer.PreviewNock-resting)<.001f,"preview released bow returns before reload");
  archer.SamplePreview(0,CollectionPreviewAction.Attack,.83f);Check(archer.PreviewArrow,"preview reload restores nocked arrow");
  var clock=new CollectionPreviewMotion();var mannequin=new CombatModel(HeroClass.Arcanist);var orbit=mannequin.InstallPreviewOrbit();bool crossed=false;float previousTime=0,previousRing=0;Quaternion previousOrbit=Quaternion.identity;
  for(int frame=0;frame<2500;frame++)
  {
   clock.Advance(.05f,frame);mannequin.SamplePreview(clock.Time,CollectionPreviewAction.Idle,1,clock.OrbitYaw);
   if(clock.Time<previousTime)
   {
    crossed=true;Check(Quaternion.Difference(previousOrbit,orbit.localRotation)<.00005f,"actual mechanical orbit remains continuous across local-clock wrap");
    float ringDelta=(clock.RingYaw-previousRing+360)%360;Check(ringDelta<.61f,"12-degree floor ring remains continuous at the same boundary");
    var expected=previousOrbit*Quaternion.Euler(0,0,.8f);Check(Same(expected,orbit.localRotation),"actual mechanical orbit retains 16 degrees per second across wrap");break;
   }
   previousTime=clock.Time;previousRing=clock.RingYaw;previousOrbit=orbit.localRotation;
  }
  Check(crossed,"orbit continuity test actually crosses the production 120-second wrap");
  return "PASS: "+n+" production isolated preview pose checks (managed transforms, no rendered engine frames)";
 }
}

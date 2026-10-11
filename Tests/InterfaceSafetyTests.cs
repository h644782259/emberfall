using System;
using Emberfall;
public static class InterfaceSafetyTests
{
 static int n;static void Check(bool b,string m){n++;if(!b)throw new Exception(m);}
 public static string Run()
 {
  n=0;foreach(float text in new[]{24f,48f,96f})foreach(float progress in new[]{18f,36f,72f})foreach(float charge in new[]{0f,23f,46f})
  {var l=new ObjectiveCardLayout(17,text,progress,charge);Check(l.BodyY>=l.HeadingY+17,"heading isolated");Check(l.ProgressY>=l.BodyY+text,"body/progress no overlap");Check(l.ChargeY>=l.ProgressY+progress,"charge separate");Check(l.Height>=l.ProgressY+progress+10,"text contained");if(charge>0)Check(l.Height>=l.ChargeY+charge+10,"charge fits card");}
  Check(PortalInteractionPolicy.CanRequest(true,false,false,false,false,4),"near portal opens");
  Check(!PortalInteractionPolicy.CanRequest(true,false,false,true,false,4),"repeat open refused");
  Check(!PortalInteractionPolicy.CanRequest(true,false,false,false,true,4),"menu blocks interaction");
  Check(!PortalInteractionPolicy.CanRequest(true,false,false,false,false,25),"distant disabled");
  Check(!PortalInteractionPolicy.IsNear(float.NaN)&&!PortalInteractionPolicy.IsNear(4.3f*4.3f),"invalid/exact outer edge excluded");
  var g=new SaveLifecycleGate();int writes=0;Func<bool> save=()=>{writes++;return true;};
  g.Observe(true,true,save);g.Observe(true,true,save);Check(writes==1,"pause/focus callbacks coalesce");
  g.Observe(false,true,save);g.Observe(true,true,save);Check(writes==2,"next suspension saves new progress");
  g.Observe(false,false,save);g.Observe(true,false,save);Check(writes==2,"title never creates save");
  g.Observe(false,true,save);g.Observe(true,true,()=>{writes++;return false;});g.Observe(true,true,save);Check(writes==4,"failed background write can retry");
  return n+" HUD/portal/lifecycle assertions passed";
 }
}

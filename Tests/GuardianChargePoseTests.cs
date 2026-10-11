using System;using Emberfall;
public static class GuardianChargePoseTests
{
 public static string Run()
 {
  int n=0;Action<bool,string> check=(ok,why)=>{n++;if(!ok)throw new Exception(why);};
  foreach(float dt in new[]{1f/120,1f/60,1f/30,.1f})
  {
   float time=0,remaining=9f/BossAttackPolicy.ChargeSpeed;bool active=true;int contactFrames=0;
   while(active)
   {
    time+=dt;remaining=Math.Max(0,remaining-dt);
    float shortRecovery=Math.Max(0,1-time*3);
    var phase=EnemyActionPose.Select(false,active,shortRecovery);
    check(phase==EnemyPosePhase.ActiveCharge&&EnemyActionPose.Charge(phase)==1,"charge pose outlives short contact animation");
    if(time>=.617f) {contactFrames++;check(EnemyActionPose.Charge(phase)>0&&shortRecovery==0,"late swept hit remains visibly braced");}
    // Controller renders before retiring the attack on its final swept frame.
    if(remaining<=.001f)active=false;
   }
   check(contactFrames>0,"fixture includes reported late-contact window");
   check(EnemyActionPose.Select(false,active,0)==EnemyPosePhase.Idle,"finished charge returns to idle");
  }
  foreach(float recovery in new[]{0,.7f,1f})
  {
   check(EnemyActionPose.Select(false,false,recovery)!=EnemyPosePhase.ActiveCharge,"cancel/death/disable removes active pose regardless of remaining recovery");
   check(EnemyActionPose.Charge(EnemyActionPose.Select(true,false,recovery))==0,"normal windup does not gain charging brace");
  }
  check(EnemyActionPose.Contact(EnemyPosePhase.Recovery,0)==1&&EnemyActionPose.Contact(EnemyPosePhase.Recovery,1)==0,"ordinary contact/recovery unchanged");
  var walking=new LocomotionPoseState();walking.Advance(0,0,.1f,3,true,false,0);
  check(walking.Phase==0&&walking.Speed==0,"active charge does not fabricate walking stride");
  return "PASS: "+n+" Guardian active-charge lifetime/late-hit/cancel assertions (managed, not rendering)";
 }
}

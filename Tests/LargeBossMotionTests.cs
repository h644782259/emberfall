using System;using Emberfall;
public static class LargeBossMotionTests
{
 static int n;static void Check(bool ok,string why){n++;if(!ok)throw new Exception(why);}
 static float Delta(float a,float b){return (b-a+540)%360-180;}
 public static string Run()
 {
  var slow=new LargeBossMotion();var fast=new LargeBossMotion();
  for(int i=0;i<20;i++)slow.Advance(.1f,LargeBossPhase.Windup);
  for(int i=0;i<200;i++)fast.Advance(.01f,LargeBossPhase.Windup);
  Check(Math.Abs(Delta(slow.FirstAngle,fast.FirstAngle))<.004f&&Math.Abs(Delta(slow.SecondAngle,fast.SecondAngle))<.004f,"analytic speed integration is frame partition stable");
  foreach(var phase in new[]{LargeBossPhase.Combat,LargeBossPhase.Windup,LargeBossPhase.Beam,LargeBossPhase.Exposed,LargeBossPhase.Recovery})
  {
   for(int i=0;i<3000;i++)
   {
    float a=slow.FirstAngle,b=slow.SecondAngle;slow.Advance(.02f,phase);
    Check(Math.Abs(Delta(a,slow.FirstAngle))<=1.101f&&Math.Abs(Delta(b,slow.SecondAngle))<=.761f,"long-running phase switch never jumps orbital angle");
    Check(slow.FirstVelocity>=6.99f&&slow.FirstVelocity<=55.01f&&slow.SecondVelocity>=-38.01f&&slow.SecondVelocity<=-4.99f,"velocities remain within intended phase range");
   }
  }
  float before=slow.FirstAngle;
  foreach(float dt in new[]{0,-1,float.NaN,float.PositiveInfinity})slow.Advance(dt,LargeBossPhase.Beam);
  slow.Advance(.1f,LargeBossPhase.Finished);Check(before==slow.FirstAngle,"zero/invalid/finished cannot rotate");
  slow.Advance(100,LargeBossPhase.Beam);Check(Math.Abs(Delta(before,slow.FirstAngle))<=5.501f,"hitch cannot spin through many unreadable revolutions");
  var windup=LargeBossMotion.Pose(LargeBossPhase.Windup,0);var beam=LargeBossMotion.Pose(LargeBossPhase.Beam,8);
  var exposed=LargeBossMotion.Pose(LargeBossPhase.Exposed,5);var recover=LargeBossMotion.Pose(LargeBossPhase.Recovery,2);
  Check(windup.Opening<beam.Opening&&beam.Opening<exposed.Opening,"windup beam and exposed armour silhouettes differ");
  Check(windup.Height<LargeBossMotion.Pose(LargeBossPhase.Windup,2.2f).Height,"windup compresses body before release");
  Check(exposed.CoreScale>beam.CoreScale&&exposed.Pitch>0&&beam.Pitch<0,"exposure lifts readable core rather than posing as attack");
  Check(recover.Brace==1&&LargeBossMotion.Pose(LargeBossPhase.Recovery,0).Brace==0,"recovery visibly releases bracing");
  return "PASS: "+n+" large boss pose/angular-continuity assertions (not rendered animation)";
 }
}

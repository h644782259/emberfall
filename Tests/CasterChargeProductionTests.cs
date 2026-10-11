using System;using Emberfall;using UnityEngine;
namespace Emberfall { public sealed partial class CombatModel {public Quaternion ChargeLeft=>leftArm.localRotation;public Quaternion ChargeTorso=>spine.localRotation;} }
public static class CasterChargeProductionTests
{
 static int n;static void Check(bool ok,string message){n++;if(!ok)throw new Exception(message);}
 static bool Same(Quaternion a,Quaternion b)=>Quaternion.Difference(a,b)<.00001f;
 public static string Run()
 {
  foreach(var hero in new[]{HeroClass.Arcanist,HeroClass.Summoner})for(int skill=0;skill<10;skill++)
  {
   var m=new CombatModel(hero);m.PlayAction((skill+3)%10,false);int identity=m.Identity;float duration=m.Duration,age=m.Age;
   foreach(float progress in new[]{0f,.2f,.5f,.8f,.999f,1f}){m.AnimateCharge(progress,skill);Check(m.Identity==identity&&m.Duration==duration&&m.Age==age,"charge pose cannot mutate logical action identity or nominal timeline");}
   var arm=m.Arm;var weapon=m.Weapon;var left=m.ChargeLeft;var torso=m.ChargeTorso;
   var release=new CombatModel(hero);release.ReleaseCharge(skill);
   Check(Same(arm,release.Arm)&&Same(weapon,release.Weapon)&&Same(left,release.ChargeLeft)&&Same(torso,release.ChargeTorso),"actual skill charge converges to its committed family pose");
   m.AnimateCharge(.999f,skill);Check(Same(m.Arm,arm)&&Same(m.Weapon,weapon),"charge family approaches release continuously");
   m.AnimateCharge(1,skill);m.AnimateCharge(float.NaN,skill);m.AnimateCharge(float.PositiveInfinity,skill);Check(Same(m.Arm,arm)&&Same(m.Weapon,weapon),"invalid progress cannot poison pose");
  }
  // Slot 3 is a permanent attribute passive. A directional charge uses actual slot 0.
  var a=new CombatModel(HeroClass.Arcanist);var poses=new Quaternion[4];a.AnimateCharge(.9f,0);poses[0]=a.Arm;a.AnimateCharge(.9f,1);poses[1]=a.Arm;a.AnimateCharge(.9f,5);poses[2]=a.Arm;var s=new CombatModel(HeroClass.Summoner);s.AnimateCharge(.9f,2);poses[3]=s.Arm;
  for(int i=0;i<4;i++)for(int j=i+1;j<4;j++)Check(!Same(poses[i],poses[j]),"directional ground self and contract charges have distinct real poses");
  return "PASS: "+n+" production caster charge/family/commit checks (managed transforms, not rendered animation)";
 }
}

using System;using Emberfall;
public static class CombatSightRulesTests
{
 public static string Run()
 {
  int n=0;
  foreach(CombatSightKind kind in Enum.GetValues(typeof(CombatSightKind)))
  {
   Check(CombatSightRules.Allows(kind,true,true),"clear route",ref n);
   Check(!CombatSightRules.Allows(kind,false,true),"solid cover",ref n);
   Check(CombatSightRules.Allows(kind,true,false)==(kind!=CombatSightKind.Melee),"water distinction",ref n);
  }
  for(int i=0;i<=100;i++)
  {
   float limit=i/100f;float visible=CombatSightRules.VisibleFraction(f=>f<=limit);
   Check(visible<=limit+.00001f&&limit-visible<.0001f,"preview/commit stops before wall",ref n);
  }
  Check(CombatSightRules.VisibleFraction(null)==0,"missing route",ref n);
  Check(CombatSightRules.VisibleFraction(f=>false)==0,"invalid origin",ref n);
  return "PASS: "+n+" shared combat sight policy assertions";
 }
 static void Check(bool condition,string reason,ref int n){n++;if(!condition)throw new Exception(reason);}
}

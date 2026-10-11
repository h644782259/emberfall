using System;
using Emberfall;
public static class WeaponStructureTests
{
    public static string Run()
    {
        int n=0;
        Action<bool,string> check=(ok,message)=>{n++;if(!ok)throw new Exception(message);};
        foreach(int input in new[]{int.MinValue,-1,0,1,2,3,4,5,int.MaxValue})
        {
            var s=new WeaponStructure(input);
            check(s.Tier>=0&&s.Tier<=4,"tier safely bounded");
            check(Math.Abs(s.StaffShaftCenter-s.StaffShaftHalfLength-s.StaffBottom)<.00001f,"shaft begins at bottom anchor");
            check(Math.Abs(s.StaffShaftCenter+s.StaffShaftHalfLength-s.StaffCollar)<.00001f,"shaft terminates at collar, not beyond crystal");
            check(s.StaffCollar<s.StaffCore&&s.StaffCore<s.StaffTop,"collar/core/top ordered");
            check(s.StaffTop<1.76f&&s.StaffBottom>=-.94f,"staff silhouette bounded at all tiers");
            check(s.SwordTip>s.SwordRoot&&s.SwordTip<1.72f,"blade extends from guard with bounded reach");
            check(s.BowReach>=.58f&&s.BowReach<.83f,"bow string and tip reach bounded");
        }
        var high=new WeaponStructure(4);
        check(.94f*1.39f>-high.StaffBottom+.36f,"negative control: legacy high-tier lower shaft exceeded new safe grip envelope");
        check(high.StaffShaftCenter+high.StaffShaftHalfLength<high.StaffCore-high.StaffCoreDiameter*.5f,"fixed high staff shaft does not pierce core");
        for(int tier=1;tier<4;tier++)
        {
            var a=new WeaponStructure(tier);var b=new WeaponStructure(tier+1);
            check(b.SwordTip>a.SwordTip&&b.BowReach>a.BowReach,"upgrading retains weapon silhouette growth");
            check(b.StaffBottom==a.StaffBottom&&b.StaffCoreDiameter>a.StaffCoreDiameter,"staff growth retains safe lower grip length and enlarges upper focus");
        }
        return "PASS: "+n+" weapon structure relationships (no engine/render validation)";
    }
}

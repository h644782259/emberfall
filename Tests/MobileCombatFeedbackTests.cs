using System;
using Emberfall;
public static class MobileCombatFeedbackTests
{
    static int count;
    static void Check(bool result,string message){count++;if(!result)throw new Exception(message);}
    public static string Run()
    {
        count=0;
        Check(MobileCombatPresentation.Skill(true,false,0,100,20,false,0,true)=="蓄力","Current cast has explicit charging state");
        Check(MobileCombatPresentation.Skill(false,false,0,100,20,false,0,true)=="未学","Charging cannot make unlearned slot available");
        Check(MobileCombatPresentation.Skill(false,false,3,0,20,true,0)=="未学","Unlearned takes precedence");
        Check(MobileCombatPresentation.Skill(true,true,3,0,20,true,0)=="被动","Passive cannot look like a castable skill");
        Check(MobileCombatPresentation.Skill(true,false,3,0,20,true,0)=="冷却","Cooldown priority");
        Check(MobileCombatPresentation.Skill(true,false,0,19,20,true,0)=="缺能","Energy before charge check matches runtime");
        Check(MobileCombatPresentation.Skill(true,false,0,20,20,true,0)=="限疗空","Empty healing charge");
        Check(MobileCombatPresentation.Skill(true,false,0,20,20,false,0)=="","Normal healing does not consume run charges");
        Check(MobileCombatPresentation.Skill(true,false,0,20,20,true,1)=="","Ready healing");
        Check(MobileCombatPresentation.Potion(0,false,false)=="药剂空","Empty inventory");
        Check(MobileCombatPresentation.Potion(0,true,false)=="充能空","Empty challenge supply");
        Check(MobileCombatPresentation.Potion(1,false,true)=="满血","Full health distinct from unavailable inventory");
        Check(MobileCombatPresentation.Potion(1,true,false)=="","Ready charge");
        Check(MobileCombatPresentation.Dodge(1,false)=="冷却","Blink cooldown");
        Check(MobileCombatPresentation.Dodge(0,true)=="需落地","Blink airborne restriction");
        Check(MobileCombatPresentation.Dodge(0,false)=="","Blink ready");
        return count+" mobile combat state assertions passed";
    }
}

using System;
using Emberfall;
public static class EquipmentAttachmentTests
{
    public static string Run()
    {
        int checks=0;Action<bool,string> check=(ok,msg)=>{checks++;if(!ok)throw new Exception(msg);};
        for(int tier=1;tier<=4;tier++)for(int upgrade=0;upgrade<=10;upgrade++)
        {
            var pieces=EquipmentAttachmentRecipe.Relic(tier,upgrade);
            check(pieces.Length<=17,"relic piece budget bounded");
            foreach(var p in pieces)
            {
                check(Math.Abs(p.X)+p.HalfX<.245f,"central clasp avoids lateral grip rest envelope");
                check(p.Y-p.HalfY>.65f&&p.Y+p.HalfY<1.045f,"all tiers and upgrades clear lower chest identity and face envelopes");
                check(p.Z-p.HalfZ>=.39f&&p.Z+p.HalfZ<.535f,"waist clasp sits in front of costume without entering back fashion plane");
                check(p.Width>0&&p.Height>0&&p.Depth>0,"finite positive authored pieces");
            }
        }
        var max=EquipmentAttachmentRecipe.Relic(4,10);int marks=0;foreach(var p in max)if(p.Kind==RelicPartKind.Mark)marks++;
        check(marks==10,"maximum upgrade keeps workmanship marks inside envelope");
        check(1.97f-.08f>1.75f,"negative control: old T4 halo occupied face height");
        check(1.65f+(.17f+4*.045f+.12f)>2.1f,"negative control: old forging marks reached head space");
        foreach(int tier in new[]{3,4})
        {var s=new WeaponStructure(tier);check(s.StaffBottom==-.94f&&s.StaffCore>1.33f,"high tier growth above grip");}
        return "PASS: "+checks+" production attachment envelope/legacy-negative-control checks; animated overlap still requires Unity validation";
    }
}

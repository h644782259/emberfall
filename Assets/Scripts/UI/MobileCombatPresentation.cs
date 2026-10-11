using System;
namespace Emberfall
{
    // Pure state ordering shared by the labels and rejected-tap feedback.
    public static class MobileCombatPresentation
    {
        public static string Skill(bool learned,bool passive,float cooldown,float energy,float cost,bool limitedHealing,int charges,bool charging=false)
        {
            if(!learned)return "未学";
            if(passive)return "被动";
            if(charging)return "蓄力";
            if(cooldown>.01f)return "冷却";
            if(energy<cost)return "缺能";
            if(limitedHealing&&charges<=0)return "限疗空";
            return "";
        }
        // Compact button captions only; combat APIs retain the complete reason.
        public static string SkillRejectionCaption(string reason)
        {
            if(reason=="目标被遮挡")return "被遮挡";
            if(reason=="距离不足")return "太远";
            return reason??string.Empty;
        }
        public static string Potion(int count,bool limited,bool full)
        {return count<=0?(limited?"充能空":"药剂空"):full?"满血":"";}
        public static string Dodge(float cooldown,bool airborne)
        {return cooldown>.01f?"冷却":airborne?"需落地":"";}
    }
}

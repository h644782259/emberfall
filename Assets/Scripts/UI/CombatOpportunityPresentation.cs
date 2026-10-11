using System;
namespace Emberfall
{
    public static class CombatOpportunityPresentation
    {
        public static string Vanguard(float counter){return counter>0?"反击窗口 "+counter.ToString("0.0")+"秒":"";}
        public static bool MeteorReady(bool learned,float cooldown,float energy,float cost,bool castBlocked)
        {return learned&&!castBlocked&&cooldown<=0&&energy>=cost;}
        public static string Arcanist(bool frost,bool burn,bool burnRoute,bool actionReady)
        {
            // For non-burn routes, actionReady is the actual resolver-backed shatter query, not just cooldown/energy.
            // Burn refreshes the existing DoT; this does not claim a new bonus.
            // Route relevance wins when the target carries both statuses.
            if(burnRoute&&burn)return actionReady?"灼烧 · 陨星续燃":"目标灼烧";
            if(!burnRoute&&actionReady)return frost?"霜痕 · 可碎冰":"当前落点可碎冰";
            if(frost)return "目标霜痕";
            return burn?"目标灼烧":"";
        }
        public static string Ranger(int poison,bool vulnerable)
        {return poison>=3?(vulnerable?"三层毒 · 易伤":"目标三层毒"):vulnerable?"目标易伤":poison>0?"目标中毒 ×"+poison:"";}
        public static string Summoner(int count,float lifetime,float opportunity)
        {
            if(opportunity>0)return "契机"+Math.Ceiling(opportunity)+"秒·"+count+"伴·"+(float.IsPositiveInfinity(lifetime)?"常驻":Math.Ceiling(Math.Max(0,lifetime))+"秒");
            if(count<=0)return "暂无伙伴";
            return "伙伴 "+count+ (float.IsPositiveInfinity(lifetime)?" · 常驻":" · 最短 "+Math.Ceiling(Math.Max(0,lifetime))+"秒");
        }
    }
}

using System;
using System.Collections.Generic;
namespace Emberfall
{
    public sealed partial class RunChoices
    {
        // Desktop pages can reach either equipped page; mobile has fixed learned active buttons.
        public static int[] UsableRanks(GameProfile profile, bool mobile)
        {
            var result = new int[GameBalance.SkillCount];
            if (profile == null || profile.skillRanks == null) return result;
            for (int skill=0; skill<result.Length && skill<profile.skillRanks.Length; skill++)
                if (!GameBalance.IsPassive(skill) && profile.skillRanks[skill]>0 &&
                    (mobile || profile.equippedSkills!=null && Array.IndexOf(profile.equippedSkills,skill)>=0))
                    result[skill]=profile.skillRanks[skill];
            return result;
        }
        public void PrepareRoomChoice(int stage, GameProfile profile, bool mobile, int seed)
        {
            if (profile==null || stage<1 || stage>2 || AwaitingChoice || stage!=CompletedWave+1 || active.Count!=stage-1) return;
            int[] usable=UsableRanks(profile,mobile);
            var eligible=new List<RunBlessing>();
            foreach(RunBlessing item in Enum.GetValues(typeof(RunBlessing)))
                if(!active.Contains(item)&&IsCompatible(item,profile.heroClass,usable))eligible.Add(item);
            var random=new RoomChoiceRandom(unchecked(seed+stage*577+(int)profile.heroClass*103+(int)profile.specialization*71+(int)profile.summonerRoute*43));
            var result=new List<RunBlessing>();
            RunBlessing first=RunBlessing.DodgeShock;
            foreach(var item in active){first=item;break;}
            // Exactly one strongly associated slot at rest; patch/open cards
            // remain outside that association so the second choice has breadth.
            AddRoomCard(result,eligible,random,item=>stage==1?RoomStyle(item):RoomAssociated(first,item));
            AddRoomCard(result,eligible,random,item=>(stage==1?RoomResource(item)||RoomSurvival(item):
                !RoomAssociated(first,item)&&(RoomResource(first)?RoomSurvival(item):RoomSurvival(first)?RoomResource(item):RoomResource(item)||RoomSurvival(item))));
            bool needsBoss=stage==2&&!result.Exists(RoomBossEffective);
            AddRoomCard(result,eligible,random,item=>(stage==1||!RoomAssociated(first,item))&&(!needsBoss||RoomBossEffective(item)));
            if(result.Count!=3)throw new InvalidOperationException("Room blessing roles must have three eligible distinct cards.");
            CompletedWave=stage;offer=result.ToArray();AwaitingChoice=true;
        }
        private sealed class RoomChoiceRandom
        {
            private uint state;
            public RoomChoiceRandom(int seed){state=unchecked((uint)seed)^0x9e3779b9u;if(state==0)state=1;}
            public int Next(int count){state^=state<<13;state^=state>>17;state^=state<<5;return (int)(state%(uint)count);}
        }
        private static void AddRoomCard(List<RunBlessing> result,List<RunBlessing> eligible,RoomChoiceRandom random,Predicate<RunBlessing> condition)
        {
            var pool=eligible.FindAll(item=>!result.Contains(item)&&condition(item));
            if(pool.Count>0)result.Add(pool[random.Next(pool.Count)]);
        }
        public static bool RoomResource(RunBlessing item)
        {return item==RunBlessing.FlowingEssence||item==RunBlessing.QuickRecovery||item==RunBlessing.SwiftHands;}
        public static bool RoomSurvival(RunBlessing item)
        {return item==RunBlessing.IronSkin||item==RunBlessing.LastStand||item==RunBlessing.ExecutionMend;}
        public static bool RoomStyle(RunBlessing item)
        {return !RoomResource(item)&&!RoomSurvival(item)&&item!=RunBlessing.RiskContract||item==RunBlessing.SwiftHands;}
        public static bool RoomBossEffective(RunBlessing item)
        {return item!=RunBlessing.MarkedPursuit&&item!=RunBlessing.ExecutionMend&&item!=RunBlessing.RiskContract;}
        public static bool RoomAssociated(RunBlessing first,RunBlessing candidate)
        {
            switch(first)
            {
                case RunBlessing.KeenSight:return candidate==RunBlessing.DeadlyEdge;
                case RunBlessing.DeadlyEdge:return candidate==RunBlessing.KeenSight;
                case RunBlessing.DodgeShock:case RunBlessing.MarkedPursuit:return candidate==RunBlessing.SwiftHands||candidate==RunBlessing.BattleFervor;
                case RunBlessing.ChargedWard:case RunBlessing.InterruptFlow:return candidate==RunBlessing.FlowingEssence||candidate==RunBlessing.QuickRecovery;
                case RunBlessing.BattleFervor:return candidate==RunBlessing.KeenSight||candidate==RunBlessing.DeadlyEdge;
                case RunBlessing.QuickRecovery:return candidate==RunBlessing.FlowingEssence;
                case RunBlessing.SwiftHands:return candidate==RunBlessing.BattleFervor;
                case RunBlessing.FlowingEssence:return candidate==RunBlessing.QuickRecovery;
                case RunBlessing.IronSkin:return candidate==RunBlessing.LastStand;
                case RunBlessing.LastStand:return candidate==RunBlessing.IronSkin;
                case RunBlessing.ExecutionMend:return candidate==RunBlessing.IronSkin||candidate==RunBlessing.LastStand;
                case RunBlessing.RiskContract:return candidate==RunBlessing.IronSkin||candidate==RunBlessing.LastStand;
                default:return false;
            }
        }
        public static string Association(RunBlessing item,GameProfile profile,bool mobile)
        {
            if(profile==null)return "当前配置可用";
            int[] ranks=UsableRanks(profile,mobile);
            if(!IsCompatible(item,profile.heroClass,ranks))return "需可用的对应技能";
            if(item==RunBlessing.DodgeShock)return "闪避 → 普攻反击";
            if(item==RunBlessing.MarkedPursuit)return GameBalance.SkillName(profile.heroClass,7)+" → 击杀护卫后追击";
            if(item==RunBlessing.ChargedWard)
            {
                int preferred=profile.heroClass==HeroClass.Arcanist?1:profile.heroClass==HeroClass.Vanguard?7:profile.heroClass==HeroClass.Summoner?4:9;
                return GameBalance.SkillName(profile.heroClass,ranks[preferred]>0?preferred:9)+" → 蓄力护佑";
            }
            if(item==RunBlessing.InterruptFlow)return "控制打断 → 回能";
            if(item==RunBlessing.FlowingEssence)return "补资源 · 持续回能";
            if(item==RunBlessing.QuickRecovery)return "补节奏 · 新施法冷却更短";
            if(item==RunBlessing.ExecutionMend)return "击败首领护卫 → 补给生命";
            if(item==RunBlessing.IronSkin||item==RunBlessing.LastStand)return "补生存 · 留容错";
            if(item==RunBlessing.SwiftHands)return "普攻 → 更快回能";
            if(item==RunBlessing.KeenSight||item==RunBlessing.DeadlyEdge)return "玩家直伤暴击 · 不含持续/伙伴";
            return item==RunBlessing.RiskContract?"承受更多伤害换金币":"强化普攻与技能伤害";
        }
    }
}

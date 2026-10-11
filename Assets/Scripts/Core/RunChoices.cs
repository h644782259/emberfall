using System;
using System.Collections.Generic;

namespace Emberfall
{
    public enum RunBlessing
    {
        DodgeShock, ChargedWard, InterruptFlow, MarkedPursuit, ExecutionMend, RiskContract,
        KeenSight, DeadlyEdge, BattleFervor, QuickRecovery, SwiftHands, IronSkin, LastStand, FlowingEssence
    }

    /// <summary>Per-expedition choices: never written into the permanent character stats.</summary>
    public sealed partial class RunChoices
    {
        private readonly HashSet<RunBlessing> active = new HashSet<RunBlessing>();
        private RunBlessing[] offer = new RunBlessing[0];
        public bool AwaitingChoice { get; private set; }
        public int CompletedWave { get; private set; }
        public RunBlessing[] Offer { get { return (RunBlessing[])offer.Clone(); } }
        public IEnumerable<RunBlessing> Active { get { return active; } }
        public bool Has(RunBlessing blessing) { return active.Contains(blessing); }
        public static int StackLimit(RunBlessing blessing) { return Enum.IsDefined(typeof(RunBlessing), blessing) ? 1 : 0; }
        public float AttackMultiplier { get { return Has(RunBlessing.BattleFervor) ? 1.10f : 1f; } }
        public float CriticalMultiplier { get { return Has(RunBlessing.DeadlyEdge) ? 2.15f : 1.65f; } }
        public float CooldownMultiplier { get { return Has(RunBlessing.QuickRecovery) ? .85f : 1f; } }
        public float AttackSpeedMultiplier { get { return Has(RunBlessing.SwiftHands) ? 1.18f : 1f; } }
        public float ExtraEnergyPerSecond { get { return Has(RunBlessing.FlowingEssence) ? 2f : 0f; } }
        public float CritChance(float baseChance)
        {
            if (float.IsNaN(baseChance) || float.IsInfinity(baseChance)) baseChance = 0;
            baseChance = Math.Max(0f, Math.Min(1f, baseChance));
            return Has(RunBlessing.KeenSight) ? Math.Max(baseChance, Math.Min(.8f, baseChance + .2f)) : baseChance;
        }
        public float IncomingDamageMultiplier(float healthFraction)
        {
            float reduction = Has(RunBlessing.IronSkin) ? .1f : 0f;
            if (Has(RunBlessing.LastStand) && healthFraction <= .35f && healthFraction > 0) reduction += .15f;
            return 1f - Math.Min(.25f, reduction);
        }
        public void Reset() { active.Clear(); offer = new RunBlessing[0]; AwaitingChoice = false; CompletedWave = 0; }

        public void Prepare(int completedWave, HeroClass hero, int[] skills, int seed)
        {
            if (completedWave < 1 || completedWave > 2 || AwaitingChoice || completedWave <= CompletedWave) return;
            CompletedWave = completedWave;
            var compatible = new List<RunBlessing>();
            var remaining = new List<RunBlessing>();
            foreach (RunBlessing blessing in Enum.GetValues(typeof(RunBlessing)))
            {
                if (active.Contains(blessing)) continue;
                remaining.Add(blessing);
                if (IsCompatible(blessing, hero, skills)) compatible.Add(blessing);
            }
            var random = new Random(seed);
            var result = new List<RunBlessing>();
            // At least two immediately usable choices; select a third usable one
            // whenever possible. One prior reward leaves at least two universals.
            while (result.Count < 3 && compatible.Count > 0)
            {
                int index = random.Next(compatible.Count); RunBlessing chosen = compatible[index];
                result.Add(chosen); compatible.RemoveAt(index); remaining.Remove(chosen);
            }
            while (result.Count < 3 && remaining.Count > 0)
            {
                int index = random.Next(remaining.Count); result.Add(remaining[index]); remaining.RemoveAt(index);
            }
            for (int i = result.Count - 1; i > 0; i--) { int j = random.Next(i + 1); RunBlessing temp = result[i]; result[i] = result[j]; result[j] = temp; }
            offer = result.ToArray(); AwaitingChoice = true;
        }

        public bool Choose(int index)
        {
            if (!AwaitingChoice || index < 0 || index >= offer.Length) return false;
            active.Add(offer[index]); AwaitingChoice = false; offer = new RunBlessing[0]; return true;
        }

        public static bool IsCompatible(RunBlessing blessing, HeroClass hero, int[] ranks)
        {
            if (!Enum.IsDefined(typeof(RunBlessing), blessing)) return false;
            if (blessing == RunBlessing.FlowingEssence || blessing == RunBlessing.QuickRecovery)
            {
                if(ranks==null||(int)hero<0||(int)hero>=GameBalance.ClassNames.Length)return false;
                for(int skill=0;skill<ranks.Length&&skill<GameBalance.SkillCount;skill++)
                    if(ranks[skill]>=1&&ranks[skill]<=3&&!GameBalance.IsPassive(skill)&&
                        (blessing==RunBlessing.FlowingEssence?GameBalance.SkillEnergyCost(hero,skill)>0:GameBalance.EffectiveCooldown(hero,skill,ranks[skill])>1))return true;
                return false;
            }
            if ((int)blessing >= (int)RunBlessing.KeenSight || blessing == RunBlessing.DodgeShock || blessing == RunBlessing.ExecutionMend || blessing == RunBlessing.RiskContract) return true;
            if (ranks == null) return false;
            if (blessing == RunBlessing.MarkedPursuit) return hero == HeroClass.Ranger && ranks.Length > 7 && ranks[7] > 0;
            if (blessing == RunBlessing.ChargedWard) return (hero == HeroClass.Arcanist && ranks.Length > 1 && ranks[1] > 0) ||
                (hero == HeroClass.Vanguard && ranks.Length > 7 && ranks[7] > 0) || (hero == HeroClass.Summoner && ranks.Length > 4 && ranks[4] > 0) || (ranks.Length > 9 && ranks[9] > 0);
            return (hero == HeroClass.Vanguard || hero == HeroClass.Ranger) && ranks.Length > 1 && ranks[1] > 0 ||
                hero == HeroClass.Arcanist && (ranks.Length > 0 && ranks[0] > 0 || ranks.Length > 1 && ranks[1] > 0) ||
                hero == HeroClass.Summoner && ranks.Length > 0 && ranks[0] > 0;
        }

        public static string Name(RunBlessing blessing)
        {
            string[] names = { "踏风余震", "蓄星护佑", "断势回流", "猎印追风", "破阵甘霖", "星烬契约", "鹰眼星辉", "致命锋芒", "战意昂扬", "流转回响", "疾风快手", "星铁护体", "背水守护", "灵流奔涌" };
            return Enum.IsDefined(typeof(RunBlessing), blessing) ? names[(int)blessing] : "未知祝福";
        }
        public static string Description(RunBlessing blessing)
        {
            if (!Enum.IsDefined(typeof(RunBlessing), blessing)) return "无效祝福";
            return new[] {
                "闪现后2秒内，下次普攻命中释放一次小范围冲击。",
                "完成一次蓄力施法，获得2秒25%减伤。取消施法不触发。",
                "真正打断敌人预警时回复12能量，冷却1秒。首领带青色符号的预警可被指定控制技能打断。",
                "击杀带狩猎标记的目标，获得2.5秒20%移速；首领房可借护卫触发。",
                "击败每波的守卫或首领，恢复12%最大生命（每波一次）。",
                "本局受到伤害+15%，通关金币+30%；撤离或失败不发额外奖励。",
                "本局暴击率+20个百分点，最高80%。玩家普攻与可暴击直伤技能生效；持续伤害与伙伴不继承。",
                "本局玩家普攻与可暴击直伤技能的暴击倍率由165%提高至215%。持续伤害与伙伴不继承暴击加成。",
                "本局玩家攻击伤害+10%，技能按攻击计算部分同效；伙伴伤害+10%（仅一次），不继承暴击祝福。不改变永久装备。",
                "本局新施放技能冷却缩短15%，最低1秒。不会清空正在运行的冷却。",
                "本局普通攻击速度+18%；不加速技能、持续伤害或召唤物攻击。",
                "本局额外减伤10%；与背水守护相加最多25%，保留整体减伤下限。",
                "本局生命不高于35%时额外减伤15%；不回血，与星铁护体合计最多25%。",
                "本局每秒额外恢复2能量（总计6/秒）；能量仍不超过100。"
            }[(int)blessing] + " 每项最多1层，离开本局清除。";
        }
    }
}

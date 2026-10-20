using System;
using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    // Written atomically with the matching reward identity. Legacy saves may omit it.
    [Serializable]
    public sealed class RewardPresentationReceipt
    {
        public string Id;
        public int Gold,Experience,Materials,RefinementStones;
        public bool FirstCompletion,FirstCoreAvailable;
        public int UnlockedNode=-1,SharedBefore,SharedAfter;
    }
    public static class MasteryProgressionRules
    {
        public const int MaximumRank=35,InitialInvestment=10,EnhancedInvestment=20;
        public static int Cap(int level)
        {return level<30?0:level<35?5:level<50?10:level<65?15:level<80?20:level<95?30:MaximumRank;}
        public static readonly int FirstUnlockLevel=FindFirstUnlockLevel();
        private static int FindFirstUnlockLevel(){for(int level=1;level<=100;level++)if(Cap(level)>0)return level;return 100;}
        public static readonly string TierSummary=BuildTierSummary();
        public static readonly string CoreSummary=InitialInvestment+"点初阶 / "+EnhancedInvestment+"点增强 · 仅启用一个核心";
        private static string BuildTierSummary()
        {
            string levels="",caps="";int previous=Cap(0);
            for(int level=1;level<=100;level++)
            {int cap=Cap(level);if(cap==previous)continue;levels+=(levels.Length>0?"/":"")+level;caps+=(caps.Length>0?"/":"")+cap;previous=cap;}
            return levels+"级 → 上限"+caps+"点";
        }
    }
    public enum HeroClass { Vanguard, Arcanist, Ranger, Summoner }
    public enum ItemSlot { Weapon, Armor, Relic }
    public enum Rarity { Common, Rare, Epic, Legendary }
    public enum FashionSlot { Wings, Weapon }
    public enum MartialSpecialization { Balanced, Assault, Survival }
    public enum ElementalistSpecialization { None, Shatter, Burn }
    public enum EquipmentMechanic { None, FrostEcho, CinderTrail, ReturningBlade, VenomSpread, TwinSummonResonance, WeaponPower, WeaponPrecision, WeaponRuin, ArmorPower, ArmorPrecision, ArmorRuin, RelicPower, RelicPrecision, RelicRuin, WeaponFlow, WeaponTempo, ArmorFlow, ArmorStride, RelicVitality, RelicWard, RelicLeech }
    public enum SummonerRoute { Bonded, Pack }
    public enum MasteryType { Offense, Vitality, Guard, Technique }

    /// <summary>One source of truth for the build codex, acquisition routes and combat contracts.</summary>
    public static class BuildCatalog
    {
        public static EquipmentMechanic[] MechanicsFor(HeroClass hero)
        {
            switch (hero)
            {
                case HeroClass.Arcanist: return new[] { EquipmentMechanic.FrostEcho, EquipmentMechanic.CinderTrail };
                case HeroClass.Vanguard: return new[] { EquipmentMechanic.ReturningBlade };
                case HeroClass.Ranger: return new[] { EquipmentMechanic.VenomSpread };
                case HeroClass.Summoner: return new[] { EquipmentMechanic.TwinSummonResonance };
                default: return new EquipmentMechanic[0];
            }
        }

        public static bool IsAttributeGem(EquipmentMechanic gem)
        {return gem>=EquipmentMechanic.WeaponPower&&gem<=EquipmentMechanic.RelicLeech;}
        public static bool GemCompatible(EquipmentMechanic gem,HeroClass hero)
        {return IsAttributeGem(gem);}
        public static EquipmentMechanic[] GemsFor(HeroClass hero)
        {
            var gems=new List<EquipmentMechanic>();
            for(int i=(int)EquipmentMechanic.WeaponPower;i<=(int)EquipmentMechanic.RelicLeech;i++)gems.Add((EquipmentMechanic)i);
            return gems.ToArray();
        }
        public static EquipmentMechanic[] DroppableGemsFor(HeroClass hero)
        {return Array.FindAll(GemsFor(hero),gem=>gem!=EquipmentMechanic.RelicVitality);}
        public static EquipmentMechanic CurrentGem(EquipmentMechanic gem)
        {
            if(IsAttributeGem(gem)||gem==EquipmentMechanic.None)return gem;
            return gem==EquipmentMechanic.CinderTrail?EquipmentMechanic.WeaponPower:gem==EquipmentMechanic.ReturningBlade?EquipmentMechanic.WeaponRuin:gem==EquipmentMechanic.FrostEcho?EquipmentMechanic.RelicWard:gem==EquipmentMechanic.VenomSpread?EquipmentMechanic.RelicPrecision:EquipmentMechanic.RelicVitality;
        }
        public static int GemAttribute(EquipmentMechanic gem){if(gem==EquipmentMechanic.RelicLeech)return 9;if(gem>=EquipmentMechanic.WeaponFlow&&gem<=EquipmentMechanic.RelicWard)return gem==EquipmentMechanic.WeaponFlow||gem==EquipmentMechanic.ArmorFlow?6:gem==EquipmentMechanic.WeaponTempo?7:gem==EquipmentMechanic.ArmorStride?8:gem==EquipmentMechanic.RelicVitality?3:5;return IsAttributeGem(gem)?(int)gem-(int)EquipmentMechanic.WeaponPower:MechanicAttribute(gem);}
        public static float GemAttributeValue(EquipmentMechanic gem,Rarity rarity,int rank)
        {float[] basis={.03f,.015f,.06f,.05f,.08f,.01f,.04f,.015f,.02f,.0025f};int attribute=GemAttribute(gem);return basis[Math.Max(0,Math.Min(9,attribute))]*(1+(int)rarity)*(1+.2f*rank);}
        public static string AttributeLabel(int attribute)
        {return new[]{"攻击","暴击率","暴击伤害","生命","护甲","受到伤害减免","能量回复","技能冷却缩减","移动速度","吸血"}[Math.Max(0,Math.Min(9,attribute))];}
        public static string GemAttributeLabel(EquipmentMechanic gem){return AttributeLabel(GemAttribute(gem));}
        public static string GemAttributeSummary(EquipmentMechanic gem,Rarity rarity,int rank)
        {return GemAttributeLabel(gem)+" +"+(GemAttributeValue(gem,rarity,rank)*100).ToString("0.#")+"%";}
        public static int MechanicAttribute(EquipmentMechanic gem)
        {return gem==EquipmentMechanic.CinderTrail?0:gem==EquipmentMechanic.ReturningBlade?2:gem==EquipmentMechanic.FrostEcho?4:gem==EquipmentMechanic.VenomSpread?1:3;}
        public static float MechanicAttributeValue(EquipmentMechanic gem,int rank)
        {return (rank+1)*(gem==EquipmentMechanic.CinderTrail?.02f:gem==EquipmentMechanic.VenomSpread?.005f:.03f);}
        public static string GemFormName(EquipmentMechanic gem,int variant)
        {return GemFormRules.Name(CurrentGem(gem),variant);}
        public static string GemFormDescription(EquipmentMechanic gem,int variant,int ascension)
        {return GemFormRules.Description(CurrentGem(gem),variant,Math.Max(1,Math.Min(3,ascension)));}
        public static string GemAscensionLabel(EquipmentMechanic gem){return GemAttributeLabel(gem);}
        public static float GemAscensionValue(EquipmentMechanic gem,int rank){return GemFormRules.Value(gem,0,rank);}
        public static float AscensionPower(int rank){return 1+.2f*Math.Max(0,Math.Min(3,rank));}
        public static float AscensionRange(int rank){return 1+.1f*Math.Max(0,Math.Min(3,rank));}
        public static int GemPrice(Rarity rarity){return rarity==Rarity.Common?4:rarity==Rarity.Rare?8:rarity==Rarity.Epic?12:24;}

        public static HeroClass MechanicClass(EquipmentMechanic mechanic)
        {
            switch (mechanic)
            {
                case EquipmentMechanic.FrostEcho:
                case EquipmentMechanic.CinderTrail: return HeroClass.Arcanist;
                case EquipmentMechanic.VenomSpread: return HeroClass.Ranger;
                case EquipmentMechanic.TwinSummonResonance: return HeroClass.Summoner;
                default: return HeroClass.Vanguard;
            }
        }

        public static ItemSlot MechanicSlot(EquipmentMechanic mechanic)
        {
            if(mechanic==EquipmentMechanic.RelicLeech)return ItemSlot.Relic;
            if(mechanic>=EquipmentMechanic.WeaponFlow&&mechanic<=EquipmentMechanic.RelicWard)return (ItemSlot)(((int)mechanic-(int)EquipmentMechanic.WeaponFlow)/2);
            if(IsAttributeGem(mechanic))return (ItemSlot)(((int)mechanic-(int)EquipmentMechanic.WeaponPower)/3);
            return mechanic == EquipmentMechanic.CinderTrail || mechanic == EquipmentMechanic.ReturningBlade ? ItemSlot.Weapon : ItemSlot.Relic;
        }

        // Shared with combat dispatch and allocation previews; do not duplicate selected-variant budgets.
        public static float ConcentratedVenomCoefficient(int rank){return rank<=1?2.4f:rank==2?3.6f:4.8f;}
        public static float FrostEchoOpeningMultiplier(bool wide){return wide?.65f:.8f;}
        public static float FrostEchoCoefficient(bool wide){return wide?.45f:.6f;}
        public static float FrostEchoRadiusMultiplier(bool wide){return wide?1.35f:1f;}
        public static float CinderTrailRadiusMultiplier(bool concentrated){return concentrated?.7f:1f;}
        public static float CinderTrailTickMultiplier(bool concentrated){return concentrated?1f/7f:.1f;}
        public const float CinderDirectMultiplier=.8f;

        public static string VenomModifier(bool concentrated){return concentrated?"单发 · 收束毒矢":"扇形 · 毒种传播";}
        public static string VenomSkillSummary(int rank,bool concentrated)
        {
            string identity=GameBalance.SkillName(HeroClass.Ranger,0)+" · "+VenomModifier(concentrated)+"\n";
            if(concentrated)return identity+"一发窄幅实体毒矢，直伤 "+(100*ConcentratedVenomCoefficient(rank)).ToString("0.##")+"%攻击；仅首个实际拦截目标，受墙体和前排阻挡，固定目标意图不保证送达。取消扇形、多目标爆炸与毒传播；保留普通三毒引爆，独立且一次。能量/冷却不变。";
            return identity+GameBalance.SkillEvolution(HeroClass.Ranger,0,rank)+"\n变体A：毒爆加成 -20%；每2秒向附近最多2个合法目标传播1层毒素。";
        }
        public static bool ConcentratedVenomEquipped(GameProfile profile)
        {
            if(profile==null||profile.heroClass!=HeroClass.Ranger||profile.inventory==null)return false;
            if(profile.attachments!=null){var a=profile.attachments.Find(x=>x.mechanic==EquipmentMechanic.VenomSpread);if(a!=null)return a.mounted&&a.ascensionRank>0&&a.variantUnlocked&&a.variant==1;}
            var item=profile.inventory.Find(x=>x.id==profile.relicId);
            return item!=null&&item.slot==ItemSlot.Relic&&item.mechanic==EquipmentMechanic.VenomSpread&&item.mechanicVariantUnlocked&&item.mechanicVariant==1;
        }
        public static string VenomSkillOverride(GameProfile profile,int skill,int rank)
        {
            if(profile==null||profile.heroClass!=HeroClass.Ranger||skill!=0||profile.inventory==null)return "";
            if(profile.attachments!=null){var a=profile.attachments.Find(x=>x.mechanic==EquipmentMechanic.VenomSpread);if(a!=null)return !a.mounted||a.ascensionRank<=0?"":VenomSkillSummary(Math.Max(1,Math.Min(3,rank)),a.variantUnlocked&&a.variant==1);}
            var item=profile.inventory.Find(x=>x.id==profile.relicId);
            if(item==null||item.slot!=ItemSlot.Relic||item.mechanic!=EquipmentMechanic.VenomSpread)return "";
            return VenomSkillSummary(Math.Max(1,Math.Min(3,rank)),item.mechanicVariantUnlocked&&item.mechanicVariant==1);
        }

        public static bool HasMechanicVariant(EquipmentMechanic mechanic)
        { return mechanic!=EquipmentMechanic.None&&Enum.IsDefined(typeof(EquipmentMechanic),mechanic); }

        public static string GemName(EquipmentMechanic mechanic)
        {
            if(IsAttributeGem(mechanic))return new[]{"赤锋","鹰眼","裂魂","赤铠","明镜","破军","赤曜","星瞳","灾星","灵汐","疾咏","涌泉","轻羽","长生","星佑","噬生"}[(int)mechanic-(int)EquipmentMechanic.WeaponPower]+"宝石";
            switch(mechanic)
            {
                case EquipmentMechanic.FrostEcho:return "霜鸣宝石";
                case EquipmentMechanic.CinderTrail:return "烬辉宝石";
                case EquipmentMechanic.ReturningBlade:return "旋锋宝石";
                case EquipmentMechanic.VenomSpread:return "碧蚀宝石";
                case EquipmentMechanic.TwinSummonResonance:return "双契宝石";
                default:return "宝石";
            }
        }
        public static string MechanicName(EquipmentMechanic mechanic)
        {
            switch (mechanic)
            {
                case EquipmentMechanic.FrostEcho: return "霜回护符";
                case EquipmentMechanic.CinderTrail: return "余烬法杖";
                case EquipmentMechanic.ReturningBlade: return "回刃长剑";
                case EquipmentMechanic.VenomSpread: return "蔓毒护符";
                case EquipmentMechanic.TwinSummonResonance: return "双契共鸣";
                default: return "无机制";
            }
        }

        public static string MechanicDescription(EquipmentMechanic mechanic)
        {
            if(IsAttributeGem(mechanic))return "基础属性："+GemAttributeLabel(mechanic)+"加成。首次升华解锁两种形态；3、6、9阶各可升华一次。变体A："+GemFormDescription(mechanic,0,1)+"变体B："+GemFormDescription(mechanic,1,1);
            switch (mechanic)
            {
                case EquipmentMechanic.FrostEcho: return "变体A：凝霜回响 · 新星首击保留80%伤害；0.7秒后原范围回响造成60%基础伤害并再次控制。变体B：扩散霜环 · 回响范围扩大35%；首击保留65%、回响造成45%基础伤害，适合控制分散敌人。灼燃专精仅减速。";
                case EquipmentMechanic.CinderTrail: return "变体A：燎原余烬 · 陨星直伤降低20%，保留完整半径火场；每0.5秒造成首陨基础伤害的10%，共4跳、合计40%，适合覆盖敌群。变体B：凝焰火核 · 陨星直伤降低20%，火场半径缩小30%；每跳约14.3%，共4跳、合计约57.1%，适合集火首领。";
                case EquipmentMechanic.ReturningBlade: return "普攻伤害 -8%；每1.5秒回刃弹向4米内另一个目标，造成110%基础伤害；回收后强化下一刀，击杀再弹65%。变体B：放弃全部弹射与回收强化，取消普攻-8%；真实完美闪避的反击窗口延至3秒，下次175%反击改窄刺，可沿合法地面前进最多2米。";
                case EquipmentMechanic.VenomSpread: return "变体A：毒素引爆加成伤害 -20%；每2秒向附近最多2个目标传播1层毒素。变体B：扇形箭改为一发窄幅实体毒矢，直伤240%/360%/480%攻击，仅命中首个拦截目标；无扇形、爆炸和传播，保留普通三毒引爆，能量与冷却不变。";
                case EquipmentMechanic.TwinSummonResonance: return "普通召唤上限改为2；伙伴伤害 +60%、生命 +20%。不同类型伙伴1.5秒内实际命中同一目标时共鸣追加35%基础伤害，冷却3秒。";
                default: return "装备机制只在穿戴且职业匹配时生效。";
            }
        }

        public static string MechanicSource(EquipmentMechanic mechanic)
        {
            if (mechanic == EquipmentMechanic.None || !Enum.IsDefined(typeof(EquipmentMechanic), mechanic)) return "无";
            return "副本宝石由星台封印通关宝箱产出：每次1颗，史诗80%、传说20%。升阶最高9阶；3、6、9阶各可消耗24碎片升华一次，强化机制；首次升华解锁支持的机制形态。";
        }

        public static string MartialSpecializationName(HeroClass hero,MartialSpecialization choice)
        {return choice==MartialSpecialization.Assault?(hero==HeroClass.Vanguard?"破阵":"鹰眼"):choice==MartialSpecialization.Survival?(hero==HeroClass.Vanguard?"铁壁":"疾风"):"均衡";}
        public static string MartialSpecializationDescription(HeroClass hero,MartialSpecialization choice)
        {
            if(choice==MartialSpecialization.Balanced)return "均衡：保持原有攻击、防御与机动能力；专精可免费切换。";
            if(hero==HeroClass.Vanguard)return choice==MartialSpecialization.Assault?"破阵：攻击提高15%，护甲降低10%，偏向主动进攻。":"铁壁：护甲提高25%、生命提高10%，攻击降低8%，偏向近身生存。";
            return choice==MartialSpecialization.Assault?"鹰眼：暴击率增加8个百分点、暴击伤害增加20个百分点，移动速度降低5%。":"疾风：移动速度提高10%、技能冷却缩减增加10个百分点，攻击降低8%，偏向游走与持续释放技能。";
        }
        public static string SpecializationName(ElementalistSpecialization specialization)
        {
            return specialization == ElementalistSpecialization.Shatter ? "碎冰" : specialization == ElementalistSpecialization.Burn ? "灼燃" : "均衡";
        }

        public static string SpecializationDescription(ElementalistSpecialization specialization)
        {
            if (specialization == ElementalistSpecialization.Shatter) return "普攻积霜，新星冻结，陨星首个满足条件的命中碎冰；陨星直伤 -15%、碎冰 +100%。霜环延长霜痕，终极法术先积霜后引爆。";
            if (specialization == ElementalistSpecialization.Burn) return "普攻余烬、新星减速留人、陨星直伤 -20%并灼烧90%。霜焰行者提高移速并留下火区；终极法术降低直伤换持续灼烧。";
            return "保留原有冰霜控制；陨星消耗冰霜印记，碎冰追加50%基础伤害。专精仅改变元素师，可免费切换。";
        }

        public static string ClassSignatureDescription(HeroClass hero)
        {
            if (hero == HeroClass.Vanguard) return "完美闪避真正避开攻击时回复12能量；2秒内下一次普攻造成175%伤害。";
            if (hero == HeroClass.Arcanist) return "对同一目标3次普攻触发霜触；学新星后转为3秒霜痕，灼燃转为余烬。完美闪避强化下次命中的霜痕或余烬，3秒不叠加。";
            if (hero == HeroClass.Ranger) return "普攻叠加最多3层毒素；扇形箭消耗3层引爆，每个目标每次施法仅触发一次。";
            return "以灵能法术直接攻击；召唤限时灵狼追击、星灵炮台远射，大招召唤树灵范围重击。召唤物自动攻击，到期消失。";
        }

        public static string MasteryName(MasteryType mastery)
        {
            return new[] { "破敌精通", "生命精通", "坚壁精通", "技巧精通" }[(int)mastery];
        }
        public static string MasteryDescription(MasteryType mastery)
        {
            return new[] { "每点攻击 +0.3%；核心：技能命中后6秒内下一次普攻追加一次非暴击攻击，初阶60% / 增强100%攻击伤害；冷却6 / 4秒。",
                "每点生命 +0.5%；核心：受到实际伤害后生命低于50%且存活时，恢复3% / 5%最大生命；冷却12 / 10秒，不会复活。",
                "每点护甲 +0.75%；核心：完美闪避获得15% / 25%减伤，持续2 / 3秒；冷却8 / 6秒。",
                "每点普攻回能 +0.1；核心：技能实际消耗累计60 / 45能量，回复8 / 12能量并减冷却0.4 / 0.7秒；冷却8 / 6秒，冷却中不累计。" }[(int)mastery]
                + MasteryProgressionRules.TierSummary+"；"+MasteryProgressionRules.CoreSummary+"。营地可重置职业精通。";
        }
        public const string DamageRules = "暴击：普攻与直接技能每次施法掷骰，延迟命中继承结果；持续伤害、反应和伙伴不暴击。伙伴继承当前攻击与生命及契约阶数，数量与召唤模式影响输出。";

    }

    /// <summary>Durable chest receipt. Unity serializes the fields; nullable view properties
    /// let the reveal UI distinguish a gold-only reward without unsupported nullable fields.</summary>
    public enum RewardMaterialKind { None, StarAshFragment }
    public enum ChestRewardKind { Legacy, SingleChest }
    [Serializable]
    public class MaterialExchangeReceipt
    { public string id;public long sequence;public RewardMaterialKind materialKind;public int threadsDelta,materialsDelta; }
    // Display snapshots only. Unselected contents never enter inventory or grant paths.
    [Serializable]
    public class ChestContentPreview
    {
        public int kind,count;
        public ItemData equipment;
        public FashionData fashion;
        public EquipmentMechanic gemMechanic;
        public Rarity gemRarity;
    }
    [Serializable]
    public class ChestReward
    {
        public ChestContentPreview[] contents;
        public int primaryKind,primaryCount,primarySlot,refinementStonesDelta,affixReforgeStonesDelta,basicAttackMaterialsDelta;
        public bool HasLegendaryMaterial {get{return basicAttackMaterialsDelta>0||affixReforgeStonesDelta>0;}}
        public Rarity primaryRarity;
        public string[] equipmentIds;
        public EquipmentMechanic gemMechanic;
        public bool duplicateGem;
        public Rarity gemRarity=Emberfall.Rarity.Epic;
        public int selectedChest=-1;
        // Zero is the historical identical-chest schema; never infer it from choice.
        public int rulesRevision;
        public bool hasCurrencyDeltas;
        public int goldDelta, threadsDelta,materialsDelta;
        public ChestRewardKind rewardKind;public RewardMaterialKind materialKind;
        public int materials,baseGold,duplicateGold,baseThreads,duplicateThreads;public bool legacyGoldProtection;
        public string id;
        public int choice;
        public int gold;
        public int rarityIndex = -1;
        public int appearanceTier = -1;
        public int slotIndex = -1;
        public string name;
        public bool duplicate;
        public string summary;
        public string Id { get { return id; } }
        public int Gold { get { return gold; } }
        public Rarity? Rarity { get { return rarityIndex < 0 ? (Emberfall.Rarity?)null : (Emberfall.Rarity)rarityIndex; } }
        public FashionSlot? Slot { get { return slotIndex < 0 ? (FashionSlot?)null : (FashionSlot)slotIndex; } }
        public string Name { get { return name; } }
        public bool Duplicate { get { return duplicate; } }
    }

    [Serializable]
    public class FashionData
    {
        public string id;
        public FashionSlot slot;
        public Rarity rarity;
        public string name;
        // Quality determines the collected silhouette; ranks add details without changing its tier.
        public int appearanceTier=-1;
        public int upgradeRank;
        public Rarity VisualRarity {get{return AppearanceRarity;}}
        public Rarity AppearanceRarity {get{return appearanceTier>=0&&appearanceTier<=3?(Rarity)appearanceTier:rarity;}}
    }
    public enum EnemyKind { Slime, Goblin, Wisp, Guardian }
    public enum ZoneKind { Wilderness, Dungeon }
    public enum SkillCategory { Damage, Control, Mobility, Buff, Defense, Healing }

    public enum RewardMomentKind { FirstCore, MechanicExchange, Ascension, MaterialExchange, FashionExchange, StrictUpgrade }
    // Transient presentation snapshot, never serialized into GameProfile or used to grant.
    public sealed class RewardMoment
    {
        public long Sequence; public string SlotId; public HeroClass HeroClass; public RewardMomentKind Kind;
        public ItemData Item; public FashionData Fashion; public MechanicAttachment Attachment; public int GoldDelta,MaterialsDelta,ThreadsDelta;
    }

    [Serializable]
    public class ItemData
    {
        public string id;
        public string name;
        public ItemSlot slot;
        public Rarity rarity;
        public int level;
        public int attack;
        public int defense;
        public int health;
        public float criticalChance,criticalDamageBonus,attackPercent;
        public int statRollRevision;
        public int upgradeLevel;
        public int enhancementRevision;
        public EquipmentMechanic mechanic;
        public bool locked;
        public int mechanicVariant;
        public bool mechanicVariantUnlocked;
        public int balanceRevision;
        // Persistent origins make changing a rank reversible without repeatedly
        // rounding already-upgraded attributes. Legacy saves initialize these once.
        public bool upgradeBaseInitialized;
        public int baseAttack;
        public int baseDefense;
        public int baseHealth;
        public int upgradeAnchorLevel;
        public int upgradeAnchorAttack;
        public int upgradeAnchorDefense;
        public int upgradeAnchorHealth;
    }

    /// <summary>Legacy save data only; preset editing and switching have been retired.</summary>
    [Serializable]
    public class BuildPreset
    {
        public int version = 1;
        public bool populated;
        public HeroClass heroClass;
        public int[] skillRanks;
        public int[] masteryRanks;
        public int masteryCore = -1;
        public ElementalistSpecialization specialization;
        public MartialSpecialization martialSpecialization;
        public SummonerRoute summonerRoute;
        public int[] equippedSkills;
        public int[] hotbarKeys;
        public int hotbarPage;
        public string weaponId, armorId, relicId;
        // Null in legacy presets: preserve current item variants. -1 means no unlocked variant captured.
        public int[] equipmentVariants;
        public EquipmentMechanic[] equipmentMechanics;
        // A zero enum entry is not evidence: each slot explicitly records whether its mechanism is known.
        public int equipmentMechanicKnownMask;
        public EquipmentMechanic[] mountedAttachments;
        public int[] attachmentVariants;
    }

    /// <summary>Fixed class-local allocations. Inventory, equipment identity and rewards live only on GameProfile.</summary>
    [Serializable]
    public sealed class ClassBuildState
    {
        public int version=1;
        public bool initialized;
        public HeroClass heroClass;
        public int[] skillRanks,masteryRanks,equippedSkills,hotbarKeys;
        public EquipmentMechanic[] mountedGems;
        public int masteryCore=-1,hotbarPage,tutorialMask;
        public ElementalistSpecialization specialization;
        public MartialSpecialization martialSpecialization;
        public SummonerRoute summonerRoute;
        public BuildPreset[] buildPresets;
        public bool classTutorialCompleted;
        public int growthRevision;
        public bool automaticGrowth=true;
        public ProgressionGoalKind progressionGoal;
        public string progressionGoalItemId;
        public int progressionGoalTier,progressionGoalLevel;
        public EquipmentMechanic progressionGoalMechanic;
        public Rarity progressionGoalMinimumRarity;
    }

    public enum ProgressionGoalKind { None, Core, Variant, Ascension, SecondPreset, Tier, Reforge, ClassTutorial, CombatTrial }

    [Serializable]
    public sealed class MechanicAttachment
    {
        public string id, legacySourceId;
        public EquipmentMechanic mechanic;
        public int level=1, upgradeRank, variant;
        public int ascensionRank=-1;
        public Rarity rarity=Rarity.Epic;
        public bool mounted=true, variantUnlocked;
    }

    public static class SkillStockRules
    {
        public static int Skill(HeroClass hero){return hero==HeroClass.Vanguard?5:hero==HeroClass.Arcanist?4:hero==HeroClass.Ranger?4:0;}
        public static float Seconds(HeroClass hero){return hero==HeroClass.Vanguard?12:hero==HeroClass.Arcanist?9:hero==HeroClass.Ranger?14:8;}
        public static void Normalize(GameProfile profile)
        {
            bool legacy=profile.skillStockVersion<1;
            if(profile.skillStockCounts==null||profile.skillStockCounts.Length!=4)profile.skillStockCounts=new int[4];
            if(profile.skillStockRemaining==null||profile.skillStockRemaining.Length!=4)profile.skillStockRemaining=new float[4];
            if(profile.skillStockPeriods==null||profile.skillStockPeriods.Length!=4)profile.skillStockPeriods=new float[4];
            for(int i=0;i<4;i++)
            {
                float basis=Seconds((HeroClass)i),period=profile.skillStockPeriods[i],remaining=profile.skillStockRemaining[i];
                int count=legacy?2:Math.Max(0,Math.Min(2,profile.skillStockCounts[i]));
                if(float.IsNaN(period)||float.IsInfinity(period)||period<basis*.7f||period>basis)period=basis;
                if(count==2)remaining=0;
                else if(float.IsNaN(remaining)||float.IsInfinity(remaining)||remaining<=0||remaining>period)remaining=period;
                profile.skillStockCounts[i]=count;profile.skillStockPeriods[i]=period;profile.skillStockRemaining[i]=remaining;
            }
        }
    }

    [Serializable]
    public class GameProfile
    {
        public int rewardInventoryRevision;
        public int skillStockVersion;
        public int[] skillStockCounts;
        public float[] skillStockRemaining,skillStockPeriods;

        public int attachmentRevision;
        public List<MechanicAttachment> attachments=new List<MechanicAttachment>();
        public List<string> growthRewardReceipts=new List<string>();
        public List<string> achievementReceipts=new List<string>();
        public bool automaticGrowth=true;
        public int growthRevision;
        public int classStateRevision;
        public ClassBuildState[] classStates;
        public int version = 1;
        public HeroClass heroClass;
        public int level = 1;
        public int xp;
        public int gold = 60;
        public int potions = 5;
        public int revivalStones;
        public int[] gemBags=new int[3];
        public int gemBagSeed,gemBagsOpened;
        public int retiredGemRefundShards;
        public int[] revivalClaimedTiers=new int[3];
        public int skillPoints;
        public int tutorialMask;
        public bool classTutorialCompleted;
        public ProgressionGoalKind progressionGoal;
        public string progressionGoalItemId;
        public int progressionGoalTier;
        public EquipmentMechanic progressionGoalMechanic;
        public Rarity progressionGoalMinimumRarity;
        public int progressionGoalLevel;
        public int highestAdventureTier;
        public int[] adventureBestTiers = new int[5];
        public int adventureRewardRevision;
        public int legendaryEquipmentMisses;
        public int independentTierRevision;
        public bool pendingAdventureChest;
        public int pendingChestMode = -1;
        public int groundGold, groundPotions;
        public int[] chapterBestTiers=new int[3];
        public int[] chapterBestLevels=new int[3];
        public int chapterMasteryMask;
        public int[] chapterMasteryTiers = new int[4];
        public int chapterRevision;
        public int chapterTierRevision;
        // Legacy deserialize-only field; normalized into chapterBestTiers and cleared.
        public int[] chapterDifficultyBestTiers;
        [System.NonSerialized] internal bool chapterTierMigrationPending;
        public int chapterCompletedMask;
        public int chapterFirstRewardMask;
        public long chapterRewardSequence;
        public string lastChapterRewardId;
        public int chapterHighestAdventureTier;
        public int chapterPriorAdventureTier;
        public ElementalistSpecialization specialization;
        public MartialSpecialization martialSpecialization;
        public int[] masteryRanks = new int[4];
        public int masteryCore = -1;
        public int masteryRevision;
        public SummonerRoute summonerRoute;
        public BuildPreset[] buildPresets;
        public int fashionThreads;
        // Slot training is the sole enhancement authority. Item upgradeLevel is
        // only the current equipped-stat cache; legacy anchors preserve item bases.
        public int[] slotUpgradeRanks = new int[3];
        public bool slotUpgradesInitialized;
        public int equipmentEnhancementRevision, equipmentUpgradeAttempts;
        [System.NonSerialized] internal bool enhancementMigrationPending;
        public int mechanicMaterials;
        public int affixReforgeStones,affixReforgeCount;
        public int basicAttackMaterials;
        public int forestCrystalMisses,redrockReforgeMisses,arenaReforgeMisses,corridorFashionMisses;
        public int[] basicAttackTiers = new int[4];
        public int refinementStones,refinementCount,refinementMaxCount;
        public bool pendingChestGemSource;
        public bool pendingChestChapterSource;
        public int variantKnowledgeRevision;
        public List<EquipmentMechanic> variantKnowledge = new List<EquipmentMechanic>();
        public int materialRewardedClears;
        public bool firstClearRewardClaimed;
        public bool pendingFirstClearReward;
        public List<EquipmentMechanic> discoveredMechanics = new List<EquipmentMechanic>();
        public List<ItemData> pendingLoot = new List<ItemData>();
        public List<ItemData> recoveryLoot = new List<ItemData>();
        public bool autoSellCommon;
        public bool autoSellRare;
        public int[] skillRanks = new int[GameBalance.SkillCount];
        public int[] equippedSkills = GameBalance.DefaultLoadout();
        public int hotbarPage;
        public int[] hotbarKeys = (int[])GameBalance.DefaultHotbarKeys.Clone();
        public int kills;
        // Durable stage checkpoint; paid exactly once at the run result, never on a hit.
        public int dungeonRewardRevision, dungeonExperience, dungeonGold, dungeonPotions;
        public List<ItemData> dungeonLoot = new List<ItemData>();
        public int clearedRuns;
        public int bestFloor;
        public string lastModeRewardId;
        public string lastDungeonRewardId;
        public RewardPresentationReceipt lastModeRewardDetails,lastDungeonRewardDetails,lastChapterRewardDetails;
        public List<string> sideEventRewardReceipts = new List<string>();
        public int quarryWorkLevel,starChartTier;
        public int currentHub;
        public int unlockedHubMask=1;
        public List<ItemData> inventory = new List<ItemData>();
        public string weaponId;
        public string armorId;
        public string relicId;
        public int fashionQualityRevision;
        public List<FashionData> fashions = new List<FashionData>();
        public string wingsFashionId;
        public string weaponFashionId;
        public int chestRulesRevision,pendingChestRulesRevision;public bool pendingChestLegacyGoldProtection;
        public string pendingChestQualificationId;public ChestReward pendingChestDraw;
        public long threadMaterialSequence;public MaterialExchangeReceipt lastThreadMaterialReceipt;
        public bool pendingFashionChest;
        public int pendingChestTier = 1;
        public ChestReward lastChestReward;
        public bool pendingChestReveal;
    }

    public struct StatBlock
    {
        public float MaxHealth;
        public float Damage;
        public float Armor;
        public float MoveSpeed;
        public float CritChance;
        public float CritDamageBonus;
        public float DamageReduction,EnergyRecovery,CooldownReduction,LifeSteal;
        public float GemHealthyDamage,GemLowHealthGuard,GemLowEnergyRecovery,GemLowHealthDamage,GemHealthyGuard,GemHighEnergyRecovery;
    }

    public static class GameBalance
    {
        public const float ArcanistFinaleRadius=16f,ArcanistPulseRadius=13.6f;
        public const int SkillCount = 11;
        public const int TrackingLaserSkill = 10;
        public static int SkillCountFor(HeroClass hero) { return hero==HeroClass.Arcanist?11:10; }
        public const int HotbarSize = 10;
        public const int HotbarPotion = -2;
        public const int HotbarPages = 3;
        public static readonly int[] DefaultHotbarKeys = { 49, 50, 51, 52, 53, 122, 120, 99, 118, 98 };
        public static readonly string[] ClassNames = { "剑卫", "元素师", "游侠", "唤灵师" };
        public static readonly string[] ClassDescriptions = {
            "挥剑近战 · 旋风斩击 · 坚韧生存",
            "连击霜印 · 冰霜控制 · 陨星爆发",
            "灵巧射击 · 散射箭雨 · 灵活游走",
            "契约召唤 · 灵狼星灵 · 协同作战"
        };
        public static readonly Color[] ClassColors = {
            new Color(1f, .65f, .26f), new Color(.4f, .7f, 1f), new Color(.38f, .88f, .65f), new Color(.83f, .63f, 1f)
        };
        public static readonly int[] SkillRequiredLevels = { 1, 4, 6, 4, 10, 6, 13, 20, 13, 30, 20 };
        public static readonly int[][] SkillPrerequisites = {
            new int[0], new[] { 0 }, new[] { 1 }, new[] { 0 }, new[] { 2 },
            new[] { 3 }, new[] { 5 }, new[] { 4 }, new[] { 5 }, new[] { 7, 6 }, new[] { 4 }
        };
        public static int SkillTreeRow(int skill) { return new[] { 0, 1, 2, 1, 3, 2, 4, 5, 4, 6, 5 }[skill]; }
        public static float SkillTreeColumn(int skill) { return new[] { 1f, 0f, 0f, 2f, 0f, 2f, 1f, 0f, 2f, 1f, 2f }[skill]; }
        public static string PrerequisiteDescription(HeroClass hero, int skill)
        {
            int[] parents = SkillPrerequisites[skill];
            if (parents.Length == 0) return "起始技能 · 无前置";
            string result = "前置：";
            for (int i = 0; i < parents.Length; i++) result += (i == 0 ? "" : " + ") + SkillName(hero, parents[i]);
            return result;
        }
        // Budgets reflect damage coverage, control uptime and protection, rather
        // than position in the skill tree. IDs stay stable for existing saves.
        // Dash and chain attacks cycle faster than persistent fields; shields
        // leave an exposed interval even at rank three. Summons have a hard cap.
        private static readonly float[,] ClassSkillCooldowns = {
            { 5f, 9f, 18f, 0f, 28f, 11f, 32f, 16f, 0f, 42f },
            { 10f, 11f, 22f, 0f, 8f, 26f, 34f, 20f, 0f, 45f },
            { 6f, 12f, 19f, 0f, 12f, 18f, 32f, 16f, 0f, 40f },
            { 7f, 16f, 14f, 0f, 12f, 28f, 36f, 21f, 0f, 46f }
        };
        private static readonly float[,] ClassSkillEnergyCosts = {
            { 12f, 20f, 34f, 0f, 30f, 24f, 28f, 38f, 0f, 64f },
            { 18f, 28f, 42f, 0f, 24f, 28f, 30f, 38f, 0f, 68f },
            { 14f, 20f, 34f, 0f, 22f, 30f, 28f, 42f, 0f, 64f },
            { 16f, 28f, 30f, 0f, 28f, 32f, 36f, 40f, 0f, 70f }
        };
        private static readonly string[,] SkillNames = {
            { "旋风斩", "裂地冲击", "剑刃风暴", "剑术精研", "破空剑气", "破军突进", "生命战旗", "大地崩裂", "不屈意志", "终焉裁决" },
            { "冰霜新星", "陨星术", "虚空漩涡", "奥能亲和", "雷霆锁链", "霜焰行者", "上升重力", "重力开关", "元素回响", "流星雨" },
            { "扇形箭", "震荡陷阱", "天幕箭雨", "弱点洞察", "逐风跃击", "毒蔓牢笼", "疾风穿云", "幻影连射", "灵风庇佑", "万箭归星" },
            { "灵能冲击", "荆棘牢笼", "灵狼召唤", "灵魂共鸣", "星灵炮台", "灵魂长矛", "烬羽召唤", "引力印记", "契约余响", "远古树灵" }
        };
        private static readonly string[,] SkillDescriptions = {
            {
                "旋转斩击周围敌人。低消耗、短冷却，觉醒后牵引收束。", "向前方重击，击退并击倒敌人；可打断带青色符号的首领预警，首领打断后5秒免疫再次打断。", "连续释放剑气，切割周围的敌人。",
                "被动：永久提高攻击与防御，学习后自动生效。", "向前释放大型剑气，持续推进并击退沿途怪物；每个目标命中一次，遇障碍停止。", "向前突进并连续斩击，撕开敌阵。",
                "树立生命战旗，持续恢复生命；升阶获得防护与回复能量。", "沿前方逐段引爆地脉，将普通/精英敌人击飞；首领免疫浮空，仍受到伤害。", "被动：濒危时自动触发减伤防护；触发后有独立内置冷却。",
                "巨剑裁决与多段剑阵爆发，终结大范围敌群。无需战意，适合聚怪后的爆发。"
            },
            {
                "向角色前方较长的矩形区域释放冰刺，持续4.2至5.4秒，将普通/精英敌人抛起；目标受重力下落，回到冰刺高度后会再次被击飞。首领保留霜痕，并可打断其带青色符号的预警。", "在瞄准地点降下陨星，造成范围爆发，将普通/精英敌人垂直抛起后自然落地；主冲击可打断带青色符号的首领预警。", "创造旋转的虚空漩涡，将敌人吸向中心并反复撕裂。升阶增大主体与吸附范围。",
                "被动：永久提高法术攻击与生命，学习后自动生效。", "雷霆在敌人间跳跃，逐个造成伤害并短暂眩晕。", "踏焰疾行，撞击敌人并留下灼烧火径。",
                "召唤持续翻滚的大型重力球，周围气流螺旋上升；将范围内普通与精英怪物压倒在地，持续造成伤害。", "以角色为圆心反转重力，将周围普通与精英怪物抬到空中，再受重力坠落；升空、滞空、落地共造成3次范围伤害，首领承受伤害但免疫浮空。", "被动：受伤后释放元素反击并回复能量，具有内置冷却。",
                "向角色前方大片矩形和身后小片矩形区域持续召来陨石；命中普通与精英怪物时将其砸起，随后自然坠地。无需奥能，释放后可自由行动。"
            },
            {
                "向前方发射多支穿透箭矢。短冷却，适合清理敌群。", "在瞄准地点引爆陷阱，伤害并眩晕敌人；主爆炸可打断带青色符号的首领预警。", "向瞄准地点持续倾泻箭雨。",
                "被动：永久提高暴击几率和移动速度，学习后自动生效。", "向瞄准方向跃进5米，0.55秒后砸地，造成范围伤害、击退并击倒普通/精英敌人；空中不能普攻，起跳仅有0.18秒保护，落地获得机动增益。", "毒蔓使敌人减速并叠加中毒，最多3层；离开毒区后毒伤仍会持续。",
                "向前射出三支穿透风箭，并获得机动与普攻增益。", "锁定选区内的一名敌人持续追射，并使其受到的伤害提高12%至20%。目标死亡后不自动转锁。", "被动：受伤时自动短暂无敌并提高移动速度，具有内置冷却。",
                "连续向目标周围降下实体箭雨，最后集中落箭冲击；觉醒后终爆追加放射箭。无需专注，适合敌群密集时使用。"
            },
            {
                "向前方释放灵能冲击，造成扇形伤害并将敌人轰开；可打断带青色符号的首领预警。", "在目标地点生长荆棘，使敌人减速并持续中毒；首领减速效果降低。", "召唤灵狼自动追击并近战撕咬，持续14秒；群契模式同时召唤两只。再次施放替换旧灵狼。",
                "被动：提高自身攻击，召唤物继承强化后的攻击。学习后永久生效。", "在选定地点架起木架星灵炮台，自动发射追踪炮弹，射程10.5米，持续12秒；炮台固定不动，再次施放替换旧炮台。", "自身发射穿透敌人的灵魂长矛，造成260%攻击伤害。",
                "召唤由炽热熔石与燃烧羽焰构成的烬羽灵，自动移动并发射穿透灵羽攻击敌人，持续12秒；再次施放替换旧烬羽灵。", "在目标上方召唤能量球，持续4.2秒压制范围内的普通/精英敌人，使其倒地无法行动，结束时爆发并短暂击倒；首领承受伤害但免疫倒地。", "被动：受伤时释放灵能反击并回复灵力，具有独立内置冷却。",
                "召唤一只远古树灵守卫，持续24秒，使用范围重击并击倒敌人；最多存在1只树灵；无需灵力；每2.5秒为6米内可见的主人恢复10%/12%/14%最大生命，树灵范围重击造成225%攻击伤害。"
            }
        };
        private static readonly string[,] ReinforcedEffects = {
            { "追加一段旋斩；伤害+30%，范围+15%。", "追加震荡，延长眩晕；伤害+30%，范围+15%。", "剑刃风暴持续4.35秒；整段伤害单独预算，范围+15%。", "", "大型剑气造成390%攻击伤害，向前推进并强化击退。", "冲锋末端追加爆发；伤害+30%，路径扩大。", "", "裂地增加至7段，逐段击飞；伤害+30%，范围+15%。", "", "裁决增加至7段；伤害+30%，范围+15%。" },
            { "前方矩形区域追加一次冰刺冲击；伤害+30%，范围+15%。", "双陨星连续落下；伤害+30%，范围+15%。", "风暴脉冲加快；整段伤害单独预算，范围+15%。", "", "连锁最多8个目标；伤害+30%，范围+15%。", "霜焰持续8秒，强化霜环伤害；灼燃沿途留下火区。", "", "漩涡攻击增加至11次；伤害+30%，范围+15%。", "", "天灾增加至7段；伤害+30%，范围+15%。" },
            { "增加至7支箭矢；伤害+30%，作用范围+15%。", "陷阱追加余震；伤害+30%，范围+15%。", "箭雨持续6.5秒；整段伤害单独预算，范围+15%。", "", "跃击范围+15%，落地伤害提高，获得6秒机动与普攻增益。", "毒区持续更久；伤害+30%，范围+15%。", "", "连续射出17支穿透箭；伤害+30%，射程扩大。", "", "箭幕增加至10段；伤害+30%，范围+15%。" },
            { "灵能冲击伤害+30%，范围+15%，击退敌人。", "荆棘伤害+30%，范围+15%，延长减速与中毒覆盖。", "灵狼攻击+30%，持续18秒；自动追击近处敌人。", "", "炮台攻击+30%，持续16秒，固定位置自动发射灵弹。", "灵魂长矛伤害+30%，穿透范围+15%。", "", "引力印记伤害+30%，范围+15%，持续倒地压制。", "", "树灵持续30秒，重击伤害+30%，范围扩大。" }
        };
        private static readonly string[,] AwakenedEffects = {
            { "三段旋斩并牵引收束；伤害+60%，范围+35%。", "追加终结冲击与强化眩晕；伤害+60%，范围+35%。", "持续5.1秒，牵引敌人并追加收尾爆发。", "", "大型剑气造成480%攻击伤害，推进距离与宽度增加，强力击退。", "冲锋终点爆发，起点追加残影冲击。", "", "8段裂地逐段击飞，并追加末端爆破；伤害+60%，范围+35%。", "", "8段裁决，终击留下2秒剑气领域。" },
            { "前方矩形区域追加冲击，并向前发射8枚冰片；同目标冰片递减并设上限。", "双陨星后留下2秒灼烧领域。", "高速脉冲聚拢敌人，并追加收束爆发。", "", "连锁最多10个目标，每个节点追加溅射。", "霜焰持续10秒，每秒释放强化霜环；灼燃沿途留下火区。", "", "13次漩涡攻击，更强吸附并追加终结爆发。", "", "8段天灾，终击留下聚怪元素领域。" },
            { "9支穿透箭，命中追加爆裂；同目标后续命中递减，伤害有上限。", "引爆前吸附敌人，随后追加余震。", "持续7.5秒箭雨，并在结束时追加爆发。", "", "跃击范围+35%，起点追加残影陷阱，落地获得7秒机动与普攻增益。", "毒区吸附敌人，并在结束时追加爆裂。", "", "连续射出20支追踪穿透箭，强化覆盖能力。", "", "11段箭幕，终爆追加12支放射箭。" },
            { "灵能冲击伤害+60%，范围+35%，将敌群轰开。", "荆棘伤害+60%，范围+35%，强化毒伤与区域控制。", "灵狼攻击+60%，持续22秒；自动追击近处敌人。", "", "炮台攻击+60%，持续20秒，固定位置自动发射灵弹。", "灵魂长矛伤害+60%，穿透范围+35%。", "", "引力印记伤害+60%，范围+35%，能量球持续压制并爆发。", "", "树灵持续36秒，重击伤害+60%，范围扩大。" }
        };
        public static string ClassName(HeroClass value) { return ClassNames[(int)value]; }
        public static Color ClassColor(HeroClass value) { return ClassColors[(int)value]; }
        public static string SkillName(HeroClass value, int slot) { return slot==10?"巡星天柱":SkillNames[(int)value, slot]; }
        public static string SkillDescription(HeroClass value, int slot) { return slot==10?"在地面召出冲天激光圆柱，脱手后自动移动追踪附近敌人；目标死亡后重新寻敌，持续灼击范围内怪物。":SkillDescriptions[(int)value, slot]; }
        public static string EnergyName(HeroClass value) { return new[] { "战意", "奥能", "专注", "灵力" }[(int)value]; }
        public static bool IsPassive(int skill) { return skill == 3 || skill == 8; }
        public static int SkillPointBudget(int level) { return Math.Max(1, level - 1); }
        public static int SkillPointsGainedAtLevel(int level) { return level <= 1 ? 0 : SkillPointBudget(level) - SkillPointBudget(level - 1); }
        public static int SkillRankRequiredLevel(int skill, int rank) { return rank <= 1 ? SkillRequiredLevels[skill] : Math.Max(2, SkillRequiredLevels[skill]) + (rank == 2 ? 8 : 18); }
        public static float SkillRangeMultiplier(int rank) { return rank <= 1 ? 1f : rank == 2 ? 1.15f : 1.35f; }
        public static float SkillRangeMultiplier(HeroClass hero,int skill,int rank)
        { return hero==HeroClass.Arcanist&&skill==2 ? (rank<=1?1f:rank==2?1.35f:1.75f) : SkillRangeMultiplier(rank); }
        public static string SkillRankName(int rank) { return rank <= 0 ? "未习得" : rank == 1 ? "初习" : rank == 2 ? "强化" : "觉醒"; }
        public static SkillCategory GetSkillCategory(HeroClass hero, int skill)
        {
            if (skill == 3) return SkillCategory.Buff;
            if (skill == 8) return hero == HeroClass.Vanguard || hero == HeroClass.Ranger ? SkillCategory.Defense : SkillCategory.Damage;
            if (skill == 6) return hero == HeroClass.Vanguard ? SkillCategory.Healing : hero==HeroClass.Arcanist?SkillCategory.Control:SkillCategory.Damage;
            if (hero == HeroClass.Vanguard)
            {
                if (skill == 1 || skill == 7) return SkillCategory.Control;
                if (skill == 4) return SkillCategory.Control;
                if (skill == 5) return SkillCategory.Mobility;
            }
            else if (hero == HeroClass.Arcanist)
            {
                if (skill == 0 || skill == 2 || skill == 7) return SkillCategory.Control;
                if (skill == 5) return SkillCategory.Damage;
            }
            else if (hero == HeroClass.Ranger)
            {
                if (skill == 1 || skill == 5) return SkillCategory.Control;
                if (skill == 4) return SkillCategory.Mobility;
            }
            else if (hero == HeroClass.Summoner)
            {
                if (skill == 1 || skill == 7) return SkillCategory.Control;
                if (skill == 5) return SkillCategory.Damage;
            }
            return SkillCategory.Damage;
        }
        public static string CategoryName(SkillCategory category) { return new[] { "输出", "控制", "位移", "增益", "防御", "治疗" }[(int)category]; }
        public static string SkillEvolution(HeroClass hero, int skill, int rank)
        {
            int stage = Math.Max(1, Math.Min(3, rank)) - 1;
            if(skill==10)return "持续"+(6+stage)+"秒，每0.5秒造成"+((45+stage*10)*1.5f).ToString("0.#")+"%攻击伤害。对地面光圈内的所有敌人造成范围伤害，自动追踪并切换目标；无目标时停留，继续寻找。";
            if(hero==HeroClass.Arcanist&&skill==9)return "持续召唤"+(14+stage*4)+"颗陨石，覆盖前方大矩形与后方小矩形；陨石可砸飞普通与精英怪物，首领免疫浮空。";
            if(hero==HeroClass.Arcanist&&skill==2)return "旋转漩涡持续吸附，撕裂"+(9+stage*2)+"次；升阶扩大范围。";
            if(hero==HeroClass.Arcanist&&skill==7)return "以自身为中心抬升敌群，随后自然坠地；升空、滞空、落地共3次，每次造成"+(150+stage*45)+"%攻击伤害，首领免疫浮空。";
            if(hero==HeroClass.Arcanist&&skill==6)return "重力球持续"+(5+stage)+"秒，持续压倒范围内普通与精英怪物；每0.5秒造成"+(45+stage*10)+"%攻击伤害，首领免疫倒地。";
            if(hero==HeroClass.Vanguard&&skill==4)return new[]{
                "向前释放大型剑气，以每秒7米推进9米，宽6.8米；造成300%攻击伤害并击退怪物。每个目标命中一次，遇障碍停止；首领遵循控制抗性。",
                "向前释放大型剑气，以每秒7米推进10.4米，宽7.8米；造成390%攻击伤害并强化击退。每个目标命中一次，遇障碍停止；首领遵循控制抗性。",
                "向前释放大型剑气，以每秒7米推进12.2米，宽9.2米；造成480%攻击伤害并强力击退。每个目标命中一次，遇障碍停止；首领遵循控制抗性。"}[stage];
            if(hero==HeroClass.Arcanist&&skill==5)return new[]{"持续6秒；霜环每1.5秒造成95%攻击伤害。","持续8秒；霜环每1.2秒造成115%攻击伤害。","持续10秒；霜环每1秒造成135%攻击伤害。"}[stage]+"灼燃改为火路，每0.5秒造成65%攻击伤害，重叠不叠加。";
            if (skill == 3)
            {
                if (hero == HeroClass.Vanguard) return new[] { "攻击 +8%，防御 +2。永久生效。", "攻击 +14%，防御 +4。", "攻击 +22%，防御 +7。" }[stage];
                if (hero == HeroClass.Arcanist) return new[] { "攻击 +6%，最大生命 +4%。", "攻击 +11%，最大生命 +7%。", "攻击 +18%，最大生命 +10%。" }[stage];
                if (hero == HeroClass.Summoner) return new[] { "攻击 +6%，召唤物继承自身攻击。永久生效。", "攻击 +11%，召唤物继承强化后的攻击。", "攻击 +18%，召唤物继承强化后的攻击。" }[stage];
                return new[] { "暴击 +4%，移动速度 +3%。", "暴击 +7%，移动速度 +6%。", "暴击 +12%，移动速度 +10%。" }[stage];
            }
            if (skill == 8)
            {
                if (hero == HeroClass.Vanguard) return new[] { "濒危时减伤30%，持续3秒；内置冷却45秒。", "减伤40%，持续4秒；内置冷却38秒。", "减伤50%/5秒并自动反击；内置冷却30秒。" }[stage];
                if (hero == HeroClass.Arcanist || hero == HeroClass.Summoner) return new[] { "受伤后范围反击90%攻击，回复4能量；冷却45秒。", "反击120%攻击，回复6能量；冷却38秒。", "反击150%攻击，回复8能量并控制周围敌人1.5秒；冷却30秒。" }[stage];
                return new[] { "受伤后无敌0.25秒、移速+15%/3秒；冷却45秒。", "无敌0.4秒、移速+20%/4秒；冷却38秒。", "无敌0.55秒、移速+25%/5秒，回复8能量；冷却30秒。" }[stage];
            }
            if(hero==HeroClass.Summoner&&skill==6)return new[]{"烬羽灵持续12秒，每1.5秒发射穿透灵羽。","烬羽灵持续16秒，攻击+30%。","烬羽灵持续20秒，攻击+60%。"}[stage];
            if (skill == 6 && hero != HeroClass.Vanguard)
                return SkillDescription(hero,skill) + new[] { "", " 伤害+30%，范围+15%。", " 伤害+60%，范围+35%。" }[stage];
            if (skill == 6)
            {
                string limited = " 限疗模式：5秒内自身恢复" + new[] { "60%", "70%", "80%" }[stage] + "最大生命，消耗1次治疗充能。";

                return new[] { "5秒内恢复30%最大生命，不造成伤害。", "5秒恢复42%生命，获得18%减伤。", "5秒恢复55%生命，获得25%减伤并回复8能量。" }[stage] + limited;
            }
            if (stage == 0)
            {
                if(hero==HeroClass.Vanguard&&skill==0)return "旋转斩击周围敌人。低消耗、短冷却。";
                if(hero==HeroClass.Ranger&&skill==9)return "连续向目标周围降下实体箭雨，最后集中落箭冲击。无需专注。";
                if(hero==HeroClass.Summoner&&skill==9)return "召唤一只远古树灵守卫，持续24秒，范围重击造成225%攻击伤害并击倒敌人；最多1只，无需灵力。每2.5秒为6米内可见的主人恢复10%最大生命。";
                return SkillDescription(hero, skill);
            }
            return stage == 1 ? ReinforcedEffects[(int)hero,skill] : AwakenedEffects[(int)hero,skill];
        }
        public static float SkillCooldown(HeroClass hero, int skill)
        {
            ValidateSkillBudget(hero, skill);
            return skill==10?24f:ClassSkillCooldowns[(int)hero, skill];
        }
        public static float SkillEnergyCost(HeroClass hero, int skill)
        {
            ValidateSkillBudget(hero, skill);
            return skill==10?30f:skill==9?0:(float)Math.Floor(ClassSkillEnergyCosts[(int)hero, skill]*2d/3d);
        }
        public static float EffectiveCooldown(HeroClass hero, int skill, int rank)
        {
            ValidateSkillBudget(hero,skill);
            if(skill==SkillStockRules.Skill(hero))return SkillStockRules.Seconds(hero);
            return SkillCooldown(hero, skill) * (1f - (Math.Max(1, Math.Min(3, rank)) - 1) * .07f);
        }
        private static void ValidateSkillBudget(HeroClass hero, int skill)
        {
            if ((int)hero < 0 || (int)hero >= ClassSkillCooldowns.GetLength(0)) throw new ArgumentOutOfRangeException(nameof(hero));
            if (skill < 0 || skill >= SkillCount) throw new ArgumentOutOfRangeException(nameof(skill));
        }
        public static int[] DefaultLoadout(HeroClass hero=HeroClass.Vanguard)
        {
            int[] result = new int[HotbarSize * HotbarPages];
            for (int i = 0; i < result.Length; i++) result[i] = -1;
            int slot = 0;
            for (int skill = 0; skill < SkillCountFor(hero); skill++) if (!IsPassive(skill)) result[slot++] = skill;
            for(int page=0;page<HotbarPages;page++)result[page*HotbarSize+HotbarSize-1]=HotbarPotion;
            return result;
        }
        public static bool IsBindableKey(int key)
        {
            if (key == 97 || key == 100 || key == 102 || key == 103 || key == 104 || key == 105 || key == 106 || key == 107 || key == 109 || key == 111 || key == 112 || key == 115 || key == 116 || key == 119) return false;
            return (key >= 97 && key <= 122) || (key >= 48 && key <= 57) || (key >= 282 && key <= 293);
        }
        public static string KeyName(int key)
        {
            if (key >= 97 && key <= 122) return ((char)(key - 32)).ToString();
            if (key >= 48 && key <= 57) return ((char)key).ToString();
            if (key >= 282 && key <= 293) return "F" + (key - 281);
            return "未绑定";
        }
        public static string SlotName(ItemSlot slot) { return new[] { "武器", "护甲", "饰品" }[(int)slot]; }
        public static string RarityName(Rarity rarity) { return new[] { "普通", "稀有", "史诗", "传说" }[(int)rarity]; }
        public static Color RarityColor(Rarity rarity) {
            return new[] { new Color(.76f,.8f,.84f), new Color(.35f,.65f,1f), new Color(.77f,.43f,1f), new Color(1f,.72f,.26f) }[(int)rarity];
        }
        public static int XpToNext(int level) { return 4 * (240 + (Math.Max(1,Math.Min(100,level)) - 1) * 120); }
    }
}

namespace Emberfall
{
    // Shared by every projectile from one attack, including the delayed second shot.
    public sealed class BasicAttackHitBudget
    {
        private bool claimed;
        private readonly int tier;
        private readonly System.Collections.Generic.Dictionary<object,float> targetDamage;
        public BasicAttackHitBudget(int attackTier=1)
        {
            tier=System.Math.Max(1,System.Math.Min(4,attackTier));
            if(tier>=3)targetDamage=new System.Collections.Generic.Dictionary<object,float>();
        }
        // Normalize against one base attack so critical and damage modifiers scale together.
        public float ClaimDamageScale(object target)
        {
            if(tier<3)return 1f;
            if(target==null)return 0f;
            float used;targetDamage.TryGetValue(target,out used);
            float allowed=System.Math.Min(BasicAttackGrowth.ProjectileMultiplier(tier),System.Math.Max(0f,BasicAttackGrowth.TargetMultiplier(tier)-used));
            targetDamage[target]=used+allowed;
            return allowed/BasicAttackGrowth.ProjectileMultiplier(tier);
        }
        public bool TryClaim(){if(claimed)return false;claimed=true;return true;}
    }
    public static class BasicAttackGrowth
    {
        public const string MaterialName="战技源晶";
        public static int DropChance(int tier){return 10+System.Math.Min(20,System.Math.Max(1,tier)-1);}
        public static int RefinementChance(int tier){return (100-DropChance(tier))/2;}
        public static int FragmentChance(int tier){return 100-DropChance(tier)-RefinementChance(tier);}
        public static string DropSummary(int tier){return "战技源晶 ×1 · "+DropChance(tier)+"% / 装备洗练石 · "+RefinementChance(tier)+"% / 星烬碎片 · "+FragmentChance(tier)+"%";}
        public static int Tier(GameProfile p,HeroClass hero)
        {int i=(int)hero;return p.basicAttackTiers==null||i<0||i>=p.basicAttackTiers.Length?1:System.Math.Max(1,System.Math.Min(4,p.basicAttackTiers[i]));}
        public static int Cost(int tier){return tier>=4?0:1<<(System.Math.Max(1,tier)-1);}
        public static int Shots(int tier){return tier<=1?1:tier==2?2:tier==3?4:8;}
        public static float ProjectileMultiplier(int tier){return tier<=1?1f:tier==2?.6f:tier==3?1.2f:1.4f;}
        public static float TargetMultiplier(int tier){return tier<=1?1f:tier==2?1.2f:tier==3?1.4f:1.6f;}
        public static float Range(int tier){return 2.8f+(System.Math.Max(1,System.Math.Min(4,tier))-1)*.6f;}
        public static float Angle(int tier){return 110f+(System.Math.Max(1,System.Math.Min(4,tier))-1)*20f;}
        public static string Description(HeroClass hero,int tier)
        {return hero==HeroClass.Vanguard?"前方 "+Range(tier).ToString("0.0")+" 米、"+Angle(tier).ToString("0")+"°扇形挥斩":tier==1?"单体单发攻击":tier==2?"向前连续发射两次，间隔0.12秒":"向前方60°扇形发射 "+Shots(tier)+" 枚"+(hero==HeroClass.Ranger?"箭矢":"法球");}
    }
    // Every form targets its gem's own attribute; shared rules drive UI and combat.
    public enum GemFormTrigger { Healthy, LowHealth, LowEnergy, HighEnergy, Moving, Still, Hit, Cast, Hurt, Dodge }
    public static class GemFormRules
    {
        private static readonly GemFormTrigger[] first={GemFormTrigger.Healthy,GemFormTrigger.Still,GemFormTrigger.Healthy,GemFormTrigger.Still,GemFormTrigger.Healthy,GemFormTrigger.LowHealth,GemFormTrigger.LowEnergy,GemFormTrigger.HighEnergy,GemFormTrigger.Moving,GemFormTrigger.Hit,GemFormTrigger.Healthy,GemFormTrigger.LowHealth,GemFormTrigger.Healthy,GemFormTrigger.HighEnergy,GemFormTrigger.HighEnergy,GemFormTrigger.LowHealth};
        private static readonly GemFormTrigger[] second={GemFormTrigger.Cast,GemFormTrigger.Hit,GemFormTrigger.Cast,GemFormTrigger.Hurt,GemFormTrigger.Hurt,GemFormTrigger.Cast,GemFormTrigger.Cast,GemFormTrigger.Cast,GemFormTrigger.Cast,GemFormTrigger.LowEnergy,GemFormTrigger.Cast,GemFormTrigger.Hurt,GemFormTrigger.Cast,GemFormTrigger.Cast,GemFormTrigger.Hurt,GemFormTrigger.Hit};
        private static readonly string[] names={"锐势","施法强袭","凝神鹰眼","连击鹰眼","裂魂锋芒","施法裂魂","静息丰盈","承伤生机","满盈铁壁","受击固甲","濒危坚守","施法守御","匮能回流","施法涌能","充盈疾咏","施法回转","疾行踏风","施法踏风","命中灵汐","匮能灵汐","满盈疾咏","施法疾咏","濒危涌泉","受击涌泉","满盈轻羽","施法轻羽","充盈长生","施法长生","充盈星佑","受击星佑","背水噬生","连击噬生"};
        private static int Index(EquipmentMechanic gem){return Math.Max(0,Math.Min(15,(int)gem-(int)EquipmentMechanic.WeaponPower));}
        public static GemFormTrigger Trigger(EquipmentMechanic gem,int variant){return (variant==1?second:first)[Index(gem)];}
        public static string Name(EquipmentMechanic gem,int variant){return names[Index(gem)*2+(variant==1?1:0)];}
        private static readonly float[] values={.05f,.025f,.15f,.08f,.12f,.025f,.20f,.025f,.08f,.0025f};
        public static float Value(EquipmentMechanic gem,int variant,int rank)
        {return values[BuildCatalog.GemAttribute(gem)]*Math.Max(0,Math.Min(3,rank))*(variant==1?1.5f:1f);}
        public static string Description(EquipmentMechanic gem,int variant,int rank)
        {
            var trigger=Trigger(gem,variant);
            string[] conditions={"生命≥80%时","生命≤50%时","能量低于50%时","能量≥50%时","移动时","停止移动时","造成伤害后4秒内","释放技能后4秒内","受到伤害后4秒内","释放技能后4秒内"};
            return conditions[(int)trigger]+"，"+BuildCatalog.GemAttributeLabel(gem)+" +"+(Value(gem,variant,rank)*100).ToString("0.###")+"%。"+((int)trigger>=6?"重复触发刷新持续时间，不叠层。":"");
        }
        public static void Apply(ref StatBlock stats,int attribute,float value)
        {
            switch(attribute){case 0:stats.Damage*=1+value;break;case 1:stats.CritChance=Math.Min(1,stats.CritChance+value);break;case 2:stats.CritDamageBonus+=value;break;case 3:stats.MaxHealth*=1+value;break;case 4:stats.Armor*=1+value;break;case 5:stats.DamageReduction=Math.Min(.5f,stats.DamageReduction+value);break;case 6:stats.EnergyRecovery+=value;break;case 7:stats.CooldownReduction=Math.Min(.4f,stats.CooldownReduction+value);break;case 8:stats.MoveSpeed*=1+value;break;case 9:stats.LifeSteal+=value;break;}
        }
    }

}

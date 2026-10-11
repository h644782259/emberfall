using System.Globalization;
namespace Emberfall
{
    public static class ChapterEntryPresentation
    {
        public static string DifficultyName(ChapterDifficulty difficulty)
        {return difficulty==ChapterDifficulty.Hard?"困难":difficulty==ChapterDifficulty.Heroic?"英雄":"普通";}
        public static string Story(ChapterNode node)
        {
            var definition=ChapterDefinition.Get(node);
            return "观星员的线索 · "+definition.StoryIntro+"\n目标 · "+definition.Mechanic+"\n完成后 · "+definition.Outcome+"\n下一线索 · "+definition.NextClue;
        }
        public static string TierEffect(ChapterNode node)
        {return "通关当前阶后解锁下一阶，阶数持续提升。";}
        public static string UnlockHint(ChapterNode node)
        {return node==ChapterNode.Redrock?"通关林庭复明后解锁":node==ChapterNode.StarPlatform?"通关赤岩断供后解锁":"第一段星路 · 随时出发";}
        public static string DifficultyHint(ChapterDifficulty difficulty,bool unlocked)
        {return unlocked?(difficulty==ChapterDifficulty.Normal?"标准敌人强度":"生命 ×"+ChapterDefinition.HealthMultiplier(difficulty).ToString("0.##",CultureInfo.InvariantCulture)+" · 伤害 ×"+ChapterDefinition.DamageMultiplier(difficulty).ToString("0.##",CultureInfo.InvariantCulture)):
            "先解锁本节点";}
        public static string RewardBreakdown(GameProfile profile,ChapterNode node,ChapterDifficulty difficulty,int tier)
        {
            int repeat=ChapterProgression.MaterialReward(node,tier),total=ChapterProgression.CompletionMaterials(profile,node,tier);
            int bonus=RewardMaterials(profile,node,difficulty,tier)-total;
            return "通关 "+repeat+(total>repeat?"  +  首次通关 1":"")+(bonus>0?"  +  难度首通 "+bonus:"")+"\n完成挑战后获得奖励";
        }
        public static string MasteryProgress(GameProfile profile,ChapterNode node,ChapterDifficulty difficulty)
        {
            if(profile==null||difficulty==ChapterDifficulty.Normal||(profile.chapterCompletedMask&(1<<(int)node))==0)return "";
            int mask=profile.chapterMasteryMask,required=node==ChapterNode.ForestCourt?1:node==ChapterNode.Redrock?2:12;
            string goal=node==ChapterNode.ForestCourt?"首房至少2敌仍存活时完成双封印":node==ChapterNode.Redrock?"首房其余5敌仍存活时击败断供目标，再成功撤离":
                "真实打断首领 "+((mask&4)!=0?"✓":"○")+" / 亲自破锚制造暴露 "+((mask&8)!=0?"✓":"○")+"（可分局）";
            bool earned=(mask&required)==required;int tier=int.MaxValue;
            for(int i=0;i<4;i++)if((required&(1<<i))!=0)tier=System.Math.Min(tier,profile.chapterMasteryTiers!=null&&profile.chapterMasteryTiers.Length>i?profile.chapterMasteryTiers[i]:0);
            string badge=node==ChapterNode.ForestCourt?"双印行者":node==ChapterNode.Redrock?"断供猎手":"星台破局者";
            return "\n可选精通 · "+goal+"。本次节点成功保存才记录；无战斗加成，不影响节点通关。"+
                (earned?"\n徽记 / 称号「"+badge+"」 · 记录第 "+tier+" 阶":"\n徽记 / 称号「"+badge+"」尚未取得");
        }
        public static int RewardMaterials(GameProfile profile,ChapterNode node,ChapterDifficulty difficulty,int tier)
        {
            int bit=ChapterProgression.DifficultyRewardBit(node,difficulty);
            return ChapterProgression.CompletionMaterials(profile,node,tier)+(bit!=0&&(profile.chapterDifficultyRewardMask&bit)==0?ChapterProgression.DifficultyFirstRewardMaterials:0);
        }
        public static string Preview(GameProfile profile,ChapterNode node,ChapterDifficulty difficulty,int tier,bool limited)
        {
            string health=ChapterDefinition.HealthMultiplier(difficulty).ToString("0.##",CultureInfo.InvariantCulture);
            string damage=ChapterDefinition.DamageMultiplier(difficulty).ToString("0.##",CultureInfo.InvariantCulture);
            int repeat=ChapterProgression.MaterialReward(node,tier),total=ChapterProgression.CompletionMaterials(profile,node,tier);
            int rewardBit=ChapterProgression.DifficultyRewardBit(node,difficulty);
            int firstDifficulty=rewardBit!=0&&(profile.chapterDifficultyRewardMask&rewardBit)==0?ChapterProgression.DifficultyFirstRewardMaterials:0;
            return "敌人生命 ×"+health+" / 伤害 ×"+damage+"\n"+
                ChapterDefinition.DifficultyMechanic(node,difficulty)+"\n"+
                "通关当前阶后解锁下一阶。\n"+

                "完成奖励 "+(total+firstDifficulty)+" 碎片（重复 "+repeat+(total>repeat?" + 节点首次1":"")+(firstDifficulty>0?" + 首次4":"")+"）；每节点一次额外奖励；不发旧副本宝箱。\n"+
                "旧档按合法逐档完成记录一次补领；缺失或非法记录不推断，实际通关后领取。\n"+
                (!profile.firstClearRewardClaimed?(profile.pendingFirstClearReward?"共享一次首通宝石已待领取；本次不重复。\n":(node==ChapterNode.StarPlatform?"星台通关完成整章，可领取共享一次首通宝石。\n":"首通宝石需完成整章：通关星台；本节点不授予资格。\n")):"")+TierEffect(node)+MasteryProgress(profile,node,difficulty);
        }
        private static string MechanismReport(ChapterResultSnapshot result)
        {
            if(result.EmberCreated+result.FrostCreated==0)return "";
            return "\n机制实例（实际扣血才计生效）\n烬地 · 生成 "+result.EmberCreated+" / 生效 "+result.EmberEffective+
                "\n霜环回响 · 生成 "+result.FrostCreated+" / 生效 "+result.FrostEffective+
                (result.EmberCreated>result.EmberEffective?"\n下次将烬地落点放在敌人推进路线上。":result.FrostCreated>result.FrostEffective?"\n下次留意霜环回响延迟与敌人位置。":"");
        }
        public static string Result(ChapterResultSnapshot result)
        {
            if(result==null)return "章节记录暂不可用";
            string text=ChapterDefinition.Get(result.Node).Name+" · 第 "+result.Tier+" 阶"+"\n"+
                "携带药剂 "+result.EntryPotions+"\n\n";
            if(result.Failed)return text+"止步房间 "+(result.Room+1)+" / "+ChapterDefinition.RoomCount(result.Node)+"\n"+
                (result.Node==ChapterNode.ForestCourt?"封印 "+result.Seals+"/2 · 一 "+result.FirstSealSeconds.ToString("0.0")+"s · 二 "+result.SecondSealSeconds.ToString("0.0")+"s\n":"")+
                "最后受击："+(string.IsNullOrEmpty(result.LastHit)?"未记录":result.LastHit)+" · "+result.LastHitAmount.ToString("0.#")+"\n"+
                (string.IsNullOrEmpty(result.Failure)?"本次挑战未完成。":result.Failure)+"\n本次击杀经验 +"+result.KillExperience+"；未发通关经验。\n节点与阶数未解锁；回营重试。"+MechanismReport(result);
            if(!result.Saved)return text+"节点完成 · 结算尚未保存\n奖励与解锁尚未提交，重试保存后再继续。"+MechanismReport(result);
            if(result.RewardDetailsUnavailable)return text+"奖励已保存 · 旧回执缺少明细，无法恢复准确数额与本次解锁结果；不会重复发放。"+MechanismReport(result);
            text+="奖励已保存 · +"+result.Materials+" 碎片\n击杀经验 +"+result.KillExperience+" · 通关经验 +"+result.CompletionExperience;
            if(result.FirstCompletion)text+="\n"+ChapterDefinition.Get(result.Node).Outcome;
            if(result.FirstCoreAvailable)text+="\n首通宝石已可领取（共享一次）";
            if(result.UnlockedNode>=0)text+="\n新节点："+ChapterDefinition.Get((ChapterNode)result.UnlockedNode).Name;

            if(result.Difficulty==ChapterDifficulty.Heroic)text+="\n可挑战第 "+(result.Tier==int.MaxValue?int.MaxValue:result.Tier+1)+" 阶";
            if(result.FirstCompletion)text+="\n下一线索 · "+ChapterDefinition.Get(result.Node).NextClue;
            return text+MechanismReport(result);
        }
        public static string DesktopResult(ChapterResultSnapshot result)
        {
            string text=Result(result);
            if(result==null||result.Failed||!result.Saved||result.RewardDetailsUnavailable)return text;
            string amounts="奖励已保存 · +"+result.Materials+" 碎片\n击杀经验 +"+result.KillExperience+" · 通关经验 +"+result.CompletionExperience;
            return text.Replace(amounts,"奖励已保存");
        }
        public static string Result(ChapterNode node,bool failed,bool pending)
        {
            if(failed)return "本次没有完成节点，故事与阶数进度未推进。\n可以回营整备，再次挑战。";
            if(pending)return "节点战斗已完成，结算尚未保存。\n故事、解锁与奖励尚未提交；重试保存后再继续。";
            var definition=ChapterDefinition.Get(node);
            return definition.Outcome+"\n\n下一线索 · "+definition.NextClue+"\n\n"+TierEffect(node)+"\n已解锁节点可回营后独立重玩。";
        }
    }
}

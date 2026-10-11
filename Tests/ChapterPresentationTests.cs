using System;
using Emberfall;
public static class ChapterPresentationTests
{
    static int checks;
    static void Check(bool value,string reason){checks++;if(!value)throw new Exception(reason);}
    public static string Run()
    {
        checks=0;var p=new GameProfile();
        foreach(ChapterNode node in Enum.GetValues(typeof(ChapterNode)))
        {
            var d=ChapterDefinition.Get(node);string story=ChapterEntryPresentation.Story(node);
            Check(story.Contains(d.StoryIntro)&&story.Contains(d.Outcome)&&story.Contains(d.NextClue),"shared cause, outcome and next clue");
            Check(ChapterEntryPresentation.TierEffect(node).Contains(node==ChapterNode.StarPlatform?"解锁下一阶":"不推进共享最高阶"),"only star advertises shared progression");
            foreach(int tier in new[]{1,4,5,9,10,19,20,39,40,100})
            foreach(ChapterDifficulty diff in Enum.GetValues(typeof(ChapterDifficulty)))
            foreach(bool limited in new[]{false,true})
            {
                p.chapterFirstRewardMask=0;p.chapterDifficultyRewardMask=0;
                string first=ChapterEntryPresentation.Preview(p,node,diff,tier,limited);
                int repeat=ChapterProgression.MaterialReward(node,tier);
                Check(first.Contains("完成奖励 "+(repeat+1+(diff==ChapterDifficulty.Normal?0:4))+" 碎片")&&first.Contains("首次1"),"first reward follows shared tier bands plus one fixed shard");
                Check(first.Contains(ChapterDefinition.DifficultyMechanic(node,diff)),"exact production difficulty mechanic described");
                Check(first.Contains("困难/英雄每节点各一次，全章额外最多24")&&first.Contains("不发旧副本宝箱")&&first.Contains(node==ChapterNode.StarPlatform?"星台通关完成整章，可领取共享一次首通核心":"本节点不授予资格"),"actual shared first-core eligibility and no legacy chest");
                Check(first.Contains(limited?"初始3次":"携带药剂"),"healing rules are independent");
                p.chapterFirstRewardMask=1<<(int)node;p.chapterDifficultyRewardMask=63;
                string replay=ChapterEntryPresentation.Preview(p,node,diff,tier,limited);
                Check(replay.Contains("完成奖励 "+repeat+" 碎片")&&!replay.Contains("首次1"),"replay does not promise first shard again");
            }
            Check(!ChapterEntryPresentation.Result(node,false,true).Contains(d.Outcome)&&!ChapterEntryPresentation.Result(node,true,false).Contains(d.NextClue),"failed/pending outcomes never claim story success");
            Check(ChapterEntryPresentation.Result(node,false,false).Contains(d.Outcome)&&ChapterEntryPresentation.Result(node,false,false).Contains(d.NextClue),"committed completion displays shared consequences");
        }
        Check(ChapterEntryPresentation.Preview(p,ChapterNode.ForestCourt,ChapterDifficulty.Hard,1,false).Contains("生命 ×1.2 / 伤害 ×1.15"),"hard multipliers match actual core values");
        Check(ChapterEntryPresentation.Preview(p,ChapterNode.ForestCourt,ChapterDifficulty.Heroic,1,false).Contains("生命 ×1.35 / 伤害 ×1.25"),"heroic multipliers match actual core values");
        Check(ChapterDefinition.DifficultyMechanic(ChapterNode.StarPlatform,ChapterDifficulty.Hard).Contains("追加无新锚，只能打断"),"boss follow-up preview matches actual counter opportunities");
        p.pendingFirstClearReward=true;
        Check(ChapterEntryPresentation.Preview(p,ChapterNode.ForestCourt,ChapterDifficulty.Normal,1,false).Contains("已待领取；本次不重复"),"pending first core never advertised as second reward");
        p.firstClearRewardClaimed=true;
        Check(!ChapterEntryPresentation.Preview(p,ChapterNode.StarPlatform,ChapterDifficulty.Normal,1,false).Contains("首通核心"),"claimed shared core not advertised again");
        return "PASS: "+checks+" chapter presentation, shared definitions and reward-boundary checks";
    }
}

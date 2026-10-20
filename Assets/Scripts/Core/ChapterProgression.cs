using System;
namespace Emberfall
{
    public enum ChapterNode { ForestCourt=0, Redrock=1, StarPlatform=2 }
    public sealed class ChapterDefinition
    {
        public readonly string Name,Story,Mechanic,Outcome,NextClue;
        public readonly int BaseMaterials;
        public string StoryIntro {get{return Story;}}
        private ChapterDefinition(string name,int materials,string story,string mechanic,string outcome,string next){Name=name;BaseMaterials=materials;Story=story;Mechanic=mechanic;Outcome=outcome;NextClue=next;}
        static readonly ChapterDefinition[] Nodes={
            new ChapterDefinition("林庭复明",1,"净化林庭残火，沿旧径撤离。","先净化，再寻找出口","林庭封印重归稳定，撤离的通路已经打开。","循残余火迹前往赤岩，寻找封锁线的供能者。"),
            new ChapterDefinition("赤岩断供",1,"截断赤岩守军，穿越封锁线。","先猎杀，再突破封锁","赤岩供能被切断，星台的外缘防线出现缺口。","穿过封锁线，在入口确认整备后直面守望者。"),
            new ChapterDefinition("星台封印",2,"在入口确认整备，直入星台迎战守望者。","直入首领场，击败守望者即可通关","守望者退去，第一章的星路重新连通。","可以重访三处节点，挑战已解锁的更高阶数。")};
        public static ChapterDefinition Get(ChapterNode node){if(!ChapterProgression.Valid(node))throw new ArgumentOutOfRangeException(nameof(node));return Nodes[(int)node];}
        public static int RoomCount(ChapterNode node){Get(node);return node==ChapterNode.StarPlatform?1:2;}
        public static RoomObjective RoomKind(ChapterNode node,int index)
        {Get(node);if(index<0||index>=RoomCount(node))throw new ArgumentOutOfRangeException(nameof(index));return node==ChapterNode.ForestCourt?(index==0?RoomObjective.Purify:RoomObjective.Escape):node==ChapterNode.Redrock?(index==0?RoomObjective.Hunt:RoomObjective.Escape):RoomObjective.Boss;}
        public static string CombatMechanic(ChapterNode node)
        {
            Get(node);
            if(node==ChapterNode.ForestCourt)return "金环护援使6米内可见同伴减伤30%；引开、遮挡或击杀可断供；单个2.1米荆棘脉冲圈，可绕行";
            if(node==ChapterNode.Redrock)return "守岗敌人形成交火，需要选择突破路线；单条热涌短线受墙阻挡，可绕端点";
            return "首段有效打断进入2秒恢复；破坏全部锚进入6秒暴露、受伤+35%。未反制则追加扫射；每次挑战的扫射方向与安全锚位置会变化。";
        }
        public const float BaseHealthMultiplier=1.35f,BaseDamageMultiplier=1.25f;

    }
    public static class ChapterProgression
    {
        public static bool Valid(ChapterNode node){return (int)node>=0&&(int)node<3;}
        public static int UnlockLevel(ChapterNode node){return Valid(node)?30+(int)node*10:101;}
        public static int LevelTier(int level){return Math.Max(1,Math.Min(10,level/10));}
        public static int AvailableTier(GameProfile profile,ChapterNode node)
        {int best=CompletedTier(profile,node);return best==int.MaxValue?int.MaxValue:best+1;}
        public static int CompletedTier(GameProfile profile,ChapterNode node)
        {return profile==null||!Valid(node)||profile.chapterBestTiers==null||profile.chapterBestTiers.Length<3?0:Math.Max(0,profile.chapterBestTiers[(int)node]);}
        public static bool CanEnter(GameProfile profile,ChapterNode node,int tier)
        {return CanEnter(profile,node)&&tier>=1&&tier<=AvailableTier(profile,node);}
        public static int EnemyLevel(int characterLevel)
        {int level=Math.Max(1,Math.Min(100,characterLevel));return ((level+9)/10)*10;}
        public static float TierHealthMultiplier(int tier)
        {double step=(Math.Max(1,tier)-1d)*10d;return (float)(1d+.18d*step+.003d*step*step);}
        public static float TierDamageMultiplier(int tier)
        {double step=(Math.Max(1,tier)-1d)*10d;return (float)(1d+.10d*step+.001d*step*step);}
        public static bool IsUnlocked(GameProfile profile,ChapterNode node){return profile!=null&&Valid(node)&&profile.level>=UnlockLevel(node);}
        public static bool CanEnter(GameProfile profile,ChapterNode node){return IsUnlocked(profile,node);}
        public static int MaterialReward(ChapterNode node,int tier){return TierRewardBand.Materials(ChapterDefinition.Get(node).BaseMaterials,1)+TierRewardBand.Clamp(tier)-1;}
        public static int CompletionMaterials(GameProfile profile,ChapterNode node,int tier)
        {return MaterialReward(node,tier)+((profile.chapterFirstRewardMask&(1<<(int)node))==0?1:0);}
        internal static void Normalize(GameProfile profile)
        {
            profile.chapterMasteryMask&=15;
            var oldMastery=profile.chapterMasteryTiers;profile.chapterMasteryTiers=new int[4];
            for(int i=0;i<4;i++)if((profile.chapterMasteryMask&(1<<i))!=0)profile.chapterMasteryTiers[i]=Math.Max(1,oldMastery!=null&&i<oldMastery.Length?oldMastery[i]:1);
            profile.chapterRevision=Math.Max(0,Math.Min(1,profile.chapterRevision));
            var oldLevels=profile.chapterBestLevels;profile.chapterBestLevels=new int[3];
            for(int i=0;i<3;i++)profile.chapterBestLevels[i]=Math.Max(0,Math.Min(100,oldLevels!=null&&i<oldLevels.Length?oldLevels[i]:0));
            profile.chapterCompletedMask&=7;profile.chapterFirstRewardMask&=profile.chapterCompletedMask;
            if(profile.chapterTierRevision>2)throw new ArgumentException("章节阶数版本不受支持，原文件保留。");
            var oldBest=profile.chapterBestTiers;profile.chapterBestTiers=new int[3];
            for(int node=0;node<3;node++)
            {
                int best=oldBest!=null&&node<oldBest.Length?Math.Max(0,oldBest[node]):0;
                // Read-only migration of the retired nine-slot save layout.
                if(profile.chapterTierRevision<2&&profile.chapterDifficultyBestTiers!=null)
                    for(int slot=node*3;slot<node*3+3&&slot<profile.chapterDifficultyBestTiers.Length;slot++)best=Math.Max(best,profile.chapterDifficultyBestTiers[slot]);
                if((profile.chapterCompletedMask&(1<<node))!=0)best=Math.Max(1,best);
                profile.chapterBestTiers[node]=best;
            }
            if(profile.chapterTierRevision<2){profile.chapterTierRevision=2;profile.chapterTierMigrationPending=true;}
            profile.chapterDifficultyBestTiers=null;
            profile.chapterRewardSequence=Math.Max(0,profile.chapterRewardSequence);
            Guid receipt;profile.lastChapterRewardId=Guid.TryParseExact(profile.lastChapterRewardId,"N",out receipt)?receipt.ToString("N"):null;
            profile.chapterHighestAdventureTier=Math.Max(0,Math.Min(100,profile.chapterHighestAdventureTier));
            profile.chapterPriorAdventureTier=Math.Max(0,Math.Min(100,profile.chapterPriorAdventureTier));
        }
    }
    public sealed class ChapterRunReceipt
    {
        public ChapterNode Node {get;private set;}
        public int Tier {get;private set;} public int Materials {get;private set;}
        public string Id {get;private set;} public long Sequence {get;private set;}
        internal int MasteryEvidence;
        internal bool MasteryEligible;
        internal readonly string SavePath;
        internal ChapterRunReceipt(ChapterNode node,int tier,int materials,long sequence,string path)
        {Node=node;Tier=tier;Materials=materials;Sequence=sequence;SavePath=path;Id=Guid.NewGuid().ToString("N");}
    }
}

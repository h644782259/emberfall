using System;
namespace Emberfall
{
    public sealed partial class RunChoices
    {
        public static bool ChapterTacticsAvailable(GameProfile profile, ChapterNode node)
        {return profile!=null&&(profile.chapterCompletedMask&4)!=0&&(profile.chapterCompletedMask&(1<<(int)node))!=0;}
        public static RunBlessing ChapterTactic(GameProfile profile,bool mobile,int slot)
        {
            if(slot==0)return RunBlessing.DodgeShock;
            if(slot==2)return RunBlessing.IronSkin;
            if(slot!=1)throw new ArgumentOutOfRangeException(nameof(slot));
            int[] ranks=UsableRanks(profile,mobile);
            for(int i=0;i<ranks.Length;i++)if(ranks[i]>0&&EnemyControlPolicy.IsInterruptSkill(profile.heroClass,i))return RunBlessing.InterruptFlow;
            return RunBlessing.SwiftHands;
        }
        internal bool RestoreChapterTactic(RunBlessing tactic)
        {
            if(active.Count!=0||AwaitingChoice||CompletedWave!=0||
                (tactic!=RunBlessing.DodgeShock&&tactic!=RunBlessing.IronSkin&&tactic!=RunBlessing.InterruptFlow&&tactic!=RunBlessing.SwiftHands))return false;
            active.Add(tactic);CompletedWave=1;return true;
        }
        public bool ChooseChapterTactic(GameProfile profile,ChapterNode node,bool mobile,int slot)
        {
            if(!ChapterTacticsAvailable(profile,node)||slot<0||slot>2||active.Count!=0||AwaitingChoice||CompletedWave!=0)return false;
            active.Add(ChapterTactic(profile,mobile,slot));CompletedWave=1;return true;
        }
    }
}

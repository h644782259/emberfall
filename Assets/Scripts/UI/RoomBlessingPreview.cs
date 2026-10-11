namespace Emberfall
{
    // Uses the same immutable plan constructor as RoomChainState.Next; never consumes RNG or advances the run.
    public static class RoomBlessingPreview
    {
        public static bool TryNext(RoomChainState run,int choiceStage,out RoomChainPlan next)
        {
            next=null;
            if(run==null||run.Finished||run.Room==null)return false;
            RoomChainPlan current=run.Room;
            bool opening=current.Index==0&&choiceStage==1&&run.DoorUnlocked;
            bool rest=current.Index==3&&choiceStage==2&&current.Interlude&&!run.DoorUnlocked;
            if(!opening&&!rest)return false;
            next=new RoomChainPlan(current.Index+1,current.Seed);
            return true;
        }
        public static string Subtitle(RoomChainState run,int choiceStage,string fallback)
        {
            RoomChainPlan next;if(!TryNext(run,choiceStage,out next))return fallback;
            string task;
            switch(next.Objective)
            {
                case RoomObjective.Purify:task="净化两印 · 留意金环护援";break;
                case RoomObjective.Hunt:task="击败金环魔灵开门";break;
                case RoomObjective.Escape:task="北门站稳4秒 · 守卫争夺";break;
                case RoomObjective.Boss:task="首领与2名护卫全灭";break;
                default:return fallback;
            }
            return "下一房 · "+RoomTactics.Name(next.Objective)+" · "+task;
        }
    }
}

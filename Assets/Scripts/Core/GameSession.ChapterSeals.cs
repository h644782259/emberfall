namespace Emberfall
{
    public sealed partial class GameSession
    {
        public ChapterSealPresentation ChapterSealView(int index)
        {
            float fraction;bool contested,complete;
            if(!TryGetChapterSeal(index,out fraction,out contested,out complete)||chapterPlan==null)return null;
            bool occupied=Player!=null&&RoomTacticalRegion.ContainsPlayer(CombatFx.Flat(Player.transform.position-chapterPlan.Objectives[index]).sqrMagnitude);
            return new ChapterSealPresentation(index,ChapterRun.SealProgress(index),occupied,contested,complete,InputBlocked);
        }
    }
}

namespace Emberfall
{
    public sealed partial class PlayerController
    {
        private int channelCast;
        internal bool IsSkillChannelCurrent(int castId) => channelCast!=0&&channelCast==castId&&!IsDead;
        internal void EndSkillChannel(int castId)
        {if(channelCast==castId)StopSkillChannel();}
        private void StopSkillChannel()
        {if(channelCast==0)return;channelCast=0;if(model!=null)model.CancelAction();}
        private void InterruptSkillForBlink()
        {
            GameAudio.InterruptSkillVoice(this);
            if(charge!=null)charge.Cancel();
            CancelCombatPose();
        }
        private void BeginSkillChannel(int skill,int castId)
        {StopSkillChannel();if(SkillCastingPolicy.Channeled(HeroClass,skill))channelCast=castId;}
    }
}

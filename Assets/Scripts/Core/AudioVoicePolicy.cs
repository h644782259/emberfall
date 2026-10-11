namespace Emberfall
{
    public enum SoundCue { Attack, Cast, Hit, Dodge, Loot, LevelUp, Victory, Death, UI, CriticalHit, Judgment }
    public static class AudioVoicePolicy
    {
        public const int Voices = 8, OrdinaryVoices = 6;
        public static bool IsCritical(SoundCue cue)
        { return cue == SoundCue.Cast || cue == SoundCue.Dodge || cue == SoundCue.LevelUp || cue == SoundCue.Victory || cue == SoundCue.Death || cue == SoundCue.Judgment; }
        public static bool IsImpact(SoundCue cue) { return cue == SoundCue.Hit || cue == SoundCue.CriticalHit; }
        public static int Select(SoundCue cue, int busyMask, int cursor)
        {
            bool critical = IsCritical(cue);
            int first = critical ? OrdinaryVoices : 0, count = critical ? Voices - OrdinaryVoices : OrdinaryVoices;
            for(int i=0;i<count;i++) { int index=first+(cursor+i)%count; if((busyMask&(1<<index))==0)return index; }
            return critical ? first+cursor%count : -1;
        }
    }
}

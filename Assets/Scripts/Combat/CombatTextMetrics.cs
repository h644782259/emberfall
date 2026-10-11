namespace Emberfall
{
    // One value per live caption, never a global string cache. Pixel scaling remains live.
    public readonly struct CombatTextMetrics
    {
        private readonly float aspect;
        private readonly int revision;
        private readonly bool valid;
        public CombatTextMetrics(float aspect,int revision)
        {this.aspect=aspect;this.revision=revision;valid=true;}
        public bool TryGet(int fontRevision,out float value)
        {value=aspect;return valid&&revision==fontRevision;}
    }
}

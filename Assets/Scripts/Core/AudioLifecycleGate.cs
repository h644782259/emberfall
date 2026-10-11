namespace Emberfall
{
    public enum AudioLifecycleTransition { None, Suspend, Resume }
    public sealed class AudioLifecycleGate
    {
        public bool BackgroundPaused {get;private set;}
        public AudioLifecycleTransition Observe(bool paused)
        {
            if(paused==BackgroundPaused)return AudioLifecycleTransition.None;
            BackgroundPaused=paused;
            return paused?AudioLifecycleTransition.Suspend:AudioLifecycleTransition.Resume;
        }
        public void Reset(){BackgroundPaused=false;}
    }
}

namespace Emberfall
{
    // Operating-system suspension never owns the manual menu bit.
    public sealed class ApplicationPauseState
    {
        public bool Focused { get; private set; } = true;
        public bool Suspended { get; private set; }
        public bool BackgroundPaused { get { return !Focused || Suspended; } }
        public void SetFocus(bool focused) { Focused = focused; }
        public void SetSuspended(bool suspended) { Suspended = suspended; }
        // Foreground menus and gameplay share 60 FPS; only background mobile rendering is throttled.
        public int TargetFrameRate(bool mobile, bool advancing)
        { return mobile && BackgroundPaused ? 15 : 60; }
        public bool CanAdvance(bool started, bool manualPause, bool modal, bool dead)
        { return started && !manualPause && !modal && !dead && !BackgroundPaused; }
    }
}

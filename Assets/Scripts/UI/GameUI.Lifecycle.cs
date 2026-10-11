using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private int backConsumedFrame=-1;
        // Read before handling Back; consumers later in the frame cannot reuse it.
        public bool GameplayBackAllowed { get { return session!=null&&!session.InputBlocked&&panel==Panel.None&&rebindingSlot<0&&!exitRequest.Open&&backConsumedFrame!=Time.frameCount; } }
        private readonly TouchViewportState touchViewport=new TouchViewportState();
        private readonly TouchReleaseLatch lifecycleRelease=new TouchReleaseLatch();
        public bool LifecycleTouchBlocked
        {
            get
            {
                if(session!=null&&session.BackgroundPaused)return true;
                bool held=Input.touchCount>0||Input.GetMouseButton(0)||Input.GetMouseButtonUp(0);
                return lifecycleRelease.IsBlocked(Time.unscaledTime,held,held);
            }
        }
        public void RefreshTouchViewport(){ObserveTouchViewport(MobileControls.SafeArea);}
        private void ObserveTouchViewport(Rect safe)
        {
            if(!MobileControls.Active)return;
            if(!touchViewport.Observe(Screen.width,Screen.height,safe.x,safe.y,safe.width,safe.height,MobileControls.Layout.Scale,Screen.dpi,(int)Screen.orientation))return;
            MobileControls.ResetInput();
            CancelBackgroundInput();
        }
        private static bool AndroidBackExitEnabled
        {
            get
            {
#if UNITY_ANDROID
                return true;
#else
                return false;
#endif
            }
        }
    }
}

using System;
namespace Emberfall
{
    public enum WaterEnvironment { Brook, Courtyard, Tactical }
    public struct WaterPresentation
    {
        public readonly float Width, DeepWidth, CurrentWidth, CurrentOffset, CurrentBrightness;
        public const int SurfaceLayers=3, ContactStrips=4;
        public WaterPresentation(WaterEnvironment environment,float width)
        {
            Width=float.IsNaN(width)||float.IsInfinity(width)?2.4f:Math.Max(.5f,Math.Min(8,width));
            DeepWidth=Width*(environment==WaterEnvironment.Brook?.73f:environment==WaterEnvironment.Courtyard?.79f:.76f);
            CurrentWidth=Width*(environment==WaterEnvironment.Brook?.26f:environment==WaterEnvironment.Courtyard?.33f:.29f);
            CurrentOffset=Width*(environment==WaterEnvironment.Brook?.11f:environment==WaterEnvironment.Courtyard?-.08f:.06f);
            CurrentBrightness=environment==WaterEnvironment.Brook?1:environment==WaterEnvironment.Courtyard?.88f:.93f;
        }
    }
}

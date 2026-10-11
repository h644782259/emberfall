using System;
namespace Emberfall
{
    public struct EnvironmentLightProfile
    {
        public readonly float KeyIntensity, FillIntensity, KeyElevation, AmbientScale, FogDensity;
        public readonly int Hub;
        public const int TownAccentLights=2;
        public const float AccentRange=5.5f, PortalRange=7f;
        public EnvironmentLightProfile(bool dungeon,int hub)
        {
            Hub=Math.Max(0,Math.Min(2,hub));
            KeyIntensity=dungeon?1.1f:Hub==1?1.28f:Hub==2?1.12f:1.35f;
            FillIntensity=dungeon?.28f:Hub==1?.30f:Hub==2?.37f:.32f;
            KeyElevation=dungeon?48:Hub==1?48:Hub==2?55:42;
            AmbientScale=dungeon?1:Hub==1?.95f:Hub==2?.92f:1;
            FogDensity=dungeon?.016f:Hub==1?.008f:Hub==2?.010f:.009f;
        }
    }
}

using UnityEngine;
namespace Emberfall
{
    public static class ArenaSizeRules
    {
        public const float BossArenaMultiplier=1.25f;
        // Cathedral final waves, BossGauntlet (mode 2 / layout 4), the chain
        // throne and StarPlatform boss room. HoldPoint/Survival have no Boss.
        public static bool HasBossEncounter(int layout){return layout==0||layout==1||layout==4||layout==14||layout==105;}
        public static float LayoutScale(int layout){if(BossScaleRules.ReviewLegacyDimensions)return 1;float room=layout>=100?(layout%2==0?1f:1.18f):layout>=20?1f+((layout-20)/2%4)*.12f:layout>=10?1f+(layout-10)%3*.12f:layout==1?1.12f:1f;return room*(HasBossEncounter(layout)?BossArenaMultiplier:1);}
        public static float Radius(ZoneKind zone,int layout,int hub){return zone==ZoneKind.Dungeon?(layout<2?28:18)*LayoutScale(layout):hub==0?32:22;}
        public static Vector3 Point(int layout,Vector3 point){float scale=LayoutScale(layout);return new Vector3(point.x*scale,point.y,point.z*scale);}
        public static Vector3 LocalPoint(int layout,Vector3 point){float scale=LayoutScale(layout);return new Vector3(point.x/scale,point.y,point.z/scale);}
        public static float CameraDistance(float distance,bool dungeon,int layout){return distance*(dungeon&&LayoutScale(layout)>1?1.15f:1);}
    }
}

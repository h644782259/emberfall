using UnityEngine;
namespace Emberfall
{
    // Visual-only profiles; neither equipment statistics nor collision dimensions use these.
    internal static class RearSilhouette
    {
        internal static Vector3 Shape(HeroClass hero, Vector3 point, float across, float length)
        {
            if(hero==HeroClass.Vanguard) { point.y*=.62f; point.x*=1.12f; point.z-=length*.04f; }
            else if(hero==HeroClass.Arcanist) { point.x*=.72f; point.x+=(across<0?-1:1)*length*.07f; point.y*=1.08f; }
            else if(hero==HeroClass.Ranger) { point.x=point.x*.55f-.24f; point.y*=.74f+.17f*(1-across); point.z+=length*.06f; }
            else { point.x*=.76f; point.x+=(across<0?-1:1)*length*.21f; point.y*=.70f; point.z-=length*.08f; }
            return point;
        }
        internal static Vector3 WingOffset(HeroClass hero)
        {
            return hero==HeroClass.Ranger ? new Vector3(-.08f,.12f,-.16f) :
                hero==HeroClass.Arcanist ? new Vector3(0,.18f,-.10f) :
                hero==HeroClass.Summoner ? new Vector3(0,.09f,-.18f) : new Vector3(0,.03f,-.10f);
        }
        internal static float WingYaw(HeroClass hero) { return hero==HeroClass.Ranger?24:hero==HeroClass.Summoner?30:hero==HeroClass.Arcanist?16:12; }
        internal static float WingSpread(HeroClass hero) { return hero==HeroClass.Ranger?44:hero==HeroClass.Summoner?38:hero==HeroClass.Arcanist?24:32; }
    }
}

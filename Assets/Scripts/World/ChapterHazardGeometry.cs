using UnityEngine;
namespace Emberfall
{
    public static class ChapterHazardGeometry
    {
        public const float HeatHalfWidth=.55f;
        public static Vector3 ClipLine(Vector3 from,Vector3 to)
        {
            if(WorldTraversal.HasGroundPath(from,to,.12f))return to;
            float low=0,high=1;for(int i=0;i<12;i++){float mid=(low+high)*.5f;if(WorldTraversal.HasGroundPath(from,Vector3.Lerp(from,to,mid),.12f))low=mid;else high=mid;}
            return Vector3.Lerp(from,to,low);
        }
    }
}

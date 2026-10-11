using System;
namespace Emberfall
{
    public static class CameraVisibilityRules
    {
        public const int MaximumSurfaces=256,MaximumFaded=32;
        public static bool ReserveGroup(ref int available,int needed)
        {if(needed<0||needed>available)return false;available-=needed;return true;}
        // Segment slab intersection includes feet/torso/target endpoints, never geometry behind them.
        public static bool ProtectsBox(float ox,float oy,float oz,float px,float py,float pz,float minX,float minY,float minZ,float maxX,float maxY,float maxZ)
        {
            float sum=ox+oy+oz+px+py+pz+minX+minY+minZ+maxX+maxY+maxZ;
            if(float.IsNaN(sum)||float.IsInfinity(sum))return false;
            float dx=px-ox,dy=py-oy,dz=pz-oz,near=0,far=1;
            float distance=(float)Math.Sqrt(dx*dx+dy*dy+dz*dz);
            if(distance<=.01f)return false;
            return Slab(ox,dx,minX,maxX,ref near,ref far)&&Slab(oy,dy,minY,maxY,ref near,ref far)&&
                Slab(oz,dz,minZ,maxZ,ref near,ref far)&&near*distance<distance-.25f;
        }
        private static bool Slab(float origin,float direction,float min,float max,ref float near,ref float far)
        {
            if(Math.Abs(direction)<.000001f)return origin>=min&&origin<=max;
            float first=(min-origin)/direction,last=(max-origin)/direction;
            if(first>last){float swap=first;first=last;last=swap;}
            near=Math.Max(near,first);far=Math.Min(far,last);return near<=far;
        }
        public static float FadeStep(float current,bool occluded,float seconds)
        {
            if(float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<=0)return current;
            float target=occluded?.2f:1f;
            float blend=1-(float)Math.Exp(-10*Math.Min(seconds,.1f));
            return Math.Max(.2f,Math.Min(1,current+(target-current)*blend));
        }
        public static float Zoom(float normal,float nearestBuilding)
        {float proximity=1-Math.Max(0,Math.Min(1,nearestBuilding/4));return Math.Max(13,normal*(1-.16f*proximity));}
        public static float Projection(float normalizedAnchor)
        {return 1-2*Math.Max(.25f,Math.Min(.75f,normalizedAnchor));}
    }
}

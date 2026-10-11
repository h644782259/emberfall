using System;
namespace Emberfall
{
    public static class WorldLabelReadability
    {
        public const int MaximumLabels=128;
        public const float MaximumDistance=34f;
        public static bool Visible(float depth,float distanceSquared)
        {return depth>0&&!float.IsNaN(distanceSquared)&&distanceSquared<=MaximumDistance*MaximumDistance;}
        public static float Scale(float projectedPixels,int lines,float hudScale=1)
        {
            if(float.IsNaN(projectedPixels)||float.IsInfinity(projectedPixels)||projectedPixels<=.01f)return 1;
            int count=Math.Max(1,Math.Min(8,lines));
            // Keep a twelve physical-pixel floor on small displays; larger HUDs grow together.
            float density=float.IsNaN(hudScale)||float.IsInfinity(hudScale)?1:Math.Max(.3f,hudScale);
            return Math.Max(Math.Max(12,12*density)*count,Math.Min(Math.Max(12,22*density)*count,projectedPixels))/projectedPixels;
        }
    }
}

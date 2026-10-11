using System;
namespace Emberfall
{
    public static class HudLogicalScale
    {
        public static float For(float width,float height)
        {if(float.IsNaN(width)||float.IsNaN(height)||float.IsInfinity(width)||float.IsInfinity(height))return 1;return Math.Max(.3f,Math.Min(width/1280f,height/720f));}
    }
}

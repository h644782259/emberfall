using System;
namespace Emberfall
{
    // Bounded deterministic cosmetic placement. Tests provide geometric coverage predicates;
    // runtime supplies actual area line-of-sight plus the complete local footprint.
    public static class FilledVfxPlacement
    {
        private static readonly float[] Scales={1f,.72f,.45f,.24f,.1f};
        public static bool TryPlace(float x,float z,float radius,float extent,Func<float,float,float,bool> clear,
            out float placedX,out float placedZ,out float scale)
        {
            placedX=placedZ=0;scale=0;
            if(clear==null||!Finite(x)||!Finite(z)||!Finite(radius)||!Finite(extent)||radius<=0||extent<=0)return false;
            float distance=(float)Math.Sqrt(x*x+z*z),heading=distance>.001f?(float)Math.Atan2(z,x):0;
            // Full-size candidates in clear directions precede any reduction; no random retry/pop.
            foreach(float shrink in Scales)
            {
                float footprint=extent*shrink;
                if(footprint>radius)continue;
                float radial=Math.Min(distance,radius-footprint);
                for(int ring=0;ring<2;ring++)for(int n=0;n<16;n++)
                {
                    // At center, first preserve the exact landing point, then search its visible side.
                    float r=ring==0?radial:Math.Min(radius-footprint,Math.Max(radial,radius*.38f));
                    if(r<.001f&&n>0)break; // Do not retest the same center sixteen times.
                    float angle=heading+(n%2==0?1:-1)*((n+1)/2)*(float)Math.PI/8;
                    float px=(float)Math.Cos(angle)*r,pz=(float)Math.Sin(angle)*r;
                    if(!clear(px,pz,footprint))continue;
                    placedX=px;placedZ=pz;scale=shrink;return true;
                }
            }
            return false;
        }
        private static bool Finite(float x)=>!float.IsNaN(x)&&!float.IsInfinity(x);
    }
}

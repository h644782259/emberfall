using System;
namespace Emberfall
{
    public struct WaterFlowSample
    {
        public readonly float X,Z,DirectionX,DirectionZ;
        public WaterFlowSample(float x,float z,float dx,float dz){X=x;Z=z;DirectionX=dx;DirectionZ=dz;}
    }
    public sealed class WaterFlowPath
    {
        private readonly float[] x,z,lengths;
        public float Length {get;private set;}
        public WaterFlowPath(float[] x,float[] z)
        {
            if(x==null||z==null||x.Length!=z.Length||x.Length<2)throw new ArgumentException("Flow needs matching path points");
            this.x=(float[])x.Clone();this.z=(float[])z.Clone();lengths=new float[x.Length];
            for(int i=0;i<x.Length;i++)
            {
                if(float.IsNaN(x[i])||float.IsNaN(z[i])||float.IsInfinity(x[i])||float.IsInfinity(z[i]))throw new ArgumentException("Finite path required");
                if(i>0){float dx=x[i]-x[i-1],dz=z[i]-z[i-1];Length+=(float)Math.Sqrt(dx*dx+dz*dz);lengths[i]=Length;}
            }
            if(Length<.01f)throw new ArgumentException("Flow needs nonzero path");
        }
        public WaterFlowSample Sample(float distance)
        {
            if(float.IsNaN(distance)||float.IsInfinity(distance))distance=0;
            distance-=(float)Math.Floor(distance/Length)*Length;
            for(int i=1;i<x.Length;i++)
            {
                float span=lengths[i]-lengths[i-1];if(span<.001f||distance>lengths[i])continue;
                float t=(distance-lengths[i-1])/span;
                return new WaterFlowSample(x[i-1]+(x[i]-x[i-1])*t,z[i-1]+(z[i]-z[i-1])*t,(x[i]-x[i-1])/span,(z[i]-z[i-1])/span);
            }
            return new WaterFlowSample(x[0],z[0],0,1);
        }
    }
    public sealed class WaterFlowClock
    {
        public float Distance {get;private set;}
        private float pending;
        public static float Speed(WaterEnvironment environment){return environment==WaterEnvironment.Brook?.85f:environment==WaterEnvironment.Courtyard?.22f:.45f;}
        public bool Advance(float delta,float speed,float length)
        {
            if(delta<=0||float.IsNaN(delta)||float.IsInfinity(delta)||length<=0||float.IsNaN(length)||float.IsInfinity(length)||float.IsNaN(speed)||float.IsInfinity(speed))return false;
            pending+=Math.Min(delta,.25f);if(pending<.1f)return false;
            Distance=(Distance+pending*Math.Max(0,speed))%length;pending=0;return true;
        }
    }
}

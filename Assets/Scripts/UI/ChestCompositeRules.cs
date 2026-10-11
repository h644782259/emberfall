using System;
namespace Emberfall
{
    // RGBA bytes, independent of CPU endianness. Interpolate premultiplied color,
    // then unpremultiply once for the single standard-alpha GUI draw.
    public static class ChestCompositeRules
    {
        public const int Steps=8;
        public static uint Blend(uint a,uint b,int step)
        {
            step=Math.Max(0,Math.Min(Steps,step));int left=Steps-step;
            int aa=(int)(a>>24),ba=(int)(b>>24),weighted=aa*left+ba*step;
            if(weighted==0)return 0;
            uint result=(uint)((weighted+Steps/2)/Steps)<<24;
            for(int shift=0;shift<24;shift+=8)
            {int value=(int)((a>>shift)&255)*aa*left+(int)((b>>shift)&255)*ba*step;result|=(uint)((value+weighted/2)/weighted)<<shift;}
            return result;
        }
    }
}

using System;
using System.Collections.Generic;
namespace Emberfall
{
    public enum AttachmentShape { Sphere,Box,Disc }
    public enum RelicPartKind { Mount,Gem,Wing,Satellite,Halo,Mark }
    public struct EquipmentAttachmentPart
    {
        public readonly RelicPartKind Kind;
        public readonly AttachmentShape Shape;
        public readonly float X,Y,Z,Width,Height,Depth,Roll;
        public EquipmentAttachmentPart(RelicPartKind kind,AttachmentShape shape,float x,float y,float z,float w,float h,float d,float roll=0)
        {Kind=kind;Shape=shape;X=x;Y=y;Z=z;Width=w;Height=h;Depth=d;Roll=roll;}
        public float HalfX {get {float a=Roll*(float)Math.PI/180;return Math.Abs((float)Math.Cos(a))*Width*.5f+Math.Abs((float)Math.Sin(a))*Height*.5f;}}
        public float HalfY {get {if(Shape==AttachmentShape.Disc)return Depth*.5f;float a=Roll*(float)Math.PI/180;return Math.Abs((float)Math.Sin(a))*Width*.5f+Math.Abs((float)Math.Cos(a))*Height*.5f;}}
        public float HalfZ {get{return Shape==AttachmentShape.Disc?Height:Depth*.5f;}}
    }
    public static class EquipmentAttachmentRecipe
    {
        // Shared waist-clasp envelope leaves chest class emblems (>1.1), face (>1.75),
        // lateral grips (|x|>.34) and back fashions (z<0) readable in authored rest poses.
        // It is a structural envelope, not a guarantee for every animated silhouette.
        public static EquipmentAttachmentPart[] Relic(int tier,int upgrade)
        {
            tier=Math.Max(1,Math.Min(4,tier));upgrade=Math.Max(0,Math.Min(10,upgrade));
            float size=.09f+tier*.009f,y=.84f;var p=new List<EquipmentAttachmentPart>();
            p.Add(new EquipmentAttachmentPart(RelicPartKind.Mount,AttachmentShape.Disc,0,y,.43f,size*1.7f,.035f,size*1.7f));
            p.Add(new EquipmentAttachmentPart(RelicPartKind.Gem,AttachmentShape.Sphere,0,y,.48f,size,size*1.18f,size*.7f));
            for(int side=-1;side<=1;side+=2)
            {
                if(tier>=2)p.Add(new EquipmentAttachmentPart(RelicPartKind.Wing,AttachmentShape.Box,side*(size+.035f),y,.46f,.065f+tier*.01f,.04f,.055f,side*22));
                if(upgrade>=3)p.Add(new EquipmentAttachmentPart(RelicPartKind.Satellite,AttachmentShape.Sphere,side*(size+.08f),y+.005f,.48f,.045f,.045f,.045f));
            }
            if(upgrade>=7)p.Add(new EquipmentAttachmentPart(RelicPartKind.Halo,AttachmentShape.Sphere,0,.975f,.46f,upgrade>=10?.08f:.06f,upgrade>=10?.08f:.06f,upgrade>=10?.08f:.06f));
            for(int i=0;i<upgrade;i++)
            {float a=i*(float)Math.PI*2/10;p.Add(new EquipmentAttachmentPart(RelicPartKind.Mark,AttachmentShape.Sphere,(float)Math.Cos(a)*(size+.03f),y+(float)Math.Sin(a)*(size+.03f),.49f,.035f,.035f,.035f));}
            return p.ToArray();
        }
    }
}

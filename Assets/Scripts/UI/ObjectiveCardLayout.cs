using System;
namespace Emberfall
{
 public struct ObjectiveCardLayout
 {
  public float HeadingY,BodyY,ProgressY,ChargeY,Height;
  public ObjectiveCardLayout(float headingHeight,float bodyHeight,float progressHeight,float chargeHeight)
  {HeadingY=8;BodyY=HeadingY+Math.Max(17,headingHeight)+6;ProgressY=BodyY+Math.Max(24,bodyHeight)+4;ChargeY=ProgressY+Math.Max(18,progressHeight)+8;Height=chargeHeight>0?ChargeY+chargeHeight+10:ProgressY+Math.Max(18,progressHeight)+10;}
 }
}

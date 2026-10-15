float _EmberTerrainEnabled,_EmberTerrainDungeon,_EmberTerrainRadius;
float EmberHill(float2 p,float2 center,float radius,float height)
{float q=saturate(1-dot(p-center,p-center)/(radius*radius));return height*q*q*q;}
float EmberHeight(float2 p)
{
 if(_EmberTerrainDungeon>.5){float r=_EmberTerrainRadius;float side=saturate((abs(p.x)-3)/3);side=side*side*(3-2*side);float crossing=saturate((abs(p.y)-3.3)/2);crossing=crossing*crossing*(3-2*crossing);
 return _EmberTerrainEnabled*side*crossing*(EmberHill(p,float2(-r*.52,-r*.38),r*.42,2.4)+EmberHill(p,float2(r*.48,r*.42),r*.40,2.1)+EmberHill(p,float2(-r*.46,r*.50),r*.34,1.4)+EmberHill(p,float2(r*.55,-r*.40),r*.35,1.3));}
 return _EmberTerrainEnabled*(EmberHill(p,float2(-13,-11),7,2)+EmberHill(p,float2(16,1),5.5,.85)+EmberHill(p,float2(-17,13),5,1.4)+EmberHill(p,float2(-20,-2),5,1.1)+EmberHill(p,float2(-24,-18),7.5,3.1)+EmberHill(p,float2(23,13),8,2.6)+EmberHill(p,float2(-9,25),7,2.8)+EmberHill(p,float2(11,26),6.5,1.8)+EmberHill(p,float2(27,-15),5,1.9));
}

// Height and its analytic slope share each hill evaluation. The old central
// difference evaluated the entire terrain five times for every world vertex.
float3 EmberHillGradient(float2 p,float2 center,float radius,float height)
{
 float2 d=p-center;
 float invRadius2=1/(radius*radius);
 float q=saturate(1-dot(d,d)*invRadius2);
 float slope=-6*height*q*q*invRadius2;
 return float3(height*q*q*q,slope*d.x,slope*d.y);
}
float3 EmberHeightGradient(float2 p)
{
 if(_EmberTerrainDungeon>.5)
 {
  // Preserve the old finite-difference normal across the narrow C1 corridor
  // seams, where a .02m sample can straddle different smoothstep segments.
  if(abs(abs(p.x)-3)<.021||abs(abs(p.x)-6)<.021||abs(abs(p.y)-3.3)<.021||abs(abs(p.y)-5.3)<.021)
   return float3(EmberHeight(p),
    (EmberHeight(p+float2(.02,0))-EmberHeight(p-float2(.02,0)))/.04,
    (EmberHeight(p+float2(0,.02))-EmberHeight(p-float2(0,.02)))/.04);
  float r=_EmberTerrainRadius;
  float t=saturate((abs(p.x)-3)/3),u=saturate((abs(p.y)-3.3)/2);
  float side=t*t*(3-2*t),crossing=u*u*(3-2*u);
  float sideSlope=2*t*(1-t)*sign(p.x),crossingSlope=3*u*(1-u)*sign(p.y);
  float3 field=EmberHillGradient(p,float2(-r*.52,-r*.38),r*.42,2.4)+
   EmberHillGradient(p,float2(r*.48,r*.42),r*.40,2.1)+
   EmberHillGradient(p,float2(-r*.46,r*.50),r*.34,1.4)+
   EmberHillGradient(p,float2(r*.55,-r*.40),r*.35,1.3);
  return _EmberTerrainEnabled*float3(field.x*side*crossing,
   field.y*side*crossing+field.x*sideSlope*crossing,
   field.z*side*crossing+field.x*side*crossingSlope);
 }
 return _EmberTerrainEnabled*(EmberHillGradient(p,float2(-13,-11),7,2)+EmberHillGradient(p,float2(16,1),5.5,.85)+
  EmberHillGradient(p,float2(-17,13),5,1.4)+EmberHillGradient(p,float2(-20,-2),5,1.1)+
  EmberHillGradient(p,float2(-24,-18),7.5,3.1)+EmberHillGradient(p,float2(23,13),8,2.6)+
  EmberHillGradient(p,float2(-9,25),7,2.8)+EmberHillGradient(p,float2(11,26),6.5,1.8)+EmberHillGradient(p,float2(27,-15),5,1.9));
}

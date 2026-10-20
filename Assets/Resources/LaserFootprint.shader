Shader "Emberfall/Laser Solar Footprint"
{
 Properties { _Phase("Time",Float)=0 _Opacity("Envelope",Range(0,1))=1 }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent"} Blend SrcAlpha One ZWrite Off ZTest LEqual Cull Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata{float4 vertex:POSITION;float2 uv:TEXCOORD0;};
 struct v2f{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
 float _Phase,_Opacity;
 v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
 fixed4 frag(v2f i):SV_Target{
  float2 p=(i.uv-.5)*2;float r=length(p),a=atan2(p.y,p.x);
  float disc=1-smoothstep(.65,.76,r);
  float edge=exp(-pow((r-.72)*25,2));
  float rays=pow(.5+.5*sin(a*19+sin(a*7+_Phase*2)*1.7-_Phase),6);
  float corona=exp(-max(0,r-.73)*18)*smoothstep(.65,.75,r)*rays;
  float fade=1-smoothstep(.88,1,r);
  float pulse=.8+.2*sin(_Phase*8);
  float wave=frac(_Phase*1.7);
  float ripple=exp(-pow((r-wave*.8)*35,2))*(1-wave);
  float impact=exp(-r*r*45)*(.7+.3*sin(_Phase*13));
  float3 color=lerp(float3(.12,.65,1),float3(.75,.95,1),edge);
  return fixed4(color,(disc*.12+edge*.5+corona*.65+ripple*.28+impact*.7)*fade*pulse*_Opacity);
 }
 ENDCG }
 }
}

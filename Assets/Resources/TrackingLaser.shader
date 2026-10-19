Shader "Emberfall/Tracking Laser"
{
 Properties { _Tint("Tint",Color)=(.25,.8,1,1) _Phase("Phase",Float)=0 _Fade("Fade",Float)=0 }
 SubShader
 {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" }
  Pass
  {
   Blend SrcAlpha One
   ZWrite Off
   Cull Off
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct input { float4 vertex:POSITION;float2 uv:TEXCOORD0; };
   struct output { float4 position:SV_POSITION;float2 uv:TEXCOORD0; };
   fixed4 _Tint;float _Phase,_Fade;
   output vert(input v){output o;o.position=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
   fixed4 frag(output i):SV_Target
   {
    float streak=pow(saturate(.5+.5*sin(i.uv.x*75+sin(i.uv.y*12-_Phase*6))),8);
    float flow=.75+.25*sin(i.uv.y*35-_Phase*12);
    float tip=1-smoothstep(.8,1,i.uv.y);
    return fixed4(lerp(_Tint.rgb,fixed3(.85,1,1),streak),(.12+streak*.36)*flow*tip*_Fade);
   }
   ENDCG
  }
 }
}

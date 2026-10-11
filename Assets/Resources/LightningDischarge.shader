Shader "Emberfall/Lightning Discharge"
{
 Properties { _Tint("Electric blue",Color)=(.08,.6,1,1) _Opacity("Discharge envelope",Range(0,1))=1 _Phase("Discharge age",Float)=0 }
 SubShader
 {
  Tags {"Queue"="Transparent" "RenderType"="Transparent"}
  Blend SrcAlpha One ZWrite Off ZTest LEqual Cull Off
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;};
   struct v2f {float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 view:TEXCOORD1;float3 local:TEXCOORD2;};
   float4 _Tint;float _Opacity,_Phase;
   v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.view=WorldSpaceViewDir(v.vertex);o.local=v.vertex.xyz;return o;}
   fixed4 frag(v2f i):SV_Target
   {
    float facing=abs(dot(normalize(i.normal),normalize(i.view)));
    // A narrow near-white ionized core surrounded by cooler blue plasma.
    // Brightness follows the strike envelope; the latched branch path stays fixed.
    float filament=pow(facing,18);
    float grain=.92+.08*sin(i.local.y*47+_Phase*67);
    float3 color=lerp(_Tint.rgb*.82,float3(.93,.97,1)*1.2,filament)*grain;
    return fixed4(color,_Opacity*(.22+.78*facing));
   }
   ENDCG
  }
 }
}

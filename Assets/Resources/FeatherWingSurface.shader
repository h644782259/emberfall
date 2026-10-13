Shader "Emberfall/Feather Wing Surface"
{
 Properties { _MainTex("Painted feathers",2D)="white"{} _Cutoff("Silhouette",Range(0,1))=.55 _Glow("Rarity glow",Range(0,1))=0 }
 SubShader {
  Tags {"Queue"="AlphaTest" "RenderType"="TransparentCutout"}
  Pass {
   Cull Off ZWrite On
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex;float4 _MainTex_ST;float _Cutoff,_Glow;
   struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;float3 normal:NORMAL;};
   struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 normal:TEXCOORD1;};
   v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.uv,_MainTex);o.normal=UnityObjectToWorldNormal(v.normal);return o;}
   fixed4 frag(v2f i):SV_Target {
    fixed4 c=tex2D(_MainTex,i.uv);clip(c.a-_Cutoff);
    float light=.72+.24*abs(dot(normalize(i.normal),normalize(float3(.3,.7,.6))));
    float glow=_Glow*saturate((max(c.r,max(c.g,c.b))-.65)*2);
    return fixed4(c.rgb*(light+glow),1);
   }
   ENDCG
  }
 }
}

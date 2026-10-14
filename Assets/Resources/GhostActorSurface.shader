Shader "Emberfall/Ghost Actor Surface"
{
 Properties { _MainTex("Appearance",2D)="white"{} _Color("Original color",Color)=(1,1,1,1) _Cutoff("Silhouette",Range(0,1))=.1 }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent"}
  Pass {
   Cull Off ZWrite On Blend SrcAlpha OneMinusSrcAlpha
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex;float4 _MainTex_ST,_Color;float _Cutoff;
   struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;float3 normal:NORMAL;};
   struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 normal:TEXCOORD1;};
   v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.uv,_MainTex);o.normal=UnityObjectToWorldNormal(v.normal);return o;}
   fixed4 frag(v2f i):SV_Target {
    fixed4 art=tex2D(_MainTex,i.uv);clip(art.a-_Cutoff);
    float light=.62+.38*abs(dot(normalize(i.normal),normalize(float3(.3,.7,.6))));
    float3 color=lerp(art.rgb*_Color.rgb,float3(.38,.74,.88),.32)*light;
    return fixed4(color,.70*art.a);
   }
   ENDCG
  }
 }
}

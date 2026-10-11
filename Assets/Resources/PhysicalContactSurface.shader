Shader "Emberfall/Physical Contact Surface"
{
 Properties { _Tint("Material color",Color)=(.35,.28,.2,1) _DetailTex("Material pores",2D)="white" {} _Opacity("Coverage",Range(0,1))=1 _Liquid("Wet surface",Range(0,1))=0 }
 SubShader
 {
  Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" } ZWrite On Cull Back
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;};
   struct v2f {float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 local:TEXCOORD1;float3 view:TEXCOORD2;};
   sampler2D _DetailTex;float4 _Tint;float _Opacity,_Liquid;
   v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.local=v.vertex.xyz;o.view=WorldSpaceViewDir(v.vertex);return o;}
   fixed4 frag(v2f i):SV_Target
   {
    float coverage=frac(sin(dot(floor(i.pos.xy),float2(12.9898,78.233)))*43758.5453);clip(_Opacity-coverage);
    float3 n=normalize(i.normal),v=normalize(i.view),l=normalize(float3(-.35,.8,-.4));float3 weight=abs(n);weight/=max(.001,weight.x+weight.y+weight.z);
    float3 grain=tex2D(_DetailTex,i.local.yz*2).rgb*weight.x+tex2D(_DetailTex,i.local.xz*2).rgb*weight.y+tex2D(_DetailTex,i.local.xy*2).rgb*weight.z;
    float shade=.28+.72*saturate(dot(n,l));float spec=pow(saturate(dot(n,normalize(l+v))),lerp(12,65,_Liquid));
    float3 albedo=_Tint.rgb*lerp(.55+grain*1.2,1,_Liquid*.65);return fixed4(albedo*shade+lerp(_Tint.rgb,float3(.8,.8,.8),_Liquid)*spec*lerp(.12,.5,_Liquid),1);
   }
   ENDCG
  }
 }
}

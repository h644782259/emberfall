Shader "Emberfall/Thin Ground Skill Deposit"
{
 Properties { _MaterialAtlas("Natural soil",2D)="white"{} _Deposit("Wet soil / ash",Float)=0 _Opacity("Coverage",Range(0,1))=1 _Age("Age",Float)=0 }
 SubShader
 {
  Tags {"Queue"="Transparent-5" "RenderType"="Transparent"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Back
  Pass
  {
   Tags {"LightMode"="ForwardBase"}
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #pragma multi_compile_fwdbase
   #include "UnityCG.cginc"
   #include "UnityLightingCommon.cginc"
   #include "AutoLight.cginc"
   struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;float4 color:COLOR;};
   struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 normal:TEXCOORD1;float3 world:TEXCOORD2;float3 view:TEXCOORD3;float4 color:COLOR;SHADOW_COORDS(4)};
   sampler2D _MaterialAtlas;float _Deposit,_Opacity,_Age;
   v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.normal=UnityObjectToWorldNormal(v.normal);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.view=WorldSpaceViewDir(v.vertex);o.color=v.color;TRANSFER_SHADOW(o);return o;}
   float3 soil(float2 uv)
   {
    float2 mirrored=1-abs(frac(uv*.5)*2-1);
    float2 at=float2(.5,.5)+(.045+mirrored*.91)*float2(.25,.5);
    return tex2Dgrad(_MaterialAtlas,at,ddx(uv)*float2(.25,.5)*.91,ddy(uv)*float2(.25,.5)*.91).rgb;
   }
   fixed4 frag(v2f i):SV_Target
   {
    float3 n=normalize(i.normal),view=normalize(i.view),light=normalize(UnityWorldSpaceLightDir(i.world));
    UNITY_LIGHT_ATTENUATION(attenuation,i,i.world);
    float3 illumination=max(float3(.10,.10,.10),ShadeSH9(float4(n,1)))+_LightColor0.rgb*saturate(dot(n,light))*attenuation;
    float3 material=soil(i.uv*.72);float lum=dot(material,float3(.21,.72,.07));
    float3 natural=lerp(material,float3(lum,lum*.95,lum*.82),.62);
    float wet=smoothstep(.42,.72,i.color.r)*(.7+.3*i.color.g);
    float3 albedo=natural*float3(.55,.57,.49);float alpha=i.color.a*_Opacity*(.22+wet*.36);
    float spec=pow(saturate(dot(n,normalize(view+light))),48)*wet*.12;
    if(_Deposit>.5)
    {
     albedo=natural*.25;alpha=i.color.a*_Opacity*(.18+.22*i.color.g);spec=0;
    }
    float3 color=albedo*illumination+_LightColor0.rgb*spec*attenuation;
    // Sparse cooling grains sit inside the ash, without a luminous outline.
    if(_Deposit>.5)color+=float3(.37,.055,.009)*pow(i.color.g,15)*exp(-_Age*2.4);
    return fixed4(color,saturate(alpha));
   }
   ENDCG
  }
 }
}

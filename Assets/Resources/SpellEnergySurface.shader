Shader "Emberfall/Layered Spell Energy"
{
 Properties { _Tint("Energy tint",Color)=(.3,.7,1,1) _FlameTex("Natural combustion",2D)="white" {} _Opacity("Envelope",Range(0,1))=1 _Phase("Flow time",Float)=0 _DarkCore("Absorption core",Float)=0 _Heat("Hot emission",Float)=1 _Wispy("Turbulent wind wall",Float)=0 }
 SubShader
 {
  Tags {"Queue"="Transparent" "RenderType"="Transparent"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Off
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
   struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;};
   struct v2f {float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 local:TEXCOORD1;float3 view:TEXCOORD2;float2 uv:TEXCOORD3;float3 world:TEXCOORD4;float4 screen:TEXCOORD5;float eye:TEXCOORD6;SHADOW_COORDS(7)};
   sampler2D _FlameTex;UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);float4 _Tint;float _Opacity,_Phase,_DarkCore,_Heat,_Wispy;
   float hash(float3 p){p=frac(p*.3183+.17);p*=19;return frac(p.x*p.y*p.z*(p.x+p.y+p.z));}
   float noise(float3 p){float3 b=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(lerp(hash(b),hash(b+float3(1,0,0)),f.x),lerp(hash(b+float3(0,1,0)),hash(b+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(b+float3(0,0,1)),hash(b+float3(1,0,1)),f.x),lerp(hash(b+float3(0,1,1)),hash(b+1),f.x),f.y),f.z);}
   v2f vert(appdata v){v2f o;float wave=sin(v.vertex.y*6+v.vertex.x*3-_Phase*7)*.025*(1-_DarkCore);v.vertex.xyz+=v.normal*wave;o.pos=UnityObjectToClipPos(v.vertex);o.local=v.vertex.xyz;o.normal=UnityObjectToWorldNormal(v.normal);o.view=WorldSpaceViewDir(v.vertex);o.uv=v.uv;o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.screen=ComputeScreenPos(o.pos);o.eye=-UnityObjectToViewPos(v.vertex).z;TRANSFER_SHADOW(o);return o;}
   fixed4 frag(v2f i):SV_Target
   {
    float3 n=normalize(i.normal);float3 light=normalize(UnityWorldSpaceLightDir(i.world));UNITY_LIGHT_ATTENUATION(attenuation,i,i.world);
    float3 illumination=max(float3(.10,.10,.10),ShadeSH9(float4(n,1)))+_LightColor0.rgb*saturate(dot(n,light))*attenuation;
    float rim=pow(1-saturate(abs(dot(n,normalize(i.view)))),3);
    float grain=noise(i.local*6+float3(_Phase*.8,-_Phase*2,0));float fine=noise(i.local*28+_Phase);
    float streams=pow(.5+.5*sin(i.uv.x*97-grain*7-_Phase*13),5);
    float shade=saturate(dot(illumination,float3(.21,.72,.07)));
    float3 dark=_Tint.rgb*.15,hot=lerp(_Tint.rgb,float3(1,.96,.82),.10);
    float3 color=lerp(dark,_Tint.rgb,grain)*illumination+hot*(streams*.22+pow(rim,4)*.18+pow(fine,10)*.25)*_Heat;
    float alpha=_Opacity*_Tint.a*(.5+.5*smoothstep(.15,.75,grain));
    if(_Tint.r>_Tint.b*1.8&&_Tint.r>_Tint.g*1.25&&_Wispy<.5&&_DarkCore<.5)
    {
     float2 flameUV=float2(frac(i.uv.x+.045*sin(_Phase*2+i.local.y*3)),saturate(i.uv.y*.92+.035*sin(_Phase*3)));
     float4 combustion=tex2D(_FlameTex,flameUV);
     float3 hotMatter=combustion.rgb*float3(.94,.64,.22);
     color=hotMatter*(.74+.16*shade);
     // Combustion keeps its own modest emission; no white shell or rigid rim.
     alpha=_Opacity*_Tint.a*smoothstep(.055,.55,combustion.a)*smoothstep(.16,.51,grain)*(.72+.28*abs(dot(n,normalize(i.view))));
    }
    if(_Wispy>.5){float flow=noise(i.local*float3(5,9,5)+float3(_Phase*2,-_Phase*3,_Phase));float filaments=noise(i.local*float3(12,22,12)+float3(-_Phase*3,_Phase*2,0));float edge=smoothstep(0,.12,i.uv.y)*(1-smoothstep(.78,1,i.uv.y));alpha*=smoothstep(.25,.65,flow)*(.3+.7*filaments)*edge;color=lerp(_Tint.rgb*.35,_Tint.rgb*.9,flow)*illumination+hot*pow(rim,3)*.04;}
    if(_DarkCore>.5){color=float3(.008,.005,.023)+_Tint.rgb*pow(rim,2)*1.2;alpha=_Opacity;}
    if(_DarkCore<.5)
    {
     float raw=SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture,UNITY_PROJ_COORD(i.screen));
     float orthographicDepth=raw;
     #if defined(UNITY_REVERSED_Z)
     orthographicDepth=1-raw;
     #endif
     float scene=lerp(LinearEyeDepth(raw),lerp(_ProjectionParams.y,_ProjectionParams.z,orthographicDepth),unity_OrthoParams.w);
     alpha*=saturate((scene-i.eye)/.10);
    }
    return fixed4(color,saturate(alpha));
   }
   ENDCG
  }
 }
}

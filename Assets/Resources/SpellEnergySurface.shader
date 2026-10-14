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
    float3 flow=i.world*3+float3(_Phase*.35,-_Phase*.9,0);
    float grain=noise(flow);float fine=noise(flow*2.13+grain);
    float streams=smoothstep(.52,.86,grain*.65+fine*.35);
    float shade=saturate(dot(illumination,float3(.21,.72,.07)));
    float3 dark=_Tint.rgb*.15,hot=lerp(_Tint.rgb,float3(1,.96,.82),.10);
    float3 color=lerp(dark,_Tint.rgb,grain)*illumination+hot*(streams*.22+pow(rim,4)*.18+pow(fine,10)*.25)*_Heat;
    float alpha=_Opacity*_Tint.a*smoothstep(.12,.72,grain)*(.25+.75*smoothstep(.02,.55,abs(dot(n,normalize(i.view)))));
    if(_Tint.r>_Tint.b*1.8&&_Tint.r>_Tint.g*1.25&&_Wispy<.5&&_DarkCore<.5)
    {
     // Match the field's continuous current across moving jets and residual fuel.
     float3 current=i.world*2.1-float3(_Time.y*.16,_Time.y*1.8,_Time.y*.11);
     float broad=noise(current);
     float density=broad*.68+noise(current*2.07+float3(broad*1.4,0,broad))*.32;
     float height=saturate(i.uv.y);
     float temperature=saturate(density*.75+(1-height)*.32);
     color=lerp(float3(.65,.065,.008),float3(1,.58,.09),smoothstep(.25,.85,temperature));
     alpha=_Opacity*_Tint.a*smoothstep(.22,.72,density-height*.12)
      *smoothstep(.02,.5,abs(dot(n,normalize(i.view))))*(1-smoothstep(.55,1,height));
    }
    if(_Wispy>.5){float flow=noise(i.world*float3(2,3,2)+float3(_Phase*.7,-_Phase*1.8,_Phase*.3));float filaments=noise(i.world*float3(4,7,4)+float3(-_Phase,_Phase*1.4,0));float edge=smoothstep(0,.12,i.uv.y)*(1-smoothstep(.78,1,i.uv.y));alpha*=smoothstep(.25,.65,flow)*(.3+.7*filaments)*edge;color=lerp(_Tint.rgb*.35,_Tint.rgb*.9,flow)*illumination+hot*pow(rim,3)*.04;}
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

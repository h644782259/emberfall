Shader "Emberfall/Fractured Gravity Matter"
{
 Properties { _Phase("Time",Float)=0 _Opacity("Coverage",Range(0,1))=1 _Tint("Crack emission",Color)=(.6,.18,1,1) _RockTex("Basalt",2D)="white" {} }
 SubShader { Tags {"Queue"="AlphaTest" "RenderType"="TransparentCutout"} ZWrite On Cull Back
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma target 3.0
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;};
 struct v2f {float4 pos:SV_POSITION;float3 local:TEXCOORD0;float3 normal:TEXCOORD1;float3 view:TEXCOORD2;};
 sampler3D _FlowNoise;sampler2D _RockTex;float _Phase,_Opacity;float4 _Tint;
 float noise(float3 p){return tex3Dlod(_FlowNoise,float4(p/32,0)).r;}
 v2f vert(appdata v){v2f o;float g=noise(v.vertex.xyz*17);v.vertex.xyz+=v.normal*(g-.5)*.035;o.pos=UnityObjectToClipPos(v.vertex);o.local=v.vertex.xyz;o.normal=UnityObjectToWorldNormal(v.normal);o.view=WorldSpaceViewDir(v.vertex);return o;}
 fixed4 frag(v2f i):SV_Target{
  clip(_Opacity-frac(sin(dot(floor(i.pos.xy),float2(12.9898,78.233)))*43758.5453));
  float3 n=normalize(i.normal),v=normalize(i.view),l=normalize(float3(-.4,.8,-.5));
  float3 weights=pow(abs(n),4);weights/=max(.001,dot(weights,float3(1,1,1)));
  float3 rock=tex2D(_RockTex,i.local.yz*2).rgb*weights.x+tex2D(_RockTex,i.local.xz*2).rgb*weights.y+tex2D(_RockTex,i.local.xy*2).rgb*weights.z;
  float grain=noise(i.local*19),fine=noise(i.local*53);
  float crack=1-smoothstep(.014,.055,abs(noise(i.local*37)-.5));
  float rim=pow(1-saturate(dot(n,v)),3);
  float shade=.18+.65*saturate(dot(n,l));
  float3 color=(rock*.65+float3(.04,.025,.065))*(.65+fine*.6)*shade;
  color+=_Tint.rgb*(crack*(.25+.15*sin(_Phase*3+grain*18))+rim*.5);
  color+=float3(.35,.42,.6)*pow(saturate(dot(n,normalize(v+l))),32)*.3;
  return fixed4(color,1);
 }
 ENDCG }
 UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
 }
}

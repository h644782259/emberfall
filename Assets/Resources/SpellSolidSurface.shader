Shader "Emberfall/Solid Spell Surface"
{
 Properties { _Tint("Body color",Color)=(.1,.4,1,1) _RockTex("Basalt material",2D)="white" {} _RockDetail("Authored material blend",Range(0,1))=0 _Phase("Animation age",Float)=0 _Opacity("Lifetime envelope",Range(0,1))=1 _Surface("Rock / arrow / crystal",Float)=0 }
 SubShader
 {
  Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
  ZWrite On ZTest LEqual Cull Back
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #include "UnityCG.cginc"
   struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;};
   struct v2f {float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 local:TEXCOORD1;float3 view:TEXCOORD2;float2 uv:TEXCOORD3;};
   sampler2D _RockTex;float4 _Tint;float _Phase,_Opacity,_Surface,_RockDetail;
   float hash(float3 p){p=frac(p*.3183+.17);p*=19;return frac(p.x*p.y*p.z*(p.x+p.y+p.z));}
   float noise(float3 p){float3 b=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(lerp(hash(b),hash(b+float3(1,0,0)),f.x),lerp(hash(b+float3(0,1,0)),hash(b+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(b+float3(0,0,1)),hash(b+float3(1,0,1)),f.x),lerp(hash(b+float3(0,1,1)),hash(b+1),f.x),f.y),f.z);}
   v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.local=v.vertex.xyz;o.view=WorldSpaceViewDir(v.vertex);o.uv=v.uv;return o;}
   fixed4 frag(v2f i):SV_Target
   {
    // Fade by coverage, keeping the solid body and its depth instead of a transparent shell.
    clip(_Opacity-hash(float3(floor(i.pos.xy),0)));
    float3 n=normalize(i.normal),v=normalize(i.view),l=normalize(float3(-.4,.8,-.5));
    float shade=.23+.77*saturate(dot(n,l));float rim=pow(1-saturate(abs(dot(n,v))),5);
    float grain=noise(i.local*12),fine=noise(i.local*42);
    float3 color;
    if(_Surface<.5)
    {
     float seam=1-smoothstep(.012,.045,abs(noise(i.local*5)-.48));
     float3 axis=abs(n);axis/=max(.001,axis.x+axis.y+axis.z);
     float3 basalt=tex2D(_RockTex,i.local.yz*1.3).rgb*axis.x+tex2D(_RockTex,i.local.xz*1.3).rgb*axis.y+tex2D(_RockTex,i.local.xy*1.3).rgb*axis.z;
     float3 albedo=lerp(lerp(float3(.045,.028,.025),_Tint.rgb*.65,grain),basalt*(.65+.35*_Tint.rgb),_RockDetail);
     color=albedo*shade;
     color+=float3(1,.16,.008)*seam*(.65+.15*sin(_Phase*7));
     color+=_Tint.rgb*rim*.12;
    }
    else if(_Surface<1.5)
    {
     float grooves=.7+.3*sin(i.local.y*95+fine*4);
     color=_Tint.rgb*shade*grooves;
     float spec=pow(saturate(dot(n,normalize(l+v))),36);
     color+=lerp(_Tint.rgb,float3(1,.85,.4),.15)*spec*.28;
    }
    else
    {
     float cracks=1-smoothstep(.008,.026,abs(fine-.5));
     color=_Tint.rgb*(.3+.6*shade)+_Tint.rgb*(cracks*.14+rim*.25);
     color+=float3(.3,.75,1)*pow(saturate(dot(n,normalize(l+v))),64)*.2;
    }
    return fixed4(color,1);
   }
   ENDCG
  }
 }
}

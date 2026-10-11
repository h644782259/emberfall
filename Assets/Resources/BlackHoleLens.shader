Shader "Emberfall/Black Hole Lens"
{
 Properties { _Opacity("Lifetime envelope",Range(0,1))=1 _Phase("Flow age",Float)=0 }
 SubShader
 {
  Tags {"Queue"="Transparent-10" "RenderType"="Transparent"}
  Pass
  {
   Blend SrcAlpha OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Off
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #include "UnityCG.cginc"
   float _Opacity,_Phase;
   struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
   struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
   v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
   float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float noise(float2 p){float2 b=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(b),hash(b+float2(1,0)),f.x),lerp(hash(b+float2(0,1)),hash(b+1),f.x),f.y);}
   fixed4 frag(v2f i):SV_Target
   {
    float2 p=i.uv*2-1;float r=length(p);clip(1-r);
    // Only the black hole artwork moves; world geometry and scene pixels stay untouched.
    float ring=exp(-pow((r-.435)/.025,2));
    float angle=atan2(p.y,p.x);float flow=noise(float2(angle*3-_Phase*1.5,r*45));
    // Coherent rotating hot knots and trailing texture make angular travel visible.
    float orbitAngle=angle-_Phase*1.65;
    float knots=pow(.5+.5*cos(orbitAngle*3+r*7),10);
    float grains=noise(float2(cos(orbitAngle),sin(orbitAngle))*9+r*float2(37,19));
    float uneven=.3+knots*1.35+grains*.28;
    float wake=pow(.5+.5*cos(orbitAngle*3-r*17-.65),5);
    // A single thin, inclined disk and its glow halo replace the giant concentric hoops.
    float diskY=p.y-p.x*.16+.065;
    float disk=exp(-abs(diskY)*65)*(1-smoothstep(.65,.95,abs(p.x)))*smoothstep(.2,.48,abs(p.x));
    float halo=exp(-pow((r-.47)/.07,2))*(.18+grains*.22+wake*.6);
    float shadow=1-smoothstep(.405,.419,r);
    float3 hot=lerp(float3(1,.12,.012),float3(1,.69,.23),flow);
    float3 light=hot*(ring*uneven+halo*.35+disk*(.35+.85*noise(float2(p.x*21-_Phase*7,p.y*17))));
    float3 color=float3(.001,.001,.002)*shadow;
    color+=light;
    float coverage=max(shadow,max(ring,max(halo,disk)));
    return fixed4(color,saturate(coverage)*_Opacity);
   }
   ENDCG
  }
 }
}

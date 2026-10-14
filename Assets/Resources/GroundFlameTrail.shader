Shader "Emberfall/Continuous Ground Flame"
{
 Properties { _Clock("Producer clock",Float)=0 _Life("Burn lifetime",Float)=3.2 }
 SubShader
 {
  Tags {"Queue"="Transparent" "RenderType"="Transparent"}
  Blend One OneMinusSrcAlpha ZWrite Off Cull Back
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #include "UnityCG.cginc"
   struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;float3 side:TEXCOORD1;};
   struct v2f {float4 pos:SV_POSITION;float3 world:TEXCOORD0;float2 uv:TEXCOORD1;float4 screen:TEXCOORD2;float4 support:TEXCOORD3;};
   float _Clock,_Life,_Steps;
   UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
   sampler3D _FlowNoise;
   float noise(float3 p){float3 cell=floor(p),f=frac(p);f=f*f*(3-2*f);return tex3Dlod(_FlowNoise,float4((cell+f+.5)/32,0)).r;}
   v2f vert(appdata v)
   {
    v2f o;float3 world=mul(unity_ObjectToWorld,v.vertex).xyz;
    o.support=float4(v.side.x,world.y,v.side.z,v.side.y);
    // This is a volume boundary, never a visible flame surface.
    world.y+=1.65*sqrt(saturate(1-v.uv.x*v.uv.x));
    o.pos=UnityWorldToClipPos(world);o.world=world;o.uv=v.uv;
    o.screen=ComputeScreenPos(o.pos);return o;
   }
   fixed4 frag(v2f i):SV_Target
   {
    float age=max(0,_Clock-i.uv.y);
    float envelope=smoothstep(0,.12,age+.025)*(1-smoothstep(_Life-.85,_Life,age));
    clip(envelope-.001);
    float3 ray=normalize(lerp(i.world-_WorldSpaceCameraPos,-UNITY_MATRIX_V[2].xyz,unity_OrthoParams.w));
    float raw=SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture,UNITY_PROJ_COORD(i.screen));
    float ortho=raw;
    #if defined(UNITY_REVERSED_Z)
    ortho=1-raw;
    #endif
    float scene=lerp(LinearEyeDepth(raw),lerp(_ProjectionParams.y,_ProjectionParams.z,ortho),unity_OrthoParams.w);
    float4 sum=0;
    // Integrate rising fuel through a continuous world-space field. No per-step sprites.
    float stride=2.4/_Steps;
    [loop] for(int s=0;s<12;s++)
    {
     if(s>=_Steps)break;
     float3 p=i.world+ray*((s+.5)*stride);
     float height=p.y-i.support.y;
     float across=i.uv.x+dot(p.xz-i.world.xz,i.support.xz)/1.05;
     float eye=-mul(UNITY_MATRIX_V,float4(p,1)).z;
     if(eye>=scene||height<0)break;
     if(height>1.6)continue;
     float h=height/1.45;
     float2 curl=float2(sin(p.z*3.1-_Clock*2.4+h*4),cos(p.x*2.7+_Clock*1.8-h*3))*(.10+h*.24);
     float3 flow=float3((p.xz+curl)*2.8,height*2.1-_Clock*2.9);
     float fuel=noise(flow)*.72+noise(flow*2.03+7.1)*.28;
     float body=smoothstep(.29+h*.52,.44+h*.46,fuel);
     float width=1-smoothstep(.30,.98,abs(across)+h*.27);
     float density=body*width*saturate(height*16)*(1-smoothstep(.65,1.04,h));
     density*=step(0,height)*step(height,1.6)*saturate((scene-eye)*12)*envelope*smoothstep(0,.9,i.support.w);
     float a=1-exp(-density*stride*3);
     float heat=saturate((fuel-.38)*1.35+(1-h)*.27);
     float3 color=lerp(float3(1,.095,.008),float3(1,.48,.025),smoothstep(.25,.65,heat));
     color=lerp(color,float3(1,.78,.18),smoothstep(.70,.96,heat));
     sum.rgb+=(1-sum.a)*color*a;sum.a+=(1-sum.a)*a;
    }
    return sum;
   }
   ENDCG
  }
 }
}

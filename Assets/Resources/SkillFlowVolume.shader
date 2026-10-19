Shader "Emberfall/Continuous Skill Volume"
{
 Properties { _Clock("Producer time",Float)=0 _Opacity("Envelope",Float)=1 _Kind("Element",Float)=0 _Steps("Samples",Float)=20 _Tint("Tint",Color)=(1,.3,.03,1) _Density("Optical density",Float)=1 _AlphaLimit("Maximum coverage",Float)=1 _Sparsity("Open flame gaps",Float)=0 }
 SubShader
 {
  Tags {"Queue"="Transparent" "RenderType"="Transparent"}
  Blend One OneMinusSrcAlpha ZWrite Off Cull Front ZTest Always
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #include "UnityCG.cginc"
   struct appdata {float4 vertex:POSITION;};
   struct v2f {float4 pos:SV_POSITION;float3 world:TEXCOORD0;float4 screen:TEXCOORD1;};
   float _Clock,_Opacity,_Kind,_Steps,_Density,_AlphaLimit,_Sparsity;float4 _Tint;
   UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
   sampler3D _FlowNoise;
   float noise(float3 p){float3 cell=floor(p),f=frac(p);f=f*f*(3-2*f);return tex3Dlod(_FlowNoise,float4((cell+f+.5)/32,0)).r;}
   v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.screen=ComputeScreenPos(o.pos);return o;}
   fixed4 frag(v2f i):SV_Target
   {
    float3 ray=normalize(lerp(i.world-_WorldSpaceCameraPos,-UNITY_MATRIX_V[2].xyz,unity_OrthoParams.w));
    float3 back=mul(unity_WorldToObject,float4(i.world,1)).xyz;
    float3 direction=mul((float3x3)unity_WorldToObject,ray);
    float3 safe=lerp(-1,1,step(0,direction))*max(abs(direction),.00001);
    float3 a=(-.5-back)/safe,b=(.5-back)/safe;
    float entry=max(min(a.x,b.x),max(min(a.y,b.y),min(a.z,b.z)));
    float exit=min(max(a.x,b.x),min(max(a.y,b.y),max(a.z,b.z)));
    float raw=SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture,UNITY_PROJ_COORD(i.screen)),ortho=raw;
    #if defined(UNITY_REVERSED_Z)
    ortho=1-raw;
    #endif
    float scene=lerp(LinearEyeDepth(raw),lerp(_ProjectionParams.y,_ProjectionParams.z,ortho),unity_OrthoParams.w);
    float backEye=-mul(UNITY_MATRIX_V,float4(i.world,1)).z;
    float eyeStep=-mul((float3x3)UNITY_MATRIX_V,ray).z;
    exit=min(exit,(scene-backEye)/max(.0001,eyeStep));
    clip(exit-entry);
    float stride=max(0,exit-entry)/_Steps;
    float4 sum=0;
    [loop] for(int s=0;s<10;s++)
    {
     if(s>=_Steps)break;
     float distance=entry+(s+.5)*stride;
     float3 local=back+direction*distance,world=i.world+ray*distance;
     float h=local.y+.5,radial=length(local.xz)*2;
     float shape=(1-smoothstep(.48,.98,radial+h*.18))*smoothstep(0,.08,h)*(1-smoothstep(.7,1,h));
     if(shape<.001)continue;
     float density,grain;float3 color;
     if(_Kind<.5)
     {
     float2 curl=float2(sin(world.z*2.4-_Clock*2.1+h*4),cos(world.x*2.7+_Clock*1.6-h*3))*(.1+h*.22);
     float3 flow=float3((world.xz+curl)*2.5,world.y*2.1-_Clock*2.7);
     grain=noise(flow)*.72+noise(flow*2.03+7.1)*.28;
     density=smoothstep(.27+h*.5,.43+h*.46,grain)*shape;
     // Dense flames do not use the extra gap-noise sample.
     UNITY_BRANCH if(_Sparsity!=0)
      density*=lerp(1,smoothstep(.26,.64,noise(float3(world.xz*.72,0))),_Sparsity);
     float heat=saturate((grain-.38)*1.35+(1-h)*.27);
     color=lerp(float3(1,.09,.004),float3(1,.53,.035),smoothstep(.25,.68,heat));
     color=lerp(color,float3(1,.8,.22),smoothstep(.72,.96,heat));
     }
     else
     {
      float angle=atan2(local.z,local.x)+_Clock*(.65+_Kind*.1)+h*2;
      float3 eddy=float3(cos(angle)*radial*3,h*3-_Clock*.8,sin(angle)*radial*3);
      grain=noise(eddy)*.7+noise(eddy*2.07)*.3;
      density=smoothstep(.24,.7,grain)*shape*(_Kind<1.5?.32:_Kind<2.5?.65:.38);
      color=lerp(_Tint.rgb*.35,_Tint.rgb,grain);
      if(_Kind>2.5&&_Kind<3.5)color+=_Tint.rgb*pow(grain,6)*.4;
     }
     float eye=-mul(UNITY_MATRIX_V,float4(world,1)).z;
     density*=saturate((scene-eye)*10)*step(0,eye)*_Opacity*_Density;
     float alpha=1-exp(-density*stride*3);
     sum.rgb+=(1-sum.a)*color*alpha;sum.a+=(1-sum.a)*alpha;
    }
    // Preserve premultiplied colour while bounding accumulated opacity. Merely
    // reducing per-sample density still becomes opaque across a wide volume.
    sum*=min(1,_AlphaLimit/max(.0001,sum.a));
    return sum;
   }
   ENDCG
  }
 }
}

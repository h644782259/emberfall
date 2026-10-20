Shader "Emberfall/Elementalist Sculpted Volume"
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
    entry=max(entry,(_ProjectionParams.y-backEye)/max(.0001,eyeStep));
    clip(exit-entry);
    float stride=max(0,exit-entry)/_Steps;
    float4 sum=0;
    [loop] for(int s=0;s<16;s++)
    {
     if(s>=_Steps)break;
     float distance=entry+(s+.5)*stride;
     float3 local=back+direction*distance,world=i.world+ray*distance;
     float h=local.y+.5,radial=length(local.xz)*2;
     float angle=atan2(local.z,local.x),spin=angle+_Clock*1.7-h*6;
     float3 flow=float3(cos(spin)*radial*4,h*5-_Clock*2.3,sin(spin)*radial*4);
     float grain=noise(flow)*.7+noise(flow*2.13+8.3)*.3;
     float edge=(1-smoothstep(.78,1,radial))*smoothstep(0,.035,h)*(1-smoothstep(.84,1,h));
     float density=0;float3 color=_Tint.rgb;
     if(_Kind<.5) // Slender ray with an erupting foot and rising broken light.
     {
      float core=exp(-radial*radial*1800);
      float halo=exp(-radial*radial*110)*.11;
      float flareRadius=.12+.6*exp(-h*18);
      float flame=exp(-pow((radial-flareRadius)*9,2));
      float tongues=pow(saturate(grain*1.55-.28),3);
      float foot=flame*tongues*exp(-h*9);
      float sparks=pow(saturate(noise(float3(angle*3,h*24-_Clock*4,radial*9))*1.6-.55),9);
      sparks*=exp(-pow((radial-.22)*7,2));
      density=edge*(core*.95+halo+foot*.75+sparks*.3);
      color=lerp(_Tint.rgb,float3(.88,1,1),saturate(core+foot*.65));
      color*=1+core*2.5+foot*.8;
     }
     else if(_Kind<2.5) // Thick rolling currents; irregular wisps, never wire rings.
     {
      float shell=exp(-pow((radial-(.70+.10*sin(h*5)))*7,2));
      float ribbon=pow(.5+.5*sin(angle*2+h*18-_Clock*4),5);
      float wake=smoothstep(.3,.72,grain)*ribbon;
      density=edge*shell*wake*.32;
      color=lerp(float3(.17,.035,.4),float3(.25,.65,1),smoothstep(.4,.8,grain));
      color+=float3(.5,.2,.8)*pow(grain,5);
     }
     else if(_Kind<3.5) // Continuous tapered flame wake behind the falling rock.
     {
      float taper=1-smoothstep(.15,1,radial+h*.65);
      density=edge*taper*smoothstep(.25+h*.35,.64+h*.3,grain)*1.2;
      float heat=saturate(grain*.8+(1-h)*.5);
      color=lerp(float3(.8,.035,.002),float3(1,.65,.08),heat);
      color=lerp(color,float3(1,.95,.55),smoothstep(.8,1,heat));
     }
     else // Low frost cloud between the crystal clusters.
     {
      density=edge*(1-h)*smoothstep(.3,.7,grain)*.2;
      color=lerp(float3(.12,.35,.6),float3(.6,.9,1),grain);
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

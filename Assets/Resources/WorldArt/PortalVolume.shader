Shader "Emberfall/Deep Travel Portal"
{
    Properties
    {
        _FlowNoise("Shared volume noise",3D)="white"{}
        _Color("Mist tint",Color)=(.25,.63,.53,1)
        _DeepColor("Depth tint",Color)=(.045,.095,.12,1)
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        Cull Front ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local __ EMBER_PORTAL_TEXTURE_NOISE
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;};
            struct v2f {float4 vertex:SV_POSITION;float3 local:TEXCOORD0;float3 camera:TEXCOORD1;UNITY_FOG_COORDS(2)};
            float4 _Color,_DeepColor;
            sampler3D _FlowNoise;
            v2f vert(appdata v)
            {
                v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.local=v.vertex.xyz;o.camera=mul(unity_WorldToObject,float4(_WorldSpaceCameraPos,1)).xyz;UNITY_TRANSFER_FOG(o,o.vertex);return o;
            }
            float hash(float3 p){p=frac(p*.3183099+.13);p*=17;return frac(p.x*p.y*p.z*(p.x+p.y+p.z));}
            float noise(float3 p)
            {
                #if defined(EMBER_PORTAL_TEXTURE_NOISE)
                // Hardware trilinear filtering replaces eight arithmetic hashes.
                // Smooth interpolation and the complete 20-step volume remain.
                float3 cell=floor(p),fraction=frac(p);
                fraction=fraction*fraction*(3-2*fraction);
                return tex3Dlod(_FlowNoise,float4((cell+fraction+.5)/32,0)).r;
                #else
                float3 i=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(lerp(hash(i),hash(i+float3(1,0,0)),f.x),lerp(hash(i+float3(0,1,0)),hash(i+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(i+float3(0,0,1)),hash(i+float3(1,0,1)),f.x),lerp(hash(i+float3(0,1,1)),hash(i+1),f.x),f.y),f.z);
                #endif
            }
            fixed4 frag(v2f i):SV_Target
            {
                float3 ray=normalize(i.local-i.camera),safe=ray+step(abs(ray),.0001)*.0001;
                float3 lo=(float3(-1.56,.035,-.53)-i.camera)/safe,hi=(float3(1.56,4.01,.53)-i.camera)/safe;
                float3 entry=min(lo,hi),leave=max(lo,hi);float begin=max(0,max(entry.x,max(entry.y,entry.z))),end=min(leave.x,min(leave.y,leave.z));
                float stepLength=max(0,end-begin)/20;float alpha=0;float3 accumulated=0;
                [unroll]for(int n=0;n<20;n++)
                {
                    float3 p=i.camera+ray*(begin+(n+.5)*stepLength);
                    float halfWidth=p.y<2.35?1.53:1.53*sqrt(max(.003,1-pow((p.y-2.35)/1.65,2)));
                    float u=p.x/max(.03,halfWidth),height=(p.y-.06)/3.91;
                    float thickness=.11+.38*sqrt(saturate(1-u*u))*sin(saturate(height)*3.14159);
                    float edge=saturate((halfWidth-abs(p.x))*7)*saturate((thickness-abs(p.z))*13)*saturate(height*15)*saturate((1-height)*15);
                    float3 flow=p*2.8+float3(sin(p.y*1.8+_Time.y*.28)*.5,-_Time.y*.32,_Time.y*.19);
                    float detail=noise(flow)+noise(flow*2.17)*.34;
                    float tendril=pow(saturate(detail*.86),3);
                    float density=(.65+tendril*2.1)*edge;
                    float absorb=1-exp(-density*stepLength*3.9);
                    float depth=saturate((p.z+.5));
                    float3 color=lerp(_DeepColor.rgb,_Color.rgb,.28+tendril*.52)+float3(.14,.25,.15)*pow(saturate(detail-.62)*2.1,3);
                    color*=lerp(.70,1.12,depth);accumulated+=(1-alpha)*absorb*color;alpha+=(1-alpha)*absorb;
                }
                fixed4 result=fixed4(accumulated/max(.001,alpha),min(.94,alpha));UNITY_APPLY_FOG(i.fogCoord,result);return result;
            }
            ENDCG
        }
    }
    FallBack Off
}

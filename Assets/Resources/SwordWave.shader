Shader "Emberfall/Travelling Sword Energy"
{
    Properties { _Age("Age",Float)=0 _Opacity("Opacity",Float)=1 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct input {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
            struct output {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float4 screen:TEXCOORD1;float eye:TEXCOORD2;};
            float _Age,_Opacity;
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            output vert(input v)
            {
                output o;
                float freedom=sin(v.uv.x*3.141593)*sin(v.uv.y*3.141593);
                v.vertex.y+=freedom*(.23*sin(v.uv.x*19-v.uv.y*8+_Age*12)+.1*sin(v.uv.x*37+_Age*17));
                v.vertex.z+=freedom*.20*sin(v.uv.x*15+v.uv.y*9-_Age*10);
                v.vertex.y*=.7+.3*smoothstep(0,.2,_Age);
                o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;
                o.screen=ComputeScreenPos(o.vertex);o.eye=-UnityObjectToViewPos(v.vertex).z;return o;
            }
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p)
            {
                float2 b=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(hash(b),hash(b+float2(1,0)),f.x),lerp(hash(b+float2(0,1)),hash(b+1),f.x),f.y);
            }
            fixed4 frag(output i):SV_Target
            {
                float end=pow(saturate(sin(i.uv.x*3.141593)),.4);
                float2 flow=float2(i.uv.x*9,i.uv.y*5-_Age*4);
                float curl=noise(flow+noise(flow*.65+_Age)*1.8);
                float detail=noise(flow*2.4+float2(_Age,-_Age*2));
                float fuel=curl*.7+detail*.3;
                float edge=1-smoothstep(.12,.44,i.uv.y);
                float filament=pow(saturate(1-abs(fuel-.52)*5),3);
                float fade=1-smoothstep(.45,1,i.uv.y+(fuel-.5)*.22);
                float3 wake=lerp(float3(.015,.16,.55),float3(.06,.70,1.35),filament);
                float3 blade=lerp(float3(1.2,.32,.025),float3(1.5,1.1,.38),.3+filament*.7);
                float3 color=lerp(wake,blade,edge);
                float density=lerp(.20+filament*.45,.92,edge);
                float scene=LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture,UNITY_PROJ_COORD(i.screen)));
                return fixed4(color,end*fade*density*_Opacity*saturate((scene-i.eye)/.15));
            }
            ENDCG
        }
    }
}

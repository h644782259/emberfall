Shader "Emberfall/Filled Spell Volume"
{
    Properties
    {
        _Color ("Tint", Color) = (0.25,0.75,1,1)
        _FlameTex ("Natural combustion", 2D) = "white" {}
        _Opacity ("Opacity", Range(0,1)) = 1
        _Progress ("Lifetime", Range(0,1)) = 0
        _Style ("Dissolve", Range(0,1)) = 0
        _Element ("Element family", Float) = 0
        _Seed ("Variation", Float) = 0
        _Sculpted ("Sculpted elemental body", Float) = 0
        _ImpactLight ("Impact pulse", Range(0,1)) = 0
        _EnvelopeMode ("State envelope", Float) = 0
        _EnvelopeAge ("State age", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            #include "AutoLight.cginc"
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float3 normal:TEXCOORD1; float3 local:TEXCOORD2; float3 view:TEXCOORD3; float3 world:TEXCOORD4; float4 screen:TEXCOORD5; float eye:TEXCOORD6; SHADOW_COORDS(7) };
            fixed4 _Color; float _Opacity, _Progress, _Style, _EnvelopeMode, _EnvelopeAge, _Element, _Seed, _ImpactLight, _Sculpted;
            sampler2D _FlameTex;
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            v2f vert(appdata v)
            {v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.normal=UnityObjectToWorldNormal(v.normal);o.local=v.vertex.xyz;o.view=WorldSpaceViewDir(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.screen=ComputeScreenPos(o.pos);o.eye=-UnityObjectToViewPos(v.vertex).z;TRANSFER_SHADOW(o);return o;}
            float hash(float3 p) { p=frac(p*.3183099+float3(.13,.37,.71));p*=17;return frac(p.x*p.y*p.z*(p.x+p.y+p.z)); }
            float noise(float3 p) {
                float3 b=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(lerp(hash(b),hash(b+float3(1,0,0)),f.x),lerp(hash(b+float3(0,1,0)),hash(b+float3(1,1,0)),f.x),f.y),
                    lerp(lerp(hash(b+float3(0,0,1)),hash(b+float3(1,0,1)),f.x),lerp(hash(b+float3(0,1,1)),hash(b+1),f.x),f.y),f.z);
            }
            fixed4 frag(v2f i):SV_Target
            {
                float3 normal=normalize(i.normal),mainDirection=normalize(UnityWorldSpaceLightDir(i.world));
                UNITY_LIGHT_ATTENUATION(attenuation,i,i.world);
                float3 illumination=max(float3(.10,.10,.10),ShadeSH9(float4(normal,1)))+_LightColor0.rgb*saturate(dot(normal,mainDirection))*attenuation;
                float rim=pow(1-saturate(abs(dot(normal,normalize(i.view)))),2);
                float time=_Time.y+_Seed*3.17;
                float3 flow=i.local*5+float3(_Seed,-time*1.7,0);
                float grain=noise(flow)*.65+noise(flow*2.13+7)*.35;
                float fine=noise(i.local*23+_Seed);
                float side=saturate(dot(illumination,float3(.21,.72,.07)));
                float light=.24+.76*side;
                float heat=saturate(grain*.85+rim*.4+(1-saturate(i.uv.y))*.25);
                float3 core=lerp(_Color.rgb,1,.12),outer=_Color.rgb*.32;
                float pattern=grain;
                if(_Element>.5&&_Element<1.5) { // Fire: turbulent bright root, dark cooling tongues.
                    core=float3(1,.48,.045);outer=lerp(float3(.48,.025,.006),_Color.rgb,.2);
                    heat=saturate(heat+.2-saturate(i.uv.y)*.3);
                } else if(_Element<2.5&&_Element>1.5) { // Ice: crisp internal facets and fine cracks.
                    core=float3(.055,.55,1);outer=_Color.rgb*.32;
                    float veins=1-smoothstep(.025,.065,abs(fine-.5));pattern=max(grain*.65,veins);heat=saturate(rim*.8+veins*.6+.18);
                } else if(_Element<3.5&&_Element>2.5) { // Lightning: travelling filaments, white-hot core.
                    float strand=pow(saturate(.5+.5*sin(i.local.y*38+grain*12-time*19)),10);
                    core=lerp(_Color.rgb,float3(.25,.65,1),.22);outer=_Color.rgb*.4;pattern=max(grain,strand);heat=saturate(.35+strand*.65+rim*.3);
                } else if(_Element<4.5&&_Element>3.5) { // Poison: shaded living matter with subdued wet variation.
                    core=float3(.22,.32,.09);outer=lerp(float3(.065,.09,.035),_Color.rgb*.16,.55);pattern=smoothstep(.2,.75,grain);heat=saturate(pattern*.5+rim*.1);
                } else if(_Element<5.5&&_Element>4.5) { // Summoning: opposing currents and violet seams.
                    core=lerp(_Color.rgb,float3(.58,.075,1),.18);outer=lerp(float3(.18,.035,.38),_Color.rgb,.2);
                    pattern=.5+.5*sin(i.local.y*18+grain*9-time*5);heat=saturate(pattern*.55+rim*.6);
                } else { // Steel/arcane: bright cutting edge over a shaded body.
                    heat=saturate(rim*.6+grain*.45+i.uv.y*.3);
                }
                // A bright interior, moving colored layers and a cooling silhouette share one pass.
                float bands=.5+.5*sin(i.uv.y*19+grain*6-time*3.5);
                float highlights=pow(saturate(fine*.65+rim*.45),4);
                float dissolve=saturate((_Progress-.62)*2.63)*_Style;
                float alpha=_Color.a*_Opacity*saturate((pattern+.48-dissolve)*3);
                if(_EnvelopeMode>.5)
                {
                    // The authored cage already leaves face/chest open. Preserve its
                    // silhouette, with steady guard ribs and a soft rising healing band.
                    float ribs=.65+.35*abs(sin(i.uv.x*6.283185));
                    alpha=_Color.a*_Opacity*ribs;
                    float traveling=pow(saturate(1-abs(frac(i.uv.y-_EnvelopeAge*.35)-.5)*2),10);
                    heat=saturate(.18+rim*.5+traveling*.55);
                    light=.4+.6*side;
                    if(_EnvelopeMode>1.5 && _EnvelopeMode<2.5)
                        alpha*=.65+.35*saturate((_EnvelopeAge-i.uv.y*.18)*18+1);
                    if(_EnvelopeMode>2.5 && _EnvelopeMode<3.5)
                        alpha*=.55+.45*pow(.5+.5*sin(i.uv.y*6.283185-_EnvelopeAge*4),3);
                }
                if(_Sculpted>.5 && _Element>1.5 && _Element<2.5)
                    alpha=_Color.a*_Opacity*saturate(1-dissolve); // Stable ice facets, no noisy holes.

                float3 tint=lerp(outer,core,smoothstep(.1,.9,heat))*light;
                float3 accent=(_Element>.5&&_Element<1.5)?float3(1,.22,.025):
                    (_Element>1.5&&_Element<2.5)?float3(.12,.48,1):
                    (_Element>2.5&&_Element<3.5)?float3(.52,.22,1):
                    (_Element>4.5)?float3(.4,.2,1):_Color.rgb;
                tint=lerp(tint,accent*1.15,bands*.18*(1-heat));
                tint+=core*(pow(rim,4)*.12+highlights*.16)*(1-_Progress*.35);
                if(_Sculpted>.5)
                {
                    // Dark backs, saturated midtones and a narrow rim reveal the real mesh thickness.
                    float3 bodyTint=_Color.rgb*illumination*.82;
                    float edge=pow(rim,4);
                    tint=lerp(bodyTint,tint,_Element>1.5&&_Element<2.5?.28:.58);
                    tint+=core*edge*.12;
                }
                // Saturated release, shaded midtones and a narrow travelling light edge.
                // Reuses the existing pass; no extra lights, bloom requirement or draw calls.
                float ribbon=pow(saturate(1-abs(frac(i.uv.y*2.0-_Progress*1.8+_Seed)-.5)*2),16);
                float edgeLight=pow(rim,5)*.14+ribbon*.12;
                if(_EnvelopeMode<.5)tint+=core*(edgeLight+_ImpactLight*.12);
                // White is a tiny, brief contact glint; colored body owns the silhouette.
                tint+=float3(1,.95,.85)*pow(rim,10)*pow(_ImpactLight,3)*.22;
                if(_Element>.5&&_Element<1.5)
                {
                    float2 flameUV=float2(frac(i.uv.x+.045*sin(time*2+i.local.y*3)),saturate(i.uv.y*.92+.035*sin(time*3)));
                    float4 combustion=tex2D(_FlameTex,flameUV);
                    tint=combustion.rgb*float3(.94,.64,.22)*(.74+.16*side);
                    alpha=_Color.a*_Opacity*smoothstep(.055,.55,combustion.a)*smoothstep(.16,.51,grain)*(.72+.28*abs(dot(normal,normalize(i.view))));
                }
                float raw=SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture,UNITY_PROJ_COORD(i.screen));
                float orthographicDepth=raw;
                #if defined(UNITY_REVERSED_Z)
                orthographicDepth=1-raw;
                #endif
                float scene=lerp(LinearEyeDepth(raw),lerp(_ProjectionParams.y,_ProjectionParams.z,orthographicDepth),unity_OrthoParams.w);
                // Real geometry enters the floor smoothly without losing the
                // above-ground tongues, ice facets or contracted actor envelope.
                alpha*=saturate((scene-i.eye)/(_Element>.5&&_Element<1.5?.10:.045));
                return fixed4(tint,saturate(alpha));
            }
            ENDCG
        }
    }
}

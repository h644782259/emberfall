Shader "Emberfall/Weathered World"
{
    Properties
    {
        _Color("Tint",Color)=(1,1,1,1)
        _MainTex("Material grain",2D)="gray"{}
        _Glossiness("Smoothness",Range(0,1))=.2
        _Metallic("Metallic",Range(0,1))=0
        _EmissionColor("Emission",Color)=(0,0,0,0)
        _GrainScale("Grain per meter",Float)=.45
        _GrainStrength("Grain contrast",Float)=.4
        [HideInInspector] _ProjectionFloor("Projection contribution floor (-1 uses platform default)",Float)=-1
        _Wind("Leaf breeze",Float)=0
        _Cull("Face culling",Float)=2
        _ColorTexture("Colored ground detail",Float)=0
        _AtlasRegion("Texture region",Vector)=(1,1,0,0)
        _Relief("Surface micro relief",Range(0,.06))=.012
        _Weather("Stone weathering",Float)=0
        _VertexTint("Use vegetation color and wind weights",Float)=0
        _TerrainFollow("Follow authored terrain",Float)=0
        _OcclusionFade("Camera occlusion coverage",Range(0,1))=1
        _GroundTex("Trail edge meadow",2D)="gray"{}
        _TrailEdge("Blend road shoulders",Float)=0
        _PavingBlend("Buried paving perimeter",Float)=0
        _RugSurface("Worn woven ground cloth",Float)=0
        _WaterSurface("Animated water micro waves",Float)=0
        _LeafSurface("Individual leaf UV detail",Float)=0
    }
    SubShader
    {
        Tags {"RenderType"="Opaque"}
        Cull [_Cull]
        LOD 250
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 3.0
        #pragma multi_compile __ EMBER_TERRAIN_LEGACY
        #pragma multi_compile __ EMBER_WORLD_LEGACY_CLIP
        #pragma multi_compile_local __ EMBER_WORLD_FADE
        #pragma multi_compile_local __ EMBER_MEADOW_SIMPLE
        #pragma multi_compile __ EMBER_WORLD_SINGLE_GRAIN_REVIEW
        #include "UnityCG.cginc"
        #include "TerrainField.cginc"
        sampler2D _MainTex,_GroundTex;
        fixed4 _Color,_EmissionColor;
        half _Glossiness,_Metallic;
        float _ProjectionFloor,_GrainScale,_GrainStrength,_Wind,_ColorTexture,_Relief,_Weather,_VertexTint,_TrailEdge,_WaterSurface,_LeafSurface,_TerrainFollow,_OcclusionFade,_PavingBlend,_RugSurface;
        float4 _AtlasRegion;
        float _EmberPreparedTerrain;
        struct Input {float4 screenPos;float3 worldPos;float3 worldNormal;float2 uv_MainTex;float4 color:COLOR;INTERNAL_DATA};
        fixed3 Tile(float2 uv)
        {
            // Mirrored repetition is continuous at tile boundaries. Derivatives
            // are measured before folding so distant surfaces keep stable mips.
            float2 folded=1-abs(frac(uv*.5)*2-1);
            float2 scale=_AtlasRegion.xy*.92;
            float2 at=_AtlasRegion.zw+_AtlasRegion.xy*.04+folded*scale;
            return tex2Dgrad(_MainTex,at,ddx(uv)*scale,ddy(uv)*scale).rgb;
        }
        fixed3 Grain(float3 p,float3 w)
        {
            // Flat ground and axis-aligned props have zero-weight projections.
            // Do not fetch those textures only to multiply the samples by zero.
            fixed3 value=0;
            UNITY_BRANCH if(w.x>0)value+=Tile(p.yz)*w.x;
            UNITY_BRANCH if(w.y>0)value+=Tile(p.xz)*w.y;
            UNITY_BRANCH if(w.z>0)value+=Tile(p.xy)*w.z;
            return value;
        }
        void vert(inout appdata_full v)
        {
            float3 p=mul(unity_ObjectToWorld,v.vertex).xyz;
            float h=0,dx=0,dz=0;
            UNITY_BRANCH if(_TerrainFollow!=0)
            {
#if defined(EMBER_TERRAIN_LEGACY)
                h=EmberHeight(p.xz)*_TerrainFollow;
                dx=(EmberHeight(p.xz+float2(.02,0))-EmberHeight(p.xz-float2(.02,0)))/.04*_TerrainFollow;
                dz=(EmberHeight(p.xz+float2(0,.02))-EmberHeight(p.xz-float2(0,.02)))/.04*_TerrainFollow;
#else
                float3 terrain;
                // Missing UV streams can supply w=1; only our explicit marker is valid.
                UNITY_BRANCH if(_EmberPreparedTerrain>.5 && v.texcoord2.w == -12345.0) terrain=v.texcoord2.xyz*_TerrainFollow;
                else terrain=EmberHeightGradient(p.xz)*_TerrainFollow;
                h=terrain.x;dx=terrain.y;dz=terrain.z;
#endif
            }
            float3 n=UnityObjectToWorldNormal(v.normal);
            n=normalize(float3(n.x-dx*n.y,n.y,n.z-dz*n.y));
            p.y+=h;
            float sway=_Wind*lerp(1,v.color.a,_VertexTint);
            UNITY_BRANCH if(sway!=0)
            {
                p.x+=sway*sin(_Time.y*1.3+p.z*.8+p.x*.5)*.035;
                p.z+=sway*cos(_Time.y*.9+p.x*.7)*.02;
            }
            v.vertex=mul(unity_WorldToObject,float4(p,1));
            v.normal=normalize(mul(n,(float3x3)unity_ObjectToWorld));
            // World meshes include ribbons without UV tangents; supply a stable frame.
            float3 reference=abs(v.normal.y)>.9?float3(0,0,1):float3(0,1,0);
            v.tangent=float4(normalize(cross(reference,v.normal)),1);
        }
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float3 normal=normalize(WorldNormalVector(IN,float3(0,0,1)));
            float3 p=IN.worldPos*_GrainScale;
            fixed3 detail;
            float grain;
            #if defined(EMBER_MEADOW_SIMPLE)
            // Mobile meadow: terrain texture, one projection,
            // no secondary micro-grain or per-pixel relief reconstruction.
            detail=Tile(p.xz);
            grain=dot(detail,float3(.299,.587,.114));
            #else
            // Subtract a small uniform floor before renormalizing: negligible
            // projections reach zero continuously, allowing Grain to skip them.
            #if defined(SHADER_API_MOBILE)
            float projectionFloor=_ProjectionFloor<0?.003:_ProjectionFloor;
            #else
            float projectionFloor=max(_ProjectionFloor,0);
            #endif
            float3 w=max(pow(abs(normal),4)-projectionFloor,0);w/=max(dot(w,float3(1,1,1)),.001);
            UNITY_BRANCH if(_LeafSurface>.5)
            {
                detail=tex2D(_MainTex,IN.uv_MainTex).rgb;
                grain=dot(detail,float3(.299,.587,.114));
            }
            else
            {
                detail=Grain(p,w);
                #if defined(EMBER_WORLD_SINGLE_GRAIN_REVIEW)
                // Explicit profiling candidate; ordinary launches retain both frequencies.
                grain=dot(detail,float3(.299,.587,.114));
                #else
                // Preserve the secondary grain frequency without evaluating it on leaves.
                grain=dot(lerp(detail,Grain(p*3.17+float3(.31,.17,.43),w),.22),float3(.299,.587,.114));
                #endif
            }
            #endif
            o.Albedo=lerp(_Color.rgb*lerp(1, .55+grain*1.1,_GrainStrength),detail*1.15,_ColorTexture);
            if(_LeafSurface>.5)o.Albedo=_Color.rgb*(.65+detail*1.2);
            float macro=.95+.05*sin(IN.worldPos.x*.57+sin(IN.worldPos.z*.41))*sin(IN.worldPos.z*.63);
            float patina=saturate(normal.y)*_Weather*(.035+.025*sin(IN.worldPos.x*1.7+IN.worldPos.z*.93));
            o.Albedo=lerp(o.Albedo*macro,o.Albedo*float3(.75,.93,.72),patina);
            o.Albedo*=lerp(float3(1,1,1),IN.color.rgb,_VertexTint);
            if(_TrailEdge>.5)
            {
                float edge=smoothstep(.01,.20,min(IN.uv_MainTex.x,1-IN.uv_MainTex.x)+(grain-.4)*.08);
                fixed3 meadow=tex2D(_GroundTex,IN.worldPos.xz*.4).rgb*1.15;
                o.Albedo=lerp(meadow,o.Albedo,edge);
            }
            if(_PavingBlend>.5)
            {
                float edge=smoothstep(.025,.93,IN.color.a+(grain-.5)*.13);
                fixed3 meadow=tex2D(_GroundTex,IN.worldPos.xz*.4).rgb*1.15;
                // Low stone islands show weathered material; the buried edge
                // uses the exact terrain sample and retains real depth/light.
                float moss=saturate((.48-grain)*1.8)*(1-edge*.7);
                o.Albedo=lerp(meadow,lerp(o.Albedo,meadow,moss*.32),edge);
            }
            if(_RugSurface>.5)
            {
                float2 cloth=IN.uv_MainTex;
                float wornBorder=smoothstep(.04,.10,min(min(cloth.x,1-cloth.x),min(cloth.y,1-cloth.y)));
                float thread=.5+.5*sin(IN.worldPos.x*34)*sin(IN.worldPos.z*39);
                float faded=.87+.13*sin(IN.worldPos.x*1.8+sin(IN.worldPos.z*2.4));
                fixed3 fiber=_Color.rgb*(.65+grain*.65)*(faded+thread*.08);
                fiber=lerp(fiber*float3(1.13,1.07,.94),fiber,wornBorder);
                float stain=saturate(sin(IN.worldPos.x*2.9)*sin(IN.worldPos.z*3.7)-.2)*.21;
                fiber=lerp(fiber,fiber*float3(.63,.57,.46),stain);
                // Frayed, earth-stained cloth edges soften the silhouette while
                // the closed thin fabric retains its actual contact thickness.
                float hem=smoothstep(.02,.68,IN.color.a+(grain-.5)*.22);
                fixed3 dirtyHem=fiber*float3(.71,.62,.46);
                o.Albedo=lerp(dirtyHem,fiber,hem);
            }
            #if defined(EMBER_MEADOW_SIMPLE)
            o.Normal=half3(0,0,1);
            #else
            // Surface gradient uses pixel derivatives: no extra normal-map texture reads.
            float3 dpdx=ddx(IN.worldPos),dpdy=ddy(IN.worldPos);
            float3 r1=cross(dpdy,normal),r2=cross(normal,dpdx);
            float det=dot(dpdx,r1);
            float3 grad=(r1*ddx(grain)+r2*ddy(grain))*_Relief/(abs(det)>.000001?det:1);
            grad=clamp(grad,-.45,.45);
            float3 bumped=normalize(normal-grad);
            if(_WaterSurface>.5)
            {
                float2 q=IN.worldPos.xz;
                float waveA=dot(q,float2(2.1,1.4))-_Time.y*1.2;
                float waveB=dot(q,float2(-1.3,3.1))+_Time.y*.83;
                float2 ripples=cos(waveA)*float2(2.1,1.4)*.025+cos(waveB)*float2(-1.3,3.1)*.012;
                bumped=normalize(normal-float3(ripples.x,0,ripples.y));
                float eddy=sin(dot(q,float2(.83,-.59))+sin(waveA*.42))*sin(waveB*.51);
                float glint=pow(saturate(sin(waveA+eddy)*sin(waveB)),9)*.075;
                o.Albedo=o.Albedo*(.96+.04*eddy)+glint*float3(.42,.57,.55);
            }
            o.Normal=normalize(float3(dot(bumped,WorldNormalVector(IN,float3(1,0,0))),dot(bumped,WorldNormalVector(IN,float3(0,1,0))),dot(bumped,normal)));
            #endif
            o.Metallic=_Metallic;
            o.Smoothness=_Glossiness*lerp(.7,1.05,grain);
            o.Occlusion=lerp(.85,1,grain);
            o.Emission=_EmissionColor.rgb;
            o.Alpha=_Color.a;
            // Opaque terrain props retain lighting and depth while letting the
            // protected actor show through a stable, fine screen-space mask.
            #if defined(EMBER_WORLD_FADE) || defined(EMBER_WORLD_LEGACY_CLIP)
            if(_OcclusionFade<.999)
            {
                float2 pixel=floor(IN.screenPos.xy/max(IN.screenPos.w,.0001)*_ScreenParams.xy);
                float coverage=frac(52.9829189*frac(dot(pixel,float2(.06711056,.00583715))));
                clip(_OcclusionFade-coverage);
            }
            #endif
        }
        ENDCG
    }
    Fallback "Standard"
}

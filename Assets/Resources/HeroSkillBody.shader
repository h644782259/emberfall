Shader "Emberfall/Physical Hero Skill Body"
{
 Properties { [MainColor] _Tint("Material tint",Color)=(.4,.5,.6,1) [MainTexture] _BodyTex("Surface texture",2D)="white" {} _MaterialAtlas("Natural surface atlas",2D)="white" {} _Surface("Physical matter",Float)=0 _Opacity("Lifetime coverage",Range(0,1))=1 _Phase("Time",Float)=0 _Heat("Contact heat",Range(0,1))=0 _CoreLight("Contained magic light",Range(0,1))=0 _Engraving("Forged gold channels",Range(0,1))=0 }
 SubShader
 {
  Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
  ZWrite On ZTest LEqual Cull Back
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
   struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;};
   struct v2f {float4 pos:SV_POSITION;float3 materialPos:TEXCOORD0;float3 normal:TEXCOORD1;float3 view:TEXCOORD2;float3 localNormal:TEXCOORD3;float3 worldPos:TEXCOORD4;SHADOW_COORDS(5)};
   sampler2D _BodyTex,_MaterialAtlas;float4 _Tint;float _Surface,_Opacity,_Phase,_Heat,_CoreLight,_Engraving;
   float hash(float3 p){p=frac(p*.3183+.17);p*=19;return frac(p.x*p.y*p.z*(p.x+p.y+p.z));}
   float noise(float3 p){float3 b=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(lerp(hash(b),hash(b+float3(1,0,0)),f.x),lerp(hash(b+float3(0,1,0)),hash(b+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(b+float3(0,0,1)),hash(b+float3(1,0,1)),f.x),lerp(hash(b+float3(0,1,1)),hash(b+1),f.x),f.y),f.z);}
   float4 matterRect()
   {
    // The 1254px source changes rows at pixel 627: four columns, two equal rows.
    if(_Surface<.5)return float4(.25,.5,.25,.5);
    if(_Surface<1.5)return float4(0,.5,.25,.5);
    if(_Surface<2.5)return float4(.75,.5,.25,.5);
    if(_Surface<3.5)return float4(.75,0,.25,.5);
    if(_Surface<4.5)return float4(.5,0,.25,.5);
    if(_Surface<5.5)return float4(.5,.5,.25,.5);
    if(_Surface<6.5)return float4(0,0,.25,.5);
    return float4(.25,0,.25,.5);
   }
   float3 sampleMatter(float2 uv,float4 rect)
   {
    // Mirrored samples join continuously. Derivatives come from unwrapped
    // coordinates so a repeat seam cannot pull a pale neighboring atlas tile
    // into a coarse mip and draw a false grid across wet ground.
    float2 mirrored=1-abs(frac(uv*.5)*2-1);
    float2 at=rect.xy+(.045+mirrored*.91)*rect.zw;
    return tex2Dgrad(_MaterialAtlas,at,ddx(uv)*rect.zw*.91,ddy(uv)*rect.zw*.91).rgb;
   }
   v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.materialPos=v.vertex.xyz;o.normal=UnityObjectToWorldNormal(v.normal);o.localNormal=v.normal;o.view=WorldSpaceViewDir(v.vertex);o.worldPos=mul(unity_ObjectToWorld,v.vertex).xyz;TRANSFER_SHADOW(o);return o;}
   fixed4 frag(v2f i):SV_Target
   {
    clip(_Opacity-hash(float3(floor(i.pos.xy),0)));
    float3 n=normalize(i.normal),view=normalize(i.view),light=normalize(UnityWorldSpaceLightDir(i.worldPos));
    float attenuation=SHADOW_ATTENUATION(i),rim=pow(1-saturate(abs(dot(n,view))),5);
    float3 diffuse=max(float3(.15,.15,.15),ShadeSH9(float4(n,1)))+_LightColor0.rgb*saturate(dot(n,light))*attenuation;
    float grain=noise(i.materialPos*13),fine=noise(i.materialPos*61);
    float3 weights=pow(abs(normalize(i.localNormal)),4);weights/=max(.001,weights.x+weights.y+weights.z);
    float4 rect=matterRect();float scale=_Surface>4.5&&_Surface<5.5?.42:_Surface>3.5&&_Surface<4.5?1.3:1.05;
    float3 textureSample=sampleMatter(i.materialPos.yz*scale,rect)*weights.x+sampleMatter(i.materialPos.xz*scale,rect)*weights.y+sampleMatter(i.materialPos.xy*scale,rect)*weights.z;
    float3 albedo,color;float gloss=20,specular=.12;
    float luminance=dot(textureSample,float3(.21,.72,.07));
    if(_Surface<.5){albedo=(.16+textureSample*.72)*lerp(float3(.75,.72,.68),_Tint.rgb*1.4,.35);}
    else if(_Surface<1.5){albedo=textureSample*lerp(float3(.9,.93,.98),_Tint.rgb*1.7,.28);gloss=64;specular=.42;}
    else if(_Surface<2.5){albedo=textureSample*lerp(float3(.72,.9,1),_Tint.rgb*1.3,.25);gloss=92;specular=.55;}
    else if(_Surface<3.5){albedo=textureSample*lerp(float3(.8,.9,.65),_Tint.rgb*1.4,.22);gloss=10;}
    else if(_Surface<4.5){albedo=textureSample*lerp(float3(.78,.82,.85),_Tint.rgb*1.7,.26);gloss=18;}
    else if(_Surface<5.5){albedo=textureSample*lerp(float3(.8,.9,.68),_Tint.rgb*1.7,.18);gloss=78;specular=.4;}
    else {albedo=textureSample*lerp(float3(.8,.8,.75),_Tint.rgb*1.5,.24);gloss=12;}
    diffuse*=.90+.1*luminance;
    color=albedo*diffuse;
    color+=pow(saturate(dot(n,normalize(view+light))),gloss)*_LightColor0.rgb*specular*attenuation;
    color+=_Tint.rgb*rim*.05;
    if(_Surface<.5){float hotSeam=saturate((textureSample.r-textureSample.g)*6);color+=float3(1,.22,.025)*hotSeam*_Heat*.9;}
    else color+=_Tint.rgb*_Heat*.045;
    // Light remains inside the textured solid, preserving facet shadows and
    // saturated material color instead of washing it out with a white halo.
    color+=_Tint.rgb*_CoreLight*(.55+grain*.45);
    if(_Surface>.5&&_Surface<1.5&&_Engraving>0)
    {
     // Gold inlay travels along cut channels while the blue steel retains its
     // dark sides, rough grain and directional specular highlights.
     float spine=1-smoothstep(.018,.055,abs(i.materialPos.x));
     float chevron=1-smoothstep(.04,.085,abs(frac(i.materialPos.y*3+abs(i.materialPos.x)*2)-.5));
     float channels=max(spine*.7,chevron*.38)*_Engraving;
     float flow=.45+.55*pow(saturate(sin(i.materialPos.y*4-_Phase*4)),3);
     color=lerp(color,color*.65+float3(.7,.4,.065)*diffuse,channels*.55);
     color+=float3(1,.52,.09)*channels*(_CoreLight+_Heat*.12)*flow;
    }
    return fixed4(color,1);
   }
   ENDCG
  }
  Pass
  {
   Tags { "LightMode"="ShadowCaster" }
   CGPROGRAM
   #pragma vertex shadowVert
   #pragma fragment shadowFrag
   #pragma target 3.0
   #pragma multi_compile_shadowcaster
   #include "UnityCG.cginc"
   struct shadowVaryings { V2F_SHADOW_CASTER; float3 world:TEXCOORD1; };
   float _Opacity;
   shadowVaryings shadowVert(appdata_base v)
   {
    shadowVaryings o; o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
    TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
    return o;
   }
   float shadowHash(float3 p){p=frac(p*.3183+.17);p*=19;return frac(p.x*p.y*p.z*(p.x+p.y+p.z));}
   float4 shadowFrag(shadowVaryings i):SV_Target
   {
    // Solid matter and its contact shadow retire together during the final fade.
    clip(_Opacity-shadowHash(floor(i.world*97)));
    SHADOW_CASTER_FRAGMENT(i)
   }
   ENDCG
  }
 }
}

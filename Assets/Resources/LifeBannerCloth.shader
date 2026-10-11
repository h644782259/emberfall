Shader "Emberfall/Life Banner Woven Heraldry"
{
 Properties {
  [MainColor] _Color("Forest cloth tint",Color)=(.18,.37,.20,1)
  _MainTex("Material atlas",2D)="white"{}
  _Region("Tile region",Vector)=(.48,.48,.01,.51)
  _PatternScale("Texture repeat",Float)=2
  _TextureStrength("Texture strength",Range(0,1))=.65
  _Relief("Woven relief",Range(0,4))=1
  _Glossiness("Smoothness",Range(0,1))=.1
  _Metallic("Metallic",Range(0,1))=0
  _EmissionColor("Healing response",Color)=(0,0,0,0)
  _Pulse("Real healing beat",Range(0,1))=0
  _Opacity("Lifetime coverage",Range(0,1))=1
 }
 SubShader {
  Tags {"RenderType"="TransparentCutout" "Queue"="AlphaTest"} LOD 200
  Cull Back ZWrite On
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows addshadow
  #pragma target 3.0
  sampler2D _MainTex;fixed4 _Color,_EmissionColor;float4 _Region;
  half _PatternScale,_TextureStrength,_Relief,_Glossiness,_Metallic,_Pulse,_Opacity;
  struct Input {float2 uv_MainTex;float3 worldPos;};
  float coverageHash(float3 p){p=frac(p*.3183+.17);p*=19;return frac(p.x*p.y*p.z*(p.x+p.y+p.z));}
  fixed3 weave(float2 p) {
   float2 mirrored=1-abs(frac(p*.5)*2-1);
   float2 at=_Region.zw+_Region.xy*(.04+mirrored*.92);
   return tex2Dgrad(_MainTex,at,ddx(p)*_Region.xy*.92,ddy(p)*_Region.xy*.92).rgb;
  }
  void surf(Input IN,inout SurfaceOutputStandard o) {
   clip(_Opacity-coverageHash(floor(IN.worldPos*97)));
   float2 uv=IN.uv_MainTex,p=uv*_PatternScale;
   fixed3 grain=weave(p);
   // Embroidered border and a lance/leaf crest are carried by the flexible
   // closed cloth UVs, so they bend and remain readable on both real skins.
   float edge=min(min(uv.x,1-uv.x),min(uv.y,1-uv.y));
   float border=(1-smoothstep(.045,.065,edge))*smoothstep(.014,.025,edge);
   float2 q=(uv-float2(.48,.5))*float2(1,1.4);
   float diamond=abs(q.x)*1.65+abs(q.y);
   float crest=(1-smoothstep(.215,.24,diamond))*smoothstep(.17,.19,diamond);
   float lance=(1-smoothstep(.018,.033,abs(q.x)))*(1-smoothstep(.29,.33,abs(q.y)));
   float leaves=(1-smoothstep(.014,.034,abs(abs(q.x)-(.15-abs(q.y)*.32))))*(1-smoothstep(.18,.24,abs(q.y)));
   float thread=saturate(max(border,max(crest,max(lance,leaves))));
   fixed3 forest=_Color.rgb*lerp(fixed3(1,1,1),grain*1.5,_TextureStrength);
   fixed3 gold=fixed3(.79,.52,.11)*(.7+dot(grain,fixed3(.3,.59,.11))*.5);
   o.Albedo=lerp(forest,gold,thread);
   float2 dx=float2(.0014,0),dy=float2(0,.0014);
   half x=dot(weave(p-dx)-weave(p+dx),half3(.3,.59,.11));
   half y=dot(weave(p-dy)-weave(p+dy),half3(.3,.59,.11));
   o.Normal=normalize(half3(x*_Relief,y*_Relief,1));
   o.Metallic=thread*.25;o.Smoothness=lerp(_Glossiness,.31,thread);
   o.Emission=_EmissionColor.rgb+fixed3(.9,.54,.1)*thread*_Pulse*.48;
   o.Alpha=1;
  }
  ENDCG
 }
 Fallback "Standard"
}

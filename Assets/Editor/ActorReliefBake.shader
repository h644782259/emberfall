Shader "Hidden/Emberfall/Actor Relief Bake"
{
 SubShader { Pass {
 ZTest Always Cull Off ZWrite Off
 CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;
 float4 frag(v2f_img i) : SV_Target
 {
     // ActorGrain offsets .0014 in pattern space; all atlas regions are .48 wide.
     float2 d=float2(.0014*.48*.92,0);
     float x=dot(tex2Dlod(_MainTex,float4(i.uv-d,0,0)).rgb-tex2Dlod(_MainTex,float4(i.uv+d,0,0)).rgb,float3(.3,.59,.11));
     float y=dot(tex2Dlod(_MainTex,float4(i.uv-d.yx,0,0)).rgb-tex2Dlod(_MainTex,float4(i.uv+d.yx,0,0)).rgb,float3(.3,.59,.11));
     return float4(float2(x,y)*.5+(128.0/255.0),0,1);
 }
 ENDCG
 } }
}

Shader "Emberfall/ThreatBoundary"
{
    SubShader
    {
        Tags { "Queue"="Overlay-100" "RenderType"="Transparent" "IgnoreProjector"="True" }
        // Safety-critical outlines remain visible above overlapping floor particles.
        // This material is used only on thin, controller-owned threat boundaries.
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input { float4 vertex : POSITION; fixed4 color : COLOR; };
            struct Output { float4 vertex : SV_POSITION; fixed4 color : COLOR; };
            Output vert(Input input) { Output result; result.vertex=UnityObjectToClipPos(input.vertex); result.color=input.color; return result; }
            fixed4 frag(Output input) : SV_Target { return input.color; }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}

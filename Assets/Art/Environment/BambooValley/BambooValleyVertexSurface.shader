Shader "Wuxia Roguelite/Bamboo Valley Vertex Surface"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _Smoothness ("Smoothness", Range(0,1)) = 0.25
        _Emission ("Emission", Range(0,5)) = 0
        [HideInInspector] _Foliage ("Legacy foliage flag", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull [_Cull]
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0
        fixed4 _Color;
        half _Smoothness;
        half _Emission;
        #include "Assets/Scripts/Camera/ForegroundOcclusion.cginc"
        struct Input { float4 color : COLOR; float3 worldPos; float4 screenPos; };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            ApplyForegroundOcclusion(IN.worldPos, IN.screenPos);
            fixed3 c = IN.color.rgb * _Color.rgb;
            o.Albedo = c;
            o.Emission = c * _Emission;
            o.Smoothness = _Smoothness;
            o.Metallic = 0;
            o.Alpha = 1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}

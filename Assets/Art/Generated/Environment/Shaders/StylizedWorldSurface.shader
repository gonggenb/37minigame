Shader "Wuxia Roguelite/Stylized World Surface"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _MainTex ("Albedo", 2D) = "white" {}
        _WorldTiling ("World Tiling", Float) = 0.18
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 150

        CGPROGRAM
        #pragma surface surf Lambert noforwardadd

        sampler2D _MainTex;
        fixed4 _Color;
        half _WorldTiling;

        struct Input
        {
            float3 worldPos;
        };

        void surf(Input input, inout SurfaceOutput output)
        {
            fixed4 albedo = tex2D(_MainTex, input.worldPos.xz * _WorldTiling) * _Color;
            output.Albedo = albedo.rgb;
            output.Alpha = 1;
        }
        ENDCG
    }

    Fallback "Diffuse"
}

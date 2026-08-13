Shader "Wuxia Roguelite/HD2D Water Surface"
{
    Properties
    {
        _Color ("Tint", Color) = (0.82, 0.9, 0.88, 0.86)
        _MainTex ("Water Albedo", 2D) = "white" {}
        _WorldTiling ("World Tiling", Float) = 0.14
        _FlowSpeed ("Flow Speed", Float) = 0.025
        _Alpha ("Alpha", Range(0, 1)) = 0.88
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-20" }
        LOD 150
        ZWrite Off

        CGPROGRAM
        #pragma surface surf Lambert alpha:fade noforwardadd

        sampler2D _MainTex;
        fixed4 _Color;
        half _WorldTiling;
        half _FlowSpeed;
        half _Alpha;

        struct Input
        {
            float3 worldPos;
        };

        void surf(Input input, inout SurfaceOutput output)
        {
            float2 baseUv = input.worldPos.xz * _WorldTiling;
            fixed3 layerA = tex2D(_MainTex, baseUv + float2(_Time.y * _FlowSpeed, 0)).rgb;
            fixed3 layerB = tex2D(_MainTex, baseUv * 1.7 + float2(-_Time.y * _FlowSpeed * 0.45, _Time.y * _FlowSpeed * 0.2)).rgb;
            fixed3 albedo = lerp(layerA, layerB, 0.28) * _Color.rgb;
            output.Albedo = albedo;
            output.Emission = albedo * 0.03;
            output.Alpha = _Alpha * _Color.a;
        }
        ENDCG
    }

    Fallback "Transparent/Diffuse"
}

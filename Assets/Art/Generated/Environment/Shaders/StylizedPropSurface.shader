Shader "Wuxia Roguelite/Stylized Prop Surface"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _MainTex ("Albedo", 2D) = "white" {}
        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Strength", Range(0, 2)) = 0.6
        _Smoothness ("Smoothness", Range(0, 1)) = 0.18
        _Saturation ("Saturation", Range(0, 1)) = 0.78
        _Contrast ("Contrast", Range(0.5, 1.5)) = 0.9
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 150

        CGPROGRAM
        #pragma surface surf Lambert fullforwardshadows noforwardadd addshadow

        sampler2D _MainTex;
        sampler2D _BumpMap;
        fixed4 _Color;
        half _BumpScale;
        half _Smoothness;
        half _Saturation;
        half _Contrast;

        struct Input
        {
            float2 uv_MainTex;
            float4 color : COLOR;
        };

        void surf(Input input, inout SurfaceOutput output)
        {
            fixed3 color = tex2D(_MainTex, input.uv_MainTex).rgb * _Color.rgb * input.color.rgb;
            half luminance = dot(color, half3(0.299h, 0.587h, 0.114h));
            color = lerp(luminance.xxx, color, _Saturation);
            color = (color - 0.5h) * _Contrast + 0.5h;
            output.Albedo = saturate(color);
            fixed3 normal = UnpackNormal(tex2D(_BumpMap, input.uv_MainTex));
            normal.xy *= _BumpScale;
            output.Normal = normalize(normal);
            output.Gloss = _Smoothness;
            output.Alpha = 1;
        }
        ENDCG
    }

    Fallback "Diffuse"
}

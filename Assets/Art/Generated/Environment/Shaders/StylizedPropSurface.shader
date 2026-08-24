Shader "Wuxia Roguelite/Stylized Prop Surface"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _MainTex ("Albedo", 2D) = "white" {}
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
        fixed4 _Color;
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
            output.Alpha = 1;
        }
        ENDCG
    }

    Fallback "Diffuse"
}

Shader "Hidden/Wuxia/TiltShift"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _BlurTex ("Blurred scenery", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex;
        sampler2D _BlurTex;
        float4 _MainTex_TexelSize;
        float4 _BlurDirection;
        float4 _Focus;

        half4 Blur(v2f_img i) : SV_Target
        {
            float2 stepUV = _BlurDirection.xy;
            half4 color = tex2D(_MainTex, i.uv) * 0.227027;
            color += tex2D(_MainTex, i.uv + stepUV * 1.384615) * 0.316216;
            color += tex2D(_MainTex, i.uv - stepUV * 1.384615) * 0.316216;
            color += tex2D(_MainTex, i.uv + stepUV * 3.230769) * 0.0702705;
            color += tex2D(_MainTex, i.uv - stepUV * 3.230769) * 0.0702705;
            return color;
        }

        half4 Composite(v2f_img i) : SV_Target
        {
            float2 blurUV = i.uv;
            #if UNITY_UV_STARTS_AT_TOP
            if (_MainTex_TexelSize.y < 0) blurUV.y = 1 - blurUV.y;
            #endif
            half4 sharp = tex2D(_MainTex, i.uv);
            float distanceFromFocus = abs(blurUV.y - _Focus.x);
            float mask = smoothstep(_Focus.y, _Focus.y + _Focus.z, distanceFromFocus) * _Focus.w;
            half3 blurred = tex2D(_BlurTex, blurUV).rgb;
            return half4(lerp(sharp.rgb, blurred, mask), sharp.a);
        }
        ENDCG
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Blur
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Composite
            ENDCG
        }
    }
    Fallback Off
}

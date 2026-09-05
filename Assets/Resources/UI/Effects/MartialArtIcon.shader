Shader "Wuxia/UI/MartialArtIcon"
{
    Properties
    {
        _MainTex ("Icon", 2D) = "white" {}
        _PrimaryColor ("School", Color) = (1,1,1,1)
        _SecondaryColor ("Secret second school", Color) = (1,1,1,1)
        _Schools ("School motifs", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _PrimaryColor, _SecondaryColor, _Schools;

            float stroke(float distance, float width)
            {
                return 1.0 - smoothstep(width, width + 0.012, abs(distance));
            }

            // Distinct silhouettes survive small sizes and grayscale: slash, mist, shield, trails, crescent.
            float motif(float2 p, float school)
            {
                if (school < 0.5)
                {
                    float slash = stroke(p.x - p.y * 0.76, 0.015);
                    float trail = stroke(p.x - p.y * 0.76 + 0.17, 0.006) * 0.5;
                    return (slash + trail) * saturate(1.0 - abs(p.y) / 0.43);
                }
                if (school < 1.5)
                {
                    float mist = stroke(length(p - float2(-0.15, -0.05)) - 0.22, 0.015);
                    mist += stroke(length(p - float2(0.14, 0.12)) - 0.18, 0.012) * 0.75;
                    mist += exp(-length(p - float2(-0.26, 0.26)) * 48.0);
                    return saturate(mist);
                }
                if (school < 2.5)
                {
                    float shield = max(abs(p.x) * 0.86 - 0.26, abs(p.y + 0.015) - 0.32);
                    shield = max(shield, abs(p.x) * 0.62 - p.y * 0.6 - 0.25);
                    return stroke(shield, 0.013);
                }
                if (school < 3.5)
                {
                    float wave = p.y - sin(p.x * 7.0) * 0.11;
                    return (stroke(wave - 0.17, 0.012) + stroke(wave + 0.02, 0.008) * 0.65
                        + stroke(wave + 0.20, 0.006) * 0.45) * saturate(1.0 - abs(p.x) / 0.46);
                }
                float ring = stroke(length(p) - 0.34, 0.028);
                return ring * smoothstep(-0.30, 0.12, p.x - p.y * 0.6);
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                float2 p = uv - 0.5;
                float split = smoothstep(0.38, 0.62, uv.x) * _Schools.z;
                float3 accent = lerp(_PrimaryColor.rgb, _SecondaryColor.rgb, split);
                float4 art = tex2D(_MainTex, uv);
                float luminance = dot(art.rgb, float3(0.299, 0.587, 0.114));
                // Preserve ink cuts and bright metal cores, recolor the midtones most strongly.
                float3 tinted = accent * (0.24 + luminance * 1.12);
                tinted = lerp(tinted, float3(0.94, 0.95, 0.88), smoothstep(0.64, 1.0, luminance) * 0.8);
                float3 subject = lerp(art.rgb, tinted, 0.76);

                float2 d = _MainTex_TexelSize.xy * 2.0;
                float halo = tex2D(_MainTex, uv + float2(d.x, 0)).a;
                halo += tex2D(_MainTex, uv - float2(d.x, 0)).a;
                halo += tex2D(_MainTex, uv + float2(0, d.y)).a;
                halo += tex2D(_MainTex, uv - float2(0, d.y)).a;
                halo *= 0.25;
                float energy = lerp(motif(p, _Schools.x), motif(p, _Schools.y), split);
                float vignette = saturate(1.0 - length(p) / 0.59);
                float edgeFade = smoothstep(0.0, 0.065, min(min(uv.x, 1.0 - uv.x), min(uv.y, 1.0 - uv.y)));
                float effectAlpha = saturate(halo * 0.60 + energy * 0.68 + vignette * 0.28) * edgeFade;
                float3 effectColor = accent * (0.40 + energy * 0.60 + halo * 0.28);
                float alpha = art.a + effectAlpha * (1.0 - art.a);
                float3 rgb = (subject * art.a + effectColor * effectAlpha * (1.0 - art.a)) / max(alpha, 0.001);
                return float4(rgb, alpha);
            }
            ENDCG
        }
    }
}

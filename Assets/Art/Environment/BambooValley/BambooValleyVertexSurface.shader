Shader "Wuxia Roguelite/Bamboo Valley Vertex Surface"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _Smoothness ("Smoothness", Range(0,1)) = 0.25
        _Emission ("Emission", Range(0,5)) = 0
        _Foliage ("Player visibility foliage", Float) = 0
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
        half _Foliage;
        float4 _BambooPlayerPosition;
        struct Input { float4 color : COLOR; float3 worldPos; float4 screenPos; };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            if (_Foliage > 0.5 && _BambooPlayerPosition.w > 0.5)
            {
                float3 player = _BambooPlayerPosition.xyz + float3(0,1,0);
                float3 ray = _WorldSpaceCameraPos - player;
                float t = dot(IN.worldPos-player,ray) / max(dot(ray,ray),0.01);
                float distanceToRay = length(IN.worldPos-player-saturate(t)*ray);
                float fade = smoothstep(1.2,2.4,distanceToRay);
                if(t>0 && t<1 && IN.worldPos.y>player.y-.4)
                {
                    float2 pixel=floor(IN.screenPos.xy / IN.screenPos.w * _ScreenParams.xy);
                    float threshold=frac(dot(pixel,float2(.754877666,.569840296)));
                    clip(lerp(.12,1,fade)-threshold);
                }
            }
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

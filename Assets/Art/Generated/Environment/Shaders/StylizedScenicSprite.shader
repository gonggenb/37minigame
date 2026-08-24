Shader "Wuxia Roguelite/Stylized Scenic Sprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Saturation ("Saturation", Range(0, 1)) = 0.72
        _Contrast ("Contrast", Range(0.5, 1.5)) = 0.82
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _Flip ("Flip", Vector) = (1, 1, 1, 1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "UnitySprites.cginc"

            half _Saturation;
            half _Contrast;

            struct ScenicV2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_FOG_COORDS(1)
            };

            ScenicV2f vert(appdata_t input)
            {
                ScenicV2f output;
                input.vertex = UnityFlipSprite(input.vertex, _Flip);
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color * _RendererColor;
                #ifdef PIXELSNAP_ON
                output.vertex = UnityPixelSnap(output.vertex);
                #endif
                UNITY_TRANSFER_FOG(output, output.vertex);
                return output;
            }

            fixed4 frag(ScenicV2f input) : SV_Target
            {
                fixed4 color = SampleSpriteTexture(input.texcoord);
                half luminance = dot(color.rgb, half3(0.299h, 0.587h, 0.114h));
                color.rgb = lerp(luminance.xxx, color.rgb, _Saturation);
                color.rgb = saturate((color.rgb - 0.5h) * _Contrast + 0.5h);
                color *= input.color;
                UNITY_APPLY_FOG(input.fogCoord, color);
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}

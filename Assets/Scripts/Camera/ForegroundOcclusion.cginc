#ifndef WUXIA_FOREGROUND_OCCLUSION_INCLUDED
#define WUXIA_FOREGROUND_OCCLUSION_INCLUDED

float4 _ForegroundAnchor;  // Visible actor centre, strength.
float4 _ForegroundOpening; // Horizontal/vertical world radii at actor depth, foot height.

void ApplyForegroundOcclusion(float3 worldPosition, float4 screenPosition)
{
    // Keep environmental shadows intact. Scene/other cameras have zero strength.
    #if !defined(UNITY_PASS_SHADOWCASTER)
    if (_ForegroundAnchor.w <= 0.0 || worldPosition.y <= _ForegroundOpening.z + 0.12) return;
    float3 actor = mul(UNITY_MATRIX_V, float4(_ForegroundAnchor.xyz, 1)).xyz;
    float3 surface = mul(UNITY_MATRIX_V, float4(worldPosition, 1)).xyz;
    float actorDepth = -actor.z;
    float depth = -surface.z;
    if (depth <= 0.0 || actorDepth <= depth) return;

    // Perspective-correct opening: nearby geometry must not project across the actor.
    // View-space radii stay circular/elliptical in both portrait and landscape.
    float2 delta = surface.xy * (actorDepth / max(depth, 0.001)) - actor.xy;
    float radial = length(delta / max(_ForegroundOpening.xy, float2(0.01, 0.01)));
    float opening = 1.0 - smoothstep(0.52, 1.0, radial);
    float inFront = smoothstep(0.08, 0.35, actorDepth - depth);
    // Preserve walkable ground/bridge decks and the bases of foreground objects.
    float aboveGround = smoothstep(0.12, 0.42, worldPosition.y - _ForegroundOpening.z);
    float coverage = 1.0 - opening * inFront * aboveGround * _ForegroundAnchor.w;
    float2 pixel = floor(screenPosition.xy / screenPosition.w * _ScreenParams.xy);
    float threshold = frac(52.9829189 * frac(dot(pixel, float2(0.06711056, 0.00583715))));
    // Zero coverage must discard even threshold-zero pixels through stacked bamboo layers.
    clip(coverage - max(threshold, 0.0001));
    #endif
}
#endif

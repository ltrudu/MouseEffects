// Radial Dithering Shader
// Creates a dithering effect in a circular area around the mouse cursor
// Based on Bayer ordered dithering pattern

struct PSInput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

cbuffer DitheringParams : register(b0)
{
    float2 MousePosition;       // Mouse position in screen pixels
    float2 ViewportSize;        // Viewport size in pixels
    float Radius;               // Radius of dithering effect in pixels
    float Intensity;            // Dithering intensity (0-1)
    float PatternScale;         // Scale of the dither pattern (1-8)
    float Time;                 // Current time for animation
    float AnimationSpeed;       // Speed of pattern animation
    float EdgeSoftness;         // Softness of the effect edge (0-1)
    float EnableAnimation;      // 1.0 = animation on, 0.0 = off
    float InvertPattern;        // 1.0 = invert pattern, 0.0 = normal
    float4 Color1;              // Primary dither color (visible pixels)
    float4 Color2;              // Secondary dither color (background)
    float FalloffType;          // 0=linear, 1=smooth, 2=sharp, 3=ring
    float RingWidth;            // Width of ring effect (when FalloffType=3)
    float EnableGlow;           // 1.0 = glow on, 0.0 = off
    float GlowIntensity;        // Glow intensity (0-1)
    float4 GlowColor;           // RGBA glow color
    float ColorBlendMode;       // 0=replace, 1=multiply, 2=screen, 3=overlay
    float Threshold;            // Dither threshold adjustment (-0.5 to 0.5)
    float EnableNoise;          // Add noise to break up banding
    float NoiseAmount;          // Amount of noise to add
    float Alpha;                // Overall effect opacity (0-1)
};

Texture2D<float4> ScreenTexture : register(t0);
SamplerState LinearSampler : register(s0);

// 8x8 Bayer dithering matrix (normalized to 0-1 range)
// This provides 64 levels of dithering for smooth gradients
static const float BayerMatrix8x8[64] = {
     0.0/64.0,  32.0/64.0,   8.0/64.0,  40.0/64.0,   2.0/64.0,  34.0/64.0,  10.0/64.0,  42.0/64.0,
    48.0/64.0,  16.0/64.0,  56.0/64.0,  24.0/64.0,  50.0/64.0,  18.0/64.0,  58.0/64.0,  26.0/64.0,
    12.0/64.0,  44.0/64.0,   4.0/64.0,  36.0/64.0,  14.0/64.0,  46.0/64.0,   6.0/64.0,  38.0/64.0,
    60.0/64.0,  28.0/64.0,  52.0/64.0,  20.0/64.0,  62.0/64.0,  30.0/64.0,  54.0/64.0,  22.0/64.0,
     3.0/64.0,  35.0/64.0,  11.0/64.0,  43.0/64.0,   1.0/64.0,  33.0/64.0,   9.0/64.0,  41.0/64.0,
    51.0/64.0,  19.0/64.0,  59.0/64.0,  27.0/64.0,  49.0/64.0,  17.0/64.0,  57.0/64.0,  25.0/64.0,
    15.0/64.0,  47.0/64.0,   7.0/64.0,  39.0/64.0,  13.0/64.0,  45.0/64.0,   5.0/64.0,  37.0/64.0,
    63.0/64.0,  31.0/64.0,  55.0/64.0,  23.0/64.0,  61.0/64.0,  29.0/64.0,  53.0/64.0,  21.0/64.0
};

// 4x4 Bayer matrix for coarser dithering
static const float BayerMatrix4x4[16] = {
     0.0/16.0,   8.0/16.0,   2.0/16.0,  10.0/16.0,
    12.0/16.0,   4.0/16.0,  14.0/16.0,   6.0/16.0,
     3.0/16.0,  11.0/16.0,   1.0/16.0,   9.0/16.0,
    15.0/16.0,   7.0/16.0,  13.0/16.0,   5.0/16.0
};

// Simple hash function for noise
float hash(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

// Get dither value from Bayer matrix
float GetBayerValue(float2 screenPos, float scale)
{
    // Apply animation offset if enabled
    float2 animOffset = float2(0, 0);
    if (EnableAnimation > 0.5)
    {
        animOffset = float2(Time * AnimationSpeed * 10.0, Time * AnimationSpeed * 7.0);
    }

    float2 pos = (screenPos + animOffset) / scale;
    int2 coord = int2(fmod(abs(pos), 8.0));
    int index = coord.y * 8 + coord.x;

    float bayerValue = BayerMatrix8x8[index];

    // Add noise if enabled
    if (EnableNoise > 0.5)
    {
        float noise = hash(screenPos + Time * 0.1) * 2.0 - 1.0;
        bayerValue += noise * NoiseAmount * 0.1;
        bayerValue = saturate(bayerValue);
    }

    return InvertPattern > 0.5 ? 1.0 - bayerValue : bayerValue;
}

// Calculate falloff based on type
float CalculateFalloff(float normalizedDist)
{
    float falloff = 0.0;

    if (FalloffType < 0.5)
    {
        // Linear falloff
        falloff = 1.0 - normalizedDist;
    }
    else if (FalloffType < 1.5)
    {
        // Smooth (quadratic) falloff
        falloff = 1.0 - normalizedDist;
        falloff = falloff * falloff;
    }
    else if (FalloffType < 2.5)
    {
        // Sharp falloff
        falloff = 1.0 - normalizedDist;
        falloff = pow(falloff, 0.5);
    }
    else
    {
        // Ring effect
        float ringCenter = 1.0 - RingWidth;
        float ringDist = abs(normalizedDist - ringCenter);
        falloff = 1.0 - saturate(ringDist / (RingWidth * 0.5));
        falloff = falloff * falloff;
    }

    // Apply edge softness
    float softEdge = smoothstep(1.0, 1.0 - EdgeSoftness, normalizedDist);
    falloff *= softEdge;

    return saturate(falloff);
}

// Blend colors based on mode
float4 BlendColors(float4 screenColor, float4 ditherColor, float blendMode)
{
    float4 result;

    if (blendMode < 0.5)
    {
        // Replace mode
        result = ditherColor;
    }
    else if (blendMode < 1.5)
    {
        // Multiply mode
        result = screenColor * ditherColor;
    }
    else if (blendMode < 2.5)
    {
        // Screen mode
        result = 1.0 - (1.0 - screenColor) * (1.0 - ditherColor);
    }
    else
    {
        // Overlay mode
        float4 low = 2.0 * screenColor * ditherColor;
        float4 high = 1.0 - 2.0 * (1.0 - screenColor) * (1.0 - ditherColor);
        result = lerp(low, high, step(0.5, screenColor));
    }

    result.a = ditherColor.a;
    return result;
}

// Vertex shader - generates fullscreen quad procedurally
PSInput VSMain(uint vertexId : SV_VertexID)
{
    PSInput output;

    // Generate fullscreen triangle strip: 0,1,2,3 -> positions
    float2 uv = float2((vertexId << 1) & 2, vertexId & 2);
    output.Position = float4(uv * 2.0 - 1.0, 0.0, 1.0);
    output.Position.y = -output.Position.y; // Flip Y for DirectX
    output.TexCoord = uv;

    return output;
}

// Pixel shader - applies dithering effect
float4 PSMain(PSInput input) : SV_TARGET
{
    float2 uv = input.TexCoord;
    float2 screenPos = uv * ViewportSize;

    // Calculate distance from mouse
    float2 toMouse = screenPos - MousePosition;
    float dist = length(toMouse);

    // Normalize distance relative to radius
    float normalizedDist = dist / Radius;

    // Only apply effect within the radius
    if (normalizedDist < 1.0)
    {
        // Get screen color
        float4 screenColor = ScreenTexture.Sample(LinearSampler, uv);

        // Calculate falloff
        float falloff = CalculateFalloff(normalizedDist);

        // Get dither pattern value
        float ditherValue = GetBayerValue(screenPos, PatternScale);

        // Calculate the dither threshold based on falloff and intensity
        float threshold = falloff * Intensity + Threshold;

        // Determine if this pixel should show Color1 or Color2
        float ditherResult = step(ditherValue, threshold);

        // Blend between the two dither colors
        float4 ditherColor = lerp(Color2, Color1, ditherResult);

        // Apply color blend mode with screen content
        float4 blendedColor = BlendColors(screenColor, ditherColor, ColorBlendMode);

        // Final color with falloff-based alpha and overall opacity
        float4 finalColor = blendedColor;
        finalColor.a = falloff * Color1.a * Alpha;

        // Optional edge glow
        if (EnableGlow > 0.5)
        {
            float edgeGlow = falloff * (1.0 - falloff) * 4.0; // Peaks at 0.5
            float3 glow = GlowColor.rgb * edgeGlow * GlowIntensity;
            finalColor.rgb += glow;
        }

        return finalColor;
    }
    else
    {
        // Outside radius - fully transparent
        return float4(0, 0, 0, 0);
    }
}

// Shockwave Shader
// Creates expanding circular shockwave rings with glow and optional screen distortion

struct PSInput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

// Shockwave data structure - must match C# ShockwaveGPU struct
struct ShockwaveData
{
    float2 Position;    // Center of shockwave in screen pixels
    float Radius;       // Current radius (how far wave has expanded)
    float Age;          // Time since spawn
    float Lifetime;     // Total lifetime
    float Padding1;
    float Padding2;
    float Padding3;
    float Padding4;
};

cbuffer ShockwaveParams : register(b0)
{
    float2 ViewportSize;
    float Time;
    int ShockwaveCount;
    float RingThickness;
    float GlowIntensity;
    float EnableDistortion;
    float DistortionStrength;
    float HdrBrightness;
    float Padding1;
    float Padding2;
    float Padding3;
    float4 RingColor;
};

Texture2D<float4> ScreenTexture : register(t0);
StructuredBuffer<ShockwaveData> Shockwaves : register(t1);
SamplerState LinearSampler : register(s0);

static const float PI = 3.14159265359;
static const float TWO_PI = 6.28318530718;

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

// Smooth step function for smooth transitions
float smootherstep(float edge0, float edge1, float x)
{
    float t = saturate((x - edge0) / (edge1 - edge0));
    return t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
}

// Pixel shader - renders shockwave rings with glow and optional distortion
float4 PSMain(PSInput input) : SV_TARGET
{
    float2 uv = input.TexCoord;
    float2 screenPos = uv * ViewportSize;

    // Accumulate total distortion and ring intensity
    float2 totalDistortion = float2(0, 0);
    float totalRingIntensity = 0.0;

    for (int i = 0; i < ShockwaveCount; i++)
    {
        ShockwaveData shockwave = Shockwaves[i];

        // Vector from shockwave center to this pixel
        float2 toPixel = screenPos - shockwave.Position;
        float dist = length(toPixel);

        // Skip if too far from ring
        float maxDist = shockwave.Radius + RingThickness * 2.0;
        if (dist > maxDist || dist < 0.001)
            continue;

        // Normalized direction
        float2 direction = toPixel / dist;

        // Calculate distance from ring center (the expanding edge)
        float distFromRing = abs(dist - shockwave.Radius);

        // Ring intensity based on distance from ring edge
        // Creates a bright core with falloff
        float ringFalloff = 1.0 - smootherstep(0.0, RingThickness, distFromRing);

        // Age-based fade out (quadratic for smooth fade)
        float normalizedAge = shockwave.Age / shockwave.Lifetime;
        float ageFade = 1.0 - normalizedAge;
        ageFade = ageFade * ageFade; // Quadratic fade

        // Combined ring intensity
        float ringIntensity = ringFalloff * ageFade;

        // Add glow effect (wider falloff)
        float glowFalloff = 1.0 - smootherstep(0.0, RingThickness * 3.0, distFromRing);
        float glowIntensity = glowFalloff * ageFade * 0.5;

        // Total intensity for this shockwave
        float intensity = ringIntensity + glowIntensity * GlowIntensity;
        totalRingIntensity += intensity;

        // Distortion effect (radial push outward from ring)
        if (EnableDistortion > 0.5)
        {
            // Distortion is strongest at the ring edge
            float distortionAmount = ringIntensity * DistortionStrength * ageFade;
            totalDistortion += direction * distortionAmount;
        }
    }

    // Start with base color
    float4 finalColor = float4(0, 0, 0, 0);

    // Apply distortion if enabled and we have screen texture
    if (EnableDistortion > 0.5)
    {
        // Convert distortion from pixels to UV space
        float2 uvOffset = totalDistortion / ViewportSize;
        float2 distortedUV = saturate(uv + uvOffset);

        // Sample screen at distorted position
        finalColor = ScreenTexture.SampleLevel(LinearSampler, distortedUV, 0);
    }

    // Add ring color with glow
    if (totalRingIntensity > 0.001)
    {
        float3 ringGlow = RingColor.rgb * totalRingIntensity * HdrBrightness;

        if (EnableDistortion > 0.5)
        {
            // Additive blend on top of distorted screen
            finalColor.rgb += ringGlow;
        }
        else
        {
            // Just the ring (for additive blend mode)
            finalColor.rgb = ringGlow;
            finalColor.a = saturate(totalRingIntensity);
        }
    }

    return finalColor;
}

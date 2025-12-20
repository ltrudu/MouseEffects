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

// Pixel shader - renders shockwave rings with glow and dramatic screen distortion
float4 PSMain(PSInput input) : SV_TARGET
{
    float2 uv = input.TexCoord;
    float2 screenPos = uv * ViewportSize;

    // Accumulate total distortion and ring intensity
    float2 totalDistortion = float2(0, 0);
    float totalRingIntensity = 0.0;
    float chromaticAmount = 0.0;

    for (int i = 0; i < ShockwaveCount; i++)
    {
        ShockwaveData shockwave = Shockwaves[i];

        // Vector from shockwave center to this pixel
        float2 toPixel = screenPos - shockwave.Position;
        float dist = length(toPixel);

        // Wider distortion area for more dramatic effect
        float distortionWidth = RingThickness * 4.0;
        float maxDist = shockwave.Radius + distortionWidth;

        if (dist > maxDist || dist < 0.001)
            continue;

        // Normalized direction
        float2 direction = toPixel / dist;

        // Calculate signed distance from ring center (negative inside, positive outside)
        float signedDistFromRing = dist - shockwave.Radius;
        float distFromRing = abs(signedDistFromRing);

        // Age-based fade out (quadratic for smooth fade)
        float normalizedAge = shockwave.Age / shockwave.Lifetime;
        float ageFade = 1.0 - normalizedAge;
        ageFade = ageFade * ageFade; // Quadratic fade

        // Ring intensity based on distance from ring edge
        float ringFalloff = 1.0 - smootherstep(0.0, RingThickness, distFromRing);
        float ringIntensity = ringFalloff * ageFade;

        // Add glow effect (wider falloff)
        float glowFalloff = 1.0 - smootherstep(0.0, RingThickness * 3.0, distFromRing);
        float glowIntensity = glowFalloff * ageFade * 0.5;

        // Total intensity for this shockwave
        float intensity = ringIntensity + glowIntensity * GlowIntensity;
        totalRingIntensity += intensity;

        // Dramatic distortion effect
        if (EnableDistortion > 0.5)
        {
            // Create a wave-like distortion profile
            // Compression in front of wave, expansion behind
            float waveProfile;
            if (signedDistFromRing > 0)
            {
                // Outside the ring - push outward (expansion)
                waveProfile = smootherstep(0.0, distortionWidth, distFromRing);
                waveProfile = (1.0 - waveProfile) * 1.5;
            }
            else
            {
                // Inside the ring - pull inward slightly then push (compression wave)
                float innerFalloff = smootherstep(0.0, distortionWidth * 0.5, distFromRing);
                waveProfile = -(1.0 - innerFalloff) * 0.5;
            }

            // Add sinusoidal ripple for extra drama
            float ripple = sin(distFromRing * 0.15 - shockwave.Age * 20.0) * 0.3;
            ripple *= (1.0 - smootherstep(0.0, distortionWidth, distFromRing));

            // Combined distortion amount
            float distortionAmount = (waveProfile + ripple) * DistortionStrength * ageFade;
            totalDistortion += direction * distortionAmount;

            // Chromatic aberration amount based on distortion intensity
            chromaticAmount += abs(distortionAmount) * 0.02 * ageFade;
        }
    }

    // Start with base color
    float4 finalColor = float4(0, 0, 0, 0);

    // Apply distortion if enabled and we have screen texture
    if (EnableDistortion > 0.5)
    {
        // Convert distortion from pixels to UV space
        float2 uvOffset = totalDistortion / ViewportSize;
        float2 distortedUV = uv + uvOffset;

        // Clamp UV to valid range
        distortedUV = saturate(distortedUV);

        // Chromatic aberration for extra drama
        if (chromaticAmount > 0.001)
        {
            float2 chromaticOffset = uvOffset * chromaticAmount;
            float2 uvR = saturate(distortedUV + chromaticOffset);
            float2 uvB = saturate(distortedUV - chromaticOffset);

            finalColor.r = ScreenTexture.SampleLevel(LinearSampler, uvR, 0).r;
            finalColor.g = ScreenTexture.SampleLevel(LinearSampler, distortedUV, 0).g;
            finalColor.b = ScreenTexture.SampleLevel(LinearSampler, uvB, 0).b;
            finalColor.a = 1.0;
        }
        else
        {
            // Sample screen at distorted position
            finalColor = ScreenTexture.SampleLevel(LinearSampler, distortedUV, 0);
        }
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

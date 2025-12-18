// Spotlight Effect Shader
// Creates dramatic theater lighting centered on the mouse cursor

static const float PI = 3.14159265359;

struct PSInput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

cbuffer SpotlightParams : register(b0)
{
    float2 MousePosition;        // Mouse position in screen pixels
    float2 ViewportSize;         // Viewport size in pixels
    float SpotlightRadius;       // Spotlight radius in pixels
    float EdgeSoftness;          // Softness of the spotlight edge
    float DarknessLevel;         // Darkness outside spotlight (0=black, 1=full)
    float BrightnessBoost;       // Brightness multiplier inside spotlight
    int ColorTemperature;        // 0=warm, 1=neutral, 2=cool
    float DustParticlesEnabled;  // 1.0 = enabled, 0.0 = disabled
    float DustDensity;           // Density of dust particles
    float Time;                  // Total time in seconds
    float HdrMultiplier;         // HDR brightness multiplier
    float Padding1;
    float2 Padding2;
};

Texture2D<float4> ScreenTexture : register(t0);
SamplerState LinearSampler : register(s0);

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

// Simple hash function for noise
float hash(float2 p)
{
    p = frac(p * float2(443.897, 441.423));
    p += dot(p, p.yx + 19.19);
    return frac(p.x * p.y);
}

// Simplex-inspired noise
float noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);

    float a = hash(i);
    float b = hash(i + float2(1.0, 0.0));
    float c = hash(i + float2(0.0, 1.0));
    float d = hash(i + float2(1.0, 1.0));

    float2 u = f * f * (3.0 - 2.0 * f);

    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

// Calculate spotlight mask with soft edge
float spotlightMask(float2 screenPos, float2 center, float radius, float softness)
{
    float dist = length(screenPos - center);
    return 1.0 - smoothstep(radius - softness, radius + softness, dist);
}

// Calculate color temperature tint
float3 applyColorTemperature(float3 color, int temperature)
{
    if (temperature == 0) // Warm
        return color * float3(1.2, 1.0, 0.8);
    else if (temperature == 2) // Cool
        return color * float3(0.8, 0.9, 1.1);
    else // Neutral
        return color;
}

// Calculate dust particle brightness
float dustParticle(float2 screenPos, float2 center, float radius, float time)
{
    float totalDust = 0.0;

    // Generate multiple dust particles
    for (int i = 0; i < 20; i++)
    {
        float fi = float(i);

        // Create unique position for each particle using hash
        float2 seed = float2(fi * 12.345, fi * 67.890);
        float angle = hash(seed) * 2.0 * PI;
        float distFromCenter = hash(seed + float2(1.0, 1.0)) * radius;

        // Floating motion
        float floatSpeed = hash(seed + float2(2.0, 2.0)) * 0.5 + 0.3;
        float verticalOffset = sin(time * floatSpeed + fi) * 20.0;
        float horizontalOffset = cos(time * floatSpeed * 0.7 + fi * 0.5) * 15.0;

        // Particle position
        float2 particlePos = center + float2(
            cos(angle) * distFromCenter + horizontalOffset,
            sin(angle) * distFromCenter + verticalOffset
        );

        // Distance to particle
        float dist = length(screenPos - particlePos);

        // Particle size varies
        float particleSize = (hash(seed + float2(3.0, 3.0)) * 1.5 + 0.5) * 2.0;

        // Particle brightness with soft falloff
        float particleBrightness = 1.0 - smoothstep(0.0, particleSize, dist);
        particleBrightness *= particleBrightness; // Square for sharper falloff

        // Particle opacity flickers
        float flicker = sin(time * 3.0 + fi * 2.0) * 0.3 + 0.7;
        particleBrightness *= flicker;

        totalDust += particleBrightness;
    }

    return saturate(totalDust);
}

// Pixel shader - applies spotlight effect
float4 PSMain(PSInput input) : SV_TARGET
{
    float2 uv = input.TexCoord;
    float2 screenPos = uv * ViewportSize;

    // Sample the screen texture
    float4 screenColor = ScreenTexture.Sample(LinearSampler, uv);

    // Calculate spotlight mask (1.0 inside, 0.0 outside)
    float mask = spotlightMask(screenPos, MousePosition, SpotlightRadius, EdgeSoftness);

    // Apply vignette (darkening around edges of screen)
    float2 vignetteUV = uv * 2.0 - 1.0;
    float vignette = 1.0 - length(vignetteUV) * 0.3;
    vignette = saturate(vignette);

    // Combine vignette with darkness level
    float darkness = DarknessLevel * vignette;

    // Blend between darkness and brightness based on spotlight mask
    float brightnessMult = lerp(darkness, BrightnessBoost, mask);

    // Apply brightness to screen color
    float3 color = screenColor.rgb * brightnessMult;

    // Apply color temperature tint (only inside spotlight)
    color = lerp(color, applyColorTemperature(color, ColorTemperature), mask);

    // Add dust particles if enabled
    if (DustParticlesEnabled > 0.5 && mask > 0.1)
    {
        float dust = dustParticle(screenPos, MousePosition, SpotlightRadius, Time);
        dust *= DustDensity;

        // Dust is more visible in the lit area
        dust *= mask;

        // Add subtle dust glow
        float3 dustColor = applyColorTemperature(float3(1, 1, 1), ColorTemperature);
        color += dustColor * dust * 0.3 * HdrMultiplier;
    }

    // Ensure we don't go below darkness level or above max brightness
    color = max(color, screenColor.rgb * darkness);
    color = min(color, screenColor.rgb * BrightnessBoost * 2.0);

    return float4(color, 1.0);
}

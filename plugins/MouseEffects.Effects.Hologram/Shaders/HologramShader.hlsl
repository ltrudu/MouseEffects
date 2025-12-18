// Hologram Effect Shader
// Creates sci-fi holographic projection with scan lines, flickering, and chromatic aberration

static const float PI = 3.14159265359;

struct PSInput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

cbuffer HologramParams : register(b0)
{
    float2 MousePosition;           // Mouse position in screen pixels
    float2 ViewportSize;            // Viewport size in pixels
    float Radius;                   // Effect radius
    float ScanLineDensity;          // Scan line density
    float ScanLineSpeed;            // Scan line speed
    float FlickerIntensity;         // Flicker intensity
    int ColorTint;                  // Color theme: 0=Cyan, 1=Blue, 2=Green, 3=Purple
    float EdgeGlowStrength;         // Edge glow strength
    float NoiseAmount;              // Noise overlay amount
    float ChromaticAberration;      // RGB split amount
    float TintStrength;             // Color tint strength
    float Time;                     // Total time in seconds
    float HdrMultiplier;            // HDR brightness multiplier
    float Padding;
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

// Hash function for pseudo-random numbers
float hash(float2 p)
{
    p = frac(p * float2(443.897, 441.423));
    p += dot(p, p.yx + 19.19);
    return frac(p.x * p.y);
}

// 1D hash
float hash1(float p)
{
    p = frac(p * 0.1031);
    p *= p + 33.33;
    p *= p + p;
    return frac(p);
}

// Noise function
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

// Get hologram color based on tint selection
float3 getHologramColor(int tint)
{
    switch (tint)
    {
        case 0: return float3(0.0, 1.0, 1.0);      // Cyan
        case 1: return float3(0.255, 0.412, 0.882); // Blue (#4169E1)
        case 2: return float3(0.0, 1.0, 0.498);     // Green (#00FF7F)
        case 3: return float3(0.576, 0.439, 0.859); // Purple (#9370DB)
        default: return float3(0.0, 1.0, 1.0);      // Default to Cyan
    }
}

// RGB split (chromatic aberration) for holographic effect
float3 rgbSplit(float2 uv, float amount, float2 direction)
{
    float r = ScreenTexture.Sample(LinearSampler, uv + direction * amount).r;
    float g = ScreenTexture.Sample(LinearSampler, uv).g;
    float b = ScreenTexture.Sample(LinearSampler, uv - direction * amount).b;
    return float3(r, g, b);
}

// Scan lines effect
float scanLines(float2 uv, float time)
{
    float y = uv.y * ViewportSize.y;
    float scanLine = sin(y * (2.0 * PI / ScanLineDensity) + time * ScanLineSpeed * 10.0);

    // Create sharper scan lines
    scanLine = pow(abs(scanLine), 0.5) * sign(scanLine);

    // Map to 0.7-1.0 range for subtle darkening
    return scanLine * 0.15 + 0.85;
}

// Flicker effect
float flicker(float time)
{
    // Combine multiple noise frequencies for natural flicker
    float f1 = hash1(floor(time * 20.0));
    float f2 = hash1(floor(time * 47.0));
    float f3 = noise(float2(time * 13.0, 0));

    float flickerValue = (f1 * 0.4 + f2 * 0.3 + f3 * 0.3);

    // Map to brightness range
    return 1.0 + (flickerValue - 0.5) * FlickerIntensity;
}

// Edge detection for glow
float edgeDetect(float2 uv)
{
    float3 sampleOffsets[4] = {
        float3(-1, 0, 0),
        float3(1, 0, 0),
        float3(0, -1, 0),
        float3(0, 1, 0)
    };

    float pixelSize = 1.0 / ViewportSize.x;
    float3 center = ScreenTexture.Sample(LinearSampler, uv).rgb;
    float edge = 0.0;

    for (int i = 0; i < 4; i++)
    {
        float2 offset = sampleOffsets[i].xy * pixelSize * 2.0;
        float3 sample = ScreenTexture.Sample(LinearSampler, uv + offset).rgb;
        edge += length(center - sample);
    }

    return saturate(edge * 2.0);
}

// Pixel shader - applies hologram effect
float4 PSMain(PSInput input) : SV_TARGET
{
    float2 uv = input.TexCoord;
    float2 screenPos = uv * ViewportSize;

    // Vector from mouse to current pixel
    float2 toMouse = screenPos - MousePosition;
    float dist = length(toMouse);

    // Calculate effect influence (1.0 at center, 0.0 at radius)
    float influence = 1.0 - saturate(dist / Radius);

    // Early exit if outside radius
    if (influence <= 0.0)
    {
        return float4(0, 0, 0, 0);
    }

    // Direction from mouse (normalized)
    float2 dir = dist > 0.001 ? toMouse / dist : float2(0, 0);

    // 1. Apply chromatic aberration
    float3 color;
    if (ChromaticAberration > 0.0)
    {
        float aberrationAmount = ChromaticAberration * influence;
        color = rgbSplit(uv, aberrationAmount, dir);
    }
    else
    {
        color = ScreenTexture.Sample(LinearSampler, uv).rgb;
    }

    // 2. Apply holographic color tint
    float3 holoColor = getHologramColor(ColorTint);
    color = lerp(color, holoColor * dot(color, float3(0.299, 0.587, 0.114)), TintStrength * influence);

    // 3. Apply scan lines
    float scanLineEffect = scanLines(uv, Time);
    color *= scanLineEffect;

    // 4. Apply flicker
    float flickerEffect = flicker(Time);
    color *= flickerEffect;

    // 5. Add noise overlay
    float noiseVal = noise(uv * 800.0 + Time * 5.0);
    color += (noiseVal - 0.5) * NoiseAmount * influence * 0.5;

    // 6. Edge glow effect
    float edge = edgeDetect(uv);
    float edgeGlow = edge * EdgeGlowStrength * influence;
    color += holoColor * edgeGlow;

    // 7. Add holographic glow at edges of effect radius
    float edgeFalloff = smoothstep(0.0, 0.3, influence) * smoothstep(1.0, 0.7, influence);
    color += holoColor * edgeFalloff * 0.3;

    // 8. Occasional bright glitches
    float glitchHash = hash(floor(uv * 300.0) + floor(Time * 15.0));
    if (glitchHash > 0.995)
    {
        color += holoColor * influence * 0.5;
    }

    // 9. Horizontal interference lines (rare)
    float interferenceY = floor(uv.y * 100.0 + Time * 2.0);
    float interference = hash1(interferenceY);
    if (interference > 0.97)
    {
        color *= 1.0 + (interference - 0.97) * 10.0 * influence;
    }

    // Smooth edge fade
    float alpha = smoothstep(0.0, 0.2, influence);

    // Output with HDR multiplier
    return float4(color * HdrMultiplier, alpha);
}

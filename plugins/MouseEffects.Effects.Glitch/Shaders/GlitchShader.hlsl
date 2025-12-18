// Glitch Effect Shader
// Creates digital corruption and distortion artifacts around the mouse cursor

static const float PI = 3.14159265359;

struct PSInput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

cbuffer GlitchParams : register(b0)
{
    float2 MousePosition;        // Mouse position in screen pixels
    float2 ViewportSize;         // Viewport size in pixels
    float Radius;                // Effect radius
    float Intensity;             // Overall glitch intensity
    float RgbSplitAmount;        // Chromatic aberration amount
    float ScanLineFrequency;     // Scan line frequency
    float BlockSize;             // Block displacement size
    float NoiseAmount;           // Noise overlay amount
    float GlitchFrequency;       // How often glitches change
    float Time;                  // Total time in seconds
    float HdrMultiplier;         // HDR brightness multiplier
    float Padding;
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

// Hash function for pseudo-random numbers
float hash(float2 p)
{
    p = frac(p * float2(443.897, 441.423));
    p += dot(p, p.yx + 19.19);
    return frac(p.x * p.y);
}

// 2D hash for better randomness
float2 hash2(float2 p)
{
    p = float2(dot(p, float2(127.1, 311.7)),
               dot(p, float2(269.5, 183.3)));
    return frac(sin(p) * 43758.5453);
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

// RGB split (chromatic aberration)
float3 rgbSplit(float2 uv, float amount, float2 direction)
{
    float r = ScreenTexture.Sample(LinearSampler, uv + direction * amount).r;
    float g = ScreenTexture.Sample(LinearSampler, uv).g;
    float b = ScreenTexture.Sample(LinearSampler, uv - direction * amount).b;
    return float3(r, g, b);
}

// Block displacement glitch
float2 blockGlitch(float2 uv, float blockSize, float time)
{
    float2 blockCoord = floor(uv * ViewportSize / blockSize);
    float blockHash = hash(blockCoord + floor(time * GlitchFrequency));

    // Random offset for this block
    float offset = (blockHash - 0.5) * 0.1;

    // Only apply to some blocks
    if (blockHash > 0.7)
    {
        return float2(offset, 0);
    }

    return float2(0, 0);
}

// Scan line distortion
float scanLineDistortion(float2 uv, float time)
{
    float y = uv.y * ViewportSize.y;
    float scanLine = floor(y / ScanLineFrequency);
    float scanHash = hash(float2(scanLine, floor(time * GlitchFrequency * 2.0)));

    // Random horizontal offset for this scan line
    if (scanHash > 0.8)
    {
        return (scanHash - 0.5) * 0.05;
    }

    return 0.0;
}

// Color corruption
float3 colorCorruption(float3 color, float2 uv, float time)
{
    float corruptionHash = hash(floor(uv * 10.0) + floor(time * GlitchFrequency * 3.0));

    // Randomly invert or shift colors
    if (corruptionHash > 0.9)
    {
        // Color inversion
        return 1.0 - color;
    }
    else if (corruptionHash > 0.85)
    {
        // Channel swap
        return color.gbr;
    }

    return color;
}

// Pixel shader - applies glitch effect
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

    // Apply intensity scaling
    float effectStrength = influence * Intensity;

    // Direction from mouse
    float2 dir = dist > 0.001 ? toMouse / dist : float2(0, 0);

    // Time-based glitch variation
    float glitchTime = Time * GlitchFrequency;
    float glitchPhase = frac(glitchTime);

    // 1. Block displacement
    float2 blockOffset = blockGlitch(uv, BlockSize, Time) * effectStrength;

    // 2. Scan line distortion
    float scanOffset = scanLineDistortion(uv, Time) * effectStrength;

    // Apply distortions to UV
    float2 distortedUV = uv + float2(blockOffset.x + scanOffset, blockOffset.y);
    distortedUV = saturate(distortedUV);

    // 3. RGB split (chromatic aberration)
    float3 color;
    if (RgbSplitAmount > 0.0)
    {
        float splitAmount = RgbSplitAmount * effectStrength;
        color = rgbSplit(distortedUV, splitAmount, dir);
    }
    else
    {
        color = ScreenTexture.Sample(LinearSampler, distortedUV).rgb;
    }

    // 4. Color corruption
    color = colorCorruption(color, uv, Time);

    // 5. Noise overlay
    float noiseVal = noise(uv * 800.0 + Time * 10.0) * NoiseAmount * effectStrength;
    color += noiseVal;

    // 6. Digital artifacts - random bright pixels
    float artifactHash = hash(floor(uv * 500.0) + floor(glitchTime * 10.0));
    if (artifactHash > 0.98)
    {
        color += float3(1, 1, 1) * effectStrength * 0.5;
    }

    // 7. Temporal flickering
    float flicker = noise(float2(glitchTime * 20.0, 0)) * 0.2 + 0.9;
    color *= flicker;

    // Smooth edge fade
    float edgeFade = smoothstep(0.0, 0.3, influence);

    // Output with alpha based on influence
    float alpha = edgeFade * min(1.0, Intensity);

    return float4(color * HdrMultiplier, alpha);
}

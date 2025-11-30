// Water Ripple Shader
// Creates expanding water ripples with realistic wave interference via superposition

struct PSInput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

// Ripple data structure - must match C# RippleGPU struct
struct RippleData
{
    float2 Position;    // Center of ripple in screen pixels
    float Radius;       // Current outer radius (how far wave has traveled)
    float Amplitude;    // Current max amplitude (decays over lifetime)
    float Age;          // Time since spawn
    float Lifetime;     // Total lifetime
    float WaveSpeed;      // Per-ripple wave speed
    float InvWavelength;  // Precomputed: TWO_PI / wavelength (avoids division)
    float Damping;        // Per-ripple damping
    float Padding1;
    float Padding2;
    float Padding3;
};

cbuffer RippleParams : register(b0)
{
    float2 ViewportSize;
    float Time;
    int RippleCount;
    float WaveSpeed;
    float Wavelength;
    float Damping;
    float EnableGrid;
    float GridSpacing;
    float GridThickness;
    float GridPadding1;
    float GridPadding2;
    float4 GridColor;
};

Texture2D<float4> ScreenTexture : register(t0);
StructuredBuffer<RippleData> Ripples : register(t1);
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

// Pixel shader - applies water ripple distortion
float4 PSMain(PSInput input) : SV_TARGET
{
    float2 uv = input.TexCoord;
    float2 screenPos = uv * ViewportSize;

    // Accumulate total displacement from all ripples (wave superposition)
    float2 totalDisplacement = float2(0, 0);

    for (int i = 0; i < RippleCount; i++)
    {
        RippleData ripple = Ripples[i];

        // Skip inactive ripples
        if (ripple.Amplitude <= 0.0)
            continue;

        // Vector from ripple center to this pixel
        float2 toPixel = screenPos - ripple.Position;
        float dist = length(toPixel);

        // Skip if too far (wave hasn't reached here yet or has passed)
        // The wave front is at ripple.Radius, with some width behind it
        float waveWidth = ripple.Radius * 0.8; // Wave occupies 80% of radius
        float innerRadius = ripple.Radius - waveWidth;

        if (dist > ripple.Radius || dist < innerRadius * 0.5)
            continue;

        // Normalize direction
        float2 direction = dist > 0.001 ? toPixel / dist : float2(0, 0);

        // Calculate wave phase based on distance from center
        // Wave equation: sin(k*r) where k = 2*PI/wavelength (precomputed as InvWavelength)
        float phase = ripple.InvWavelength * dist;

        // Sine wave for the ripple
        float wave = sin(phase);

        // Amplitude decay:
        // 1. Decay with distance from wave front (strongest at front, weaker behind)
        float distFromFront = ripple.Radius - dist;
        float frontDecay = saturate(distFromFront / waveWidth);
        frontDecay = frontDecay * frontDecay; // Quadratic falloff

        // 2. Decay with age (amplitude decreases over lifetime)
        float ageDecay = 1.0 - (ripple.Age / ripple.Lifetime);
        ageDecay = ageDecay * ageDecay; // Quadratic for more natural fade

        // 3. Decay with distance from center (energy spreads out, using per-ripple damping)
        float spreadDecay = 1.0 / (1.0 + dist * ripple.Damping * 0.01);

        // Combine all decay factors
        float amplitude = ripple.Amplitude * frontDecay * ageDecay * spreadDecay;

        // Calculate displacement (radial, based on wave height)
        float2 displacement = direction * wave * amplitude;

        // Add to total (superposition - waves add/cancel naturally)
        totalDisplacement += displacement;
    }

    // Convert displacement from pixels to UV space
    float2 uvOffset = totalDisplacement / ViewportSize;

    // Apply displacement to UV
    float2 distortedUV = uv + uvOffset;

    // Clamp to valid UV range
    distortedUV = saturate(distortedUV);

    // Sample the screen at the distorted position
    float4 color = ScreenTexture.SampleLevel(LinearSampler, distortedUV, 0);

    // Optional grid overlay to visualize distortion
    if (EnableGrid > 0.5)
    {
        // Use distorted UV for grid to show the deformation
        float2 gridPos = distortedUV * ViewportSize;

        // Calculate distance to nearest grid line
        float2 gridFrac = frac(gridPos / GridSpacing);
        float2 gridDist = min(gridFrac, 1.0 - gridFrac) * GridSpacing;
        float minDist = min(gridDist.x, gridDist.y);

        // Draw line if within thickness
        float lineAlpha = 1.0 - smoothstep(0.0, GridThickness, minDist);

        // Blend grid color
        color.rgb = lerp(color.rgb, GridColor.rgb, lineAlpha * GridColor.a);
    }

    return float4(color.rgb, 1.0);
}

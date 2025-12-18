// Smoke Shader - Soft, wispy smoke particles with gaussian falloff

cbuffer FrameConstants : register(b0)
{
    float2 ViewportSize;
    float Time;
    float HdrMultiplier;
    float Softness;
    float Opacity;
    float2 Padding;
}

struct SmokeParticle
{
    float2 Position;
    float2 Velocity;
    float4 Color;
    float Size;
    float Lifetime;
    float MaxLifetime;
    float Age;
    float ExpansionRate;
    float TurbulencePhase;
    float RotationAngle;
    float InitialSize;
    float4 Padding;
};

StructuredBuffer<SmokeParticle> Particles : register(t0);

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 UV : TEXCOORD0;
    float4 Color : COLOR0;
    float Alpha : TEXCOORD1;
    float Softness : TEXCOORD2;
};

// Vertex shader - Generate quad per particle instance
VSOutput VSMain(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    VSOutput output;
    SmokeParticle particle = Particles[instanceId];

    // Skip dead particles
    if (particle.Lifetime <= 0)
    {
        output.Position = float4(0, 0, 0, 0);
        output.UV = float2(0, 0);
        output.Color = float4(0, 0, 0, 0);
        output.Alpha = 0;
        output.Softness = 0;
        return output;
    }

    // Calculate alpha based on lifetime (fade in and out)
    float lifeFraction = particle.Lifetime / particle.MaxLifetime;

    // Fade in quickly at birth
    float fadeIn = saturate((1.0 - lifeFraction) * 8.0);

    // Fade out slowly as it rises and dissipates
    float fadeOut = saturate(lifeFraction * 1.2);

    // Age-based fade (smoke becomes more transparent as it expands)
    float ageFade = saturate(1.0 - (particle.Age / particle.MaxLifetime) * 0.5);

    float alpha = min(fadeIn, fadeOut) * ageFade * Opacity;

    // Generate quad vertices (two triangles)
    float2 quadUV;
    if (vertexId == 0) quadUV = float2(-1, -1);
    else if (vertexId == 1) quadUV = float2(1, -1);
    else if (vertexId == 2) quadUV = float2(-1, 1);
    else if (vertexId == 3) quadUV = float2(-1, 1);
    else if (vertexId == 4) quadUV = float2(1, -1);
    else quadUV = float2(1, 1);

    // Apply slight rotation for organic look
    float c = cos(particle.RotationAngle);
    float s = sin(particle.RotationAngle);
    float2x2 rotation = float2x2(c, -s, s, c);
    float2 rotatedUV = mul(rotation, quadUV);

    // Scale by particle size
    float2 offset = rotatedUV * particle.Size;

    // Position in screen space
    float2 screenPos = particle.Position + offset;

    // Convert to NDC
    float2 ndc = (screenPos / ViewportSize) * 2.0 - 1.0;
    ndc.y = -ndc.y; // Flip Y for DirectX

    output.Position = float4(ndc, 0, 1);
    output.UV = quadUV; // Keep unrotated UV for SDF
    output.Color = particle.Color;
    output.Alpha = alpha;
    output.Softness = Softness;

    return output;
}

// Smooth noise function for organic smoke texture
float hash(float2 p)
{
    p = frac(p * float2(234.34, 435.345));
    p += dot(p, p + 34.23);
    return frac(p.x * p.y);
}

float noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f); // Smoothstep

    float a = hash(i);
    float b = hash(i + float2(1.0, 0.0));
    float c = hash(i + float2(0.0, 1.0));
    float d = hash(i + float2(1.0, 1.0));

    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

// Fractal Brownian Motion for wispy smoke texture
float fbm(float2 p)
{
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;

    for (int i = 0; i < 4; i++)
    {
        value += amplitude * noise(p * frequency);
        frequency *= 2.0;
        amplitude *= 0.5;
    }

    return value;
}

// Soft smoke puff SDF - Very soft gaussian-like falloff
float smokePuff(float2 p, float softness)
{
    float dist = length(p);

    // Multiple layers of falloff for very soft edges
    float core = exp(-dist * dist * (2.0 - softness * 1.5));
    float glow = exp(-dist * dist * 0.5);

    // Add noise texture for wispy appearance
    float noiseValue = fbm(p * 2.0 + float2(Time * 0.1, Time * 0.05));
    float wispy = noiseValue * 0.3;

    // Combine for final soft smoke appearance
    float intensity = core * 0.7 + glow * 0.3 + wispy;

    return saturate(intensity);
}

// Pixel shader - Render soft smoke with noise texture
float4 PSMain(VSOutput input) : SV_TARGET
{
    if (input.Alpha <= 0.001)
        discard;

    // Calculate soft smoke intensity with noise
    float intensity = smokePuff(input.UV, input.Softness);

    // Additional wispy detail at edges
    float edgeNoise = fbm(input.UV * 3.0 + float2(Time * 0.2, Time * 0.15));
    float edgeMask = 1.0 - saturate(length(input.UV) - 0.3);
    intensity += edgeNoise * edgeMask * 0.2;

    // Apply color
    float4 color = input.Color;
    color.rgb *= intensity;
    color.a = intensity * input.Alpha;

    // Very subtle flickering for organic feel
    float flicker = 0.95 + 0.05 * sin(Time * 3.0 + input.Position.x * 0.05);
    color.rgb *= flicker;

    // Apply HDR multiplier
    color.rgb *= HdrMultiplier;

    return color;
}

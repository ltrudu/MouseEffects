// Firework particle shader with instancing, glow, and trail effects

cbuffer FrameData : register(b0)
{
    float2 ViewportSize;
    float Time;
    float GlowIntensity;
    float EnableTrails;
    float TrailLength;
    float HdrMultiplier;
    float Padding1;
    float Padding2;
    float Padding3;
    float Padding4;
    float Padding5;
};

struct ParticleInstance
{
    float2 Position;
    float2 Velocity;
    float4 Color;
    float Size;
    float Life;
    float MaxLife;
    float Padding;
};

StructuredBuffer<ParticleInstance> Particles : register(t0);

struct VSOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR;
    float2 TexCoord : TEXCOORD0;
    float LifeFactor : TEXCOORD1;
    float IsTrail : TEXCOORD2;
};

// Vertex shader for particle quads
VSOutput VSMain(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    ParticleInstance particle = Particles[instanceId];

    // Skip dead particles
    if (particle.Life <= 0)
    {
        VSOutput output;
        output.Position = float4(0, 0, -2, 1);
        output.Color = float4(0, 0, 0, 0);
        output.TexCoord = float2(0, 0);
        output.LifeFactor = 0;
        output.IsTrail = 0;
        return output;
    }

    // Generate quad vertex (2 triangles = 6 vertices)
    float2 offsets[6] = {
        float2(-1, -1), float2(1, -1), float2(-1, 1),
        float2(-1, 1), float2(1, -1), float2(1, 1)
    };
    float2 texCoords[6] = {
        float2(0, 1), float2(1, 1), float2(0, 0),
        float2(0, 0), float2(1, 1), float2(1, 0)
    };

    float2 offset = offsets[vertexId];
    float2 texCoord = texCoords[vertexId];

    // Calculate life factor for fade
    float lifeFactor = particle.Life / particle.MaxLife;

    // Size with fade - particles shrink as they die
    float size = particle.Size * (0.3 + 0.7 * lifeFactor);

    float isTrail = 0.0;

    // Simple trail: stretch quad based on velocity magnitude (no rotation)
    if (EnableTrails > 0.5)
    {
        float speed = length(particle.Velocity);
        if (speed > 20.0)
        {
            isTrail = 1.0;
            float stretch = 1.0 + min(speed * TrailLength * 0.005, 2.0);

            // Stretch based on velocity direction
            float2 velNorm = particle.Velocity / speed;

            // Offset along velocity for trail effect
            // Negative offset.y vertices get pushed back along velocity
            if (offset.y < 0)
            {
                offset -= velNorm * stretch * 0.5;
            }
            else
            {
                offset += velNorm * stretch * 0.3;
            }
        }
    }

    // Convert to normalized device coordinates
    float2 screenPos = particle.Position + offset * size;
    float2 ndcPos = (screenPos / ViewportSize) * 2.0 - 1.0;
    ndcPos.y = -ndcPos.y;

    VSOutput output;
    output.Position = float4(ndcPos, 0, 1);
    output.Color = particle.Color;
    output.TexCoord = texCoord;
    output.LifeFactor = lifeFactor;
    output.IsTrail = isTrail;
    return output;
}

// Pixel shader - renders particle with glow effect
float4 PSMain(VSOutput input) : SV_TARGET
{
    float2 center = input.TexCoord - 0.5;
    float dist = length(center) * 2.0;

    // Softer falloff for trails
    float edgeSoftness = input.IsTrail > 0.5 ? 0.5 : 0.3;
    float alpha = 1.0 - smoothstep(edgeSoftness, 1.0, dist);

    // Add glow effect
    float glow = exp(-dist * dist * 2.0) * GlowIntensity;

    float finalAlpha = (alpha + glow) * input.LifeFactor;

    float4 color = input.Color;

    // Brighten core
    float coreBrightness = 1.0 + (1.0 - dist) * 0.5;
    color.rgb *= coreBrightness;

    // White hot core
    float coreWhite = (1.0 - smoothstep(0.0, 0.3, dist)) * 0.3 * input.LifeFactor;
    color.rgb += float3(coreWhite, coreWhite, coreWhite);

    // HDR boost - amplify bright areas for HDR displays
    float hdrBoost = 1.0 + glow * HdrMultiplier * 2.0;
    color.rgb *= hdrBoost;

    color.a = finalAlpha;

    if (color.a < 0.01)
        discard;

    return color;
}

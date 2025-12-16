// Particle trail shader with instancing

cbuffer FrameData : register(b0)
{
    float2 ViewportSize;
    float Time;
    float HdrMultiplier;
    float4 Padding;
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
};

// Vertex shader for particle quads
VSOutput VSMain(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    ParticleInstance particle = Particles[instanceId];

    // Skip dead particles
    if (particle.Life <= 0)
    {
        VSOutput output;
        output.Position = float4(0, 0, -2, 1); // Behind camera
        output.Color = float4(0, 0, 0, 0);
        output.TexCoord = float2(0, 0);
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

    // Calculate size with fade
    float lifeFactor = particle.Life / particle.MaxLife;
    float size = particle.Size * lifeFactor;

    // Convert to normalized device coordinates
    float2 screenPos = particle.Position + offset * size;
    float2 ndcPos = (screenPos / ViewportSize) * 2.0 - 1.0;
    ndcPos.y = -ndcPos.y; // Flip Y for screen coords

    VSOutput output;
    output.Position = float4(ndcPos, 0, 1);
    output.Color = particle.Color * lifeFactor; // Fade alpha
    output.TexCoord = texCoord;
    return output;
}

// Pixel shader - renders particle with soft circle
float4 PSMain(VSOutput input) : SV_TARGET
{
    // Distance from center
    float2 center = input.TexCoord - 0.5;
    float dist = length(center) * 2.0;

    // Soft circle falloff
    float alpha = 1.0 - smoothstep(0.5, 1.0, dist);

    // Glow effect for HDR
    float glow = exp(-dist * dist * 2.0);

    float4 color = input.Color;
    color.a *= alpha;

    // HDR boost - amplify bright core areas
    float hdrBoost = 1.0 + glow * HdrMultiplier * 1.5;
    color.rgb *= hdrBoost;

    return color;
}

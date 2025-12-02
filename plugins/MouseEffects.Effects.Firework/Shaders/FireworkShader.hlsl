// Firework particle shader with instancing, glow, and trail effects

cbuffer FrameData : register(b0)
{
    float2 ViewportSize;
    float Time;
    float GlowIntensity;
    float EnableTrails;
    float TrailLength;
    float EnableSparkle;    // Kept for compatibility but not used
    float SparkleIntensity; // Kept for compatibility but not used
    float Padding;
    float Padding2;
    float Padding3;
    float Padding4;
};

struct ParticleInstance
{
    float2 Position;
    float2 Velocity;
    float4 Color;
    float Size;
    float Life;
    float MaxLife;
    float SparklePhase;
};

StructuredBuffer<ParticleInstance> Particles : register(t0);

struct VSOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR;
    float2 TexCoord : TEXCOORD0;
    float LifeFactor : TEXCOORD1;
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
        output.LifeFactor = 0;
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

    // Add trail elongation if enabled
    if (EnableTrails > 0.5)
    {
        float speed = length(particle.Velocity);
        if (speed > 10.0)
        {
            float2 velDir = normalize(particle.Velocity);
            float elongation = min(speed * TrailLength * 0.01, 3.0);

            // Create perpendicular vector (tangent) - use consistent orientation
            float2 tangent = float2(velDir.y, -velDir.x);

            // Scale the offset: X is width (tangent direction), Y is length (velocity direction)
            // Stretch in the OPPOSITE direction of velocity (trail behind the particle)
            float2 stretchedOffset;
            stretchedOffset.x = offset.x; // Width stays the same
            stretchedOffset.y = offset.y * (1.0 + elongation); // Elongate in Y

            // Rotate to align with velocity direction
            offset.x = stretchedOffset.x * tangent.x + stretchedOffset.y * (-velDir.x);
            offset.y = stretchedOffset.x * tangent.y + stretchedOffset.y * (-velDir.y);
        }
    }

    // Convert to normalized device coordinates
    float2 screenPos = particle.Position + offset * size;
    float2 ndcPos = (screenPos / ViewportSize) * 2.0 - 1.0;
    ndcPos.y = -ndcPos.y; // Flip Y for screen coords

    VSOutput output;
    output.Position = float4(ndcPos, 0, 1);
    output.Color = particle.Color;
    output.TexCoord = texCoord;
    output.LifeFactor = lifeFactor;
    return output;
}

// Pixel shader - renders particle with glow effect
float4 PSMain(VSOutput input) : SV_TARGET
{
    // Distance from center
    float2 center = input.TexCoord - 0.5;
    float dist = length(center) * 2.0;

    // Base soft circle falloff
    float alpha = 1.0 - smoothstep(0.3, 1.0, dist);

    // Add glow effect (outer ring)
    float glow = exp(-dist * dist * 2.0) * GlowIntensity;

    // Combine effects
    float finalAlpha = (alpha + glow) * input.LifeFactor;

    // Color with intensity boost from glow
    float4 color = input.Color;

    // Brighten core
    float coreBrightness = 1.0 + (1.0 - dist) * 0.5;
    color.rgb *= coreBrightness;

    // Add white hot core for bright particles
    float coreWhite = (1.0 - smoothstep(0.0, 0.3, dist)) * 0.3 * input.LifeFactor;
    color.rgb += float3(coreWhite, coreWhite, coreWhite);

    color.a = finalAlpha;

    // Discard nearly invisible pixels
    if (color.a < 0.01)
        discard;

    return color;
}

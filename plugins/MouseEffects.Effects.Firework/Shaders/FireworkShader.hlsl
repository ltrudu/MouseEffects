// Firework particle shader with instancing, glow, trails, and sparkle effects

cbuffer FrameData : register(b0)
{
    float2 ViewportSize;
    float Time;
    float GlowIntensity;
    float EnableTrails;
    float TrailLength;
    float EnableSparkle;
    float SparkleIntensity;
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
    float2 Velocity : TEXCOORD1;
    float LifeFactor : TEXCOORD2;
    float SparklePhase : TEXCOORD3;
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
        output.Velocity = float2(0, 0);
        output.LifeFactor = 0;
        output.SparklePhase = 0;
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

            // Elongate in velocity direction
            float2 tangent = float2(-velDir.y, velDir.x);
            offset = offset.x * tangent + offset.y * velDir * (1.0 + elongation);
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
    output.Velocity = particle.Velocity;
    output.LifeFactor = lifeFactor;
    output.SparklePhase = particle.SparklePhase;
    return output;
}

// Pixel shader - renders particle with glow, sparkle, and color effects
float4 PSMain(VSOutput input) : SV_TARGET
{
    // Distance from center
    float2 center = input.TexCoord - 0.5;
    float dist = length(center) * 2.0;

    // Base soft circle falloff
    float alpha = 1.0 - smoothstep(0.3, 1.0, dist);

    // Add glow effect (outer ring)
    float glow = exp(-dist * dist * 2.0) * GlowIntensity;

    // Add sparkle effect
    float sparkle = 0.0;
    if (EnableSparkle > 0.5)
    {
        // Create sparkling effect using sin waves
        float sparkleT = input.SparklePhase + Time * 15.0;
        sparkle = (sin(sparkleT) * 0.5 + 0.5) * SparkleIntensity;

        // Make sparkles more pronounced near center
        sparkle *= (1.0 - dist) * (1.0 - dist);

        // Random-looking sparkle pattern
        float sparkle2 = sin(sparkleT * 1.7 + 1.3) * 0.5 + 0.5;
        float sparkle3 = sin(sparkleT * 2.3 + 2.7) * 0.5 + 0.5;
        sparkle = max(sparkle, sparkle2 * 0.7) * SparkleIntensity;
    }

    // Combine effects
    float finalAlpha = (alpha + glow + sparkle) * input.LifeFactor;

    // Color with intensity boost from glow
    float4 color = input.Color;

    // Brighten core
    float coreBrightness = 1.0 + (1.0 - dist) * 0.5;
    color.rgb *= coreBrightness;

    // Add white hot core for bright particles
    float coreWhite = (1.0 - smoothstep(0.0, 0.3, dist)) * 0.3 * input.LifeFactor;
    color.rgb += float3(coreWhite, coreWhite, coreWhite);

    // Apply sparkle brightness
    if (sparkle > 0.5)
    {
        color.rgb += float3(sparkle * 0.3, sparkle * 0.3, sparkle * 0.3);
    }

    color.a = finalAlpha;

    // Discard nearly invisible pixels
    if (color.a < 0.01)
        discard;

    return color;
}

// Gravity Well Shader - Particles with motion trails

cbuffer FrameConstants : register(b0)
{
    float2 ViewportSize;
    float Time;
    float HdrMultiplier;
    float4 Padding;
}

struct ParticleInstance
{
    float2 Position;
    float2 Velocity;
    float4 Color;
    float Size;
    float Mass;
    float TrailAlpha;
    float Lifetime;
    float RotationAngle;
    float AngularVelocity;
    float Padding1;
    float Padding2;
};

StructuredBuffer<ParticleInstance> Particles : register(t0);

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 UV : TEXCOORD0;
    float4 Color : COLOR0;
    float TrailAlpha : TEXCOORD1;
    float Size : TEXCOORD2;
};

// Vertex shader - Generate quad per particle instance
VSOutput VSMain(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    VSOutput output;
    ParticleInstance particle = Particles[instanceId];

    // Skip inactive particles
    if (particle.Lifetime <= 0 || particle.Size <= 0)
    {
        output.Position = float4(0, 0, 0, 0);
        output.UV = float2(0, 0);
        output.Color = float4(0, 0, 0, 0);
        output.TrailAlpha = 0;
        output.Size = 0;
        return output;
    }

    // Generate quad vertices (two triangles)
    float2 quadUV;
    if (vertexId == 0) quadUV = float2(-1, -1);
    else if (vertexId == 1) quadUV = float2(1, -1);
    else if (vertexId == 2) quadUV = float2(-1, 1);
    else if (vertexId == 3) quadUV = float2(-1, 1);
    else if (vertexId == 4) quadUV = float2(1, -1);
    else quadUV = float2(1, 1);

    // Apply rotation
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
    ndc.y = -ndc.y;

    output.Position = float4(ndc, 0, 1);
    output.UV = quadUV;
    output.Color = particle.Color;
    output.TrailAlpha = particle.TrailAlpha;
    output.Size = particle.Size;

    return output;
}

// Circle SDF for round particles
float sdCircle(float2 p, float r)
{
    return length(p) - r;
}

// Pixel shader - Render glowing particles with optional trails
float4 PSMain(VSOutput input) : SV_TARGET
{
    if (input.Size <= 0)
        discard;

    // Distance from center
    float dist = length(input.UV);

    // Core particle (bright center)
    float core = 1.0 - smoothstep(0.0, 0.2, dist);

    // Glow layers (soft halo)
    float glow1 = 1.0 - smoothstep(0.0, 0.5, dist);
    float glow2 = 1.0 - smoothstep(0.0, 0.8, dist);
    float glow3 = 1.0 - smoothstep(0.0, 1.0, dist);

    // Combine layers
    float intensity = core * 2.5 + glow1 * 1.0 + glow2 * 0.5 + glow3 * 0.2;

    // Add trail stretch effect based on velocity
    if (input.TrailAlpha > 0.01)
    {
        // Elongate particles in direction of motion to create trail effect
        float2 stretchUV = input.UV;
        stretchUV.x *= lerp(1.0, 0.3, input.TrailAlpha);
        float stretchDist = length(stretchUV);

        float trail = 1.0 - smoothstep(0.0, 1.2, stretchDist);
        intensity += trail * input.TrailAlpha * 0.8;
    }

    // Add subtle pulse based on time
    float pulse = 0.9 + 0.1 * sin(Time * 3.0 + input.Position.x * 0.05);
    intensity *= pulse;

    // Apply color
    float4 color = input.Color;
    color.rgb *= intensity;
    color.a = saturate(intensity);

    // Apply HDR multiplier
    color.rgb *= HdrMultiplier;

    // Add bright center highlight
    float highlight = 1.0 - smoothstep(0.0, 0.1, dist);
    color.rgb += highlight * float3(1, 1, 1) * 0.5 * HdrMultiplier;

    return color;
}

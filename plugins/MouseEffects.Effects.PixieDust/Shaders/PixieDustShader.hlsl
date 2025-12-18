// PixieDust Shader - Star-shaped sparkle particles with glow

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
    float Lifetime;
    float MaxLifetime;
    float RotationAngle;
    float GlowIntensity;
    float SpinSpeed;
    float BirthTime;
    float Padding;
};

StructuredBuffer<ParticleInstance> Particles : register(t0);

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 UV : TEXCOORD0;
    float4 Color : COLOR0;
    float Alpha : TEXCOORD1;
    float GlowIntensity : TEXCOORD2;
};

// Vertex shader - Generate quad per particle instance
VSOutput VSMain(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    VSOutput output;
    ParticleInstance particle = Particles[instanceId];

    // Skip dead particles
    if (particle.Lifetime <= 0)
    {
        output.Position = float4(0, 0, 0, 0);
        output.UV = float2(0, 0);
        output.Color = float4(0, 0, 0, 0);
        output.Alpha = 0;
        output.GlowIntensity = 0;
        return output;
    }

    // Calculate alpha based on lifetime (fade in and out)
    float lifeFraction = particle.Lifetime / particle.MaxLifetime;
    float fadeIn = saturate((1.0 - lifeFraction) * 5.0); // Quick fade in
    float fadeOut = saturate(lifeFraction * 2.0); // Slower fade out
    float alpha = min(fadeIn, fadeOut);

    // Generate quad vertices (two triangles)
    // Vertex order: 0-1-2, 2-1-3
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
    ndc.y = -ndc.y; // Flip Y for DirectX

    output.Position = float4(ndc, 0, 1);
    output.UV = quadUV; // Keep unrotated UV for SDF
    output.Color = particle.Color;
    output.Alpha = alpha;
    output.GlowIntensity = particle.GlowIntensity;

    return output;
}

// Star SDF - Creates a 5-pointed star shape
float sdStar(float2 p, float r, int n, float m)
{
    // Star with n points, r = radius, m = sharpness (0.5-1.0)
    static const float PI = 3.14159265359;

    // Convert to polar coordinates
    float a = atan2(p.y, p.x);
    float l = length(p);

    // Star angle calculation
    float an = PI / float(n);
    float en = PI / m;
    float acs = (floor(0.5 + a / an) * an);

    // Distance to star edge
    float2 p2 = float2(cos(acs), sin(acs)) * r;
    return length(p - p2) - r * 0.3;
}

// Diamond/4-pointed star SDF (simpler and faster)
float sdDiamond(float2 p, float size)
{
    float2 q = abs(p);
    return (q.x + q.y - size) / sqrt(2.0);
}

// Circle SDF for round sparkle option
float sdCircle(float2 p, float r)
{
    return length(p) - r;
}

// Pixel shader - Render star with glow
float4 PSMain(VSOutput input) : SV_TARGET
{
    if (input.Alpha <= 0.001)
        discard;

    // Use diamond shape for performance (can be replaced with sdStar for true stars)
    float dist = sdDiamond(input.UV, 0.5);

    // Create sharp core
    float core = 1.0 - smoothstep(0.0, 0.1, dist);

    // Create glow layers
    float glow1 = 1.0 - smoothstep(0.0, 0.4, dist);
    float glow2 = 1.0 - smoothstep(0.0, 0.7, dist);
    float glow3 = 1.0 - smoothstep(0.0, 1.0, dist);

    // Combine layers with different intensities
    float intensity = core * 2.0 + glow1 * 0.8 + glow2 * 0.4 + glow3 * 0.2;
    intensity *= input.GlowIntensity;

    // Add twinkle effect
    float twinkle = 0.8 + 0.2 * sin(Time * 5.0 + input.Position.x * 0.1 + input.Position.y * 0.1);
    intensity *= twinkle;

    // Apply color
    float4 color = input.Color;
    color.rgb *= intensity;
    color.a = intensity * input.Alpha;

    // Apply HDR multiplier for bright displays
    color.rgb *= HdrMultiplier;

    return color;
}

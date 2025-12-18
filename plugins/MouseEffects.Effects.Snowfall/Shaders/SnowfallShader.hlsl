// Snowfall Shader - Procedural 6-pointed snowflakes with glow

cbuffer FrameConstants : register(b0)
{
    float2 ViewportSize;
    float Time;
    float HdrMultiplier;
    float4 Padding;
}

struct SnowflakeInstance
{
    float2 Position;
    float2 Velocity;
    float4 Color;
    float Size;
    float Lifetime;
    float MaxLifetime;
    float RotationAngle;
    float RotationSpeed;
    float WindPhase;
    float GlowIntensity;
    float Padding;
};

StructuredBuffer<SnowflakeInstance> Snowflakes : register(t0);

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 UV : TEXCOORD0;
    float4 Color : COLOR0;
    float Alpha : TEXCOORD1;
    float GlowIntensity : TEXCOORD2;
};

// Vertex shader - Generate quad per snowflake instance
VSOutput VSMain(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    VSOutput output;
    SnowflakeInstance snowflake = Snowflakes[instanceId];

    // Skip dead snowflakes
    if (snowflake.Lifetime <= 0)
    {
        output.Position = float4(0, 0, 0, 0);
        output.UV = float2(0, 0);
        output.Color = float4(0, 0, 0, 0);
        output.Alpha = 0;
        output.GlowIntensity = 0;
        return output;
    }

    // Calculate alpha based on lifetime (fade in and out)
    float lifeFraction = snowflake.Lifetime / snowflake.MaxLifetime;
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
    float c = cos(snowflake.RotationAngle);
    float s = sin(snowflake.RotationAngle);
    float2x2 rotation = float2x2(c, -s, s, c);
    float2 rotatedUV = mul(rotation, quadUV);

    // Scale by snowflake size
    float2 offset = rotatedUV * snowflake.Size;

    // Position in screen space
    float2 screenPos = snowflake.Position + offset;

    // Convert to NDC
    float2 ndc = (screenPos / ViewportSize) * 2.0 - 1.0;
    ndc.y = -ndc.y; // Flip Y for DirectX

    output.Position = float4(ndc, 0, 1);
    output.UV = quadUV; // Keep unrotated UV for SDF
    output.Color = snowflake.Color;
    output.Alpha = alpha;
    output.GlowIntensity = snowflake.GlowIntensity;

    return output;
}

// Helper function: Rotate a 2D vector
float2 rotate2D(float2 p, float angle)
{
    float c = cos(angle);
    float s = sin(angle);
    return float2(c * p.x - s * p.y, s * p.x + c * p.y);
}

// Line segment SDF
float sdLineSegment(float2 p, float2 a, float2 b)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float h = saturate(dot(pa, ba) / dot(ba, ba));
    return length(pa - ba * h);
}

// 6-pointed snowflake SDF with branches
float SnowflakeSDF(float2 p, float size)
{
    static const float PI = 3.14159265359;
    float result = 1e10;

    // Create 6 main arms
    for (int i = 0; i < 6; i++)
    {
        float angle = float(i) * PI / 3.0;
        float2 dir = float2(cos(angle), sin(angle));

        // Main arm - from center to edge
        float2 armEnd = dir * size;
        float armDist = sdLineSegment(p, float2(0, 0), armEnd);
        result = min(result, armDist);

        // Add branches at different positions along the main arm
        float branchCount = 2.0; // Number of branch levels
        for (float b = 0.5; b <= branchCount; b += 1.0)
        {
            float branchPos = b / (branchCount + 1.0);
            float2 branchStart = dir * size * branchPos;
            float branchLength = size * 0.3 * (1.0 - branchPos); // Smaller branches toward the tip

            // Two branches per level (symmetric)
            float branchAngle = 0.5; // Radians

            // Left branch
            float2 branchDirLeft = rotate2D(dir, branchAngle);
            float2 branchEndLeft = branchStart + branchDirLeft * branchLength;
            float branchDistLeft = sdLineSegment(p, branchStart, branchEndLeft);
            result = min(result, branchDistLeft);

            // Right branch
            float2 branchDirRight = rotate2D(dir, -branchAngle);
            float2 branchEndRight = branchStart + branchDirRight * branchLength;
            float branchDistRight = sdLineSegment(p, branchStart, branchEndRight);
            result = min(result, branchDistRight);
        }
    }

    // Add a small circle at the center
    float centerDist = length(p) - size * 0.1;
    result = min(result, centerDist);

    return result;
}

// Pixel shader - Render snowflake with glow
float4 PSMain(VSOutput input) : SV_TARGET
{
    if (input.Alpha <= 0.001)
        discard;

    // Calculate snowflake SDF
    float dist = SnowflakeSDF(input.UV, 0.7);

    // Create sharp core
    float core = 1.0 - smoothstep(0.0, 0.05, dist);

    // Create glow layers
    float glow1 = 1.0 - smoothstep(0.0, 0.15, dist);
    float glow2 = 1.0 - smoothstep(0.0, 0.3, dist);
    float glow3 = 1.0 - smoothstep(0.0, 0.5, dist);

    // Combine layers with different intensities
    float intensity = core * 2.5 + glow1 * 1.2 + glow2 * 0.6 + glow3 * 0.3;
    intensity *= input.GlowIntensity;

    // Add subtle sparkle effect
    float sparkle = 0.9 + 0.1 * sin(Time * 3.0 + input.Position.x * 0.1 + input.Position.y * 0.1);
    intensity *= sparkle;

    // Apply color
    float4 color = input.Color;
    color.rgb *= intensity;
    color.a = saturate(intensity * input.Alpha * 0.9); // Slightly reduce alpha for better blending

    // Apply HDR multiplier for bright displays
    color.rgb *= HdrMultiplier;

    return color;
}

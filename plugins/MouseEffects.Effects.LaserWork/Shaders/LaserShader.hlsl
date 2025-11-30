// Laser Work Shader with instancing
// Renders glowing laser beams with additive blending

cbuffer FrameData : register(b0)
{
    float2 ViewportSize;
    float GlowIntensity;
    float MinAlpha;
    float MaxAlpha;
    float Padding;
    float Padding2;
    float Padding3;
};

struct LaserInstance
{
    float2 Position;
    float2 Direction;
    float4 Color;
    float Length;
    float Width;
    float Life;
    float MaxLife;
};

StructuredBuffer<LaserInstance> Lasers : register(t0);

struct VSOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR;
    float2 TexCoord : TEXCOORD0;
};

// Vertex shader for laser quads
VSOutput VSMain(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    LaserInstance laser = Lasers[instanceId];

    // Skip dead lasers
    if (laser.Life <= 0)
    {
        VSOutput output;
        output.Position = float4(0, 0, -2, 1); // Behind camera
        output.Color = float4(0, 0, 0, 0);
        output.TexCoord = float2(0, 0);
        return output;
    }

    // Generate quad vertices (2 triangles = 6 vertices)
    // Laser extends from Position along Direction for Length pixels
    // Width defines the thickness perpendicular to direction

    // Local offsets: x along length, y perpendicular (width)
    float2 localOffsets[6] = {
        float2(0, -0.5),  // Start, bottom
        float2(1, -0.5),  // End, bottom
        float2(0, 0.5),   // Start, top
        float2(0, 0.5),   // Start, top
        float2(1, -0.5),  // End, bottom
        float2(1, 0.5)    // End, top
    };

    float2 texCoords[6] = {
        float2(0, 0),
        float2(1, 0),
        float2(0, 1),
        float2(0, 1),
        float2(1, 0),
        float2(1, 1)
    };

    float2 localOffset = localOffsets[vertexId];
    float2 texCoord = texCoords[vertexId];

    // Calculate perpendicular direction for width
    float2 perp = float2(-laser.Direction.y, laser.Direction.x);

    // Transform local offset to world space
    // X offset along laser direction (scaled by length)
    // Y offset along perpendicular (scaled by width)
    float2 worldOffset = laser.Direction * localOffset.x * laser.Length
                       + perp * localOffset.y * laser.Width;

    float2 screenPos = laser.Position + worldOffset;

    // Convert to normalized device coordinates
    float2 ndcPos = (screenPos / ViewportSize) * 2.0 - 1.0;
    ndcPos.y = -ndcPos.y; // Flip Y for screen coords

    VSOutput output;
    output.Position = float4(ndcPos, 0, 1);
    output.Color = laser.Color;
    output.TexCoord = texCoord;
    return output;
}

// Pixel shader - renders laser with glow effect
float4 PSMain(VSOutput input) : SV_TARGET
{
    // TexCoord.x goes from 0 (start) to 1 (end) along laser
    // TexCoord.y goes from 0 (one edge) to 1 (other edge) across width

    // Distance from center line (width-wise)
    float distFromCenter = abs(input.TexCoord.y - 0.5) * 2.0;

    // Create glow falloff - brighter in center, dimmer at edges
    float coreBrightness = 1.0 - pow(distFromCenter, 2.0);
    float glowFalloff = exp(-distFromCenter * 3.0);

    // Combine core and glow
    float intensity = coreBrightness + glowFalloff * GlowIntensity;

    // Apply color with intensity and alpha
    float4 color = input.Color;
    color.rgb *= intensity;
    color.a *= saturate(intensity);

    // Add extra bloom/glow to the center
    float centerGlow = exp(-distFromCenter * 5.0) * GlowIntensity * 0.5;
    color.rgb += color.rgb * centerGlow;

    return color;
}

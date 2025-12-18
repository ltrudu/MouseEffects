// Retro 8-bit pixel explosion shader with sharp, chunky pixels

cbuffer FrameData : register(b0)
{
    float2 ViewportSize;
    float Time;
    float Padding1;
    float Padding2;
    float Padding3;
    float Padding4;
    float Padding5;
};

struct PixelInstance
{
    float2 Position;
    float2 Velocity;
    float4 Color;
    float Size;
    float Life;
    float MaxLife;
    float Rotation;
};

StructuredBuffer<PixelInstance> Pixels : register(t0);

struct VSOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR;
    float2 TexCoord : TEXCOORD0;
    float LifeFactor : TEXCOORD1;
};

// Vertex shader for pixel quads
VSOutput VSMain(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    PixelInstance pixel = Pixels[instanceId];

    // Skip dead pixels
    if (pixel.Life <= 0)
    {
        VSOutput output;
        output.Position = float4(0, 0, -2, 1);
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
    float lifeFactor = pixel.Life / pixel.MaxLife;

    // Apply rotation for tumbling effect
    float c = cos(pixel.Rotation);
    float s = sin(pixel.Rotation);
    float2 rotated = float2(
        offset.x * c - offset.y * s,
        offset.x * s + offset.y * c
    );

    // Convert to normalized device coordinates
    float2 screenPos = pixel.Position + rotated * pixel.Size;
    float2 ndcPos = (screenPos / ViewportSize) * 2.0 - 1.0;
    ndcPos.y = -ndcPos.y;

    VSOutput output;
    output.Position = float4(ndcPos, 0, 1);
    output.Color = pixel.Color;
    output.TexCoord = texCoord;
    output.LifeFactor = lifeFactor;
    return output;
}

// Pixel shader - renders sharp square pixels with no anti-aliasing for retro feel
float4 PSMain(VSOutput input) : SV_TARGET
{
    // Center coordinates from -1 to 1
    float2 p = (input.TexCoord - 0.5) * 2.0;

    // Sharp square - no smoothing, just hard edges for 8-bit aesthetic
    float squareDist = max(abs(p.x), abs(p.y));

    // Hard cutoff at edge - creates crisp pixel boundaries
    if (squareDist > 1.0)
        discard;

    // Apply life-based fade
    float alpha = input.LifeFactor;

    // Slight brightness variation based on life - pixels dim as they age
    float brightness = 0.7 + 0.3 * input.LifeFactor;

    float4 color = input.Color;
    color.rgb *= brightness;
    color.a = alpha;

    if (color.a < 0.01)
        discard;

    return color;
}

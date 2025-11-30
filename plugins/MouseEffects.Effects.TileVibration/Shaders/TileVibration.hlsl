// Tile Vibration Shader
// Creates vibrating, shrinking tiles that show captured screen content

struct PSInput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

// Tile data structure - must match C# TileGPU struct
struct TileData
{
    float2 Position;    // Center position in screen pixels
    float Age;          // Normalized age (0-1, where 1 = expired)
    float Width;        // Current width in pixels
    float Height;       // Current height in pixels
    float RandomSeed;   // For randomized vibration
    float Padding1;
    float Padding2;
};

cbuffer TileParams : register(b0)
{
    float2 ViewportSize;       // Viewport size in pixels
    float Time;                // Time for animation (already multiplied by speed)
    int TileCount;             // Number of active tiles
    int VibrationFlags;        // Bitmask: 1=displacement, 2=zoom, 4=rotation
    float DisplacementMax;     // Max pixels of displacement
    float ZoomMin;             // Min zoom scale
    float ZoomMax;             // Max zoom scale
    float RotationAmplitude;   // Max rotation in radians
    int EdgeStyle;             // 0=Sharp, 1=Soft
    int OutlineEnabled;        // 1=enabled, 0=disabled
    float OutlineSize;         // Outline thickness in pixels
    float4 OutlineColor;       // Outline RGBA color
    float4 Padding1;
    float4 Padding2;
};

Texture2D<float4> ScreenTexture : register(t0);
StructuredBuffer<TileData> Tiles : register(t1);
SamplerState LinearSampler : register(s0);

// Rotate a 2D vector around origin
float2 Rotate2D(float2 pt, float ang)
{
    float s = sin(ang);
    float c = cos(ang);
    return float2(
        pt.x * c - pt.y * s,
        pt.x * s + pt.y * c
    );
}

// Vertex shader - generates fullscreen quad procedurally
PSInput VSMain(uint vertexId : SV_VertexID)
{
    PSInput output;

    // Generate fullscreen triangle strip: 0,1,2,3 -> positions
    float2 uv = float2((vertexId << 1) & 2, vertexId & 2);
    output.Position = float4(uv * 2.0 - 1.0, 0.0, 1.0);
    output.Position.y = -output.Position.y; // Flip Y for DirectX
    output.TexCoord = uv;

    return output;
}

// Pixel shader - renders tiles over screen content
float4 PSMain(PSInput input) : SV_TARGET
{
    float2 uv = input.TexCoord;
    float2 screenPos = uv * ViewportSize;

    // Start with the original screen color (use SampleLevel to avoid gradient issues in loop)
    float4 result = ScreenTexture.SampleLevel(LinearSampler, uv, 0);

    // Track coverage for early-out optimization
    float totalCoverage = 0.0;

    // Check each tile (tiles are sorted oldest first, so newer ones render on top)
    for (int i = 0; i < TileCount; i++)
    {
        // Early-out: if pixel is fully covered, no need to process more tiles
        if (totalCoverage >= 0.99)
            break;
        TileData tile = Tiles[i];

        // Skip expired tiles
        if (tile.Age >= 1.0)
            continue;

        // Calculate distance from tile center
        float2 toTile = screenPos - tile.Position;
        float2 halfSize = float2(tile.Width, tile.Height) * 0.5;

        // Check if pixel is within tile bounds (using axis-aligned rectangle)
        float2 absToTile = abs(toTile);

        // Calculate outline bounds (slightly larger than tile)
        float2 outlineHalfSize = halfSize + OutlineSize;
        bool inOutlineZone = OutlineEnabled &&
                             absToTile.x < outlineHalfSize.x &&
                             absToTile.y < outlineHalfSize.y &&
                             (absToTile.x >= halfSize.x || absToTile.y >= halfSize.y);

        bool inTile = absToTile.x < halfSize.x && absToTile.y < halfSize.y;

        if (inOutlineZone)
        {
            // Draw outline
            float ageFade = 1.0 - tile.Age;
            float4 outline = OutlineColor;
            outline.a *= ageFade;
            result = lerp(result, float4(outline.rgb, 1.0), outline.a);
        }
        else if (inTile)
        {
            // Pixel is inside this tile
            // Calculate normalized position within tile (-1 to 1)
            float2 tileUV = toTile / halfSize;

            // Calculate vibration effects based on time and seed
            float seed = tile.RandomSeed;

            // Displacement vibration
            float2 displacement = float2(0, 0);
            if (VibrationFlags & 1)
            {
                displacement.x = sin(Time * 20.0 + seed * 100.0) * DisplacementMax;
                displacement.y = cos(Time * 17.0 + seed * 73.0) * DisplacementMax;
            }

            // Zoom vibration
            float zoom = 1.0;
            if (VibrationFlags & 2)
            {
                float zoomT = sin(Time * 15.0 + seed * 50.0) * 0.5 + 0.5;
                zoom = lerp(ZoomMin, ZoomMax, zoomT);
            }

            // Rotation vibration
            float rotation = 0.0;
            if (VibrationFlags & 4)
            {
                rotation = sin(Time * 12.0 + seed * 37.0) * RotationAmplitude;
            }

            // Transform UV for sampling
            // 1. Apply zoom (scale from center)
            float2 transformedUV = tileUV / zoom;

            // 2. Apply rotation
            if (rotation != 0.0)
            {
                transformedUV = Rotate2D(transformedUV, rotation);
            }

            // Convert back to tile-local coordinates and then to screen coordinates
            float2 samplePos = tile.Position + transformedUV * halfSize + displacement;

            // Clamp to screen bounds
            samplePos = clamp(samplePos, float2(0, 0), ViewportSize - 1);

            // Convert to UV for sampling
            float2 sampleUV = samplePos / ViewportSize;

            // Sample the screen texture at the transformed position (SampleLevel for loop compatibility)
            float4 tileColor = ScreenTexture.SampleLevel(LinearSampler, sampleUV, 0);

            // Calculate edge mask
            float mask = 1.0;
            if (EdgeStyle == 1) // Soft edges
            {
                // Calculate distance from edge (0 at edge, 1 at center)
                float2 edgeDist = 1.0 - absToTile / halfSize;
                float minEdge = min(edgeDist.x, edgeDist.y);
                mask = smoothstep(0.0, 0.3, minEdge);
            }

            // Apply fade based on age
            float ageFade = 1.0 - tile.Age;
            mask *= ageFade;

            // Blend tile over result
            result = lerp(result, tileColor, mask);

            // Update coverage for early-out (approximate coverage accumulation)
            totalCoverage = totalCoverage + mask * (1.0 - totalCoverage);
        }
    }

    return float4(result.rgb, 1.0);
}

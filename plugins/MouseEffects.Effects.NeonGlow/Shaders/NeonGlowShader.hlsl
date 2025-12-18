// NeonGlow Shader - 80s Synthwave Neon Trails with Multilayer Bloom

cbuffer Constants : register(b0)
{
    float2 ViewportSize;      // 8 bytes
    float Time;               // 4 bytes
    float LineThickness;      // 4 bytes = 16
    float GlowIntensity;      // 4 bytes
    int GlowLayers;           // 4 bytes
    float FadeSpeed;          // 4 bytes
    float SmoothingFactor;    // 4 bytes = 32
    float4 PrimaryColor;      // 16 bytes = 48
    float4 SecondaryColor;    // 16 bytes = 64
    float HdrMultiplier;      // 4 bytes
    int ColorMode;            // 4 bytes (0=fixed, 1=rainbow, 2=gradient)
    float RainbowSpeed;       // 4 bytes
    float Padding;            // 4 bytes = 80
}

struct TrailPoint
{
    float2 Position;          // 8 bytes
    float Age;                // 4 bytes (0 = newest, increases over time)
    float MaxAge;             // 4 bytes = 16
    float4 Color;             // 16 bytes = 32
};

StructuredBuffer<TrailPoint> TrailPoints : register(t0);

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

// Fullscreen triangle vertex shader
VSOutput VSMain(uint vertexId : SV_VertexID)
{
    VSOutput output;
    float2 uv = float2((vertexId << 1) & 2, vertexId & 2);
    output.Position = float4(uv * 2.0 - 1.0, 0.0, 1.0);
    output.Position.y = -output.Position.y;
    output.TexCoord = uv;
    return output;
}

// Signed distance to line segment
float sdSegment(float2 p, float2 a, float2 b)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float h = saturate(dot(pa, ba) / dot(ba, ba));
    return length(pa - ba * h);
}

// Smooth exponential glow function
float expGlow(float dist, float thickness, float falloff)
{
    return exp(-dist * falloff / thickness);
}

// Pixel shader - draws smooth neon trail with multilayer glow
float4 PSMain(VSOutput input) : SV_TARGET
{
    float2 pixelPos = input.TexCoord * ViewportSize;
    float4 finalColor = float4(0, 0, 0, 0);

    // Iterate through trail segments
    float minDist = 999999.0;
    float4 closestColor = PrimaryColor;
    float closestAlpha = 0.0;

    // Process trail points pairwise to create line segments
    for (int i = 0; i < 511; i++)
    {
        TrailPoint p0 = TrailPoints[i];
        TrailPoint p1 = TrailPoints[i + 1];

        // Skip inactive points
        if (p0.MaxAge <= 0 || p1.MaxAge <= 0) continue;
        if (p0.Age >= p0.MaxAge || p1.Age >= p1.MaxAge) continue;

        // Calculate distance to line segment
        float dist = sdSegment(pixelPos, p0.Position, p1.Position);

        // Calculate fade based on age (newer = brighter)
        float fade0 = 1.0 - saturate(p0.Age / p0.MaxAge);
        float fade1 = 1.0 - saturate(p1.Age / p1.MaxAge);
        float avgFade = (fade0 + fade1) * 0.5;

        // Track closest segment for color
        if (dist < minDist)
        {
            minDist = dist;
            closestColor = lerp(p0.Color, p1.Color, 0.5);
            closestAlpha = avgFade;
        }
    }

    // If we found a segment, apply multilayer glow
    if (minDist < 999999.0)
    {
        // Core line (sharp, bright)
        float core = expGlow(minDist, LineThickness, 8.0) * closestAlpha;

        // Multiple glow layers with different falloffs
        float glow = 0.0;

        if (GlowLayers >= 1)
            glow += expGlow(minDist, LineThickness * 2.0, 3.0) * 0.8;
        if (GlowLayers >= 2)
            glow += expGlow(minDist, LineThickness * 4.0, 1.5) * 0.6;
        if (GlowLayers >= 3)
            glow += expGlow(minDist, LineThickness * 8.0, 0.8) * 0.4;
        if (GlowLayers >= 4)
            glow += expGlow(minDist, LineThickness * 16.0, 0.4) * 0.3;
        if (GlowLayers >= 5)
            glow += expGlow(minDist, LineThickness * 32.0, 0.2) * 0.2;

        glow *= closestAlpha * GlowIntensity;

        // Combine core and glow
        float totalIntensity = saturate(core + glow);

        // Apply color with HDR support
        finalColor = closestColor * totalIntensity * HdrMultiplier;
        finalColor.a = totalIntensity;
    }

    return finalColor;
}

// CometTrail Shader - Blazing comet with fiery tail and sparks

cbuffer Constants : register(b0)
{
    float2 ViewportSize;      // 8 bytes
    float Time;               // 4 bytes
    float HeadSize;           // 4 bytes = 16
    float TrailWidth;         // 4 bytes
    float GlowIntensity;      // 4 bytes
    float ColorTemperature;   // 4 bytes (0 = cooler, 1 = hotter)
    float FadeSpeed;          // 4 bytes = 32
    int SparkCount;           // 4 bytes
    float SparkSize;          // 4 bytes
    float SmoothingFactor;    // 4 bytes
    float HdrMultiplier;      // 4 bytes = 48
    float2 MousePosition;     // 8 bytes
    float Padding1;           // 4 bytes
    float Padding2;           // 4 bytes = 64
    float4 CoreColor;         // 16 bytes = 80 (white core)
    float4 EdgeColor;         // 16 bytes = 96 (dark red edge)
}

struct TrailPoint
{
    float2 Position;          // 8 bytes
    float Age;                // 4 bytes
    float MaxAge;             // 4 bytes = 16
    float4 Color;             // 16 bytes = 32
};

struct Spark
{
    float2 Position;          // 8 bytes
    float2 Velocity;          // 8 bytes = 16
    float Age;                // 4 bytes
    float MaxAge;             // 4 bytes
    float Size;               // 4 bytes
    float Brightness;         // 4 bytes = 32
};

StructuredBuffer<TrailPoint> TrailPoints : register(t0);
StructuredBuffer<Spark> Sparks : register(t1);

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

// Fire gradient based on distance (0 = core, 1 = edge)
float4 getFireColor(float t)
{
    // Fire gradient: White -> Yellow -> Orange -> Red -> Dark Red
    t = saturate(t);

    if (t < 0.2)  // White to Yellow
    {
        float s = t / 0.2;
        return float4(1.0, 1.0, 1.0 - s * 0.5, 1.0);
    }
    else if (t < 0.4)  // Yellow to Orange
    {
        float s = (t - 0.2) / 0.2;
        return float4(1.0, 1.0 - s * 0.45, 0.5 - s * 0.5, 1.0);
    }
    else if (t < 0.6)  // Orange to Red
    {
        float s = (t - 0.4) / 0.2;
        return float4(1.0, 0.55 - s * 0.27, 0.0, 1.0);
    }
    else if (t < 0.8)  // Red to Dark Red
    {
        float s = (t - 0.6) / 0.2;
        return float4(1.0 - s * 0.45, 0.28 - s * 0.28, 0.0, 1.0);
    }
    else  // Dark Red fade
    {
        float s = (t - 0.8) / 0.2;
        return float4(0.55 - s * 0.01, 0.0, 0.0, 1.0 - s * 0.5);
    }
}

// Noise function for fire turbulence
float noise(float2 p)
{
    return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
}

// Pixel shader - draws blazing comet with fiery trail and sparks
float4 PSMain(VSOutput input) : SV_TARGET
{
    float2 pixelPos = input.TexCoord * ViewportSize;
    float4 finalColor = float4(0, 0, 0, 0);

    // Draw the trail
    float minDist = 999999.0;
    float4 closestColor = CoreColor;
    float closestAlpha = 0.0;
    float closestAge = 0.0;

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
        float avgAge = (p0.Age + p1.Age) * 0.5;

        // Track closest segment
        if (dist < minDist)
        {
            minDist = dist;
            closestColor = lerp(p0.Color, p1.Color, 0.5);
            closestAlpha = avgFade;
            closestAge = avgAge;
        }
    }

    // If we found a trail segment, apply fire effect
    if (minDist < 999999.0)
    {
        // Add turbulence to make the fire look more dynamic
        float turbulence = noise(pixelPos * 0.05 + Time * 2.0) * 0.3;
        float effectiveDist = minDist + turbulence * TrailWidth * 0.5;

        // Calculate fire gradient based on distance from trail center
        float distFactor = saturate(effectiveDist / (TrailWidth * 3.0));

        // Core trail with exponential falloff
        float core = expGlow(effectiveDist, TrailWidth, 4.0) * closestAlpha;

        // Multiple glow layers for fire effect
        float glow = 0.0;
        glow += expGlow(effectiveDist, TrailWidth * 2.0, 2.0) * 0.8;
        glow += expGlow(effectiveDist, TrailWidth * 4.0, 1.0) * 0.6;
        glow += expGlow(effectiveDist, TrailWidth * 8.0, 0.5) * 0.4;
        glow *= closestAlpha * GlowIntensity;

        // Get fire color based on distance (core = hot white, edge = dark red)
        float4 fireColor = getFireColor(distFactor);

        // Apply temperature adjustment
        fireColor = lerp(fireColor, closestColor, ColorTemperature);

        // Combine core and glow
        float totalIntensity = saturate(core + glow);

        // Apply color with HDR support
        finalColor = fireColor * totalIntensity * HdrMultiplier;
        finalColor.a = totalIntensity;
    }

    // Draw the comet head (bright core at mouse position)
    float headDist = length(pixelPos - MousePosition);
    if (headDist < HeadSize * 3.0)
    {
        // Bright white-hot core
        float headCore = expGlow(headDist, HeadSize * 0.5, 6.0);

        // Multiple glow layers for intense bloom
        float headGlow = 0.0;
        headGlow += expGlow(headDist, HeadSize * 1.0, 3.0) * 0.9;
        headGlow += expGlow(headDist, HeadSize * 2.0, 1.5) * 0.7;
        headGlow += expGlow(headDist, HeadSize * 3.0, 0.8) * 0.5;
        headGlow *= GlowIntensity * 1.2;

        // Head color (very hot white core with slight yellow/orange tint)
        float headDistFactor = saturate(headDist / (HeadSize * 2.0));
        float4 headColor = getFireColor(headDistFactor * 0.5); // Stay in hot range

        float headIntensity = saturate(headCore + headGlow);
        float4 headFinalColor = headColor * headIntensity * HdrMultiplier * 1.5;
        headFinalColor.a = headIntensity;

        // Blend head with trail (additive)
        finalColor.rgb += headFinalColor.rgb;
        finalColor.a = saturate(finalColor.a + headFinalColor.a);
    }

    // Draw sparks
    for (int j = 0; j < 256; j++)
    {
        Spark spark = Sparks[j];

        // Skip inactive sparks
        if (spark.Age >= spark.MaxAge || spark.MaxAge <= 0) continue;

        float sparkDist = length(pixelPos - spark.Position);
        float sparkFade = 1.0 - saturate(spark.Age / spark.MaxAge);

        if (sparkDist < spark.Size * 3.0)
        {
            // Spark glow
            float sparkGlow = expGlow(sparkDist, spark.Size, 2.0);
            sparkGlow += expGlow(sparkDist, spark.Size * 2.0, 1.0) * 0.5;
            sparkGlow *= sparkFade * spark.Brightness;

            // Sparks are orange-red embers
            float4 sparkColor = getFireColor(0.4 + sparkFade * 0.3); // Orange to red

            float4 sparkFinalColor = sparkColor * sparkGlow * HdrMultiplier * GlowIntensity * 0.8;
            sparkFinalColor.a = sparkGlow;

            // Blend spark with final color (additive)
            finalColor.rgb += sparkFinalColor.rgb;
            finalColor.a = saturate(finalColor.a + sparkFinalColor.a);
        }
    }

    return finalColor;
}

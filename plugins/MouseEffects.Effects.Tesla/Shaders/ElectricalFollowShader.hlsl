// Electrical Follow Trail Shader
// Renders connected electrical lines between trail points with branch bolts and sparkles

static const float TAU = 6.28318530718;
static const float PI = 3.14159265359;

cbuffer TrailConstants : register(b0)
{
    float2 ViewportSize;          // 8 bytes
    float Time;                   // 4 bytes
    float GlowIntensity;          // 4 bytes = 16
    float FlickerSpeed;           // 4 bytes
    float FlickerIntensity;       // 4 bytes
    float CrackleIntensity;       // 4 bytes
    float LineThickness;          // 4 bytes = 32
    float4 PrimaryColor;          // 16 bytes = 48
    float4 SecondaryColor;        // 16 bytes = 64
    float BurstProbability;       // 4 bytes
    float BurstIntensity;         // 4 bytes
    float NoiseScale;             // 4 bytes
    float BranchBoltEnabled;      // 4 bytes = 80
    float BranchBoltCount;        // 4 bytes
    float BranchBoltLength;       // 4 bytes
    float BranchBoltThickness;    // 4 bytes
    float BranchBoltSpread;       // 4 bytes = 96
    float SparkleEnabled;         // 4 bytes
    float SparkleCount;           // 4 bytes
    float SparkleSize;            // 4 bytes
    float SparkleIntensity;       // 4 bytes = 112
    float4 BranchBoltColor;       // 16 bytes = 128
    float4 SparkleColor;          // 16 bytes = 144
};

struct TrailPoint
{
    float2 Position;
    float Lifetime;
    float MaxLifetime;
    float4 Color;
};

StructuredBuffer<TrailPoint> TrailPoints : register(t0);

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
    float2 ScreenPos : TEXCOORD1;
};

// ===== Helper Functions =====

float RandomFloat(float2 seed)
{
    seed = sin(seed * float2(123.45, 546.23)) * 345.21 + 12.57;
    return frac(seed.x * seed.y);
}

float2 RandomFloat2(float2 seed)
{
    return float2(
        RandomFloat(seed),
        RandomFloat(seed * 1.7320508)
    );
}

float SimpleNoise(float2 uv, float octaves)
{
    float sn = 0.0;
    float amplitude = 3.0;
    float deno = 0.0;
    octaves = clamp(octaves, 1.0, 6.0);

    for (float i = 1.0; i <= octaves; i++)
    {
        float2 grid = smoothstep(0.0, 1.0, frac(uv));
        float2 id = floor(uv);
        float2 offs = float2(0.0, 1.0);
        float bl = RandomFloat(id);
        float br = RandomFloat(id + offs.yx);
        float tl = RandomFloat(id + offs);
        float tr = RandomFloat(id + offs.yy);
        sn += lerp(lerp(bl, br, grid.x), lerp(tl, tr, grid.x), grid.y) * amplitude;
        deno += amplitude;
        uv *= 3.5;
        amplitude *= 0.5;
    }
    return sn / deno;
}

float LineSDF(float2 p, float2 a, float2 b, float thickness)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h) - thickness;
}

// Get the parameter t along the line segment (0 = at point a, 1 = at point b)
float LineParameter(float2 p, float2 a, float2 b)
{
    float2 pa = p - a;
    float2 ba = b - a;
    return clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
}

// Rotate a 2D vector
float2 Rotate(float2 v, float angle)
{
    float c = cos(angle);
    float s = sin(angle);
    return float2(v.x * c - v.y * s, v.x * s + v.y * c);
}

// Render a sparkle (bright point with glow)
float3 RenderSparkle(float2 screenPos, float2 sparklePos, float3 color, float size, float intensity, float seed)
{
    float dist = length(screenPos - sparklePos);

    // Core bright point
    float core = smoothstep(size * 2.0, 0.0, dist);

    // Outer glow
    float glow = intensity * 3.0 / max(1.0, dist * 0.5);
    glow = saturate(glow);

    // Twinkle effect
    float twinkle = sin(Time * 20.0 + seed * TAU) * 0.5 + 0.5;
    twinkle = lerp(0.5, 1.0, twinkle);

    // Random flash
    float flash = RandomFloat(float2(floor(Time * 15.0 + seed * 50.0), seed));
    flash = flash > 0.7 ? 1.5 : 1.0;

    return color * (core * 2.0 + glow) * twinkle * flash * intensity;
}

// Render a branch bolt (jagged lightning line)
float3 RenderBranchBolt(float2 screenPos, float2 origin, float angle, float boltLength, float3 color, float thickness, float seed)
{
    float3 result = float3(0.0, 0.0, 0.0);

    // Direction of the bolt
    float2 dir = float2(cos(angle), sin(angle));
    float2 perp = float2(-dir.y, dir.x);

    // Create jagged segments
    int numSegments = 4;
    float segLen = boltLength / float(numSegments);
    float2 prevPoint = origin;

    for (int i = 0; i < numSegments; i++)
    {
        // Random displacement for jagged look
        float segSeed = seed + float(i) * 0.1;
        float displacement = (RandomFloat(float2(segSeed, segSeed * 2.3)) - 0.5) * segLen * 0.8;

        // Animated displacement
        float animDisp = sin(Time * 15.0 + segSeed * TAU) * segLen * 0.3;
        displacement += animDisp;

        // Next point along bolt with perpendicular displacement
        float2 nextPoint = prevPoint + dir * segLen + perp * displacement;

        // Taper thickness toward end
        float taperFactor = 1.0 - float(i) / float(numSegments);
        float segThickness = thickness * taperFactor * 0.5;

        // Render this segment
        float lineDist = LineSDF(screenPos, prevPoint, nextPoint, segThickness);

        // Glow for this segment
        float3 segGlow = GlowIntensity * 5.0 / max(0.5, lineDist) * color;
        segGlow = saturate(1.0 - exp(segGlow * -0.05));
        segGlow *= taperFactor; // Fade toward end

        result += segGlow;

        prevPoint = nextPoint;
    }

    // Flicker the whole bolt
    float flickerPhase = FlickerSpeed * Time * TAU * 1.5;
    float flickerRand = RandomFloat(float2(seed * 2.0, seed)) * TAU;
    float flicker = sin(flickerPhase + flickerRand) * 0.5 + 0.5;
    flicker = lerp(0.6, 1.0, flicker);

    // Random crackle
    float crackle = RandomFloat(float2(floor(Time * 25.0 + seed * 80.0), seed));
    crackle = crackle > 0.8 ? 0.2 : 1.0;

    result *= flicker * crackle;

    return result;
}

// Render an electrical line segment between two points
float3 RenderElectricalSegment(float2 screenPos, float2 pointA, float2 pointB, float3 color, float lifeFactor, float seed)
{
    float2 dir = pointB - pointA;
    float segmentLen = length(dir);

    // Skip very short segments
    if (segmentLen < 0.5)
        return float3(0.0, 0.0, 0.0);

    float2 dirNorm = dir / segmentLen;

    // Get perpendicular direction for noise displacement
    float2 perp = float2(-dirNorm.y, dirNorm.x);

    // Time-based animation
    float2 t = float2(0.0, fmod(Time + seed * 100.0, 200.0) * 3.0);

    // Calculate noise based on position along the line
    float lineT = LineParameter(screenPos, pointA, pointB);
    float2 noiseUV = float2(lineT * segmentLen * 0.1, seed * 10.0);
    float noise = SimpleNoise(noiseUV * NoiseScale * 5.0 - t, 2.0) * 2.0 - 1.0;

    // Apply crackling displacement perpendicular to line
    float edgeFade = smoothstep(0.0, 0.15, lineT) * smoothstep(1.0, 0.85, lineT);
    float2 displacement = perp * noise * CrackleIntensity * 3.0 * edgeFade;

    // Displace both endpoints slightly for crackling effect
    float2 dispA = pointA + perp * noise * CrackleIntensity * 1.5;
    float2 dispB = pointB + perp * noise * CrackleIntensity * 1.5;

    // Main electrical line
    float thickness = LineThickness * 0.5;
    float lineDist = LineSDF(screenPos, dispA, dispB, thickness);

    // Glow falloff
    float3 lineGlow = GlowIntensity * 8.0 / max(0.5, lineDist) * color;
    lineGlow = saturate(1.0 - exp(lineGlow * -0.03));

    // Taper at ends
    float taper = smoothstep(0.0, 0.1, lineT) * smoothstep(1.0, 0.9, lineT);
    lineGlow *= taper;

    float3 result = lineGlow;

    // Flicker effect
    float flickerPhase = FlickerSpeed * Time * TAU;
    float flickerRand = RandomFloat(float2(seed, seed * 1.73)) * TAU;
    float flicker = sin(flickerPhase + flickerRand) * 0.5 + 0.5;
    flicker = lerp(1.0, flicker, FlickerIntensity);

    // Additional random crackle flicker
    float crackleFlicker = RandomFloat(float2(floor(Time * 30.0 + seed * 100.0), seed));
    crackleFlicker = crackleFlicker > 0.85 ? 0.3 : 1.0;
    flicker *= lerp(1.0, crackleFlicker, CrackleIntensity * 0.5);

    result *= flicker;

    // ===== Branch Bolts =====
    if (BranchBoltEnabled > 0.5)
    {
        int boltCount = int(BranchBoltCount);
        for (int b = 0; b < boltCount && b < 8; b++)
        {
            // Distribute bolts along the segment
            float boltSeed = seed + float(b) * 0.17;
            float boltT = RandomFloat(float2(boltSeed, boltSeed * 1.5));

            // Random time offset for bolt appearance
            float boltTime = fmod(Time * 2.0 + boltSeed * 10.0, 1.0);
            float boltActive = smoothstep(0.0, 0.1, boltTime) * smoothstep(0.4, 0.2, boltTime);

            if (boltActive > 0.1)
            {
                // Position along segment
                float2 boltOrigin = lerp(pointA, pointB, boltT);

                // Random angle with spread from perpendicular
                float baseAngle = atan2(perp.y, perp.x);
                float spreadRad = BranchBoltSpread * PI / 180.0;
                float boltAngle = baseAngle + (RandomFloat(float2(boltSeed * 2.0, boltSeed)) - 0.5) * spreadRad;

                // Randomly flip to other side
                if (RandomFloat(float2(boltSeed * 3.0, boltSeed)) > 0.5)
                    boltAngle += PI;

                // Get bolt color (use config color or segment color)
                float3 boltColor = BranchBoltColor.a > 0.5 ? BranchBoltColor.rgb : color * 1.2;

                // Render the branch bolt
                float3 boltGlow = RenderBranchBolt(
                    screenPos,
                    boltOrigin,
                    boltAngle,
                    BranchBoltLength,
                    boltColor,
                    BranchBoltThickness,
                    boltSeed
                );

                result += boltGlow * boltActive * edgeFade;
            }
        }
    }

    // ===== Sparkles =====
    if (SparkleEnabled > 0.5)
    {
        int sparkleCount = int(SparkleCount);
        for (int s = 0; s < sparkleCount && s < 12; s++)
        {
            float sparkleSeed = seed + float(s) * 0.23;

            // Position along segment with some perpendicular offset
            float sparkleT = RandomFloat(float2(sparkleSeed, sparkleSeed * 1.3));
            float perpOffset = (RandomFloat(float2(sparkleSeed * 2.0, sparkleSeed)) - 0.5) * LineThickness * 4.0;

            float2 sparklePos = lerp(pointA, pointB, sparkleT) + perp * perpOffset;

            // Animated offset
            float animOffset = sin(Time * 10.0 + sparkleSeed * TAU) * 3.0;
            sparklePos += perp * animOffset;

            // Random timing for sparkle appearance
            float sparkleTime = fmod(Time * 3.0 + sparkleSeed * 20.0, 1.0);
            float sparkleActive = smoothstep(0.0, 0.05, sparkleTime) * smoothstep(0.3, 0.1, sparkleTime);

            if (sparkleActive > 0.1)
            {
                // Get sparkle color (use config color or brighter segment color)
                float3 sparkleCol = SparkleColor.a > 0.5 ? SparkleColor.rgb : color * 1.5 + float3(0.3, 0.3, 0.3);

                float3 sparkleGlow = RenderSparkle(
                    screenPos,
                    sparklePos,
                    sparkleCol,
                    SparkleSize,
                    SparkleIntensity,
                    sparkleSeed
                );

                result += sparkleGlow * sparkleActive * edgeFade;
            }
        }
    }

    // Life-based fade
    float fade = smoothstep(0.0, 0.15, lifeFactor) * smoothstep(0.0, 0.3, lifeFactor);
    result *= fade;

    return result;
}

// ===== Vertex Shader =====
VSOutput VSMain(uint vertexId : SV_VertexID)
{
    VSOutput output;
    float2 uv = float2((vertexId << 1) & 2, vertexId & 2);
    output.Position = float4(uv * 2.0 - 1.0, 0.0, 1.0);
    output.Position.y = -output.Position.y;
    output.TexCoord = uv;
    output.ScreenPos = uv * ViewportSize;
    return output;
}

// ===== Pixel Shader =====
float4 PSMain(VSOutput input) : SV_TARGET
{
    float2 screenPos = input.ScreenPos;
    float3 finalColor = float3(0.0, 0.0, 0.0);

    uint pointCount;
    uint stride;
    TrailPoints.GetDimensions(pointCount, stride);

    // Render lines between consecutive points
    for (uint i = 0; i < pointCount - 1; i++)
    {
        TrailPoint pointA = TrailPoints[i];
        TrailPoint pointB = TrailPoints[i + 1];

        // Skip if either point is dead
        if (pointA.Lifetime <= 0.0 || pointB.Lifetime <= 0.0)
            continue;

        // Use the minimum lifetime of both points for the segment
        float minLifetime = min(pointA.Lifetime, pointB.Lifetime);
        float maxLifetime = max(pointA.MaxLifetime, pointB.MaxLifetime);
        float lifeFactor = minLifetime / maxLifetime;

        // Blend colors between the two points
        float3 segmentColor = lerp(pointA.Color.rgb, pointB.Color.rgb, 0.5);
        float segmentAlpha = lerp(pointA.Color.a, pointB.Color.a, 0.5);

        // Generate a seed from point positions
        float seed = RandomFloat(pointA.Position * 0.01);

        // Render the electrical segment with bolts and sparkles
        float3 segmentGlow = RenderElectricalSegment(
            screenPos,
            pointA.Position,
            pointB.Position,
            segmentColor,
            lifeFactor,
            seed
        );

        finalColor += segmentGlow * segmentAlpha;
    }

    float alpha = saturate(length(finalColor) * 2.0);

    if (alpha < 0.005)
        discard;

    return float4(finalColor, alpha);
}

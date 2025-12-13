// Lightning Trail Shader
// Renders a jagged lightning bolt trail with many branches following the mouse

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
    return frac(sin(dot(seed, float2(12.9898, 78.233))) * 43758.5453);
}

float2 RandomFloat2(float2 seed)
{
    return float2(
        RandomFloat(seed),
        RandomFloat(seed * 1.7320508 + 0.5)
    );
}

// SDF for a line segment
float LineSDF(float2 p, float2 a, float2 b)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h);
}

// Rotate a 2D vector by angle
float2 Rotate(float2 v, float angle)
{
    float c = cos(angle);
    float s = sin(angle);
    return float2(v.x * c - v.y * s, v.x * s + v.y * c);
}

// Render a single lightning bolt segment with jagged path
// Returns glow intensity at the given screen position
float RenderLightningSegment(float2 screenPos, float2 startPos, float2 endPos, float thickness, float seed, float lifeFactor)
{
    float2 dir = endPos - startPos;
    float segmentLen = length(dir);

    if (segmentLen < 1.0)
        return 0.0;

    float2 dirNorm = dir / segmentLen;
    float2 perp = float2(-dirNorm.y, dirNorm.x);

    // Number of jagged segments based on length
    int numJags = max(2, int(segmentLen / 8.0));
    float jagLen = segmentLen / float(numJags);

    float totalGlow = 0.0;
    float2 prevPoint = startPos;

    // Create jagged path
    for (int j = 0; j < numJags && j < 16; j++)
    {
        float t = float(j + 1) / float(numJags);
        float2 basePoint = lerp(startPos, endPos, t);

        // Add perpendicular displacement for jagged effect (except at endpoints)
        float2 nextPoint = basePoint;
        if (j < numJags - 1)
        {
            float jagSeed = seed + float(j) * 0.173;
            float displacement = (RandomFloat(float2(jagSeed, jagSeed * 2.1)) - 0.5) * jagLen * CrackleIntensity * 1.5;

            // Animated displacement
            float animPhase = Time * FlickerSpeed * 0.5 + jagSeed * TAU;
            displacement += sin(animPhase) * jagLen * 0.2;

            nextPoint += perp * displacement;
        }

        // Calculate distance to this line segment
        float dist = LineSDF(screenPos, prevPoint, nextPoint);

        // Taper thickness based on lifetime
        float taperThickness = thickness * lifeFactor;

        // Calculate glow for this segment
        float segGlow = taperThickness / max(0.5, dist);
        segGlow = saturate(segGlow * 0.5);

        totalGlow += segGlow;
        prevPoint = nextPoint;
    }

    return totalGlow;
}

// Render a branch bolt (smaller lightning that forks off)
float RenderBranchBolt(float2 screenPos, float2 origin, float angle, float boltLength, float thickness, float seed, float lifeFactor)
{
    float2 dir = float2(cos(angle), sin(angle));
    float2 perp = float2(-dir.y, dir.x);

    // Create jagged branch with 3-5 segments
    int numSegs = 3 + int(RandomFloat(float2(seed * 3.0, seed)) * 3.0);
    float segLen = boltLength / float(numSegs);

    float totalGlow = 0.0;
    float2 prevPoint = origin;

    for (int i = 0; i < numSegs && i < 6; i++)
    {
        float segSeed = seed + float(i) * 0.19;

        // Random displacement perpendicular to direction
        float displacement = (RandomFloat(float2(segSeed, segSeed * 2.7)) - 0.5) * segLen * 0.8;

        // Animated displacement
        float animDisp = sin(Time * FlickerSpeed + segSeed * TAU) * segLen * 0.25;
        displacement += animDisp;

        // Next point along branch
        float2 nextPoint = prevPoint + dir * segLen + perp * displacement;

        // Taper thickness toward end
        float taperFactor = 1.0 - float(i) / float(numSegs);
        float segThickness = thickness * taperFactor * lifeFactor * 0.6;

        // Calculate distance and glow
        float dist = LineSDF(screenPos, prevPoint, nextPoint);
        float segGlow = segThickness / max(0.3, dist);
        segGlow = saturate(segGlow * 0.4) * taperFactor;

        totalGlow += segGlow;
        prevPoint = nextPoint;
    }

    // Flicker effect
    float flickerPhase = Time * FlickerSpeed * 1.5 + seed * TAU;
    float flicker = sin(flickerPhase) * 0.3 + 0.7;

    // Random crackle (occasional dimming)
    float crackle = RandomFloat(float2(floor(Time * 20.0 + seed * 50.0), seed));
    crackle = crackle > 0.85 ? 0.3 : 1.0;

    return totalGlow * flicker * crackle;
}

// Render sparkle effect at a point
float3 RenderSparkle(float2 screenPos, float2 sparklePos, float3 color, float size, float intensity, float seed)
{
    float dist = length(screenPos - sparklePos);

    // Core bright point
    float core = smoothstep(size * 2.0, 0.0, dist);

    // Outer glow
    float glow = size * intensity / max(1.0, dist * 0.3);
    glow = saturate(glow);

    // Twinkle effect
    float twinkle = sin(Time * 25.0 + seed * TAU) * 0.4 + 0.6;

    // Random flash
    float flash = RandomFloat(float2(floor(Time * 18.0 + seed * 40.0), seed));
    flash = flash > 0.75 ? 1.8 : 1.0;

    return color * (core * 3.0 + glow) * twinkle * flash * intensity;
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

    // Global flicker for entire trail
    float globalFlicker = sin(Time * FlickerSpeed) * FlickerIntensity * 0.3 + (1.0 - FlickerIntensity * 0.3);

    // Render lightning segments between consecutive trail points
    for (uint i = 0; i < pointCount - 1; i++)
    {
        TrailPoint pointA = TrailPoints[i];
        TrailPoint pointB = TrailPoints[i + 1];

        // Skip if either point is dead
        if (pointA.Lifetime <= 0.0 || pointB.Lifetime <= 0.0)
            continue;

        // Life factor for fading
        float minLifetime = min(pointA.Lifetime, pointB.Lifetime);
        float maxLifetime = max(pointA.MaxLifetime, pointB.MaxLifetime);
        float lifeFactor = saturate(minLifetime / maxLifetime);

        // Smooth fade at start and end of life
        lifeFactor = smoothstep(0.0, 0.2, lifeFactor);

        // Blend colors
        float3 segmentColor = lerp(pointA.Color.rgb, pointB.Color.rgb, 0.5);

        // Generate seed from positions
        float seed = RandomFloat(pointA.Position * 0.01 + float2(float(i), 0.0));

        // ===== Main Lightning Segment =====
        float mainGlow = RenderLightningSegment(
            screenPos,
            pointA.Position,
            pointB.Position,
            LineThickness,
            seed,
            lifeFactor
        );

        finalColor += segmentColor * mainGlow * GlowIntensity * globalFlicker;

        // ===== Branch Bolts =====
        if (BranchBoltEnabled > 0.5)
        {
            float2 segDir = pointB.Position - pointA.Position;
            float segLen = length(segDir);
            float2 segDirNorm = segLen > 0.1 ? segDir / segLen : float2(1.0, 0.0);
            float2 segPerp = float2(-segDirNorm.y, segDirNorm.x);

            int branchCount = int(BranchBoltCount);
            for (int b = 0; b < branchCount && b < 8; b++)
            {
                float branchSeed = seed + float(b) * 0.23;

                // Position along segment for this branch
                float branchT = RandomFloat(float2(branchSeed, branchSeed * 1.7));
                float2 branchOrigin = lerp(pointA.Position, pointB.Position, branchT);

                // Random angle based on perpendicular with spread
                float baseAngle = atan2(segPerp.y, segPerp.x);
                float spreadRad = BranchBoltSpread * PI / 180.0;
                float branchAngle = baseAngle + (RandomFloat(float2(branchSeed * 2.0, branchSeed)) - 0.5) * spreadRad;

                // Randomly flip to other side of main bolt
                if (RandomFloat(float2(branchSeed * 3.0, branchSeed * 1.1)) > 0.5)
                    branchAngle += PI;

                // Time-based appearance (branches flicker in and out)
                float branchTime = fmod(Time * 3.0 + branchSeed * 10.0, 1.0);
                float branchActive = smoothstep(0.0, 0.15, branchTime) * smoothstep(0.6, 0.3, branchTime);

                if (branchActive > 0.05)
                {
                    // Branch color (use config or segment color)
                    float3 branchColor = BranchBoltColor.a > 0.5 ? BranchBoltColor.rgb : segmentColor * 1.1;

                    // Randomize branch length slightly
                    float branchLen = BranchBoltLength * (0.6 + RandomFloat(float2(branchSeed * 4.0, branchSeed)) * 0.8);

                    float branchGlow = RenderBranchBolt(
                        screenPos,
                        branchOrigin,
                        branchAngle,
                        branchLen,
                        BranchBoltThickness,
                        branchSeed,
                        lifeFactor
                    );

                    finalColor += branchColor * branchGlow * GlowIntensity * branchActive * globalFlicker;

                    // Sub-branches (smaller branches off the main branches)
                    if (branchLen > 15.0 && RandomFloat(float2(branchSeed * 5.0, branchSeed)) > 0.5)
                    {
                        float2 subOrigin = branchOrigin + float2(cos(branchAngle), sin(branchAngle)) * branchLen * 0.5;
                        float subAngle = branchAngle + (RandomFloat(float2(branchSeed * 6.0, branchSeed)) - 0.5) * PI * 0.5;

                        float subGlow = RenderBranchBolt(
                            screenPos,
                            subOrigin,
                            subAngle,
                            branchLen * 0.4,
                            BranchBoltThickness * 0.6,
                            branchSeed + 0.5,
                            lifeFactor * 0.7
                        );

                        finalColor += branchColor * subGlow * GlowIntensity * 0.5 * branchActive * globalFlicker;
                    }
                }
            }
        }

        // ===== Sparkles at junction points =====
        if (SparkleEnabled > 0.5)
        {
            int sparkleCount = int(SparkleCount);
            for (int s = 0; s < sparkleCount && s < 8; s++)
            {
                float sparkleSeed = seed + float(s) * 0.31;

                // Sparkles around the segment
                float sparkleT = RandomFloat(float2(sparkleSeed, sparkleSeed * 1.5));
                float2 basePos = lerp(pointA.Position, pointB.Position, sparkleT);

                // Offset perpendicular to segment
                float2 segDir2 = normalize(pointB.Position - pointA.Position);
                float2 sparklePerp = float2(-segDir2.y, segDir2.x);
                float perpOffset = (RandomFloat(float2(sparkleSeed * 2.0, sparkleSeed)) - 0.5) * LineThickness * 6.0;

                float2 sparklePos = basePos + sparklePerp * perpOffset;

                // Animated movement
                float animOffset = sin(Time * 12.0 + sparkleSeed * TAU) * 4.0;
                sparklePos += sparklePerp * animOffset;

                // Sparkle timing
                float sparkleTime = fmod(Time * 4.0 + sparkleSeed * 15.0, 1.0);
                float sparkleActive = smoothstep(0.0, 0.1, sparkleTime) * smoothstep(0.4, 0.15, sparkleTime);

                if (sparkleActive > 0.1)
                {
                    float3 sparkleCol = SparkleColor.a > 0.5 ? SparkleColor.rgb : segmentColor * 1.5 + float3(0.2, 0.2, 0.2);

                    float3 sparkleGlow = RenderSparkle(
                        screenPos,
                        sparklePos,
                        sparkleCol,
                        SparkleSize,
                        SparkleIntensity,
                        sparkleSeed
                    );

                    finalColor += sparkleGlow * sparkleActive * lifeFactor;
                }
            }
        }
    }

    // Calculate alpha from brightness
    float alpha = saturate(length(finalColor) * 1.5);

    if (alpha < 0.005)
        discard;

    return float4(finalColor, alpha);
}

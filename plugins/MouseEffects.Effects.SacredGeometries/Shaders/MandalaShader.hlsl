// MandalaShader.hlsl - Sacred Geometry Mandala Renderer
// Procedurally draws sacred geometry patterns with glow effects and morphing animations

static const float PI = 3.14159265359;
static const float TAU = 6.28318530718;
static const float SQRT3 = 1.73205080757;

// Pattern type constants
static const int PATTERN_SEED_OF_LIFE = 0;
static const int PATTERN_FLOWER_OF_LIFE = 1;
static const int PATTERN_VESICA_PISCIS = 2;
static const int PATTERN_MERKABA = 3;
static const int PATTERN_SRI_YANTRA = 4;
static const int PATTERN_METATRONS_CUBE = 5;
static const int PATTERN_TREE_OF_LIFE = 6;
static const int PATTERN_TORUS = 7;
static const int PATTERN_TETRAHEDRON_GRID = 8;
static const int PATTERN_PLATONIC_SOLIDS = 9;

// Constant buffer
cbuffer Constants : register(b0)
{
    float2 ViewportSize;
    float Time;
    float GlowIntensity;

    float HdrMultiplier;
    float LineThickness;
    int ActiveMandalaCount;
    float TwinkleIntensity;

    float4 PrimaryColor;
    float4 SecondaryColor;

    float RainbowSpeed;
    int RainbowEnabled;
    float RainbowHue;
    float Padding1;

    float FadeInDuration;
    float FadeOutDuration;
    float ScaleInDuration;
    float ScaleOutDuration;

    int MorphEnabled;
    float MorphSpeed;
    float MorphIntensity;
    int MorphBetweenPatterns;

    float4 Padding3;
};

// Mandala instance data (80 bytes)
struct MandalaInstance
{
    float2 Position;
    float Radius;
    float Rotation;

    float RotationSpeed;
    float RotationDirection;
    float Lifetime;
    float MaxLifetime;

    float PatternIndex;
    float AppearMode;
    float SpawnTime;
    float PatternComplexity;

    float4 Color;

    float MorphPhase;
    float MorphTargetPattern;
    float MorphSpeedMult;
    float Padding;
};

StructuredBuffer<MandalaInstance> Mandalas : register(t0);

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
    float2 LocalPos : TEXCOORD1;
    nointerpolation uint InstanceId : TEXCOORD2;
};

// ============================================
// SDF Primitives
// ============================================

float CircleSDF(float2 p, float r)
{
    return length(p) - r;
}

float RingSDF(float2 p, float r, float thickness)
{
    return abs(length(p) - r) - thickness;
}

float LineSDF(float2 p, float2 a, float2 b, float thickness)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h) - thickness;
}

float TriangleSDF(float2 p, float2 p0, float2 p1, float2 p2, float thickness)
{
    float d = 1e10;
    d = min(d, LineSDF(p, p0, p1, thickness));
    d = min(d, LineSDF(p, p1, p2, thickness));
    d = min(d, LineSDF(p, p2, p0, thickness));
    return d;
}

// 2D Rotation
float2 Rotate2D(float2 p, float angle)
{
    float c = cos(angle);
    float s = sin(angle);
    return float2(p.x * c - p.y * s, p.x * s + p.y * c);
}

// ============================================
// Sacred Geometry Pattern Functions with Internal Animation
// Each pattern now accepts a morphPhase (0-1) for internal animation
// ============================================

// Pattern 0: Seed of Life - 7 overlapping circles with breathing animation
float SeedOfLifeSDF(float2 uv, float radius, float thickness, float morphPhase, float intensity)
{
    float d = 1e10;
    float r = radius * 0.5;

    // Breathing animation - circles expand and contract
    float breathe = 1.0 + sin(morphPhase * TAU) * 0.15 * intensity;
    r *= breathe;

    // Center circle with pulse
    float centerPulse = 1.0 + sin(morphPhase * TAU * 2.0) * 0.1 * intensity;
    d = min(d, RingSDF(uv, r * centerPulse, thickness));

    // 6 surrounding circles with wave motion
    [unroll]
    for (int i = 0; i < 6; i++)
    {
        float angle = float(i) * TAU / 6.0;
        // Wave offset - circles move in and out sequentially
        float waveOffset = sin(morphPhase * TAU + float(i) * TAU / 6.0) * radius * 0.1 * intensity;
        float2 offset = float2(cos(angle), sin(angle)) * (r + waveOffset);
        d = min(d, RingSDF(uv - offset, r, thickness));
    }

    return d;
}

// Pattern 1: Flower of Life - 19 overlapping circles with bloom animation
float FlowerOfLifeSDF(float2 uv, float radius, float thickness, float morphPhase, float intensity)
{
    float d = 1e10;
    float r = radius / 3.0;

    // Bloom animation - outer rings grow and shrink
    float bloom = 1.0 + sin(morphPhase * TAU) * 0.1 * intensity;

    // Center circle - pulse independently
    float centerPulse = 1.0 + sin(morphPhase * TAU * 3.0) * 0.08 * intensity;
    d = min(d, RingSDF(uv, r * centerPulse, thickness));

    // First ring - 6 circles with rotation effect
    [unroll]
    for (int i = 0; i < 6; i++)
    {
        float angle = float(i) * TAU / 6.0 + morphPhase * 0.2 * intensity;
        float2 offset = float2(cos(angle), sin(angle)) * r;
        d = min(d, RingSDF(uv - offset, r, thickness));
    }

    // Second ring - 12 circles with opposite rotation and bloom
    [unroll]
    for (int j = 0; j < 12; j++)
    {
        float angle = float(j) * TAU / 12.0 - morphPhase * 0.15 * intensity;
        float dist = r * 2.0 * bloom;
        float2 offset = float2(cos(angle), sin(angle)) * dist;
        d = min(d, RingSDF(uv - offset, r, thickness));
    }

    return d;
}

// Pattern 2: Vesica Piscis - 2 overlapping circles with merge animation
float VesicaPiscisSDF(float2 uv, float radius, float thickness, float morphPhase, float intensity)
{
    float d = 1e10;

    // Circles move together and apart
    float separation = 0.5 + sin(morphPhase * TAU) * 0.25 * intensity;
    float offset = radius * separation;

    // Circle sizes pulse in opposition
    float sizeMod1 = 1.0 + sin(morphPhase * TAU) * 0.1 * intensity;
    float sizeMod2 = 1.0 - sin(morphPhase * TAU) * 0.1 * intensity;

    // Left circle
    d = min(d, RingSDF(uv - float2(-offset, 0), radius * sizeMod1, thickness));
    // Right circle
    d = min(d, RingSDF(uv - float2(offset, 0), radius * sizeMod2, thickness));

    return d;
}

// Pattern 3: Merkaba - Star tetrahedron with counter-rotation
float MerkabaSDF(float2 uv, float radius, float thickness, float morphPhase, float intensity)
{
    float d = 1e10;
    float r = radius * 0.9;

    // Counter-rotating triangles
    float rotation1 = morphPhase * TAU * 0.5 * intensity;
    float rotation2 = -morphPhase * TAU * 0.5 * intensity;

    // Pulsing size
    float pulse = 1.0 + sin(morphPhase * TAU * 2.0) * 0.08 * intensity;
    float r1 = r * pulse;
    float r2 = r * (2.0 - pulse);

    // Upward triangle with rotation
    float2 t1a = Rotate2D(float2(0, r1), rotation1);
    float2 t1b = Rotate2D(float2(-r1 * SQRT3 * 0.5, -r1 * 0.5), rotation1);
    float2 t1c = Rotate2D(float2(r1 * SQRT3 * 0.5, -r1 * 0.5), rotation1);
    d = min(d, TriangleSDF(uv, t1a, t1b, t1c, thickness));

    // Downward triangle with counter-rotation
    float2 t2a = Rotate2D(float2(0, -r2), rotation2);
    float2 t2b = Rotate2D(float2(-r2 * SQRT3 * 0.5, r2 * 0.5), rotation2);
    float2 t2c = Rotate2D(float2(r2 * SQRT3 * 0.5, r2 * 0.5), rotation2);
    d = min(d, TriangleSDF(uv, t2a, t2b, t2c, thickness));

    return d;
}

// Pattern 4: Sri Yantra - 9 interlocking triangles with wave animation
float SriYantraSDF(float2 uv, float radius, float thickness, float morphPhase, float intensity)
{
    float d = 1e10;

    // Outer circle with pulse
    float outerPulse = 1.0 + sin(morphPhase * TAU) * 0.05 * intensity;
    d = min(d, RingSDF(uv, radius * outerPulse, thickness));

    // 4 upward triangles with wave offset
    float scales[4] = { 0.95, 0.75, 0.55, 0.35 };
    [unroll]
    for (int i = 0; i < 4; i++)
    {
        float wave = sin(morphPhase * TAU + float(i) * PI * 0.5) * 0.05 * intensity;
        float r = radius * (scales[i] + wave);
        float2 t1a = float2(0, r);
        float2 t1b = float2(-r * SQRT3 * 0.5, -r * 0.5);
        float2 t1c = float2(r * SQRT3 * 0.5, -r * 0.5);
        d = min(d, TriangleSDF(uv, t1a, t1b, t1c, thickness));
    }

    // 5 downward triangles with opposite wave
    float scales2[5] = { 0.85, 0.65, 0.45, 0.25, 0.1 };
    [unroll]
    for (int j = 0; j < 5; j++)
    {
        float wave = sin(morphPhase * TAU + float(j) * PI * 0.4 + PI) * 0.05 * intensity;
        float r = radius * (scales2[j] + wave);
        float yOffset = radius * 0.05 * float(j);
        float2 t2a = float2(0, -r + yOffset);
        float2 t2b = float2(-r * SQRT3 * 0.5, r * 0.5 + yOffset);
        float2 t2c = float2(r * SQRT3 * 0.5, r * 0.5 + yOffset);
        d = min(d, TriangleSDF(uv, t2a, t2b, t2c, thickness));
    }

    // Center point (bindu) - pulsing
    float binduPulse = 1.0 + sin(morphPhase * TAU * 4.0) * 0.5 * intensity;
    d = min(d, CircleSDF(uv, thickness * 3.0 * binduPulse));

    return d;
}

// Pattern 5: Metatron's Cube - 13 circles with energy flow animation
float MetatronsCubeSDF(float2 uv, float radius, float thickness, float morphPhase, float intensity)
{
    float d = 1e10;
    float r = radius * 0.15;

    // Store positions for lines
    float2 positions[13];
    int posIndex = 0;

    // Center circle - strong pulse
    float centerPulse = 1.0 + sin(morphPhase * TAU * 2.0) * 0.2 * intensity;
    positions[posIndex++] = float2(0, 0);
    d = min(d, RingSDF(uv, r * centerPulse, thickness));

    // Inner hexagon (6 circles) - rotate and pulse sequentially
    [unroll]
    for (int i = 0; i < 6; i++)
    {
        float angle = float(i) * TAU / 6.0 + morphPhase * 0.3 * intensity;
        float nodePulse = 1.0 + sin(morphPhase * TAU + float(i) * TAU / 6.0) * 0.15 * intensity;
        float2 pos = float2(cos(angle), sin(angle)) * radius * 0.5;
        positions[posIndex++] = pos;
        d = min(d, RingSDF(uv - pos, r * nodePulse, thickness));
    }

    // Outer hexagon (6 circles) - counter-rotate
    [unroll]
    for (int j = 0; j < 6; j++)
    {
        float angle = float(j) * TAU / 6.0 + PI / 6.0 - morphPhase * 0.2 * intensity;
        float nodePulse = 1.0 + sin(morphPhase * TAU + float(j) * TAU / 6.0 + PI) * 0.15 * intensity;
        float2 pos = float2(cos(angle), sin(angle)) * radius;
        positions[posIndex++] = pos;
        d = min(d, RingSDF(uv - pos, r * nodePulse, thickness));
    }

    // Connecting lines with energy flow (thickness varies)
    float lineThick = thickness * 0.5;

    // Center to inner hexagon
    [unroll]
    for (int k = 1; k <= 6; k++)
    {
        float flow = (1.0 + sin(morphPhase * TAU * 3.0 + float(k)) * 0.3 * intensity);
        d = min(d, LineSDF(uv, positions[0], positions[k], lineThick * flow));
    }

    // Inner hexagon edges
    [unroll]
    for (int m = 1; m <= 6; m++)
    {
        int next = m == 6 ? 1 : m + 1;
        d = min(d, LineSDF(uv, positions[m], positions[next], lineThick));
    }

    // Inner to outer connections
    [unroll]
    for (int n = 1; n <= 6; n++)
    {
        d = min(d, LineSDF(uv, positions[n], positions[n + 6], lineThick));
        int nextOuter = n == 6 ? 7 : n + 7;
        d = min(d, LineSDF(uv, positions[n], positions[nextOuter], lineThick));
    }

    // Outer hexagon edges
    [unroll]
    for (int p = 7; p <= 12; p++)
    {
        int next = p == 12 ? 7 : p + 1;
        d = min(d, LineSDF(uv, positions[p], positions[next], lineThick));
    }

    return d;
}

// Pattern 6: Tree of Life - 10 sephiroth with energy ascending animation
float TreeOfLifeSDF(float2 uv, float radius, float thickness, float morphPhase, float intensity)
{
    float d = 1e10;
    float nodeR = radius * 0.1;

    // 10 Sephiroth positions with breathing animation
    float2 sephiroth[10];

    // Ascending energy wave
    float wave[10];
    for (int w = 0; w < 10; w++)
    {
        wave[w] = sin(morphPhase * TAU - float(w) * 0.3) * intensity;
    }

    float breathe = 1.0 + sin(morphPhase * TAU) * 0.1 * intensity;

    sephiroth[0] = float2(0, radius * 0.9 * breathe);
    sephiroth[1] = float2(-radius * 0.45, radius * 0.6);
    sephiroth[2] = float2(radius * 0.45, radius * 0.6);
    sephiroth[3] = float2(-radius * 0.45, radius * 0.2);
    sephiroth[4] = float2(radius * 0.45, radius * 0.2);
    sephiroth[5] = float2(0, 0);
    sephiroth[6] = float2(-radius * 0.45, -radius * 0.4);
    sephiroth[7] = float2(radius * 0.45, -radius * 0.4);
    sephiroth[8] = float2(0, -radius * 0.6);
    sephiroth[9] = float2(0, -radius * 0.9 * breathe);

    // Draw nodes with individual pulses
    [unroll]
    for (int i = 0; i < 10; i++)
    {
        float nodePulse = 1.0 + wave[i] * 0.3;
        d = min(d, RingSDF(uv - sephiroth[i], nodeR * nodePulse, thickness));
    }

    // Draw paths with energy flow
    float lineThick = thickness * 0.5;
    d = min(d, LineSDF(uv, sephiroth[0], sephiroth[1], lineThick));
    d = min(d, LineSDF(uv, sephiroth[0], sephiroth[2], lineThick));
    d = min(d, LineSDF(uv, sephiroth[1], sephiroth[2], lineThick));
    d = min(d, LineSDF(uv, sephiroth[1], sephiroth[3], lineThick));
    d = min(d, LineSDF(uv, sephiroth[1], sephiroth[5], lineThick));
    d = min(d, LineSDF(uv, sephiroth[2], sephiroth[4], lineThick));
    d = min(d, LineSDF(uv, sephiroth[2], sephiroth[5], lineThick));
    d = min(d, LineSDF(uv, sephiroth[3], sephiroth[4], lineThick));
    d = min(d, LineSDF(uv, sephiroth[3], sephiroth[5], lineThick));
    d = min(d, LineSDF(uv, sephiroth[3], sephiroth[6], lineThick));
    d = min(d, LineSDF(uv, sephiroth[4], sephiroth[5], lineThick));
    d = min(d, LineSDF(uv, sephiroth[4], sephiroth[7], lineThick));
    d = min(d, LineSDF(uv, sephiroth[5], sephiroth[6], lineThick));
    d = min(d, LineSDF(uv, sephiroth[5], sephiroth[7], lineThick));
    d = min(d, LineSDF(uv, sephiroth[5], sephiroth[8], lineThick));
    d = min(d, LineSDF(uv, sephiroth[6], sephiroth[7], lineThick));
    d = min(d, LineSDF(uv, sephiroth[6], sephiroth[8], lineThick));
    d = min(d, LineSDF(uv, sephiroth[7], sephiroth[8], lineThick));
    d = min(d, LineSDF(uv, sephiroth[8], sephiroth[9], lineThick));

    return d;
}

// Pattern 7: Torus - Animated concentric rings with flowing depth
float TorusSDF(float2 uv, float radius, float thickness, float time, float morphPhase, float intensity)
{
    float d = 1e10;
    int numRings = 16;

    // Additional morphing animation on top of time-based animation
    float morphOffset = morphPhase * TAU * intensity;

    [unroll]
    for (int i = 0; i < numRings; i++)
    {
        float t = float(i) / float(numRings);
        // Combine time animation with morph animation
        float depth = cos(t * TAU + time * 2.0 + morphOffset);
        float ringRadius = radius * (0.3 + 0.7 * (depth * 0.5 + 0.5));
        float ringThickness = thickness * (0.5 + 0.5 * (depth * 0.5 + 0.5));
        float ringY = radius * 0.4 * sin(t * TAU + time * 2.0 + morphOffset);

        d = min(d, RingSDF(uv - float2(0, ringY), ringRadius, ringThickness));
    }

    return d;
}

// Pattern 8: 64 Tetrahedron Grid - Triangular lattice with wave distortion
float TetrahedronGridSDF(float2 uv, float radius, float thickness, float morphPhase, float intensity)
{
    float d = 1e10;
    float gridSize = radius / 4.0;

    int gridCount = 4;

    for (int row = -gridCount; row <= gridCount; row++)
    {
        for (int col = -gridCount; col <= gridCount; col++)
        {
            float xOffset = (row % 2) * gridSize * 0.5;
            float2 cellCenter = float2(col * gridSize + xOffset, row * gridSize * SQRT3 * 0.5);

            if (length(cellCenter) > radius * 1.1) continue;

            // Wave distortion based on distance from center
            float distFromCenter = length(cellCenter) / radius;
            float waveOffset = sin(morphPhase * TAU + distFromCenter * 4.0) * gridSize * 0.1 * intensity;
            cellCenter += normalize(cellCenter + 0.001) * waveOffset;

            // Triangle size variation
            float sizeMod = 1.0 + sin(morphPhase * TAU * 2.0 + distFromCenter * PI) * 0.15 * intensity;

            // Upward triangle
            float2 t1a = cellCenter + float2(0, gridSize * 0.33 * sizeMod);
            float2 t1b = cellCenter + float2(-gridSize * 0.29 * sizeMod, -gridSize * 0.17 * sizeMod);
            float2 t1c = cellCenter + float2(gridSize * 0.29 * sizeMod, -gridSize * 0.17 * sizeMod);
            d = min(d, TriangleSDF(uv, t1a, t1b, t1c, thickness * 0.5));

            // Downward triangle
            float2 t2Center = cellCenter + float2(gridSize * 0.5, gridSize * SQRT3 * 0.17);
            float2 t2a = t2Center + float2(0, -gridSize * 0.33 * sizeMod);
            float2 t2b = t2Center + float2(-gridSize * 0.29 * sizeMod, gridSize * 0.17 * sizeMod);
            float2 t2c = t2Center + float2(gridSize * 0.29 * sizeMod, gridSize * 0.17 * sizeMod);
            d = min(d, TriangleSDF(uv, t2a, t2b, t2c, thickness * 0.5));
        }
    }

    // Outer boundary circle with pulse
    float outerPulse = 1.0 + sin(morphPhase * TAU) * 0.05 * intensity;
    d = min(d, RingSDF(uv, radius * outerPulse, thickness));

    return d;
}

// Helper function for individual platonic solid shapes (must be defined before PlatonicSolidsSDF)
float GetPlatonicSolid(float2 rotUV, float r, float thickness, int solidIndex)
{
    float d = 1e10;

    if (solidIndex == 0)
    {
        // Tetrahedron
        float2 t1a = float2(0, r);
        float2 t1b = float2(-r * SQRT3 * 0.5, -r * 0.5);
        float2 t1c = float2(r * SQRT3 * 0.5, -r * 0.5);
        d = min(d, TriangleSDF(rotUV, t1a, t1b, t1c, thickness));
        d = min(d, LineSDF(rotUV, float2(0, 0), t1a, thickness * 0.5));
        d = min(d, LineSDF(rotUV, float2(0, 0), t1b, thickness * 0.5));
        d = min(d, LineSDF(rotUV, float2(0, 0), t1c, thickness * 0.5));
    }
    else if (solidIndex == 1)
    {
        // Cube
        float s = r * 0.7;
        d = min(d, LineSDF(rotUV, float2(-s, -s), float2(s, -s), thickness));
        d = min(d, LineSDF(rotUV, float2(s, -s), float2(s, s), thickness));
        d = min(d, LineSDF(rotUV, float2(s, s), float2(-s, s), thickness));
        d = min(d, LineSDF(rotUV, float2(-s, s), float2(-s, -s), thickness));
        float s2 = s * 0.5;
        float offset = s * 0.3;
        d = min(d, LineSDF(rotUV, float2(-s2 + offset, -s2 + offset), float2(s2 + offset, -s2 + offset), thickness * 0.7));
        d = min(d, LineSDF(rotUV, float2(s2 + offset, -s2 + offset), float2(s2 + offset, s2 + offset), thickness * 0.7));
        d = min(d, LineSDF(rotUV, float2(s2 + offset, s2 + offset), float2(-s2 + offset, s2 + offset), thickness * 0.7));
        d = min(d, LineSDF(rotUV, float2(-s2 + offset, s2 + offset), float2(-s2 + offset, -s2 + offset), thickness * 0.7));
        d = min(d, LineSDF(rotUV, float2(-s, -s), float2(-s2 + offset, -s2 + offset), thickness * 0.5));
        d = min(d, LineSDF(rotUV, float2(s, -s), float2(s2 + offset, -s2 + offset), thickness * 0.5));
        d = min(d, LineSDF(rotUV, float2(s, s), float2(s2 + offset, s2 + offset), thickness * 0.5));
        d = min(d, LineSDF(rotUV, float2(-s, s), float2(-s2 + offset, s2 + offset), thickness * 0.5));
    }
    else if (solidIndex == 2)
    {
        // Octahedron
        d = min(d, LineSDF(rotUV, float2(0, r), float2(r, 0), thickness));
        d = min(d, LineSDF(rotUV, float2(r, 0), float2(0, -r), thickness));
        d = min(d, LineSDF(rotUV, float2(0, -r), float2(-r, 0), thickness));
        d = min(d, LineSDF(rotUV, float2(-r, 0), float2(0, r), thickness));
        d = min(d, LineSDF(rotUV, float2(-r, 0), float2(r, 0), thickness * 0.5));
        d = min(d, LineSDF(rotUV, float2(0, -r), float2(0, r), thickness * 0.5));
    }
    else if (solidIndex == 3)
    {
        // Dodecahedron (pentagon)
        [unroll]
        for (int i = 0; i < 5; i++)
        {
            float a1 = float(i) * TAU / 5.0 - PI / 2.0;
            float a2 = float(i + 1) * TAU / 5.0 - PI / 2.0;
            float2 p1 = float2(cos(a1), sin(a1)) * r;
            float2 p2 = float2(cos(a2), sin(a2)) * r;
            d = min(d, LineSDF(rotUV, p1, p2, thickness));
            d = min(d, LineSDF(rotUV, float2(0, 0), p1, thickness * 0.5));
        }
        [unroll]
        for (int j = 0; j < 5; j++)
        {
            float a1 = float(j) * TAU / 5.0 - PI / 2.0 + PI / 5.0;
            float a2 = float(j + 1) * TAU / 5.0 - PI / 2.0 + PI / 5.0;
            float2 p1 = float2(cos(a1), sin(a1)) * r * 0.4;
            float2 p2 = float2(cos(a2), sin(a2)) * r * 0.4;
            d = min(d, LineSDF(rotUV, p1, p2, thickness * 0.7));
        }
    }
    else
    {
        // Icosahedron
        d = min(d, TriangleSDF(rotUV,
            float2(0, r),
            float2(-r * SQRT3 * 0.5, -r * 0.5),
            float2(r * SQRT3 * 0.5, -r * 0.5), thickness));
        d = min(d, TriangleSDF(rotUV,
            float2(0, -r * 0.5),
            float2(-r * SQRT3 * 0.25, r * 0.25),
            float2(r * SQRT3 * 0.25, r * 0.25), thickness * 0.7));
        d = min(d, LineSDF(rotUV, float2(0, r), float2(0, -r * 0.5), thickness * 0.5));
        d = min(d, LineSDF(rotUV, float2(-r * SQRT3 * 0.5, -r * 0.5), float2(r * SQRT3 * 0.25, r * 0.25), thickness * 0.5));
        d = min(d, LineSDF(rotUV, float2(r * SQRT3 * 0.5, -r * 0.5), float2(-r * SQRT3 * 0.25, r * 0.25), thickness * 0.5));
    }

    return d;
}

// Pattern 9: Platonic Solids - Morphing between different solids
float PlatonicSolidsSDF(float2 uv, float radius, float thickness, float time, float morphPhase, float intensity)
{
    float d = 1e10;

    // Use morphPhase to smoothly transition between solids
    float solidCycle = morphPhase * 5.0;  // 5 solids
    int solidIndex = int(floor(solidCycle)) % 5;
    int nextSolid = (solidIndex + 1) % 5;
    float blendFactor = frac(solidCycle);

    // Smooth blend using smoothstep
    blendFactor = smoothstep(0.0, 1.0, blendFactor);

    // Rotation continues
    float angle = time * 0.5;
    float2 rotUV = Rotate2D(uv, angle);

    float r = radius * 0.8;

    // Get SDF for current and next solid, then blend
    float d1 = GetPlatonicSolid(rotUV, r, thickness, solidIndex);
    float d2 = GetPlatonicSolid(rotUV, r, thickness, nextSolid);

    d = lerp(d1, d2, blendFactor * intensity);

    // If morphing is disabled or low, just use current solid
    if (intensity < 0.1)
    {
        int fixedSolid = int(time * 0.2) % 5;
        d = GetPlatonicSolid(rotUV, r, thickness, fixedSolid);
    }

    return d;
}

// ============================================
// Pattern Dispatcher with Morphing Support
// ============================================

float GetPatternSDF(float2 uv, int patternType, float radius, float thickness, float time, float complexity, float morphPhase, float intensity)
{
    switch (patternType)
    {
        case PATTERN_SEED_OF_LIFE:      return SeedOfLifeSDF(uv, radius, thickness, morphPhase, intensity);
        case PATTERN_FLOWER_OF_LIFE:    return FlowerOfLifeSDF(uv, radius, thickness, morphPhase, intensity);
        case PATTERN_VESICA_PISCIS:     return VesicaPiscisSDF(uv, radius, thickness, morphPhase, intensity);
        case PATTERN_MERKABA:           return MerkabaSDF(uv, radius, thickness, morphPhase, intensity);
        case PATTERN_SRI_YANTRA:        return SriYantraSDF(uv, radius, thickness, morphPhase, intensity);
        case PATTERN_METATRONS_CUBE:    return MetatronsCubeSDF(uv, radius, thickness, morphPhase, intensity);
        case PATTERN_TREE_OF_LIFE:      return TreeOfLifeSDF(uv, radius, thickness, morphPhase, intensity);
        case PATTERN_TORUS:             return TorusSDF(uv, radius, thickness, time, morphPhase, intensity);
        case PATTERN_TETRAHEDRON_GRID:  return TetrahedronGridSDF(uv, radius, thickness, morphPhase, intensity);
        case PATTERN_PLATONIC_SOLIDS:   return PlatonicSolidsSDF(uv, radius, thickness, time, morphPhase, intensity);
        default:                        return SeedOfLifeSDF(uv, radius, thickness, morphPhase, intensity);
    }
}

// Blend between two patterns for smooth morphing
float GetMorphedPatternSDF(float2 uv, int pattern1, int pattern2, float blend, float radius, float thickness, float time, float complexity, float morphPhase, float intensity)
{
    float d1 = GetPatternSDF(uv, pattern1, radius, thickness, time, complexity, morphPhase, intensity);
    float d2 = GetPatternSDF(uv, pattern2, radius, thickness, time, complexity, morphPhase, intensity);

    // Smooth blend between patterns
    float smoothBlend = smoothstep(0.0, 1.0, blend);
    return lerp(d1, d2, smoothBlend);
}

// ============================================
// Appearance Mode Calculations
// ============================================

float GetAppearanceFactor(float lifetime, float maxLifetime, int mode)
{
    float t = lifetime / maxLifetime;

    float fadeInT = 1.0 - t;
    float fadeOutT = t;

    float fadeIn = smoothstep(0.0, FadeInDuration / maxLifetime, fadeInT);
    float fadeOut = smoothstep(0.0, FadeOutDuration / maxLifetime, fadeOutT);
    float fade = fadeIn * fadeOut;

    float scaleIn = smoothstep(0.0, ScaleInDuration / maxLifetime, fadeInT);
    float scaleOut = smoothstep(0.0, ScaleOutDuration / maxLifetime, fadeOutT);
    float scale = scaleIn * scaleOut;

    if (mode == 0) return fade;
    if (mode == 1) return scale;
    return fade * scale;
}

float GetScaleFactor(float lifetime, float maxLifetime, int mode)
{
    if (mode == 0) return 1.0;

    float t = lifetime / maxLifetime;
    float fadeInT = 1.0 - t;
    float fadeOutT = t;

    float scaleIn = smoothstep(0.0, ScaleInDuration / maxLifetime, fadeInT);
    float scaleOut = smoothstep(0.0, ScaleOutDuration / maxLifetime, fadeOutT);

    return scaleIn * scaleOut;
}

// ============================================
// Vertex Shader
// ============================================

VSOutput VSMain(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    VSOutput output;

    MandalaInstance m = Mandalas[instanceId];

    float scaleFactor = GetScaleFactor(m.Lifetime, m.MaxLifetime, int(m.AppearMode));
    float effectiveRadius = m.Radius * scaleFactor;

    float quadSize = effectiveRadius * 2.5;

    float2 quadVerts[6] = {
        float2(-1, -1), float2(1, -1), float2(-1, 1),
        float2(-1, 1), float2(1, -1), float2(1, 1)
    };

    float2 localPos = quadVerts[vertexId] * quadSize;

    float2 screenPos = m.Position + localPos;

    float2 ndc = (screenPos / ViewportSize) * 2.0 - 1.0;
    ndc.y = -ndc.y;

    output.Position = float4(ndc, 0.0, 1.0);
    output.TexCoord = quadVerts[vertexId] * 0.5 + 0.5;
    output.LocalPos = localPos;
    output.InstanceId = instanceId;

    return output;
}

// ============================================
// Pixel Shader
// ============================================

float4 PSMain(VSOutput input) : SV_TARGET
{
    MandalaInstance m = Mandalas[input.InstanceId];

    float appearFactor = GetAppearanceFactor(m.Lifetime, m.MaxLifetime, int(m.AppearMode));
    float scaleFactor = GetScaleFactor(m.Lifetime, m.MaxLifetime, int(m.AppearMode));

    if (appearFactor < 0.001) discard;

    float2 rotatedPos = Rotate2D(input.LocalPos, m.Rotation);
    float2 scaledPos = rotatedPos / max(scaleFactor, 0.001);

    float thickness = LineThickness;

    // Get morph intensity (0 if disabled)
    float morphIntensity = MorphEnabled ? MorphIntensity : 0.0;

    float sdf;

    // Check if morphing between patterns
    if (MorphEnabled && MorphBetweenPatterns && m.MorphTargetPattern >= 0)
    {
        // Morph between current pattern and target pattern
        sdf = GetMorphedPatternSDF(
            scaledPos,
            int(m.PatternIndex),
            int(m.MorphTargetPattern),
            m.MorphPhase,  // Use morph phase as blend factor
            m.Radius,
            thickness,
            Time + m.SpawnTime,
            m.PatternComplexity,
            m.MorphPhase,
            morphIntensity
        );
    }
    else
    {
        // Single pattern with internal animation
        sdf = GetPatternSDF(
            scaledPos,
            int(m.PatternIndex),
            m.Radius,
            thickness,
            Time + m.SpawnTime,
            m.PatternComplexity,
            m.MorphPhase,
            morphIntensity
        );
    }

    float dist = abs(sdf);

    // Multi-layer glow effect
    float coreGlow = exp(-dist * dist * 0.1) * 1.5;
    float midGlow = exp(-dist * 0.05) * 0.8;
    float outerGlow = exp(-dist * 0.02) * 0.3;

    float glow = (coreGlow + midGlow + outerGlow) * GlowIntensity;

    // Twinkle effect
    float twinkle = 1.0 + sin(Time * 15.0 + m.SpawnTime * 10.0 + length(scaledPos) * 0.1) * TwinkleIntensity;
    glow *= twinkle;

    float4 color = m.Color;

    float centerDist = length(scaledPos) / m.Radius;
    color = lerp(color, SecondaryColor, centerDist * 0.3);

    float alpha = saturate(glow * appearFactor);

    float hdrBoost = 1.0 + coreGlow * HdrMultiplier * 0.5;

    float4 result = float4(color.rgb * hdrBoost, alpha);

    if (result.a < 0.01) discard;

    return result;
}

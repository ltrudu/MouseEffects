// MandalaShader.hlsl - Sacred Geometry Mandala Renderer
// Procedurally draws sacred geometry patterns with glow effects

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

    float4 Padding2;
    float4 Padding3;
};

// Mandala instance data
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
// Sacred Geometry Pattern Functions
// ============================================

// Pattern 0: Seed of Life - 7 overlapping circles
float SeedOfLifeSDF(float2 uv, float radius, float thickness)
{
    float d = 1e10;
    float r = radius * 0.5;

    // Center circle
    d = min(d, RingSDF(uv, r, thickness));

    // 6 surrounding circles
    [unroll]
    for (int i = 0; i < 6; i++)
    {
        float angle = float(i) * TAU / 6.0;
        float2 offset = float2(cos(angle), sin(angle)) * r;
        d = min(d, RingSDF(uv - offset, r, thickness));
    }

    return d;
}

// Pattern 1: Flower of Life - 19 overlapping circles
float FlowerOfLifeSDF(float2 uv, float radius, float thickness)
{
    float d = 1e10;
    float r = radius / 3.0;

    // Center circle
    d = min(d, RingSDF(uv, r, thickness));

    // First ring - 6 circles
    [unroll]
    for (int i = 0; i < 6; i++)
    {
        float angle = float(i) * TAU / 6.0;
        float2 offset = float2(cos(angle), sin(angle)) * r;
        d = min(d, RingSDF(uv - offset, r, thickness));
    }

    // Second ring - 12 circles
    [unroll]
    for (int j = 0; j < 12; j++)
    {
        float angle = float(j) * TAU / 12.0;
        float2 offset = float2(cos(angle), sin(angle)) * r * 2.0;
        d = min(d, RingSDF(uv - offset, r, thickness));
    }

    return d;
}

// Pattern 2: Vesica Piscis - 2 overlapping circles
float VesicaPiscisSDF(float2 uv, float radius, float thickness)
{
    float d = 1e10;
    float offset = radius * 0.5;

    // Left circle
    d = min(d, RingSDF(uv - float2(-offset, 0), radius, thickness));
    // Right circle
    d = min(d, RingSDF(uv - float2(offset, 0), radius, thickness));

    return d;
}

// Pattern 3: Merkaba - Star tetrahedron (2 interlocking triangles)
float MerkabaSDF(float2 uv, float radius, float thickness)
{
    float d = 1e10;
    float r = radius * 0.9;

    // Upward triangle
    float2 t1a = float2(0, r);
    float2 t1b = float2(-r * SQRT3 * 0.5, -r * 0.5);
    float2 t1c = float2(r * SQRT3 * 0.5, -r * 0.5);
    d = min(d, TriangleSDF(uv, t1a, t1b, t1c, thickness));

    // Downward triangle (inverted)
    float2 t2a = float2(0, -r);
    float2 t2b = float2(-r * SQRT3 * 0.5, r * 0.5);
    float2 t2c = float2(r * SQRT3 * 0.5, r * 0.5);
    d = min(d, TriangleSDF(uv, t2a, t2b, t2c, thickness));

    return d;
}

// Pattern 4: Sri Yantra - 9 interlocking triangles
float SriYantraSDF(float2 uv, float radius, float thickness)
{
    float d = 1e10;

    // Outer circle
    d = min(d, RingSDF(uv, radius, thickness));

    // 4 upward triangles (varying sizes)
    float scales[4] = { 0.95, 0.75, 0.55, 0.35 };
    [unroll]
    for (int i = 0; i < 4; i++)
    {
        float r = radius * scales[i];
        float2 t1a = float2(0, r);
        float2 t1b = float2(-r * SQRT3 * 0.5, -r * 0.5);
        float2 t1c = float2(r * SQRT3 * 0.5, -r * 0.5);
        d = min(d, TriangleSDF(uv, t1a, t1b, t1c, thickness));
    }

    // 5 downward triangles (varying sizes)
    float scales2[5] = { 0.85, 0.65, 0.45, 0.25, 0.1 };
    [unroll]
    for (int j = 0; j < 5; j++)
    {
        float r = radius * scales2[j];
        float yOffset = radius * 0.05 * float(j);
        float2 t2a = float2(0, -r + yOffset);
        float2 t2b = float2(-r * SQRT3 * 0.5, r * 0.5 + yOffset);
        float2 t2c = float2(r * SQRT3 * 0.5, r * 0.5 + yOffset);
        d = min(d, TriangleSDF(uv, t2a, t2b, t2c, thickness));
    }

    // Center point (bindu)
    d = min(d, CircleSDF(uv, thickness * 3.0));

    return d;
}

// Pattern 5: Metatron's Cube - 13 circles with connecting lines
float MetatronsCubeSDF(float2 uv, float radius, float thickness)
{
    float d = 1e10;
    float r = radius * 0.15;  // Node radius

    // Store positions for lines
    float2 positions[13];
    int posIndex = 0;

    // Center circle
    positions[posIndex++] = float2(0, 0);
    d = min(d, RingSDF(uv, r, thickness));

    // Inner hexagon (6 circles)
    [unroll]
    for (int i = 0; i < 6; i++)
    {
        float angle = float(i) * TAU / 6.0;
        float2 pos = float2(cos(angle), sin(angle)) * radius * 0.5;
        positions[posIndex++] = pos;
        d = min(d, RingSDF(uv - pos, r, thickness));
    }

    // Outer hexagon (6 circles) - rotated 30 degrees
    [unroll]
    for (int j = 0; j < 6; j++)
    {
        float angle = float(j) * TAU / 6.0 + PI / 6.0;
        float2 pos = float2(cos(angle), sin(angle)) * radius;
        positions[posIndex++] = pos;
        d = min(d, RingSDF(uv - pos, r, thickness));
    }

    // Connecting lines - connect all 13 points
    // Center to inner hexagon
    [unroll]
    for (int k = 1; k <= 6; k++)
    {
        d = min(d, LineSDF(uv, positions[0], positions[k], thickness * 0.5));
    }

    // Inner hexagon edges
    [unroll]
    for (int m = 1; m <= 6; m++)
    {
        int next = m == 6 ? 1 : m + 1;
        d = min(d, LineSDF(uv, positions[m], positions[next], thickness * 0.5));
    }

    // Inner to outer connections
    [unroll]
    for (int n = 1; n <= 6; n++)
    {
        d = min(d, LineSDF(uv, positions[n], positions[n + 6], thickness * 0.5));
        int nextOuter = n == 6 ? 7 : n + 7;
        d = min(d, LineSDF(uv, positions[n], positions[nextOuter], thickness * 0.5));
    }

    // Outer hexagon edges
    [unroll]
    for (int p = 7; p <= 12; p++)
    {
        int next = p == 12 ? 7 : p + 1;
        d = min(d, LineSDF(uv, positions[p], positions[next], thickness * 0.5));
    }

    return d;
}

// Pattern 6: Tree of Life - 10 sephiroth with paths
float TreeOfLifeSDF(float2 uv, float radius, float thickness)
{
    float d = 1e10;
    float nodeR = radius * 0.1;

    // 10 Sephiroth positions (Kabbalistic Tree of Life layout)
    float2 sephiroth[10];
    sephiroth[0] = float2(0, radius * 0.9);           // 1. Kether (Crown)
    sephiroth[1] = float2(-radius * 0.45, radius * 0.6);  // 2. Chokmah (Wisdom)
    sephiroth[2] = float2(radius * 0.45, radius * 0.6);   // 3. Binah (Understanding)
    sephiroth[3] = float2(-radius * 0.45, radius * 0.2);  // 4. Chesed (Mercy)
    sephiroth[4] = float2(radius * 0.45, radius * 0.2);   // 5. Geburah (Severity)
    sephiroth[5] = float2(0, 0);                          // 6. Tiphareth (Beauty)
    sephiroth[6] = float2(-radius * 0.45, -radius * 0.4); // 7. Netzach (Victory)
    sephiroth[7] = float2(radius * 0.45, -radius * 0.4);  // 8. Hod (Splendor)
    sephiroth[8] = float2(0, -radius * 0.6);              // 9. Yesod (Foundation)
    sephiroth[9] = float2(0, -radius * 0.9);              // 10. Malkuth (Kingdom)

    // Draw nodes (circles)
    [unroll]
    for (int i = 0; i < 10; i++)
    {
        d = min(d, RingSDF(uv - sephiroth[i], nodeR, thickness));
    }

    // Draw paths (22 connections)
    // Pillar connections and cross-connections
    d = min(d, LineSDF(uv, sephiroth[0], sephiroth[1], thickness * 0.5));  // Kether-Chokmah
    d = min(d, LineSDF(uv, sephiroth[0], sephiroth[2], thickness * 0.5));  // Kether-Binah
    d = min(d, LineSDF(uv, sephiroth[1], sephiroth[2], thickness * 0.5));  // Chokmah-Binah
    d = min(d, LineSDF(uv, sephiroth[1], sephiroth[3], thickness * 0.5));  // Chokmah-Chesed
    d = min(d, LineSDF(uv, sephiroth[1], sephiroth[5], thickness * 0.5));  // Chokmah-Tiphareth
    d = min(d, LineSDF(uv, sephiroth[2], sephiroth[4], thickness * 0.5));  // Binah-Geburah
    d = min(d, LineSDF(uv, sephiroth[2], sephiroth[5], thickness * 0.5));  // Binah-Tiphareth
    d = min(d, LineSDF(uv, sephiroth[3], sephiroth[4], thickness * 0.5));  // Chesed-Geburah
    d = min(d, LineSDF(uv, sephiroth[3], sephiroth[5], thickness * 0.5));  // Chesed-Tiphareth
    d = min(d, LineSDF(uv, sephiroth[3], sephiroth[6], thickness * 0.5));  // Chesed-Netzach
    d = min(d, LineSDF(uv, sephiroth[4], sephiroth[5], thickness * 0.5));  // Geburah-Tiphareth
    d = min(d, LineSDF(uv, sephiroth[4], sephiroth[7], thickness * 0.5));  // Geburah-Hod
    d = min(d, LineSDF(uv, sephiroth[5], sephiroth[6], thickness * 0.5));  // Tiphareth-Netzach
    d = min(d, LineSDF(uv, sephiroth[5], sephiroth[7], thickness * 0.5));  // Tiphareth-Hod
    d = min(d, LineSDF(uv, sephiroth[5], sephiroth[8], thickness * 0.5));  // Tiphareth-Yesod
    d = min(d, LineSDF(uv, sephiroth[6], sephiroth[7], thickness * 0.5));  // Netzach-Hod
    d = min(d, LineSDF(uv, sephiroth[6], sephiroth[8], thickness * 0.5));  // Netzach-Yesod
    d = min(d, LineSDF(uv, sephiroth[7], sephiroth[8], thickness * 0.5));  // Hod-Yesod
    d = min(d, LineSDF(uv, sephiroth[8], sephiroth[9], thickness * 0.5));  // Yesod-Malkuth

    return d;
}

// Pattern 7: Torus - Animated concentric rings (3D illusion)
float TorusSDF(float2 uv, float radius, float thickness, float time)
{
    float d = 1e10;
    int numRings = 16;

    [unroll]
    for (int i = 0; i < numRings; i++)
    {
        float t = float(i) / float(numRings);
        // Simulate 3D torus cross-section with depth
        float depth = cos(t * TAU + time * 2.0);
        float ringRadius = radius * (0.3 + 0.7 * (depth * 0.5 + 0.5));
        float ringThickness = thickness * (0.5 + 0.5 * (depth * 0.5 + 0.5));
        float ringY = radius * 0.4 * sin(t * TAU + time * 2.0);

        d = min(d, RingSDF(uv - float2(0, ringY), ringRadius, ringThickness));
    }

    return d;
}

// Pattern 8: 64 Tetrahedron Grid - Triangular lattice
float TetrahedronGridSDF(float2 uv, float radius, float thickness)
{
    float d = 1e10;
    float gridSize = radius / 4.0;

    // Create triangular grid
    int gridCount = 4;

    for (int row = -gridCount; row <= gridCount; row++)
    {
        for (int col = -gridCount; col <= gridCount; col++)
        {
            // Offset every other row for triangular pattern
            float xOffset = (row % 2) * gridSize * 0.5;
            float2 cellCenter = float2(col * gridSize + xOffset, row * gridSize * SQRT3 * 0.5);

            // Skip if outside radius
            if (length(cellCenter) > radius * 1.1) continue;

            // Upward triangle
            float2 t1a = cellCenter + float2(0, gridSize * 0.33);
            float2 t1b = cellCenter + float2(-gridSize * 0.29, -gridSize * 0.17);
            float2 t1c = cellCenter + float2(gridSize * 0.29, -gridSize * 0.17);
            d = min(d, TriangleSDF(uv, t1a, t1b, t1c, thickness * 0.5));

            // Downward triangle (offset)
            float2 t2Center = cellCenter + float2(gridSize * 0.5, gridSize * SQRT3 * 0.17);
            float2 t2a = t2Center + float2(0, -gridSize * 0.33);
            float2 t2b = t2Center + float2(-gridSize * 0.29, gridSize * 0.17);
            float2 t2c = t2Center + float2(gridSize * 0.29, gridSize * 0.17);
            d = min(d, TriangleSDF(uv, t2a, t2b, t2c, thickness * 0.5));
        }
    }

    // Outer boundary circle
    d = min(d, RingSDF(uv, radius, thickness));

    return d;
}

// Pattern 9: Platonic Solids - Rotating 2D projections
float PlatonicSolidsSDF(float2 uv, float radius, float thickness, float time)
{
    float d = 1e10;

    // Animate between different solids
    float solidPhase = frac(time * 0.2);
    int solidIndex = int(time * 0.2) % 5;

    // Rotation based on time
    float angle = time * 0.5;
    float2 rotUV = Rotate2D(uv, angle);

    float r = radius * 0.8;

    // Draw based on solid type
    if (solidIndex == 0)
    {
        // Tetrahedron (4 faces) - equilateral triangle
        float2 t1a = float2(0, r);
        float2 t1b = float2(-r * SQRT3 * 0.5, -r * 0.5);
        float2 t1c = float2(r * SQRT3 * 0.5, -r * 0.5);
        d = min(d, TriangleSDF(rotUV, t1a, t1b, t1c, thickness));
        // Inner lines to suggest 3D
        d = min(d, LineSDF(rotUV, float2(0, 0), t1a, thickness * 0.5));
        d = min(d, LineSDF(rotUV, float2(0, 0), t1b, thickness * 0.5));
        d = min(d, LineSDF(rotUV, float2(0, 0), t1c, thickness * 0.5));
    }
    else if (solidIndex == 1)
    {
        // Cube (6 faces) - square with diagonals
        float s = r * 0.7;
        d = min(d, LineSDF(rotUV, float2(-s, -s), float2(s, -s), thickness));
        d = min(d, LineSDF(rotUV, float2(s, -s), float2(s, s), thickness));
        d = min(d, LineSDF(rotUV, float2(s, s), float2(-s, s), thickness));
        d = min(d, LineSDF(rotUV, float2(-s, s), float2(-s, -s), thickness));
        // Inner cube (perspective)
        float s2 = s * 0.5;
        float offset = s * 0.3;
        d = min(d, LineSDF(rotUV, float2(-s2 + offset, -s2 + offset), float2(s2 + offset, -s2 + offset), thickness * 0.7));
        d = min(d, LineSDF(rotUV, float2(s2 + offset, -s2 + offset), float2(s2 + offset, s2 + offset), thickness * 0.7));
        d = min(d, LineSDF(rotUV, float2(s2 + offset, s2 + offset), float2(-s2 + offset, s2 + offset), thickness * 0.7));
        d = min(d, LineSDF(rotUV, float2(-s2 + offset, s2 + offset), float2(-s2 + offset, -s2 + offset), thickness * 0.7));
        // Connecting lines
        d = min(d, LineSDF(rotUV, float2(-s, -s), float2(-s2 + offset, -s2 + offset), thickness * 0.5));
        d = min(d, LineSDF(rotUV, float2(s, -s), float2(s2 + offset, -s2 + offset), thickness * 0.5));
        d = min(d, LineSDF(rotUV, float2(s, s), float2(s2 + offset, s2 + offset), thickness * 0.5));
        d = min(d, LineSDF(rotUV, float2(-s, s), float2(-s2 + offset, s2 + offset), thickness * 0.5));
    }
    else if (solidIndex == 2)
    {
        // Octahedron (8 faces) - diamond shape
        d = min(d, LineSDF(rotUV, float2(0, r), float2(r, 0), thickness));
        d = min(d, LineSDF(rotUV, float2(r, 0), float2(0, -r), thickness));
        d = min(d, LineSDF(rotUV, float2(0, -r), float2(-r, 0), thickness));
        d = min(d, LineSDF(rotUV, float2(-r, 0), float2(0, r), thickness));
        // Cross lines
        d = min(d, LineSDF(rotUV, float2(-r, 0), float2(r, 0), thickness * 0.5));
        d = min(d, LineSDF(rotUV, float2(0, -r), float2(0, r), thickness * 0.5));
    }
    else if (solidIndex == 3)
    {
        // Dodecahedron (12 faces) - pentagon
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
        // Inner pentagon
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
        // Icosahedron (20 faces) - complex triangular mesh
        // Outer triangle
        d = min(d, TriangleSDF(rotUV,
            float2(0, r),
            float2(-r * SQRT3 * 0.5, -r * 0.5),
            float2(r * SQRT3 * 0.5, -r * 0.5), thickness));
        // Inner inverted triangle
        d = min(d, TriangleSDF(rotUV,
            float2(0, -r * 0.5),
            float2(-r * SQRT3 * 0.25, r * 0.25),
            float2(r * SQRT3 * 0.25, r * 0.25), thickness * 0.7));
        // Connecting to vertices
        d = min(d, LineSDF(rotUV, float2(0, r), float2(0, -r * 0.5), thickness * 0.5));
        d = min(d, LineSDF(rotUV, float2(-r * SQRT3 * 0.5, -r * 0.5), float2(r * SQRT3 * 0.25, r * 0.25), thickness * 0.5));
        d = min(d, LineSDF(rotUV, float2(r * SQRT3 * 0.5, -r * 0.5), float2(-r * SQRT3 * 0.25, r * 0.25), thickness * 0.5));
    }

    return d;
}

// ============================================
// Pattern Dispatcher
// ============================================

float GetPatternSDF(float2 uv, int patternType, float radius, float thickness, float time, float complexity)
{
    switch (patternType)
    {
        case PATTERN_SEED_OF_LIFE:      return SeedOfLifeSDF(uv, radius, thickness);
        case PATTERN_FLOWER_OF_LIFE:    return FlowerOfLifeSDF(uv, radius, thickness);
        case PATTERN_VESICA_PISCIS:     return VesicaPiscisSDF(uv, radius, thickness);
        case PATTERN_MERKABA:           return MerkabaSDF(uv, radius, thickness);
        case PATTERN_SRI_YANTRA:        return SriYantraSDF(uv, radius, thickness);
        case PATTERN_METATRONS_CUBE:    return MetatronsCubeSDF(uv, radius, thickness);
        case PATTERN_TREE_OF_LIFE:      return TreeOfLifeSDF(uv, radius, thickness);
        case PATTERN_TORUS:             return TorusSDF(uv, radius, thickness, time);
        case PATTERN_TETRAHEDRON_GRID:  return TetrahedronGridSDF(uv, radius, thickness);
        case PATTERN_PLATONIC_SOLIDS:   return PlatonicSolidsSDF(uv, radius, thickness, time);
        default:                        return SeedOfLifeSDF(uv, radius, thickness);
    }
}

// ============================================
// Appearance Mode Calculations
// ============================================

float GetAppearanceFactor(float lifetime, float maxLifetime, int mode)
{
    float t = lifetime / maxLifetime;  // 1 at spawn, 0 at death

    // Fade calculations
    float fadeInT = 1.0 - t;  // 0 at spawn, goes to 1
    float fadeOutT = t;       // 1 at spawn, goes to 0

    float fadeIn = smoothstep(0.0, FadeInDuration / maxLifetime, fadeInT);
    float fadeOut = smoothstep(0.0, FadeOutDuration / maxLifetime, fadeOutT);
    float fade = fadeIn * fadeOut;

    // Scale calculations
    float scaleIn = smoothstep(0.0, ScaleInDuration / maxLifetime, fadeInT);
    float scaleOut = smoothstep(0.0, ScaleOutDuration / maxLifetime, fadeOutT);
    float scale = scaleIn * scaleOut;

    if (mode == 0) return fade;      // Fade only
    if (mode == 1) return scale;     // Scale only
    return fade * scale;              // Both
}

float GetScaleFactor(float lifetime, float maxLifetime, int mode)
{
    if (mode == 0) return 1.0;  // Fade mode - no scale

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

    // Calculate scale factor for appearance mode
    float scaleFactor = GetScaleFactor(m.Lifetime, m.MaxLifetime, int(m.AppearMode));
    float effectiveRadius = m.Radius * scaleFactor;

    // Generate fullscreen-ish quad vertices for this mandala
    // We create a quad large enough to contain the mandala + glow
    float quadSize = effectiveRadius * 2.5;  // Extra space for glow

    // Quad vertices (2 triangles = 6 vertices)
    float2 quadVerts[6] = {
        float2(-1, -1), float2(1, -1), float2(-1, 1),
        float2(-1, 1), float2(1, -1), float2(1, 1)
    };

    float2 localPos = quadVerts[vertexId] * quadSize;

    // Transform to screen position
    float2 screenPos = m.Position + localPos;

    // Convert to NDC
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

    // Get appearance factors
    float appearFactor = GetAppearanceFactor(m.Lifetime, m.MaxLifetime, int(m.AppearMode));
    float scaleFactor = GetScaleFactor(m.Lifetime, m.MaxLifetime, int(m.AppearMode));

    // Early discard if fully faded
    if (appearFactor < 0.001) discard;

    // Apply rotation to local position
    float2 rotatedPos = Rotate2D(input.LocalPos, m.Rotation);

    // Scale the coordinates (inverse of visual scale - larger scaleFactor = smaller coords for same visual size)
    float2 scaledPos = rotatedPos / max(scaleFactor, 0.001);

    // Get pattern SDF
    float thickness = LineThickness;
    float sdf = GetPatternSDF(scaledPos, int(m.PatternIndex), m.Radius, thickness, Time + m.SpawnTime, m.PatternComplexity);

    // Convert SDF to glow
    float dist = abs(sdf);

    // Multi-layer glow effect
    float coreGlow = exp(-dist * dist * 0.1) * 1.5;   // Bright core
    float midGlow = exp(-dist * 0.05) * 0.8;          // Medium spread
    float outerGlow = exp(-dist * 0.02) * 0.3;        // Wide ambient

    float glow = (coreGlow + midGlow + outerGlow) * GlowIntensity;

    // Apply twinkle effect
    float twinkle = 1.0 + sin(Time * 15.0 + m.SpawnTime * 10.0 + length(scaledPos) * 0.1) * TwinkleIntensity;
    glow *= twinkle;

    // Get color
    float4 color = m.Color;

    // Add secondary color variation based on distance from center
    float centerDist = length(scaledPos) / m.Radius;
    color = lerp(color, SecondaryColor, centerDist * 0.3);

    // Apply glow and appearance factor
    float alpha = saturate(glow * appearFactor);

    // HDR brightness boost for core
    float hdrBoost = 1.0 + coreGlow * HdrMultiplier * 0.5;

    // Final color
    float4 result = float4(color.rgb * hdrBoost, alpha);

    // Discard near-transparent pixels
    if (result.a < 0.01) discard;

    return result;
}

// Sacred Geometry Shapes Shader
// Renders individual sacred geometry shapes with glow and animation effects

static const float PI = 3.14159265359;
static const float TAU = 6.28318530718;
static const float PHI = 1.61803398875; // Golden ratio

cbuffer ShapesConstants : register(b0)
{
    float2 ViewportSize;
    float Time;
    float GlowIntensity;
    float HdrMultiplier;
    float LineThickness;
    int ActiveShapeCount;
    float TwinkleIntensity;
    float4 PrimaryColor;
    float4 SecondaryColor;
    float RainbowSpeed;
    int RainbowEnabled;
    float AnimationSpeed;
    int MorphEnabled;
    float MorphSpeed;
    float MorphIntensity;
    int MorphBetweenShapes;
    int IndependentRainbow;
    float3 Padding1;
    float Padding1b;
    float4 Padding2;
};

struct ShapeInstance
{
    float2 Position;
    float Radius;
    float Rotation;
    float RotationSpeed;
    float RotationDirection;
    float Lifetime;
    float MaxLifetime;
    int ShapeIndex;
    int AppearMode;
    float SpawnTime;
    float AnimPhase;
    float4 Color;
    float MorphPhase;
    int MorphTargetShape;
    float MorphSpeedMult;
    float Padding;
};

StructuredBuffer<ShapeInstance> Shapes : register(t0);

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
    float2 LocalPos : TEXCOORD1;
    nointerpolation float4 Color : COLOR0;
    nointerpolation float Rotation : TEXCOORD2;
    nointerpolation int ShapeIndex : TEXCOORD3;
    nointerpolation float LifeFactor : TEXCOORD4;
    nointerpolation float AnimPhase : TEXCOORD5;
    nointerpolation float AppearFactor : TEXCOORD6;
    nointerpolation float MorphPhase : TEXCOORD7;
    nointerpolation int MorphTargetShape : TEXCOORD8;
};

// 2D rotation matrix
float2x2 Rotate2D(float angle)
{
    float c = cos(angle);
    float s = sin(angle);
    return float2x2(c, -s, s, c);
}

// Basic SDF primitives
float CircleSDF(float2 p, float r)
{
    return length(p) - r;
}

float LineSDF(float2 p, float2 a, float2 b, float thickness)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float h = saturate(dot(pa, ba) / dot(ba, ba));
    return length(pa - ba * h) - thickness;
}

float RingSDF(float2 p, float r, float thickness)
{
    return abs(length(p) - r) - thickness;
}

// Triangle SDF
float TriangleSDF(float2 p, float size)
{
    float k = sqrt(3.0);
    p.x = abs(p.x) - size;
    p.y = p.y + size / k;
    if (p.x + k * p.y > 0.0)
        p = float2(p.x - k * p.y, -k * p.x - p.y) / 2.0;
    p.x -= clamp(p.x, -2.0 * size, 0.0);
    return -length(p) * sign(p.y);
}

// Hexagon SDF
float HexagonSDF(float2 p, float r)
{
    float3 k = float3(-0.866025404, 0.5, 0.577350269);
    p = abs(p);
    p -= 2.0 * min(dot(k.xy, p), 0.0) * k.xy;
    p -= float2(clamp(p.x, -k.z * r, k.z * r), r);
    return length(p) * sign(p.y);
}

// Pentagon SDF
float PentagonSDF(float2 p, float r)
{
    float3 k = float3(0.809016994, 0.587785252, 0.726542528);
    p.x = abs(p.x);
    p -= 2.0 * min(dot(float2(-k.x, k.y), p), 0.0) * float2(-k.x, k.y);
    p -= 2.0 * min(dot(float2(k.x, k.y), p), 0.0) * float2(k.x, k.y);
    p -= float2(clamp(p.x, -r * k.z, r * k.z), r);
    return length(p) * sign(p.y);
}

// Star polygon SDF (n points)
float StarSDF(float2 p, float r, int n, float m)
{
    float an = PI / float(n);
    float en = PI / m;
    float2 acs = float2(cos(an), sin(an));
    float2 ecs = float2(cos(en), sin(en));

    float bn = fmod(atan2(p.x, p.y) + PI, 2.0 * an) - an;
    p = length(p) * float2(cos(bn), abs(sin(bn)));
    p -= r * acs;
    p += ecs * clamp(-dot(p, ecs), 0.0, r * acs.y / ecs.y);
    return length(p) * sign(p.x);
}

// ============================================
// SHAPE 0: Vesica Piscis - Two overlapping circles
// ============================================
float VesicaPiscisSDF(float2 p, float anim)
{
    float separation = 0.3 + sin(anim) * 0.05;
    float r = 0.5;

    float c1 = CircleSDF(p - float2(-separation, 0), r);
    float c2 = CircleSDF(p - float2(separation, 0), r);

    float ring1 = abs(c1) - LineThickness;
    float ring2 = abs(c2) - LineThickness;

    return min(ring1, ring2);
}

// ============================================
// SHAPE 1: Seed of Life - 7 circles
// ============================================
float SeedOfLifeSDF(float2 p, float anim)
{
    float result = 1e10;
    float r = 0.3;
    float breathe = 1.0 + sin(anim * 0.5) * 0.05;

    // Center circle
    result = min(result, abs(CircleSDF(p, r * breathe)) - LineThickness);

    // 6 surrounding circles
    for (int i = 0; i < 6; i++)
    {
        float angle = float(i) * TAU / 6.0 + anim * 0.1;
        float2 offset = float2(cos(angle), sin(angle)) * r * breathe;
        result = min(result, abs(CircleSDF(p - offset, r * breathe)) - LineThickness);
    }

    return result;
}

// ============================================
// SHAPE 2: Flower of Life - 19 circles
// ============================================
float FlowerOfLifeSDF(float2 p, float anim)
{
    float result = 1e10;
    float r = 0.2;
    float breathe = 1.0 + sin(anim * 0.3) * 0.03;

    // Center
    result = min(result, abs(CircleSDF(p, r * breathe)) - LineThickness);

    // Inner ring (6 circles)
    for (int i = 0; i < 6; i++)
    {
        float angle = float(i) * TAU / 6.0;
        float2 offset = float2(cos(angle), sin(angle)) * r * breathe;
        result = min(result, abs(CircleSDF(p - offset, r * breathe)) - LineThickness);
    }

    // Outer ring (12 circles)
    for (int j = 0; j < 12; j++)
    {
        float angle = float(j) * TAU / 12.0 + TAU / 24.0;
        float dist = r * 1.732 * breathe; // sqrt(3)
        float2 offset = float2(cos(angle), sin(angle)) * dist;
        result = min(result, abs(CircleSDF(p - offset, r * breathe)) - LineThickness);
    }

    return result;
}

// ============================================
// SHAPE 3: Fruit of Life - 13 circles
// ============================================
float FruitOfLifeSDF(float2 p, float anim)
{
    float result = 1e10;
    float r = 0.15;
    float pulse = 1.0 + sin(anim * 0.4) * 0.05;

    // Center circle
    result = min(result, abs(CircleSDF(p, r * pulse)) - LineThickness);

    // Inner hexagon (6 circles)
    for (int i = 0; i < 6; i++)
    {
        float angle = float(i) * TAU / 6.0 + PI / 6.0;
        float2 offset = float2(cos(angle), sin(angle)) * r * 2.0 * pulse;
        result = min(result, abs(CircleSDF(p - offset, r * pulse)) - LineThickness);
    }

    // Outer hexagon (6 circles)
    for (int j = 0; j < 6; j++)
    {
        float angle = float(j) * TAU / 6.0;
        float2 offset = float2(cos(angle), sin(angle)) * r * 3.464 * pulse; // 2*sqrt(3)
        result = min(result, abs(CircleSDF(p - offset, r * pulse)) - LineThickness);
    }

    return result;
}

// ============================================
// SHAPE 4: Egg of Life - 7 circles in egg pattern
// ============================================
float EggOfLifeSDF(float2 p, float anim)
{
    float result = 1e10;
    float r = 0.25;
    float breathe = 1.0 + sin(anim * 0.5) * 0.04;

    // Center circle
    result = min(result, abs(CircleSDF(p, r * breathe)) - LineThickness);

    // 6 surrounding circles forming egg shape
    for (int i = 0; i < 6; i++)
    {
        float angle = float(i) * TAU / 6.0 + PI / 6.0;
        float2 offset = float2(cos(angle), sin(angle)) * r * breathe;
        result = min(result, abs(CircleSDF(p - offset, r * breathe)) - LineThickness);
    }

    return result;
}

// ============================================
// SHAPE 5: Metatron's Cube - 13 circles with lines
// ============================================
float MetatronsCubeSDF(float2 p, float anim)
{
    float result = 1e10;
    float r = 0.1;
    float scale = 0.35;
    float energy = sin(anim * 0.5) * 0.5 + 0.5;

    // Store circle centers
    float2 centers[13];
    centers[0] = float2(0, 0);

    // Inner ring (6)
    for (int i = 0; i < 6; i++)
    {
        float angle = float(i) * TAU / 6.0;
        centers[i + 1] = float2(cos(angle), sin(angle)) * scale;
    }

    // Outer ring (6)
    for (int j = 0; j < 6; j++)
    {
        float angle = float(j) * TAU / 6.0 + PI / 6.0;
        centers[j + 7] = float2(cos(angle), sin(angle)) * scale * 2.0;
    }

    // Draw circles
    for (int k = 0; k < 13; k++)
    {
        result = min(result, abs(CircleSDF(p - centers[k], r)) - LineThickness);
    }

    // Draw connecting lines
    for (int m = 0; m < 13; m++)
    {
        for (int n = m + 1; n < 13; n++)
        {
            float lineDist = LineSDF(p, centers[m], centers[n], LineThickness * 0.5);
            result = min(result, lineDist);
        }
    }

    return result;
}

// ============================================
// SHAPE 6: Tree of Life - 10 Sephiroth
// ============================================
float TreeOfLifeSDF(float2 p, float anim)
{
    float result = 1e10;
    float r = 0.08;
    float energy = sin(anim * 0.3) * 0.5 + 0.5;

    // 10 Sephiroth positions (normalized to fit)
    float2 seph[10];
    seph[0] = float2(0, -0.7);      // Kether
    seph[1] = float2(-0.3, -0.5);   // Binah
    seph[2] = float2(0.3, -0.5);    // Chokmah
    seph[3] = float2(-0.3, -0.15);  // Geburah
    seph[4] = float2(0.3, -0.15);   // Chesed
    seph[5] = float2(0, 0.0);       // Tiphareth
    seph[6] = float2(-0.3, 0.25);   // Hod
    seph[7] = float2(0.3, 0.25);    // Netzach
    seph[8] = float2(0, 0.45);      // Yesod
    seph[9] = float2(0, 0.7);       // Malkuth

    // Draw spheres with pulse animation
    for (int i = 0; i < 10; i++)
    {
        float pulse = 1.0 + sin(anim + float(i) * 0.5) * 0.1;
        result = min(result, abs(CircleSDF(p - seph[i], r * pulse)) - LineThickness);
    }

    // Draw paths (22 traditional paths simplified)
    int paths[22 * 2] = {
        0,1, 0,2, 1,2, 1,3, 2,4, 1,5, 2,5, 3,4, 3,5, 4,5,
        3,6, 4,7, 5,6, 5,7, 5,8, 6,7, 6,8, 7,8, 6,9, 7,9, 8,9, 0,5
    };

    for (int j = 0; j < 22; j++)
    {
        int a = paths[j * 2];
        int b = paths[j * 2 + 1];
        result = min(result, LineSDF(p, seph[a], seph[b], LineThickness * 0.4));
    }

    return result;
}

// ============================================
// SHAPE 7: Sri Yantra - 9 interlocking triangles
// ============================================
float SriYantraSDF(float2 p, float anim)
{
    float result = 1e10;
    float wave = sin(anim * 0.3) * 0.02;

    // 4 upward triangles of different sizes
    float sizes_up[4] = { 0.7, 0.5, 0.35, 0.15 };
    float offsets_up[4] = { 0.15, 0.05, -0.02, -0.08 };

    for (int i = 0; i < 4; i++)
    {
        float2 tp = p - float2(0, offsets_up[i] + wave * float(i));
        float tri = abs(TriangleSDF(tp, sizes_up[i])) - LineThickness;
        result = min(result, tri);
    }

    // 5 downward triangles
    float sizes_down[5] = { 0.65, 0.45, 0.3, 0.2, 0.08 };
    float offsets_down[5] = { -0.2, -0.1, 0.0, 0.08, 0.12 };

    for (int j = 0; j < 5; j++)
    {
        float2 tp = p - float2(0, offsets_down[j] - wave * float(j));
        tp.y = -tp.y; // Flip for downward
        float tri = abs(TriangleSDF(tp, sizes_down[j])) - LineThickness;
        result = min(result, tri);
    }

    // Outer circles
    result = min(result, abs(CircleSDF(p, 0.85)) - LineThickness);
    result = min(result, abs(CircleSDF(p, 0.9)) - LineThickness);

    return result;
}

// ============================================
// SHAPE 8: Merkaba - Star Tetrahedron
// ============================================
float MerkabaSDF(float2 p, float anim)
{
    float result = 1e10;
    float size = 0.6;
    float pulse = 1.0 + sin(anim * 0.5) * 0.05;

    // Upward triangle
    float2 p1 = mul(Rotate2D(anim * 0.2), p);
    float tri1 = abs(TriangleSDF(p1, size * pulse)) - LineThickness;

    // Downward triangle (rotated 180 degrees)
    float2 p2 = mul(Rotate2D(-anim * 0.2 + PI), p);
    float tri2 = abs(TriangleSDF(p2, size * pulse)) - LineThickness;

    result = min(tri1, tri2);

    // Inner detail
    float innerSize = size * 0.4 * pulse;
    float inner1 = abs(TriangleSDF(p1, innerSize)) - LineThickness * 0.5;
    float2 p3 = mul(Rotate2D(PI), p1);
    float inner2 = abs(TriangleSDF(p3, innerSize)) - LineThickness * 0.5;

    result = min(result, min(inner1, inner2));

    return result;
}

// ============================================
// SHAPE 9: Hexagram - Star of David
// ============================================
float HexagramSDF(float2 p, float anim)
{
    float size = 0.55;
    float pulse = 1.0 + sin(anim * 0.4) * 0.03;

    // Upward triangle
    float tri1 = abs(TriangleSDF(p, size * pulse)) - LineThickness;

    // Downward triangle
    float2 pFlip = float2(p.x, -p.y);
    float tri2 = abs(TriangleSDF(pFlip, size * pulse)) - LineThickness;

    return min(tri1, tri2);
}

// ============================================
// SHAPE 10: Pentagram - 5-pointed star
// ============================================
float PentagramSDF(float2 p, float anim)
{
    float result = 1e10;
    float r = 0.7;
    float pulse = 1.0 + sin(anim * 0.4) * 0.03;

    // Use star SDF
    float star = abs(StarSDF(p, r * pulse, 5, 2.5)) - LineThickness;
    result = min(result, star);

    // Add outer circle
    result = min(result, abs(CircleSDF(p, r * pulse)) - LineThickness);

    return result;
}

// ============================================
// SHAPE 11: Golden Spiral
// ============================================
float GoldenSpiralSDF(float2 p, float anim)
{
    float result = 1e10;

    // Animate the spiral rotation
    float2 rp = mul(Rotate2D(anim * 0.2), p);

    // Draw Fibonacci rectangles
    float2 centers[8];
    float sizes[8];

    // Build Fibonacci sequence for sizes
    sizes[0] = 0.05;
    sizes[1] = 0.05;
    for (int i = 2; i < 8; i++)
    {
        sizes[i] = sizes[i-1] + sizes[i-2];
    }

    // Scale to fit
    float maxSize = sizes[7];
    for (int j = 0; j < 8; j++)
    {
        sizes[j] = sizes[j] / maxSize * 0.8;
    }

    // Draw quarter-circle arcs for the spiral
    float2 arcCenter = float2(0, 0);
    float baseAngle = 0;

    for (int k = 0; k < 7; k++)
    {
        float r = sizes[k];

        // Draw arc segment
        float angle = atan2(rp.y - arcCenter.y, rp.x - arcCenter.x);
        float dist = length(rp - arcCenter);

        // Quarter arc in the right direction
        float startAngle = baseAngle;
        float endAngle = baseAngle + PI * 0.5;

        if (angle >= startAngle && angle <= endAngle)
        {
            float arcDist = abs(dist - r);
            result = min(result, arcDist - LineThickness);
        }

        // Move center for next arc
        float2 offset;
        int dir = k % 4;
        if (dir == 0) offset = float2(sizes[k], 0);
        else if (dir == 1) offset = float2(0, sizes[k]);
        else if (dir == 2) offset = float2(-sizes[k], 0);
        else offset = float2(0, -sizes[k]);

        arcCenter += offset;
        baseAngle += PI * 0.5;
    }

    // Simplified: just draw a logarithmic spiral
    float angle = atan2(rp.y, rp.x);
    float dist = length(rp);

    // Golden spiral: r = a * e^(b * theta)
    float a = 0.05;
    float b = 0.306349; // ln(PHI) / (PI/2)

    // Find closest point on spiral
    float spiralAngle = angle + anim;
    for (int n = -4; n < 8; n++)
    {
        float theta = spiralAngle + float(n) * TAU;
        if (theta > 0)
        {
            float spiralR = a * exp(b * theta);
            if (spiralR < 1.0)
            {
                float2 spiralPoint = float2(cos(theta - anim), sin(theta - anim)) * spiralR;
                float d = length(rp - spiralPoint);
                result = min(result, d - LineThickness);
            }
        }
    }

    return result;
}

// ============================================
// SHAPE 12: Tetrahedron (2D projection)
// ============================================
float TetrahedronSDF(float2 p, float anim)
{
    float result = 1e10;
    float size = 0.6;

    // Rotate the projection
    float2 rp = mul(Rotate2D(anim * 0.3), p);

    // Project tetrahedron vertices
    float2 v[4];
    float phase = anim * 0.5;
    v[0] = float2(0, -size * 0.8);
    v[1] = float2(-size * 0.7 * cos(phase), size * 0.4);
    v[2] = float2(size * 0.7 * cos(phase), size * 0.4);
    v[3] = float2(0, size * 0.2 * sin(phase));

    // Draw edges
    result = min(result, LineSDF(rp, v[0], v[1], LineThickness));
    result = min(result, LineSDF(rp, v[0], v[2], LineThickness));
    result = min(result, LineSDF(rp, v[0], v[3], LineThickness));
    result = min(result, LineSDF(rp, v[1], v[2], LineThickness));
    result = min(result, LineSDF(rp, v[1], v[3], LineThickness));
    result = min(result, LineSDF(rp, v[2], v[3], LineThickness));

    return result;
}

// ============================================
// SHAPE 13: Cube (2D projection)
// ============================================
float CubeSDF(float2 p, float anim)
{
    float result = 1e10;
    float size = 0.4;

    // Animated rotation
    float rx = anim * 0.4;
    float ry = anim * 0.3;

    // 3D cube vertices projected to 2D with rotation
    float3 vertices[8];
    vertices[0] = float3(-1, -1, -1) * size;
    vertices[1] = float3(1, -1, -1) * size;
    vertices[2] = float3(1, 1, -1) * size;
    vertices[3] = float3(-1, 1, -1) * size;
    vertices[4] = float3(-1, -1, 1) * size;
    vertices[5] = float3(1, -1, 1) * size;
    vertices[6] = float3(1, 1, 1) * size;
    vertices[7] = float3(-1, 1, 1) * size;

    // Rotate and project
    float2 proj[8];
    for (int i = 0; i < 8; i++)
    {
        float3 v = vertices[i];
        // Rotate around Y
        float3 vy = float3(v.x * cos(ry) + v.z * sin(ry), v.y, -v.x * sin(ry) + v.z * cos(ry));
        // Rotate around X
        float3 vx = float3(vy.x, vy.y * cos(rx) - vy.z * sin(rx), vy.y * sin(rx) + vy.z * cos(rx));
        // Simple orthographic projection
        proj[i] = vx.xy;
    }

    // Draw edges
    int edges[12 * 2] = { 0,1, 1,2, 2,3, 3,0, 4,5, 5,6, 6,7, 7,4, 0,4, 1,5, 2,6, 3,7 };
    for (int j = 0; j < 12; j++)
    {
        result = min(result, LineSDF(p, proj[edges[j*2]], proj[edges[j*2+1]], LineThickness));
    }

    return result;
}

// ============================================
// SHAPE 14: Octahedron (2D projection)
// ============================================
float OctahedronSDF(float2 p, float anim)
{
    float result = 1e10;
    float size = 0.5;

    float rx = anim * 0.35;
    float ry = anim * 0.45;

    // Octahedron vertices
    float3 vertices[6];
    vertices[0] = float3(0, -1, 0) * size;
    vertices[1] = float3(0, 1, 0) * size;
    vertices[2] = float3(-1, 0, 0) * size;
    vertices[3] = float3(1, 0, 0) * size;
    vertices[4] = float3(0, 0, -1) * size;
    vertices[5] = float3(0, 0, 1) * size;

    float2 proj[6];
    for (int i = 0; i < 6; i++)
    {
        float3 v = vertices[i];
        float3 vy = float3(v.x * cos(ry) + v.z * sin(ry), v.y, -v.x * sin(ry) + v.z * cos(ry));
        float3 vx = float3(vy.x, vy.y * cos(rx) - vy.z * sin(rx), vy.y * sin(rx) + vy.z * cos(rx));
        proj[i] = vx.xy;
    }

    // Draw edges
    int edges[12 * 2] = { 0,2, 0,3, 0,4, 0,5, 1,2, 1,3, 1,4, 1,5, 2,4, 4,3, 3,5, 5,2 };
    for (int j = 0; j < 12; j++)
    {
        result = min(result, LineSDF(p, proj[edges[j*2]], proj[edges[j*2+1]], LineThickness));
    }

    return result;
}

// ============================================
// SHAPE 15: Dodecahedron (simplified 2D)
// ============================================
float DodecahedronSDF(float2 p, float anim)
{
    float result = 1e10;
    float size = 0.6;
    float pulse = 1.0 + sin(anim * 0.3) * 0.03;

    // Draw concentric pentagons with rotation
    for (int i = 0; i < 3; i++)
    {
        float2 rp = mul(Rotate2D(anim * 0.2 + float(i) * PI / 5.0), p);
        float r = size * (1.0 - float(i) * 0.3) * pulse;
        result = min(result, abs(PentagonSDF(rp, r)) - LineThickness);
    }

    // Connect vertices with lines
    for (int j = 0; j < 5; j++)
    {
        float angle1 = float(j) * TAU / 5.0 + anim * 0.2 - PI/2.0;
        float angle2 = float(j) * TAU / 5.0 + anim * 0.2 + PI/5.0 - PI/2.0;
        float2 p1 = float2(cos(angle1), sin(angle1)) * size * pulse;
        float2 p2 = float2(cos(angle2), sin(angle2)) * size * 0.4 * pulse;
        result = min(result, LineSDF(p, p1, p2, LineThickness * 0.5));
    }

    return result;
}

// ============================================
// SHAPE 16: Icosahedron (simplified 2D)
// ============================================
float IcosahedronSDF(float2 p, float anim)
{
    float result = 1e10;
    float size = 0.55;

    float rx = anim * 0.3;
    float ry = anim * 0.4;

    // Simplified icosahedron with triangular mesh appearance
    // Draw 3 nested triangles with rotation
    for (int i = 0; i < 3; i++)
    {
        float2 rp = mul(Rotate2D(float(i) * PI / 3.0 + anim * 0.2), p);
        float s = size * (1.0 - float(i) * 0.25);
        result = min(result, abs(TriangleSDF(rp, s)) - LineThickness);
    }

    // Add hexagonal center
    float2 hp = mul(Rotate2D(anim * 0.15), p);
    result = min(result, abs(HexagonSDF(hp, size * 0.35)) - LineThickness);

    return result;
}

// ============================================
// SHAPE 17: Torus (2D representation)
// ============================================
float TorusSDF(float2 p, float anim)
{
    float result = 1e10;

    // Animated concentric rings
    int numRings = 8;
    for (int i = 0; i < numRings; i++)
    {
        float t = float(i) / float(numRings - 1);
        float r = 0.2 + t * 0.5;
        float thickness = LineThickness * (1.0 + sin(anim + t * TAU) * 0.3);
        float offset = sin(anim * 2.0 + t * TAU) * 0.05;

        float2 rp = p + float2(offset, 0);
        result = min(result, abs(CircleSDF(rp, r)) - thickness);
    }

    return result;
}

// ============================================
// SHAPE 18: Tetrahedron Grid (64)
// ============================================
float TetrahedronGridSDF(float2 p, float anim)
{
    float result = 1e10;
    float gridSize = 0.15;
    float wave = sin(anim * 0.5) * 0.02;

    // Create triangular grid
    int gridCount = 4;
    float totalSize = float(gridCount) * gridSize;

    for (int row = 0; row <= gridCount; row++)
    {
        for (int col = 0; col <= gridCount - row; col++)
        {
            // Upward triangles
            float x = (float(col) + float(row) * 0.5) * gridSize - totalSize * 0.4;
            float y = float(row) * gridSize * 0.866 - totalSize * 0.35;

            float2 center = float2(x, y);
            center += float2(sin(anim + x * 5.0), cos(anim + y * 5.0)) * wave;

            float tri = abs(TriangleSDF(p - center, gridSize * 0.4)) - LineThickness * 0.7;
            result = min(result, tri);

            // Downward triangles (fill gaps)
            if (col < gridCount - row && row > 0)
            {
                float2 center2 = float2(x + gridSize * 0.5, y - gridSize * 0.289);
                center2 += float2(sin(anim + center2.x * 5.0), cos(anim + center2.y * 5.0)) * wave;
                float2 pc = p - center2;
                pc.y = -pc.y;
                float tri2 = abs(TriangleSDF(pc, gridSize * 0.4)) - LineThickness * 0.7;
                result = min(result, tri2);
            }
        }
    }

    return result;
}

// ============================================
// SHAPE 19: Borromean Rings
// ============================================
float BorromeanRingsSDF(float2 p, float anim)
{
    float result = 1e10;
    float r = 0.35;
    float separation = 0.15;
    float pulse = 1.0 + sin(anim * 0.4) * 0.03;

    // Three interlocking circles
    float2 centers[3];
    for (int i = 0; i < 3; i++)
    {
        float angle = float(i) * TAU / 3.0 + anim * 0.1;
        centers[i] = float2(cos(angle), sin(angle)) * separation * pulse;
    }

    // Draw rings
    for (int j = 0; j < 3; j++)
    {
        float ring = abs(CircleSDF(p - centers[j], r * pulse)) - LineThickness;
        result = min(result, ring);
    }

    return result;
}

// ============================================
// Main shape dispatcher
// ============================================
float GetShapeSDF(float2 p, int shapeIndex, float anim)
{
    switch (shapeIndex)
    {
        case 0: return VesicaPiscisSDF(p, anim);
        case 1: return SeedOfLifeSDF(p, anim);
        case 2: return FlowerOfLifeSDF(p, anim);
        case 3: return FruitOfLifeSDF(p, anim);
        case 4: return EggOfLifeSDF(p, anim);
        case 5: return MetatronsCubeSDF(p, anim);
        case 6: return TreeOfLifeSDF(p, anim);
        case 7: return SriYantraSDF(p, anim);
        case 8: return MerkabaSDF(p, anim);
        case 9: return HexagramSDF(p, anim);
        case 10: return PentagramSDF(p, anim);
        case 11: return GoldenSpiralSDF(p, anim);
        case 12: return TetrahedronSDF(p, anim);
        case 13: return CubeSDF(p, anim);
        case 14: return OctahedronSDF(p, anim);
        case 15: return DodecahedronSDF(p, anim);
        case 16: return IcosahedronSDF(p, anim);
        case 17: return TorusSDF(p, anim);
        case 18: return TetrahedronGridSDF(p, anim);
        case 19: return BorromeanRingsSDF(p, anim);
        default: return CircleSDF(p, 0.5);
    }
}

// ============================================
// Morphed Shape SDF - blends between two shapes
// ============================================
float GetMorphedShapeSDF(float2 p, int currentShape, int targetShape, float morphPhase, float anim)
{
    // Get SDF for both shapes
    float sdf1 = GetShapeSDF(p, currentShape, anim);
    float sdf2 = GetShapeSDF(p, targetShape, anim);

    // Smooth blend between shapes using smoothstep for easing
    float t = smoothstep(0.0, 1.0, morphPhase);

    // Linear interpolation of SDFs creates smooth morph effect
    return lerp(sdf1, sdf2, t);
}

// ============================================
// Vertex Shader
// ============================================
VSOutput VSMain(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    ShapeInstance shape = Shapes[instanceId];

    VSOutput output;

    // Skip inactive shapes
    if (shape.Lifetime <= 0 || instanceId >= (uint)ActiveShapeCount)
    {
        output.Position = float4(0, 0, -2, 1);
        output.TexCoord = float2(0, 0);
        output.LocalPos = float2(0, 0);
        output.Color = float4(0, 0, 0, 0);
        output.Rotation = 0;
        output.ShapeIndex = 0;
        output.LifeFactor = 0;
        output.AnimPhase = 0;
        output.AppearFactor = 0;
        output.MorphPhase = 0;
        output.MorphTargetShape = 0;
        return output;
    }

    // Quad vertices (2 triangles)
    float2 quadVerts[6] = {
        float2(-1, -1), float2(1, -1), float2(-1, 1),
        float2(-1, 1), float2(1, -1), float2(1, 1)
    };
    float2 texCoords[6] = {
        float2(0, 1), float2(1, 1), float2(0, 0),
        float2(0, 0), float2(1, 1), float2(1, 0)
    };

    float2 vertex = quadVerts[vertexId];

    // Calculate appearance factor based on mode
    float lifeFactor = shape.Lifetime / max(shape.MaxLifetime, 0.001);
    float appearFactor = 1.0;

    // Fade in/out
    float fadeIn = saturate(lifeFactor * 5.0); // First 20% of life
    float fadeOut = saturate((1.0 - lifeFactor) * 5.0); // Last 20% of life... wait that's wrong
    // Actually: fadeOut should be high when lifeFactor is high (beginning) and low when dying
    fadeOut = lifeFactor; // Simpler: just use life factor for fade out

    if (shape.AppearMode == 0) // Fade
    {
        appearFactor = min(fadeIn, fadeOut);
    }
    else if (shape.AppearMode == 1) // Scale
    {
        float scaleIn = saturate(lifeFactor * 3.0);
        appearFactor = scaleIn * lifeFactor;
    }
    else // Both
    {
        float scaleIn = saturate(lifeFactor * 3.0);
        appearFactor = min(fadeIn, fadeOut) * scaleIn;
    }

    // Apply scale from appearance
    float scale = shape.Radius;
    if (shape.AppearMode >= 1)
    {
        scale *= saturate(lifeFactor * 3.0);
    }

    // Make quad larger to accommodate glow effect (1.5x padding)
    float glowPadding = 1.5;
    float quadScale = scale * glowPadding;

    // Transform vertex
    float2 worldPos = shape.Position + vertex * quadScale;

    // Convert to NDC
    float2 ndc = (worldPos / ViewportSize) * 2.0 - 1.0;
    ndc.y = -ndc.y;

    output.Position = float4(ndc, 0, 1);
    output.TexCoord = texCoords[vertexId];
    output.LocalPos = vertex * glowPadding; // Scaled to match larger quad
    output.Color = shape.Color;
    output.Rotation = shape.Rotation;
    output.ShapeIndex = shape.ShapeIndex;
    output.LifeFactor = lifeFactor;
    output.AnimPhase = shape.AnimPhase;
    output.AppearFactor = appearFactor;
    output.MorphPhase = shape.MorphPhase;
    output.MorphTargetShape = shape.MorphTargetShape;

    return output;
}

// ============================================
// HSV to RGB conversion
// ============================================
float3 HSVtoRGB(float3 hsv)
{
    float h = hsv.x * 6.0;
    float s = hsv.y;
    float v = hsv.z;

    float c = v * s;
    float x = c * (1.0 - abs(fmod(h, 2.0) - 1.0));
    float m = v - c;

    float3 rgb;
    if (h < 1.0) rgb = float3(c, x, 0);
    else if (h < 2.0) rgb = float3(x, c, 0);
    else if (h < 3.0) rgb = float3(0, c, x);
    else if (h < 4.0) rgb = float3(0, x, c);
    else if (h < 5.0) rgb = float3(x, 0, c);
    else rgb = float3(c, 0, x);

    return rgb + m;
}

// ============================================
// Pixel Shader
// ============================================
float4 PSMain(VSOutput input) : SV_TARGET
{
    if (input.LifeFactor <= 0)
        discard;

    // Get local position for SDF (-1 to 1)
    float2 localPos = input.LocalPos;

    // Apply rotation
    localPos = mul(Rotate2D(input.Rotation), localPos);

    // Get shape distance (with optional morphing)
    float anim = input.AnimPhase + Time * AnimationSpeed;
    float dist;

    if (MorphEnabled && MorphBetweenShapes && input.MorphPhase > 0.0)
    {
        // Morph between current shape and target shape
        dist = GetMorphedShapeSDF(localPos, input.ShapeIndex, input.MorphTargetShape,
                                   input.MorphPhase * MorphIntensity, anim);
    }
    else
    {
        dist = GetShapeSDF(localPos, input.ShapeIndex, anim);
    }

    // Create shape mask with anti-aliasing
    float shape = 1.0 - smoothstep(-0.02, 0.02, dist);

    // Multi-layer glow effect (works outside the shape too)
    float glowFalloff = max(abs(dist), 0.001);
    float glow1 = exp(-glowFalloff * 15.0) * GlowIntensity * 0.8;
    float glow2 = exp(-glowFalloff * 8.0) * GlowIntensity * 0.4;
    float glow3 = exp(-glowFalloff * 4.0) * GlowIntensity * 0.2;
    float totalGlow = glow1 + glow2 + glow3;

    // Early discard if nothing visible
    if (shape < 0.01 && totalGlow < 0.01)
        discard;

    // Calculate color
    float3 baseColor = input.Color.rgb;

    // Rainbow mode - cycle through hues
    if (RainbowEnabled)
    {
        float hueOffset = IndependentRainbow ? input.AnimPhase : 0.0;
        float hue = frac(Time * RainbowSpeed + hueOffset);
        baseColor = HSVtoRGB(float3(hue, 0.85, 1.0));
    }

    // Twinkle effect
    float twinkle = sin(Time * 5.0 + localPos.x * 10.0 + localPos.y * 10.0) * 0.5 + 0.5;
    twinkle = 1.0 + twinkle * TwinkleIntensity * 0.5;

    // Combine shape and glow
    // Shape gets full color, glow falls off
    float3 shapeColor = baseColor * shape;
    float3 glowColor = baseColor * totalGlow;
    float3 finalColor = (shapeColor + glowColor) * twinkle;

    // HDR boost - more subtle, only boost the glow
    float hdrBoost = 1.0 + totalGlow * (HdrMultiplier - 1.0) * 0.5;
    finalColor *= hdrBoost;

    // Apply appearance factor for fade
    float alpha = saturate(shape + totalGlow * 0.7) * input.AppearFactor * input.LifeFactor;

    return float4(finalColor, alpha);
}

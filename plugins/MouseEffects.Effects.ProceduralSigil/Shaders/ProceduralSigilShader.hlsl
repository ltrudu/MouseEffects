// ProceduralSigilShader.hlsl - Magical Sigil / Arcane Portal Renderer
// Procedural SDF-based rendering with multiple configurable layers

static const float PI = 3.14159265359;
static const float TAU = 6.28318530718;
static const float SQRT3 = 1.73205080757;

// Layer flag constants
static const uint LAYER_CENTER = 1;
static const uint LAYER_INNER = 2;
static const uint LAYER_MIDDLE = 4;
static const uint LAYER_RUNES = 8;
static const uint LAYER_GLOW = 16;

// Animation flag constants
static const uint ANIM_ROTATE = 1;
static const uint ANIM_PULSE = 2;
static const uint ANIM_MORPH = 4;

// Sigil style constants
static const uint STYLE_ARCANE_CIRCLE = 0;
static const uint STYLE_TRIANGLE_MANDALA = 1;
static const uint STYLE_MOON = 2;

cbuffer Constants : register(b0)
{
    float2 ViewportSize;
    float2 SigilCenter;

    float Time;
    float SigilRadius;
    float LineThickness;
    float GlowIntensity;

    float4 CoreColor;
    float4 MidColor;
    float4 EdgeColor;

    uint LayerFlags;
    uint AnimationFlags;
    float RotationSpeed;
    float PulseSpeed;

    float PulseAmplitude;
    float MorphAmount;
    float FadeAlpha;
    float HdrMultiplier;

    float CounterRotateLayers;
    float RuneScrollSpeed;
    float InnerRotationMult;
    float MiddleRotationMult;

    // Style and Triangle Mandala parameters
    uint SigilStyle;
    int TriangleLayers;
    float ZoomSpeed;
    float ZoomAmount;

    int InnerTriangles;
    float FractalDepth;
    float MoonPhaseRotationSpeed;
    float ZodiacRotationSpeed;

    float MoonPhaseOffset;
    float TreeOfLifeScale;
    float StarfieldDensity;
    float CosmicGlowIntensity;

    // Energy particle parameters
    float ParticleIntensity;
    float ParticleSpeed;
    uint ParticleType;
    float ParticleEntropy;

    float ParticleSize;
    float FireRiseHeight;
    float ElectricitySpread;
    float SigilAlpha;
};

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

// ============================================
// Reusable SDF Primitives Library
// ============================================

// Basic circle SDF
float sdCircle(float2 p, float r)
{
    return length(p) - r;
}

// Ring (circle outline) SDF
float sdRing(float2 p, float r, float thickness)
{
    return abs(length(p) - r) - thickness;
}

// Line segment SDF
float sdLine(float2 p, float2 a, float2 b, float thickness)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h) - thickness;
}

// Arc SDF (portion of a ring)
float sdArc(float2 p, float r, float startAngle, float sweepAngle, float thickness)
{
    // Rotate point to align with arc start
    float c = cos(-startAngle);
    float s = sin(-startAngle);
    float2 rotP = float2(p.x * c - p.y * s, p.x * s + p.y * c);

    // Get angle of rotated point
    float angle = atan2(rotP.y, rotP.x);
    if (angle < 0.0) angle += TAU;

    // Check if within sweep angle
    if (angle <= sweepAngle)
    {
        return abs(length(p) - r) - thickness;
    }

    // Distance to endpoints
    float2 p1 = float2(cos(startAngle), sin(startAngle)) * r;
    float2 p2 = float2(cos(startAngle + sweepAngle), sin(startAngle + sweepAngle)) * r;
    return min(length(p - p1), length(p - p2)) - thickness;
}

// Regular polygon SDF
float sdPolygon(float2 p, float r, int sides)
{
    float angle = atan2(p.y, p.x);
    float segmentAngle = TAU / float(sides);
    float halfSegment = segmentAngle * 0.5;

    // Find which segment we're in
    float segmentIndex = floor((angle + halfSegment) / segmentAngle);
    float localAngle = angle - segmentIndex * segmentAngle;

    // Distance to edge
    float edgeDist = length(p) * cos(localAngle) - r * cos(halfSegment);
    return edgeDist;
}

// Star SDF
float sdStar(float2 p, float outerR, float innerR, int points)
{
    float angle = atan2(p.y, p.x);
    float segmentAngle = PI / float(points);

    // Wrap angle
    float localAngle = abs(fmod(angle + segmentAngle * 0.5, segmentAngle * 2.0) - segmentAngle);

    // Interpolate between inner and outer radius
    float t = localAngle / segmentAngle;
    float targetR = lerp(outerR, innerR, t);

    return length(p) - targetR;
}

// Hexagon SDF
float sdHexagon(float2 p, float r)
{
    p = abs(p);
    float2 q = float2(p.x * SQRT3 * 0.5 + p.y * 0.5, p.y);
    return max(q.x, q.y) - r;
}

// Triangle SDF
float sdTriangle(float2 p, float r)
{
    float2 q = float2(abs(p.x) - r, p.y + r / SQRT3);
    float a = q.x;
    float b = p.y * 0.5 + r / (2.0 * SQRT3);
    float c = -q.y + r / SQRT3;
    return max(max(a, b), c) - r * 0.5;
}

// ============================================
// SDF Operations
// ============================================

float opUnion(float d1, float d2)
{
    return min(d1, d2);
}

float opSubtract(float d1, float d2)
{
    return max(d1, -d2);
}

float opIntersect(float d1, float d2)
{
    return max(d1, d2);
}

float opSmoothUnion(float d1, float d2, float k)
{
    float h = clamp(0.5 + 0.5 * (d2 - d1) / k, 0.0, 1.0);
    return lerp(d2, d1, h) - k * h * (1.0 - h);
}

float2 opRotate(float2 p, float angle)
{
    float c = cos(angle);
    float s = sin(angle);
    return float2(p.x * c - p.y * s, p.x * s + p.y * c);
}

float2 opRepeatPolar(float2 p, int count)
{
    float angle = atan2(p.y, p.x);
    float segmentAngle = TAU / float(count);
    angle = fmod(angle + segmentAngle * 0.5, segmentAngle) - segmentAngle * 0.5;
    return float2(cos(angle), sin(angle)) * length(p);
}

// ============================================
// Energy Particle Functions
// ============================================

// Hash function for pseudo-random numbers
float hash(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

// Flicker function for electricity effect
float flicker(float t, float speed)
{
    float f1 = sin(t * speed) * 0.5 + 0.5;
    float f2 = sin(t * speed * 2.7) * 0.5 + 0.5;
    float f3 = sin(t * speed * 4.3) * 0.5 + 0.5;
    return (f1 * 0.5 + f2 * 0.3 + f3 * 0.2) * 0.6 + 0.4;
}

// Compute crackling energy particles along sigil geometry
// Fire particles rise upward like embers, electricity crackles in place
float3 ComputeEnergyParticles(float2 p, float sdfDist, float radius, float time,
                               float3 baseColor, float intensity, float speed, uint particleType,
                               float entropy, float pSize, float fireHeight, float elecSpread)
{
    if (particleType == 0) // None - no particles
    {
        return float3(0, 0, 0);
    }

    // Determine if this is a fire-type particle (rises) or electricity-type (stays)
    bool isFireType = (particleType == 1);
    bool isMixed = (particleType == 3);

    // For mixed mode, use position hash to determine particle behavior
    float mixHash = hash(floor(p / (LineThickness * 5.0)));
    bool behavesAsFire = isMixed ? (mixHash > 0.5) : isFireType;

    float3 result = float3(0, 0, 0);

    if (behavesAsFire)
    {
        // === FIRE PARTICLES: Rise upward like embers ===
        float riseSpeed = speed * 50.0 * (1.0 + entropy * 0.5) * fireHeight;
        float cellSize = LineThickness * 3.0 * pSize;

        // Create rising particle streams
        float2 fireP = p;
        // Offset Y by time to create rising effect (negative Y = upward in screen space)
        fireP.y += time * riseSpeed;

        // Add horizontal wobble as particles rise - more dramatic with higher size
        float wobbleAmount = LineThickness * 2.0 * pSize * (1.0 + entropy);
        float wobble = sin(time * speed * 4.0 + p.x * 0.05 + hash(floor(p.x / cellSize)) * 6.28) * wobbleAmount;
        fireP.x += wobble * entropy;

        float2 cellId = floor(fireP / cellSize);
        float cellHash = hash(cellId);

        // Particle spawn based on proximity to sigil edge - spread affects spawn area
        float spawnRange = LineThickness * 3.0 * pSize;
        float edgeProximity = 1.0 - smoothstep(0.0, spawnRange, abs(sdfDist));

        // Calculate particle lifetime based on how far it has risen
        // fireHeight controls how long particles live/how high they go
        float risePhase = frac(time * speed * (0.5 / fireHeight) + cellHash);
        float maxRiseHeight = radius * fireHeight; // How high particle can rise

        // Particles spawn at edge and fade as they rise
        float spawnProb = step(0.6 - entropy * 0.2, cellHash);
        float lifetimeFade = 1.0 - smoothstep(0.0, 1.0, risePhase);
        lifetimeFade *= lifetimeFade; // Quadratic falloff

        // Check if this pixel is near a rising ember
        float2 cellCenter = (cellId + 0.5) * cellSize;
        // Add some random offset per particle
        cellCenter.x += (hash(cellId + 0.5) - 0.5) * cellSize * 0.8;

        float particleDist = length(fireP - cellCenter);

        // Particle size - affected by pSize parameter, shrinks as it rises
        float baseParticleSize = LineThickness * pSize * 1.5;
        float currentSize = baseParticleSize * lerp(1.0, 0.3, risePhase) * (1.0 + entropy * 0.3);
        float glow = exp(-particleDist * particleDist / (currentSize * currentSize * 2.0));

        // Flicker
        float flick = flicker(time * speed + cellHash * 20.0, 15.0 + entropy * 10.0);

        // Fire color - hotter (brighter/yellower) at spawn, cooler (darker/redder) as it rises
        float heat = 1.0 - risePhase * 0.7;
        heat += hash(cellId + time * 0.1) * 0.3;
        float3 fireColor = lerp(float3(0.8, 0.1, 0.0), float3(1.0, 0.9, 0.4), saturate(heat));
        // Add white-hot sparks occasionally - more with larger size
        fireColor = lerp(fireColor, float3(1.0, 1.0, 0.9), step(0.92 - pSize * 0.05, cellHash) * heat);

        float fireIntensity = glow * spawnProb * lifetimeFade * edgeProximity * flick;
        result = fireColor * fireIntensity * intensity * 2.5 * pSize;
    }
    else
    {
        // === ELECTRICITY PARTICLES: Crackle in place near edges ===
        // elecSpread controls how far from edge electricity can appear
        float spreadRange = LineThickness * 4.0 * elecSpread;
        float edgeProximity = 1.0 - smoothstep(0.0, spreadRange, abs(sdfDist));
        if (edgeProximity < 0.01) return result;

        float cellSize = LineThickness * lerp(4.0, 2.0, entropy) * pSize;

        // Swirling drift for electricity - affected by spread
        float driftAmount = entropy * LineThickness * 2.0 * elecSpread;
        float2 drift = float2(
            sin(time * speed * 3.0 + p.y * 0.02) * driftAmount,
            cos(time * speed * 2.7 + p.x * 0.02) * driftAmount
        );
        float2 driftedP = p + drift;

        float timeShift = time * speed * entropy * 2.0;
        float2 cellP = driftedP / cellSize;
        float2 cellId = floor(cellP + float2(sin(timeShift), cos(timeShift * 0.7)) * entropy);

        float cellHash = hash(cellId + floor(time * speed * (0.5 + entropy)));

        // More particles with higher spread
        float spawnThreshold = lerp(0.75, 0.5, entropy) - (elecSpread - 1.0) * 0.1;
        spawnThreshold = max(0.3, spawnThreshold);
        float flickerSpeedVal = lerp(10.0, 25.0, entropy);

        float flickerPhase = time * speed + cellHash * 10.0 + sin(time * speed * 5.0 * entropy) * entropy;
        float visible = step(spawnThreshold, cellHash) * flicker(flickerPhase, flickerSpeedVal);

        // Random bright bursts for electricity - more frequent with higher spread
        float burstThreshold = 0.88 - (elecSpread - 1.0) * 0.1;
        if (entropy > 0.3)
        {
            float burstHash = hash(cellId + floor(time * speed * 5.0));
            float burst = step(burstThreshold, burstHash) * (sin(time * speed * 30.0) * 0.5 + 0.5);
            visible = max(visible, burst * entropy);
        }

        float2 cellCenter = (cellId + 0.5) * cellSize;
        float2 jitter = float2(hash(cellId + time), hash(cellId.yx + time)) - 0.5;
        cellCenter += jitter * entropy * cellSize * 0.5 * elecSpread;

        float particleDist = length(driftedP - cellCenter);
        // Particle size affected by pSize parameter
        float glowSize = LineThickness * pSize * lerp(1.0, 1.5, sin(time * speed * 8.0 + cellHash * 6.28) * 0.5 + 0.5);
        float glow = exp(-particleDist * particleDist / (glowSize * glowSize * 2.0));

        // Electricity color - blue/white with bright arcs
        float arcIntensity = hash(cellId + float2(time * 3.0, 0));
        float3 elecColor = lerp(baseColor, float3(0.7, 0.85, 1.0), 0.5);
        elecColor = lerp(elecColor, float3(0.95, 0.98, 1.0), step(0.8, arcIntensity));

        float intensityMult = lerp(1.0, 1.5, entropy) * pSize;
        result = elecColor * glow * visible * edgeProximity * intensity * intensityMult * 2.0;
    }

    return result;
}

// ============================================
// Composite Sigil Elements
// ============================================

// Multiple concentric rings with varying thickness
float sdConcentricRings(float2 p, float baseR, int count, float spacing, float thickness)
{
    float d = 1e10;
    [unroll]
    for (int i = 0; i < 5; i++)
    {
        if (i >= count) break;
        float r = baseR - float(i) * spacing;
        if (r > 0.0)
        {
            // Alternate thickness for visual variety
            float t = thickness * (1.0 - float(i) * 0.15);
            d = min(d, sdRing(p, r, t));
        }
    }
    return d;
}

// Radial lines emanating from center
float sdRadialLines(float2 p, float innerR, float outerR, int count, float thickness)
{
    float d = 1e10;
    float angleStep = TAU / float(count);

    [unroll]
    for (int i = 0; i < 24; i++)
    {
        if (i >= count) break;
        float angle = float(i) * angleStep;
        float2 dir = float2(cos(angle), sin(angle));
        float2 a = dir * innerR;
        float2 b = dir * outerR;
        d = min(d, sdLine(p, a, b, thickness));
    }
    return d;
}

// Triangular lattice pattern
float sdTriangularLattice(float2 p, float r, int subdivisions, float thickness)
{
    float d = 1e10;
    float step = r / float(subdivisions);

    // Draw triangular grid lines
    [unroll]
    for (int i = 0; i <= 4; i++)
    {
        if (i > subdivisions) break;
        float offset = float(i) * step;

        // Horizontal-ish lines (3 directions for triangular grid)
        for (int dir = 0; dir < 3; dir++)
        {
            float angle = float(dir) * PI / 3.0;
            float2 lineDir = float2(cos(angle), sin(angle));
            float2 perpDir = float2(-lineDir.y, lineDir.x);

            // Multiple parallel lines
            for (int j = -3; j <= 3; j++)
            {
                float2 lineStart = perpDir * float(j) * step - lineDir * r;
                float2 lineEnd = perpDir * float(j) * step + lineDir * r;

                // Clip to circle
                float dist = length(lineStart + (lineEnd - lineStart) * 0.5);
                if (dist < r * 1.1)
                {
                    d = min(d, sdLine(p, lineStart, lineEnd, thickness * 0.5));
                }
            }
        }
    }

    // Clip to circular boundary
    float circleMask = sdCircle(p, r);
    if (circleMask > 0.0) d = 1e10;

    return d;
}

// Procedural rune-like glyphs around a ring
float sdRuneRing(float2 p, float r, float glyphSize, int glyphCount, float scrollOffset, float thickness)
{
    float d = 1e10;
    float angleStep = TAU / float(glyphCount);

    [unroll]
    for (int i = 0; i < 16; i++)
    {
        if (i >= glyphCount) break;
        float baseAngle = float(i) * angleStep + scrollOffset;
        float2 center = float2(cos(baseAngle), sin(baseAngle)) * r;
        float2 localP = p - center;

        // Rotate glyph to face outward
        localP = opRotate(localP, -baseAngle - PI * 0.5);

        // Simple procedural glyph based on index
        float glyphD = 1e10;
        int glyphType = i % 6;

        if (glyphType == 0)
        {
            // Vertical line with horizontal bars
            glyphD = sdLine(localP, float2(0, -glyphSize), float2(0, glyphSize), thickness);
            glyphD = min(glyphD, sdLine(localP, float2(-glyphSize * 0.5, 0), float2(glyphSize * 0.5, 0), thickness));
        }
        else if (glyphType == 1)
        {
            // Triangle glyph
            float2 a = float2(0, glyphSize);
            float2 b = float2(-glyphSize * 0.7, -glyphSize * 0.5);
            float2 c = float2(glyphSize * 0.7, -glyphSize * 0.5);
            glyphD = sdLine(localP, a, b, thickness);
            glyphD = min(glyphD, sdLine(localP, b, c, thickness));
            glyphD = min(glyphD, sdLine(localP, c, a, thickness));
        }
        else if (glyphType == 2)
        {
            // Circle with dot
            glyphD = sdRing(localP, glyphSize * 0.7, thickness);
            glyphD = min(glyphD, sdCircle(localP, thickness * 2.0));
        }
        else if (glyphType == 3)
        {
            // X shape
            glyphD = sdLine(localP, float2(-glyphSize, -glyphSize), float2(glyphSize, glyphSize), thickness);
            glyphD = min(glyphD, sdLine(localP, float2(-glyphSize, glyphSize), float2(glyphSize, -glyphSize), thickness));
        }
        else if (glyphType == 4)
        {
            // Arrow pointing up
            glyphD = sdLine(localP, float2(0, -glyphSize), float2(0, glyphSize), thickness);
            glyphD = min(glyphD, sdLine(localP, float2(0, glyphSize), float2(-glyphSize * 0.5, glyphSize * 0.5), thickness));
            glyphD = min(glyphD, sdLine(localP, float2(0, glyphSize), float2(glyphSize * 0.5, glyphSize * 0.5), thickness));
        }
        else
        {
            // Curved arc glyph
            glyphD = sdArc(localP, glyphSize * 0.6, 0.0, PI, thickness);
            glyphD = min(glyphD, sdLine(localP, float2(-glyphSize * 0.6, 0), float2(-glyphSize * 0.6, -glyphSize * 0.5), thickness));
            glyphD = min(glyphD, sdLine(localP, float2(glyphSize * 0.6, 0), float2(glyphSize * 0.6, -glyphSize * 0.5), thickness));
        }

        d = min(d, glyphD);
    }

    return d;
}

// Center geometric flower pattern
float sdGeometricFlower(float2 p, float r, int petals, float morphPhase)
{
    float d = 1e10;
    float angleStep = TAU / float(petals);

    // Morphing between different center patterns
    float morph = sin(morphPhase * TAU) * 0.5 + 0.5;

    // Inner star
    float starR = r * lerp(0.3, 0.5, morph);
    float innerR = starR * lerp(0.3, 0.6, morph);
    d = min(d, abs(sdStar(p, starR, innerR, petals)) - LineThickness);

    // Center rings
    d = min(d, sdRing(p, r * 0.15, LineThickness));
    d = min(d, sdRing(p, r * 0.08, LineThickness * 0.7));

    // Petal arcs
    [unroll]
    for (int i = 0; i < 12; i++)
    {
        if (i >= petals) break;
        float angle = float(i) * angleStep;
        float2 petalCenter = float2(cos(angle), sin(angle)) * r * 0.5;
        float petalR = r * 0.35;
        d = min(d, sdRing(opRotate(p - petalCenter, -angle), petalR, LineThickness * 0.8));
    }

    return d;
}

// ============================================
// Moon Sigil Primitives
// ============================================

// Moon phase SDF - creates crescent or full moon shape
// phase: 0 = new moon, 0.5 = full moon, 1 = new moon (cycle)
float sdMoonPhase(float2 p, float r, float phase)
{
    // phase 0 = new moon (dark), 0.5 = full moon, 1 = new moon
    float fullness = sin(phase * TAU) * 0.5 + 0.5; // 0 to 1 to 0

    // Main circle
    float mainCircle = sdCircle(p, r);

    // Shadow circle offset
    float shadowOffset = r * 2.0 * (1.0 - fullness);
    float shadowDir = phase < 0.5 ? 1.0 : -1.0;
    float2 shadowP = p - float2(shadowOffset * shadowDir, 0.0);
    float shadowCircle = sdCircle(shadowP, r * 1.1);

    // For new moon (phase near 0 or 1), just show ring
    if (fullness < 0.1)
    {
        return sdRing(p, r, LineThickness * 0.5);
    }

    // Subtract shadow from main for crescent
    if (fullness < 0.95)
    {
        return opSubtract(mainCircle, shadowCircle);
    }

    // Full moon
    return mainCircle;
}

// Individual moon phase shapes for the ring - progressive from new to full and back
float sdMoonPhaseIcon(float2 p, float r, int phaseIndex)
{
    float d = 1e10;
    float thickness = LineThickness * 0.3;
    float innerR = r * 0.85;

    // phaseIndex 0-7: new, wax cres, first qtr, wax gib, full, wan gib, last qtr, wan cres
    // Outer ring always visible
    d = sdRing(p, r, thickness);

    if (phaseIndex == 0)
    {
        // New moon - just the outline ring (already drawn above)
        // Add a small dot in center to indicate new moon
        d = min(d, sdCircle(p, thickness * 1.5));
    }
    else if (phaseIndex == 1)
    {
        // Waxing crescent - small sliver on RIGHT side (light coming from right)
        float crescentWidth = innerR * 0.35;
        float outer = sdCircle(p, innerR);
        float shadowOffset = innerR * 0.7;
        float inner = sdCircle(p - float2(shadowOffset, 0), innerR);
        d = min(d, max(outer, -inner));
    }
    else if (phaseIndex == 2)
    {
        // First quarter - RIGHT half filled
        float halfMoon = max(sdCircle(p, innerR), -p.x);
        d = min(d, halfMoon);
    }
    else if (phaseIndex == 3)
    {
        // Waxing gibbous - mostly filled, small shadow on LEFT
        float outer = sdCircle(p, innerR);
        float shadowOffset = innerR * 0.7;
        float inner = sdCircle(p + float2(shadowOffset, 0), innerR);
        d = min(d, max(outer, -inner));
    }
    else if (phaseIndex == 4)
    {
        // Full moon - completely filled circle
        d = min(d, sdCircle(p, innerR));
    }
    else if (phaseIndex == 5)
    {
        // Waning gibbous - mostly filled, small shadow on RIGHT
        float outer = sdCircle(p, innerR);
        float shadowOffset = innerR * 0.7;
        float inner = sdCircle(p - float2(shadowOffset, 0), innerR);
        d = min(d, max(outer, -inner));
    }
    else if (phaseIndex == 6)
    {
        // Last quarter - LEFT half filled
        float halfMoon = max(sdCircle(p, innerR), p.x);
        d = min(d, halfMoon);
    }
    else
    {
        // Waning crescent - small sliver on LEFT side
        float outer = sdCircle(p, innerR);
        float shadowOffset = innerR * 0.7;
        float inner = sdCircle(p + float2(shadowOffset, 0), innerR);
        d = min(d, max(outer, -inner));
    }

    return d;
}

// Zodiac symbol SDFs - simplified geometric representations
float sdZodiacAries(float2 p, float size)
{
    float d = 1e10;
    float t = LineThickness * 0.4;
    // Ram horns - two curved arcs
    d = min(d, sdArc(p + float2(size * 0.25, 0), size * 0.35, PI * 0.3, PI * 0.8, t));
    d = min(d, sdArc(p - float2(size * 0.25, 0), size * 0.35, PI * 1.9, PI * 0.8, t));
    d = min(d, sdLine(p, float2(0, size * 0.1), float2(0, -size * 0.4), t));
    return d;
}

float sdZodiacTaurus(float2 p, float size)
{
    float d = 1e10;
    float t = LineThickness * 0.4;
    // Bull head - circle with horns
    d = min(d, sdRing(p - float2(0, -size * 0.2), size * 0.35, t));
    d = min(d, sdArc(p + float2(size * 0.35, size * 0.15), size * 0.25, PI * 0.5, PI * 0.7, t));
    d = min(d, sdArc(p + float2(-size * 0.35, size * 0.15), size * 0.25, PI * 1.8, PI * 0.7, t));
    return d;
}

float sdZodiacGemini(float2 p, float size)
{
    float d = 1e10;
    float t = LineThickness * 0.4;
    // Two parallel lines with bars
    d = min(d, sdLine(p, float2(-size * 0.25, -size * 0.4), float2(-size * 0.25, size * 0.4), t));
    d = min(d, sdLine(p, float2(size * 0.25, -size * 0.4), float2(size * 0.25, size * 0.4), t));
    d = min(d, sdLine(p, float2(-size * 0.35, size * 0.35), float2(size * 0.35, size * 0.35), t));
    d = min(d, sdLine(p, float2(-size * 0.35, -size * 0.35), float2(size * 0.35, -size * 0.35), t));
    return d;
}

float sdZodiacCancer(float2 p, float size)
{
    float d = 1e10;
    float t = LineThickness * 0.4;
    // Crab claws - two 69-like curves
    d = min(d, sdArc(p + float2(0, size * 0.15), size * 0.25, PI * 0.0, PI * 1.2, t));
    d = min(d, sdArc(p - float2(0, size * 0.15), size * 0.25, PI * 1.0, PI * 1.2, t));
    d = min(d, sdCircle(p + float2(size * 0.25, size * 0.15), t * 2.0));
    d = min(d, sdCircle(p - float2(size * 0.25, size * 0.15), t * 2.0));
    return d;
}

float sdZodiacLeo(float2 p, float size)
{
    float d = 1e10;
    float t = LineThickness * 0.4;
    // Lion's mane - curved arc with loop
    d = min(d, sdArc(p + float2(0, size * 0.1), size * 0.35, PI * 0.3, PI * 1.4, t));
    d = min(d, sdRing(p + float2(size * 0.25, -size * 0.2), size * 0.15, t));
    return d;
}

float sdZodiacVirgo(float2 p, float size)
{
    float d = 1e10;
    float t = LineThickness * 0.4;
    // M with tail
    d = min(d, sdLine(p, float2(-size * 0.35, size * 0.35), float2(-size * 0.35, -size * 0.2), t));
    d = min(d, sdLine(p, float2(-size * 0.35, -size * 0.2), float2(-size * 0.1, size * 0.1), t));
    d = min(d, sdLine(p, float2(-size * 0.1, size * 0.1), float2(-size * 0.1, -size * 0.2), t));
    d = min(d, sdLine(p, float2(-size * 0.1, -size * 0.2), float2(size * 0.15, size * 0.1), t));
    d = min(d, sdLine(p, float2(size * 0.15, size * 0.1), float2(size * 0.15, -size * 0.35), t));
    d = min(d, sdArc(p + float2(size * 0.25, -size * 0.25), size * 0.15, PI * 1.5, PI * 1.0, t));
    return d;
}

float sdZodiacLibra(float2 p, float size)
{
    float d = 1e10;
    float t = LineThickness * 0.4;
    // Scales - horizontal line with curved top
    d = min(d, sdLine(p, float2(-size * 0.4, -size * 0.25), float2(size * 0.4, -size * 0.25), t));
    d = min(d, sdArc(p + float2(0, size * 0.2), size * 0.35, PI * 1.0, PI * 1.0, t));
    d = min(d, sdLine(p, float2(-size * 0.4, -size * 0.1), float2(size * 0.4, -size * 0.1), t));
    return d;
}

float sdZodiacScorpio(float2 p, float size)
{
    float d = 1e10;
    float t = LineThickness * 0.4;
    // M with arrow tail
    d = min(d, sdLine(p, float2(-size * 0.35, size * 0.35), float2(-size * 0.35, -size * 0.2), t));
    d = min(d, sdLine(p, float2(-size * 0.35, -size * 0.2), float2(-size * 0.1, size * 0.1), t));
    d = min(d, sdLine(p, float2(-size * 0.1, size * 0.1), float2(-size * 0.1, -size * 0.2), t));
    d = min(d, sdLine(p, float2(-size * 0.1, -size * 0.2), float2(size * 0.15, size * 0.1), t));
    d = min(d, sdLine(p, float2(size * 0.15, size * 0.1), float2(size * 0.15, -size * 0.25), t));
    // Arrow
    d = min(d, sdLine(p, float2(size * 0.15, -size * 0.25), float2(size * 0.35, -size * 0.35), t));
    d = min(d, sdLine(p, float2(size * 0.25, -size * 0.2), float2(size * 0.35, -size * 0.35), t));
    return d;
}

float sdZodiacSagittarius(float2 p, float size)
{
    float d = 1e10;
    float t = LineThickness * 0.4;
    // Arrow pointing up-right
    d = min(d, sdLine(p, float2(-size * 0.35, size * 0.35), float2(size * 0.35, -size * 0.35), t));
    d = min(d, sdLine(p, float2(size * 0.35, -size * 0.35), float2(size * 0.1, -size * 0.35), t));
    d = min(d, sdLine(p, float2(size * 0.35, -size * 0.35), float2(size * 0.35, -size * 0.1), t));
    d = min(d, sdLine(p, float2(-size * 0.1, size * 0.0), float2(size * 0.15, size * 0.0), t));
    return d;
}

float sdZodiacCapricorn(float2 p, float size)
{
    float d = 1e10;
    float t = LineThickness * 0.4;
    // Sea goat - V with curved tail
    d = min(d, sdLine(p, float2(-size * 0.25, size * 0.35), float2(-size * 0.1, -size * 0.1), t));
    d = min(d, sdLine(p, float2(-size * 0.1, -size * 0.1), float2(size * 0.1, size * 0.2), t));
    d = min(d, sdArc(p + float2(size * 0.2, -size * 0.1), size * 0.2, PI * 0.5, PI * 1.5, t));
    return d;
}

float sdZodiacAquarius(float2 p, float size)
{
    float d = 1e10;
    float t = LineThickness * 0.4;
    // Two wavy lines
    float wave1 = sin(p.x * 8.0 / size) * size * 0.08;
    float wave2 = sin(p.x * 8.0 / size) * size * 0.08;
    float y1 = p.y - size * 0.12 - wave1;
    float y2 = p.y + size * 0.12 - wave2;

    if (abs(p.x) < size * 0.4)
    {
        d = min(d, abs(y1) - t);
        d = min(d, abs(y2) - t);
    }
    return d;
}

float sdZodiacPisces(float2 p, float size)
{
    float d = 1e10;
    float t = LineThickness * 0.4;
    // Two fish facing opposite ways - curved parentheses with line
    d = min(d, sdArc(p + float2(size * 0.3, 0), size * 0.3, PI * 0.5, PI * 1.0, t));
    d = min(d, sdArc(p - float2(size * 0.3, 0), size * 0.3, PI * 1.5, PI * 1.0, t));
    d = min(d, sdLine(p, float2(-size * 0.4, 0), float2(size * 0.4, 0), t));
    return d;
}

// Get zodiac symbol by index (0-11)
float sdZodiacSymbol(float2 p, float size, int index)
{
    // Rotate symbol to face outward
    switch (index % 12)
    {
        case 0: return sdZodiacAries(p, size);
        case 1: return sdZodiacTaurus(p, size);
        case 2: return sdZodiacGemini(p, size);
        case 3: return sdZodiacCancer(p, size);
        case 4: return sdZodiacLeo(p, size);
        case 5: return sdZodiacVirgo(p, size);
        case 6: return sdZodiacLibra(p, size);
        case 7: return sdZodiacScorpio(p, size);
        case 8: return sdZodiacSagittarius(p, size);
        case 9: return sdZodiacCapricorn(p, size);
        case 10: return sdZodiacAquarius(p, size);
        default: return sdZodiacPisces(p, size);
    }
}

// Tree of Life (Kabbalah) - 10 sephiroth + paths
float sdTreeOfLife(float2 p, float size)
{
    float d = 1e10;
    float nodeR = size * 0.08;
    float t = LineThickness * 0.5;

    // Scale to fit
    float s = size * 0.9;

    // Sephiroth positions (normalized -1 to 1, then scaled)
    // Keter (Crown) - top
    float2 keter = float2(0, s * 0.45);
    // Chokmah (Wisdom) - top right
    float2 chokmah = float2(s * 0.3, s * 0.32);
    // Binah (Understanding) - top left
    float2 binah = float2(-s * 0.3, s * 0.32);
    // Chesed (Mercy) - right
    float2 chesed = float2(s * 0.3, s * 0.08);
    // Geburah (Severity) - left
    float2 geburah = float2(-s * 0.3, s * 0.08);
    // Tiphareth (Beauty) - center
    float2 tiphareth = float2(0, s * 0.0);
    // Netzach (Victory) - lower right
    float2 netzach = float2(s * 0.3, -s * 0.2);
    // Hod (Glory) - lower left
    float2 hod = float2(-s * 0.3, -s * 0.2);
    // Yesod (Foundation) - lower center
    float2 yesod = float2(0, -s * 0.32);
    // Malkuth (Kingdom) - bottom
    float2 malkuth = float2(0, -s * 0.45);

    // Draw the 22 paths (connections between sephiroth)
    // Vertical paths (3)
    d = min(d, sdLine(p, keter, tiphareth, t * 0.7));
    d = min(d, sdLine(p, tiphareth, yesod, t * 0.7));
    d = min(d, sdLine(p, yesod, malkuth, t * 0.7));

    // Left pillar
    d = min(d, sdLine(p, binah, geburah, t * 0.7));
    d = min(d, sdLine(p, geburah, hod, t * 0.7));

    // Right pillar
    d = min(d, sdLine(p, chokmah, chesed, t * 0.7));
    d = min(d, sdLine(p, chesed, netzach, t * 0.7));

    // Horizontal paths
    d = min(d, sdLine(p, chokmah, binah, t * 0.7));
    d = min(d, sdLine(p, chesed, geburah, t * 0.7));
    d = min(d, sdLine(p, netzach, hod, t * 0.7));

    // Diagonal paths
    d = min(d, sdLine(p, keter, chokmah, t * 0.7));
    d = min(d, sdLine(p, keter, binah, t * 0.7));
    d = min(d, sdLine(p, chokmah, tiphareth, t * 0.7));
    d = min(d, sdLine(p, binah, tiphareth, t * 0.7));
    d = min(d, sdLine(p, chesed, tiphareth, t * 0.7));
    d = min(d, sdLine(p, geburah, tiphareth, t * 0.7));
    d = min(d, sdLine(p, tiphareth, netzach, t * 0.7));
    d = min(d, sdLine(p, tiphareth, hod, t * 0.7));
    d = min(d, sdLine(p, netzach, yesod, t * 0.7));
    d = min(d, sdLine(p, hod, yesod, t * 0.7));
    d = min(d, sdLine(p, netzach, malkuth, t * 0.7));
    d = min(d, sdLine(p, hod, malkuth, t * 0.7));

    // Draw the 10 sephiroth (nodes) with rings
    d = min(d, sdRing(p - keter, nodeR, t));
    d = min(d, sdRing(p - chokmah, nodeR, t));
    d = min(d, sdRing(p - binah, nodeR, t));
    d = min(d, sdRing(p - chesed, nodeR, t));
    d = min(d, sdRing(p - geburah, nodeR, t));
    d = min(d, sdRing(p - tiphareth, nodeR * 1.2, t)); // Center node larger
    d = min(d, sdRing(p - netzach, nodeR, t));
    d = min(d, sdRing(p - hod, nodeR, t));
    d = min(d, sdRing(p - yesod, nodeR, t));
    d = min(d, sdRing(p - malkuth, nodeR, t));

    // Inner dots for each sephirah
    d = min(d, sdCircle(p - keter, t * 1.5));
    d = min(d, sdCircle(p - chokmah, t * 1.5));
    d = min(d, sdCircle(p - binah, t * 1.5));
    d = min(d, sdCircle(p - chesed, t * 1.5));
    d = min(d, sdCircle(p - geburah, t * 1.5));
    d = min(d, sdCircle(p - tiphareth, t * 2.0));
    d = min(d, sdCircle(p - netzach, t * 1.5));
    d = min(d, sdCircle(p - hod, t * 1.5));
    d = min(d, sdCircle(p - yesod, t * 1.5));
    d = min(d, sdCircle(p - malkuth, t * 1.5));

    return d;
}

// Runic letters for mystical text ring
float sdRuneLetter(float2 p, float size, int runeIndex)
{
    float d = 1e10;
    float t = LineThickness * 0.35;
    float s = size;

    // Elder Futhark-inspired runes
    int idx = runeIndex % 12;

    if (idx == 0) // Fehu - F shape
    {
        d = min(d, sdLine(p, float2(0, -s), float2(0, s), t));
        d = min(d, sdLine(p, float2(0, s * 0.5), float2(s * 0.5, s), t));
        d = min(d, sdLine(p, float2(0, 0), float2(s * 0.4, s * 0.3), t));
    }
    else if (idx == 1) // Uruz - upside down U
    {
        d = min(d, sdLine(p, float2(-s * 0.3, s), float2(-s * 0.3, -s * 0.5), t));
        d = min(d, sdLine(p, float2(-s * 0.3, -s * 0.5), float2(s * 0.3, -s), t));
        d = min(d, sdLine(p, float2(s * 0.3, -s), float2(s * 0.3, s), t));
    }
    else if (idx == 2) // Thurisaz - thorn
    {
        d = min(d, sdLine(p, float2(0, -s), float2(0, s), t));
        d = min(d, sdLine(p, float2(0, s * 0.4), float2(s * 0.4, 0), t));
        d = min(d, sdLine(p, float2(s * 0.4, 0), float2(0, -s * 0.4), t));
    }
    else if (idx == 3) // Ansuz - A-like
    {
        d = min(d, sdLine(p, float2(0, -s), float2(0, s), t));
        d = min(d, sdLine(p, float2(0, s), float2(s * 0.4, s * 0.4), t));
        d = min(d, sdLine(p, float2(0, s * 0.2), float2(s * 0.3, -s * 0.2), t));
    }
    else if (idx == 4) // Raido - R-like
    {
        d = min(d, sdLine(p, float2(-s * 0.2, -s), float2(-s * 0.2, s), t));
        d = min(d, sdLine(p, float2(-s * 0.2, s), float2(s * 0.3, s * 0.4), t));
        d = min(d, sdLine(p, float2(s * 0.3, s * 0.4), float2(-s * 0.2, 0), t));
        d = min(d, sdLine(p, float2(-s * 0.2, 0), float2(s * 0.3, -s), t));
    }
    else if (idx == 5) // Kenaz - < shape
    {
        d = min(d, sdLine(p, float2(-s * 0.3, s), float2(s * 0.3, 0), t));
        d = min(d, sdLine(p, float2(s * 0.3, 0), float2(-s * 0.3, -s), t));
    }
    else if (idx == 6) // Gebo - X gift
    {
        d = min(d, sdLine(p, float2(-s * 0.4, -s * 0.8), float2(s * 0.4, s * 0.8), t));
        d = min(d, sdLine(p, float2(-s * 0.4, s * 0.8), float2(s * 0.4, -s * 0.8), t));
    }
    else if (idx == 7) // Wunjo - P flag
    {
        d = min(d, sdLine(p, float2(0, -s), float2(0, s), t));
        d = min(d, sdLine(p, float2(0, s), float2(s * 0.4, s * 0.5), t));
        d = min(d, sdLine(p, float2(s * 0.4, s * 0.5), float2(0, 0), t));
    }
    else if (idx == 8) // Hagalaz - H shape
    {
        d = min(d, sdLine(p, float2(-s * 0.3, -s), float2(-s * 0.3, s), t));
        d = min(d, sdLine(p, float2(s * 0.3, -s), float2(s * 0.3, s), t));
        d = min(d, sdLine(p, float2(-s * 0.3, 0), float2(s * 0.3, 0), t));
    }
    else if (idx == 9) // Nauthiz - cross
    {
        d = min(d, sdLine(p, float2(0, -s), float2(0, s), t));
        d = min(d, sdLine(p, float2(-s * 0.3, s * 0.3), float2(s * 0.3, -s * 0.3), t));
    }
    else if (idx == 10) // Isa - vertical line
    {
        d = min(d, sdLine(p, float2(0, -s), float2(0, s), t));
    }
    else // Jera - interlocked angles
    {
        d = min(d, sdLine(p, float2(-s * 0.3, s * 0.5), float2(0, s), t));
        d = min(d, sdLine(p, float2(0, s), float2(0, 0), t));
        d = min(d, sdLine(p, float2(s * 0.3, -s * 0.5), float2(0, -s), t));
        d = min(d, sdLine(p, float2(0, -s), float2(0, 0), t));
    }

    return d;
}

// Moon phase ring - 8 phases arranged in a circle
float sdMoonPhasesRing(float2 p, float radius, float phaseSize, float rotationOffset)
{
    float d = 1e10;

    // Boundary rings
    d = min(d, sdRing(p, radius - phaseSize * 1.2, LineThickness * 0.5));
    d = min(d, sdRing(p, radius + phaseSize * 1.2, LineThickness * 0.5));

    // 8 moon phases
    [unroll]
    for (int i = 0; i < 8; i++)
    {
        float angle = float(i) * TAU / 8.0 + rotationOffset;
        float2 center = float2(cos(angle), sin(angle)) * radius;
        float2 localP = p - center;

        // Rotate to face outward
        localP = opRotate(localP, -angle - PI * 0.5);

        d = min(d, sdMoonPhaseIcon(localP, phaseSize, i));
    }

    // Small decorative dots between phases
    [unroll]
    for (int j = 0; j < 8; j++)
    {
        float angle = (float(j) + 0.5) * TAU / 8.0 + rotationOffset;
        float2 dotPos = float2(cos(angle), sin(angle)) * radius;
        d = min(d, sdCircle(p - dotPos, LineThickness * 1.2));
    }

    return d;
}

// Zodiac ring - 12 symbols arranged in a circle
float sdZodiacRing(float2 p, float radius, float symbolSize, float rotationOffset)
{
    float d = 1e10;

    // Boundary rings
    d = min(d, sdRing(p, radius - symbolSize * 1.3, LineThickness * 0.6));
    d = min(d, sdRing(p, radius + symbolSize * 1.3, LineThickness * 0.6));

    // Dividing lines between zodiac segments
    [unroll]
    for (int k = 0; k < 12; k++)
    {
        float angle = float(k) * TAU / 12.0 + rotationOffset;
        float2 inner = float2(cos(angle), sin(angle)) * (radius - symbolSize * 1.2);
        float2 outer = float2(cos(angle), sin(angle)) * (radius + symbolSize * 1.2);
        d = min(d, sdLine(p, inner, outer, LineThickness * 0.3));
    }

    // 12 zodiac symbols
    [unroll]
    for (int i = 0; i < 12; i++)
    {
        float angle = (float(i) + 0.5) * TAU / 12.0 + rotationOffset;
        float2 center = float2(cos(angle), sin(angle)) * radius;
        float2 localP = p - center;

        // Rotate to face outward
        localP = opRotate(localP, -angle - PI * 0.5);

        d = min(d, sdZodiacSymbol(localP, symbolSize, i));
    }

    return d;
}

// Runic text ring
float sdRunicRing(float2 p, float radius, float runeSize, int runeCount, float scrollOffset)
{
    float d = 1e10;

    // Boundary rings
    d = min(d, sdRing(p, radius - runeSize * 0.9, LineThickness * 0.4));
    d = min(d, sdRing(p, radius + runeSize * 0.9, LineThickness * 0.4));

    // Runes
    [unroll]
    for (int i = 0; i < 24; i++)
    {
        if (i >= runeCount) break;
        float angle = float(i) * TAU / float(runeCount) + scrollOffset;
        float2 center = float2(cos(angle), sin(angle)) * radius;
        float2 localP = p - center;

        // Rotate to face outward
        localP = opRotate(localP, -angle - PI * 0.5);

        d = min(d, sdRuneLetter(localP, runeSize, i));
    }

    // Small dots between runes
    [unroll]
    for (int j = 0; j < 24; j++)
    {
        if (j >= runeCount) break;
        float angle = (float(j) + 0.5) * TAU / float(runeCount) + scrollOffset;
        float2 dotPos = float2(cos(angle), sin(angle)) * radius;
        d = min(d, sdCircle(p - dotPos, LineThickness));
    }

    return d;
}

// ============================================
// Moon Sigil Layer Rendering
// ============================================

// Center - Tree of Life
float RenderMoonCenter(float2 p, float radius, float time)
{
    float d = 1e10;

    // Tree of Life in center - rotates OPPOSITE to moon phases ring
    float treeSize = radius * TreeOfLifeScale;
    float centerRotation = -time * MoonPhaseRotationSpeed; // Opposite direction to moon phases
    float2 rotP = opRotate(p, centerRotation);
    d = min(d, sdTreeOfLife(rotP, treeSize));

    // Subtle inner boundary circle (also rotates with tree)
    d = min(d, sdRing(p, treeSize * 0.55, LineThickness * 0.3));

    return d;
}

// Inner ring - Runic text
float RenderMoonInner(float2 p, float radius, float time)
{
    float d = 1e10;

    // Runic ring with scrolling animation
    float runicRadius = radius * 0.45;
    float runeSize = radius * 0.035;
    float scrollOffset = time * RuneScrollSpeed * -0.5; // Counter-rotate

    d = min(d, sdRunicRing(p, runicRadius, runeSize, 18, scrollOffset));

    return d;
}

// Middle ring - Zodiac symbols
float RenderMoonMiddle(float2 p, float radius, float time, float pulsePhase)
{
    float d = 1e10;

    // Pulse effect
    float pulse = 1.0;
    if (AnimationFlags & ANIM_PULSE)
    {
        pulse = 1.0 + sin(pulsePhase * TAU) * PulseAmplitude * 0.08;
    }

    // Zodiac ring
    float zodiacRadius = radius * 0.65 * pulse;
    float symbolSize = radius * 0.055;
    float rotOffset = time * ZodiacRotationSpeed;

    d = min(d, sdZodiacRing(p, zodiacRadius, symbolSize, rotOffset));

    return d;
}

// Outer ring - Moon phases
float RenderMoonOuter(float2 p, float radius, float time)
{
    float d = 1e10;

    // Moon phases ring
    float moonRadius = radius * 0.88;
    float phaseSize = radius * 0.07;
    float rotOffset = time * MoonPhaseRotationSpeed + MoonPhaseOffset;

    d = min(d, sdMoonPhasesRing(p, moonRadius, phaseSize, rotOffset));

    // Outer decorative boundary
    d = min(d, sdRing(p, radius * 0.98, LineThickness * 0.8));

    // Corner decorations at cardinal points
    [unroll]
    for (int i = 0; i < 4; i++)
    {
        float angle = float(i) * TAU / 4.0 + time * RotationSpeed * 0.2;
        float2 cornerPos = float2(cos(angle), sin(angle)) * radius * 0.98;

        // Small star/diamond at corners
        float2 localP = opRotate(p - cornerPos, time * RotationSpeed);
        d = min(d, sdStar(localP, radius * 0.025, radius * 0.012, 4));
    }

    return d;
}

// ============================================
// Triangle Mandala Primitives
// ============================================

// Equilateral triangle outline SDF
float sdTriangleOutline(float2 p, float size, float thickness)
{
    // Vertices of equilateral triangle centered at origin
    float h = size * SQRT3 * 0.5;
    float2 v0 = float2(0.0, h * 0.666);
    float2 v1 = float2(-size * 0.5, -h * 0.333);
    float2 v2 = float2(size * 0.5, -h * 0.333);

    float d = sdLine(p, v0, v1, thickness);
    d = min(d, sdLine(p, v1, v2, thickness));
    d = min(d, sdLine(p, v2, v0, thickness));
    return d;
}

// Nested triangles with alternating orientation
float sdNestedTriangles(float2 p, float outerSize, int count, float thickness, float time, bool counterRotate)
{
    float d = 1e10;
    float sizeStep = outerSize / float(count + 1);

    [unroll]
    for (int i = 0; i < 8; i++)
    {
        if (i >= count) break;
        float size = outerSize - float(i) * sizeStep;
        if (size < outerSize * 0.1) break;

        // Alternating rotation direction
        float rotDir = counterRotate ? (i % 2 == 0 ? 1.0 : -1.0) : 1.0;
        float rotAngle = time * RotationSpeed * rotDir * (1.0 + float(i) * 0.1);

        // Also alternate triangle orientation (upside down)
        if (i % 2 == 1)
            rotAngle += PI;

        float2 rotP = opRotate(p, rotAngle);
        d = min(d, sdTriangleOutline(rotP, size, thickness * (1.0 - float(i) * 0.08)));
    }
    return d;
}

// Fractal triangles - triangles within triangles
float sdFractalTriangle(float2 p, float size, int depth, float thickness, float time)
{
    float d = 1e10;

    // Main triangle
    float rotAngle = time * RotationSpeed * 0.3;
    float2 rotP = opRotate(p, rotAngle);
    d = min(d, sdTriangleOutline(rotP, size, thickness));

    // Recursive inner triangles at corners
    if (depth > 0)
    {
        float innerSize = size * 0.4;
        float h = size * SQRT3 * 0.5;

        // Three corner positions for inner triangles
        float2 positions[3];
        positions[0] = float2(0.0, h * 0.4);           // Top
        positions[1] = float2(-size * 0.3, -h * 0.2); // Bottom left
        positions[2] = float2(size * 0.3, -h * 0.2);  // Bottom right

        [unroll]
        for (int i = 0; i < 3; i++)
        {
            float2 cornerP = opRotate(positions[i], rotAngle);
            float2 localP = opRotate(p - cornerP, -rotAngle + PI); // Inverted inner triangles
            d = min(d, sdTriangleOutline(localP, innerSize, thickness * 0.7));

            // Deeper recursion
            if (depth > 1)
            {
                float deeperSize = innerSize * 0.35;
                d = min(d, sdTriangleOutline(opRotate(localP, time * RotationSpeed * 0.5), deeperSize, thickness * 0.5));
            }
        }
    }

    return d;
}

// Rotating triangle ring - triangles arranged in a circle
float sdTriangleRing(float2 p, float radius, int count, float triSize, float thickness, float time, float rotationMult)
{
    float d = 1e10;
    float angleStep = TAU / float(count);

    [unroll]
    for (int i = 0; i < 12; i++)
    {
        if (i >= count) break;
        float angle = float(i) * angleStep + time * RotationSpeed * rotationMult;
        float2 center = float2(cos(angle), sin(angle)) * radius;
        float2 localP = p - center;

        // Each triangle rotates on its own axis
        float localRot = time * RotationSpeed * (i % 2 == 0 ? 1.5 : -1.5);
        localP = opRotate(localP, localRot);

        d = min(d, sdTriangleOutline(localP, triSize, thickness));
    }
    return d;
}

// Sacred geometry - overlapping triangles forming Star of David pattern
float sdSacredTriangles(float2 p, float size, float thickness, float time)
{
    float d = 1e10;

    // Upward triangle
    float rot1 = time * RotationSpeed * 0.2;
    float2 p1 = opRotate(p, rot1);
    d = min(d, sdTriangleOutline(p1, size, thickness));

    // Downward triangle (Star of David)
    float2 p2 = opRotate(p, rot1 + PI);
    d = min(d, sdTriangleOutline(p2, size, thickness));

    return d;
}

// Zooming triangle layers
float sdZoomingTriangles(float2 p, float baseSize, int layers, float thickness, float time, float zoomSpeed, float zoomAmount)
{
    float d = 1e10;

    [unroll]
    for (int i = 0; i < 6; i++)
    {
        if (i >= layers) break;

        // Each layer has different zoom phase
        float phase = time * zoomSpeed + float(i) * TAU / float(layers);
        float zoom = 1.0 + sin(phase) * zoomAmount;
        float size = baseSize * (0.3 + 0.7 * float(layers - i) / float(layers)) * zoom;

        // Counter-rotating layers
        float rot = time * RotationSpeed * (i % 2 == 0 ? 1.0 : -0.7);
        float2 rotP = opRotate(p, rot);

        // Alternate between regular and inverted triangles
        if (i % 2 == 1)
            rotP = opRotate(rotP, PI);

        d = min(d, sdTriangleOutline(rotP, size, thickness * (1.0 - float(i) * 0.1)));
    }

    return d;
}

// ============================================
// Triangle Mandala Layer Rendering
// ============================================

// Center sacred geometry
float RenderMandalaCenter(float2 p, float radius, float time, float morphPhase)
{
    float d = 1e10;

    // Sacred triangles at center (Star of David pattern)
    float sacredSize = radius * 0.25;
    d = min(d, sdSacredTriangles(p, sacredSize, LineThickness, time));

    // Inner rotating hexagon
    float hexRot = time * RotationSpeed * 0.8;
    float2 hexP = opRotate(p, hexRot);
    d = min(d, abs(sdHexagon(hexP, radius * 0.12)) - LineThickness * 0.8);

    // Center point
    d = min(d, sdCircle(p, LineThickness * 2.5));

    // Morphing inner ring
    float ringR = radius * 0.18 * (1.0 + sin(morphPhase * TAU) * 0.1);
    d = min(d, sdRing(p, ringR, LineThickness * 0.6));

    return d;
}

// Inner fractal triangles
float RenderMandalaInner(float2 p, float radius, float time)
{
    float d = 1e10;

    // Fractal triangles
    float fractalSize = radius * 0.45;
    int depth = max(1, min(3, int(FractalDepth)));
    d = min(d, sdFractalTriangle(p, fractalSize, depth, LineThickness * 0.8, time));

    // Triangle ring around fractal
    d = min(d, sdTriangleRing(p, radius * 0.35, InnerTriangles, radius * 0.08, LineThickness * 0.6, time, 0.5));

    return d;
}

// Middle nested triangles with counter-rotation
float RenderMandalaMiddle(float2 p, float radius, float time, float pulsePhase)
{
    float d = 1e10;

    // Pulse effect
    float pulse = 1.0;
    if (AnimationFlags & ANIM_PULSE)
    {
        pulse = 1.0 + sin(pulsePhase * TAU) * PulseAmplitude * 0.15;
    }

    // Nested counter-rotating triangles
    float nestedSize = radius * 0.7 * pulse;
    d = min(d, sdNestedTriangles(p, nestedSize, TriangleLayers, LineThickness, time, CounterRotateLayers > 0.5));

    // Zooming triangle layers
    d = min(d, sdZoomingTriangles(p, radius * 0.55 * pulse, max(2, TriangleLayers - 1), LineThickness * 0.7, time, ZoomSpeed, ZoomAmount));

    // Decorative ring between layers
    d = min(d, sdRing(p, radius * 0.5 * pulse, LineThickness * 0.5));

    return d;
}

// Outer triangle band with symbols
float RenderMandalaOuter(float2 p, float radius, float time)
{
    float d = 1e10;

    // Outer boundary triangles
    float outerRot = time * RotationSpeed * 0.3;
    float2 outerP = opRotate(p, outerRot);
    d = min(d, sdTriangleOutline(outerP, radius * 0.95, LineThickness));

    // Inverted outer triangle
    float2 invP = opRotate(p, outerRot + PI);
    d = min(d, sdTriangleOutline(invP, radius * 0.95, LineThickness));

    // Triangle ring at outer edge
    d = min(d, sdTriangleRing(p, radius * 0.85, 6, radius * 0.1, LineThickness * 0.7, time, -0.4));

    // Corner decorations
    float h = radius * 0.95 * SQRT3 * 0.5;
    float2 corners[3];
    corners[0] = float2(0.0, h * 0.666);
    corners[1] = float2(-radius * 0.95 * 0.5, -h * 0.333);
    corners[2] = float2(radius * 0.95 * 0.5, -h * 0.333);

    [unroll]
    for (int i = 0; i < 3; i++)
    {
        float2 cornerPos = opRotate(corners[i], outerRot);
        d = min(d, sdCircle(p - cornerPos, LineThickness * 3.0));
        d = min(d, sdRing(p - cornerPos, radius * 0.06, LineThickness * 0.5));
    }

    // Radial lines from center to corners
    d = min(d, sdRadialLines(opRotate(p, outerRot), radius * 0.3, radius * 0.8, 3, LineThickness * 0.4));
    d = min(d, sdRadialLines(opRotate(p, outerRot + PI / 3.0), radius * 0.3, radius * 0.75, 3, LineThickness * 0.4));

    return d;
}

// ============================================
// Sigil Layer Rendering
// ============================================

// Center core geometry
float RenderCenterCore(float2 p, float radius, float time, float morphPhase)
{
    float d = 1e10;

    // Geometric flower at center
    d = min(d, sdGeometricFlower(p, radius * 0.35, 6, morphPhase));

    // Inner hexagon
    float hexD = abs(sdHexagon(p, radius * 0.2)) - LineThickness;
    d = min(d, hexD);

    // Rotating inner triangle
    float triAngle = time * RotationSpeed * 0.5;
    float2 triP = opRotate(p, triAngle);
    float triD = abs(sdPolygon(triP, radius * 0.12, 3)) - LineThickness * 0.8;
    d = min(d, triD);

    return d;
}

// Inner lattice geometry
float RenderInnerLattice(float2 p, float radius, float time)
{
    float d = 1e10;

    // Rotation based on settings
    float rotation = time * RotationSpeed * InnerRotationMult;
    if (CounterRotateLayers > 0.5) rotation = -rotation;
    float2 rotP = opRotate(p, rotation);

    // Triangular lattice
    d = min(d, sdTriangularLattice(rotP, radius * 0.55, 3, LineThickness * 0.6));

    // Radial connection lines
    d = min(d, sdRadialLines(rotP, radius * 0.25, radius * 0.55, 12, LineThickness * 0.5));

    // Hexagonal nodes
    [unroll]
    for (int i = 0; i < 6; i++)
    {
        float angle = float(i) * TAU / 6.0 + rotation;
        float2 nodePos = float2(cos(angle), sin(angle)) * radius * 0.4;
        d = min(d, sdHexagon(p - nodePos, radius * 0.04) - LineThickness * 0.5);
    }

    return d;
}

// Middle concentric rings
float RenderMiddleRings(float2 p, float radius, float time, float pulsePhase)
{
    float d = 1e10;

    // Rotation
    float rotation = time * RotationSpeed * MiddleRotationMult;
    float2 rotP = opRotate(p, rotation);

    // Pulse effect on ring radii
    float pulse = 1.0;
    if (AnimationFlags & ANIM_PULSE)
    {
        pulse = 1.0 + sin(pulsePhase * TAU) * PulseAmplitude * 0.1;
    }

    // Main rings
    d = min(d, sdRing(p, radius * 0.6 * pulse, LineThickness));
    d = min(d, sdRing(p, radius * 0.7 * pulse, LineThickness * 0.8));
    d = min(d, sdRing(p, radius * 0.8 * pulse, LineThickness * 1.2));

    // Decorative arcs between rings
    [unroll]
    for (int i = 0; i < 6; i++)
    {
        float angle = float(i) * TAU / 6.0 + rotation;
        float sweepAngle = TAU / 12.0;
        d = min(d, sdArc(p, radius * 0.65 * pulse, angle, sweepAngle, LineThickness * 0.6));
        d = min(d, sdArc(p, radius * 0.75 * pulse, angle + TAU / 12.0, sweepAngle, LineThickness * 0.6));
    }

    return d;
}

// Outer rune band
float RenderRuneBand(float2 p, float radius, float time)
{
    float d = 1e10;

    // Outer boundary rings
    d = min(d, sdRing(p, radius * 0.88, LineThickness * 0.6));
    d = min(d, sdRing(p, radius * 0.98, LineThickness * 0.6));

    // Runes between the rings
    float scrollOffset = time * RuneScrollSpeed;
    d = min(d, sdRuneRing(p, radius * 0.93, radius * 0.04, 12, scrollOffset, LineThickness * 0.5));

    // Decorative dots
    [unroll]
    for (int i = 0; i < 24; i++)
    {
        float angle = float(i) * TAU / 24.0;
        float2 dotPos = float2(cos(angle), sin(angle)) * radius * 0.88;
        d = min(d, sdCircle(p - dotPos, LineThickness * 1.2));
    }

    return d;
}

// ============================================
// Glow and Color
// ============================================

float3 ComputeGlow(float dist, float3 color, float intensity)
{
    float glow = 0.0;

    // Inner bright glow (sharp)
    glow += exp(-dist * dist * 0.5) * 1.0;

    // Mid-range glow
    glow += exp(-dist * 0.15) * 0.6;

    // Outer soft glow
    glow += exp(-dist * 0.05) * 0.3;

    return color * glow * intensity;
}

float3 GetSigilColor(float dist, float radius, float time)
{
    // Distance-based color gradient
    float t = saturate(dist / (radius * 0.5));

    // Blend from core to edge
    float3 color = lerp(CoreColor.rgb, MidColor.rgb, t);
    color = lerp(color, EdgeColor.rgb, saturate((dist - radius * 0.3) / (radius * 0.5)));

    // Slight hue shift over time for magical feel
    float hueShift = sin(time * 0.5) * 0.05;
    // Simple RGB rotation approximation
    float3 shifted = color;
    shifted.r = color.r * cos(hueShift) - color.g * sin(hueShift);
    shifted.g = color.r * sin(hueShift) + color.g * cos(hueShift);

    return lerp(color, shifted, 0.3);
}

// ============================================
// Vertex Shader
// ============================================

VSOutput VSMain(uint vertexId : SV_VertexID)
{
    VSOutput output;

    // Fullscreen triangle
    float2 uv = float2((vertexId << 1) & 2, vertexId & 2);
    output.Position = float4(uv * 2.0 - 1.0, 0.0, 1.0);
    output.Position.y = -output.Position.y;
    output.TexCoord = uv;

    return output;
}

// ============================================
// Pixel Shader
// ============================================

float4 PSMain(VSOutput input) : SV_TARGET
{
    // Convert to pixel coordinates centered on sigil
    float2 pixelPos = input.TexCoord * ViewportSize;
    float2 p = pixelPos - SigilCenter;

    // Early out for pixels far from sigil
    float distFromCenter = length(p);
    if (distFromCenter > SigilRadius * 2.0)
    {
        return float4(0, 0, 0, 0);
    }

    // Animation phases
    float rotationPhase = Time * RotationSpeed;
    float pulsePhase = Time * PulseSpeed;
    float morphPhase = Time * 0.3;

    if (AnimationFlags & ANIM_MORPH)
    {
        morphPhase = Time * 0.5 * MorphAmount;
    }

    // Accumulate SDF from all enabled layers based on style
    float sdf = 1e10;

    if (SigilStyle == STYLE_MOON)
    {
        // Moon style with phases, zodiac, runes, and Tree of Life
        if (LayerFlags & LAYER_CENTER)
        {
            sdf = min(sdf, RenderMoonCenter(p, SigilRadius, Time));
        }

        if (LayerFlags & LAYER_INNER)
        {
            sdf = min(sdf, RenderMoonInner(p, SigilRadius, Time));
        }

        if (LayerFlags & LAYER_MIDDLE)
        {
            sdf = min(sdf, RenderMoonMiddle(p, SigilRadius, Time, pulsePhase));
        }

        if (LayerFlags & LAYER_RUNES)
        {
            sdf = min(sdf, RenderMoonOuter(p, SigilRadius, Time));
        }
    }
    else if (SigilStyle == STYLE_TRIANGLE_MANDALA)
    {
        // Triangle Mandala style
        if (LayerFlags & LAYER_CENTER)
        {
            sdf = min(sdf, RenderMandalaCenter(p, SigilRadius, Time, morphPhase));
        }

        if (LayerFlags & LAYER_INNER)
        {
            sdf = min(sdf, RenderMandalaInner(p, SigilRadius, Time));
        }

        if (LayerFlags & LAYER_MIDDLE)
        {
            sdf = min(sdf, RenderMandalaMiddle(p, SigilRadius, Time, pulsePhase));
        }

        if (LayerFlags & LAYER_RUNES)
        {
            sdf = min(sdf, RenderMandalaOuter(p, SigilRadius, Time));
        }
    }
    else
    {
        // Arcane Circle style (default)
        if (LayerFlags & LAYER_CENTER)
        {
            sdf = min(sdf, RenderCenterCore(p, SigilRadius, Time, morphPhase));
        }

        if (LayerFlags & LAYER_INNER)
        {
            sdf = min(sdf, RenderInnerLattice(p, SigilRadius, Time));
        }

        if (LayerFlags & LAYER_MIDDLE)
        {
            sdf = min(sdf, RenderMiddleRings(p, SigilRadius, Time, pulsePhase));
        }

        if (LayerFlags & LAYER_RUNES)
        {
            sdf = min(sdf, RenderRuneBand(p, SigilRadius, Time));
        }
    }

    // Distance from sigil shapes
    float dist = abs(sdf);

    // Base color from gradient
    float3 baseColor = GetSigilColor(distFromCenter, SigilRadius, Time);

    // Compute glow
    float3 glowColor = float3(0, 0, 0);
    if (LayerFlags & LAYER_GLOW)
    {
        glowColor = ComputeGlow(dist, baseColor, GlowIntensity);
    }
    else
    {
        // Simple anti-aliased edge without glow
        float edge = 1.0 - smoothstep(0.0, LineThickness * 2.0, dist);
        glowColor = baseColor * edge;
    }

    // Apply energy particles (fire/electricity) to all styles
    if (ParticleIntensity > 0.01 && ParticleType > 0)
    {
        float3 energyColor = ComputeEnergyParticles(
            p, sdf, SigilRadius, Time,
            baseColor, ParticleIntensity, ParticleSpeed, ParticleType, ParticleEntropy,
            ParticleSize, FireRiseHeight, ElectricitySpread
        );
        glowColor += energyColor;
    }

    // Pulsing brightness
    float brightness = 1.0;
    if (AnimationFlags & ANIM_PULSE)
    {
        brightness = 1.0 + sin(pulsePhase * TAU * 2.0) * 0.2 * PulseAmplitude;
    }

    // HDR boost for glow
    float hdrBoost = 1.0 + exp(-dist * 0.3) * HdrMultiplier * 0.3;

    // Final color
    float3 finalColor = glowColor * brightness * hdrBoost;

    // Alpha based on glow intensity, FadeAlpha (for click modes), and SigilAlpha (user setting)
    float alpha = saturate(length(glowColor) * 1.5) * FadeAlpha * SigilAlpha;

    // Discard nearly transparent pixels
    if (alpha < 0.01)
    {
        discard;
    }

    return float4(finalColor, alpha);
}

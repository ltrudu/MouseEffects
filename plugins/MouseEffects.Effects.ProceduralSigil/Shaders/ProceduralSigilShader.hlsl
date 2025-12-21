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

    // Accumulate SDF from all enabled layers
    float sdf = 1e10;

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

    // Alpha based on glow intensity
    float alpha = saturate(length(glowColor) * 1.5) * FadeAlpha;

    // Discard nearly transparent pixels
    if (alpha < 0.01)
    {
        discard;
    }

    return float4(finalColor, alpha);
}

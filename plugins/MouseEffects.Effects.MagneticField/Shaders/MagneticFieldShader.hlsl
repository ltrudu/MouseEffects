// MagneticFieldShader.hlsl - Magnetic field line visualization
// Creates curved field lines emanating from magnetic poles with flowing animation

static const float PI = 3.14159265359;
static const float TAU = 6.28318530718;

// Constant buffer
cbuffer Constants : register(b0)
{
    float2 ViewportSize;
    float2 MousePosition;

    float Time;
    int LineCount;
    float FieldStrength;
    float AnimationSpeed;

    float LineThickness;
    float GlowIntensity;
    float EffectRadius;
    float DualPoleMode;

    float PoleSeparation;
    int ColorMode;
    float FieldCurvature;
    float FlowScale;

    float FlowSpeed;
    float Padding1;
    float Padding2;
    float Padding3;

    float4 NorthColor;
    float4 SouthColor;
    float4 UnifiedColor;
};

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

// ============================================
// Utility Functions
// ============================================

float2 Rotate2D(float2 p, float angle)
{
    float c = cos(angle);
    float s = sin(angle);
    return float2(p.x * c - p.y * s, p.x * s + p.y * c);
}

// Simple hash for variation
float Hash(float2 p)
{
    float h = dot(p, float2(127.1, 311.7));
    return frac(sin(h) * 43758.5453123);
}

// Smooth minimum function for field blending
float SmoothMin(float a, float b, float k)
{
    float h = max(k - abs(a - b), 0.0) / k;
    return min(a, b) - h * h * k * 0.25;
}

// ============================================
// Magnetic Field Line Calculation
// ============================================

// Calculate distance to a magnetic field line
// pole: pole position
// angle: angle of this field line from pole (0 to TAU)
// uv: current pixel position
float FieldLineDistance(float2 pole, float angle, float2 uv)
{
    float2 dir = uv - pole;
    float dist = length(dir);

    // Early out if too far
    if (dist > EffectRadius || dist < 1.0)
        return 1000.0;

    // Calculate the actual angle from pole to pixel
    float pixelAngle = atan2(dir.y, dir.x);

    // Normalize angle difference
    float angleDiff = pixelAngle - angle;
    angleDiff = atan2(sin(angleDiff), cos(angleDiff)); // Wrap to [-PI, PI]

    // Apply field curvature - lines curve more as they extend outward
    // This creates the classic dipole magnetic field pattern
    float curveAmount = sin(dist * FieldCurvature * 0.01) * FieldStrength * 0.5;
    float targetAngle = angle + curveAmount;

    // Calculate angular distance
    float angularDist = abs(angleDiff) * dist;

    // Distance falloff - lines fade at edges
    float radialFalloff = smoothstep(EffectRadius, EffectRadius * 0.5, dist);

    return angularDist * (1.0 + (1.0 - radialFalloff) * 2.0);
}

// Calculate dipole field line (connects north to south pole)
float DipoleFieldLine(float2 northPole, float2 southPole, float angle, float2 uv)
{
    // For dipole, field lines curve from north to south
    float2 midpoint = (northPole + southPole) * 0.5;
    float2 poleAxis = southPole - northPole;
    float poleDistance = length(poleAxis);

    // Calculate which pole this pixel is closer to
    float2 toNorth = uv - northPole;
    float2 toSouth = uv - southPole;
    float distNorth = length(toNorth);
    float distSouth = length(toSouth);

    float minDist = 1000.0;

    // Use north pole field line calculation for closer half
    if (distNorth < distSouth)
    {
        minDist = FieldLineDistance(northPole, angle, uv);
    }
    else
    {
        // For south pole, offset angle by PI to create opposite pole
        minDist = FieldLineDistance(southPole, angle + PI, uv);
    }

    return minDist;
}

// ============================================
// Vertex Shader - Fullscreen triangle
// ============================================

VSOutput VSMain(uint vertexId : SV_VertexID)
{
    VSOutput output;

    // Generate fullscreen triangle
    float2 uv = float2((vertexId << 1) & 2, vertexId & 2);
    output.Position = float4(uv * 2.0 - 1.0, 0.0, 1.0);
    output.Position.y = -output.Position.y;
    output.TexCoord = uv;

    return output;
}

// ============================================
// Pixel Shader - Magnetic Field Visualization
// ============================================

float4 PSMain(VSOutput input) : SV_TARGET
{
    float2 screenPos = input.TexCoord * ViewportSize;
    float2 p = screenPos - MousePosition;

    // Calculate pole positions
    float2 northPole = float2(0, 0);
    float2 southPole = float2(0, 0);

    if (DualPoleMode > 0.5)
    {
        // Dual pole: north and south separated
        northPole = float2(0, -PoleSeparation * 0.5);
        southPole = float2(0, PoleSeparation * 0.5);
    }
    else
    {
        // Single pole mode (monopole, which doesn't exist in physics but looks cool)
        northPole = float2(0, 0);
        southPole = float2(0, 0);
    }

    float minFieldDist = 1000.0;
    float lineIndex = 0;
    bool isNorthSide = false;

    // Calculate field lines
    for (int i = 0; i < LineCount; i++)
    {
        float angle = (float(i) / float(LineCount)) * TAU;
        float fieldDist;

        if (DualPoleMode > 0.5)
        {
            // Dipole field
            fieldDist = DipoleFieldLine(northPole, southPole, angle, p);
        }
        else
        {
            // Monopole field (radial from center)
            fieldDist = FieldLineDistance(northPole, angle, p);
        }

        if (fieldDist < minFieldDist)
        {
            minFieldDist = fieldDist;
            lineIndex = float(i);

            // Determine if this is north or south side (for coloring)
            if (DualPoleMode > 0.5)
            {
                float2 toPixel = p - northPole;
                isNorthSide = length(toPixel) < length(p - southPole);
            }
            else
            {
                isNorthSide = true; // All lines are "north" in monopole
            }
        }
    }

    // Early out if no field lines nearby
    if (minFieldDist > LineThickness * 3.0)
        discard;

    // ============================================
    // Field Line Intensity
    // ============================================

    // Core line with smooth falloff
    float lineIntensity = exp(-minFieldDist * minFieldDist / (LineThickness * LineThickness));

    // Glow halo around line
    float glowRadius = LineThickness * 2.5;
    float glow = exp(-minFieldDist * minFieldDist / (glowRadius * glowRadius)) * 0.5;

    // ============================================
    // Animated Flow
    // ============================================

    // Calculate distance from pole for flow animation
    float distFromPole = length(p - (isNorthSide ? northPole : southPole));

    // Flowing energy along field lines
    float flowPattern = frac(distFromPole * FlowScale - Time * FlowSpeed);
    flowPattern = smoothstep(0.3, 0.7, flowPattern); // Sharpen the flow pattern

    // Pulse effect
    float pulse = sin(Time * 2.0 + lineIndex * 0.5) * 0.15 + 0.85;

    // Combine flow and pulse
    float animation = flowPattern * pulse;

    // ============================================
    // Color Selection
    // ============================================

    float3 fieldColor;

    if (ColorMode == 0) // North/South colors
    {
        if (DualPoleMode > 0.5)
        {
            // Different colors for north and south poles
            fieldColor = isNorthSide ? NorthColor.rgb : SouthColor.rgb;
        }
        else
        {
            // Alternate colors for visual interest in monopole
            fieldColor = (lineIndex / float(LineCount)) < 0.5 ? NorthColor.rgb : SouthColor.rgb;
        }
    }
    else if (ColorMode == 1) // Unified color
    {
        fieldColor = UnifiedColor.rgb;
    }
    else // Custom per-line color
    {
        // Rainbow gradient around field
        float hue = lineIndex / float(LineCount);
        float3 rainbow = float3(
            sin(hue * TAU + 0.0) * 0.5 + 0.5,
            sin(hue * TAU + 2.09) * 0.5 + 0.5,
            sin(hue * TAU + 4.19) * 0.5 + 0.5
        );
        fieldColor = rainbow;
    }

    // ============================================
    // Distance Fade
    // ============================================

    float distFromCenter = length(p);
    float distanceFade = smoothstep(EffectRadius * 1.1, EffectRadius * 0.7, distFromCenter);

    // ============================================
    // Final Composition
    // ============================================

    float totalIntensity = (lineIntensity + glow * GlowIntensity) * animation * distanceFade;
    float3 finalColor = fieldColor * totalIntensity * GlowIntensity;

    // Alpha calculation
    float alpha = saturate(totalIntensity * 0.8);

    // Discard very faint pixels for performance
    if (alpha < 0.01)
        discard;

    return float4(finalColor, alpha);
}

// PortalShader.hlsl - Swirling dimensional portal/vortex effect at mouse cursor
// Creates a magical gateway with spiraling energy, depth illusion, and rim glow

static const float PI = 3.14159265359;
static const float TAU = 6.28318530718;

// Constant buffer
cbuffer Constants : register(b0)
{
    float2 ViewportSize;
    float2 MousePosition;

    float Time;
    float PortalRadius;
    float RotationSpeed;
    float SpiralTightness;

    int SpiralArms;
    float GlowIntensity;
    float DepthStrength;
    float RimParticlesEnabled;

    float4 PortalColor;
    float4 RimColor;

    float InnerDarkness;
    float DistortionStrength;
    float HdrMultiplier;
    float ParticleSpeed;

    int ColorTheme; // 0=Blue, 1=Purple, 2=Orange, 3=Rainbow
    float Padding1;
    float Padding2;
    float Padding3;
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

// Simple hash for randomness
float Hash(float2 p)
{
    float h = dot(p, float2(127.1, 311.7));
    return frac(sin(h) * 43758.5453123);
}

// HSV to RGB conversion for rainbow mode
float3 HSVtoRGB(float h, float s, float v)
{
    float c = v * s;
    float x = c * (1.0 - abs(fmod(h * 6.0, 2.0) - 1.0));
    float m = v - c;

    float3 rgb;
    if (h < 1.0 / 6.0)
        rgb = float3(c, x, 0.0);
    else if (h < 2.0 / 6.0)
        rgb = float3(x, c, 0.0);
    else if (h < 3.0 / 6.0)
        rgb = float3(0.0, c, x);
    else if (h < 4.0 / 6.0)
        rgb = float3(0.0, x, c);
    else if (h < 5.0 / 6.0)
        rgb = float3(x, 0.0, c);
    else
        rgb = float3(c, 0.0, x);

    return rgb + m;
}

// Get portal color based on theme
float3 GetPortalColor(float factor)
{
    if (ColorTheme == 0) // Blue
        return lerp(float3(0.1, 0.3, 0.8), float3(0.4, 0.7, 1.0), factor);
    else if (ColorTheme == 1) // Purple
        return lerp(float3(0.4, 0.1, 0.8), float3(0.8, 0.4, 1.0), factor);
    else if (ColorTheme == 2) // Orange
        return lerp(float3(0.8, 0.3, 0.1), float3(1.0, 0.7, 0.3), factor);
    else // Rainbow
    {
        float hue = frac(Time * 0.2 + factor * 0.3);
        return HSVtoRGB(hue, 0.8, 1.0);
    }
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
// Pixel Shader - Portal/Vortex Effect
// ============================================

float4 PSMain(VSOutput input) : SV_TARGET
{
    float2 screenPos = input.TexCoord * ViewportSize;
    float2 p = screenPos - MousePosition;

    // Convert to polar coordinates
    float dist = length(p);
    float angle = atan2(p.y, p.x);

    // Early out if outside portal radius + glow
    float maxDist = PortalRadius * 1.8;
    if (dist > maxDist)
        discard;

    // Normalized distance (0 at center, 1 at edge)
    float normDist = saturate(dist / PortalRadius);

    // ============================================
    // Spiral Pattern
    // ============================================

    // Create spiral distortion - tighter spirals near center
    float spiralAngle = angle + (1.0 - normDist) * SpiralTightness * 5.0 - Time * RotationSpeed;

    // Multiple rotating layers for depth
    float layer1 = sin(spiralAngle * float(SpiralArms) + Time * RotationSpeed * 2.0) * 0.5 + 0.5;
    float layer2 = sin(spiralAngle * float(SpiralArms) * 1.5 - Time * RotationSpeed * 1.5) * 0.5 + 0.5;
    float layer3 = sin(spiralAngle * float(SpiralArms) * 0.7 + Time * RotationSpeed * 0.8) * 0.5 + 0.5;

    // Combine layers with depth falloff - softer combination
    float spiralPattern = (layer1 * 0.4 + layer2 * 0.35 + layer3 * 0.25);

    // Soft radial variation instead of harsh rings
    float radialWave = sin(normDist * 8.0 - Time * 1.5) * 0.15;
    spiralPattern = spiralPattern * (1.0 + radialWave);

    // ============================================
    // Depth Illusion - Center recedes
    // ============================================

    // Darker toward center with adjustable strength
    float depthFade = pow(normDist, 1.0 - DepthStrength * 0.5);
    float centerDarkness = lerp(InnerDarkness, 1.0, depthFade);

    // Add swirling darkness pattern
    float darknessSpiral = sin((angle + Time * RotationSpeed * 0.5) * 3.0 + normDist * 10.0);
    centerDarkness *= 1.0 - (1.0 - normDist) * 0.3 * darknessSpiral;

    // ============================================
    // UV Distortion - Swirl toward center
    // ============================================

    float2 distortedUV = p;
    float distortAmount = (1.0 - normDist) * DistortionStrength;
    float distortAngle = angle + distortAmount * 2.0;
    distortedUV = float2(cos(distortAngle), sin(distortAngle)) * dist;

    // Add turbulence
    float turbulence = sin(distortedUV.x * 0.1 + Time) * cos(distortedUV.y * 0.1 - Time);
    spiralPattern += turbulence * 0.1 * (1.0 - normDist);

    // ============================================
    // Rim Particles/Sparks
    // ============================================

    float particles = 0.0;
    if (RimParticlesEnabled > 0.5 && normDist > 0.7 && normDist < 1.0)
    {
        // Rotating particle field around rim
        float particleAngle = angle + Time * ParticleSpeed;
        float particleCount = 32.0;
        float particleIndex = floor(particleAngle / TAU * particleCount);

        // Create particle at specific angles
        float particlePos = frac(particleAngle / TAU * particleCount);
        float particlePulse = Hash(float2(particleIndex, floor(Time * 2.0)));

        // Sharp particle spots
        if (particlePos < 0.15 && particlePulse > 0.6)
        {
            float particleBrightness = exp(-(particlePos - 0.075) * (particlePos - 0.075) * 1000.0);
            particles = particleBrightness * (sin(Time * 10.0 + particleIndex) * 0.5 + 0.5);
        }
    }

    // ============================================
    // Glow Layers
    // ============================================

    // Main portal glow - concentrated in spiral arms
    float coreGlow = exp(-dist * dist / (PortalRadius * PortalRadius * 0.8)) * spiralPattern;

    // Soft rim glow - reduced intensity
    float rimGlow = exp(-pow(1.0 - normDist, 2.0) * 8.0);

    // Combine glows - clamped to prevent blow-out
    float totalGlow = saturate((coreGlow * 0.7 + rimGlow * 0.4 + particles) * GlowIntensity);

    // ============================================
    // Color Application
    // ============================================

    // Get colors based on position
    float3 baseColor = GetPortalColor(spiralPattern);

    // Mix colors - outer rim gets rim color tint
    float3 finalColor = lerp(baseColor, RimColor.rgb, normDist * 0.3);

    // Apply depth darkening
    finalColor *= centerDarkness;

    // Apply glow as brightness modulation - keeps colors saturated
    finalColor *= totalGlow * 1.5;

    // ============================================
    // Alpha Calculation
    // ============================================

    // Smooth falloff at edge
    float edgeFalloff = smoothstep(1.2, 0.8, normDist);

    // Center should be transparent (void), only rim and spiral should be visible
    float centerHole = smoothstep(0.0, 0.4, normDist); // Transparent in center
    float alpha = saturate(totalGlow * edgeFalloff * centerHole * 0.8);

    // ============================================
    // HDR Boost
    // ============================================

    // Gentle HDR boost only for HDR displays - clamped to prevent blow-out
    float hdrBoost = 1.0 + saturate(coreGlow * 0.5) * (HdrMultiplier - 1.0);
    finalColor *= hdrBoost;

    // ============================================
    // Final Output
    // ============================================

    if (alpha < 0.01)
        discard;

    return float4(finalColor, alpha);
}

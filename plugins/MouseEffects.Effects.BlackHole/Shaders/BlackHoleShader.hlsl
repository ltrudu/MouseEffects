// Black Hole Effect Shader
// Creates gravitational lensing distortion around the mouse cursor

static const float PI = 3.14159265359;

struct PSInput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

cbuffer BlackHoleParams : register(b0)
{
    float2 MousePosition;        // Mouse position in screen pixels
    float2 ViewportSize;         // Viewport size in pixels
    float Radius;                // Black hole radius
    float DistortionStrength;    // Strength of gravitational lensing
    float EventHorizonSize;      // Size of event horizon (0-1 as fraction of radius)
    float AccretionDiskEnabled;  // 1.0 = enabled, 0.0 = disabled
    float RotationSpeed;         // Speed of accretion disk rotation
    float GlowIntensity;         // Brightness of accretion disk
    float Time;                  // Total time in seconds
    float HdrMultiplier;         // HDR brightness multiplier
    float CenterAlpha;           // Alpha of the black hole center
    float CenterGlowEnabled;     // 1.0 = animate bright pixels in center
    float CenterGlowThreshold;   // Luminosity threshold for glow (0-1)
    float CenterGlowIntensity;   // Glow brightness multiplier
    float4 AccretionDiskColor;   // Color of accretion disk
};

Texture2D<float4> ScreenTexture : register(t0);
SamplerState LinearSampler : register(s0);

// Vertex shader - generates fullscreen quad procedurally
PSInput VSMain(uint vertexId : SV_VertexID)
{
    PSInput output;

    // Generate fullscreen triangle strip: 0,1,2,3 -> positions
    float2 uv = float2((vertexId << 1) & 2, vertexId & 2);
    output.Position = float4(uv * 2.0 - 1.0, 0.0, 1.0);
    output.Position.y = -output.Position.y; // Flip Y for DirectX
    output.TexCoord = uv;

    return output;
}

// Simple hash function for noise
float hash(float2 p)
{
    p = frac(p * float2(443.897, 441.423));
    p += dot(p, p.yx + 19.19);
    return frac(p.x * p.y);
}

// Simplex-inspired noise for accretion disk turbulence
float noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);

    float a = hash(i);
    float b = hash(i + float2(1.0, 0.0));
    float c = hash(i + float2(0.0, 1.0));
    float d = hash(i + float2(1.0, 1.0));

    float2 u = f * f * (3.0 - 2.0 * f);

    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

// Pixel shader - applies black hole gravitational lensing
float4 PSMain(PSInput input) : SV_TARGET
{
    float2 uv = input.TexCoord;
    float2 screenPos = uv * ViewportSize;

    // Vector from mouse to current pixel
    float2 toCenter = screenPos - MousePosition;
    float dist = length(toCenter);

    // Normalize direction
    float2 dir = dist > 0.001 ? toCenter / dist : float2(0, 0);

    // Calculate effect falloff (1.0 at center, 0.0 at radius)
    float influence = 1.0 - saturate(dist / Radius);

    // Schwarzschild radius for gravitational lensing formula
    // This creates the characteristic bending of light around the black hole
    float schwarzschildRadius = Radius * EventHorizonSize * 0.5;
    float lensingStrength = schwarzschildRadius / max(dist, 1.0);

    // Apply gravitational lensing distortion
    // Light bends toward the black hole - stronger effect closer to center
    float2 warpedOffset = dir * lensingStrength * DistortionStrength * Radius * 0.5;
    float2 warpedScreenPos = screenPos + warpedOffset;
    float2 warpedUV = warpedScreenPos / ViewportSize;

    // Sample the warped screen texture
    float4 screenColor = ScreenTexture.Sample(LinearSampler, saturate(warpedUV));

    // Event horizon - dark center of the black hole
    float eventHorizonDist = schwarzschildRadius;
    float inEventHorizon = 1.0 - smoothstep(eventHorizonDist * 0.8, eventHorizonDist, dist);

    // Calculate luminosity for center glow effect
    float luminosity = dot(screenColor.rgb, float3(0.299, 0.587, 0.114));

    // Center glow effect - animate bright pixels inside event horizon
    if (CenterGlowEnabled > 0.5 && inEventHorizon > 0.01 && luminosity > CenterGlowThreshold)
    {
        // Calculate how much above threshold this pixel is
        float glowFactor = (luminosity - CenterGlowThreshold) / (1.0 - CenterGlowThreshold + 0.001);
        glowFactor = saturate(glowFactor);

        // Animate glow with pulsing effect
        float glowPulse = 0.5 + 0.5 * sin(Time * 3.0 + dist * 0.05);
        float glowPulse2 = 0.5 + 0.5 * sin(Time * 5.0 - luminosity * 10.0);
        float combinedPulse = glowPulse * 0.6 + glowPulse2 * 0.4;

        // Apply glow - brighten the pixel based on pulse and intensity
        float glowAmount = glowFactor * inEventHorizon * combinedPulse * CenterGlowIntensity;
        screenColor.rgb *= 1.0 + glowAmount * HdrMultiplier;

        // Add a subtle color shift toward warm colors for the glow
        screenColor.rgb += float3(0.2, 0.1, 0.05) * glowAmount * HdrMultiplier;
    }
    else
    {
        // Darken inside event horizon (nothing escapes, not even light)
        screenColor.rgb = lerp(screenColor.rgb, float3(0, 0, 0), inEventHorizon);
    }

    // Accretion disk - glowing ring of matter orbiting the black hole
    float4 finalColor = screenColor;

    if (AccretionDiskEnabled > 0.5 && dist < Radius)
    {
        // Disk is strongest at a specific radius (around 1.5x Schwarzschild radius)
        float diskInnerRadius = eventHorizonDist * 1.2;
        float diskOuterRadius = Radius * 0.9;
        float diskRadius = (diskInnerRadius + diskOuterRadius) * 0.5;
        float diskWidth = (diskOuterRadius - diskInnerRadius) * 0.5;

        // Distance from ideal disk radius
        float diskDist = abs(dist - diskRadius);
        float diskIntensity = 1.0 - smoothstep(0.0, diskWidth, diskDist);

        // Add rotation to the accretion disk
        float angle = atan2(dir.y, dir.x);
        float rotation = angle + Time * RotationSpeed;

        // Add turbulence using noise
        float2 noiseCoord = float2(rotation * 3.0, dist * 0.02);
        float turbulence = noise(noiseCoord + Time * 0.5) * 0.5 + 0.5;
        turbulence += noise(noiseCoord * 2.1 + Time * 0.7) * 0.25;

        // Disk intensity varies with turbulence
        diskIntensity *= turbulence;

        // Doppler shift effect - one side blue-shifted, other red-shifted
        float dopplerShift = sin(rotation) * 0.3;
        float3 diskColor = AccretionDiskColor.rgb;

        // Apply doppler color shift
        diskColor.r *= 1.0 + max(0.0, -dopplerShift);
        diskColor.b *= 1.0 + max(0.0, dopplerShift);

        // Gravitational redshift near event horizon (light loses energy escaping)
        float redshift = smoothstep(eventHorizonDist, diskInnerRadius, dist);
        diskColor.rgb *= lerp(float3(1, 0.3, 0.1), float3(1, 1, 1), redshift);

        // Apply disk glow
        float3 diskGlow = diskColor * diskIntensity * GlowIntensity * HdrMultiplier;

        // Additive blending for glow
        finalColor.rgb += diskGlow;
    }

    // Fade effect at the edges
    float edgeFade = smoothstep(Radius, Radius * 0.7, dist);

    // Outside the effect radius, return transparent
    float alpha = edgeFade;

    // Apply center alpha - reduces opacity inside event horizon
    // CenterAlpha = 1.0: fully opaque black center
    // CenterAlpha = 0.0: fully transparent center (see through)
    alpha *= lerp(1.0, CenterAlpha, inEventHorizon);

    finalColor.a = alpha;

    return finalColor;
}

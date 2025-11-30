// Screen Distortion Shader
// Creates a lens/ripple distortion effect around the mouse cursor

struct PSInput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

cbuffer DistortionParams : register(b0)
{
    float2 MousePosition;   // Mouse position in screen pixels
    float2 ViewportSize;    // Viewport size in pixels
    float DistortionRadius; // Radius of distortion effect in pixels
    float DistortionStrength; // Strength of distortion (0-1)
    float RippleFrequency;  // Number of ripples
    float RippleSpeed;      // Speed of ripple animation
    float Time;             // Current time for animation
    float WaveHeight;       // Height/amplitude of ripple waves (0-1)
    float WaveWidth;        // Width/thickness of ripple waves (0.1-2)
    float EnableChromatic;  // 1.0 = chromatic aberration on, 0.0 = off
    float EnableGlow;       // 1.0 = glow on, 0.0 = off
    float GlowIntensity;    // Glow intensity (0-1)
    float4 GlowColor;       // RGBA glow color
    float EnableWireframe;  // 1.0 = wireframe on, 0.0 = off
    float WireframeSpacing; // Grid spacing in pixels
    float WireframeThickness; // Line thickness
    float WireframePadding;
    float4 WireframeColor;  // RGBA wireframe color
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

// Pixel shader - applies distortion effect
float4 PSMain(PSInput input) : SV_TARGET
{
    float2 uv = input.TexCoord;
    float2 screenPos = uv * ViewportSize;

    // Calculate distance from mouse
    float2 toMouse = screenPos - MousePosition;
    float dist = length(toMouse);

    // Normalize distance relative to radius
    float normalizedDist = dist / DistortionRadius;

    // Only distort within the radius
    if (normalizedDist < 1.0)
    {
        // Create smooth falloff
        float falloff = 1.0 - normalizedDist;
        falloff = falloff * falloff; // Quadratic falloff for smoother edges

        // Calculate distortion direction (radial)
        float2 direction = normalize(toMouse);

        // Create ripple effect
        float ripplePhase = normalizedDist * RippleFrequency - Time * RippleSpeed;
        float rawRipple = sin(ripplePhase * 3.14159 * 2.0);

        // Apply wave width - higher values create broader/flatter waves
        float ripple = sign(rawRipple) * pow(abs(rawRipple), 1.0 / max(WaveWidth, 0.1));

        // Combine lens distortion (pushes outward) with ripple
        float lensDistortion = falloff * DistortionStrength * 0.5;
        float rippleDistortion = ripple * falloff * DistortionStrength * WaveHeight;

        // Apply distortion to UV coordinates
        float2 offset = direction * (lensDistortion + rippleDistortion);
        offset = offset / ViewportSize; // Convert to UV space

        uv = uv + offset;

        // Clamp UVs to prevent sampling outside texture
        uv = saturate(uv);

        // Sample the distorted screen
        float4 distortedColor = ScreenTexture.Sample(LinearSampler, uv);
        float4 finalColor = distortedColor;

        // Optional chromatic aberration
        if (EnableChromatic > 0.5)
        {
            float2 redOffset = offset * 1.1;
            float2 blueOffset = offset * 0.9;

            float4 chromatic;
            chromatic.r = ScreenTexture.Sample(LinearSampler, saturate(input.TexCoord + redOffset)).r;
            chromatic.g = distortedColor.g;
            chromatic.b = ScreenTexture.Sample(LinearSampler, saturate(input.TexCoord + blueOffset)).b;
            chromatic.a = 1.0;

            // Blend between normal and chromatic based on distortion strength
            finalColor = lerp(distortedColor, chromatic, falloff * 0.5);
        }

        // Optional edge glow with configurable color
        if (EnableGlow > 0.5)
        {
            float edgeGlow = falloff * (1.0 - falloff) * 4.0; // Peaks at 0.5
            float3 glow = GlowColor.rgb * edgeGlow * GlowIntensity;
            finalColor.rgb += glow;
        }

        // Optional wireframe overlay to visualize distortion
        if (EnableWireframe > 0.5)
        {
            // Use distorted UV for wireframe to show the deformation
            float2 gridPos = uv * ViewportSize;

            // Calculate distance to nearest grid line
            float2 gridFrac = frac(gridPos / WireframeSpacing);
            float2 gridDist = min(gridFrac, 1.0 - gridFrac) * WireframeSpacing;
            float minDist = min(gridDist.x, gridDist.y);

            // Draw line if within thickness
            float lineAlpha = 1.0 - smoothstep(0.0, WireframeThickness, minDist);
            lineAlpha *= falloff; // Fade with effect falloff

            // Blend wireframe color
            finalColor.rgb = lerp(finalColor.rgb, WireframeColor.rgb, lineAlpha * WireframeColor.a);
        }

        // Set alpha based on effect intensity (for overlay blending)
        finalColor.a = falloff * 0.95 + 0.05; // Always slightly visible in effect area

        return finalColor;
    }
    else
    {
        // Outside distortion radius - fully transparent
        return float4(0, 0, 0, 0);
    }
}

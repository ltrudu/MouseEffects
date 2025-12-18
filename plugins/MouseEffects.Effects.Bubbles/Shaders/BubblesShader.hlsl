// Bubbles Shader - Floating soap bubbles with rainbow iridescence

cbuffer FrameConstants : register(b0)
{
    float2 ViewportSize;
    float Time;
    float HdrMultiplier;
    float4 Padding;
}

struct BubbleInstance
{
    float2 Position;
    float2 Velocity;
    float4 BaseColor;
    float Size;
    float Lifetime;
    float MaxLifetime;
    float IridescencePhase;
    float IridescenceSpeed;
    float WobblePhase;
    float WobbleAmplitudeX;
    float WobbleAmplitudeY;
    float FloatSpeed;
    float DriftSpeed;
    float PopProgress;
    float RimThickness;
    float Transparency;
    float HighlightIntensity;
    float Padding1;
    float Padding2;
};

StructuredBuffer<BubbleInstance> Bubbles : register(t0);

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 UV : TEXCOORD0;
    float4 BaseColor : COLOR0;
    float Alpha : TEXCOORD1;
    float IridescencePhase : TEXCOORD2;
    float RimThickness : TEXCOORD3;
    float HighlightIntensity : TEXCOORD4;
    float PopProgress : TEXCOORD5;
};

// Convert HSV to RGB for rainbow iridescence
float3 HsvToRgb(float h, float s, float v)
{
    float c = v * s;
    float x = c * (1.0 - abs(fmod(h * 6.0, 2.0) - 1.0));
    float m = v - c;

    float3 rgb;
    if (h < 0.166667) rgb = float3(c, x, 0);
    else if (h < 0.333333) rgb = float3(x, c, 0);
    else if (h < 0.5) rgb = float3(0, c, x);
    else if (h < 0.666667) rgb = float3(0, x, c);
    else if (h < 0.833333) rgb = float3(x, 0, c);
    else rgb = float3(c, 0, x);

    return rgb + m;
}

// Vertex shader - Generate quad per bubble instance
VSOutput VSMain(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    VSOutput output;
    BubbleInstance bubble = Bubbles[instanceId];

    // Skip dead bubbles
    if (bubble.Lifetime <= 0)
    {
        output.Position = float4(0, 0, 0, 0);
        output.UV = float2(0, 0);
        output.BaseColor = float4(0, 0, 0, 0);
        output.Alpha = 0;
        output.IridescencePhase = 0;
        output.RimThickness = 0;
        output.HighlightIntensity = 0;
        output.PopProgress = 0;
        return output;
    }

    // Calculate alpha based on lifetime (fade in and out)
    float lifeFraction = bubble.Lifetime / bubble.MaxLifetime;
    float fadeIn = saturate((1.0 - lifeFraction) * 4.0); // Quick fade in
    float fadeOut = saturate(lifeFraction * 2.0); // Slower fade out
    float alpha = min(fadeIn, fadeOut) * bubble.Transparency;

    // Generate quad vertices (two triangles)
    float2 quadUV;
    if (vertexId == 0) quadUV = float2(-1, -1);
    else if (vertexId == 1) quadUV = float2(1, -1);
    else if (vertexId == 2) quadUV = float2(-1, 1);
    else if (vertexId == 3) quadUV = float2(-1, 1);
    else if (vertexId == 4) quadUV = float2(1, -1);
    else quadUV = float2(1, 1);

    // Scale by bubble size (with pop expansion)
    float popScale = 1.0 + bubble.PopProgress * bubble.PopProgress * 0.5; // Expand when popping
    float2 offset = quadUV * bubble.Size * popScale;

    // Position in screen space
    float2 screenPos = bubble.Position + offset;

    // Convert to NDC
    float2 ndc = (screenPos / ViewportSize) * 2.0 - 1.0;
    ndc.y = -ndc.y; // Flip Y for DirectX

    output.Position = float4(ndc, 0, 1);
    output.UV = quadUV;
    output.BaseColor = bubble.BaseColor;
    output.Alpha = alpha * (1.0 - bubble.PopProgress); // Fade out during pop
    output.IridescencePhase = bubble.IridescencePhase;
    output.RimThickness = bubble.RimThickness;
    output.HighlightIntensity = bubble.HighlightIntensity;
    output.PopProgress = bubble.PopProgress;

    return output;
}

// Pixel shader - Render bubble with iridescence and highlight
float4 PSMain(VSOutput input) : SV_TARGET
{
    if (input.Alpha <= 0.001)
        discard;

    // Calculate distance from center
    float dist = length(input.UV);

    // Discard pixels outside the circle
    if (dist > 1.0)
        discard;

    // Calculate bubble rim (outline)
    float rimStart = 1.0 - input.RimThickness;
    float rimMask = smoothstep(rimStart - 0.05, rimStart, dist);

    // Calculate iridescence based on angle and distance
    // Thin film interference simulation
    float angle = atan2(input.UV.y, input.UV.x);
    float normalizedAngle = (angle + 3.14159265) / (2.0 * 3.14159265); // 0 to 1

    // Combine angle and distance for iridescence pattern
    float iridescenceValue = frac(normalizedAngle + dist * 2.0 + input.IridescencePhase);

    // Generate rainbow color
    float3 iridescenceColor = HsvToRgb(iridescenceValue, 0.6, 1.0);

    // Create highlight (top-left reflection)
    float2 highlightPos = float2(-0.35, -0.35);
    float highlightDist = length(input.UV - highlightPos);
    float highlight = 1.0 - smoothstep(0.0, 0.3, highlightDist);
    highlight = pow(highlight, 3.0) * input.HighlightIntensity;

    // Combine colors
    float3 bubbleColor = lerp(
        input.BaseColor.rgb * 0.5, // Base tint (subtle)
        iridescenceColor,           // Iridescence color
        rimMask * input.HighlightIntensity // More iridescence on rim
    );

    // Add highlight (white reflection)
    bubbleColor += highlight * float3(1.0, 1.0, 1.0);

    // Create center transparency gradient (bubbles are more transparent in center)
    float centerTransparency = 1.0 - (dist * 0.3);

    // Pop effect - create expanding ripple pattern
    float popEffect = 0.0;
    if (input.PopProgress > 0.0)
    {
        float rippleFreq = 10.0;
        float ripple = sin(dist * rippleFreq - input.PopProgress * 20.0);
        popEffect = ripple * input.PopProgress * 0.3;
        bubbleColor += popEffect;
    }

    // Final alpha calculation
    float finalAlpha = input.Alpha * centerTransparency;

    // Apply rim brightness boost
    bubbleColor *= 1.0 + rimMask * 0.5;

    // Apply HDR multiplier for bright displays
    bubbleColor *= HdrMultiplier;

    return float4(bubbleColor, finalAlpha);
}

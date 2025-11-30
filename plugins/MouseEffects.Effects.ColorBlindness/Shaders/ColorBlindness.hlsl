// Color Blindness Shader
// Simulates various types of color blindness and provides RGB curve adjustment
// Supports circular, rectangular, and fullscreen application modes

struct PSInput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

cbuffer ColorBlindnessParams : register(b0)
{
    float2 MousePosition;       // Mouse position in screen pixels
    float2 ViewportSize;        // Viewport size in pixels
    float Radius;               // Radius for circular mode (pixels)
    float RectWidth;            // Width for rectangular mode (pixels)
    float RectHeight;           // Height for rectangular mode (pixels)
    float ShapeMode;            // 0=circle, 1=rectangle, 2=fullscreen
    float FilterType;           // 0=none, 1=deuteranopia, 2=protanopia, 3=tritanopia, 4=grayscale, 5=grayscale_inv, 6=inverted
    float Intensity;            // Filter intensity (0-1)
    float ColorBoost;           // Color saturation boost (0-2)
    float EdgeSoftness;         // Edge softness for shape modes (0-1)
    float EnableCurves;         // 1.0 = RGB curves enabled
    float CurveStrength;        // Strength of curve adjustment (0-1)
    float4 Padding;             // Padding for alignment
};

Texture2D<float4> ScreenTexture : register(t0);
Texture1D<float4> CurveLUT : register(t1);  // 256-entry lookup table for RGB curves
SamplerState LinearSampler : register(s0);
SamplerState PointSampler : register(s1);

// Color blindness simulation matrices
// Based on research by Machado, Oliveira, and Fernandes (2009)

// Deuteranopia (green-weak) - most common
static const float3x3 DeuteranopiaMatrix = {
    0.625, 0.375, 0.0,
    0.700, 0.300, 0.0,
    0.0,   0.300, 0.700
};

// Protanopia (red-weak)
static const float3x3 ProtanopiaMatrix = {
    0.567, 0.433, 0.0,
    0.558, 0.442, 0.0,
    0.0,   0.242, 0.758
};

// Tritanopia (blue-yellow)
static const float3x3 TritanopiaMatrix = {
    0.950, 0.050, 0.0,
    0.0,   0.433, 0.567,
    0.0,   0.475, 0.525
};

// Grayscale weights (luminance)
static const float3 GrayscaleWeights = float3(0.2126, 0.7152, 0.0722);

// Apply color blindness filter
float3 ApplyColorBlindness(float3 color, float filterType)
{
    if (filterType < 0.5)
    {
        // None - return original
        return color;
    }
    else if (filterType < 1.5)
    {
        // Deuteranopia
        return mul(DeuteranopiaMatrix, color);
    }
    else if (filterType < 2.5)
    {
        // Protanopia
        return mul(ProtanopiaMatrix, color);
    }
    else if (filterType < 3.5)
    {
        // Tritanopia
        return mul(TritanopiaMatrix, color);
    }
    else if (filterType < 4.5)
    {
        // Grayscale
        float gray = dot(color, GrayscaleWeights);
        return float3(gray, gray, gray);
    }
    else if (filterType < 5.5)
    {
        // Grayscale inverted
        float gray = 1.0 - dot(color, GrayscaleWeights);
        return float3(gray, gray, gray);
    }
    else
    {
        // Inverted
        return 1.0 - color;
    }
}

// Apply saturation/color boost
float3 ApplyColorBoost(float3 color, float boost)
{
    float gray = dot(color, GrayscaleWeights);
    float3 grayColor = float3(gray, gray, gray);

    // Boost > 1 increases saturation, < 1 decreases
    return lerp(grayColor, color, boost);
}

// Apply RGB curves from lookup texture
float3 ApplyCurves(float3 color, float strength)
{
    // Sample the curve LUT for each channel
    // The LUT contains: R curve in .r, G curve in .g, B curve in .b, Master in .a
    float4 rSample = CurveLUT.Sample(PointSampler, color.r);
    float4 gSample = CurveLUT.Sample(PointSampler, color.g);
    float4 bSample = CurveLUT.Sample(PointSampler, color.b);

    // Apply individual channel curves first, then master curve
    float3 curved;
    curved.r = CurveLUT.Sample(PointSampler, rSample.r).a; // R through R curve, then master
    curved.g = CurveLUT.Sample(PointSampler, gSample.g).a; // G through G curve, then master
    curved.b = CurveLUT.Sample(PointSampler, bSample.b).a; // B through B curve, then master

    // Blend based on strength
    return lerp(color, curved, strength);
}

// Calculate effect mask based on shape mode
float CalculateShapeMask(float2 screenPos, float2 mousePos)
{
    if (ShapeMode > 1.5)
    {
        // Fullscreen - always apply
        return 1.0;
    }

    float2 toMouse = screenPos - mousePos;
    float mask = 0.0;

    if (ShapeMode < 0.5)
    {
        // Circle mode
        float dist = length(toMouse);
        float normalizedDist = dist / Radius;

        if (normalizedDist < 1.0)
        {
            mask = 1.0 - normalizedDist;
            mask = smoothstep(0.0, EdgeSoftness + 0.001, mask);
        }
    }
    else
    {
        // Rectangle mode
        float2 halfSize = float2(RectWidth, RectHeight) * 0.5;
        float2 absToMouse = abs(toMouse);

        if (absToMouse.x < halfSize.x && absToMouse.y < halfSize.y)
        {
            // Inside rectangle
            float2 edgeDist = halfSize - absToMouse;
            float minEdgeDist = min(edgeDist.x / halfSize.x, edgeDist.y / halfSize.y);
            mask = smoothstep(0.0, EdgeSoftness + 0.001, minEdgeDist);
        }
    }

    return mask;
}

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

// Pixel shader - applies color blindness effect
// Always renders the full screen - effect area gets filtered, rest is passthrough
// This prevents stale content issues during window dragging
float4 PSMain(PSInput input) : SV_TARGET
{
    float2 uv = input.TexCoord;
    float2 screenPos = uv * ViewportSize;

    // Sample original screen color
    float4 screenColor = ScreenTexture.Sample(LinearSampler, uv);
    float3 color = screenColor.rgb;

    // Calculate shape mask
    float mask = CalculateShapeMask(screenPos, MousePosition);

    if (mask > 0.001)
    {
        // Apply RGB curves if enabled
        if (EnableCurves > 0.5)
        {
            color = ApplyCurves(color, CurveStrength);
        }

        // Apply color blindness filter
        float3 filteredColor = ApplyColorBlindness(color, FilterType);

        // Blend original with filtered based on intensity
        filteredColor = lerp(color, filteredColor, Intensity);

        // Apply color boost
        filteredColor = ApplyColorBoost(filteredColor, ColorBoost);

        // Apply mask for smooth edges
        color = lerp(screenColor.rgb, filteredColor, mask);
    }

    // Always output fully opaque - this covers the entire screen
    // with either the effect (where mask > 0) or the original screen content
    return float4(color, 1.0);
}

// Alternative pixel shader for fullscreen mode (no alpha blending needed)
float4 PSMainFullscreen(PSInput input) : SV_TARGET
{
    float2 uv = input.TexCoord;

    // Sample original screen color
    float4 screenColor = ScreenTexture.Sample(LinearSampler, uv);
    float3 color = screenColor.rgb;

    // Apply RGB curves if enabled
    if (EnableCurves > 0.5)
    {
        color = ApplyCurves(color, CurveStrength);
    }

    // Apply color blindness filter
    float3 filteredColor = ApplyColorBlindness(color, FilterType);

    // Blend original with filtered based on intensity
    filteredColor = lerp(color, filteredColor, Intensity);

    // Apply color boost
    filteredColor = ApplyColorBoost(filteredColor, ColorBoost);

    return float4(filteredColor, 1.0);
}

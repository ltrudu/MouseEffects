// Zoom Effect Shader
// Magnifies the area around the mouse cursor with circle or rectangle shape

struct PSInput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

cbuffer ZoomParams : register(b0)
{
    float2 MousePosition;   // Mouse position in screen pixels
    float2 ViewportSize;    // Viewport size in pixels
    float ZoomFactor;       // Magnification level (2-50)
    float Radius;           // Radius for circle shape (in pixels)
    float Width;            // Width for rectangle shape (in pixels)
    float Height;           // Height for rectangle shape (in pixels)
    int ShapeType;          // 0 = Circle, 1 = Rectangle
    float BorderWidth;      // Border width in pixels
    float Padding1;
    float Padding2;
    float4 BorderColor;     // RGBA border color
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

// Calculate signed distance to rounded rectangle
float sdRoundedBox(float2 p, float2 b, float r)
{
    float2 q = abs(p) - b + r;
    return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
}

// Pixel shader - applies zoom effect
float4 PSMain(PSInput input) : SV_TARGET
{
    float2 uv = input.TexCoord;
    float2 screenPos = uv * ViewportSize;

    // Vector from mouse to current pixel
    float2 toMouse = screenPos - MousePosition;

    // Determine if we're inside the zoom region based on shape type
    float dist = 0.0;
    float edgeDist = 0.0;
    bool insideZoom = false;

    if (ShapeType == 0)
    {
        // Circle shape
        dist = length(toMouse);
        edgeDist = Radius - dist;
        insideZoom = dist < Radius;
    }
    else
    {
        // Rectangle shape - use half dimensions
        float halfW = Width * 0.5;
        float halfH = Height * 0.5;

        // Use signed distance for smooth edges
        float cornerRadius = 4.0; // Slight corner rounding
        float sdf = sdRoundedBox(toMouse, float2(halfW, halfH), cornerRadius);
        edgeDist = -sdf;
        insideZoom = sdf < 0.0;
    }

    if (insideZoom)
    {
        // Calculate zoomed UV coordinates
        // We want to sample from a smaller area around the mouse position
        float2 zoomOffset = toMouse / ZoomFactor;
        float2 zoomScreenPos = MousePosition + zoomOffset;
        float2 zoomUV = zoomScreenPos / ViewportSize;

        // Clamp to prevent sampling outside the screen
        zoomUV = saturate(zoomUV);

        // Sample the zoomed content
        float4 zoomedColor = ScreenTexture.Sample(LinearSampler, zoomUV);

        // Calculate border effect
        float borderAlpha = 0.0;
        if (BorderWidth > 0.0)
        {
            // Smooth border transition
            float borderInner = BorderWidth;
            float borderOuter = BorderWidth * 0.5;

            if (edgeDist < borderInner)
            {
                // Inside border region
                float borderFactor = 1.0 - smoothstep(0.0, borderOuter, edgeDist);
                borderAlpha = borderFactor * BorderColor.a;
            }
        }

        // Blend border color with zoomed content
        float4 finalColor = zoomedColor;
        finalColor.rgb = lerp(zoomedColor.rgb, BorderColor.rgb, borderAlpha);

        // Add subtle edge darkening for depth perception
        float edgeFalloff = smoothstep(0.0, BorderWidth * 2.0, edgeDist);
        finalColor.rgb *= lerp(0.85, 1.0, edgeFalloff);

        // Full opacity inside the zoom lens
        finalColor.a = 1.0;

        return finalColor;
    }
    else
    {
        // Outside zoom region - fully transparent
        return float4(0, 0, 0, 0);
    }
}

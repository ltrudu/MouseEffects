// Kaleidoscope Effect Shader
// Creates real-time kaleidoscopic mirroring of the screen around the mouse cursor

static const float PI = 3.14159265359;

struct PSInput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

cbuffer KaleidoscopeParams : register(b0)
{
    float2 MousePosition;        // Mouse position in screen pixels
    float2 ViewportSize;         // Viewport size in pixels
    float Radius;                // Kaleidoscope radius
    int Segments;                // Number of mirror segments
    float RotationSpeed;         // Speed of rotation
    float RotationOffset;        // Static rotation offset
    float EdgeSoftness;          // Edge blend softness
    float ZoomFactor;            // Zoom factor (1.0 = normal)
    float Time;                  // Total time in seconds
    float HdrMultiplier;         // HDR brightness multiplier
    float4 Padding;              // Padding to 64 bytes
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

// Pixel shader - applies kaleidoscope mirroring
float4 PSMain(PSInput input) : SV_TARGET
{
    float2 uv = input.TexCoord;
    float2 screenPos = uv * ViewportSize;

    // Vector from mouse to current pixel
    float2 toCenter = screenPos - MousePosition;
    float dist = length(toCenter);

    // Calculate effect influence (1.0 at center, 0.0 at radius)
    float influence = 1.0 - saturate(dist / Radius);

    // Outside effect radius, return transparent
    if (influence <= 0.0)
    {
        return float4(0, 0, 0, 0);
    }

    // Convert to polar coordinates
    float angle = atan2(toCenter.y, toCenter.x);

    // Apply rotation (both animated and static offset)
    angle += Time * RotationSpeed + RotationOffset;

    // Calculate segment angle (full circle divided by number of segments)
    float segmentAngle = (2.0 * PI) / float(Segments);

    // Normalize angle to 0-2*PI range
    float normalizedAngle = fmod(angle + PI, 2.0 * PI);
    if (normalizedAngle < 0.0)
        normalizedAngle += 2.0 * PI;

    // Find which segment we're in
    int segmentIndex = int(floor(normalizedAngle / segmentAngle));

    // Get angle within current segment
    float angleInSegment = fmod(normalizedAngle, segmentAngle);

    // Mirror every other segment to create kaleidoscope effect
    if (segmentIndex % 2 == 1)
    {
        angleInSegment = segmentAngle - angleInSegment;
    }

    // Reconstruct angle from first segment
    float mirroredAngle = angleInSegment - PI;

    // Apply zoom factor to distance
    float zoomedDist = dist / ZoomFactor;

    // Convert back to Cartesian coordinates
    float2 mirroredOffset = float2(cos(mirroredAngle), sin(mirroredAngle)) * zoomedDist;
    float2 mirroredScreenPos = MousePosition + mirroredOffset;

    // Convert to UV coordinates
    float2 mirroredUV = mirroredScreenPos / ViewportSize;

    // Sample the screen texture at the mirrored position
    float4 color = ScreenTexture.Sample(LinearSampler, saturate(mirroredUV));

    // Calculate edge fade for smooth blending
    float edgeFade = 1.0;
    if (EdgeSoftness > 0.0)
    {
        float edgeStart = 1.0 - EdgeSoftness;
        float normalizedInfluence = influence;
        edgeFade = smoothstep(0.0, edgeStart, normalizedInfluence);
    }
    else
    {
        edgeFade = influence;
    }

    // Apply HDR multiplier
    color.rgb *= HdrMultiplier;

    // Set alpha based on influence and edge fade
    color.a = edgeFade;

    return color;
}

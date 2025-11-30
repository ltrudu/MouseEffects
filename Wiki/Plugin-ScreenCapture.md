# Screen Capture Plugins

This guide explains how to create effect plugins that capture and transform screen content.

## Overview

Screen capture plugins can:

- **Transform** the screen (distortion, color filters)
- **Sample** screen content (tile effects, magnification)
- **Overlay** on captured content (annotations, highlights)

## Screen Capture Modes

MouseEffects supports two screen capture modes:

### Request Mode (Default)

- Capture happens only when requested
- No waiting for new frames
- Uses cached frame if none available
- Best for: Effects that occasionally need screen content

### Continuous Mode

- Captures every frame (up to 60 FPS)
- Waits for DWM to compose new frame
- Required for: Real-time screen transformation effects

## Enabling Screen Capture

### Property-Based (Recommended)

Override the `RequiresContinuousScreenCapture` property:

```csharp
public class MyScreenEffect : EffectBase
{
    // Enable continuous screen capture
    public override bool RequiresContinuousScreenCapture => true;

    // Rest of implementation...
}
```

The system automatically:
1. Detects this property at startup
2. Enables continuous capture when effect is active
3. Disables when effect is disabled

### Runtime Toggle

For dynamic control:

```csharp
protected override void OnRender(IRenderContext context)
{
    var d3dContext = (D3D11RenderContext)context;

    // Enable capture for this frame
    d3dContext.ContinuousCaptureMode = true;

    // Access screen texture
    var screenTexture = d3dContext.ScreenTexture;

    // Render using screen content...
}
```

## Accessing Screen Content

### Getting the Screen Texture

```csharp
protected override void OnRender(IRenderContext context)
{
    var d3dContext = (D3D11RenderContext)context;

    // Get screen texture (may be null if capture failed)
    ID3D11ShaderResourceView? screenSRV = d3dContext.ScreenTexture;

    if (screenSRV == null)
    {
        // Handle missing screen content
        // Could render placeholder or skip
        return;
    }

    // Bind to pixel shader
    d3dContext.DeviceContext.PSSetShaderResource(0, screenSRV);

    // Render...
}
```

### Using in Shaders

```hlsl
// Screen texture and sampler
Texture2D ScreenTexture : register(t0);
SamplerState ScreenSampler : register(s0);

float4 PS(VSOutput input) : SV_TARGET
{
    // Sample screen at current position
    float4 screenColor = ScreenTexture.Sample(ScreenSampler, input.TexCoord);

    // Transform the color
    float4 result = TransformColor(screenColor);

    return result;
}
```

## Example: Color Filter Effect

A complete example of a screen transformation effect:

### Effect Class

```csharp
using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.DirectX.Graphics;
using Vortice.Direct3D11;

namespace MouseEffects.Effects.ColorFilter;

public class ColorFilterEffect : EffectBase
{
    private ID3D11VertexShader? _vertexShader;
    private ID3D11PixelShader? _pixelShader;
    private ID3D11Buffer? _constantBuffer;
    private ID3D11SamplerState? _sampler;

    // Configuration
    private float _intensity = 1.0f;
    private int _filterType = 0;
    private float _radius = 200f;

    public ColorFilterEffect(EffectMetadata metadata) : base(metadata) { }

    // Enable continuous screen capture
    public override bool RequiresContinuousScreenCapture => true;

    protected override void OnInitialize(IRenderContext context)
    {
        var d3dContext = (D3D11RenderContext)context;
        var device = d3dContext.Device;

        // Load and compile shaders
        var shaderCode = LoadEmbeddedShader("Shaders.ColorFilter.hlsl");
        _vertexShader = CompileVertexShader(device, shaderCode, "VS");
        _pixelShader = CompilePixelShader(device, shaderCode, "PS");

        // Create constant buffer
        _constantBuffer = CreateConstantBuffer<FilterConstants>(device);

        // Create sampler for screen texture
        var samplerDesc = new SamplerDescription
        {
            Filter = Filter.MinMagMipLinear,
            AddressU = TextureAddressMode.Clamp,
            AddressV = TextureAddressMode.Clamp,
            AddressW = TextureAddressMode.Clamp
        };
        _sampler = device.CreateSamplerState(samplerDesc);
    }

    protected override void OnConfigure(EffectConfiguration config)
    {
        _intensity = config.Get("intensity", 1.0f);
        _filterType = config.Get("filterType", 0);
        _radius = config.Get("radius", 200f);
    }

    protected override void OnRender(IRenderContext context)
    {
        var d3dContext = (D3D11RenderContext)context;
        var deviceContext = d3dContext.DeviceContext;

        // Get screen texture
        var screenTexture = d3dContext.ScreenTexture;
        if (screenTexture == null) return;

        // Update constants
        var constants = new FilterConstants
        {
            MousePosition = d3dContext.MousePosition,
            ScreenSize = d3dContext.ViewportSize,
            Intensity = _intensity,
            FilterType = _filterType,
            Radius = _radius
        };
        UpdateConstantBuffer(deviceContext, _constantBuffer, constants);

        // Set render state
        d3dContext.SetBlendState(BlendMode.Opaque);  // Replace screen content
        deviceContext.VSSetShader(_vertexShader);
        deviceContext.PSSetShader(_pixelShader);
        deviceContext.PSSetConstantBuffer(0, _constantBuffer);
        deviceContext.PSSetShaderResource(0, screenTexture);
        deviceContext.PSSetSampler(0, _sampler);

        // Draw fullscreen quad
        deviceContext.Draw(3, 0);
    }

    protected override void OnDispose()
    {
        _sampler?.Dispose();
        _constantBuffer?.Dispose();
        _pixelShader?.Dispose();
        _vertexShader?.Dispose();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FilterConstants
    {
        public Vector2 MousePosition;
        public Vector2 ScreenSize;
        public float Intensity;
        public int FilterType;
        public float Radius;
        public float _padding;
    }
}
```

### Shader

```hlsl
// ColorFilter.hlsl - Screen color transformation

cbuffer Constants : register(b0)
{
    float2 MousePosition;
    float2 ScreenSize;
    float Intensity;
    int FilterType;
    float Radius;
    float _padding;
}

Texture2D ScreenTexture : register(t0);
SamplerState ScreenSampler : register(s0);

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

// Fullscreen triangle
VSOutput VS(uint vertexId : SV_VertexID)
{
    VSOutput output;
    float2 uv = float2((vertexId << 1) & 2, vertexId & 2);
    output.Position = float4(uv * float2(2, -2) + float2(-1, 1), 0, 1);
    output.TexCoord = uv;
    return output;
}

// Color blindness simulation matrices
static const float3x3 Protanopia = float3x3(
    0.567, 0.433, 0.000,
    0.558, 0.442, 0.000,
    0.000, 0.242, 0.758
);

static const float3x3 Deuteranopia = float3x3(
    0.625, 0.375, 0.000,
    0.700, 0.300, 0.000,
    0.000, 0.300, 0.700
);

static const float3x3 Tritanopia = float3x3(
    0.950, 0.050, 0.000,
    0.000, 0.433, 0.567,
    0.000, 0.475, 0.525
);

float4 PS(VSOutput input) : SV_TARGET
{
    float2 pixelPos = input.TexCoord * ScreenSize;

    // Sample screen
    float4 screenColor = ScreenTexture.Sample(ScreenSampler, input.TexCoord);

    // Calculate distance from mouse
    float dist = length(pixelPos - MousePosition);

    // Calculate effect mask (1 = full effect, 0 = no effect)
    float mask = 1.0 - saturate(dist / Radius);
    mask = smoothstep(0, 1, mask);  // Smooth falloff

    // Apply color transformation
    float3 transformedColor = screenColor.rgb;

    if (FilterType == 0)
        transformedColor = mul(Protanopia, screenColor.rgb);
    else if (FilterType == 1)
        transformedColor = mul(Deuteranopia, screenColor.rgb);
    else if (FilterType == 2)
        transformedColor = mul(Tritanopia, screenColor.rgb);

    // Blend based on intensity and mask
    float3 result = lerp(screenColor.rgb, transformedColor, Intensity * mask);

    return float4(result, 1.0);
}
```

## Example: Tile Capture Effect

For effects that capture tiles of screen content:

### Capturing Screen Regions

```hlsl
// Sample screen at offset position
float2 SampleScreenAtOffset(float2 baseUV, float2 offset)
{
    float2 samplePos = baseUV + offset / ScreenSize;

    // Clamp to screen bounds
    samplePos = saturate(samplePos);

    return ScreenTexture.Sample(ScreenSampler, samplePos).rgb;
}

// Sample a rectangular region
float4 SampleTile(float2 centerUV, float2 tileSize)
{
    // Convert to pixel coordinates for sampling
    float2 centerPx = centerUV * ScreenSize;
    float2 halfSize = tileSize * 0.5;

    // Sample center of tile
    return ScreenTexture.Sample(ScreenSampler, centerUV);
}
```

### Per-Tile Data

Use structured buffers for tile information:

```csharp
[StructLayout(LayoutKind.Sequential)]
private struct TileData
{
    public Vector2 Position;      // Screen position
    public Vector2 Size;          // Tile dimensions
    public Vector2 CaptureUV;     // Where to sample screen
    public float Age;             // For animation
    public float Rotation;        // Rotation angle
}
```

```hlsl
struct TileData
{
    float2 Position;
    float2 Size;
    float2 CaptureUV;
    float Age;
    float Rotation;
};

StructuredBuffer<TileData> Tiles : register(t1);

float4 PS(VSOutput input, uint instanceId : SV_InstanceID) : SV_TARGET
{
    TileData tile = Tiles[instanceId];

    // Sample screen at tile's original position
    float4 screenColor = ScreenTexture.Sample(ScreenSampler, tile.CaptureUV);

    // Apply transformations (rotation, scale based on age)
    // ...

    return screenColor;
}
```

## Screen Distortion Effects

For effects that distort the screen:

```hlsl
// Distortion shader
float4 PS(VSOutput input) : SV_TARGET
{
    float2 pixelPos = input.TexCoord * ScreenSize;
    float2 toMouse = pixelPos - MousePosition;
    float dist = length(toMouse);

    // Calculate distortion
    float distortAmount = 0;
    if (dist < Radius)
    {
        float t = 1.0 - (dist / Radius);
        distortAmount = sin(t * 3.14159 * RippleFrequency + Time * RippleSpeed) * Strength * t;
    }

    // Offset UV by distortion
    float2 distortedUV = input.TexCoord + normalize(toMouse) * distortAmount / ScreenSize;

    // Clamp to valid range
    distortedUV = saturate(distortedUV);

    // Sample distorted screen
    float4 result = ScreenTexture.Sample(ScreenSampler, distortedUV);

    // Optional: chromatic aberration
    if (EnableChromatic)
    {
        float2 offset = normalize(toMouse) * distortAmount * 0.5 / ScreenSize;
        result.r = ScreenTexture.Sample(ScreenSampler, distortedUV + offset).r;
        result.b = ScreenTexture.Sample(ScreenSampler, distortedUV - offset).b;
    }

    return result;
}
```

## Hybrid GPU Considerations

When running on laptops with integrated + discrete GPUs:

### Automatic Handling

MouseEffects handles this automatically:
1. Screen capture occurs on the GPU driving the display
2. If different from render GPU, content is copied via CPU
3. Small performance overhead, but works seamlessly

### Best Practices

1. **Don't cache screen textures** - They're updated each frame
2. **Handle null gracefully** - Capture can fail temporarily
3. **Use continuous mode** for transforms - Ensures fresh content

## Blend Modes for Screen Effects

Choose the appropriate blend mode:

| Mode | Use Case |
|------|----------|
| `Opaque` | Full screen replacement (filters, distortion) |
| `Alpha` | Partial overlay on screen content |
| `Additive` | Glow effects on top of screen |
| `Multiply` | Darkening/vignette effects |

```csharp
// For screen replacement (color filter, distortion)
d3dContext.SetBlendState(BlendMode.Opaque);

// For overlay effects (highlight, annotation)
d3dContext.SetBlendState(BlendMode.Alpha);
```

## Performance Tips

### Minimize Screen Reads

Sample the screen texture only where needed:

```hlsl
// Only sample within effect radius
if (dist > Radius)
    return float4(0, 0, 0, 0);  // Transparent outside

float4 screenColor = ScreenTexture.Sample(ScreenSampler, uv);
```

### Use Mipmaps Wisely

Screen texture doesn't have mipmaps by default. For downsampled effects:

```hlsl
// Use SampleLevel for consistent sampling
float4 color = ScreenTexture.SampleLevel(ScreenSampler, uv, 0);
```

### Batch Texture Samples

Group related samples to improve cache coherency:

```hlsl
// Sample multiple nearby positions together
float4 c0 = ScreenTexture.Sample(ScreenSampler, uv);
float4 c1 = ScreenTexture.Sample(ScreenSampler, uv + float2(1,0)/ScreenSize);
float4 c2 = ScreenTexture.Sample(ScreenSampler, uv + float2(0,1)/ScreenSize);
```

## Complete Examples

Study these built-in plugins for screen capture patterns:

| Plugin | Pattern | Description |
|--------|---------|-------------|
| **ScreenDistortion** | Continuous | Real-time lens distortion |
| **ColorBlindness** | Continuous | Color matrix transformation |
| **TileVibration** | Continuous | Captures tiles of screen |
| **RadialDithering** | Optional | Uses screen when available |

## Debugging Screen Capture

### Verify Capture Mode

```csharp
protected override void OnRender(IRenderContext context)
{
    var d3dContext = (D3D11RenderContext)context;

    Debug.WriteLine($"Continuous mode: {d3dContext.ContinuousCaptureMode}");
    Debug.WriteLine($"Screen texture: {(d3dContext.ScreenTexture != null ? "available" : "null")}");
}
```

### Visual Debug

Render the raw screen texture to verify capture:

```hlsl
// Debug: just output screen content
float4 PS(VSOutput input) : SV_TARGET
{
    return ScreenTexture.Sample(ScreenSampler, input.TexCoord);
}
```

## Next Steps

- [Basic Plugin Development](Plugin-Development.md) - Effects without screen capture
- [Architecture Guide](Architecture.md) - Full system architecture
- [Plugins Reference](Plugins.md) - Built-in plugin documentation

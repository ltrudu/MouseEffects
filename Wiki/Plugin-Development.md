# Creating Custom Plugins

This guide explains how to create effect plugins for MouseEffects without screen capture.

## Overview

MouseEffects uses a plugin architecture where effects are implemented as separate DLLs. Each plugin contains:

- **Effect Class** - Implements the visual effect logic
- **Factory Class** - Creates effect instances and provides metadata
- **Shaders** - HLSL code for GPU rendering
- **Settings Control** (optional) - Custom WPF UI for configuration

## Getting Started

### Create Plugin Project

1. Create a new Class Library project:
   ```bash
   dotnet new classlib -n MouseEffects.Effects.MyEffect -f net8.0-windows
   ```

2. Add required references:
   ```xml
   <ItemGroup>
     <ProjectReference Include="..\..\src\MouseEffects.Core\MouseEffects.Core.csproj" />
     <ProjectReference Include="..\..\src\MouseEffects.DirectX\MouseEffects.DirectX.csproj" />
   </ItemGroup>
   ```

3. Configure build output:
   ```xml
   <PropertyGroup>
     <OutputPath>..\..\src\MouseEffects.App\bin\$(Configuration)\$(TargetFramework)\plugins\</OutputPath>
     <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
   </PropertyGroup>
   ```

### Project Structure

```
MouseEffects.Effects.MyEffect/
├── MyEffect.cs              # Effect implementation
├── MyEffectFactory.cs       # Factory class
├── Shaders/
│   └── MyEffect.hlsl        # GPU shaders
└── UI/
    ├── MyEffectSettings.xaml
    └── MyEffectSettings.xaml.cs
```

## Effect Implementation

### Basic Effect Class

```csharp
using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.DirectX.Graphics;
using Vortice.Direct3D11;

namespace MouseEffects.Effects.MyEffect;

public class MyEffect : EffectBase
{
    // GPU resources
    private ID3D11VertexShader? _vertexShader;
    private ID3D11PixelShader? _pixelShader;
    private ID3D11Buffer? _constantBuffer;

    // Effect state
    private float _intensity = 1.0f;
    private Vector4 _color = new(1, 0, 0, 1);

    public MyEffect(EffectMetadata metadata) : base(metadata) { }

    protected override void OnInitialize(IRenderContext context)
    {
        var d3dContext = (D3D11RenderContext)context;
        var device = d3dContext.Device;

        // Compile and create shaders
        var shaderCode = LoadShaderResource("Shaders.MyEffect.hlsl");
        _vertexShader = CompileVertexShader(device, shaderCode, "VS");
        _pixelShader = CompilePixelShader(device, shaderCode, "PS");

        // Create constant buffer
        _constantBuffer = CreateConstantBuffer<EffectConstants>(device);
    }

    protected override void OnConfigure(EffectConfiguration config)
    {
        _intensity = config.Get("intensity", 1.0f);
        _color = config.Get("color", new Vector4(1, 0, 0, 1));
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        // Update effect state based on mouse/time
        // This runs every frame when enabled
    }

    protected override void OnRender(IRenderContext context)
    {
        var d3dContext = (D3D11RenderContext)context;
        var deviceContext = d3dContext.DeviceContext;

        // Update constant buffer
        var constants = new EffectConstants
        {
            MousePosition = d3dContext.MousePosition,
            ScreenSize = d3dContext.ViewportSize,
            Time = (float)d3dContext.TotalTime,
            Intensity = _intensity,
            Color = _color
        };
        UpdateConstantBuffer(deviceContext, _constantBuffer, constants);

        // Set render state
        d3dContext.SetBlendState(BlendMode.Additive);
        deviceContext.VSSetShader(_vertexShader);
        deviceContext.PSSetShader(_pixelShader);
        deviceContext.VSSetConstantBuffer(0, _constantBuffer);
        deviceContext.PSSetConstantBuffer(0, _constantBuffer);

        // Draw
        deviceContext.Draw(6, 0); // Fullscreen quad
    }

    protected override void OnDispose()
    {
        _constantBuffer?.Dispose();
        _pixelShader?.Dispose();
        _vertexShader?.Dispose();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct EffectConstants
    {
        public Vector2 MousePosition;
        public Vector2 ScreenSize;
        public float Time;
        public float Intensity;
        public Vector2 _padding;
        public Vector4 Color;
    }
}
```

### Key Methods

| Method | When Called | Purpose |
|--------|-------------|---------|
| `OnInitialize` | Once at startup | Create GPU resources |
| `OnConfigure` | When settings change | Apply configuration values |
| `OnUpdate` | Every frame | Update effect state |
| `OnRender` | Every frame | Render to screen |
| `OnDispose` | At shutdown | Release GPU resources |
| `OnViewportChanged` | On resize | Adapt to new dimensions |

## Factory Implementation

```csharp
using System.Windows;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.MyEffect;

public class MyEffectFactory : IEffectFactory
{
    public EffectMetadata Metadata => new()
    {
        Id = "my-effect",
        Name = "My Custom Effect",
        Description = "A custom visual effect",
        Author = "Your Name",
        Version = "1.0.0"
    };

    public IEffect CreateEffect() => new MyEffect(Metadata);

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();
        config.Set("intensity", 1.0f);
        config.Set("color", new Vector4(1, 0, 0, 1));
        config.Set("radius", 100f);
        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema(new ConfigurationParameter[]
        {
            new FloatParameter("intensity", "Intensity", "Effect strength")
            {
                MinValue = 0, MaxValue = 2, DefaultValue = 1, Step = 0.1f,
                Group = "General"
            },
            new ColorParameter("color", "Color", "Effect color")
            {
                DefaultValue = new Vector4(1, 0, 0, 1),
                SupportsAlpha = true,
                Group = "Appearance"
            },
            new FloatParameter("radius", "Radius", "Effect radius in pixels")
            {
                MinValue = 10, MaxValue = 500, DefaultValue = 100,
                Group = "Size"
            }
        });
    }

    public FrameworkElement? CreateSettingsControl(IEffect effect)
    {
        // Return null for auto-generated UI
        // Or return custom control:
        // return new MyEffectSettingsControl(effect);
        return null;
    }
}
```

## HLSL Shaders

### Basic Shader Structure

Create `Shaders/MyEffect.hlsl`:

```hlsl
// Constant buffer matching C# struct
cbuffer Constants : register(b0)
{
    float2 MousePosition;
    float2 ScreenSize;
    float Time;
    float Intensity;
    float2 _padding;
    float4 Color;
}

// Vertex shader output
struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

// Fullscreen triangle vertex shader
VSOutput VS(uint vertexId : SV_VertexID)
{
    VSOutput output;

    // Generate fullscreen triangle
    float2 uv = float2((vertexId << 1) & 2, vertexId & 2);
    output.Position = float4(uv * float2(2, -2) + float2(-1, 1), 0, 1);
    output.TexCoord = uv;

    return output;
}

// Pixel shader
float4 PS(VSOutput input) : SV_TARGET
{
    // Convert to pixel coordinates
    float2 pixelPos = input.TexCoord * ScreenSize;

    // Calculate distance from mouse
    float dist = length(pixelPos - MousePosition);

    // Create circular effect
    float radius = 100.0 * Intensity;
    float falloff = 1.0 - saturate(dist / radius);

    // Animate with time
    float pulse = sin(Time * 3.0) * 0.5 + 0.5;

    // Output color with falloff
    float4 result = Color * falloff * pulse;

    return result;
}
```

### Shader Tips

1. **Use structured buffers** for arrays of data (particles, etc.)
2. **Pack constants efficiently** - Align to 16-byte boundaries
3. **Minimize branching** - GPUs prefer SIMD operations
4. **Use intrinsics** - `saturate`, `lerp`, `smoothstep` are optimized

### Embedding Shaders

Add to `.csproj`:
```xml
<ItemGroup>
  <EmbeddedResource Include="Shaders\*.hlsl" />
</ItemGroup>
```

Load in code:
```csharp
private string LoadShaderResource(string name)
{
    var assembly = Assembly.GetExecutingAssembly();
    var resourceName = $"{GetType().Namespace}.{name}";
    using var stream = assembly.GetManifestResourceStream(resourceName);
    using var reader = new StreamReader(stream!);
    return reader.ReadToEnd();
}
```

## Custom Settings UI

### XAML Control

Create `UI/MyEffectSettingsControl.xaml`:

```xml
<UserControl x:Class="MouseEffects.Effects.MyEffect.UI.MyEffectSettingsControl"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <StackPanel Margin="10">
        <!-- Enable checkbox -->
        <CheckBox x:Name="EnabledCheckBox"
                  Content="Enable Effect"
                  IsChecked="{Binding IsEnabled}"
                  Margin="0,0,0,10"/>

        <!-- Intensity slider -->
        <TextBlock Text="Intensity" Margin="0,0,0,5"/>
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="50"/>
            </Grid.ColumnDefinitions>
            <Slider x:Name="IntensitySlider"
                    Minimum="0" Maximum="2" Value="1"
                    ValueChanged="IntensitySlider_ValueChanged"/>
            <TextBlock x:Name="IntensityValue"
                       Grid.Column="1"
                       Text="1.0"
                       TextAlignment="Right"/>
        </Grid>

        <!-- Color picker button -->
        <TextBlock Text="Color" Margin="0,10,0,5"/>
        <Button x:Name="ColorButton"
                Content="Choose Color"
                Click="ColorButton_Click"/>
    </StackPanel>
</UserControl>
```

### Code-Behind

```csharp
using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.MyEffect.UI;

public partial class MyEffectSettingsControl : UserControl
{
    private readonly IEffect _effect;
    private bool _isInitializing = true;

    public event Action<string>? SettingsChanged;

    public MyEffectSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;

        LoadConfiguration();
        _isInitializing = false;
    }

    private void LoadConfiguration()
    {
        EnabledCheckBox.IsChecked = _effect.IsEnabled;

        if (_effect.Configuration.TryGet("intensity", out float intensity))
        {
            IntensitySlider.Value = intensity;
            IntensityValue.Text = intensity.ToString("F1");
        }
    }

    private void UpdateConfiguration()
    {
        if (_isInitializing) return;

        var config = new EffectConfiguration();
        config.Set("intensity", (float)IntensitySlider.Value);
        // ... other settings

        _effect.Configure(config);
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void IntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (IntensityValue != null)
            IntensityValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void ColorButton_Click(object sender, RoutedEventArgs e)
    {
        // Show color picker dialog
        using var dialog = new System.Windows.Forms.ColorDialog();
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            // Apply color...
            UpdateConfiguration();
        }
    }
}
```

## Configuration Schema

### Parameter Types

```csharp
// Float slider
new FloatParameter("key", "Display Name", "Description")
{
    MinValue = 0,
    MaxValue = 100,
    DefaultValue = 50,
    Step = 1,
    Group = "Group Name"
}

// Integer slider
new IntParameter("key", "Display Name", "Description")
{
    MinValue = 0,
    MaxValue = 10,
    DefaultValue = 5
}

// Boolean checkbox
new BoolParameter("key", "Display Name", "Description")
{
    DefaultValue = true
}

// Color picker
new ColorParameter("key", "Display Name", "Description")
{
    DefaultValue = new Vector4(1, 0, 0, 1),
    SupportsAlpha = true
}

// Dropdown
new ChoiceParameter("key", "Display Name", "Description")
{
    Choices = new[] { "Option 1", "Option 2", "Option 3" },
    DefaultValue = 0
}
```

## GPU Resource Helpers

### Create Structured Buffer

```csharp
private ID3D11Buffer CreateStructuredBuffer<T>(ID3D11Device device, int count)
    where T : unmanaged
{
    var desc = new BufferDescription
    {
        ByteWidth = (uint)(Marshal.SizeOf<T>() * count),
        Usage = ResourceUsage.Default,
        BindFlags = BindFlags.ShaderResource,
        MiscFlags = ResourceOptionFlags.BufferStructured,
        StructureByteStride = (uint)Marshal.SizeOf<T>()
    };
    return device.CreateBuffer(desc);
}
```

### Update Buffer

```csharp
private void UpdateBuffer<T>(ID3D11DeviceContext context, ID3D11Buffer buffer, T[] data)
    where T : unmanaged
{
    context.UpdateSubresource(data, buffer);
}
```

### Create Shader Resource View

```csharp
private ID3D11ShaderResourceView CreateSRV(ID3D11Device device, ID3D11Buffer buffer, int count)
{
    var desc = new ShaderResourceViewDescription
    {
        Format = Vortice.DXGI.Format.Unknown,
        ViewDimension = ShaderResourceViewDimension.Buffer,
        Buffer = new BufferShaderResourceView
        {
            FirstElement = 0,
            NumElements = count
        }
    };
    return device.CreateShaderResourceView(buffer, desc);
}
```

## Complete Example

See the built-in plugins for complete examples:

- **ParticleTrail** - Particle system with physics
- **LaserWork** - Line rendering with collision detection
- **RadialDithering** - Pattern-based effect

## Testing Your Plugin

1. Build the plugin project
2. Check DLL appears in `plugins/` folder
3. Run MouseEffects
4. Find your effect in settings

### Debugging Tips

- Use Visual Studio debugger - attach to MouseEffects.App.exe
- Add logging with `System.Diagnostics.Debug.WriteLine()`
- Check for null GPU resources
- Verify shader compilation errors in output

## Next Steps

- [Screen Capture Plugins](Plugin-ScreenCapture.md) - Create effects that transform screen content
- [Architecture Guide](Architecture.md) - Understand the full system

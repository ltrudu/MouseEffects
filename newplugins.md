# New Plugins Implementation Documentation

This document provides detailed information about all new effect plugins implemented for MouseEffects. Use this as a reference for debugging and understanding each plugin's architecture.

---

## Table of Contents

1. [Plugin Architecture Overview](#plugin-architecture-overview)
2. [Common Patterns](#common-patterns)
3. [Plugin List by Category](#plugin-list-by-category)
4. [Detailed Plugin Documentation](#detailed-plugin-documentation)

---

## Plugin Architecture Overview

Each plugin follows a consistent structure:

```
plugins/MouseEffects.Effects.[Name]/
├── MouseEffects.Effects.[Name].csproj    # Project file
├── [Name]Effect.cs                        # Main effect class (extends EffectBase)
├── [Name]Factory.cs                       # Factory class (implements IEffectFactory)
├── Shaders/
│   └── [Name]Shader.hlsl                  # GPU shader (embedded resource)
└── UI/
    ├── [Name]SettingsControl.xaml         # WPF settings UI
    └── [Name]SettingsControl.xaml.cs      # Settings code-behind
```

### Key Base Classes and Interfaces

- **EffectBase**: Base class for all effects, provides lifecycle methods
- **IEffectFactory**: Interface for effect factories, provides metadata and configuration
- **IRenderContext**: Interface for GPU rendering context
- **IEffect**: Interface for effect instances

### Lifecycle Methods

```csharp
OnInitialize(IRenderContext context)    // Create GPU resources
OnConfigurationChanged()                 // Apply settings from Configuration
OnUpdate(GameTime gameTime, MouseState mouseState)  // Update state each frame
OnRender(IRenderContext context)         // Draw to screen
OnDispose()                              // Release GPU resources
```

---

## Common Patterns

### NullReferenceException Prevention

All settings controls initialize `_isLoading = true` at field declaration to prevent slider ValueChanged events from firing before the effect is assigned:

```csharp
private bool _isLoading = true;  // CRITICAL: Initialize at declaration
```

### GPU Structure Alignment

All GPU constant buffers use 16-byte alignment:

```csharp
[StructLayout(LayoutKind.Sequential, Size = 64)]  // Size must be multiple of 16
public struct Constants
{
    public Vector2 ViewportSize;    // 8 bytes
    public Vector2 MousePosition;   // 8 bytes
    public float Time;              // 4 bytes
    public float Padding1;          // 4 bytes (alignment)
    public float Padding2;          // 4 bytes
    public float Padding3;          // 4 bytes
    // Total: 32 bytes (multiple of 16)
}
```

### Configuration Key Prefixes

Each plugin uses a unique prefix for configuration keys to avoid collisions:

| Plugin | Prefix | Example |
|--------|--------|---------|
| PixieDust | `pd_` | `pd_particleCount` |
| MatrixRain | `mr_` | `mr_columnDensity` |
| NeonGlow | `ng_` | `ng_trailLength` |
| BlackHole | `bh_` | `bh_radius` |
| etc. | | |

### Shader Resource Binding

Standard binding slots:
- `b0`: Main constant buffer
- `t0`: Screen texture (for screen capture effects)
- `t1`: Additional textures (particle data, etc.)
- `s0`: Linear sampler
- `s1`: Point sampler

---

## Plugin List by Category

### Particle Effects
- PixieDust, Confetti, CherryBlossoms, FallingLeaves, Snowfall, Fireflies
- Hearts, Bubbles, DandelionSeeds, Rain, EmojiRain, PixelExplosion

### Trail Effects
- NeonGlow, Smoke, CometTrail, FireTrail

### Cosmic/Space Effects
- BlackHole, Portal, StarfieldWarp, Nebula, GravityWell

### Nature Effects
- Aurora, Butterflies, FlowerBloom, CrystalGrowth

### Digital/Tech Effects
- MatrixRain, Circuit, Glitch, Hologram, Runes

### Artistic Effects
- InkBlot, Kaleidoscope, PaintSplatter, Spirograph

### Physics Effects
- Shockwave, MagneticField, LightningStorm, DNAHelix

### Light Effects
- Spotlight

---

## Detailed Plugin Documentation

---

### 1. PixieDust

**ID**: `pixiedust`
**Config Prefix**: `pd_`
**Blend Mode**: Additive

**Description**: Magical sparkle particles that follow the mouse cursor with twinkling effects.

**Files**:
- `PixieDustEffect.cs` - Particle system with GPU instancing
- `PixieDustFactory.cs` - Default configuration
- `Shaders/PixieDustShader.hlsl` - Point sprite rendering with glow
- `UI/PixieDustSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 48)]
struct PixieDustConstants
{
    Vector2 ViewportSize;      // 8
    Vector2 MousePosition;     // 8
    float Time;                // 4
    float ParticleSize;        // 4
    float GlowIntensity;       // 4
    float HdrMultiplier;       // 4
    Vector4 Color;             // 16
}

[StructLayout(LayoutKind.Sequential, Size = 32)]
struct Particle
{
    Vector2 Position;          // 8
    Vector2 Velocity;          // 8
    float Life;                // 4
    float Size;                // 4
    float Brightness;          // 4
    float Rotation;            // 4
}
```

**Configuration Keys**:
- `pd_particleCount` (int, 50-500, default 200)
- `pd_particleSize` (float, 2-20, default 8)
- `pd_glowIntensity` (float, 0.5-5, default 2)
- `pd_trailLength` (float, 0.1-2, default 0.5)
- `pd_sparkleRate` (float, 0.1-3, default 1)
- `pd_colorHue` (float, 0-1, default 0.8) - Purple/pink hue
- `pd_hdrMultiplier` (float, 1-10, default 2)

**Key Features**:
- Particles spawn at mouse position with random velocity
- Twinkle effect using sin wave on brightness
- Color cycling option
- Size variation based on lifetime
- Gravity-affected falling

---

### 2. MatrixRain

**ID**: `matrixrain`
**Config Prefix**: `mr_`
**Blend Mode**: Additive

**Description**: Digital rain effect inspired by The Matrix with falling characters.

**Files**:
- `MatrixRainEffect.cs` - Column-based character system
- `MatrixRainFactory.cs` - Default configuration
- `Shaders/MatrixRainShader.hlsl` - Character rendering with glow
- `UI/MatrixRainSettingsControl.xaml/.cs` - Settings with color picker

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 64)]
struct MatrixConstants
{
    Vector2 ViewportSize;
    Vector2 MousePosition;
    float Time;
    float ColumnDensity;
    float CharSize;
    float GlowIntensity;
    Vector4 Color;
    float TrailLength;
    float CharChangeRate;
    float MinSpeed;
    float MaxSpeed;
}

[StructLayout(LayoutKind.Sequential, Size = 32)]
struct MatrixColumn
{
    float X;
    float HeadY;
    float Speed;
    float Length;
    int CharOffset;
    float Brightness;
    float Padding1, Padding2;
}
```

**Configuration Keys**:
- `mr_columnDensity` (float, 0.5-3, default 1)
- `mr_minFallSpeed` (float, 50-200, default 100)
- `mr_maxFallSpeed` (float, 200-500, default 300)
- `mr_charChangeRate` (float, 0.1-2, default 0.5)
- `mr_glowIntensity` (float, 0.5-3, default 1.5)
- `mr_trailLength` (float, 5-30, default 15)
- `mr_effectRadius` (float, 100-800, default 400)
- `mr_color` (Vector4, default green)

**Key Features**:
- Katakana-like character rendering
- Head character brighter than trail
- Character morphing animation
- Radius-based effect around mouse
- Customizable color (classic green or custom)

---

### 3. NeonGlow

**ID**: `neonglow`
**Config Prefix**: `ng_`
**Blend Mode**: Additive

**Description**: Glowing neon trail that follows mouse movement with customizable colors.

**Files**:
- `NeonGlowEffect.cs` - Trail point management
- `NeonGlowFactory.cs` - Default configuration with color modes
- `Shaders/NeonGlowShader.hlsl` - Line rendering with multi-layer glow
- `UI/NeonGlowSettingsControl.xaml/.cs` - Color mode selection

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 80)]
struct NeonConstants
{
    Vector2 ViewportSize;
    float Time;
    float LineThickness;
    float GlowIntensity;
    int GlowLayers;
    float FadeSpeed;
    float SmoothingFactor;
    Vector4 PrimaryColor;
    Vector4 SecondaryColor;
    int ColorMode;
    float RainbowSpeed;
    float HdrMultiplier;
    float Padding;
}

[StructLayout(LayoutKind.Sequential, Size = 16)]
struct TrailPoint
{
    Vector2 Position;
    float Age;
    float Padding;
}
```

**Configuration Keys**:
- `ng_maxTrailPoints` (int, 50-500, default 200)
- `ng_trailSpacing` (float, 2-15, default 5)
- `ng_lineThickness` (float, 2-20, default 6)
- `ng_glowLayers` (int, 2-8, default 4)
- `ng_glowIntensity` (float, 0.5-4, default 2)
- `ng_fadeSpeed` (float, 0.1-3, default 1)
- `ng_smoothingFactor` (float, 0-0.9, default 0.3)
- `ng_colorMode` (int, 0=Fixed, 1=Rainbow, 2=Gradient)
- `ng_primaryColor` (Vector4)
- `ng_secondaryColor` (Vector4)
- `ng_rainbowSpeed` (float, 0.1-3, default 1)

**Key Features**:
- Smooth trail following mouse
- Multi-layer glow effect
- Three color modes: Fixed, Rainbow, Gradient
- Configurable line thickness and glow layers
- Age-based fade out

---

### 4. BlackHole

**ID**: `blackhole`
**Config Prefix**: `bh_`
**Blend Mode**: Alpha
**Requires Screen Capture**: Yes

**Description**: Gravitational distortion effect that warps the screen around the mouse cursor.

**Files**:
- `BlackHoleEffect.cs` - Distortion calculation
- `BlackHoleFactory.cs` - Default configuration
- `Shaders/BlackHoleShader.hlsl` - Screen distortion with accretion disk
- `UI/BlackHoleSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 64)]
struct BlackHoleConstants
{
    Vector2 ViewportSize;
    Vector2 MousePosition;
    float Time;
    float Radius;
    float DistortionStrength;
    float EventHorizonSize;
    float AccretionDiskSize;
    float AccretionSpeed;
    float GlowIntensity;
    float HdrMultiplier;
    Vector4 AccretionColor;
}
```

**Configuration Keys**:
- `bh_radius` (float, 50-300, default 150)
- `bh_distortionStrength` (float, 0.1-2, default 0.8)
- `bh_eventHorizonSize` (float, 0.1-0.5, default 0.2)
- `bh_accretionDiskSize` (float, 0.3-1, default 0.6)
- `bh_accretionSpeed` (float, 0.5-5, default 2)
- `bh_glowIntensity` (float, 0.5-3, default 1.5)
- `bh_accretionColor` (Vector4, default orange/yellow)

**Key Features**:
- Gravitational lensing distortion
- Black event horizon center
- Rotating accretion disk with glow
- Particle effects being pulled in
- Screen capture for distortion

---

### 5. Butterflies

**ID**: `butterflies`
**Config Prefix**: `bf_`
**Blend Mode**: Alpha

**Description**: Animated butterflies that flutter around the mouse cursor.

**Files**:
- `ButterfliesEffect.cs` - Butterfly behavior and animation
- `ButterfliesFactory.cs` - Default configuration
- `Shaders/ButterfliesShader.hlsl` - Procedural butterfly rendering
- `UI/ButterfliesSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 48)]
struct ButterflyConstants
{
    Vector2 ViewportSize;
    Vector2 MousePosition;
    float Time;
    float WingSpeed;
    float Size;
    float HdrMultiplier;
    int ColorPalette;
    float Padding1, Padding2, Padding3;
}

[StructLayout(LayoutKind.Sequential, Size = 48)]
struct Butterfly
{
    Vector2 Position;
    Vector2 Velocity;
    float WingPhase;
    float Size;
    float Rotation;
    int ColorIndex;
    Vector4 Color;
}
```

**Configuration Keys**:
- `bf_count` (int, 5-50, default 15)
- `bf_size` (float, 20-80, default 40)
- `bf_wingSpeed` (float, 2-10, default 5)
- `bf_flutterAmount` (float, 0.2-1, default 0.5)
- `bf_followStrength` (float, 0.1-1, default 0.3)
- `bf_colorPalette` (int, 0=Monarch, 1=Blue Morpho, 2=Rainbow)

**Key Features**:
- Procedural butterfly wing shapes
- Realistic wing flapping animation
- Smooth following behavior with flutter
- Multiple color palettes
- Size variation

---

### 6. Portal

**ID**: `portal`
**Config Prefix**: (none, uses direct names)
**Blend Mode**: Additive
**Requires Screen Capture**: Optional

**Description**: Swirling portal vortex effect with depth and energy rings.

**Files**:
- `PortalEffect.cs` - Portal animation and rendering
- `PortalFactory.cs` - Default configuration
- `Shaders/PortalShader.hlsl` - Spiral and glow rendering
- `UI/PortalSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 96)]
struct PortalConstants
{
    Vector2 ViewportSize;
    Vector2 MousePosition;
    float Time;
    float PortalRadius;
    int SpiralArms;
    float SpiralTightness;
    float RotationSpeed;
    float GlowIntensity;
    float DepthStrength;
    float InnerDarkness;
    float DistortionStrength;
    int RimParticlesEnabled;
    float ParticleSpeed;
    int ColorTheme;
    float HdrMultiplier;
    // Padding to 96 bytes
}
```

**Configuration Keys**:
- `portalRadius` (float, 50-300, default 120)
- `spiralArms` (int, 2-8, default 4)
- `spiralTightness` (float, 0.5-3, default 1.5)
- `rotationSpeed` (float, 0.5-5, default 2)
- `glowIntensity` (float, 0.5-3, default 1.5)
- `depthStrength` (float, 0-1, default 0.5)
- `innerDarkness` (float, 0-1, default 0.8)
- `distortionStrength` (float, 0-1, default 0.3)
- `rimParticlesEnabled` (bool, default true)
- `particleSpeed` (float, 1-5, default 2)
- `colorTheme` (int, 0=Blue, 1=Orange, 2=Green, 3=Purple)

**Key Features**:
- Rotating spiral arms
- Depth illusion with inner darkness
- Rim particle effects
- Optional screen distortion
- Multiple color themes

---

### 7. Fireflies

**ID**: `fireflies`
**Config Prefix**: `ff_`
**Blend Mode**: Additive

**Description**: Glowing fireflies that float and pulse around the mouse.

**Files**:
- `FirefliesEffect.cs` - Firefly behavior with pulsing
- `FirefliesFactory.cs` - Default configuration
- `Shaders/FirefliesShader.hlsl` - Point light rendering
- `UI/FirefliesSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 32)]
struct FireflyConstants
{
    Vector2 ViewportSize;
    float Time;
    float GlowSize;
    float GlowIntensity;
    float HdrMultiplier;
    float Padding1, Padding2;
}

[StructLayout(LayoutKind.Sequential, Size = 32)]
struct Firefly
{
    Vector2 Position;
    Vector2 Velocity;
    float PulsePhase;
    float PulseSpeed;
    float Size;
    float Brightness;
}
```

**Configuration Keys**:
- `ff_count` (int, 10-100, default 30)
- `ff_glowSize` (float, 10-50, default 25)
- `ff_glowIntensity` (float, 0.5-3, default 1.5)
- `ff_pulseSpeed` (float, 0.5-3, default 1)
- `ff_wanderSpeed` (float, 10-100, default 40)
- `ff_attractionStrength` (float, 0-1, default 0.2)
- `ff_color` (Vector4, default warm yellow)

**Key Features**:
- Random wandering behavior
- Pulsing glow (on/off cycle)
- Soft attraction to mouse
- Warm yellow-green color
- Natural movement patterns

---

### 8. Snowfall

**ID**: `snowfall`
**Config Prefix**: `sf_`
**Blend Mode**: Alpha

**Description**: Falling snowflakes with wind effects around the mouse cursor.

**Files**:
- `SnowfallEffect.cs` - Snowflake physics
- `SnowfallFactory.cs` - Default configuration
- `Shaders/SnowfallShader.hlsl` - Snowflake shape rendering
- `UI/SnowfallSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 48)]
struct SnowfallConstants
{
    Vector2 ViewportSize;
    Vector2 MousePosition;
    float Time;
    float WindStrength;
    float WindFrequency;
    float GlowIntensity;
    float MinSize;
    float MaxSize;
    float HdrMultiplier;
    float Padding;
}

[StructLayout(LayoutKind.Sequential, Size = 32)]
struct Snowflake
{
    Vector2 Position;
    float Size;
    float Rotation;
    float RotationSpeed;
    float FallSpeed;
    float SwayPhase;
    float Opacity;
}
```

**Configuration Keys**:
- `sf_snowflakeCount` (int, 50-500, default 150)
- `sf_spawnRadius` (float, 100-600, default 300)
- `sf_lifetime` (float, 2-10, default 5)
- `sf_fallSpeed` (float, 20-150, default 60)
- `sf_windStrength` (float, 0-100, default 30)
- `sf_windFrequency` (float, 0.1-2, default 0.5)
- `sf_rotationSpeed` (float, 0-3, default 1)
- `sf_minSize` (float, 3-15, default 5)
- `sf_maxSize` (float, 10-40, default 20)
- `sf_glowIntensity` (float, 0-2, default 0.5)

**Key Features**:
- 6-pointed snowflake shapes
- Rotation while falling
- Wind sway effect
- Size variation
- Spawns around mouse position

---

### 9. Aurora

**ID**: `aurora`
**Config Prefix**: `au_`
**Blend Mode**: Additive

**Description**: Northern lights aurora borealis effect emanating from the mouse.

**Files**:
- `AuroraEffect.cs` - Wave animation
- `AuroraFactory.cs` - Color presets
- `Shaders/AuroraShader.hlsl` - Flowing light bands
- `UI/AuroraSettingsControl.xaml/.cs` - Settings with presets

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 112)]
struct AuroraConstants
{
    Vector2 ViewportSize;
    Vector2 MousePosition;
    float Time;
    float Height;
    float HorizontalSpread;
    int NumLayers;
    float ColorIntensity;
    float GlowStrength;
    float WaveSpeed;
    float WaveFrequency;
    float NoiseScale;
    float NoiseStrength;
    float VerticalFlow;
    float HdrMultiplier;
    Vector4 PrimaryColor;
    Vector4 SecondaryColor;
    Vector4 TertiaryColor;
    Vector4 AccentColor;
}
```

**Configuration Keys**:
- `au_height` (float, 100-500, default 250)
- `au_horizontalSpread` (float, 100-600, default 300)
- `au_numLayers` (int, 2-6, default 4)
- `au_colorIntensity` (float, 0.5-3, default 1.5)
- `au_glowStrength` (float, 0.5-3, default 1.5)
- `au_waveSpeed` (float, 0.1-2, default 0.5)
- `au_waveFrequency` (float, 0.5-3, default 1)
- `au_noiseScale` (float, 0.5-3, default 1)
- `au_noiseStrength` (float, 0-1, default 0.3)
- `au_verticalFlow` (float, 0-2, default 0.5)
- `au_primaryColor`, `au_secondaryColor`, `au_tertiaryColor`, `au_accentColor` (Vector4)

**Color Presets**:
- Classic Aurora (Green/Cyan/Purple/Pink)
- Pink Aurora
- Blue Aurora
- Red Aurora
- Purple Aurora

**Key Features**:
- Multiple flowing light layers
- Noise-based undulation
- Four-color gradient system
- Vertical flow animation
- Color preset selection

---

### 10. CherryBlossoms

**ID**: `cherryblossoms`
**Config Prefix**: `cb_`
**Blend Mode**: Alpha

**Description**: Falling cherry blossom petals with gentle floating motion.

**Files**:
- `CherryBlossomsEffect.cs` - Petal physics
- `CherryBlossomsFactory.cs` - Default configuration
- `Shaders/CherryBlossomsShader.hlsl` - Petal shape rendering
- `UI/CherryBlossomsSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 32)]
struct Petal
{
    Vector2 Position;
    Vector2 Velocity;
    float Rotation;
    float RotationSpeed;
    float Size;
    float Life;
}
```

**Configuration Keys**:
- `cb_petalCount` (int, 20-200, default 80)
- `cb_spawnRadius` (float, 100-500, default 250)
- `cb_fallSpeed` (float, 20-100, default 40)
- `cb_swayAmount` (float, 10-80, default 30)
- `cb_rotationSpeed` (float, 0.5-3, default 1)
- `cb_petalSize` (float, 8-30, default 15)
- `cb_colorVariation` (float, 0-0.3, default 0.1)

**Key Features**:
- Realistic petal shape (ellipse with notch)
- Gentle swaying motion
- Rotation while falling
- Pink color with variation
- Spawns above mouse, falls down

---

### 11. FallingLeaves

**ID**: `fallingleaves`
**Config Prefix**: `fl_`
**Blend Mode**: Alpha

**Description**: Autumn leaves falling with realistic tumbling motion.

**Files**:
- `FallingLeavesEffect.cs` - Leaf physics with tumbling
- `FallingLeavesFactory.cs` - Default configuration
- `Shaders/FallingLeavesShader.hlsl` - Leaf shape rendering
- `UI/FallingLeavesSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 48)]
struct Leaf
{
    Vector2 Position;
    Vector2 Velocity;
    float Rotation;
    float RotationSpeed;
    float TumblePhase;
    float Size;
    Vector4 Color;
}
```

**Configuration Keys**:
- `fl_leafCount` (int, 20-150, default 60)
- `fl_spawnRadius` (float, 100-500, default 300)
- `fl_fallSpeed` (float, 30-120, default 50)
- `fl_tumbleSpeed` (float, 1-5, default 2)
- `fl_swayAmount` (float, 20-100, default 50)
- `fl_leafSize` (float, 15-50, default 25)
- `fl_colorPalette` (int, 0=Autumn, 1=Summer, 2=Spring)

**Key Features**:
- Multiple leaf shapes (maple, oak, etc.)
- Tumbling rotation effect
- Autumn color palette (red, orange, yellow, brown)
- Wind sway effect
- Realistic falling physics

---

### 12. Confetti

**ID**: `confetti`
**Config Prefix**: `cf_`
**Blend Mode**: Alpha

**Description**: Colorful confetti particles bursting from clicks or following mouse.

**Files**:
- `ConfettiEffect.cs` - Confetti particle system
- `ConfettiFactory.cs` - Default configuration
- `Shaders/ConfettiShader.hlsl` - Rectangle/streamer rendering
- `UI/ConfettiSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 48)]
struct Confetti
{
    Vector2 Position;
    Vector2 Velocity;
    float Rotation;
    float RotationSpeed;
    float Width;
    float Height;
    Vector4 Color;
}
```

**Configuration Keys**:
- `cf_particleCount` (int, 50-500, default 200)
- `cf_burstCount` (int, 20-100, default 50)
- `cf_gravity` (float, 50-300, default 150)
- `cf_initialSpeed` (float, 100-500, default 300)
- `cf_rotationSpeed` (float, 1-10, default 5)
- `cf_minSize` (float, 5-15, default 8)
- `cf_maxSize` (float, 15-40, default 20)
- `cf_triggerOnClick` (bool, default true)
- `cf_continuousSpawn` (bool, default false)

**Key Features**:
- Burst on click or continuous spawn
- Rectangle and streamer shapes
- Rainbow color palette
- Gravity and air resistance
- Tumbling rotation

---

### 13. Glitch

**ID**: `glitch`
**Config Prefix**: `gl_`
**Blend Mode**: Alpha
**Requires Screen Capture**: Yes

**Description**: Digital glitch effects with RGB split and scan lines around the cursor.

**Files**:
- `GlitchEffect.cs` - Glitch timing and intensity
- `GlitchFactory.cs` - Default configuration
- `Shaders/GlitchShader.hlsl` - Screen distortion effects
- `UI/GlitchSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 64)]
struct GlitchConstants
{
    Vector2 ViewportSize;
    Vector2 MousePosition;
    float Time;
    float GlitchIntensity;
    float RgbSplitAmount;
    float ScanLineIntensity;
    float BlockGlitchAmount;
    float NoiseAmount;
    float EffectRadius;
    float HdrMultiplier;
    float GlitchSpeed;
    float Padding1, Padding2, Padding3;
}
```

**Configuration Keys**:
- `gl_glitchIntensity` (float, 0.1-1, default 0.5)
- `gl_rgbSplitAmount` (float, 0-30, default 10)
- `gl_scanLineIntensity` (float, 0-1, default 0.3)
- `gl_blockGlitchAmount` (float, 0-1, default 0.3)
- `gl_noiseAmount` (float, 0-0.5, default 0.1)
- `gl_effectRadius` (float, 50-400, default 200)
- `gl_glitchSpeed` (float, 1-10, default 5)

**Key Features**:
- RGB channel separation
- Horizontal scan lines
- Block displacement glitches
- Static noise overlay
- Radius-based effect area

---

### 14. StarfieldWarp

**ID**: `starfieldwarp`
**Config Prefix**: `sw_`
**Blend Mode**: Additive

**Description**: Hyperspace starfield effect with stars streaking past.

**Files**:
- `StarfieldWarpEffect.cs` - Star management
- `StarfieldWarpFactory.cs` - Default configuration
- `Shaders/StarfieldWarpShader.hlsl` - Star streak rendering
- `UI/StarfieldWarpSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 32)]
struct Star
{
    Vector3 Position;  // X, Y, Z (depth)
    float Speed;
    float Size;
    float Brightness;
    float Padding1, Padding2;
}
```

**Configuration Keys**:
- `sw_starCount` (int, 100-2000, default 500)
- `sw_warpSpeed` (float, 0.5-5, default 2)
- `sw_starSize` (float, 1-5, default 2)
- `sw_streakLength` (float, 0.1-2, default 0.5)
- `sw_centerX` (float, 0-1, default 0.5) - Vanishing point
- `sw_centerY` (float, 0-1, default 0.5)
- `sw_colorTint` (Vector4, default white/blue)

**Key Features**:
- 3D star positions with depth
- Speed-based streak length
- Configurable vanishing point
- Stars respawn when passing camera
- Brightness based on depth

---

### 15. InkBlot

**ID**: `inkblot`
**Config Prefix**: `ib_`
**Blend Mode**: Alpha

**Description**: Spreading ink blots that expand organically from mouse clicks.

**Files**:
- `InkBlotEffect.cs` - Blot growth simulation
- `InkBlotFactory.cs` - Default configuration
- `Shaders/InkBlotShader.hlsl` - Organic shape rendering
- `UI/InkBlotSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 48)]
struct InkBlot
{
    Vector2 Position;
    float Radius;
    float Age;
    float MaxRadius;
    float GrowthSpeed;
    Vector4 Color;
    float NoiseOffset;
    float Padding1, Padding2, Padding3;
}
```

**Configuration Keys**:
- `ib_maxBlots` (int, 5-30, default 15)
- `ib_maxRadius` (float, 50-300, default 150)
- `ib_growthSpeed` (float, 50-300, default 100)
- `ib_edgeNoise` (float, 0-0.5, default 0.2)
- `ib_fadeSpeed` (float, 0.1-1, default 0.3)
- `ib_colorMode` (int, 0=Black, 1=Color, 2=Rainbow)
- `ib_triggerOnClick` (bool, default true)

**Key Features**:
- Organic edge noise using Perlin noise
- Growth animation
- Fade out after reaching max size
- Multiple color modes
- Click or continuous trigger

---

### 16. Kaleidoscope

**ID**: `kaleidoscope`
**Config Prefix**: `ks_`
**Blend Mode**: Alpha
**Requires Screen Capture**: Yes

**Description**: Kaleidoscope mirror effect reflecting screen content.

**Files**:
- `KaleidoscopeEffect.cs` - Mirror calculation
- `KaleidoscopeFactory.cs` - Default configuration
- `Shaders/KaleidoscopeShader.hlsl` - Mirror reflection shader
- `UI/KaleidoscopeSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 48)]
struct KaleidoscopeConstants
{
    Vector2 ViewportSize;
    Vector2 MousePosition;
    float Time;
    int Segments;
    float Rotation;
    float RotationSpeed;
    float Radius;
    float Zoom;
    float HdrMultiplier;
    float Padding;
}
```

**Configuration Keys**:
- `ks_segments` (int, 4-16, default 8)
- `ks_rotationSpeed` (float, 0-2, default 0.5)
- `ks_radius` (float, 100-500, default 250)
- `ks_zoom` (float, 0.5-3, default 1)
- `ks_colorShift` (float, 0-1, default 0)

**Key Features**:
- Screen content mirroring
- Configurable segment count
- Auto-rotation option
- Zoom control
- Centered on mouse position

---

### 17. Smoke

**ID**: `smoke`
**Config Prefix**: `sm_`
**Blend Mode**: Alpha

**Description**: Wispy smoke particles rising from the mouse cursor.

**Files**:
- `SmokeEffect.cs` - Smoke particle physics
- `SmokeFactory.cs` - Default configuration
- `Shaders/SmokeShader.hlsl` - Soft particle rendering
- `UI/SmokeSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 48)]
struct SmokeParticle
{
    Vector2 Position;
    Vector2 Velocity;
    float Size;
    float Life;
    float MaxLife;
    float Rotation;
    float Opacity;
    float TurbulenceOffset;
    float Padding1, Padding2;
}
```

**Configuration Keys**:
- `sm_particleCount` (int, 50-300, default 100)
- `sm_riseSpeed` (float, 20-100, default 50)
- `sm_spreadSpeed` (float, 10-50, default 20)
- `sm_turbulence` (float, 0-1, default 0.3)
- `sm_particleSize` (float, 20-100, default 50)
- `sm_lifetime` (float, 1-5, default 2)
- `sm_opacity` (float, 0.1-0.8, default 0.4)
- `sm_color` (Vector4, default gray)

**Key Features**:
- Soft, billowy particle shapes
- Rising with turbulence
- Expansion over lifetime
- Fade out with age
- Configurable density and color

---

### 18. Hearts

**ID**: `hearts`
**Config Prefix**: `ht_`
**Blend Mode**: Additive

**Description**: Floating heart particles with pulsing animation.

**Files**:
- `HeartsEffect.cs` - Heart particle management
- `HeartsFactory.cs` - Default configuration
- `Shaders/HeartsShader.hlsl` - Heart shape SDF rendering
- `UI/HeartsSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 32)]
struct Heart
{
    Vector2 Position;
    Vector2 Velocity;
    float Size;
    float Rotation;
    float PulsePhase;
    float Life;
}
```

**Configuration Keys**:
- `ht_heartCount` (int, 10-100, default 30)
- `ht_heartSize` (float, 15-60, default 30)
- `ht_pulseSpeed` (float, 1-5, default 2)
- `ht_pulseAmount` (float, 0.1-0.5, default 0.2)
- `ht_riseSpeed` (float, 20-80, default 40)
- `ht_swayAmount` (float, 10-50, default 20)
- `ht_color` (Vector4, default red/pink)
- `ht_glowIntensity` (float, 0.5-3, default 1.5)

**Key Features**:
- Heart shape using SDF
- Pulsing size animation
- Rising with sway motion
- Glow effect
- Pink/red color options

---

### 19. Runes

**ID**: `runes`
**Config Prefix**: `rn_`
**Blend Mode**: Additive

**Description**: Mystical rune symbols that appear and glow around the cursor.

**Files**:
- `RunesEffect.cs` - Rune spawning and animation
- `RunesFactory.cs` - Default configuration
- `Shaders/RunesShader.hlsl` - Rune symbol rendering
- `UI/RunesSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 32)]
struct Rune
{
    Vector2 Position;
    float Size;
    float Rotation;
    float Age;
    int SymbolIndex;
    float GlowPhase;
    float Padding;
}
```

**Configuration Keys**:
- `rn_runeCount` (int, 5-30, default 12)
- `rn_runeSize` (float, 30-100, default 50)
- `rn_glowIntensity` (float, 0.5-3, default 1.5)
- `rn_pulseSpeed` (float, 0.5-3, default 1)
- `rn_rotationSpeed` (float, 0-1, default 0.2)
- `rn_fadeSpeed` (float, 0.2-2, default 0.5)
- `rn_color` (Vector4, default cyan/purple)

**Key Features**:
- Multiple rune symbol designs
- Pulsing glow animation
- Slow rotation
- Fade in/out lifecycle
- Mystical color options

---

### 20. Bubbles

**ID**: `bubbles`
**Config Prefix**: `bb_`
**Blend Mode**: Alpha

**Description**: Translucent bubbles that float up from the mouse cursor.

**Files**:
- `BubblesEffect.cs` - Bubble physics
- `BubblesFactory.cs` - Default configuration
- `Shaders/BubblesShader.hlsl` - Bubble with reflection/refraction
- `UI/BubblesSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 32)]
struct Bubble
{
    Vector2 Position;
    Vector2 Velocity;
    float Size;
    float WobblePhase;
    float Life;
    float Opacity;
}
```

**Configuration Keys**:
- `bb_bubbleCount` (int, 10-100, default 40)
- `bb_minSize` (float, 10-30, default 15)
- `bb_maxSize` (float, 30-80, default 50)
- `bb_riseSpeed` (float, 20-100, default 50)
- `bb_wobbleAmount` (float, 0-1, default 0.3)
- `bb_reflectionIntensity` (float, 0.2-1, default 0.5)
- `bb_lifetime` (float, 2-8, default 4)
- `bb_popProbability` (float, 0-0.3, default 0.1)

**Key Features**:
- Realistic bubble appearance with highlight
- Wobble animation
- Size variation
- Pop effect option
- Rising with slight drift

---

### 21. Circuit

**ID**: `circuit`
**Config Prefix**: `cc_`
**Blend Mode**: Additive

**Description**: Circuit board patterns that light up around the cursor.

**Files**:
- `CircuitEffect.cs` - Circuit path generation
- `CircuitFactory.cs` - Default configuration
- `Shaders/CircuitShader.hlsl` - Line and node rendering
- `UI/CircuitSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 48)]
struct CircuitConstants
{
    Vector2 ViewportSize;
    Vector2 MousePosition;
    float Time;
    float GridSize;
    float LineThickness;
    float GlowIntensity;
    float PulseSpeed;
    float TraceSpeed;
    float EffectRadius;
    float HdrMultiplier;
}
```

**Configuration Keys**:
- `cc_gridSize` (float, 20-60, default 30)
- `cc_lineThickness` (float, 1-5, default 2)
- `cc_glowIntensity` (float, 0.5-3, default 1.5)
- `cc_pulseSpeed` (float, 1-5, default 2)
- `cc_traceSpeed` (float, 50-300, default 150)
- `cc_effectRadius` (float, 100-500, default 300)
- `cc_color` (Vector4, default cyan/green)

**Key Features**:
- Procedural circuit pattern
- Energy pulse traveling along paths
- Node connection points
- Distance-based fade
- Tech-style color options

---

### 22. Hologram

**ID**: `hologram`
**Config Prefix**: `hg_`
**Blend Mode**: Additive
**Requires Screen Capture**: Yes

**Description**: Holographic display effect with scan lines and chromatic aberration.

**Files**:
- `HologramEffect.cs` - Hologram rendering
- `HologramFactory.cs` - Default configuration
- `Shaders/HologramShader.hlsl` - Hologram post-processing
- `UI/HologramSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 64)]
struct HologramConstants
{
    Vector2 ViewportSize;
    Vector2 MousePosition;
    float Time;
    float ScanLineIntensity;
    float ScanLineSpeed;
    float ChromaticAberration;
    float FlickerIntensity;
    float GlowIntensity;
    float EffectRadius;
    float HdrMultiplier;
    Vector4 TintColor;
}
```

**Configuration Keys**:
- `hg_scanLineIntensity` (float, 0-1, default 0.3)
- `hg_scanLineSpeed` (float, 1-10, default 5)
- `hg_chromaticAberration` (float, 0-20, default 5)
- `hg_flickerIntensity` (float, 0-0.5, default 0.1)
- `hg_glowIntensity` (float, 0.5-3, default 1.5)
- `hg_effectRadius` (float, 100-500, default 250)
- `hg_tintColor` (Vector4, default cyan)

**Key Features**:
- Moving scan lines
- RGB separation effect
- Random flicker
- Hologram tint color
- Edge glow effect

---

### 23. GravityWell

**ID**: `gravitywell`
**Config Prefix**: `gw_`
**Blend Mode**: Additive

**Description**: Particles orbiting and being pulled toward the mouse cursor.

**Files**:
- `GravityWellEffect.cs` - N-body physics simulation
- `GravityWellFactory.cs` - Default configuration
- `Shaders/GravityWellShader.hlsl` - Particle trail rendering
- `UI/GravityWellSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 32)]
struct GravityParticle
{
    Vector2 Position;
    Vector2 Velocity;
    float Mass;
    float Size;
    float TrailLength;
    float Brightness;
}
```

**Configuration Keys**:
- `gw_particleCount` (int, 50-500, default 200)
- `gw_gravityStrength` (float, 1000-10000, default 5000)
- `gw_particleSize` (float, 2-10, default 4)
- `gw_trailLength` (float, 5-50, default 20)
- `gw_damping` (float, 0.95-1, default 0.99)
- `gw_initialSpeed` (float, 50-300, default 150)
- `gw_color` (Vector4, default white/blue)

**Key Features**:
- Realistic gravity physics
- Orbital motion
- Motion trails
- Energy conservation with damping
- Particles can escape or orbit

---

### 24. DandelionSeeds

**ID**: `dandelionseeds`
**Config Prefix**: `ds_`
**Blend Mode**: Alpha

**Description**: Fluffy dandelion seeds floating gently around the mouse.

**Files**:
- `DandelionSeedsEffect.cs` - Seed physics
- `DandelionSeedsFactory.cs` - Default configuration
- `Shaders/DandelionSeedsShader.hlsl` - Fluffy seed rendering
- `UI/DandelionSeedsSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 32)]
struct DandelionSeed
{
    Vector2 Position;
    Vector2 Velocity;
    float Rotation;
    float FloatPhase;
    float Size;
    float Opacity;
}
```

**Configuration Keys**:
- `ds_seedCount` (int, 20-150, default 60)
- `ds_seedSize` (float, 15-50, default 25)
- `ds_floatSpeed` (float, 5-30, default 15)
- `ds_driftAmount` (float, 10-50, default 25)
- `ds_rotationSpeed` (float, 0.1-1, default 0.3)
- `ds_puffiness` (float, 0.5-1.5, default 1)

**Key Features**:
- Fluffy seed head rendering
- Gentle floating motion
- Air current drift
- Slow rotation
- Lightweight, airy feel

---

### 25. Spotlight

**ID**: `spotlight`
**Config Prefix**: `sl_`
**Blend Mode**: Multiply
**Requires Screen Capture**: Yes

**Description**: Spotlight or flashlight effect that darkens everything except around the cursor.

**Files**:
- `SpotlightEffect.cs` - Light cone calculation
- `SpotlightFactory.cs` - Default configuration
- `Shaders/SpotlightShader.hlsl` - Vignette/spotlight shader
- `UI/SpotlightSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 48)]
struct SpotlightConstants
{
    Vector2 ViewportSize;
    Vector2 MousePosition;
    float InnerRadius;
    float OuterRadius;
    float DarknessLevel;
    float EdgeSoftness;
    float Brightness;
    float HdrMultiplier;
    float Padding1, Padding2;
}
```

**Configuration Keys**:
- `sl_innerRadius` (float, 50-300, default 150)
- `sl_outerRadius` (float, 150-500, default 300)
- `sl_darknessLevel` (float, 0.5-1, default 0.9)
- `sl_edgeSoftness` (float, 0.1-1, default 0.5)
- `sl_brightness` (float, 0.8-1.5, default 1)
- `sl_followSpeed` (float, 0.1-1, default 0.5) - Smoothing

**Key Features**:
- Screen darkening outside spotlight
- Soft edge transition
- Configurable radius
- Brightness boost in center
- Smooth following option

---

### 26. MagneticField

**ID**: `magneticfield`
**Config Prefix**: `mf_`
**Blend Mode**: Additive

**Description**: Magnetic field lines visualization emanating from the cursor.

**Files**:
- `MagneticFieldEffect.cs` - Field line calculation
- `MagneticFieldFactory.cs` - Default configuration
- `Shaders/MagneticFieldShader.hlsl` - Field line rendering
- `UI/MagneticFieldSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 48)]
struct MagneticFieldConstants
{
    Vector2 ViewportSize;
    Vector2 MousePosition;
    float Time;
    int FieldLineCount;
    float LineLength;
    float LineThickness;
    float AnimationSpeed;
    float GlowIntensity;
    float HdrMultiplier;
    float Padding;
}
```

**Configuration Keys**:
- `mf_fieldLineCount` (int, 8-32, default 16)
- `mf_lineLength` (float, 50-300, default 150)
- `mf_lineThickness` (float, 1-5, default 2)
- `mf_animationSpeed` (float, 0.5-3, default 1)
- `mf_glowIntensity` (float, 0.5-3, default 1.5)
- `mf_color` (Vector4, default cyan/purple)

**Key Features**:
- Dipole field line pattern
- Animated flow along lines
- Curved field visualization
- Pulsing intensity
- North/South pole indication

---

### 27. Rain

**ID**: `rain`
**Config Prefix**: `rn_`
**Blend Mode**: Alpha

**Description**: Rain drops falling with splashes around the mouse cursor.

**Files**:
- `RainEffect.cs` - Raindrop physics
- `RainFactory.cs` - Default configuration
- `Shaders/RainShader.hlsl` - Drop and splash rendering
- `UI/RainSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 32)]
struct Raindrop
{
    Vector2 Position;
    Vector2 Velocity;
    float Length;
    float Thickness;
    float Life;
    float Brightness;
}

[StructLayout(LayoutKind.Sequential, Size = 24)]
struct Splash
{
    Vector2 Position;
    float Radius;
    float Age;
    float MaxRadius;
    float Padding;
}
```

**Configuration Keys**:
- `rn_dropCount` (int, 100-1000, default 400)
- `rn_fallSpeed` (float, 300-1000, default 600)
- `rn_dropLength` (float, 10-40, default 20)
- `rn_windAngle` (float, -30 to 30, default 0)
- `rn_splashEnabled` (bool, default true)
- `rn_intensity` (float, 0.5-2, default 1)
- `rn_spawnRadius` (float, 100-500, default 300)

**Key Features**:
- Fast falling drops
- Wind angle effect
- Splash rings on impact
- Variable drop sizes
- Intensity control

---

### 28. Nebula

**ID**: `nebula`
**Config Prefix**: `nb_`
**Blend Mode**: Additive

**Description**: Cosmic nebula clouds with swirling gas and embedded stars.

**Files**:
- `NebulaEffect.cs` - Nebula cloud animation
- `NebulaFactory.cs` - Default configuration with color presets
- `Shaders/NebulaShader.hlsl` - Volumetric cloud rendering
- `UI/NebulaSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 96)]
struct NebulaConstants
{
    Vector2 ViewportSize;
    Vector2 MousePosition;
    float Time;
    float CloudDensity;
    float CloudScale;
    float SwirlingSpeed;
    float Turbulence;
    float GlowIntensity;
    float EffectRadius;
    float HdrMultiplier;
    Vector4 PrimaryColor;
    Vector4 SecondaryColor;
    Vector4 AccentColor;
    int StarCount;
    float StarBrightness;
    float Padding1, Padding2;
}
```

**Configuration Keys**:
- `nb_cloudDensity` (float, 0.3-1, default 0.6)
- `nb_cloudScale` (float, 0.5-3, default 1)
- `nb_swirlingSpeed` (float, 0.1-1, default 0.3)
- `nb_turbulence` (float, 0.1-1, default 0.5)
- `nb_glowIntensity` (float, 0.5-3, default 1.5)
- `nb_effectRadius` (float, 150-500, default 300)
- `nb_starCount` (int, 0-100, default 30)
- `nb_starBrightness` (float, 0.5-2, default 1)
- `nb_primaryColor`, `nb_secondaryColor`, `nb_accentColor` (Vector4)

**Color Presets**:
- Orion (Blue/Purple/Pink)
- Carina (Orange/Red/Yellow)
- Eagle (Green/Cyan/Blue)
- Horsehead (Dark Red/Orange)

**Key Features**:
- Multi-layer cloud rendering using FBM noise
- Swirling animation
- Embedded star particles
- Three-color gradient system
- Turbulence distortion

---

### 29. CrystalGrowth

**ID**: `crystalgrowth`
**Config Prefix**: `cg_`
**Blend Mode**: Additive

**Description**: Growing crystal formations that emerge from mouse clicks.

**Files**:
- `CrystalGrowthEffect.cs` - Crystal growth simulation
- `CrystalGrowthFactory.cs` - Default configuration
- `Shaders/CrystalGrowthShader.hlsl` - Faceted crystal rendering
- `UI/CrystalGrowthSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 64)]
struct Crystal
{
    Vector2 Position;
    Vector2 Direction;
    float Length;
    float TargetLength;
    float Width;
    float Age;
    float GrowthSpeed;
    int BranchCount;
    Vector4 Color;
    float Facets;
    float Padding1, Padding2, Padding3;
}
```

**Configuration Keys**:
- `cg_maxCrystals` (int, 5-50, default 20)
- `cg_growthSpeed` (float, 50-300, default 150)
- `cg_maxLength` (float, 50-300, default 150)
- `cg_branchProbability` (float, 0-0.5, default 0.2)
- `cg_facetCount` (int, 4-8, default 6)
- `cg_glowIntensity` (float, 0.5-3, default 1.5)
- `cg_colorMode` (int, 0=Ice, 1=Amethyst, 2=Emerald, 3=Ruby)

**Key Features**:
- Animated growth from point
- Branching crystal structures
- Faceted surface rendering
- Inner glow effect
- Multiple crystal color types

---

### 30. PaintSplatter

**ID**: `paintsplatter`
**Config Prefix**: `ps_`
**Blend Mode**: Alpha

**Description**: Artistic paint splatters that appear on mouse movement or clicks.

**Files**:
- `PaintSplatterEffect.cs` - Splatter generation
- `PaintSplatterFactory.cs` - Default configuration
- `Shaders/PaintSplatterShader.hlsl` - Organic splatter shapes
- `UI/PaintSplatterSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 48)]
struct Splatter
{
    Vector2 Position;
    float Size;
    float Rotation;
    float Age;
    float NoiseOffset;
    Vector4 Color;
    int ShapeType;
    float Drips;
    float Padding1, Padding2;
}
```

**Configuration Keys**:
- `ps_maxSplatters` (int, 10-100, default 50)
- `ps_splatterSize` (float, 30-200, default 80)
- `ps_sizeVariation` (float, 0.3-1, default 0.5)
- `ps_edgeNoise` (float, 0.1-0.5, default 0.3)
- `ps_dripAmount` (float, 0-1, default 0.3)
- `ps_fadeSpeed` (float, 0.05-0.5, default 0.1)
- `ps_colorPalette` (int, 0=Primary, 1=Pastel, 2=Neon, 3=Earth)

**Key Features**:
- Organic splatter shapes using noise
- Paint drip effects
- Multiple color palettes
- Size variation
- Layered splatters

---

### 31. EmojiRain

**ID**: `emojirain`
**Config Prefix**: `er_`
**Blend Mode**: Alpha

**Description**: Various emoji characters falling from the mouse cursor.

**Files**:
- `EmojiRainEffect.cs` - Emoji particle system
- `EmojiRainFactory.cs` - Default configuration with emoji sets
- `Shaders/EmojiRainShader.hlsl` - Emoji texture rendering
- `UI/EmojiRainSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 32)]
struct EmojiParticle
{
    Vector2 Position;
    Vector2 Velocity;
    float Size;
    float Rotation;
    int EmojiIndex;
    float Life;
}
```

**Configuration Keys**:
- `er_emojiCount` (int, 20-200, default 80)
- `er_emojiSize` (float, 20-60, default 35)
- `er_fallSpeed` (float, 50-200, default 100)
- `er_rotationSpeed` (float, 0-3, default 1)
- `er_swayAmount` (float, 10-50, default 25)
- `er_emojiSet` (int, 0=Happy, 1=Love, 2=Party, 3=Nature, 4=Random)

**Emoji Sets**:
- Happy: Smileys and faces
- Love: Hearts and romantic
- Party: Celebration emojis
- Nature: Animals and plants
- Random: Mix of all

**Key Features**:
- Multiple emoji character options
- Rotation while falling
- Sway motion
- Emoji set selection
- GPU-rendered text

---

### 32. PixelExplosion

**ID**: `pixelexplosion`
**Config Prefix**: `pe_`
**Blend Mode**: Additive

**Description**: Retro-style pixel explosions bursting from mouse clicks.

**Files**:
- `PixelExplosionEffect.cs` - Pixel burst system
- `PixelExplosionFactory.cs` - Default configuration
- `Shaders/PixelExplosionShader.hlsl` - Square pixel rendering
- `UI/PixelExplosionSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 32)]
struct Pixel
{
    Vector2 Position;
    Vector2 Velocity;
    float Size;
    float Life;
    Vector4 Color;
}
```

**Configuration Keys**:
- `pe_burstCount` (int, 20-200, default 80)
- `pe_pixelSize` (float, 4-16, default 8)
- `pe_explosionSpeed` (float, 100-500, default 300)
- `pe_gravity` (float, 0-500, default 200)
- `pe_lifetime` (float, 0.5-3, default 1.5)
- `pe_colorMode` (int, 0=Fire, 1=Ice, 2=Electric, 3=Rainbow)

**Key Features**:
- Square pixel shapes (retro aesthetic)
- Burst on click trigger
- Gravity-affected falling
- Multiple color schemes
- Size consistency for retro look

---

### 33. DNAHelix

**ID**: `dnahelix`
**Config Prefix**: `dh_`
**Blend Mode**: Additive

**Description**: Rotating DNA double helix structure following the mouse.

**Files**:
- `DNAHelixEffect.cs` - Helix geometry generation
- `DNAHelixFactory.cs` - Default configuration
- `Shaders/DNAHelixShader.hlsl` - 3D helix rendering
- `UI/DNAHelixSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 64)]
struct DNAConstants
{
    Vector2 ViewportSize;
    Vector2 MousePosition;
    float Time;
    float HelixRadius;
    float HelixPitch;
    float RotationSpeed;
    float NodeSize;
    float ConnectionThickness;
    float GlowIntensity;
    float HdrMultiplier;
    int BasePairCount;
    float HelixLength;
    float Padding1, Padding2;
}
```

**Configuration Keys**:
- `dh_helixRadius` (float, 30-100, default 50)
- `dh_helixPitch` (float, 20-80, default 40)
- `dh_rotationSpeed` (float, 0.5-3, default 1)
- `dh_nodeSize` (float, 5-20, default 10)
- `dh_connectionThickness` (float, 2-8, default 4)
- `dh_glowIntensity` (float, 0.5-3, default 1.5)
- `dh_basePairCount` (int, 10-40, default 20)
- `dh_helixLength` (float, 100-400, default 250)

**Key Features**:
- Double helix structure
- Base pair connections (A-T, G-C colors)
- 3D rotation effect
- Backbone strands
- Scientific accuracy in structure

---

### 34. Spirograph

**ID**: `spirograph`
**Config Prefix**: `sp_`
**Blend Mode**: Additive

**Description**: Classic spirograph geometric patterns that draw from mouse movement.

**Files**:
- `SpirographEffect.cs` - Mathematical curve generation
- `SpirographFactory.cs` - Default configuration
- `Shaders/SpirographShader.hlsl` - Curve rendering with glow
- `UI/SpirographSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 128)]
struct SpirographConstants
{
    Vector2 ViewportSize;
    Vector2 MousePosition;
    float Time;
    float InnerRadius;
    float OuterRadius;
    float PenOffset;
    int Petals;
    float RotationSpeed;
    float TrailFade;
    float LineThickness;
    float GlowIntensity;
    int ColorMode;
    Vector4 PrimaryColor;
    Vector4 SecondaryColor;
    Vector4 TertiaryColor;
    float RainbowSpeed;
    float HdrMultiplier;
    float Padding1, Padding2;
}
```

**Configuration Keys**:
- `sp_innerRadius` (float, 20-100, default 40)
- `sp_outerRadius` (float, 50-200, default 100)
- `sp_penOffset` (float, 10-80, default 30)
- `sp_petals` (int, 3-24, default 7)
- `sp_rotationSpeed` (float, 0.5-5, default 2)
- `sp_trailFade` (float, 0.01-0.5, default 0.1)
- `sp_lineThickness` (float, 1-5, default 2)
- `sp_glowIntensity` (float, 0.5-3, default 1.5)
- `sp_colorMode` (int, 0=Rainbow, 1=Fixed, 2=Gradient)

**Mathematical Formula**:
```
// Hypotrochoid equations
x = (R-r)*cos(t) + d*cos((R-r)/r * t)
y = (R-r)*sin(t) - d*sin((R-r)/r * t)
// Where R=outer radius, r=inner radius, d=pen offset
```

**Key Features**:
- True spirograph mathematics
- Multiple petal configurations
- Trail persistence
- Three color modes
- Smooth curve rendering

---

### 35. CometTrail

**ID**: `comettrail`
**Config Prefix**: `ct_`
**Blend Mode**: Additive

**Description**: Blazing comet with fiery tail following the mouse cursor.

**Files**:
- `CometTrailEffect.cs` - Comet and spark particle system
- `CometTrailFactory.cs` - Default configuration
- `Shaders/CometTrailShader.hlsl` - Fire gradient and spark rendering
- `UI/CometTrailSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 96)]
struct CometConstants
{
    Vector2 ViewportSize;
    Vector2 MousePosition;
    float Time;
    float HeadSize;
    float TrailWidth;
    float GlowIntensity;
    float ColorTemperature;
    float FadeSpeed;
    float SmoothingFactor;
    float HdrMultiplier;
    int TrailPointCount;
    int SparkCount;
    float SparkSize;
    float Padding;
}

[StructLayout(LayoutKind.Sequential, Size = 32)]
struct TrailPoint
{
    Vector2 Position;
    float Age;
    float Width;
    float Brightness;
    float Padding1, Padding2, Padding3;
}

[StructLayout(LayoutKind.Sequential, Size = 32)]
struct Spark
{
    Vector2 Position;
    Vector2 Velocity;
    float Size;
    float Life;
    float Brightness;
    float Padding;
}
```

**Configuration Keys**:
- `ct_maxTrailPoints` (int, 100-500, default 250)
- `ct_trailSpacing` (float, 3-15, default 6)
- `ct_headSize` (float, 10-50, default 20)
- `ct_trailWidth` (float, 3-20, default 8)
- `ct_glowIntensity` (float, 0.5-4, default 2)
- `ct_sparkCount` (int, 0-20, default 5)
- `ct_sparkSize` (float, 1-8, default 3)
- `ct_colorTemperature` (float, 0-1, default 0.7)
- `ct_fadeSpeed` (float, 0.1-3, default 1)
- `ct_smoothingFactor` (float, 0-0.9, default 0.2)

**Key Features**:
- Bright white-hot comet head
- Fire gradient tail (white→yellow→orange→red)
- Spark particle debris
- Turbulence noise in flame
- Speed-based trail length

---

### 36. Shockwave

**ID**: `shockwave`
**Config Prefix**: `sw_`
**Blend Mode**: Additive (with optional screen distortion)
**Requires Screen Capture**: When distortion enabled

**Description**: Expanding circular shockwave rings from mouse clicks.

**Files**:
- `ShockwaveEffect.cs` - Ring expansion system
- `ShockwaveFactory.cs` - Default configuration
- `Shaders/ShockwaveShader.hlsl` - Ring and distortion rendering
- `UI/ShockwaveSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 64)]
struct ShockwaveConstants
{
    Vector2 ViewportSize;
    float Time;
    float RingThickness;
    float GlowIntensity;
    float DistortionStrength;
    float HdrMultiplier;
    int MaxShockwaves;
    Vector4 Color;
    float Padding1, Padding2, Padding3, Padding4;
}

[StructLayout(LayoutKind.Sequential, Size = 32)]
struct Shockwave
{
    Vector2 Position;
    float CurrentRadius;
    float MaxRadius;
    float Age;
    float Lifespan;
    float ExpansionSpeed;
    float Padding;
}
```

**Configuration Keys**:
- `sw_maxShockwaves` (int, 1-100, default 20)
- `sw_ringThickness` (float, 5-50, default 15)
- `sw_expansionSpeed` (float, 100-2000, default 500)
- `sw_maxRadius` (float, 100-2000, default 500)
- `sw_lifespan` (float, 0.5-10, default 2)
- `sw_glowIntensity` (float, 0.1-5, default 1.5)
- `sw_distortionStrength` (float, 0-100, default 0)
- `sw_leftClickEnabled` (bool, default true)
- `sw_rightClickEnabled` (bool, default false)
- `sw_mouseMoveEnabled` (bool, default false)
- `sw_colorPreset` (int, 0=Blue, 1=Red, 2=White, 3=Custom)

**Key Features**:
- Multiple concurrent rings
- Age-based fade out
- Optional screen distortion
- Multiple trigger modes
- Color presets

---

### 37. FireTrail

**ID**: `firetrail`
**Config Prefix**: `ft_`
**Blend Mode**: Additive

**Description**: Realistic fire and flames trailing behind the mouse cursor.

**Files**:
- `FireTrailEffect.cs` - Fire particle system with multiple types
- `FireTrailFactory.cs` - Default configuration
- `Shaders/FireTrailShader.hlsl` - Fire, smoke, and ember rendering
- `UI/FireTrailSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 80)]
struct FireConstants
{
    Vector2 ViewportSize;
    Vector2 MousePosition;
    float Time;
    float Intensity;
    float FlameHeight;
    float FlameWidth;
    float TurbulenceAmount;
    float FlickerSpeed;
    float GlowIntensity;
    float ColorSaturation;
    int FireStyle;
    float SmokeAmount;
    float EmberAmount;
    float HdrMultiplier;
    float ParticleLifetime;
    float Padding1, Padding2, Padding3;
}

[StructLayout(LayoutKind.Sequential, Size = 48)]
struct FireParticle
{
    Vector2 Position;
    Vector2 Velocity;
    float Size;
    float Life;
    float MaxLife;
    float Temperature;  // 1.0 = hot, 0.0 = cool
    float Rotation;
    int ParticleType;   // 0=Fire, 1=Smoke, 2=Ember
    float TurbulenceOffset;
    float Padding;
}
```

**Configuration Keys**:
- `ft_enabled` (bool, default true)
- `ft_intensity` (float, 0.5-3, default 1)
- `ft_particleLifetime` (float, 0.5-3, default 1.5)
- `ft_flameHeight` (float, 50-200, default 100)
- `ft_flameWidth` (float, 20-80, default 40)
- `ft_turbulenceAmount` (float, 0.1-1, default 0.5)
- `ft_flickerSpeed` (float, 1-10, default 5)
- `ft_glowIntensity` (float, 0.5-3, default 1.5)
- `ft_colorSaturation` (float, 0.5-1.5, default 1)
- `ft_fireStyle` (int, 0=Campfire, 1=Torch, 2=Inferno)
- `ft_smokeAmount` (float, 0-1, default 0.3)
- `ft_emberAmount` (float, 0-1, default 0.2)

**Fire Styles**:
- Campfire: Balanced, natural flames
- Torch: More vertical, focused
- Inferno: Intense, chaotic, bright

**Key Features**:
- Temperature-based color gradient
- Three particle types (fire, smoke, ember)
- Turbulence using FBM noise
- Flicker animation
- Multiple fire styles

---

### 38. FlowerBloom

**ID**: `flowerbloom`
**Config Prefix**: `fb_`
**Blend Mode**: Alpha

**Description**: Blooming flowers that grow and unfurl from mouse clicks.

**Files**:
- `FlowerBloomEffect.cs` - Flower growth animation
- `FlowerBloomFactory.cs` - Default configuration with presets
- `Shaders/FlowerBloomShader.hlsl` - Procedural flower rendering
- `UI/FlowerBloomSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 32)]
struct FlowerConstants
{
    Vector2 ViewportSize;
    float Time;
    float HdrMultiplier;
    float Padding1, Padding2, Padding3, Padding4;
}

[StructLayout(LayoutKind.Sequential, Size = 96)]
struct FlowerInstance
{
    Vector2 Position;
    float Size;
    float BloomProgress;     // 0-1
    float Age;
    float Lifetime;
    int FlowerType;
    int PetalCount;
    Vector4 PetalColor;
    Vector4 CenterColor;
    float Rotation;
    int ShowStem;
    float SizeVariation;
    float Padding;
}
```

**Configuration Keys**:
- `fb_maxFlowers` (int, 5-100, default 30)
- `fb_flowerSize` (float, 30-200, default 80)
- `fb_petalCount` (int, 3-12, default 5)
- `fb_bloomDuration` (float, 0.3-5, default 1)
- `fb_lifetime` (float, 2-15, default 5)
- `fb_fadeOutDuration` (float, 0.3-3, default 1)
- `fb_flowerType` (int, 0=Rose, 1=Daisy, 2=Lotus, 3=CherryBlossom)
- `fb_colorPalette` (int, 0=Spring, 1=Summer, 2=Tropical, 3=Pastel)
- `fb_showStem` (bool, default false)
- `fb_sizeVariation` (float, 0-0.5, default 0.2)
- `fb_leftClickSpawn` (bool, default true)
- `fb_rightClickSpawn` (bool, default false)
- `fb_continuousSpawn` (bool, default false)

**Flower Types**:
- Rose: Heart-shaped curved petals
- Daisy: Elongated narrow petals
- Lotus: Wide rounded petals
- Cherry Blossom: Notched petals

**Color Palettes**:
- Spring: Pinks, light yellows, lavenders
- Summer: Reds, oranges, bright blues
- Tropical: Magentas, purples, oranges
- Pastel: Soft muted colors

**Key Features**:
- Animated petal unfurling
- Procedural petal shapes using SDF
- Multiple flower types
- Center detail with stamen
- Optional stem rendering

---

### 39. LightningStorm

**ID**: `lightningstorm`
**Config Prefix**: `ls_`
**Blend Mode**: Additive

**Description**: Dramatic lightning bolts with branching and electric effects.

**Files**:
- `LightningStormEffect.cs` - Lightning bolt generation
- `LightningStormFactory.cs` - Default configuration
- `Shaders/LightningStormShader.hlsl` - Jagged bolt rendering
- `UI/LightningStormSettingsControl.xaml/.cs` - Settings UI

**GPU Structures**:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 112)]
struct LightningConstants
{
    Vector2 ViewportSize;
    Vector2 MousePosition;
    float Time;
    float BoltThickness;
    float GlowIntensity;
    float FlashIntensity;
    float JaggedAmount;
    int MaxBranches;
    float BranchProbability;
    float AfterimageStrength;
    float HdrMultiplier;
    int ColorPreset;
    Vector4 CustomColor;
    float FlickerSpeed;
    float MinStrikeDistance;
    float MaxStrikeDistance;
    int StrikeFromCursor;
    float SparkCount;
    float SparkLifetime;
    float SparkSpeed;
    float Padding;
}

[StructLayout(LayoutKind.Sequential, Size = 64)]
struct LightningBolt
{
    Vector2 StartPosition;
    Vector2 EndPosition;
    float Thickness;
    float Brightness;
    float Age;
    float Lifetime;
    int SegmentCount;
    int BranchCount;
    float JaggedSeed;
    float Padding;
    Vector4 Color;
}

[StructLayout(LayoutKind.Sequential, Size = 32)]
struct ImpactSpark
{
    Vector2 Position;
    Vector2 Velocity;
    float Size;
    float Life;
    float Brightness;
    float Padding;
}
```

**Configuration Keys**:
- `ls_enabled` (bool, default true)
- `ls_minBolts` (int, 1-5, default 1)
- `ls_maxBolts` (int, 1-10, default 3)
- `ls_boltThickness` (float, 1-8, default 3)
- `ls_glowIntensity` (float, 0.5-5, default 2)
- `ls_flashIntensity` (float, 0-1, default 0.2)
- `ls_jaggedAmount` (float, 5-30, default 15)
- `ls_branchProbability` (float, 0-0.5, default 0.2)
- `ls_maxBranches` (int, 0-5, default 2)
- `ls_afterimageStrength` (float, 0-1, default 0.3)
- `ls_strikeFromCursor` (bool, default true)
- `ls_minStrikeDistance` (float, 100-300, default 150)
- `ls_maxStrikeDistance` (float, 200-600, default 400)
- `ls_colorPreset` (int, 0=WhiteBlue, 1=Purple, 2=Green, 3=Custom)
- `ls_clickTrigger` (bool, default true)
- `ls_randomTrigger` (bool, default false)
- `ls_randomInterval` (float, 0.5-5, default 2)
- `ls_sparkEnabled` (bool, default true)
- `ls_sparkCount` (int, 5-30, default 15)
- `ls_sparkLifetime` (float, 0.2-1, default 0.5)
- `ls_sparkSpeed` (float, 100-500, default 300)

**Color Presets**:
- White/Blue: Classic lightning
- Purple: Electric/magical
- Green: Matrix/cyber
- Custom: User-defined

**Key Features**:
- Jagged bolt paths using noise
- Branching from main bolt
- Screen flash on strike
- Afterimage/persistence trail
- Impact spark particles
- Multiple trigger modes

---

## Troubleshooting Common Issues

### NullReferenceException in Settings Control

**Symptom**: Exception on slider ValueChanged during initialization

**Cause**: Slider events fire during InitializeComponent() before effect is assigned

**Fix**: Initialize `_isLoading = true` at field declaration:
```csharp
private bool _isLoading = true;  // NOT in constructor
```

### Black Screen / No Effect Visible

**Causes & Fixes**:
1. **Shader not embedded**: Check `.csproj` has `<EmbeddedResource Include="Shaders\*.hlsl" />`
2. **Blend mode wrong**: Try different blend modes (Alpha, Additive)
3. **Buffer size mismatch**: Ensure C# struct size matches HLSL cbuffer
4. **Missing texture binding**: Check t0/t1 bindings

### GPU Structure Alignment Issues

**Symptom**: Garbled or offset values in shader

**Fix**: Ensure 16-byte alignment with padding:
```csharp
[StructLayout(LayoutKind.Sequential, Size = 32)]  // Size multiple of 16
struct MyStruct
{
    Vector2 A;      // 8 bytes
    float B;        // 4 bytes
    float Padding;  // 4 bytes to reach 16
    Vector4 C;      // 16 bytes
}                   // Total: 32 bytes
```

### Settings Not Persisting

**Check**:
1. Configuration key prefix is correct
2. Both property AND Configuration.Set() are called
3. Keys match between Load and Save

### Effect Not Loading

**Check**:
1. Factory class implements `IEffectFactory`
2. Assembly is in plugins folder
3. No runtime exceptions in factory constructor
4. Dependencies are present

---

## Performance Guidelines

1. **Particle Count**: Keep under 2000 particles for smooth performance
2. **Screen Capture**: Only use when necessary (distortion effects)
3. **Shader Complexity**: Avoid excessive loops in pixel shader
4. **Buffer Updates**: Use dynamic buffers with Map/Unmap
5. **Early Exit**: Skip rendering when effect is disabled or no particles active

---

## Version History

- **v1.0.38**: Base version before new plugins
- **v1.0.39+**: Added 45 new effect plugins

---

*Document generated for MouseEffects debugging and development reference.*

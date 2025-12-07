# Plugins Reference

This document provides detailed information about each built-in effect plugin.

## Table of Contents

- [Particle Trail](#particle-trail)
- [Laser Work](#laser-work)
- [Screen Distortion](#screen-distortion)
- [Color Blindness](#color-blindness)
- [Color Blindness NG](#color-blindness-ng)
- [Radial Dithering](#radial-dithering)
- [Tile Vibration](#tile-vibration)
- [Water Ripple](#water-ripple)
- [Zoom](#zoom)
- [Firework](#firework)
- [Space Invaders](#space-invaders)

---

## Particle Trail

**ID**: `particle-trail`
**Screen Capture**: No

Creates colorful particle trails that follow your mouse cursor with realistic physics.

### Features

- GPU instanced rendering for up to 1000 particles
- Physics simulation with gravity and drag
- Smooth color interpolation over particle lifetime
- Click triggers burst of 20 particles
- Movement spawns particles based on emission rate

### Settings

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `emissionRate` | float | 10-500 | 100 | Particles spawned per second while moving |
| `particleLifetime` | float | 0.5-5.0 | 1.5 | How long each particle lives (seconds) |
| `particleSize` | float | 2-32 | 8 | Base particle size in pixels |
| `spreadAngle` | float | 0-π | 0.5 | Angular spread of particles (radians) |
| `initialSpeed` | float | 10-200 | 50 | Initial velocity of particles |
| `startColor` | Color4 | - | Orange | Color when particle spawns |
| `endColor` | Color4 | - | Pink | Color when particle dies |

### Rendering

- **Blend Mode**: Additive (creates glow effect)
- **Shader**: Point sprite rendering with size attenuation

---

## Laser Work

**ID**: `laser-work`
**Screen Capture**: No

Shoots glowing lasers from your cursor in directions relative to movement.

### Features

- Up to 500 simultaneous lasers
- Rainbow color cycling mode
- Collision detection between lasers
- Explosion system spawns new lasers on collision
- Directional emission (forward, backward, left, right)
- Auto-shrinking lasers over lifetime

### Settings

#### Emission

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `lasersPerSecond` | float | 1-100 | 20 | Lasers emitted per second |

#### Size

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `minLaserLength` | float | 10-200 | 30 | Minimum laser length (px) |
| `maxLaserLength` | float | 10-200 | 70 | Maximum laser length (px) |
| `minLaserWidth` | float | 1-20 | 2 | Minimum laser width (px) |
| `maxLaserWidth` | float | 1-20 | 6 | Maximum laser width (px) |
| `autoShrink` | bool | - | false | Shrink to 1px over lifetime |

#### Physics

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `laserSpeed` | float | 50-1000 | 400 | Movement speed (px/s) |
| `laserLifespan` | float | 0.5-10 | 3 | Laser lifetime (seconds) |

#### Visual

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `minAlpha` | float | 0-1 | 0.1 | Opacity at end of life |
| `maxAlpha` | float | 0-1 | 1.0 | Opacity at start of life |
| `glowIntensity` | float | 0-2 | 0.5 | Glow effect strength |
| `laserColor` | Color4 | - | Red | Base laser color |

#### Rainbow Mode

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `rainbowMode` | bool | - | false | Enable color cycling |
| `rainbowSpeed` | float | 0.1-5 | 1.0 | Color cycle speed |

#### Directions

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `shootForward` | bool | true | Emit in movement direction |
| `shootBackward` | bool | true | Emit opposite to movement |
| `shootLeft` | bool | true | Emit perpendicular left |
| `shootRight` | bool | true | Emit perpendicular right |

#### Collisions

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `enableCollisionExplosion` | bool | - | false | Enable laser collisions |
| `explosionLaserCount` | int | 2-24 | 8 | Lasers spawned per explosion |
| `explosionLifespanMultiplier` | float | 0.1-1 | 0.5 | Lifespan multiplier for explosion lasers |
| `explosionLasersCanCollide` | bool | - | false | Chain reaction explosions |
| `maxCollisionCount` | int | 1-10 | 3 | Max collisions per laser |

### Rendering

- **Blend Mode**: Additive
- **Shader**: Line rendering with glow

---

## Screen Distortion

**ID**: `screen-distortion`
**Screen Capture**: Yes (Continuous)

Creates lens/ripple distortion effects around your cursor that warp the screen.

### Features

- Real-time screen distortion
- Multiple distortion modes
- Chromatic aberration effect
- Optional glow layer
- Wireframe overlay option

### Settings

#### Distortion

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `distortionRadius` | float | 50-500 | 150 | Effect radius in pixels |
| `distortionStrength` | float | 0-1 | 0.3 | Distortion intensity |
| `rippleFrequency` | float | 1-20 | 8 | Wave frequency |
| `rippleSpeed` | float | 0-10 | 3 | Animation speed |
| `waveHeight` | float | 0-2 | 0.5 | Wave amplitude |
| `waveWidth` | float | 0-2 | 1.0 | Effect width |

#### Effects

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `enableChromatic` | bool | true | RGB channel separation |
| `enableGlow` | bool | true | Glow layer effect |
| `glowIntensity` | float | 0.2 | Glow strength |
| `glowColor` | Color4 | Blue | Glow color |
| `enableWireframe` | bool | false | Overlay grid pattern |
| `wireframeSpacing` | float | 30 | Grid spacing |
| `wireframeThickness` | float | 1.5 | Line thickness |
| `wireframeColor` | Color4 | Cyan | Grid color |

### Rendering

- **Blend Mode**: Alpha
- **Shader**: Fullscreen distortion with screen texture sampling

---

## Color Blindness

**ID**: `color-blindness`
**Screen Capture**: Yes (Continuous)

Advanced color vision deficiency (CVD) simulation and correction tool. Supports multiple scientifically-validated algorithms, zone-based filtering, and comparison layouts for accessibility testing.

### Features

- **17 filter types** including 6 Machado and 6 Strict algorithm variants
- **Correction & Simulation modes**: Help colorblind users see colors OR simulate what they see
- **Zone-based architecture**: Up to 4 independent zones with different filters
- **Multiple layouts**: Single, Split (horizontal/vertical), Quadrants, Comparison
- **Dual algorithm support**: Machado (RGB-space) and Strict (LMS-space) methods
- Circular, rectangular, or fullscreen application
- Editable RGB curves with Catmull-Rom interpolation
- Real-time color transformation with smooth edge transitions
- Global hotkey support (Alt+Shift+C to toggle)

### Settings

#### Layout

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `layoutMode` | int | 0-4 | 0 | 0=Single, 1=Split H, 2=Split V, 3=Quadrants, 4=Comparison |

**Layout Modes:**
- **Single**: One filter applied to entire area
- **Split Horizontal**: Two zones side by side
- **Split Vertical**: Two zones top and bottom
- **Quadrants**: Four zones in corners
- **Comparison**: Original vs filtered with virtual cursor indicator

#### Shape

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `shapeMode` | int | 0-2 | 0 | 0=Circle, 1=Rectangle, 2=Fullscreen |
| `radius` | float | 50-800 | 300 | Circle radius (px) |
| `rectWidth` | float | 100-1000 | 400 | Rectangle width (px) |
| `rectHeight` | float | 100-1000 | 300 | Rectangle height (px) |
| `edgeSoftness` | float | 0-1 | 0.2 | Edge feather amount |

#### Filter Types

Each zone can have an independent filter and mode. The plugin supports 17 filter types across two algorithm families:

| Index | Filter Name | Description |
|-------|-------------|-------------|
| 0 | None | No color transformation |
| 1 | Protanopia (Machado) | L-cone deficiency, RGB-space algorithm |
| 2 | Protanomaly (Machado) | Partial L-cone weakness, RGB-space |
| 3 | Deuteranopia (Machado) | M-cone deficiency, RGB-space algorithm |
| 4 | Deuteranomaly (Machado) | Partial M-cone weakness, RGB-space |
| 5 | Tritanopia (Machado) | S-cone deficiency, RGB-space algorithm |
| 6 | Tritanomaly (Machado) | Partial S-cone weakness, RGB-space |
| 7 | Protanopia (Strict) | L-cone deficiency, LMS-space algorithm |
| 8 | Protanomaly (Strict) | Partial L-cone weakness, LMS-space |
| 9 | Deuteranopia (Strict) | M-cone deficiency, LMS-space algorithm |
| 10 | Deuteranomaly (Strict) | Partial M-cone weakness, LMS-space |
| 11 | Tritanopia (Strict) | S-cone deficiency, LMS-space algorithm |
| 12 | Tritanomaly (Strict) | Partial S-cone weakness, LMS-space |
| 13 | Achromatopsia | Complete color blindness (rod monochromacy) |
| 14 | Achromatomaly | Partial color blindness |
| 15 | Grayscale | Simple luminance conversion |
| 16 | Inverted Grayscale | Inverted luminance |

#### Mode (Correction vs Simulation)

Each zone can operate in one of two modes:

| Mode | Description |
|------|-------------|
| **Correction** | Enhances colors to help colorblind users distinguish them. Shifts lost color information to visible channels (e.g., red-green differences shifted to blue). |
| **Simulation** | Shows how a colorblind person perceives colors. Useful for accessibility testing and empathy building. |

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `zone0Mode` | int | 0-1 | 0 | 0=Correction, 1=Simulation |
| `zone1Mode` | int | 0-1 | 0 | Mode for zone 1 |
| `zone2Mode` | int | 0-1 | 0 | Mode for zone 2 |
| `zone3Mode` | int | 0-1 | 0 | Mode for zone 3 |

#### Zone Configuration

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `zone0FilterType` | int | 0-16 | 0 | Filter for zone 0 |
| `zone1FilterType` | int | 0-16 | 0 | Filter for zone 1 |
| `zone2FilterType` | int | 0-16 | 0 | Filter for zone 2 |
| `zone3FilterType` | int | 0-16 | 0 | Filter for zone 3 |
| `intensity` | float | 0-1 | 1.0 | Filter strength |
| `colorBoost` | float | 0-2 | 1.0 | Color saturation boost |

### Algorithm Details

#### Machado Algorithm (Types 1-6)

Based on research by Machado, Oliveira, and Fernandes (2009). Uses pre-computed 3x3 matrices that operate directly in linear RGB space.

**Advantages:**
- Fast computation (single matrix multiply)
- Widely validated and used in accessibility tools
- Good visual accuracy for most use cases

**Technical Reference:**
- Machado, G. M., Oliveira, M. M., & Fernandes, L. A. (2009). "A Physiologically-based Model for Simulation of Color Vision Deficiency"

#### Strict LMS Algorithm (Types 7-12)

Based on Brettel, Viénot & Mollon (1997) confusion lines. Converts to LMS colorspace, applies cone deficiency simulation, then converts back.

**Advantages:**
- More physiologically accurate
- Better for scientific/medical applications
- Proper colorspace handling

**Technical Details:**
- Uses Hunt-Pointer-Estevez matrix for RGB→LMS conversion
- Applies confusion line projection for missing cone type
- Coefficients preserve white point (sum to 1.0)

### Use Cases

- **Accessibility Testing**: Compare original vs simulated to check UI color contrast
- **Correction Mode**: Help colorblind users distinguish problematic colors
- **Design Validation**: Use Quadrants layout to compare multiple CVD types simultaneously
- **Education**: Demonstrate how different CVD types perceive the world

#### Curves

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `enableCurves` | bool | false | Enable RGB curve adjustments |
| `curveStrength` | float | 1.0 | Curve effect blend |
| `redCurve` | string | - | Red channel curve data |
| `greenCurve` | string | - | Green channel curve data |
| `blueCurve` | string | - | Blue channel curve data |
| `masterCurve` | string | - | Overall luminosity curve |

### Curve Format

Curves are stored as semicolon-separated control points:
```
"0,0;0.25,0.25;0.5,0.5;0.75,0.75;1,1"
```

Each point is `x,y` where both values are 0-1 normalized.

### Hotkeys

| Hotkey | Action |
|--------|--------|
| **Alt+Shift+C** | Toggle effect on/off |

### Rendering

- **Blend Mode**: Opaque (fullscreen)
- **Shader**: Color matrix transformation with curve LUT

---

## Color Blindness NG

**ID**: `color-blindness-ng`
**Screen Capture**: Yes (Continuous)

Next-generation color vision deficiency (CVD) simulation and correction plugin with per-zone independent configuration, LUT-based correction, and real comparison mode with screen duplication.

### Features

- **Per-zone architecture**: Up to 4 independent zones, each configurable as Original, Simulation, or Correction
- **14 CVD simulation types** across Machado (RGB) and Strict (LMS) algorithms
- **LUT-based correction**: Per-channel color remapping with customizable gradients
- **Simulation-Guided Correction** (v1.0.28+): Uses CVD simulation to detect affected pixels before applying correction
- **Post-Correction Simulation** (v1.0.28+): Re-simulate CVD after correction to verify effectiveness
- **Sensitivity control** (v1.0.28+): Adjustable detection sensitivity for simulation-guided correction
- **Real comparison mode**: Full screen duplicated in each zone with virtual cursor projection
- **Custom preset system**: Save, load, export, and import correction presets
- **Multiple split modes**: Fullscreen, Split Vertical/Horizontal, Quadrants, Circle, Rectangle
- **Circle/Rectangle modes**: Cursor-following shapes with configurable dimensions and soft edge blending
- **Reusable CorrectionEditor**: Unified settings control for all zones with consistent UI
- **Configurable hotkeys**: Enable/disable Alt+Shift+M (toggle effect) and Alt+Shift+L (toggle settings)
- **Passthrough preset**: Apply no color changes for reference comparison
- **Improved dialog handling**: Dialogs properly appear above overlay with suspended topmost

### Split Modes

| Mode | Zones | Description |
|------|-------|-------------|
| **Fullscreen** | 1 | Single zone covering entire screen |
| **Split Vertical** | 2 | Left and Right zones |
| **Split Horizontal** | 2 | Top and Bottom zones |
| **Quadrants** | 4 | Four corner zones (TL, TR, BL, BR) |
| **Circle** | 2 | Circular zone follows cursor (Inner/Outer zones) |
| **Rectangle** | 2 | Rectangular zone follows cursor (Inner/Outer zones) |

### Zone Modes

Each zone can operate in one of three modes:

| Mode | Description |
|------|-------------|
| **Original** | No processing, displays original screen content |
| **Simulation** | Applies CVD simulation to show how colorblind users perceive colors |
| **Correction** | Applies LUT-based color remapping to help distinguish colors |

### Settings

#### Split Screen

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `splitMode` | int | 0-5 | 0 | 0=Fullscreen, 1=SplitV, 2=SplitH, 3=Quadrants, 4=Circle, 5=Rectangle |
| `splitPosition` | float | 0.1-0.9 | 0.5 | Horizontal split position |
| `splitPositionV` | float | 0.1-0.9 | 0.5 | Vertical split position |
| `comparisonMode` | bool | - | false | Enable screen duplication comparison |

#### Shape Mode Settings (Circle/Rectangle)

When Circle or Rectangle mode is selected, the shape follows the mouse cursor with an inner zone (inside the shape) and outer zone (outside the shape).

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `radius` | float | 10-1000 | 200 | Circle mode radius (pixels) |
| `rectWidth` | float | 10-2000 | 300 | Rectangle mode width (pixels) |
| `rectHeight` | float | 10-2000 | 200 | Rectangle mode height (pixels) |
| `edgeSoftness` | float | 0-1 | 0.2 | Edge blending (0=hard edge, 1=maximum soft) |

**Shape Mode Notes:**
- Comparison mode is not available for shape modes
- Split position sliders are hidden for shape modes
- Zone 0 = Inner zone (inside the shape)
- Zone 1 = Outer zone (outside the shape)
- Both zones have full configuration (Original/Simulation/Correction)
- Edge softness creates smooth blending between inner and outer zones using `smoothstep()`

#### Per-Zone Configuration

Each zone (0-3) has independent settings with prefix `zone{n}_`:

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `zone{n}_mode` | int | 0-2 | varies | 0=Original, 1=Simulation, 2=Correction |
| `zone{n}_simAlgorithm` | int | 0-1 | 0 | 0=Machado, 1=Strict |
| `zone{n}_simFilterType` | int | 0-14 | 0 | Simulation filter type |
| `zone{n}_intensity` | float | 0-1 | 1.0 | Effect intensity |

### Simulation Filter Types

| Index | Filter Name | Algorithm | Description |
|-------|-------------|-----------|-------------|
| 0 | None | - | No color transformation |
| 1 | Protanopia | Machado | L-cone deficiency |
| 2 | Protanomaly | Machado | Partial L-cone weakness |
| 3 | Deuteranopia | Machado | M-cone deficiency |
| 4 | Deuteranomaly | Machado | Partial M-cone weakness |
| 5 | Tritanopia | Machado | S-cone deficiency |
| 6 | Tritanomaly | Machado | Partial S-cone weakness |
| 7 | Protanopia | Strict/LMS | L-cone deficiency (LMS space) |
| 8 | Protanomaly | Strict/LMS | Partial L-cone weakness (LMS space) |
| 9 | Deuteranopia | Strict/LMS | M-cone deficiency (LMS space) |
| 10 | Deuteranomaly | Strict/LMS | Partial M-cone weakness (LMS space) |
| 11 | Tritanopia | Strict/LMS | S-cone deficiency (LMS space) |
| 12 | Tritanomaly | Strict/LMS | Partial S-cone weakness (LMS space) |
| 13 | Achromatopsia | - | Complete color blindness |
| 14 | Achromatomaly | - | Partial color blindness |

### LUT Correction Settings

Each zone has three channel LUTs (Red, Green, Blue) with these settings:

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `zone{n}_{color}Enabled` | bool | - | varies | Enable channel correction |
| `zone{n}_{color}Strength` | float | 0-1 | 1.0 | Channel correction strength |
| `zone{n}_{color}StartColor` | string | hex | varies | LUT gradient start color |
| `zone{n}_{color}EndColor` | string | hex | varies | LUT gradient end color |
| `zone{n}_{color}WhiteProtection` | float | 0.01-1 | 0.01 | Protect whites from correction |

### Simulation-Guided Correction (v1.0.28+)

An optional mode that uses CVD simulation to detect which pixels are affected before applying LUT correction. This ensures only pixels that would actually be affected by CVD receive correction, leaving unaffected colors unchanged.

**How It Works:**
1. Simulates the selected CVD type on the original pixel
2. Calculates the error between original and simulated colors
3. Uses the error magnitude to determine a blend weight (0-1)
4. Applies LUT correction proportionally to the detected error

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `zone{n}_simGuidedEnabled` | bool | - | false | Enable simulation-guided detection |
| `zone{n}_simGuidedAlgorithm` | int | 0-1 | 0 | 0=Machado, 1=Strict |
| `zone{n}_simGuidedFilterType` | int | 1-14 | 3 | CVD type to detect |
| `zone{n}_simGuidedSensitivity` | float | 0.5-5.0 | 2.0 | Detection sensitivity (higher = more aggressive) |

**Sensitivity Guide:**
- **0.5-1.0**: Conservative - only strong color differences trigger correction
- **2.0**: Balanced default - moderate detection threshold
- **3.0-5.0**: Aggressive - even subtle color differences trigger correction

### Post-Correction Simulation (Re-simulate for Verification) (v1.0.28+)

Applies CVD simulation AFTER correction to visualize how corrected colors appear to colorblind users. This helps non-colorblind users verify that corrections are effective.

**Use Case:**
1. Apply color correction for deuteranopia
2. Enable "Re-simulate for Verification" with deuteranopia
3. Screen shows: Original → Correction → Simulation
4. See what a deuteranope would perceive after correction is applied
5. Verify that previously confusing colors are now distinguishable

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `zone{n}_postSimEnabled` | bool | - | false | Enable post-correction simulation |
| `zone{n}_postSimAlgorithm` | int | 0-1 | 0 | 0=Machado, 1=Strict |
| `zone{n}_postSimFilterType` | int | 1-14 | 3 | CVD type to simulate |
| `zone{n}_postSimIntensity` | float | 0-1 | 1.0 | Simulation blend intensity |

**Key Distinction:**
- **Simulation-Guided Correction**: Uses simulation to DETECT which pixels need correction
- **Post-Correction Simulation**: Uses simulation AFTER correction for VISUALIZATION

Both features can be enabled independently on the same zone

#### Application Modes

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `zone{n}_appMode` | int | 0-2 | 0 | 0=Full, 1=Dominant, 2=Threshold |
| `zone{n}_threshold` | float | 0-1 | 0.3 | Threshold for threshold mode |
| `zone{n}_gradientType` | int | 0-2 | 0 | 0=LinearRGB, 1=LAB, 2=HSL |

**Application Mode Details:**
- **Full Channel**: Apply correction to all pixels where channel has value
- **Dominant Only**: Apply only when channel is the dominant color
- **Threshold**: Apply only when channel value exceeds threshold

### Comparison Mode

When enabled with a split mode, comparison mode:

1. **Duplicates the full screen** into each zone (scaled to fit)
2. **Preserves aspect ratio** with letterboxing/pillarboxing for split modes
3. **Projects a virtual cursor** showing mouse position in each zone
4. **Applies zone effects** to each duplicated view independently

This allows direct side-by-side comparison of:
- Original vs. Simulated
- Original vs. Corrected
- Multiple simulation types
- Different correction configurations

### Custom Preset System

The plugin supports custom presets for correction configurations:

#### Preset Buttons

| Button | Description |
|--------|-------------|
| **Apply** | Apply selected preset to current zone |
| **Save As...** | Save current settings as new custom preset |
| **Save** | Update existing custom preset (custom presets only) |
| **Export** | Export preset to JSON file |
| **Import** | Import preset from JSON file |

#### Preset Storage

- **Built-in presets**: Custom, Passthrough, Deuteranopia, Protanopia, Tritanopia, Deuteranomaly, Protanomaly, Tritanomaly, Red-Green (Both), High Contrast
- **Custom presets**: Stored in `{PluginDir}/ColorBlindnessNG_Presets/*.json`

#### Built-in Preset Descriptions

| Preset | Description |
|--------|-------------|
| **Custom** | User-defined settings, all channels disabled by default |
| **Passthrough** | No color correction applied - colors remain unchanged |
| **Deuteranopia** | Green-blind correction, shifts greens to cyan |
| **Protanopia** | Red-blind correction, shifts reds to cyan |
| **Tritanopia** | Blue-blind correction, shifts blues to yellow |
| **Deuteranomaly** | Green-weak correction with milder settings |
| **Protanomaly** | Red-weak correction with milder settings |
| **Tritanomaly** | Blue-weak correction with milder settings |
| **Red-Green (Both)** | Combined red and green correction |
| **High Contrast** | Enhanced color separation for general accessibility |

#### Preset JSON Format

```json
{
  "Name": "My Custom Preset",
  "Description": "Custom correction for enhanced visibility",
  "IsCustom": true,
  "CreatedDate": "2025-12-06T10:30:00Z",
  "RedEnabled": true,
  "RedStrength": 0.8,
  "RedStartColor": "#FF0000",
  "RedEndColor": "#00FFFF",
  "RedWhiteProtection": 0.2,
  "GreenEnabled": false,
  "GreenStrength": 1.0,
  "GreenStartColor": "#00FF00",
  "GreenEndColor": "#00FFFF",
  "GreenWhiteProtection": 0.0,
  "BlueEnabled": false,
  "BlueStrength": 1.0,
  "BlueStartColor": "#0000FF",
  "BlueEndColor": "#FFFF00",
  "BlueWhiteProtection": 0.0,
  "DefaultIntensity": 1.0,
  "RecommendedGradientType": 0,
  "RecommendedApplicationMode": 0
}
```

### Hotkeys

Hotkeys can be individually enabled or disabled in the settings window under the Hotkeys section.

| Hotkey | Action | Default |
|--------|--------|---------|
| **Alt+Shift+M** | Toggle effect on/off | Enabled |
| **Alt+Shift+L** | Toggle settings window visibility | Enabled |

### Algorithm Details

#### Machado Algorithm (Types 1-6)

Based on Machado, Oliveira, and Fernandes (2009). Uses pre-computed 3x3 matrices operating in linear RGB space.

**Advantages:**
- Fast computation (single matrix multiply)
- Widely validated in accessibility tools
- Good visual accuracy

#### Strict LMS Algorithm (Types 7-12)

Based on Brettel, Viénot & Mollon (1997) confusion lines. Converts to LMS colorspace for processing.

**Advantages:**
- More physiologically accurate
- Better for scientific/medical applications
- Proper colorspace handling

**Technical Details:**
- Uses Hunt-Pointer-Estevez matrix for RGB→LMS
- Applies confusion line projection
- Coefficients preserve white point

### Use Cases

1. **Accessibility Testing**
   - Compare original vs. simulated side-by-side
   - Test UI color contrast for colorblind users
   - Validate multiple CVD types simultaneously with quadrant mode

2. **Vision Assistance**
   - Apply correction to distinguish problematic colors
   - Customize per-channel correction
   - Save presets for different scenarios

3. **Education & Design**
   - Demonstrate CVD perception
   - Validate color schemes for accessibility
   - Share presets with team via export/import

### Rendering

- **Blend Mode**: Opaque (fullscreen)
- **Shader**: Per-zone processing with 12 LUT textures (4 zones × 3 channels)
- **Constant Buffer**: 288 bytes with global and per-zone parameters

---

## Radial Dithering

**ID**: `radial-dithering`
**Screen Capture**: Optional

Bayer-pattern dithering effect in a circular area around the cursor.

### Features

- Multiple falloff types (linear, smooth, ring)
- Bayer matrix dithering pattern
- Color blending modes
- Optional glow and noise
- Pattern animation

### Settings

#### Spatial

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `radius` | float | 50-500 | 200 | Effect radius (px) |
| `edgeSoftness` | float | 0-1 | 0.3 | Edge feathering |
| `falloffType` | int | 0-2 | 1 | 0=Linear, 1=Smooth, 2=Ring |
| `ringWidth` | float | 0-1 | 0.3 | Ring width (ring mode only) |

#### Pattern

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `patternScale` | float | 0.5-10 | 2.0 | Dithering pattern size |
| `intensity` | float | 0-1 | 0.5 | Dithering strength |
| `invertPattern` | bool | - | false | Invert the pattern |
| `enableAnimation` | bool | - | false | Animate the pattern |
| `animationSpeed` | float | 0-5 | 1.0 | Animation speed |

#### Colors

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `color1` | Color4 | White | First dither color |
| `color2` | Color4 | Black | Second dither color |
| `colorBlendMode` | int | 0 | Blend mode (0=Replace, 1=Multiply, etc.) |

#### Effects

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `threshold` | float | 0-1 | 0 | Dithering threshold |
| `enableGlow` | bool | - | false | Enable glow halo |
| `glowIntensity` | float | 0-1 | 0.3 | Glow strength |
| `glowColor` | Color4 | Blue | Glow color |
| `enableNoise` | bool | - | false | Add noise overlay |
| `noiseAmount` | float | 0-1 | 0.2 | Noise intensity |
| `alpha` | float | 0-1 | 1.0 | Overall opacity |

### Rendering

- **Blend Mode**: Alpha
- **Shader**: Bayer matrix dithering with falloff

---

## Tile Vibration

**ID**: `tile-vibration`
**Screen Capture**: Yes (Continuous)

Creates vibrating, shrinking tiles that display captured screen content.

### Features

- Up to 100 simultaneous tiles
- Tiles spawn along mouse movement path
- Vibration, zoom, and rotation effects
- Configurable edge styles and outlines
- Screen-based tile content

### Settings

#### Lifecycle

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `tileLifespan` | float | 0.5-10 | 2.0 | Tile lifetime (seconds) |
| `maxWidth` | float | 20-500 | 100 | Starting width (px) |
| `maxHeight` | float | 20-500 | 100 | Starting height (px) |
| `minWidth` | float | 5-100 | 20 | Ending width (px) |
| `minHeight` | float | 5-100 | 20 | Ending height (px) |
| `syncWidthHeight` | bool | - | true | Lock width/height together |

#### Visual Style

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `edgeStyle` | int | 0-1 | 0 | 0=Sharp, 1=Soft edges |
| `outlineEnabled` | bool | - | false | Draw tile outline |
| `outlineColor` | Color4 | - | White | Outline color |
| `outlineSize` | float | 1-10 | 2 | Outline thickness (px) |

#### Vibration Effects

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `vibrationSpeed` | float | 0.1-5 | 1.0 | Animation speed multiplier |
| `displacementEnabled` | bool | - | true | Enable position jitter |
| `displacementMax` | float | 1-50 | 10 | Max displacement (px) |
| `zoomEnabled` | bool | - | false | Enable size oscillation |
| `zoomMin` | float | 0.5-1 | 0.8 | Minimum zoom factor |
| `zoomMax` | float | 1-2 | 1.2 | Maximum zoom factor |
| `rotationEnabled` | bool | - | false | Enable rotation |
| `rotationAmplitude` | float | 1-90 | 15 | Max rotation (degrees) |

### Spawn Behavior

Tiles spawn when the mouse moves beyond a threshold distance:
- Threshold = `max(maxWidth, maxHeight) * 0.5`
- Ensures visual coverage without excessive tile count

### Rendering

- **Blend Mode**: Opaque
- **Shader**: Screen texture sampling with transform

---

## Water Ripple

**ID**: `water-ripple`
**Screen Capture**: Yes (Dynamic - only when ripples active)

Creates expanding water ripples on click that distort the screen with realistic wave interference. Supports separate wave parameters for click and mouse movement ripples.

### Features

- Up to 200 simultaneous ripples
- Realistic wave physics with interference patterns
- Click-triggered ripples with configurable buttons
- Optional mouse movement ripple trails
- Separate wave parameters for click vs. movement ripples
- Grid overlay for distortion visualization
- Dynamic screen capture (only captures when ripples exist)
- Performance optimized with early-out when idle

### Settings

#### General

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `maxRipples` | int | 1-200 | 50 | Maximum simultaneous ripples |
| `rippleLifespan` | float | 0.5-10 | 3.0 | Click ripple lifetime (seconds) |
| `waveSpeed` | float | 50-1000 | 200 | Click ripple expansion speed (px/s) |
| `wavelength` | float | 10-100 | 30 | Distance between wave peaks (px) |
| `damping` | float | 0.1-10 | 2.0 | Wave energy decay rate |

#### Click Triggers

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `spawnOnLeftClick` | bool | true | Create ripple on left click |
| `spawnOnRightClick` | bool | false | Create ripple on right click |
| `clickMinAmplitude` | float | 5 | Minimum click wave height (px) |
| `clickMaxAmplitude` | float | 20 | Maximum click wave height (px) |

#### Mouse Movement

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `spawnOnMove` | bool | - | false | Enable movement ripples |
| `moveSpawnDistance` | float | 10-200 | 50 | Distance before new ripple (px) |
| `moveMinAmplitude` | float | 1-50 | 3 | Minimum movement wave height (px) |
| `moveMaxAmplitude` | float | 5-100 | 10 | Maximum movement wave height (px) |
| `moveRippleLifespan` | float | 0.5-10 | 2.0 | Movement ripple lifetime (seconds) |
| `moveWaveSpeed` | float | 50-1000 | 300 | Movement ripple expansion speed (px/s) |
| `moveWavelength` | float | 10-100 | 20 | Movement wave peak distance (px) |
| `moveDamping` | float | 0.1-10 | 3.0 | Movement wave decay rate |

#### Grid Overlay

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `enableGrid` | bool | - | false | Show distortion grid |
| `gridSpacing` | float | 10-100 | 30 | Grid line spacing (px) |
| `gridThickness` | float | 0.5-5 | 1.5 | Grid line thickness (px) |
| `gridColor` | Color4 | - | Green (0,1,0.5,0.8) | Grid line color |

### Wave Physics

Each ripple simulates realistic wave behavior:

- **Expansion**: Ripples expand outward at `waveSpeed` pixels per second
- **Amplitude Decay**: Wave height decreases with distance (`damping` factor)
- **Interference**: Multiple overlapping ripples combine additively
- **Lifetime Fade**: Ripples fade out smoothly over their lifespan

### Performance Optimizations

- **Dynamic Screen Capture**: Only captures screen when ripples exist
- **Active Ripple Tracking**: Incremental count avoids O(n) operations
- **Precomputed Wavelength**: Inverse wavelength calculated on CPU
- **Early Exit**: Skips GPU operations when no active ripples
- **Partial Buffer Upload**: Only uploads active ripple data

### Rendering

- **Blend Mode**: Alpha (over screen capture)
- **Shader**: Screen distortion with sine wave displacement

---

## Zoom

**ID**: `zoom-effect`
**Screen Capture**: Yes (Continuous)

Creates a magnifying lens effect around your cursor with selectable circle or rectangle shape.

### Features

- Circle or rectangle zoom lens shape
- Adjustable zoom factor (1.1x to 5.0x)
- Configurable lens size and border
- Hotkey support for quick adjustments
- Real-time screen magnification
- Smooth rounded corners on rectangle mode

### Settings

#### Shape

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `shapeType` | int | 0-1 | 0 | 0=Circle, 1=Rectangle |

#### Zoom

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `zoomFactor` | float | 1.1-5.0 | 1.5 | Magnification level |

#### Circle Settings

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `radius` | float | 20-500 | 100 | Circle radius (px) |

#### Rectangle Settings

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `width` | float | 40-800 | 200 | Rectangle width (px) |
| `height` | float | 40-800 | 150 | Rectangle height (px) |
| `syncSizes` | bool | - | false | Lock width/height for square |

#### Border

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `borderWidth` | float | 0-10 | 2 | Border thickness (px) |
| `borderColor` | Color4 | - | Blue (0.2,0.6,1,1) | Border color |

#### Hotkeys

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `enableZoomHotkey` | bool | false | Enable Shift+Ctrl+Wheel for zoom |
| `enableSizeHotkey` | bool | false | Enable Shift+Alt+Wheel for size |

### Hotkey Controls

When enabled, hotkeys allow quick adjustment without opening settings:

| Hotkey | Action |
|--------|--------|
| **Shift+Ctrl+Mouse Wheel** | Adjust zoom factor by ±0.1 |
| **Shift+Alt+Mouse Wheel** | Adjust size by ±5% |

- Zoom hotkey adjusts the magnification level (1.1x to 5.0x)
- Size hotkey adjusts radius (circle) or width+height (rectangle)
- Changes are reflected in real-time in the settings panel
- Settings are automatically saved

### Rendering

- **Blend Mode**: Alpha
- **Shader**: Screen texture sampling with magnification

---

## Firework

**ID**: `firework`
**Screen Capture**: No

Creates stunning firework explosions with colorful particles, trails, rockets, and secondary explosions.

### Features

- GPU instanced rendering for up to 15,000 particles
- Up to 200 simultaneous fireworks/rockets
- Rocket mode with altitude-based explosions
- Secondary explosions for multi-stage effects
- Rainbow color cycling or custom colors
- Particle trails with velocity-based stretching
- Physics simulation with gravity and drag
- Randomized particle counts per firework for variety

### Settings

#### General

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `maxParticles` | int | 1000-15000 | 5000 | Maximum particles in the system |
| `maxFireworks` | int | 1-200 | 50 | Maximum simultaneous firework explosions |
| `particleLifespan` | float | 0.5-10 | 2.5 | How long particles live (seconds) |
| `minParticlesPerFirework` | int | 10-500 | 50 | Minimum particles per firework explosion |
| `maxParticlesPerFirework` | int | 10-500 | 150 | Maximum particles per firework explosion |

#### Click Trigger

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `spawnOnLeftClick` | bool | true | Create firework on left click |
| `spawnOnRightClick` | bool | false | Create firework on right click |
| `clickExplosionForce` | float | 300 | Initial velocity of particles (px/s) |

#### Movement Trigger

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `spawnOnMove` | bool | - | false | Create fireworks as mouse moves |
| `moveSpawnDistance` | float | 20-500 | 100 | Distance before spawning firework (px) |
| `moveExplosionForce` | float | 30-500 | 150 | Initial velocity for movement explosions (px/s) |

#### Particle Appearance

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `minParticleSize` | float | 1-20 | 3 | Minimum particle size (px) |
| `maxParticleSize` | float | 2-50 | 8 | Maximum particle size (px) |
| `glowIntensity` | float | 0-2 | 0.8 | Glow effect strength |
| `enableTrails` | bool | - | true | Elongate particles in movement direction |
| `trailLength` | float | 0.1-2 | 0.3 | Trail elongation amount |

#### Physics

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `gravity` | float | 0-500 | 150 | Downward acceleration (px/s²) |
| `drag` | float | 0.9-1.0 | 0.98 | Velocity damping (0.9=heavy, 1.0=none) |
| `spreadAngle` | float | 30-360 | 360 | Angular spread of explosion (degrees) |

#### Firework Colors

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `rainbowMode` | bool | - | true | Cycle through rainbow colors |
| `rainbowSpeed` | float | 0.1-5 | 0.5 | Rainbow cycling speed |
| `primaryColor` | Color4 | - | Orange (1,0.3,0.1) | Main firework color |
| `secondaryColor` | Color4 | - | Yellow (1,0.8,0.2) | Secondary color for mixing |
| `useRandomColors` | bool | - | true | Randomize colors per firework |

#### Secondary Explosion

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `enableSecondaryExplosion` | bool | - | true | Particles explode into smaller particles |
| `secondaryExplosionDelay` | float | 0.2-3 | 0.8 | Time before secondary explosion (seconds) |
| `secondaryParticleCount` | int | 5-100 | 20 | Particles in secondary explosion |
| `secondaryExplosionForce` | float | 20-300 | 100 | Force of secondary explosion (px/s) |

#### Rocket Mode

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `enableRocketMode` | bool | - | false | Launch rockets that fly up and explode |
| `rocketSpeed` | float | 100-1500 | 500 | Upward launch speed (px/s) |
| `rocketMinAltitude` | float | 5-50% | 10% | Minimum explosion altitude (% from top) |
| `rocketMaxAltitude` | float | 10-80% | 30% | Maximum explosion altitude (% from top) |
| `rocketMaxFuseTime` | float | 0.5-5 | 3.0 | Maximum time before forced explosion (seconds) |
| `rocketSize` | float | 5-50 | 12 | Rocket particle size (px) |

#### Rocket Appearance

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `rocketRainbowMode` | bool | - | true | Cycle rocket colors through rainbow |
| `rocketRainbowSpeed` | float | 0.1-5 | 0.5 | Rocket rainbow cycling speed |
| `rocketPrimaryColor` | Color4 | - | Yellow (1,0.8,0.2) | Main rocket color |
| `rocketSecondaryColor` | Color4 | - | Orange (1,0.4,0.1) | Secondary rocket color |
| `rocketUseRandomColors` | bool | - | true | Randomize colors per rocket |

### Rocket Explosion System

Rockets use an altitude-based explosion system for realistic firework displays:

1. **Target Altitude**: When a rocket spawns, it calculates a random target Y position within the altitude zone (between min and max altitude % from top of screen)
2. **Position-Based Explosion**: Rocket explodes when it reaches the target Y position
3. **Edge Case Handling**: If launched above the explosion zone, explodes after minimal travel
4. **Safety Fallback**: Max fuse time forces explosion to prevent off-screen rockets

This creates natural firework displays where all explosions occur in a consistent horizontal band across the screen.

### Particle Count Randomization

Each firework explosion spawns a random number of particles between `minParticlesPerFirework` and `maxParticlesPerFirework`. This creates variety in firework sizes while staying within the total particle budget (`maxParticles`).

- Click-triggered fireworks use the full random range
- Movement-triggered fireworks use 1/3 of the range (smaller explosions)
- Rocket explosions also use the full random range

### Rendering

- **Blend Mode**: Additive (creates glow effect)
- **Shader**: GPU instanced point sprites with trail stretching
- **Performance**: Efficient structured buffers for thousands of particles

---

## Space Invaders

**ID**: `invaders`
**Screen Capture**: No

A fully playable Space Invaders mini-game where you defend against waves of neon invaders by shooting rockets from your cursor.

### Features

- Classic Space Invaders gameplay with modern neon visuals
- Three invader types with different point values (Squid, Crab, Octopus)
- GPU instanced rendering for smooth performance
- Timed gameplay with Points-Per-Minute scoring
- High score leaderboard with persistent storage
- Game over on invader collision with mouse or screen bottom
- Hotkey support for quick game reset
- Rainbow color modes for rockets

### Game Rules

1. **Timer starts** on your first kill (not when effect is enabled)
2. **Shoot rockets** by clicking or moving the mouse
3. **Destroy invaders** before they reach the bottom or touch your cursor
4. **Score points** based on invader type:
   - Small (Squid) = 200 points
   - Medium (Crab) = 100 points
   - Big (Octopus) = 50 points
5. **Game ends** when timer expires or an invader touches mouse/bottom
6. **High scores** track Points-Per-Minute (PPM) for fair comparison

### Settings

#### Rocket Configuration

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `spawnOnLeftClick` | bool | - | true | Fire rocket on left click |
| `spawnOnRightClick` | bool | - | false | Fire rocket on right click |
| `spawnOnMove` | bool | - | false | Fire rockets while moving |
| `moveSpawnDistance` | float | 20-200 | 80 | Distance before movement rocket (px) |
| `rocketSpeed` | float | 200-1500 | 600 | Rocket travel speed (px/s) |
| `rocketSize` | float | 4-20 | 8 | Rocket particle size (px) |
| `rocketRainbowMode` | bool | - | true | Cycle rocket colors |
| `rocketRainbowSpeed` | float | 0.1-5 | 0.5 | Rainbow cycling speed |
| `rocketColor` | Color4 | - | Green (0,1,0.5) | Static rocket color |

#### Invader Configuration

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `invaderSpawnRate` | float | 0.5-5 | 1.5 | Seconds between spawns |
| `invaderMinSpeed` | float | 20-200 | 50 | Minimum descent speed (px/s) |
| `invaderMaxSpeed` | float | 50-300 | 150 | Maximum descent speed (px/s) |
| `invaderBigSize` | float | 24-80 | 48 | Base invader size (px) |
| `invaderMediumSizePercent` | float | 0.3-0.8 | 0.5 | Medium size (% of big) |
| `invaderSmallSizePercent` | float | 0.15-0.5 | 0.25 | Small size (% of big) |
| `maxActiveInvaders` | int | 5-50 | 20 | Maximum simultaneous invaders |
| `invaderDescentSpeed` | float | 10-100 | 30 | Additional downward speed (px/s) |

#### Invader Colors

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `invaderSmallColor` | Color4 | Magenta (1,0.2,0.8) | Small invader color |
| `invaderMediumColor` | Color4 | Cyan (0.2,0.8,1) | Medium invader color |
| `invaderBigColor` | Color4 | Green (0.2,1,0.4) | Big invader color |

#### Explosion Effects

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `explosionParticleCount` | int | 10-100 | 30 | Particles per explosion |
| `explosionForce` | float | 50-500 | 200 | Explosion force (px/s) |
| `explosionLifespan` | float | 0.5-3 | 1.0 | Particle lifetime (seconds) |
| `explosionParticleSize` | float | 2-15 | 6 | Explosion particle size (px) |
| `explosionGlowIntensity` | float | 0.5-3 | 1.5 | Explosion glow strength |

#### Visual Effects

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `glowIntensity` | float | 0-2 | 1.2 | Overall glow strength |
| `neonIntensity` | float | 0-2 | 1.0 | Neon effect strength |
| `enableTrails` | bool | - | true | Enable particle trails |
| `trailLength` | float | 0.1-1 | 0.4 | Trail length factor |
| `animSpeed` | float | 0.5-5 | 2.0 | Invader animation speed |

#### Scoring

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `scoreSmall` | int | 50-500 | 200 | Points for small invader |
| `scoreMedium` | int | 25-250 | 100 | Points for medium invader |
| `scoreBig` | int | 10-100 | 50 | Points for big invader |

#### Timer & Game

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `timerDuration` | float | 30-300 | 90 | Game duration (seconds) |
| `showScoreOverlay` | bool | - | true | Display score HUD |
| `enableResetHotkey` | bool | - | false | Enable Ctrl+Shift+I reset |

#### Score Overlay

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `scoreOverlaySize` | float | 16-64 | 32 | Score text size (px) |
| `scoreOverlaySpacing` | float | 1-3 | 1.5 | Character spacing multiplier |
| `scoreOverlayMargin` | float | 5-50 | 20 | Margin between elements (px) |
| `scoreOverlayBgOpacity` | float | 0-1 | 0.7 | Background opacity |
| `scoreOverlayColor` | Color4 | - | Green (0,1,0) | Score text color |
| `scoreOverlayX` | float | 0-200 | 70 | Horizontal position (px) |
| `scoreOverlayY` | float | 0-200 | 50 | Vertical position (px) |

### Hotkeys

| Hotkey | Action | Condition |
|--------|--------|-----------|
| **Ctrl+Shift+I** | Reset Game | When `enableResetHotkey` is true |

### High Score System

- Scores are tracked as **Points-Per-Minute (PPM)** for fair comparison across different timer durations
- Top 5 high scores are saved with timestamps
- New high scores are highlighted in the leaderboard
- Scores persist across sessions in the plugin configuration

### Rendering

- **Blend Mode**: Additive (creates neon glow)
- **Shader**: GPU instanced sprites with animation
- **Entity Types**: Invaders (3 types), Rockets, Explosion Particles, Score Overlay

---

## Plugin Settings Storage

All plugin settings are stored in:
```
%APPDATA%\MouseEffects\plugins\{plugin-id}.json
```

Example `particle-trail.json`:
```json
{
  "IsEnabled": true,
  "Configuration": {
    "emissionRate": 150,
    "particleLifetime": 2.0,
    "particleSize": 12,
    "startColor": { "X": 1, "Y": 0.5, "Z": 0, "W": 1 },
    "endColor": { "X": 1, "Y": 0, "Z": 0.5, "W": 1 }
  }
}
```

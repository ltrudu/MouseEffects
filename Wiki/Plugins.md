# Plugins Reference

This document provides detailed information about each built-in effect plugin.

## Table of Contents

- [Particle Trail](#particle-trail)
- [Laser Work](#laser-work)
- [Screen Distortion](#screen-distortion)
- [Color Blindness](#color-blindness)
- [Radial Dithering](#radial-dithering)
- [Tile Vibration](#tile-vibration)

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

Simulates various color blindness conditions with customizable RGB curve adjustment.

### Features

- Protanopia, Deuteranopia, Tritanopia, Achromatopsia simulation
- Circular, rectangular, or fullscreen application
- Editable RGB curves with Catmull-Rom interpolation
- Real-time color transformation
- Smooth edge transitions

### Settings

#### Shape

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `shapeMode` | int | 0-2 | 0 | 0=Circle, 1=Rectangle, 2=Fullscreen |
| `radius` | float | 50-800 | 300 | Circle radius (px) |
| `rectWidth` | float | 100-1000 | 400 | Rectangle width (px) |
| `rectHeight` | float | 100-1000 | 300 | Rectangle height (px) |
| `edgeSoftness` | float | 0-1 | 0.2 | Edge feather amount |

#### Filter

| Setting | Type | Range | Default | Description |
|---------|------|-------|---------|-------------|
| `filterType` | int | 0-3 | 1 | Color blindness type |
| `intensity` | float | 0-1 | 1.0 | Filter strength |
| `colorBoost` | float | 0-2 | 1.0 | Color saturation boost |

**Filter Types**:
- 0 = Protanopia (red-blind)
- 1 = Deuteranopia (green-blind)
- 2 = Tritanopia (blue-blind)
- 3 = Achromatopsia (complete color blindness)

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

### Rendering

- **Blend Mode**: Opaque (fullscreen)
- **Shader**: Color matrix transformation with curve LUT

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

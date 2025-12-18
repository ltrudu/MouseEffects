# GravityWell Trigger Conditions Feature

**Created**: 2025-12-18
**Status**: Planning

## Overview
Add configurable trigger conditions for when gravity forces are applied to particles. When triggers are inactive, particles continue with their current velocity but receive no new gravitational forces. Optional global drift/deceleration when gravity is off.

## Current Behavior
- Gravity is always applied every frame
- Particles continuously accelerate toward/away from cursor
- Damping setting already exists but applies uniformly

## New Feature Design

### Trigger Conditions (Checkboxes - Combinable)
```
Gravity Triggers:
☑ Always Active          ← Default ON, when checked gravity always applies
☐ On Left Mouse Down     ← Gravity only when left button held
☐ On Right Mouse Down    ← Gravity only when right button held
☐ On Mouse Move          ← Gravity only when cursor is moving
```

**Logic**:
- If "Always Active" is checked → gravity always applies (ignores other checkboxes)
- If "Always Active" is unchecked → gravity applies when ANY checked trigger is active
- If no triggers checked → gravity never applies (particles just drift)

### Drift/Deceleration Option
```
☐ Enable Drift When Inactive    ← Checkbox to enable deceleration when gravity off
   Drift Amount: [====----] 0.95  ← Slider (0.5 to 1.0, default 0.95)
```

**Logic**:
- When gravity is inactive AND drift enabled → apply drift multiplier to velocity
- Drift amount of 0.95 means velocity *= 0.95 each frame (gradual slowdown)
- Drift amount of 1.0 means no slowdown (particles maintain velocity)
- Drift amount of 0.5 means rapid slowdown

---

## Implementation Plan

### Files to Modify

1. **GravityWellEffect.cs**
   - Add trigger condition fields and properties
   - Add drift configuration fields
   - Track mouse movement (compare positions between frames)
   - Modify `UpdateParticle` to check trigger conditions before applying forces
   - Apply drift when gravity inactive and drift enabled

2. **GravityWellFactory.cs**
   - Add default config values for new settings
   - Add schema parameters for UI generation

3. **GravityWellSettingsControl.xaml**
   - Add "Gravity Triggers" section with checkboxes
   - Add "Drift Settings" section with checkbox and slider

4. **GravityWellSettingsControl.xaml.cs**
   - Add loading/saving for new configuration values
   - Add event handlers for new controls

---

## Detailed Implementation

### Step 1: Add Fields to GravityWellEffect.cs

```csharp
// Trigger condition fields
private bool _triggerAlwaysActive = true;
private bool _triggerOnLeftMouseDown = false;
private bool _triggerOnRightMouseDown = false;
private bool _triggerOnMouseMove = false;

// Drift settings
private bool _driftEnabled = false;
private float _driftAmount = 0.95f;

// Mouse movement tracking
private Vector2 _previousCursorPos;
private bool _isMouseMoving = false;
private const float MouseMoveThreshold = 2f; // pixels
```

### Step 2: Update OnConfigurationChanged

Add reading of new config keys:
- `gw_triggerAlwaysActive` (bool)
- `gw_triggerOnLeftMouseDown` (bool)
- `gw_triggerOnRightMouseDown` (bool)
- `gw_triggerOnMouseMove` (bool)
- `gw_driftEnabled` (bool)
- `gw_driftAmount` (float)

### Step 3: Update OnUpdate Method

```csharp
// Detect mouse movement
float cursorDelta = Vector2.Distance(cursorPos, _previousCursorPos);
_isMouseMoving = cursorDelta > MouseMoveThreshold;
_previousCursorPos = cursorPos;

// Determine if gravity should be active this frame
bool gravityActive = IsGravityActive(mouseState);
```

### Step 4: Add IsGravityActive Helper Method

```csharp
private bool IsGravityActive(MouseState mouseState)
{
    // If always active is checked, gravity is always on
    if (_triggerAlwaysActive)
        return true;

    // Check each trigger condition
    if (_triggerOnLeftMouseDown && mouseState.IsButtonDown(MouseButtons.Left))
        return true;
    if (_triggerOnRightMouseDown && mouseState.IsButtonDown(MouseButtons.Right))
        return true;
    if (_triggerOnMouseMove && _isMouseMoving)
        return true;

    // No triggers active
    return false;
}
```

### Step 5: Modify UpdateParticle Signature and Logic

Pass `gravityActive` flag to UpdateParticle:
```csharp
private void UpdateParticle(int particleIndex, ref ParticleInstance particle,
                            Vector2 cursorPos, float deltaTime, bool gravityActive)
{
    if (gravityActive)
    {
        // Existing gravity/force calculation code
        // ...apply acceleration...
    }
    else if (_driftEnabled)
    {
        // Apply drift deceleration when gravity is off
        particle.Velocity *= _driftAmount;
    }
    // else: particle continues with current velocity (no change)

    // Rest of update (position, rotation, trails, edge behavior)
    // ...
}
```

### Step 6: Update Factory Default Config

```csharp
// Trigger settings
config.Set("gw_triggerAlwaysActive", true);
config.Set("gw_triggerOnLeftMouseDown", false);
config.Set("gw_triggerOnRightMouseDown", false);
config.Set("gw_triggerOnMouseMove", false);

// Drift settings
config.Set("gw_driftEnabled", false);
config.Set("gw_driftAmount", 0.95f);
```

### Step 7: Update Factory Schema

Add parameters for each new setting with appropriate types (BoolParameter, FloatParameter).

### Step 8: Update Settings UI XAML

Add new section after Physics Settings:
```xaml
<!-- Gravity Triggers Section -->
<TextBlock Text="Gravity Triggers" FontWeight="Bold" FontSize="13" Margin="0,16,0,4"/>

<CheckBox x:Name="TriggerAlwaysActiveCheckBox" Content="Always Active"
          Style="{StaticResource CheckBoxStyle}"
          Checked="TriggerAlwaysActiveCheckBox_Changed"
          Unchecked="TriggerAlwaysActiveCheckBox_Changed"/>
<CheckBox x:Name="TriggerOnLeftMouseDownCheckBox" Content="On Left Mouse Down"
          Style="{StaticResource CheckBoxStyle}"
          Checked="TriggerCheckBox_Changed" Unchecked="TriggerCheckBox_Changed"/>
<CheckBox x:Name="TriggerOnRightMouseDownCheckBox" Content="On Right Mouse Down"
          Style="{StaticResource CheckBoxStyle}"
          Checked="TriggerCheckBox_Changed" Unchecked="TriggerCheckBox_Changed"/>
<CheckBox x:Name="TriggerOnMouseMoveCheckBox" Content="On Mouse Move"
          Style="{StaticResource CheckBoxStyle}"
          Checked="TriggerCheckBox_Changed" Unchecked="TriggerCheckBox_Changed"/>

<!-- Drift Settings Section -->
<TextBlock Text="Drift Settings" FontWeight="Bold" FontSize="13" Margin="0,16,0,4"/>

<CheckBox x:Name="DriftEnabledCheckBox" Content="Enable Drift When Inactive"
          Style="{StaticResource CheckBoxStyle}"
          Checked="DriftEnabledCheckBox_Changed" Unchecked="DriftEnabledCheckBox_Changed"/>

<TextBlock Text="Drift Amount" Style="{StaticResource LabelStyle}"/>
<Slider x:Name="DriftAmountSlider" Minimum="0.5" Maximum="1.0" Value="0.95"
        Style="{StaticResource SliderStyle}" ValueChanged="DriftAmountSlider_ValueChanged"/>
<TextBlock x:Name="DriftAmountValue" Text="0.95" Opacity="0.7" FontSize="11"/>
```

### Step 9: Update Settings UI Code-Behind

Add load/save logic and event handlers for all new controls.

---

## UI Behavior Notes

1. **"Always Active" checkbox interaction**:
   - When checked, the other trigger checkboxes could be visually dimmed (IsEnabled=false) to indicate they're ignored
   - Or simply leave them enabled but document that "Always Active" takes precedence

2. **Drift slider visibility**:
   - Could hide/show the Drift Amount slider based on "Enable Drift" checkbox state
   - Or always show but disable when drift is unchecked

---

## Testing Scenarios

1. Default behavior (Always Active) should work exactly as before
2. Left mouse trigger: gravity only when holding left button
3. Right mouse trigger: gravity only when holding right button
4. Mouse move trigger: gravity only when cursor is moving
5. Combined triggers: gravity when ANY condition is met
6. Drift enabled: particles slow down when gravity is off
7. Drift disabled: particles maintain velocity when gravity is off

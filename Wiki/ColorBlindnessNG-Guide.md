# ColorBlindnessNG - Complete User Guide

Welcome to the ColorBlindnessNG user guide! This document will help you understand and use this plugin from the very beginning, even if you have no technical background.

---

## Table of Contents

1. [What is Color Blindness?](#what-is-color-blindness)
2. [What Does This Plugin Do?](#what-does-this-plugin-do)
3. [Installing MouseEffects](#installing-mouseeffects)
4. [First Launch](#first-launch)
5. [Finding and Enabling ColorBlindnessNG](#finding-and-enabling-colorblindnessng)
6. [Understanding the Interface](#understanding-the-interface)
7. [Tutorial: Correcting Colors Step by Step](#tutorial-correcting-colors-step-by-step)
8. [Best Colors for Each Type of Color Blindness](#best-colors-for-each-type-of-color-blindness)
9. [Advanced Features](#advanced-features)
   - [Re-simulation Mode](#re-simulation-mode-new-in-v1031)
   - [Correction Algorithms](#correction-algorithms)
     - [LUT-Based Correction](#1-lut-based-correction-default)
     - [Daltonization](#2-daltonization)
     - [Hue Rotation](#3-hue-rotation-new-in-v1030)
     - [CIELAB Remapping](#4-cielab-remapping-new-in-v1030)
   - [Simulation-Guided Correction](#simulation-guided-correction)
   - [Application Modes](#application-modes)
   - [Gradient Types](#gradient-types)
   - [Blend Modes](#blend-modes)
   - [Circle and Rectangle Modes](#circle-and-rectangle-modes)
   - [White Protection](#white-protection)
   - [Hotkeys](#hotkeys-keyboard-shortcuts)
10. [Frequently Asked Questions](#frequently-asked-questions)

---

## What is Color Blindness?

Color blindness (also called Color Vision Deficiency or CVD) is a condition where people see colors differently than most people. It's not actually "blindness" - people with CVD can see, but certain colors look similar to them when they're actually different.

### Types of Color Blindness

| Type | What It Affects | How Common |
|------|-----------------|------------|
| **Deuteranopia** | Cannot distinguish green from red | Most common (affects ~6% of men) |
| **Deuteranomaly** | Weak green perception | Very common |
| **Protanopia** | Cannot distinguish red from green | Common (affects ~2% of men) |
| **Protanomaly** | Weak red perception | Common |
| **Tritanopia** | Cannot distinguish blue from yellow | Rare |
| **Tritanomaly** | Weak blue perception | Rare |
| **Achromatopsia** | Sees only in black and white | Very rare |

**Example:** For someone with Deuteranopia, a red apple and green leaves might look almost the same color!

---

## What Does This Plugin Do?

ColorBlindnessNG has two main purposes:

### 1. Simulation Mode (For Testing)
Shows you what the screen looks like through the eyes of someone with color blindness. This is useful for:
- Designers checking if their work is accessible
- Teachers explaining color blindness to students
- Anyone curious about how others see the world

### 2. Correction Mode (For Helping)
Changes colors on your screen to help people with color blindness tell colors apart. For example:
- Red objects might get a blue tint
- Green objects might shift to cyan
- This makes red and green look different from each other

---

## Installing MouseEffects

### Step 1: Download MouseEffects

1. Go to the MouseEffects download page:
   **https://github.com/LeCaiss662/MouseEffects/releases**

2. Find the latest version (at the top of the page)

3. Click on **MouseEffects-win-Setup.exe** to download it

4. Wait for the download to complete (usually a few seconds)

### Step 2: Run the Installer

1. Open your **Downloads** folder
   - Press `Windows + E` to open File Explorer
   - Click "Downloads" on the left side

2. Double-click on **MouseEffects-win-Setup.exe**

3. If Windows shows a security warning:
   - Click "More info"
   - Click "Run anyway"
   - This is normal for new software

4. The installation starts automatically
   - No administrator password needed
   - Installs to your user folder
   - Takes about 10-30 seconds

5. When finished, MouseEffects will start automatically

### Step 3: Verify Installation

After installation, you should see:
- A small icon in your system tray (bottom-right corner of your screen, near the clock)
- The icon looks like a mouse cursor with effects

**Can't find the icon?** Click the small arrow (^) in the system tray to show hidden icons.

---

## First Launch

### What You'll See

When MouseEffects starts for the first time:

1. **System Tray Icon** appears in the bottom-right corner
2. The application runs in the background (no main window)
3. No effects are enabled by default

### Opening the Settings Window

**Method 1: Right-click the tray icon**
1. Find the MouseEffects icon in the system tray
2. Right-click on it
3. Click "Settings"

**Method 2: Double-click the tray icon**
1. Find the MouseEffects icon in the system tray
2. Double-click on it

### The Settings Window

When the settings window opens, you'll see:

```
┌─────────────────────────────────────────────────────┐
│  MouseEffects Settings                    [—][□][X] │
├─────────────────────────────────────────────────────┤
│                                                     │
│  ☐ Particle Trail                                   │
│  ☐ Laser Work                                       │
│  ☐ Screen Distortion                                │
│  ☐ Color Blindness                                  │
│  ☐ Color Blindness NG    ← This is what we need!   │
│  ☐ Radial Dithering                                 │
│  ☐ Tile Vibration                                   │
│  ... more effects ...                               │
│                                                     │
└─────────────────────────────────────────────────────┘
```

---

## Finding and Enabling ColorBlindnessNG

### Step 1: Open Settings
1. Right-click the MouseEffects tray icon
2. Click "Settings"

### Step 2: Find ColorBlindnessNG
1. Scroll down the list of effects
2. Look for **"Color Blindness NG"**
   - Note: There's also "Color Blindness" (the older version)
   - Make sure you select "Color Blindness **NG**" (the newer, better version)

### Step 3: Enable the Plugin
1. Click the checkbox next to "Color Blindness NG"
   - ☐ → ☑
2. The effect is now active!

### Step 4: Expand the Settings
1. Click the arrow or the name "Color Blindness NG" to expand
2. You'll see all the configuration options

---

## Understanding the Interface

### The Main Settings Panel

When you expand ColorBlindnessNG, you'll see:

```
┌─────────────────────────────────────────────────────┐
│ ☑ Color Blindness NG                           [▼]  │
├─────────────────────────────────────────────────────┤
│                                                     │
│  Split Mode: [Fullscreen           ▼]               │
│                                                     │
│  ☐ Comparison Mode                                  │
│                                                     │
│  ─────────────────────────────────────────────────  │
│                                                     │
│  ZONE 0                                             │
│  Mode: [Correction ▼]                               │
│                                                     │
│  Preset: [Deuteranopia ▼]                           │
│                                                     │
│  [Apply] [Save As...] [Export] [Import]             │
│                                                     │
│  Intensity: ████████████░░░░░░ 80%                  │
│                                                     │
│  [Detailed Color Settings...]                       │
│                                                     │
└─────────────────────────────────────────────────────┘
```

### What Each Setting Means

| Setting | What It Does |
|---------|--------------|
| **Split Mode** | How to divide your screen into zones |
| **Comparison Mode** | Shows the same content in all zones for easy comparison |
| **Zone Mode** | What each zone does: Original, Simulation, or Correction |
| **Preset** | Ready-made settings for each type of color blindness |
| **Intensity** | How strong the effect is (0% = off, 100% = full strength) |

### Split Modes Explained

| Mode | What It Looks Like | Best For |
|------|-------------------|----------|
| **Fullscreen** | Entire screen affected | Daily use |
| **Split Vertical** | Left / Right | Comparing two settings |
| **Split Horizontal** | Top / Bottom | Comparing two settings |
| **Quadrants** | 4 corners | Comparing many settings |
| **Circle** | Circle follows your mouse | Quick checking |
| **Rectangle** | Rectangle follows your mouse | Quick checking |

### Zone Modes Explained

| Mode | What It Does | When To Use |
|------|--------------|-------------|
| **Original** | No changes, normal colors | Reference/comparison |
| **Simulation** | Shows how colorblind people see | Testing/education |
| **Correction** | Helps distinguish colors | Daily assistance |
| **Re-simulation** | Applies simulation on top of another zone's correction | Verify corrections work for colorblind users |

---

## Tutorial: Correcting Colors Step by Step

### Tutorial 1: Quick Start - Basic Correction

**Goal:** Get color correction working in 2 minutes.

**Who is this for:** Someone with red-green color blindness who wants a quick solution.

---

**Step 1: Open Settings**

1. Look at the bottom-right corner of your screen (near the clock)
2. Find the MouseEffects icon (looks like a cursor with effects)
3. Right-click on it
4. Click "Settings"

---

**Step 2: Enable ColorBlindnessNG**

1. In the settings window, scroll down to find "Color Blindness NG"
2. Click the checkbox to enable it: ☐ → ☑
3. Click on "Color Blindness NG" to expand the settings

---

**Step 3: Choose Your Color Blindness Type**

1. Make sure "Mode" is set to **Correction**
2. Click the "Preset" dropdown
3. Select the type that matches you:
   - **Deuteranopia** - Can't distinguish green from red (most common)
   - **Protanopia** - Can't distinguish red from green
   - **Tritanopia** - Can't distinguish blue from yellow

   *Not sure which one? Try Deuteranopia first - it's the most common.*

---

**Step 4: Apply and Test**

1. Click the **Apply** button
2. Look around your screen
3. Open a colorful image or website
4. Do reds and greens look different now?

**If yes:** You're done! The correction is working.

**If no:** Try these adjustments:
- Increase **Intensity** to 100%
- Try a different preset
- See Tutorial 3 for custom settings

---

### Tutorial 2: Compare Before and After

**Goal:** See the difference between corrected and original colors side by side.

---

**Step 1: Set Up Split View**

1. Open ColorBlindnessNG settings (if not already open)
2. Find "Split Mode" at the top
3. Click the dropdown and select **Split Vertical**
4. Your screen is now divided into left and right halves

---

**Step 2: Configure the Left Side (Zone 0)**

1. Find "ZONE 0" in the settings
2. Set **Mode** to "Original"
3. This side will show normal, unchanged colors

---

**Step 3: Configure the Right Side (Zone 1)**

1. Find "ZONE 1" in the settings
2. Set **Mode** to "Correction"
3. Set **Preset** to your color blindness type
4. Click **Apply**

---

**Step 4: Enable Comparison Mode (Optional but Recommended)**

1. Check the "Comparison Mode" checkbox
2. Now both sides show the SAME content
3. A small cursor dot shows where your mouse is on each side

---

**Step 5: Compare!**

1. Open a colorful image (try searching "color blindness test" in Google Images)
2. Look at the left side (original) and right side (corrected)
3. Notice how colors that looked the same on the left look different on the right

---

### Tutorial 3: Creating Custom Corrections

**Goal:** Fine-tune the colors to your specific needs.

---

**Step 1: Start with a Preset**

1. Set Mode to "Correction"
2. Choose the preset closest to your needs
3. Click **Apply**

This gives you a starting point.

---

**Step 2: Understand the Color Controls**

Below the preset, you'll see controls for three color channels:
- **Red Channel** - Controls how red colors are corrected
- **Green Channel** - Controls how green colors are corrected
- **Blue Channel** - Controls how blue colors are corrected

Each channel has these controls:

| Control | What It Does | Example |
|---------|--------------|---------|
| **Enabled** | Turn this channel on/off | ☑ Red enabled |
| **Start Color** | Color output when channel value is 0 | Black (#000000) |
| **End Color** | Color output when channel value is 255 | Cyan (#00FFFF) |
| **Strength** | How much to change (0-100%) | 80% |
| **White Protection** | Prevents white/gray from being tinted | 50% |

---

**Understanding How the LUT (Color Correction) Works**

This is important to understand how the correction actually transforms colors.

A LUT (Look-Up Table) is a 256-color gradient stored in memory. Think of it like a ruler with colors instead of numbers:

```
Index:    0 ──────────────────────────────────► 255
          │                                      │
Color:  START ─────── gradient ─────────────► END
        (black)                              (cyan)
```

**How it transforms a pixel:**

When we process a pixel, we use the **original channel value as an index** into the gradient.

Example: Red channel LUT with Start=Black, End=Cyan

```
Original pixel has Red = 200

Step 1: Take the red value (200)
Step 2: Use it as index into the Red LUT gradient
Step 3: Position 200/255 = 78% through the gradient
Step 4: Get color that's 78% between Black and Cyan
Step 5: Result = (0, 200, 200) - a cyan color
```

**What happens to different red values:**

| Original Red Value | Position in Gradient | Output Color |
|--------------------|---------------------|--------------|
| 0 (no red) | 0% (start) | Black (0,0,0) |
| 64 (weak red) | 25% | Dark Cyan (0,64,64) |
| 128 (medium red) | 50% | Medium Cyan (0,128,128) |
| 200 (strong red) | 78% | Bright Cyan (0,200,200) |
| 255 (maximum red) | 100% (end) | Full Cyan (0,255,255) |

**Why this works for color blindness:**

For someone with Deuteranopia (can't distinguish red from green):
- We create a Red LUT: Black → Cyan
- Strong reds become strong cyans (which contain blue - they CAN see blue)
- Weak reds become weak cyans
- No red stays black (unchanged)

Result: Red objects get a blue/cyan tint **proportional to how red they were**.

**Simple analogy - think of it like a thermometer:**

```
Temperature (input):  Cold ◄─────────────────► Hot
                       0°                      100°
                       │                        │
                       ▼                        ▼
Display color:       Blue ◄─────────────────► Red
                   (Start)                   (End)
```

The temperature doesn't get "replaced" - it just determines which color to show. Same with the LUT - the red value determines where to sample the gradient.

**Why Start Color is usually Black:**

- If Start = Black: pixels with no red stay unchanged
- If Start = Some color: even pixels with NO red get tinted (usually unwanted)

That's why the presets use Black as the Start Color - we only want to affect pixels that actually have red in them.

---

**Step 3: Adjust the Colors**

To change a color:

1. Find the channel you want to adjust (Red, Green, or Blue)
2. Click the colored box next to "Start Color" or "End Color"
3. A color picker window appears
4. Select your desired color
5. Click OK

**Example:** To make red shift toward blue:
- Start Color: Red (#FF0000)
- End Color: Cyan (#00FFFF)

---

**Step 4: Test Your Changes**

1. Open a colorful image
2. Look at how the colors appear
3. Adjust settings if needed:
   - Colors still look the same? → Increase Strength
   - Colors look too weird? → Decrease Strength or increase White Protection
   - Wrong colors affected? → Check which channels are enabled

---

**Step 5: Save Your Custom Preset**

Once you're happy with your settings:

1. Click **Save As...**
2. Enter a name for your preset (e.g., "My Custom Settings")
3. Click OK
4. Your preset now appears in the dropdown with a * symbol

---

### Tutorial 4: Verify Your Correction Works

**Goal:** Confirm that the correction actually helps distinguish colors.

---

**Step 1: Set Up Your Correction**

1. Enable ColorBlindnessNG
2. Set Mode to "Correction"
3. Choose your preset
4. Click Apply

---

**Step 2: Enable Verification**

1. Scroll down in the Zone settings
2. Find "Re-simulate for Verification"
3. Check the checkbox to enable it
4. Set the CVD type to match your preset (e.g., if using Deuteranopia preset, select Deuteranopia)

---

**Step 3: Understand What You're Seeing**

The screen now shows:
1. Your original screen →
2. Color correction applied →
3. Colorblind simulation on top

This simulates what a colorblind person would see AFTER correction is applied.

---

**Step 4: Interpret the Results**

Look at colors that are normally confusing (like red and green):

**Good Result:** The colors look DIFFERENT from each other
- This means the correction is working!
- A colorblind person would be able to tell them apart

**Bad Result:** The colors still look the SAME
- The correction isn't strong enough
- Try: Increase Intensity, increase channel Strength, or try a different preset

---

## Best Colors for Each Type of Color Blindness

### Understanding Why These Colors Work

The goal of correction is to shift confusing colors into a range that colorblind people CAN see. Here's what works best for each type:

---

### Deuteranopia & Deuteranomaly (Green-Blind)

**The Problem:** Red and green look similar (both appear brownish/yellow)

**The Solution:** Add blue to red, so it looks clearly different from green

| Channel | Enable? | Start Color | End Color | Strength | Why It Works |
|---------|---------|-------------|-----------|----------|--------------|
| **Red** | ☑ Yes | Black (#000000) | Cyan (#00FFFF) | 80-100% | Strong reds become cyan (contains blue, which they CAN see) |
| **Green** | ☐ Usually No | - | - | - | Often not needed |
| **Blue** | ☐ No | - | - | - | Blue vision is normal |

**Preset to Use:** Deuteranopia or Deuteranomaly

**Visual Result:**
- Red apples will have a blue/cyan tint
- Green leaves stay mostly green
- Now they look different!

---

### Protanopia & Protanomaly (Red-Blind)

**The Problem:** Red appears very dark (almost black), hard to see against green

**The Solution:** Brighten red and shift it toward blue/cyan

| Channel | Enable? | Start Color | End Color | Strength | Why It Works |
|---------|---------|-------------|-----------|----------|--------------|
| **Red** | ☑ Yes | Black (#000000) | Cyan (#00FFFF) | 80-100% | Strong reds become bright cyan (visible blue added) |
| **Green** | ☐ Optional | Black (#000000) | Yellow (#FFFF00) | 50% | Can help brighten greens |
| **Blue** | ☐ No | - | - | - | Blue vision is normal |

**Preset to Use:** Protanopia or Protanomaly

**Visual Result:**
- Red objects become brighter with cyan/blue tones
- Green stays green or shifts to yellow
- Red no longer looks dark and hidden

---

### Tritanopia & Tritanomaly (Blue-Blind)

**The Problem:** Blue and yellow look similar, purple looks like red

**The Solution:** Shift blue toward a visible range

| Channel | Enable? | Start Color | End Color | Strength | Why It Works |
|---------|---------|-------------|-----------|----------|--------------|
| **Red** | ☐ No | - | - | - | Red vision is normal |
| **Green** | ☐ Optional | Black (#000000) | Cyan (#00FFFF) | 50% | Adds blue distinction |
| **Blue** | ☑ Yes | Black (#000000) | Yellow (#FFFF00) | 80-100% | Strong blues become yellow (visible) |

**Preset to Use:** Tritanopia or Tritanomaly

**Visual Result:**
- Blue objects shift toward visible colors
- Yellow stays visible
- Blue and yellow now look different

---

### Red-Green Combined (Both Channels)

**The Problem:** Both red and green are hard to distinguish

**The Solution:** Correct both channels

| Channel | Enable? | Start Color | End Color | Strength | Why It Works |
|---------|---------|-------------|-----------|----------|--------------|
| **Red** | ☑ Yes | Black (#000000) | Cyan (#00FFFF) | 80% | Strong reds become cyan |
| **Green** | ☑ Yes | Black (#000000) | Magenta (#FF00FF) | 60% | Strong greens become magenta |
| **Blue** | ☐ No | - | - | - | Usually not needed |

**Preset to Use:** Red-Green (Both)

---

### Quick Reference Table

| Your Type | Use Preset | Main Channel | End Color |
|-----------|------------|--------------|-----------|
| Deuteranopia | Deuteranopia | Red | Cyan |
| Deuteranomaly | Deuteranomaly | Red | Cyan (lighter) |
| Protanopia | Protanopia | Red | Cyan |
| Protanomaly | Protanomaly | Red | Cyan (lighter) |
| Tritanopia | Tritanopia | Blue | Yellow |
| Tritanomaly | Tritanomaly | Blue | Yellow (lighter) |
| Not sure | Deuteranopia | Red | Cyan |

---

## Advanced Features

### Re-simulation Mode (New in v1.0.31)

**What it does:** Applies CVD simulation on top of another zone's corrected output. This lets you preview exactly how your color corrections will appear to a colorblind person.

**Why use it:**
- Verify that your corrections actually help distinguish colors
- Design accessible content by seeing the final result through colorblind eyes
- Fine-tune correction settings with immediate visual feedback

**How it works:**
1. Zone A applies **Correction** to the original screen
2. Zone B (in **Re-simulation** mode) takes Zone A's corrected output
3. Zone B applies **Simulation** on top, showing how a colorblind person would see the corrected colors

**How to enable:**
1. Set up at least 2 zones (use Split Vertical, Horizontal, or Quadrants mode)
2. Set one zone to **Correction** mode and configure your correction preset
3. Set another zone to **Re-simulation** mode
4. In the Re-simulation settings:
   - **Source Zone**: Select the zone with Correction (e.g., "Left", "Top-Right")
   - **CVD Type**: Choose the type of color blindness to simulate
   - **Intensity**: How strong the simulation effect is

**Example Setup (Quadrants):**
| Zone | Mode | Purpose |
|------|------|---------|
| Top-Left | Original | See normal colors |
| Top-Right | Correction | Apply your correction |
| Bottom-Left | Simulation | See how colorblind people see the original |
| Bottom-Right | Re-simulation (source: Top-Right) | See how colorblind people see your correction |

**Interpreting Results:**
- **Good:** Bottom-Left shows confused colors, Bottom-Right shows distinguishable colors
- **Bad:** Both bottom zones look the same → Correction isn't helping, try stronger settings

**Important Notes:**
- Re-simulation can only source from zones in **Correction** mode
- If you change a source zone away from Correction mode, you'll see a warning
- Source zone labels use descriptive names (Left/Right, Top/Bottom, etc.) instead of zone numbers

---

### Correction Algorithms

ColorBlindnessNG offers four different correction algorithms, each with its own approach to helping colorblind users distinguish colors. You can choose the algorithm in the **Correction Algorithm** dropdown when in Correction mode.

---

#### 1. LUT-Based Correction (Default)

**What it does:** Uses Look-Up Tables to remap colors channel by channel. This is the most customizable approach.

**How it works:**
- Each color channel (Red, Green, Blue) has its own gradient from a Start Color to an End Color
- When a pixel has red in it, that red value determines where to sample from the Red channel's gradient
- This shifts problematic colors toward more distinguishable ones

**Best for:** Users who want fine control over exactly how each color is transformed.

**Settings:** See the [LUT Color Controls](#step-2-understand-the-color-controls) section for detailed explanation.

---

#### 2. Daltonization

**What it does:** A scientifically-based algorithm that simulates what colors a colorblind person would lose, then redistributes that lost color information to channels they CAN see.

**How it works:**
1. Simulates the colorblind view of the pixel
2. Calculates the "error" (what color information was lost)
3. Adds that lost information back using colors the person can perceive

**Best for:** A balanced, automatic correction that works well for most content without tweaking.

**Settings:**
| Setting | What it does |
|---------|--------------|
| **CVD Type** | Which type of color blindness to correct for |
| **Strength** | How much correction to apply (0-100%) |

---

#### 3. Hue Rotation (New in v1.0.30)

**What it does:** Rotates colors on the "color wheel" so that confusing colors move to positions that are easier to distinguish.

**Think of it like this:** Imagine all colors arranged in a circle (the color wheel). Red-green colorblind people have trouble with colors that are close together on certain parts of the wheel. Hue Rotation simply "spins" those problem colors to a different position where they look different.

```
Before:  Red and Green are both in the "confusing zone"
         ↓
After:   Red is rotated to appear more blue/purple
         Green stays where it is
         Now they look different!
```

**How it works:**
1. Identifies colors in the "problem range" (e.g., reds and greens for deuteranopia)
2. Rotates just those colors by a certain number of degrees
3. Colors outside the problem range stay unchanged

**Best for:** Natural-looking corrections that preserve the overall feel of images while making problem colors distinguishable.

**Simple Mode Settings:**
| Setting | What it does |
|---------|--------------|
| **CVD Type** | Automatically configures which hues to rotate based on your color blindness type |
| **Strength** | How much of the rotation effect to apply (0-100%) |

**Advanced Mode** (check "Advanced Mode" to see these):

| Parameter | Range | What it means |
|-----------|-------|---------------|
| **Source Start** | 0-360° | Where the affected color range begins on the color wheel |
| **Source End** | 0-360° | Where the affected color range ends |
| **Shift** | -180° to +180° | How far to rotate the colors (positive = clockwise, negative = counter-clockwise) |
| **Falloff** | 0.0-1.0 | How soft the boundaries are (0 = sharp cutoff, 1 = very gradual blend) |

**Understanding the Color Wheel (for Advanced Mode):**
```
        Yellow (60°)
            |
Green (120°)-------- Red (0°/360°)
            |
        Blue (240°)
```

**Example for Deuteranopia (green-blind):**
- Source Start: 0° (red)
- Source End: 120° (green)
- Shift: +60° (rotate toward yellow/blue)
- Result: Reds shift toward magenta, greens shift toward cyan

---

#### 4. CIELAB Remapping (New in v1.0.30)

**What it does:** Uses a special color space called CIELAB that's designed to match how humans actually perceive colors. It can transfer color information between different "axes" of perception.

**Understanding CIELAB (simplified):**

Imagine colors have three properties:
- **L (Lightness):** How bright or dark (black to white)
- **a* (Red-Green axis):** How red or green something is
- **b* (Blue-Yellow axis):** How blue or yellow something is

```
                    +a* (Red)
                       |
        +b* (Yellow)---+---−b* (Blue)
                       |
                    −a* (Green)
```

**The Problem:** Colorblind people have trouble seeing differences along certain axes:
- Red-green colorblind: Can't see differences on the a* axis well
- Blue-yellow colorblind: Can't see differences on the b* axis well

**The Solution:** CIELAB Remapping can:
1. **Transfer** information from the axis they can't see to one they can
2. **Enhance** the contrast on certain axes
3. **Encode** color differences as brightness differences (everyone can see brightness!)

**Best for:** Sophisticated corrections that work at a perceptual level, especially good for complex images with subtle color variations.

**Simple Mode Settings:**
| Setting | What it does |
|---------|--------------|
| **CVD Type** | Automatically configures the remapping based on your color blindness type |
| **Strength** | How much of the effect to apply (0-100%) |

**Advanced Mode** (check "Advanced Mode" to see these):

| Parameter | Range | What it means |
|-----------|-------|---------------|
| **A→B Transfer** | -1.0 to +1.0 | Transfer red-green information to the blue-yellow axis. Positive values mean "take what's on the red-green axis and add it to blue-yellow" |
| **B→A Transfer** | -1.0 to +1.0 | Transfer blue-yellow information to the red-green axis |
| **A Enhance** | 0.0 to 2.0 | Amplify the red-green differences. Values >1 increase contrast, <1 reduce it |
| **B Enhance** | 0.0 to 2.0 | Amplify the blue-yellow differences |
| **L Enhance** | 0.0 to 1.0 | Convert color differences into brightness differences. At 1.0, colors that were different will also have different brightness |

**Example Settings for Deuteranopia:**
- A→B Transfer: 0.5 (send half of red-green info to blue-yellow, which they CAN see)
- B→A Transfer: 0.0 (don't transfer back)
- A Enhance: 1.0 (keep red-green as-is)
- B Enhance: 1.2 (slightly boost blue-yellow for extra distinction)
- L Enhance: 0.2 (add a little brightness difference for extra help)

**When to use which algorithm:**

| Situation | Recommended Algorithm |
|-----------|----------------------|
| Want maximum control | LUT-Based |
| Want "set and forget" | Daltonization |
| Natural-looking photos | Hue Rotation |
| Complex graphics/charts | CIELAB Remapping |
| Not sure | Try Daltonization first |

---

### Simulation-Guided Correction

**What it does:** Instead of correcting ALL colors, this feature first checks which pixels would actually be affected by color blindness, then only corrects those specific pixels.

**Why use it:**
- More natural-looking results
- Colors that aren't problematic stay unchanged
- Less "weird" looking screen

**How to enable:**
1. Set zone to "Correction" mode
2. Scroll down to find "Simulation-Guided Correction"
3. Check the checkbox to enable
4. Choose the CVD type to detect
5. Adjust **Sensitivity**:

| Sensitivity | Effect | When to Use |
|-------------|--------|-------------|
| 0.5 - 1.0 | Conservative | Only corrects obviously affected colors |
| 2.0 | Balanced | Good default for most users |
| 3.0 - 5.0 | Aggressive | Corrects even slightly affected colors |

---

### Application Modes

Control HOW the color correction is applied:

| Mode | How It Works | Best For |
|------|--------------|----------|
| **Full Channel** | Corrects any pixel with that color present | Strong, consistent correction |
| **Dominant Only** | Only corrects if that color is the strongest | More subtle, natural look |
| **Threshold** | Only corrects colors above a certain brightness | Ignoring dark/faint colors |

**How to change:**
1. In the zone settings, find "Application Mode"
2. Select your preferred mode from the dropdown

---

### Gradient Types

How colors blend from the start color to the end color:

| Type | Description | Visual Result |
|------|-------------|---------------|
| **Linear RGB** | Simple direct mixing | Fast but can look muddy |
| **Perceptual LAB** | Based on human perception | Smooth, natural transitions |
| **HSL** | Goes around the color wheel | More vibrant colors |

**Recommendation:** Use "Perceptual LAB" for the best-looking results.

---

### Blend Modes

**New in v1.0.29!** Control HOW the LUT correction color blends with the original pixel.

| Mode | How It Works | Best For |
|------|--------------|----------|
| **Channel-Weighted** (default) | Blend amount depends on color intensity | Pure/bright colors (red button, green icon) |
| **Direct** | Full replacement, controlled only by strength | Dark colors, natural photos, green forests |
| **Proportional** | Blend based on channel's relative dominance | Mixed colors where the channel isn't dominant |
| **Additive** | Adds color shift while preserving luminosity | Subtle corrections that keep brightness |
| **Screen** | Brightens colors (like Photoshop screen blend) | Creating lighter, washed-out effects |

**The Problem Blend Modes Solve:**

The original formula (Channel-Weighted) works great for **pure, bright colors** like:
- A red button (RGB: 255, 0, 0) → 100% correction applied
- A green icon (RGB: 0, 255, 0) → 100% correction applied

But it's **weak for dark or mixed colors** like:
- A dark forest green (RGB: 60, 100, 50) → Only 40% correction applied!
- A brown (RGB: 139, 90, 43) → Very little correction

**Solution:** Switch to **Direct** blend mode for photos and natural images.

**How to change:**
1. In the zone settings, find "Blend Mode" dropdown
2. Try "Direct" if colors aren't being corrected enough
3. Experiment with other modes to find what looks best for your content

**Visual Example:**

*Original forest image with dark greens:*
- Channel-Weighted: Green barely changes (correction is weak)
- Direct: Green clearly shifts to cyan (full correction)

---

### Circle and Rectangle Modes

Instead of affecting your whole screen, the effect can follow your mouse:

**Circle Mode:**
- A circular area around your cursor gets the effect
- Adjust **Radius** to change size (50-500 pixels)
- Adjust **Edge Softness** for smooth (1.0) or sharp (0.0) edges

**Rectangle Mode:**
- A rectangular area around your cursor
- Adjust **Width** and **Height** separately
- Check **Square** to make width = height

**When to use:**
- Quick color checking without affecting whole screen
- Comparing colors by moving mouse over them
- Less visually intrusive than fullscreen

---

### White Protection

Prevents neutral colors (white, gray, black) from getting tinted.

**The Problem:** Without white protection, white paper might look slightly cyan or magenta.

**The Solution:** White Protection slider (0.01 to 1.0)
- Low (0.01-0.2): Minimal protection, more correction
- Medium (0.3-0.5): Balanced (recommended)
- High (0.6-1.0): Strong protection, whites stay white

---

### Hotkeys (Keyboard Shortcuts)

| Shortcut | What It Does |
|----------|--------------|
| **Alt+Shift+M** | Turn ColorBlindnessNG on/off quickly |
| **Alt+Shift+L** | Open/close the settings window |

**To enable/disable hotkeys:**
1. Open ColorBlindnessNG settings
2. Find the "Hotkeys" section
3. Check or uncheck each hotkey

---

## Frequently Asked Questions

### Q: Which preset should I choose?

**A:** If you know your type of color blindness, choose that preset. If you're not sure:

1. Try **Deuteranopia** first (it's the most common type)
2. If it doesn't help, try **Protanopia**
3. If you have trouble with blue/yellow, try **Tritanopia**

You can also take an online color blindness test to find out your type.

---

### Q: The colors look too weird/strong

**A:** Try these adjustments:

1. **Reduce Intensity** - Move the slider to 60-70%
2. **Increase White Protection** - Move to 0.3-0.5
3. **Enable Simulation-Guided** - Only corrects affected colors
4. **Use Dominant Only mode** - More subtle correction

---

### Q: Can I use this all day?

**A:** Absolutely! Many users keep it enabled permanently. Tips for daily use:

- Use **Fullscreen** mode
- Set **Intensity** to a comfortable level (you'll get used to it)
- Use **Alt+Shift+M** to quickly toggle if needed
- Save your preferred settings as a custom preset

---

### Q: The effect disappeared after restarting

**A:** MouseEffects should remember your settings. If it doesn't:

1. Make sure to close MouseEffects properly (right-click tray → Exit)
2. Wait a moment after changing settings before closing
3. Check that your preset is still selected

---

### Q: Can I share my settings with someone else?

**A:** Yes! Use Export/Import:

**To share your settings:**
1. Save your settings as a custom preset
2. Click **Export**
3. Choose where to save the .json file
4. Send this file to your friend

**To use someone else's settings:**
1. Click **Import**
2. Find the .json file they sent
3. The preset will be added to your list

---

### Q: What's the difference between "Color Blindness" and "Color Blindness NG"?

**A:** ColorBlindnessNG is the newer, better version:

| Feature | Color Blindness | Color Blindness NG |
|---------|-----------------|-------------------|
| Per-zone settings | Limited | Full control |
| Custom presets | No | Yes, with export/import |
| Correction algorithms | 1 (Daltonization) | 4 (LUT, Daltonization, Hue Rotation, CIELAB) |
| Simulation-Guided | No | Yes |
| Re-simulation mode | No | Yes (preview corrections through colorblind eyes) |
| Shape modes | Basic | Circle & Rectangle |

**Recommendation:** Use Color Blindness NG for the best experience.

---

### Q: It's not working at all

**A:** Troubleshooting steps:

1. **Is the plugin enabled?** Check the checkbox next to "Color Blindness NG"
2. **Is the zone in Correction mode?** Check the Mode dropdown
3. **Is Intensity above 0?** Move the slider up
4. **Is a preset selected?** Choose one from the dropdown and click Apply
5. **Try restarting** - Close and reopen MouseEffects

---

## Quick Start Checklist

Use this checklist for a quick setup:

- [ ] Download MouseEffects from GitHub releases
- [ ] Run the installer (MouseEffects-win-Setup.exe)
- [ ] Find the tray icon (bottom-right, near clock)
- [ ] Right-click → Settings
- [ ] Enable "Color Blindness NG" (checkbox)
- [ ] Expand the settings (click the name)
- [ ] Set Mode to "Correction"
- [ ] Choose your Preset (Deuteranopia if unsure)
- [ ] Click "Apply"
- [ ] Test with a colorful image
- [ ] Adjust Intensity if needed

**Done!** You now have color correction enabled.

---

## Getting Help

If you need more assistance:

- **Technical Documentation:** [Plugins Reference](Plugins.md#color-blindness-ng)
- **Feature Overview:** [Features](Features.md#color-vision-accessibility-colorblindnessng)
- **Report Issues:** [GitHub Issues](https://github.com/LeCaiss662/MouseEffects/issues)

---

*This guide was created to help everyone use ColorBlindnessNG, regardless of technical experience. If something is unclear, please let us know!*

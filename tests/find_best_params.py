"""
Color Blindness Parameter Optimizer

This script tests different parameters for color blindness simulation and correction
to find the best values that make Ishihara test numbers readable.

Usage:
    python find_best_params.py

Requirements:
    pip install pillow numpy
"""

import numpy as np
from PIL import Image
import os
from itertools import product

# Output directory
OUTPUT_DIR = "Results/ParameterTests"

# ============================================================================
# Color Space Conversion Matrices
# ============================================================================

# sRGB to Linear RGB
def srgb_to_linear(c):
    """Convert sRGB to linear RGB"""
    return np.where(c <= 0.04045, c / 12.92, np.power((c + 0.055) / 1.055, 2.4))

def linear_to_srgb(c):
    """Convert linear RGB to sRGB"""
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(np.clip(c, 0.0001, 1.0), 1/2.4) - 0.055)

# RGB to LMS (Smith & Pokorny)
RGB_TO_LMS = np.array([
    [0.31399022, 0.63951294, 0.04649755],
    [0.15537241, 0.75789446, 0.08670142],
    [0.01775239, 0.10944209, 0.87256922]
])

# LMS to RGB (inverse)
LMS_TO_RGB = np.array([
    [ 5.47221206, -4.64196010,  0.16963708],
    [-1.12524190,  2.29317094, -0.16789520],
    [ 0.02980165, -0.19318073,  1.16364789]
])

def rgb_to_lms(rgb):
    """Convert linear RGB to LMS"""
    return np.dot(rgb, RGB_TO_LMS.T)

def lms_to_rgb(lms):
    """Convert LMS to linear RGB"""
    return np.dot(lms, LMS_TO_RGB.T)

# ============================================================================
# Simulation Functions
# ============================================================================

def simulate_protanopia_strict(linear_rgb, params=None):
    """
    Simulate protanopia using LMS color space.
    L' = min(L, M) - only reduce L, never increase for greens/blues
    """
    lms = rgb_to_lms(linear_rgb)
    sim_lms = lms.copy()
    # L' = min(L, M)
    sim_lms[..., 0] = np.minimum(lms[..., 0], lms[..., 1])
    return lms_to_rgb(sim_lms)

def simulate_protanopia_machado(linear_rgb):
    """Simulate protanopia using Machado matrix (operates on RGB directly)"""
    machado_matrix = np.array([
        [0.152286, 1.052583, -0.204868],
        [0.114503, 0.786281, 0.099216],
        [-0.003882, -0.048116, 1.051998]
    ])
    return np.dot(linear_rgb, machado_matrix.T)

# ============================================================================
# Correction Functions
# ============================================================================

def correct_protanopia(linear_rgb, params):
    """
    Apply protanopia correction with adjustable parameters.

    params dict:
        - redness_threshold: threshold for detecting red colors
        - blue_strength: how much blue to add to reds
        - green_boost: optional boost to greens
    """
    redness_threshold = params.get('redness_threshold', 0.0)
    blue_strength = params.get('blue_strength', 0.8)

    correction = np.zeros_like(linear_rgb)

    # Calculate redness: R - G
    redness = linear_rgb[..., 0] - linear_rgb[..., 1]

    # Only correct where redness > threshold
    mask = redness > redness_threshold

    # Add blue to red colors to make them magenta/pink
    correction[..., 2] = np.where(mask, blue_strength * redness, 0)

    corrected = linear_rgb + correction
    return np.clip(corrected, 0, 1)

def correct_protanopia_v2(linear_rgb, params):
    """
    Alternative correction: shift reds toward magenta, greens toward cyan

    params dict:
        - red_blue_add: blue to add to reds
        - green_blue_add: blue to add to greens
        - green_red_sub: red to subtract from greens
    """
    red_blue_add = params.get('red_blue_add', 0.8)
    green_blue_add = params.get('green_blue_add', 0.3)
    green_red_sub = params.get('green_red_sub', 0.2)

    correction = np.zeros_like(linear_rgb)

    r, g, b = linear_rgb[..., 0], linear_rgb[..., 1], linear_rgb[..., 2]

    # Redness: how much more red than green
    redness = np.maximum(0, r - g)

    # Greenness: how much more green than red (and not too blue)
    greenness = np.maximum(0, g - np.maximum(r * 0.8, b))

    # Apply corrections
    correction[..., 2] += red_blue_add * redness      # Reds -> magenta
    correction[..., 2] += green_blue_add * greenness  # Greens -> cyan
    correction[..., 0] -= green_red_sub * greenness   # Reduce red in greens

    corrected = linear_rgb + correction
    return np.clip(corrected, 0, 1)

def correct_protanopia_v3(linear_rgb, params):
    """
    Strong correction optimized for Ishihara tests.
    Shift reds strongly toward blue/magenta, keep greens distinct.
    """
    red_to_blue = params.get('red_to_blue', 1.0)
    red_to_green = params.get('red_to_green', 0.0)
    green_to_blue = params.get('green_to_blue', 0.5)
    saturation_boost = params.get('saturation_boost', 1.0)

    correction = np.zeros_like(linear_rgb)

    r, g, b = linear_rgb[..., 0], linear_rgb[..., 1], linear_rgb[..., 2]

    # Detect red-ish colors (R > G and R > B*1.5)
    is_reddish = (r > g) & (r > b * 1.5)
    redness = np.where(is_reddish, r - g, 0)

    # Detect green-ish colors (G > R*0.8 and G > B)
    is_greenish = (g > r * 0.8) & (g > b)
    greenness = np.where(is_greenish, g - np.maximum(r, b), 0)

    # Shift reds toward blue/magenta
    correction[..., 2] += red_to_blue * redness
    correction[..., 1] += red_to_green * redness

    # Optionally shift greens toward cyan
    correction[..., 2] += green_to_blue * greenness

    corrected = linear_rgb + correction
    return np.clip(corrected, 0, 1)

# ============================================================================
# Image Processing
# ============================================================================

def process_image(img_array, simulation_func, correction_func, sim_params=None, corr_params=None):
    """Process an image with simulation and correction"""
    # Normalize to 0-1
    img_float = img_array.astype(np.float32) / 255.0

    # Convert to linear RGB
    linear = srgb_to_linear(img_float)

    # Apply simulation
    if simulation_func:
        simulated = simulation_func(linear, sim_params)
        simulated = np.clip(simulated, 0, 1)
    else:
        simulated = linear

    # Apply correction
    if correction_func:
        corrected = correction_func(linear, corr_params or {})
    else:
        corrected = linear

    # Convert back to sRGB
    sim_srgb = linear_to_srgb(simulated)
    corr_srgb = linear_to_srgb(corrected)

    # Convert to uint8
    sim_result = (np.clip(sim_srgb, 0, 1) * 255).astype(np.uint8)
    corr_result = (np.clip(corr_srgb, 0, 1) * 255).astype(np.uint8)

    return sim_result, corr_result

def create_comparison_grid(original, simulated, corrected, title=""):
    """Create a 2x2 comparison grid"""
    h, w = original.shape[:2]

    # Create grid: Original | Corrected
    #              Simulated | Sim+Corr (what colorblind sees after correction)
    grid = np.zeros((h * 2, w * 2, 3), dtype=np.uint8)

    grid[:h, :w] = original
    grid[:h, w:] = corrected
    grid[h:, :w] = simulated

    # Apply simulation to corrected image to show what colorblind person sees
    corr_float = corrected.astype(np.float32) / 255.0
    corr_linear = srgb_to_linear(corr_float)
    sim_corr = simulate_protanopia_strict(corr_linear)
    sim_corr = np.clip(sim_corr, 0, 1)
    sim_corr_srgb = linear_to_srgb(sim_corr)
    sim_corr_result = (np.clip(sim_corr_srgb, 0, 1) * 255).astype(np.uint8)

    grid[h:, w:] = sim_corr_result

    return grid

# ============================================================================
# Parameter Search
# ============================================================================

def test_parameters():
    """Test various parameter combinations for protanopia correction"""

    # Load test image
    img_path = "extended-ishihara-color-blindness-test.jpg"
    if not os.path.exists(img_path):
        print(f"Error: {img_path} not found!")
        return

    img = Image.open(img_path).convert('RGB')
    img_array = np.array(img)

    # Create output directory
    os.makedirs(OUTPUT_DIR, exist_ok=True)

    print("Testing Protanopia Correction Parameters...")
    print("=" * 60)

    # Parameter sets to test for V3 correction
    param_sets = [
        # (red_to_blue, red_to_green, green_to_blue, name)
        (0.5, 0.0, 0.0, "weak_red_shift"),
        (0.8, 0.0, 0.0, "medium_red_shift"),
        (1.0, 0.0, 0.0, "strong_red_shift"),
        (1.2, 0.0, 0.0, "very_strong_red_shift"),
        (1.5, 0.0, 0.0, "extreme_red_shift"),

        (0.8, 0.0, 0.3, "red_shift_with_green_cyan"),
        (1.0, 0.0, 0.5, "strong_red_green_cyan"),
        (1.2, 0.0, 0.5, "very_strong_both"),

        (0.8, 0.2, 0.0, "red_to_magenta"),
        (1.0, 0.3, 0.0, "strong_red_to_magenta"),

        (1.0, 0.2, 0.3, "balanced_correction"),
        (1.2, 0.2, 0.4, "strong_balanced"),
        (1.5, 0.3, 0.5, "maximum_correction"),
    ]

    # Generate original and simulated once
    img_float = img_array.astype(np.float32) / 255.0
    linear = srgb_to_linear(img_float)

    simulated = simulate_protanopia_strict(linear)
    simulated = np.clip(simulated, 0, 1)
    sim_srgb = linear_to_srgb(simulated)
    sim_result = (np.clip(sim_srgb, 0, 1) * 255).astype(np.uint8)

    # Save simulation result
    sim_img = Image.fromarray(sim_result)
    sim_img.save(os.path.join(OUTPUT_DIR, "00_simulation_protanopia.jpg"), quality=95)
    print("Saved: 00_simulation_protanopia.jpg")

    # Test each parameter set
    for i, (r2b, r2g, g2b, name) in enumerate(param_sets):
        params = {
            'red_to_blue': r2b,
            'red_to_green': r2g,
            'green_to_blue': g2b,
        }

        corrected = correct_protanopia_v3(linear, params)
        corr_srgb = linear_to_srgb(corrected)
        corr_result = (np.clip(corr_srgb, 0, 1) * 255).astype(np.uint8)

        # Create comparison grid
        grid = create_comparison_grid(img_array, sim_result, corr_result)

        # Save
        filename = f"{i+1:02d}_protanopia_{name}_r2b{r2b}_r2g{r2g}_g2b{g2b}.jpg"
        grid_img = Image.fromarray(grid)
        grid_img.save(os.path.join(OUTPUT_DIR, filename), quality=95)

        print(f"Saved: {filename}")
        print(f"  red_to_blue={r2b}, red_to_green={r2g}, green_to_blue={g2b}")

    print("\n" + "=" * 60)
    print(f"Results saved to: {OUTPUT_DIR}/")
    print("\nGrid layout:")
    print("  Top-Left: Original")
    print("  Top-Right: Corrected")
    print("  Bottom-Left: Simulated (what colorblind sees)")
    print("  Bottom-Right: Simulated+Corrected (what colorblind sees after correction)")
    print("\nThe BEST parameters are those where Bottom-Right shows visible numbers!")

def test_simulation_parameters():
    """Test various simulation parameters"""

    img_path = "extended-ishihara-color-blindness-test.jpg"
    if not os.path.exists(img_path):
        print(f"Error: {img_path} not found!")
        return

    img = Image.open(img_path).convert('RGB')
    img_array = np.array(img)

    os.makedirs(OUTPUT_DIR, exist_ok=True)

    print("\nTesting Protanopia Simulation Parameters...")
    print("=" * 60)

    img_float = img_array.astype(np.float32) / 255.0
    linear = srgb_to_linear(img_float)

    # Test different simulation approaches
    simulations = [
        ("min_L_M", lambda rgb, p: simulate_min_lm(rgb)),
        ("machado", lambda rgb, p: simulate_protanopia_machado(rgb)),
        ("blend_50", lambda rgb, p: simulate_blend(rgb, 0.5)),
        ("blend_70", lambda rgb, p: simulate_blend(rgb, 0.7)),
        ("blend_100", lambda rgb, p: simulate_blend(rgb, 1.0)),
    ]

    for name, sim_func in simulations:
        try:
            simulated = sim_func(linear, None)
            simulated = np.clip(simulated, 0, 1)
            sim_srgb = linear_to_srgb(simulated)
            sim_result = (np.clip(sim_srgb, 0, 1) * 255).astype(np.uint8)

            filename = f"sim_protanopia_{name}.jpg"
            sim_img = Image.fromarray(sim_result)
            sim_img.save(os.path.join(OUTPUT_DIR, filename), quality=95)
            print(f"Saved: {filename}")
        except Exception as e:
            print(f"Error with {name}: {e}")

def simulate_min_lm(linear_rgb):
    """L' = min(L, M)"""
    lms = rgb_to_lms(linear_rgb)
    sim_lms = lms.copy()
    sim_lms[..., 0] = np.minimum(lms[..., 0], lms[..., 1])
    return lms_to_rgb(sim_lms)

def simulate_blend(linear_rgb, strength):
    """Blend original L with M based on strength"""
    lms = rgb_to_lms(linear_rgb)
    sim_lms = lms.copy()
    # L' = lerp(L, M, strength) but only reduce, don't increase
    new_l = lms[..., 0] * (1 - strength) + lms[..., 1] * strength
    sim_lms[..., 0] = np.minimum(lms[..., 0], new_l)
    return lms_to_rgb(sim_lms)

# ============================================================================
# Main
# ============================================================================

if __name__ == "__main__":
    print("Color Blindness Parameter Optimizer")
    print("=" * 60)

    # Change to script directory
    script_dir = os.path.dirname(os.path.abspath(__file__))
    os.chdir(script_dir)

    # Test correction parameters
    test_parameters()

    # Test simulation parameters
    test_simulation_parameters()

    print("\nDone! Check the Results/ParameterTests folder for outputs.")

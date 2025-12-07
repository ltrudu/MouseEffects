using System.Diagnostics;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Time;
using MouseEffects.DirectX.Graphics;

namespace MouseEffects.App;

/// <summary>
/// Fixed timestep game loop for updating and rendering effects.
/// </summary>
public sealed class GameLoop : IDisposable
{
    private const double MaxAccumulatedTime = 0.25; // Prevent spiral of death

    private readonly Stopwatch _stopwatch;
    private readonly OverlayManager _overlayManager;
    private readonly EffectManager _effectManager;
    private readonly IMouseInputProvider _mouseInput;

    private double _targetFrameRate = 60.0;
    private double _fixedTimestep = 1.0 / 60.0;
    private double _accumulator;
    private double _previousTime;
    private bool _running;
    private bool _disposed;
    private bool _wasAnyEffectEnabled;
    private double _lastTopmostEnforceTime;
    private const double TopmostEnforceInterval = 0.5; // Enforce topmost every 0.5 seconds

    public bool IsRunning => _running;
    public double CurrentFps { get; private set; }
    public double CaptureFps { get; private set; }

    /// <summary>
    /// Enable/disable capture FPS tracking. Only enable when FPS overlay is visible.
    /// </summary>
    public void SetTrackCaptureFps(bool enabled)
    {
        foreach (var overlay in _overlayManager.Overlays)
        {
            if (overlay.RenderContext is D3D11RenderContext d3dContext)
            {
                d3dContext.TrackCaptureFps = enabled;
            }
        }
    }

    /// <summary>
    /// Gets or sets the target frame rate (30-120 fps).
    /// </summary>
    public int TargetFrameRate
    {
        get => (int)_targetFrameRate;
        set
        {
            _targetFrameRate = Math.Clamp(value, 30, 120);
            _fixedTimestep = 1.0 / _targetFrameRate;
        }
    }

    public GameLoop(
        OverlayManager overlayManager,
        EffectManager effectManager,
        IMouseInputProvider mouseInput)
    {
        _overlayManager = overlayManager;
        _effectManager = effectManager;
        _mouseInput = mouseInput;
        _stopwatch = new Stopwatch();
    }

    /// <summary>
    /// Start the game loop.
    /// </summary>
    public void Start()
    {
        if (_running) return;

        _running = true;
        _stopwatch.Start();
        _previousTime = _stopwatch.Elapsed.TotalSeconds;
        _accumulator = 0;

        _mouseInput.Start();
    }

    /// <summary>
    /// Stop the game loop.
    /// </summary>
    public void Stop()
    {
        _running = false;
        _stopwatch.Stop();
        _mouseInput.Stop();
    }

    /// <summary>
    /// Process one iteration of the game loop.
    /// Called from the message pump.
    /// </summary>
    public void Tick()
    {
        if (!_running) return;

        var currentTime = _stopwatch.Elapsed.TotalSeconds;
        var frameTime = currentTime - _previousTime;
        _previousTime = currentTime;

        // Check if any effect is enabled
        var hasAnyEffectEnabled = _effectManager.HasAnyEffectEnabled();

        // Handle transition from enabled to disabled - clear overlay once
        if (!hasAnyEffectEnabled && _wasAnyEffectEnabled)
        {
            ClearOverlays();
            _wasAnyEffectEnabled = false;
        }

        if (!hasAnyEffectEnabled)
        {
            // Reset FPS when no effects are active
            CurrentFps = 0;
            CaptureFps = 0;
            _accumulator = 0;

            // Still need to clear input state
            if (_mouseInput is Input.GlobalMouseHook hook)
            {
                hook.EndFrame();
            }
            return;
        }

        _wasAnyEffectEnabled = true;

        // Clamp frame time to prevent spiral of death
        if (frameTime > MaxAccumulatedTime)
        {
            frameTime = MaxAccumulatedTime;
        }

        _accumulator += frameTime;

        // Calculate FPS
        if (frameTime > 0)
        {
            CurrentFps = 1.0 / frameTime;
        }

        // Get current mouse state
        var mouseState = _mouseInput.CurrentState;

        // Fixed timestep updates
        while (_accumulator >= _fixedTimestep)
        {
            var gameTime = new GameTime
            {
                TotalTime = TimeSpan.FromSeconds(currentTime),
                DeltaTime = TimeSpan.FromSeconds(_fixedTimestep)
            };

            _effectManager.Update(gameTime, mouseState);
            _accumulator -= _fixedTimestep;
        }

        // Render
        Render();

        // Periodically enforce topmost state to prevent other windows from stealing it
        if (currentTime - _lastTopmostEnforceTime >= TopmostEnforceInterval)
        {
            _overlayManager.EnforceTopmost();
            _lastTopmostEnforceTime = currentTime;
        }

        // Clear per-frame input state
        if (_mouseInput is Input.GlobalMouseHook hook2)
        {
            hook2.EndFrame();
        }
    }

    private void Render()
    {
        // Check if any effect needs screen capture
        var needsScreenCapture = _effectManager.RequiresContinuousScreenCapture();

        foreach (var overlay in _overlayManager.Overlays)
        {
            var renderContext = overlay.RenderContext;

            // Set continuous capture mode based on effect requirements
            renderContext.ContinuousCaptureMode = needsScreenCapture;

            // Only capture screen if an effect needs it
            overlay.BeginFrame(captureScreen: needsScreenCapture);
            _effectManager.Render(renderContext);
            overlay.EndFrame();

            // Update capture FPS from first overlay
            if (renderContext is D3D11RenderContext d3dContext)
            {
                CaptureFps = needsScreenCapture ? d3dContext.CaptureFps : 0;
            }
        }
    }

    /// <summary>
    /// Clear all overlays by rendering an empty transparent frame.
    /// </summary>
    private void ClearOverlays()
    {
        foreach (var overlay in _overlayManager.Overlays)
        {
            // BeginFrame clears to transparent, EndFrame presents
            overlay.BeginFrame(captureScreen: false);
            overlay.EndFrame();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Stop();
    }
}

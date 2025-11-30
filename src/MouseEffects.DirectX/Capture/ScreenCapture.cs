using System.Runtime.InteropServices;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using MouseEffects.DirectX.Graphics;
using MouseEffects.Core.Diagnostics;

namespace MouseEffects.DirectX.Capture;

/// <summary>
/// Captures the desktop screen using DXGI Desktop Duplication API.
/// Provides a texture that can be sampled by effects for screen distortion.
/// </summary>
public sealed partial class ScreenCapture : IDisposable
{
    [LibraryImport("dwmapi.dll")]
    private static partial int DwmFlush();

    private readonly D3D11GraphicsDevice _renderDevice;
    private ID3D11Device? _captureDevice;  // May be different from render device on hybrid GPU
    private ID3D11DeviceContext? _captureContext;
    private IDXGIOutputDuplication? _duplication;
    private ID3D11Texture2D? _capturedTexture;      // On capture device
    private ID3D11Texture2D? _stagingTexture;       // For cross-device copy
    private ID3D11Texture2D? _renderTexture;        // On render device
    private ID3D11ShaderResourceView? _srv;
    private bool _disposed;
    private bool _initialized;
    private bool _crossDeviceCopy;  // True if capture and render are different devices

    public int Width { get; private set; }
    public int Height { get; private set; }
    public ID3D11ShaderResourceView? ShaderResourceView => _srv;
    public bool IsAvailable => _initialized && _srv != null;

    public ScreenCapture(D3D11GraphicsDevice device)
    {
        _renderDevice = device;
    }

    /// <summary>
    /// Initialize screen capture for a specific output (monitor).
    /// </summary>
    public bool Initialize(int outputIndex = 0)
    {
        try
        {
            Log("Initializing screen capture...");

            // On hybrid GPU laptops (e.g., Optimus), the discrete GPU may have no outputs.
            // We need to find an adapter with outputs for desktop duplication.
            using var factory = DXGI.CreateDXGIFactory1<IDXGIFactory1>();

            IDXGIOutput1? output1 = null;
            IDXGIAdapter1? outputAdapter = null;
            bool useRenderAdapter = false;

            // First try the rendering adapter
            using var renderAdapter = _renderDevice.Device.QueryInterface<IDXGIDevice>().GetAdapter();
            var renderAdapterDesc = renderAdapter.Description;
            var result = renderAdapter.EnumOutputs((uint)outputIndex, out IDXGIOutput? output);

            if (result.Success && output != null)
            {
                output1 = output.QueryInterface<IDXGIOutput1>();
                output.Dispose();
                useRenderAdapter = true;
                Log($"Found output on rendering adapter: {renderAdapterDesc.Description}");
            }
            else
            {
                Log($"No output on rendering adapter ({renderAdapterDesc.Description}), scanning all adapters...");

                // Scan all adapters for outputs (needed for hybrid GPU laptops)
                for (uint adapterIdx = 0; factory.EnumAdapters1(adapterIdx, out var adapter).Success; adapterIdx++)
                {
                    var adapterDesc = adapter.Description1;
                    Log($"Checking adapter {adapterIdx}: {adapterDesc.Description}");

                    var outputResult = adapter.EnumOutputs((uint)outputIndex, out output);
                    if (outputResult.Success && output != null)
                    {
                        output1 = output.QueryInterface<IDXGIOutput1>();
                        output.Dispose();
                        outputAdapter = adapter;
                        Log($"Found output {outputIndex} on adapter: {adapterDesc.Description}");
                        break;
                    }
                    adapter.Dispose();
                }
            }

            if (output1 == null)
            {
                Log($"Failed to find output {outputIndex} on any adapter");
                return false;
            }

            var outputDesc = output1.Description;
            Width = outputDesc.DesktopCoordinates.Right - outputDesc.DesktopCoordinates.Left;
            Height = outputDesc.DesktopCoordinates.Bottom - outputDesc.DesktopCoordinates.Top;
            Log($"Output size: {Width}x{Height}");

            // Determine which device to use for capture
            ID3D11Device captureDevice;
            if (useRenderAdapter)
            {
                // Output is on the render adapter - use the render device directly
                captureDevice = _renderDevice.Device;
                _crossDeviceCopy = false;
                Log("Using render device for capture (same adapter)");
            }
            else if (outputAdapter != null)
            {
                // Output is on a different adapter - create a device for capture
                Log("Creating separate capture device for hybrid GPU configuration...");
                var featureLevels = new FeatureLevel[] { FeatureLevel.Level_11_1, FeatureLevel.Level_11_0 };
                D3D11.D3D11CreateDevice(
                    outputAdapter,
                    DriverType.Unknown,
                    DeviceCreationFlags.BgraSupport,
                    featureLevels,
                    out _captureDevice,
                    out _captureContext);

                captureDevice = _captureDevice!;
                _crossDeviceCopy = true;
                Log($"Created capture device on: {outputAdapter.Description1.Description}");
                outputAdapter.Dispose();
            }
            else
            {
                Log("Failed to determine capture adapter");
                output1.Dispose();
                return false;
            }

            // Create duplication
            try
            {
                _duplication = output1.DuplicateOutput(captureDevice);
                Log("Desktop duplication created successfully");
            }
            catch (SharpGen.Runtime.SharpGenException ex)
            {
                Log($"Failed to create duplication: {ex.Message}");
                Log("Note: Desktop duplication may fail in secure desktop mode or if another app is using it");
                output1.Dispose();
                return false;
            }

            output1.Dispose();

            // Create textures
            var textureDesc = new Texture2DDescription
            {
                Width = (uint)Width,
                Height = (uint)Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.B8G8R8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.ShaderResource,
                CPUAccessFlags = CpuAccessFlags.None
            };

            if (_crossDeviceCopy)
            {
                // Need staging texture for CPU copy between devices
                _capturedTexture = captureDevice.CreateTexture2D(textureDesc);

                var stagingDesc = textureDesc;
                stagingDesc.Usage = ResourceUsage.Staging;
                stagingDesc.BindFlags = BindFlags.None;
                stagingDesc.CPUAccessFlags = CpuAccessFlags.Read;
                _stagingTexture = captureDevice.CreateTexture2D(stagingDesc);

                // Render texture is on the main render device
                var renderDesc = textureDesc;
                renderDesc.Usage = ResourceUsage.Dynamic;
                renderDesc.CPUAccessFlags = CpuAccessFlags.Write;
                _renderTexture = _renderDevice.Device.CreateTexture2D(renderDesc);
                _srv = _renderDevice.Device.CreateShaderResourceView(_renderTexture);

                Log("Created cross-device textures for hybrid GPU");
            }
            else
            {
                // Same device - simple case
                _capturedTexture = captureDevice.CreateTexture2D(textureDesc);
                _srv = _renderDevice.Device.CreateShaderResourceView(_capturedTexture);
                Log("Created textures on render device");
            }

            _initialized = true;
            Log("Screen capture initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Log($"Screen capture initialization failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// When true, uses a longer timeout to wait for new frames.
    /// Enable this when effects need continuously updated screen content.
    /// </summary>
    public bool ContinuousCaptureMode { get; set; }

    /// <summary>
    /// Capture the current frame. Call this each frame before rendering effects that need screen data.
    /// </summary>
    public bool CaptureFrame()
    {
        if (!_initialized || _duplication == null || _capturedTexture == null)
            return false;

        IDXGIResource? resource = null;
        try
        {
            // In continuous mode, flush DWM to ensure we get the latest composed frame
            // This is important during window dragging and system animations
            if (ContinuousCaptureMode)
            {
                DwmFlush();
            }

            // Use longer timeout in continuous mode to wait for new frames
            // 16ms timeout ~= 60fps, 0ms = don't wait (use previous frame if no new one)
            uint timeout = ContinuousCaptureMode ? 16u : 0u;
            var result = _duplication.AcquireNextFrame(timeout, out var frameInfo, out resource);

            if (!result.Success || resource == null)
            {
                // No new frame available - that's OK, use previous capture
                return _srv != null;
            }

            // Optimization: Check if frame actually has updates
            // TotalMetadataBufferSize > 0 means there are dirty regions or mouse updates
            // AccumulatedFrames > 0 means actual frame content changed
            if (frameInfo.TotalMetadataBufferSize == 0 && frameInfo.AccumulatedFrames == 0)
            {
                // Frame acquired but no actual changes - skip expensive copy
                return _srv != null;
            }

            // Copy the desktop texture to our captured texture
            using var desktopTexture = resource.QueryInterface<ID3D11Texture2D>();

            if (_crossDeviceCopy && _captureContext != null && _stagingTexture != null && _renderTexture != null)
            {
                // Cross-device copy: capture device -> staging (CPU readable) -> render device
                _captureContext.CopyResource(_capturedTexture, desktopTexture);
                _captureContext.CopyResource(_stagingTexture, _capturedTexture);

                // Map staging texture on capture device
                var mapped = _captureContext.Map(_stagingTexture, 0, MapMode.Read);
                try
                {
                    // Map render texture on render device
                    var destMapped = _renderDevice.Context.Map(_renderTexture, 0, MapMode.WriteDiscard);
                    try
                    {
                        var srcPtr = mapped.DataPointer;
                        var dstPtr = destMapped.DataPointer;
                        var rowSize = Width * 4; // BGRA = 4 bytes per pixel

                        // Optimization: bulk copy when row pitches match (common case)
                        if (mapped.RowPitch == destMapped.RowPitch)
                        {
                            // Single bulk copy - much faster than row-by-row
                            var totalSize = (long)mapped.RowPitch * Height;
                            unsafe
                            {
                                Buffer.MemoryCopy((void*)srcPtr, (void*)dstPtr, totalSize, totalSize);
                            }
                        }
                        else
                        {
                            // Row-by-row copy when pitches differ
                            for (int y = 0; y < Height; y++)
                            {
                                unsafe
                                {
                                    Buffer.MemoryCopy(
                                        (void*)(srcPtr + y * (int)mapped.RowPitch),
                                        (void*)(dstPtr + y * (int)destMapped.RowPitch),
                                        rowSize, rowSize);
                                }
                            }
                        }
                    }
                    finally
                    {
                        _renderDevice.Context.Unmap(_renderTexture, 0);
                    }
                }
                finally
                {
                    _captureContext.Unmap(_stagingTexture, 0);
                }
            }
            else
            {
                // Same device - simple copy
                _renderDevice.Context.CopyResource(_capturedTexture, desktopTexture);
            }

            return true;
        }
        catch (SharpGen.Runtime.SharpGenException)
        {
            // Frame acquisition failed - try to reinitialize
            // This can happen when display settings change
            return false;
        }
        finally
        {
            resource?.Dispose();
            if (resource != null)
            {
                try { _duplication?.ReleaseFrame(); } catch { }
            }
        }
    }

    private static void Log(string message) => Logger.Log("ScreenCapture", message);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _srv?.Dispose();
        _renderTexture?.Dispose();
        _stagingTexture?.Dispose();
        _capturedTexture?.Dispose();
        _duplication?.Dispose();
        _captureContext?.Dispose();
        _captureDevice?.Dispose();
    }
}

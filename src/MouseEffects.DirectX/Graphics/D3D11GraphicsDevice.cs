using MouseEffects.Core.Diagnostics;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace MouseEffects.DirectX.Graphics;

/// <summary>
/// Wrapper around Direct3D 11 device and context.
/// </summary>
public sealed class D3D11GraphicsDevice : IDisposable
{
    private bool _disposed;
    private readonly IDXGIAdapter1? _adapter;

    public ID3D11Device Device { get; }
    public ID3D11DeviceContext Context { get; }
    public IDXGIFactory2 Factory { get; }
    public FeatureLevel FeatureLevel { get; }
    public string AdapterName { get; }

    /// <summary>
    /// Get list of available GPU adapters.
    /// </summary>
    public static List<GpuInfo> GetAvailableGpus()
    {
        var gpus = new List<GpuInfo>();
        DXGI.CreateDXGIFactory1(out IDXGIFactory2? factory);
        if (factory == null) return gpus;

        try
        {
            for (uint i = 0; factory.EnumAdapters1(i, out IDXGIAdapter1? adapter).Success; i++)
            {
                if (adapter == null) continue;
                var desc = adapter.Description1;

                if ((desc.Flags & AdapterFlags.Software) != 0)
                {
                    adapter.Dispose();
                    continue;
                }

                gpus.Add(new GpuInfo
                {
                    Name = desc.Description,
                    DedicatedMemoryMB = (int)((ulong)desc.DedicatedVideoMemory / 1024 / 1024),
                    Index = (int)i
                });
                adapter.Dispose();
            }
        }
        finally
        {
            factory.Dispose();
        }

        return gpus;
    }

    public D3D11GraphicsDevice(string? preferredAdapterName = null)
    {
        Log("Creating DXGI factory...");
        DXGI.CreateDXGIFactory1(out IDXGIFactory2? factory);
        Factory = factory ?? throw new InvalidOperationException("Failed to create DXGI factory");

        // Find adapter by name or use best available
        Log($"Finding adapter (preferred: {preferredAdapterName ?? "auto"})...");
        _adapter = FindAdapter(Factory, preferredAdapterName);

        if (_adapter != null)
        {
            var desc = _adapter.Description1;
            AdapterName = desc.Description;
            Log($"Selected adapter: {desc.Description}, VRAM: {desc.DedicatedVideoMemory / 1024 / 1024}MB");
        }
        else
        {
            AdapterName = "Default";
            Log("No adapter selected, using default");
        }

        // Feature levels to try (prefer newer)
        FeatureLevel[] featureLevels =
        [
            FeatureLevel.Level_11_1,
            FeatureLevel.Level_11_0,
            FeatureLevel.Level_10_1,
            FeatureLevel.Level_10_0
        ];

        // Device creation flags - enable debug layer for detailed error messages
        DeviceCreationFlags flags = DeviceCreationFlags.BgraSupport;

#if DEBUG
        // Try to enable debug layer (requires Windows SDK Graphics Tools)
        try
        {
            // Test if debug layer is available by checking if D3D11_CREATE_DEVICE_DEBUG works
            flags |= DeviceCreationFlags.Debug;
            Log("Debug layer enabled");
        }
        catch
        {
            flags = DeviceCreationFlags.BgraSupport;
            Log("Debug layer not available (install Graphics Tools optional feature)");
        }
#endif

        // Create device on the selected adapter
        Log("Creating D3D11 device...");
        D3D11.D3D11CreateDevice(
            _adapter,
            _adapter != null ? DriverType.Unknown : DriverType.Hardware,
            flags,
            featureLevels,
            out ID3D11Device? device,
            out FeatureLevel featureLevel,
            out ID3D11DeviceContext? context);

        Device = device ?? throw new InvalidOperationException("Failed to create D3D11 device");
        Context = context ?? throw new InvalidOperationException("Failed to get device context");
        FeatureLevel = featureLevel;
        Log($"D3D11 device created, feature level: {featureLevel}");
    }

    private static void Log(string message) => Logger.Log("D3D11Device", message);

    private static IDXGIAdapter1? FindAdapter(IDXGIFactory2 factory, string? preferredName)
    {
        var adapters = new List<(IDXGIAdapter1 adapter, AdapterDescription1 desc, int outputCount)>();

        Log("Enumerating graphics adapters...");
        for (uint i = 0; factory.EnumAdapters1(i, out IDXGIAdapter1? adapter).Success; i++)
        {
            if (adapter == null) continue;
            var desc = adapter.Description1;

            int outputCount = 0;
            for (uint j = 0; adapter.EnumOutputs(j, out var output).Success; j++)
            {
                outputCount++;
                output?.Dispose();
            }

            Log($"  [{i}] {desc.Description} (VRAM: {desc.DedicatedVideoMemory / 1024 / 1024}MB, Outputs: {outputCount})");

            if ((desc.Flags & AdapterFlags.Software) != 0)
            {
                adapter.Dispose();
                continue;
            }

            adapters.Add((adapter, desc, outputCount));
        }

        if (adapters.Count == 0) return null;

        IDXGIAdapter1? selected = null;

        // If preferred name specified, try to find it
        if (!string.IsNullOrEmpty(preferredName))
        {
            var match = adapters.FirstOrDefault(a => a.desc.Description.Contains(preferredName, StringComparison.OrdinalIgnoreCase));
            if (match.adapter != null)
            {
                selected = match.adapter;
                Log($"Found preferred adapter: {match.desc.Description}");
            }
        }

        // Fallback: prefer adapter with outputs
        if (selected == null)
        {
            var withOutputs = adapters.FirstOrDefault(a => a.outputCount > 0);
            selected = withOutputs.adapter ?? adapters[0].adapter;
        }

        // Dispose unused adapters
        foreach (var (adapter, _, _) in adapters)
        {
            if (adapter != selected) adapter.Dispose();
        }

        return selected;
    }

    [Obsolete("Use FindAdapter instead")]
    private static IDXGIAdapter1? FindBestAdapter(IDXGIFactory2 factory)
    {
        var adapters = new List<(IDXGIAdapter1 adapter, AdapterDescription1 desc, int outputCount)>();

        // Enumerate all adapters
        Log("Enumerating graphics adapters...");
        for (uint i = 0; factory.EnumAdapters1(i, out IDXGIAdapter1? adapter).Success; i++)
        {
            if (adapter == null) continue;

            var desc = adapter.Description1;

            // Count outputs (monitors) connected to this adapter
            int outputCount = 0;
            for (uint j = 0; adapter.EnumOutputs(j, out var output).Success; j++)
            {
                outputCount++;
                output?.Dispose();
            }

            Log($"  [{i}] {desc.Description}");
            Log($"      VRAM: {desc.DedicatedVideoMemory / 1024 / 1024}MB, Outputs: {outputCount}");
            Log($"      Flags: {desc.Flags}");

            // Skip software adapters
            if ((desc.Flags & AdapterFlags.Software) != 0)
            {
                Log($"      -> Skipped (software adapter)");
                adapter.Dispose();
                continue;
            }

            adapters.Add((adapter, desc, outputCount));
        }

        if (adapters.Count == 0)
        {
            Log("No suitable adapters found!");
            return null;
        }

        // Prefer adapter with outputs (connected to display)
        // This is important for hybrid GPU laptops where display is on integrated GPU
        IDXGIAdapter1? bestAdapter = null;
        var adapterWithOutputs = adapters.FirstOrDefault(a => a.outputCount > 0);

        if (adapterWithOutputs.adapter != null)
        {
            bestAdapter = adapterWithOutputs.adapter;
            Log($"Selected adapter with display outputs: {adapterWithOutputs.desc.Description}");
        }
        else
        {
            // Fallback to first adapter
            bestAdapter = adapters[0].adapter;
            Log($"Selected first available adapter: {adapters[0].desc.Description}");
        }

        // Dispose unused adapters
        foreach (var (adapter, desc, _) in adapters)
        {
            if (adapter != bestAdapter)
            {
                adapter.Dispose();
            }
        }

        return bestAdapter;
    }

    /// <summary>
    /// Create a swap chain for a window.
    /// </summary>
    public IDXGISwapChain1 CreateSwapChain(nint hwnd, int width, int height)
    {
        Log($"CreateSwapChain: hwnd={hwnd}, width={width}, height={height}");

        // Ensure valid dimensions
        width = Math.Max(1, width);
        height = Math.Max(1, height);

        var swapChainDesc = new SwapChainDescription1
        {
            Width = (uint)width,
            Height = (uint)height,
            Format = Format.B8G8R8A8_UNorm,
            Stereo = false,
            SampleDescription = new SampleDescription(1, 0),
            BufferUsage = Usage.RenderTargetOutput,
            BufferCount = 2,
            Scaling = Scaling.Stretch,
            SwapEffect = SwapEffect.Discard,  // Legacy blit model - works better cross-adapter
            AlphaMode = AlphaMode.Unspecified,  // Unspecified works with legacy swap effect
            Flags = SwapChainFlags.None
        };

        try
        {
            Log("Calling CreateSwapChainForHwnd...");
            var swapChain = Factory.CreateSwapChainForHwnd(Device, hwnd, swapChainDesc);
            Log("SwapChain created successfully");
            return swapChain;
        }
        catch (Exception ex)
        {
            Log($"CreateSwapChainForHwnd failed: {ex.Message}");
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Context.Dispose();
        Device.Dispose();
        _adapter?.Dispose();
        Factory.Dispose();
    }
}

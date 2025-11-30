using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace MouseEffects.DirectX.Graphics;

/// <summary>
/// Manages a swap chain and its render target.
/// </summary>
public sealed class SwapChainManager : IDisposable
{
    private readonly D3D11GraphicsDevice _graphicsDevice;
    private bool _disposed;

    public IDXGISwapChain1 SwapChain { get; }
    public ID3D11RenderTargetView RenderTargetView { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }

    public SwapChainManager(D3D11GraphicsDevice graphicsDevice, nint hwnd, int width, int height)
    {
        _graphicsDevice = graphicsDevice;
        Width = width;
        Height = height;

        SwapChain = graphicsDevice.CreateSwapChain(hwnd, width, height);
        RenderTargetView = CreateRenderTargetView();
    }

    private ID3D11RenderTargetView CreateRenderTargetView()
    {
        using var backBuffer = SwapChain.GetBuffer<ID3D11Texture2D>(0);
        return _graphicsDevice.Device.CreateRenderTargetView(backBuffer);
    }

    /// <summary>
    /// Resize the swap chain.
    /// </summary>
    public void Resize(int width, int height)
    {
        if (width <= 0 || height <= 0) return;
        if (width == Width && height == Height) return;

        Width = width;
        Height = height;

        // Release old render target
        RenderTargetView.Dispose();

        // Resize swap chain
        SwapChain.ResizeBuffers(0, (uint)width, (uint)height, Format.Unknown, SwapChainFlags.None);

        // Recreate render target view
        RenderTargetView = CreateRenderTargetView();
    }

    /// <summary>
    /// Begin rendering a frame.
    /// </summary>
    public void BeginFrame()
    {
        var context = _graphicsDevice.Context;

        // Set render target
        context.OMSetRenderTargets(RenderTargetView);

        // Set viewport
        context.RSSetViewport(0, 0, Width, Height);

        // Clear to transparent
        context.ClearRenderTargetView(RenderTargetView, new Color4(0, 0, 0, 0));
    }

    /// <summary>
    /// Present the frame.
    /// </summary>
    public void Present(bool vsync = true)
    {
        SwapChain.Present(vsync ? 1u : 0u, PresentFlags.None);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        RenderTargetView.Dispose();
        SwapChain.Dispose();
    }
}

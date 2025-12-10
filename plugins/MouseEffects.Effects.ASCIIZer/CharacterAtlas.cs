using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using MouseEffects.Core.Rendering;
using Vortice.DXGI;

namespace MouseEffects.Effects.ASCIIZer;

/// <summary>
/// Generates font texture atlases for ASCII rendering.
/// </summary>
public class CharacterAtlas : IDisposable
{
    /// <summary>
    /// Standard 10-character set (dark to light).
    /// </summary>
    public const string Standard = " .:-=+*#%@";

    /// <summary>
    /// Extended 70-character set for more detail.
    /// </summary>
    public const string Extended = " .'`^\",:;Il!i><~+_-?][}{1)(|\\/tfjrxnuvczXYUJCLQ0OZmwqpdbkhao*#MW&8%B@$";

    /// <summary>
    /// Unicode block characters for high resolution.
    /// </summary>
    public const string Blocks = " ░▒▓█";

    /// <summary>
    /// Simple blocks for basic rendering.
    /// </summary>
    public const string SimpleBlocks = " ▁▂▃▄▅▆▇█";

    private ITexture? _atlasTexture;
    private string _currentCharset = "";
    private int _currentCharWidth;
    private int _currentCharHeight;
    private string _currentFontFamily = "";
    private bool _currentBold;

    /// <summary>
    /// Gets the current atlas texture.
    /// </summary>
    public ITexture? Texture => _atlasTexture;

    /// <summary>
    /// Gets the number of characters in the current atlas.
    /// </summary>
    public int CharacterCount => _currentCharset.Length;

    /// <summary>
    /// Gets or creates a character atlas texture.
    /// </summary>
    /// <param name="context">The render context.</param>
    /// <param name="charset">Character set from dark to light.</param>
    /// <param name="charWidth">Width of each character cell in pixels.</param>
    /// <param name="charHeight">Height of each character cell in pixels.</param>
    /// <param name="fontFamily">Font family name.</param>
    /// <param name="bold">Whether to use bold font weight.</param>
    /// <returns>The atlas texture.</returns>
    public ITexture CreateOrUpdateAtlas(
        IRenderContext context,
        string charset,
        int charWidth,
        int charHeight,
        string fontFamily = "Consolas",
        bool bold = false)
    {
        // Check if we need to regenerate
        if (_atlasTexture != null &&
            charset == _currentCharset &&
            charWidth == _currentCharWidth &&
            charHeight == _currentCharHeight &&
            fontFamily == _currentFontFamily &&
            bold == _currentBold)
        {
            return _atlasTexture;
        }

        // Dispose old texture
        _atlasTexture?.Dispose();

        // Generate new atlas
        _atlasTexture = GenerateAtlas(context, charset, charWidth, charHeight, fontFamily, bold);
        _currentCharset = charset;
        _currentCharWidth = charWidth;
        _currentCharHeight = charHeight;
        _currentFontFamily = fontFamily;
        _currentBold = bold;

        return _atlasTexture;
    }

    private ITexture GenerateAtlas(
        IRenderContext context,
        string charset,
        int charWidth,
        int charHeight,
        string fontFamily,
        bool bold)
    {
        int charCount = charset.Length;
        if (charCount == 0)
        {
            charset = Standard;
            charCount = charset.Length;
        }

        int atlasWidth = charWidth * charCount;
        int atlasHeight = charHeight;

        // Create bitmap
        using var bitmap = new Bitmap(atlasWidth, atlasHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);

        // Setup high quality rendering
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

        // Clear to black (transparent)
        graphics.Clear(Color.Transparent);

        // Calculate font size to fit cell
        float fontSize = CalculateFontSize(graphics, fontFamily, bold, charWidth, charHeight);
        var fontStyle = bold ? FontStyle.Bold : FontStyle.Regular;

        using var font = new Font(fontFamily, fontSize, fontStyle, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(Color.White);

        // Draw each character centered in its cell
        var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        for (int i = 0; i < charCount; i++)
        {
            float x = i * charWidth + charWidth / 2f;
            float y = charHeight / 2f;
            graphics.DrawString(charset[i].ToString(), font, brush, x, y, format);
        }

        // Convert to texture
        return CreateTextureFromBitmap(context, bitmap);
    }

    private float CalculateFontSize(Graphics graphics, string fontFamily, bool bold, int cellWidth, int cellHeight)
    {
        var fontStyle = bold ? FontStyle.Bold : FontStyle.Regular;
        float fontSize = cellHeight * 0.8f;

        // Iteratively find the best font size
        for (int i = 0; i < 10; i++)
        {
            using var testFont = new Font(fontFamily, fontSize, fontStyle, GraphicsUnit.Pixel);
            var size = graphics.MeasureString("W", testFont);

            if (size.Width > cellWidth * 0.9f || size.Height > cellHeight * 0.95f)
            {
                fontSize *= 0.9f;
            }
            else
            {
                break;
            }
        }

        return Math.Max(fontSize, 6f);
    }

    private ITexture CreateTextureFromBitmap(IRenderContext context, Bitmap bitmap)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;

        // Lock bitmap and copy pixels
        var bitmapData = bitmap.LockBits(
            new Rectangle(0, 0, width, height),
            System.Drawing.Imaging.ImageLockMode.ReadOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        try
        {
            byte[] pixels = new byte[width * height * 4];
            Marshal.Copy(bitmapData.Scan0, pixels, 0, pixels.Length);

            // Convert BGRA to RGBA and extract luminance to red channel
            // The shader will sample the red channel as the character alpha
            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte b = pixels[i];
                byte g = pixels[i + 1];
                byte r = pixels[i + 2];
                byte a = pixels[i + 3];

                // Calculate luminance
                byte luma = (byte)((r * 77 + g * 150 + b * 29) >> 8);

                // Store luminance in R channel, keep alpha
                pixels[i] = luma;     // R = luminance
                pixels[i + 1] = luma; // G = luminance
                pixels[i + 2] = luma; // B = luminance
                pixels[i + 3] = a;    // A = alpha
            }

            // Create texture description
            var desc = new TextureDescription
            {
                Width = width,
                Height = height,
                Format = TextureFormat.R8G8B8A8_UNorm,
                ShaderResource = true
            };

            return context.CreateTexture(desc, pixels);
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }
    }

    /// <summary>
    /// Gets a character set by preset type.
    /// </summary>
    public static string GetCharsetPreset(int preset)
    {
        return preset switch
        {
            0 => Standard,
            1 => Extended,
            2 => Blocks,
            3 => SimpleBlocks,
            _ => Standard
        };
    }

    /// <summary>
    /// Gets the preset name for display.
    /// </summary>
    public static string GetPresetName(int preset)
    {
        return preset switch
        {
            0 => "Standard (10 chars)",
            1 => "Extended (70 chars)",
            2 => "Blocks (Unicode)",
            3 => "Simple Blocks",
            _ => "Custom"
        };
    }

    public void Dispose()
    {
        _atlasTexture?.Dispose();
        _atlasTexture = null;
    }
}

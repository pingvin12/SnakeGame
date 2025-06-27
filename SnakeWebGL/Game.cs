using Silk.NET.OpenGLES;
using SnakeCore;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using StbTrueTypeSharp;
using System.IO;
using System.Linq;
using System.Drawing.Imaging;

[assembly: SupportedOSPlatform("browser")]

namespace SnakeWebGL;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct VertexPositionColorTexture
{
    public Vector2 Position;
    public Vector3 Color;
    public Vector2 Texture;
};

public record Texture2D
{
    public uint Handle { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
}

public partial class Game
{
    private WebGlRenderer _renderer;
    private SnakeCore.Game _game;

    private class FontAtlas
    {
        public Texture2D Texture;
        public int AtlasWidth;
        public int AtlasHeight;
        public StbTrueTypeSharp.StbTrueType.stbtt_packedchar[] Chars;
        public float FontSize;
    }

    private FontAtlas _fontAtlas;

    private void EnsureFontAtlas()
    {
        if (_fontAtlas != null)
            return;

        // Load TTF font bytes
        var fontPath = Path.Combine("SnakeCore", "Resources", "ARCADECLASSIC.TTF");
        byte[] fontData = File.ReadAllBytes(fontPath);

        // Font settings
        float fontSize = 12f;
        int firstChar = 32;
        int numChars = 95; // ASCII 32-126
        int atlasWidth = 512;
        int atlasHeight = 64;

        var chars = new StbTrueTypeSharp.StbTrueType.stbtt_packedchar[numChars];
        var atlasBitmap = new byte[atlasWidth * atlasHeight];

        unsafe
        {
            fixed (byte* fontPtr = fontData)
            fixed (byte* atlasPtr = atlasBitmap)
            fixed (StbTrueTypeSharp.StbTrueType.stbtt_packedchar* charsPtr = chars)
            {
                StbTrueTypeSharp.StbTrueType.stbtt_pack_context context = new();
                StbTrueTypeSharp.StbTrueType.stbtt_PackBegin(context, atlasPtr, atlasWidth, atlasHeight, 0, 1, null);
                StbTrueTypeSharp.StbTrueType.stbtt_PackFontRange(context, fontPtr, 0, fontSize, firstChar, numChars, charsPtr);
                StbTrueTypeSharp.StbTrueType.stbtt_PackEnd(context);
            }
        }

        // Convert 8-bit alpha to RGBA
        byte[] rgba = new byte[atlasWidth * atlasHeight * 4];
        for (int i = 0; i < atlasWidth * atlasHeight; i++)
        {
            byte a = atlasBitmap[i];
            rgba[i * 4 + 0] = 255;
            rgba[i * 4 + 1] = 255;
            rgba[i * 4 + 2] = 255;
            rgba[i * 4 + 3] = a;
        }

        var tex = ((IRenderer<Texture2D>)this).CreateImage(atlasWidth, atlasHeight, rgba);
        _fontAtlas = new FontAtlas
        {
            Texture = tex,
            AtlasWidth = atlasWidth,
            AtlasHeight = atlasHeight,
            Chars = chars,
            FontSize = fontSize
        };
    }

    private Game(GL gl)
    {
        _game = new();
        _renderer = new WebGlRenderer(gl, _game.playground.DesignWidth, _game.playground.DesignHeight);

        _game.Initialize(_renderer);
    }

    public void Update(float elapsedSeconds, Direction direction)
    {
        _game.Update(elapsedSeconds, direction);
    }

    public void Draw()
    {
        _renderer.BeginRender();

        _game.Draw(0.016f, _renderer);

        _renderer.EndRender();
    }

    internal void CanvasResized(int canvasWidth, int canvasHeight)
    {
        _renderer.SetCanvasSize(canvasWidth, canvasHeight);
    }

    public static Game Create(GL gl)
    {
        return new Game(gl);
    }
}

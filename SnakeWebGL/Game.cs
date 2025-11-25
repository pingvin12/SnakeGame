using Microsoft.Extensions.Logging;
using Silk.NET.OpenGLES;
using SnakeCore;
using SnakeCore.Logging;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

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
    private bool _isInMenu = true;
    private Texture2D _uiTexture;
    private RectangleF _startButtonBounds;

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
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddProvider(new LokiLoggerProvider("http://localhost:3100/loki/api/v1/push"));// TODO, MOV TO CONFIG FILE
            builder.SetMinimumLevel(LogLevel.Information);
        });
        var logger = loggerFactory.CreateLogger("SnakeGameWebGL");

        _game = new(logger);
        _renderer = new WebGlRenderer(gl, _game.playground.DesignWidth, _game.playground.DesignHeight);

        _uiTexture = ((IRenderer<Texture2D>)_renderer).CreateImage(1, 1, new byte[] { 255, 255, 255, 255 });
        var buttonSize = new Vector2(200f, 40f);
        var buttonPosition = new Vector2(_game.playground.DesignWidth / 2f - buttonSize.X / 2f, _game.playground.DesignHeight / 2f + 20f);
        _startButtonBounds = new RectangleF(buttonPosition.X, buttonPosition.Y, buttonSize.X, buttonSize.Y);

        _game.Initialize(_renderer);
    }

    public void StartGame()
    {
        if (!_isInMenu)
            return;

        _isInMenu = false;
    }

    public void Update(float elapsedSeconds, Direction direction, bool startRequested = false)
    {
        if (_isInMenu)
        {
            if (startRequested)
                StartGame();

            return;
        }

        _game.Update(elapsedSeconds, direction);
    }

    public void HandlePointerDown(Vector2 screenPosition, int button)
    {
        if (!_isInMenu || button != 0)
            return;

        var worldPos = _renderer.ScreenToWorld(screenPosition);
        if (_startButtonBounds.Contains(worldPos))
        {
            StartGame();
        }
    }

    public void Draw()
    {
        _renderer.BeginRender();

        if (_isInMenu)
        {
            DrawMenu();
        }
        else
        {
            _game.Draw(0.016f, _renderer);
        }

        _renderer.EndRender();
    }

    internal void CanvasResized(int canvasWidth, int canvasHeight)
    {
        _renderer.SetCanvasSize(canvasWidth, canvasHeight);
    }

    private void DrawMenu()
    {
        var centerX = _game.playground.DesignWidth / 2f;
        var centerY = _game.playground.DesignHeight / 2f;

        var titlePosition = new Vector2(centerX - 36f, centerY - 20f);
        var promptPosition = new Vector2(centerX - 60f, _startButtonBounds.Y - 18f);
        var buttonTextPosition = new Vector2(_startButtonBounds.X + 70f, _startButtonBounds.Y + 14f);

        _renderer.DrawText("SNAKE", titlePosition);
        _renderer.DrawText("Press Enter, Space, or Click to start", promptPosition);
        DrawStartButton(buttonTextPosition);
    }

    private void DrawStartButton(Vector2 textPosition)
    {
        var srcRect = new Rectangle(0, 0, 1, 1);
        _renderer.DrawImage(_uiTexture, new Vector2(_startButtonBounds.X, _startButtonBounds.Y), new Vector2(_startButtonBounds.Width, _startButtonBounds.Height), 0, Vector2.Zero, srcRect, Color.DarkSeaGreen);
        _renderer.DrawImage(_uiTexture, new Vector2(_startButtonBounds.X + 4f, _startButtonBounds.Y + 4f), new Vector2(_startButtonBounds.Width - 8f, _startButtonBounds.Height - 8f), 0, Vector2.Zero, srcRect, Color.SeaGreen);
        _renderer.DrawText("Start", textPosition);
    }

    public static Game Create(GL gl)
    {
        return new Game(gl);
    }
}

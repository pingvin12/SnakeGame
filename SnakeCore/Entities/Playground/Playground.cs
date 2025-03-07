using System.Drawing;
using System.Numerics;
using StbImageSharp;

namespace SnakeCore;

public class Playground : IRenderable, ICollidable
{
    public Playground(int width = 100, int height = 100)
    {
        DesignHeight = height;
        Height = 70;
        DesignWidth = width;
        Width = 70;
        tileSize = new Vector2(DesignWidth / Width, DesignHeight / Height);
    }

    public int DesignWidth { get; }
    public int DesignHeight { get; }
    public Vector2 TileSize { get => tileSize; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    private Vector2 tileSize;
    private ImageHandle _cellImage;

    public void Initialize(IRenderer renderer)
    {
        var cellImage = ImageResult.FromMemory(Resource.px_cell, ColorComponents.RedGreenBlueAlpha);
        byte[] checkerboardData = new byte[2 * 2 * 4];

        checkerboardData[0] = LightGreen.R;
        checkerboardData[1] = LightGreen.G;
        checkerboardData[2] = LightGreen.B;
        checkerboardData[3] = LightGreen.A;

        checkerboardData[4] = DarkGreen.R;
        checkerboardData[5] = DarkGreen.G;
        checkerboardData[6] = DarkGreen.B;
        checkerboardData[7] = DarkGreen.A;

        checkerboardData[8] = DarkGreen.R;
        checkerboardData[9] = DarkGreen.G;
        checkerboardData[10] = DarkGreen.B;
        checkerboardData[11] = DarkGreen.A;

        checkerboardData[12] = LightGreen.R;
        checkerboardData[13] = LightGreen.G;
        checkerboardData[14] = LightGreen.B;
        checkerboardData[15] = LightGreen.A;

        _cellImage = renderer.CreateImage(2, 2, checkerboardData);
    }
    private static readonly Color LightGreen = Color.FromArgb(162, 209, 73);
    private static readonly Color DarkGreen = Color.FromArgb(170, 215, 81);
    private static int CHUNK_SIZE = 5;

    public void Draw(IRenderer renderer)
    {
        for (var x = 0; x < Width; x += CHUNK_SIZE)
        {
            for (var y = 0; y < Height; y += CHUNK_SIZE)
            {
                var chunkWidth = Math.Min(CHUNK_SIZE, Width - x);
                var chunkHeight = Math.Min(CHUNK_SIZE, Height - y);
                var position = new Vector2(x, y) * tileSize;
                var size = new Vector2(chunkWidth, chunkHeight) * tileSize;

                renderer.DrawImage(_cellImage, position, size, 0, Vector2.Zero,
                    new Rectangle(0, 0, 2, 2), Color.White);
            }
        }
    }

    public bool IsColliding(Vector2 position)
    {
        return position.X < 0 || position.X > Width - 1 || position.Y < 0 || position.Y > Height - 1;
    }
}
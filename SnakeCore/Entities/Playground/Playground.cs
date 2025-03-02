using System.Drawing;
using System.Numerics;
using StbImageSharp;

namespace SnakeCore;

public class Playground : IRenderable, ICollidable
{
    public Playground()
    {
        DesignHeight = 700;
        Height = 70;
        DesignWidth = 700;
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
        _cellImage = renderer.CreateImage(1, 1, cellImage.Data);
    }

    public void Draw(IRenderer renderer)
    {
        for(var x = 0; x < Width; x++)
        {
            for(var y = 0; y < Height; y++)
            {
                var lightGreen = Color.FromArgb(162, 209, 73);
                var darkGreen = Color.FromArgb(170, 215, 81);
                var color = (x + y) % 2 == 0 ? lightGreen : darkGreen;
                renderer.DrawImage(_cellImage, new Vector2(x, y) * tileSize, tileSize, 0, Vector2.Zero, color);
            }
        }
    }

    public bool IsColliding(Vector2 position)
    {
        return position.X < 0 || position.X > Width - 1 || position.Y < 0 || position.Y > Height - 1;
    }
}
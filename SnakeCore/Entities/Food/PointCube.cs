using StbImageSharp;
using System.Drawing;
using System.Numerics;

namespace SnakeCore.Entities.Food;

public class PointCube : ICollidable, IRenderable
{
    public Vector2 Position { get; }
    public Vector2 Size { get; }
    private ImageHandle _cellImage;
    public PointCube(Vector2 size, Vector2 position)
    {
        Position = position;
        Size = size;
    }

    public bool IsColliding(Vector2 pos)
    {
        return Vector2.Distance(pos, Position) <= 5f;
    }

    public void Initialize(IRenderer renderer)
    {
        var cellImage = ImageResult.FromMemory(Resource.px_cell, ColorComponents.RedGreenBlueAlpha);
        
        _cellImage = renderer.CreateImage((int)Size.X, (int)Size.Y, cellImage.Data);
    }

    public void Draw(IRenderer renderer)
    {
        renderer.DrawImage(_cellImage, Position, Size, 0, Vector2.Zero,
                    new Rectangle(0, 0, (int)Size.X, (int)Size.Y), Color.Gold);
    }
}
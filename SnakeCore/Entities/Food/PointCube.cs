using StbImageSharp;
using System.Drawing;
using System.Numerics;

namespace SnakeCore.Entities.Food;

public class PointCube : ICollidable, IRenderable
{
    public Vector2 Position { get; }

    private ImageHandle _cellImage;
    public PointCube(Vector2 position)
    {
        Position = position;
    }

    public bool IsColliding(Vector2 pos)
    {
        return pos == Position;
    }

    public void Initialize(IRenderer renderer)
    {
        var cellImage = ImageResult.FromMemory(Resource.px_cell, ColorComponents.RedGreenBlueAlpha);
        
        _cellImage = renderer.CreateImage(2, 2, cellImage.Data);
    }

    public void Draw(IRenderer renderer)
    {
        renderer.DrawImage(_cellImage, Position, new Vector2(200 / 70, 200 / 70) / 2.5F, 0, Vector2.Zero,
                    new Rectangle(0, 0, 2, 2), Color.Gold);
    }
}
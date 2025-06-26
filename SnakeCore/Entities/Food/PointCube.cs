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
    }

    public void Draw(IRenderer renderer)
    {
    }
}
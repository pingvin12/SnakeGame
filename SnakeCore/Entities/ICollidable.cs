
using System.Numerics;

namespace SnakeCore;

public interface ICollidable
{
    bool IsColliding(Vector2 position);
}
using System.Numerics;

namespace SnakeCore;

public interface ICamera
{
    Vector2 Position { get; set; }
    float Rotation { get; set; }
    float Zoom { get; set; }
    
    void SetCamera(Vector2 position, float rotation, float zoom);
    Vector2 ScreenToWorld(Vector2 screenPosition);
    Vector2 WorldToScreen(Vector2 worldPosition);
} 
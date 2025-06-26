
namespace SnakeCore;

public interface IRenderable {
    void Draw(IRenderer renderer);
    void Initialize(IRenderer render);
}
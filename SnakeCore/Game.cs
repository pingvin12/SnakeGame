using SnakeCore.Entities.Snake;
using StbImageSharp;
using System.Drawing;
using System.Numerics;
using SnakeCore.AI;
using SnakeCore.Entities.Food;

namespace SnakeCore;

public delegate void StateUpdate(float elapsedSeconds, Direction direction);

public class Game
{
    private readonly Snake _playerSnake;
    private readonly Snake _aiSnake;
    private ImageHandle? _eyeImage;
    private ImageHandle? _eatImage;
    private ImageHandle? _cellImage;
    public Playground playground;
    
    private Vector2 _cameraPosition;
    private float _cameraZoom = 2.0f;
    private readonly float _cameraSmoothing = 5.0f;

    public float Speed { get; set; } = 5;
    public int Points { get; protected set; }
    public bool IsDead => _playerSnake.IsDead;

    private List<PointCube> _pointCubes = new();
    private static readonly int PointCubeCount = 3;
    private static readonly Random _random = new();

    public Game()
    {
        playground = new Playground();
        _playerSnake = new Snake(Vector2.Zero);
        _aiSnake = new AISnake(Vector2.Zero, playground);
    }

    public void Initialize(IRenderer renderer)
    {
        var cellImage = ImageResult.FromMemory(Resource.px_cell, ColorComponents.RedGreenBlueAlpha);
        _cellImage = renderer.CreateImage(1, 1, cellImage.Data);

        var eyeImage = ImageResult.FromMemory(Resource.px_blink, ColorComponents.RedGreenBlueAlpha);
        _eyeImage = renderer.CreateImage(eyeImage.Width, eyeImage.Height, eyeImage.Data);

        var eatImage = ImageResult.FromMemory(Resource.px_eat, ColorComponents.RedGreenBlueAlpha);
        _eatImage = renderer.CreateImage(eatImage.Width, eatImage.Height, eatImage.Data);

        playground.Initialize(renderer);
        
        var playerStart = new Vector2(4, playground.Height / 2 - 2);
        _playerSnake.Initialize(playerStart);
        
        var aiStart = new Vector2(4, playground.Height / 2 + 2);
        _aiSnake.Initialize(aiStart);

        Points = 0;
        _pointCubes.Clear();
        // Spawn point cubes at random positions not occupied by snakes
        var occupied = new HashSet<Vector2>(_playerSnake.Segments.Concat(_aiSnake.Segments));
        for (int i = 0; i < PointCubeCount; i++)
        {
            Vector2 pos;
            do
            {
                pos = new Vector2(_random.Next(0, playground.Width), _random.Next(0, playground.Height));
            } while (occupied.Contains(pos));
            occupied.Add(pos);
            _pointCubes.Add(new PointCube(pos));
        }
    }

    public void Update(float elapsedSeconds, Direction direction)
    {
        _playerSnake.Update(elapsedSeconds, direction, Speed);
        CheckCollisions(_playerSnake);
        CheckPointCubeCollisions(_playerSnake);

        _aiSnake.Update(elapsedSeconds, Direction.None, Speed);
        CheckCollisions(_aiSnake);
        CheckPointCubeCollisions(_aiSnake);
    }

    private void CheckCollisions(Snake snake)
    {
        var nextHead = snake.Head + snake.CurrentDirection.ToVector2();
            
        if (playground.IsColliding(nextHead) || 
            snake.IsColliding(Vector2.Zero) || 
            WillCollideWithOtherSnake(snake, nextHead))
        {
            snake.StartDeathAnimation();
        }
    }

    private bool WillCollideWithOtherSnake(Snake currentSnake, Vector2 position)
    {
        var otherSnake = currentSnake == _playerSnake ? _aiSnake : _playerSnake;
        
        foreach (var segment in otherSnake.Segments)
        {
            if (segment == position)
                return true;
        }
        
        return false;
    }

    private void CheckPointCubeCollisions(Snake snake)
    {
        var nextHead = snake.Head + snake.CurrentDirection.ToVector2();
        for (int i = _pointCubes.Count - 1; i >= 0; i--)
        {
            if (_pointCubes[i].IsColliding(nextHead))
            {
                _pointCubes.RemoveAt(i);
                Points++;
                // Optionally respawn a new point cube
                SpawnPointCube();
            }
        }
    }

    private void SpawnPointCube()
    {
        // Avoid placing on snakes or existing cubes
        var occupied = new HashSet<Vector2>(_playerSnake.Segments.Concat(_aiSnake.Segments).Concat(_pointCubes.Select(c => c.Position)));
        Vector2 pos;
        int tries = 0;
        do
        {
            pos = new Vector2(_random.Next(0, playground.Width), _random.Next(0, playground.Height));
            tries++;
        } while (occupied.Contains(pos) && tries < 100);
        if (!occupied.Contains(pos))
            _pointCubes.Add(new PointCube(pos));
    }

    public void Draw(float elapsedSeconds, IRenderer renderer)
    {
        UpdateCamera(elapsedSeconds);
        
        renderer.SetCamera(_cameraPosition, 0, _cameraZoom);
        playground.Draw(renderer);
        // Draw point cubes
        foreach (var cube in _pointCubes)
            cube.Draw(renderer);
        var unclampedScorePos = _cameraPosition + _playerSnake.HeadOffset + _playerSnake.ShakeOffset;
        var scorePos = Vector2.Max(unclampedScorePos, Vector2.Zero);
        renderer.DrawText($"Score: {Points}", scorePos);

        DrawSnake(_playerSnake, renderer);
        DrawSnake(_aiSnake, renderer);
    }

    private void UpdateCamera(float elapsedSeconds)
    {
        var headFinal = Vector2.Clamp(
            _playerSnake.Head + _playerSnake.HeadOffset + _playerSnake.ShakeOffset,
            Vector2.Zero,
            new Vector2(playground.Width - 1, playground.Height - 1));
            
        var targetCameraPos = -(headFinal * playground.TileSize);
        _cameraPosition = Vector2.Lerp(_cameraPosition, targetCameraPos, elapsedSeconds * _cameraSmoothing);
    }

    private void DrawSnake(Snake snake, IRenderer renderer)
    {
        var segments = snake.Segments;

        var tailFinal = (snake.Tail + snake.TailOffset) * playground.TileSize;
        renderer.DrawImage(_cellImage, tailFinal, playground.TileSize, 0, Vector2.Zero, snake.Color.Dark(0.04f * segments.Count));

        for (var i = 0; i < segments.Count - 1; i++)
        {
            var body = segments[i];
            renderer.DrawImage(_cellImage, body * playground.TileSize, playground.TileSize, 0, Vector2.Zero, snake.Color.Dark(0.04f * i));
        }

        var headFinal = (snake.Head + snake.HeadOffset + snake.ShakeOffset) * playground.TileSize;
        var direction = snake.CurrentDirection.ToVector2();
        var neckFinal = Vector2.Lerp(snake.Head, snake.Head + direction, snake.MoveProgress) * playground.TileSize;
        
        renderer.DrawImage(_cellImage, neckFinal, playground.TileSize, 0, Vector2.Zero, snake.Color);
        renderer.DrawImage(_cellImage, headFinal, playground.TileSize, 0, Vector2.Zero, snake.Color);

        DrawEyes(renderer, headFinal);
    }

    private void DrawEyes(IRenderer renderer, Vector2 headFinal)
    {
        var eyeSize = playground.TileSize / 2.5F;
        var headRotation = MathF.Atan2(_playerSnake.HeadRotation.Y, _playerSnake.HeadRotation.X);
        
        var eyePosition1 = Vector2.Transform(Vector2.Zero, Matrix3x2.CreateRotation(headRotation, playground.TileSize / 2)) + headFinal;
        var eyePosition2 = Vector2.Transform(new Vector2(0, playground.TileSize.Y - eyeSize.Y), Matrix3x2.CreateRotation(headRotation, playground.TileSize / 2)) + headFinal;

        renderer.DrawImage(_eyeImage, eyePosition1, eyeSize, headRotation, Vector2.Zero, new Rectangle(20, 20, 40, 40), Color.White);
        renderer.DrawImage(_eyeImage, eyePosition2, eyeSize, headRotation, Vector2.Zero, new Rectangle(20, 20, 40, 40), Color.White);
    }
}
using StbImageSharp;
using System.Drawing;
using System.Numerics;

namespace SnakeCore;

public delegate void StateUpdate(float elapsedSeconds, Direction direction);

public class Game
{
    // Fields for the snake movement
    private readonly List<Vector2> _snake = new List<Vector2>();
    private bool _waitingForFirstInput = true;
    private float _t;

    private Direction _currDirection = Direction.None;
    private List<Direction> _nextDirections = new();

    //private Direction _currDirection = Direction.None;
    //private Direction _nextDirection = Direction.None;

    // Fields for the snake drawing
    private Vector2 _snakeHeadOffset = Vector2.Zero;
    private Vector2 _snakeHeadRotation;
    private Vector2 _tailOffset = Vector2.Zero;
    private Vector2 _shakeOffset = Vector2.Zero;


    private readonly float _shakeDurationStart = .16f;
    private float _shakeDurationRemaining = .16f;

    private readonly float _goingBackStart1 = .18f;
    private float _goingBackRemaining1 = .18f;

    private readonly float _goingBackStart2 = .06f;
    private float _goingBackRemaining2 = .06f;

    private readonly float _waitingMenuStart = 1.1f;
    private float _waitingMenuRemaining = 1.1f;

    private Direction _lastDirection1 = Direction.None;
    private Direction _lastDirection2 = Direction.None;

    private Vector2 _removedTail1;
    private Vector2 _removedTail2;

    public int Points { get; protected set; }

    public bool IsDead { get; protected set; }

    public float Speed { get; set; } = 5;

    private StateUpdate? _stateUpdate;

    private ImageHandle? _eyeImage;
    private Rectangle _eyeAnimation1;
    private Rectangle _eyeAnimation2;

    private ImageHandle? _eatImage;
    private ImageHandle? _cellImage;
    private Playground playground;
    public int DesignWidth => playground.DesignHeight;
    public int DesignHeight => playground.DesignHeight;

    private Vector2 _cameraPosition;
    private float _cameraZoom = 2.0f;
    private readonly float _cameraSmoothing = 5.0f;

    public Game()
    {
        playground = new Playground();
    }

    /// <summary>
    /// Resets the game. 
    /// </summary>
    public void Initialize(IRenderer renderer)
    {
        var cellImage = ImageResult.FromMemory(Resource.px_cell, ColorComponents.RedGreenBlueAlpha);
        _cellImage = renderer.CreateImage(1, 1, cellImage.Data);

        var eyeImage = ImageResult.FromMemory(Resource.px_blink, ColorComponents.RedGreenBlueAlpha);
        _eyeImage = renderer.CreateImage(eyeImage.Width, eyeImage.Height, eyeImage.Data);

        _eyeAnimation1 = new Rectangle(20, 20, 40, 40);
        _eyeAnimation2 = new Rectangle(20, 260, 40, 40);

        var eatImage = ImageResult.FromMemory(Resource.px_eat, ColorComponents.RedGreenBlueAlpha);
        _eatImage = renderer.CreateImage(eatImage.Width, eatImage.Height, eatImage.Data);

        playground.Initialize(renderer);
        var center = new Vector2(playground.Width / 2, playground.Height / 2);
        center = new Vector2(4, center.Y);

        _snake.Clear();
        _snake.Add(center);
        _snake.Add(center + new Vector2(-1, 0));
        _snake.Add(center + new Vector2(-2, 0));

        _waitingForFirstInput = true;
        _t = 0;

        _currDirection = Direction.Right;
        _nextDirections.Clear();

        _shakeDurationRemaining = _shakeDurationStart;
        _goingBackRemaining1 = _goingBackStart1;
        _goingBackRemaining2 = _goingBackStart2;
        _waitingMenuRemaining = _waitingMenuStart;

        IsDead = false;
        Points = 0;

        _stateUpdate = InGame;
    }

    void InGame(float elapsedSeconds, Direction direction)
    {
        if(_waitingForFirstInput
            && direction != Direction.None
            && _currDirection.Inverse() != direction)
        {
            _waitingForFirstInput = false;
            _nextDirections.Add(direction);
        }

        if(_waitingForFirstInput)
            return;


        if(direction != Direction.None && !_nextDirections.Contains(direction) && _nextDirections.LastOrDefault(_currDirection) != direction.Inverse())
        {
            _nextDirections.Add(direction);
        }

        _t += elapsedSeconds / (1F / Speed);

        if(_t >= 1)
        {
            var newHead = _snake[0] + _currDirection.ToVector2();

            _lastDirection2 = _lastDirection1;
            _lastDirection1 = _currDirection;

            var previousRemovedTail = _removedTail2;
            _removedTail2 = _removedTail1;
            _removedTail1 = _snake[^1];

            if(_nextDirections.Count > 0) // If no user input keep the last.
            {
                _currDirection = _nextDirections[0];
                _nextDirections.RemoveAt(0);
            }

            _t = 0;

            _snake.Insert(0, newHead);
            _snake.RemoveAt(_snake.Count - 1);

            var nextHead = newHead + _currDirection.ToVector2();
            var isDead = false;
            for(var i = 1; i < _snake.Count; i++)
            {
                if(_snake[i] == nextHead)
                {
                    isDead = true;
                    break;
                }
            }
            
            if(playground.IsColliding(nextHead) || isDead)
            {
                _snake.RemoveAt(0);
                _snake.Add(_removedTail1);

                _removedTail1 = _removedTail2;
                _removedTail2 = previousRemovedTail;

                ChangeState(GoingBack);
                return;
            }
        }

        // Animation
        var currDirection = _currDirection.ToVector2();
        var nextDirection = (_nextDirections.Count > 0 ? _nextDirections[0] : _currDirection).ToVector2();

        var p0 = currDirection / 2;
        var p2 = currDirection + nextDirection / 2;
        var p1 = currDirection;

        var headOffset = Vector2Extensions.LerpQuadraticBezier(p0, p2, p1, _t);

        _snakeHeadOffset = headOffset;
        _snakeHeadRotation = nextDirection;

        _tailOffset = Vector2.Lerp(Vector2.Zero, _snake[^2] - _snake[^1], _t);
    }

    void GoingBack(float elapsedSeconds, Direction direction)
    {
        if(_shakeDurationRemaining > 0)
        {
            _shakeDurationRemaining -= elapsedSeconds;

            var xOffset = Random.Shared.NextSingle(-.1f, .1f);
            var yOffset = Random.Shared.NextSingle(-.1f, .1f);
            _shakeOffset = new Vector2(xOffset, yOffset);

            if(_shakeDurationRemaining <= 0)
            {
                // Initialize next state
                ;
            }
        }
        else if(_goingBackRemaining1 > 0)
        {
            _goingBackRemaining1 -= elapsedSeconds;

            var currDirection = _lastDirection1.ToVector2();
            var p0 = currDirection / 2;
            var p1 = currDirection;
            var tailDirection = _snake[^1] - _snake[^2];

            _snakeHeadOffset = Vector2.Lerp(p0, p1, _goingBackRemaining1 / _goingBackStart1);
            _tailOffset = -Vector2.Lerp(Vector2.Zero, tailDirection, _goingBackRemaining1 / _goingBackStart1);

            if(_goingBackRemaining1 <= 0)
            {
                // Initialize next state
                _snake.Add(_removedTail1);

                _snakeHeadRotation = _lastDirection1.ToVector2();

                // Copied from the next state!
                var tailDirection1 = _snake[^1] - _snake[^2];
                _tailOffset = -Vector2.Lerp(tailDirection1 / 2, tailDirection1, _goingBackRemaining2 / _goingBackStart2);
            }
        }
        else if(_goingBackRemaining2 > 0)
        {
            _goingBackRemaining2 -= elapsedSeconds;

            var tailDirection = _snake[^1] - _snake[^2];
            _tailOffset = -Vector2.Lerp(tailDirection / 2, tailDirection, _goingBackRemaining2 / _goingBackStart2);

            if(_goingBackRemaining2 <= 0)
            {
                // Initialize next state
                ;
            }
        }
        else if(_waitingMenuRemaining > 0)
        {
            _waitingMenuRemaining -= elapsedSeconds;

            if(_waitingMenuRemaining <= 0)
            {
                // Initialize next state
                ;
            }
        }
    }

    void ChangeState(StateUpdate update)
    {
        _stateUpdate = update;
    }

    public void Update(float elapsedSeconds, Direction direction)
    {
        _stateUpdate!(elapsedSeconds, direction);
    }

    public void Draw(float elapsedSeconds, IRenderer renderer)
    {
        var head = _snake[0];
        var headFinal = Vector2.Clamp(head + _snakeHeadOffset, new Vector2(0, 0), new Vector2(playground.Width - 1, playground.Height - 1));
        var targetCameraPos = -(headFinal * playground.TileSize);

        _cameraPosition = Vector2.Lerp(_cameraPosition, targetCameraPos, elapsedSeconds * _cameraSmoothing);

        renderer.SetCamera(
            _cameraPosition, 
            0,
            _cameraZoom
        );

        playground.Draw(renderer);

        var snake = _snake;
        var snakeColor = Color.FromArgb(78, 124, 246);

        var tailFinal = (snake[^1] + _tailOffset) * playground.TileSize;
        renderer.DrawImage(_cellImage, tailFinal, playground.TileSize, 0, Vector2.Zero, snakeColor.Dark(0.04f * snake.Count));

        for (var i = 0; i < snake.Count - 1; i++)
        {
            var body = snake[i];
            renderer.DrawImage(_cellImage, body * playground.TileSize, playground.TileSize, 0, Vector2.Zero, snakeColor.Dark(0.04f * i));
        }

        var direction = _currDirection.ToVector2();
        var neckFinal = Vector2.Lerp(head, head + direction, _t) * playground.TileSize;
        headFinal = headFinal * playground.TileSize;
        
        renderer.DrawImage(_cellImage, neckFinal, playground.TileSize, 0, Vector2.Zero, snakeColor);
        renderer.DrawImage(_cellImage, headFinal, playground.TileSize, 0, Vector2.Zero, snakeColor);

        var eyeSize = playground.TileSize / 2.5F;
        var headRotation = MathF.Atan2(_snakeHeadRotation.Y, _snakeHeadRotation.X);
        var eyePosition1 = Vector2.Transform(Vector2.Zero, Matrix3x2.CreateRotation(headRotation, playground.TileSize / 2)) + headFinal;
        var eyePosition2 = Vector2.Transform(new Vector2(0, playground.TileSize.Y - eyeSize.Y), Matrix3x2.CreateRotation(headRotation, playground.TileSize / 2)) + headFinal;

        renderer.DrawImage(_eyeImage, eyePosition1, eyeSize, headRotation, Vector2.Zero, new Rectangle(20, 20, 40, 40), Color.White);
        renderer.DrawImage(_eyeImage, eyePosition2, eyeSize, headRotation, Vector2.Zero, new Rectangle(20, 20, 40, 40), Color.White);
    }
}
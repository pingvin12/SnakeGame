using System.Drawing;
using System.Numerics;
using SnakeCore;

namespace SnakeCore.Entities.Snake;

public class Snake : ICollidable
{
    private readonly List<Vector2> _segments = new();
    private Direction _currentDirection = Direction.Right;
    private List<Direction> _nextDirections = new();
    private bool _waitingForFirstInput = true;
    private float _moveProgress;

    // Death animation state
    private bool _isDying;
    private Vector2 _shakeOffset = Vector2.Zero;
    private float _shakeDurationRemaining;
    private float _goingBackRemaining1;
    private float _goingBackRemaining2;
    private float _waitingMenuRemaining;
    private Direction _lastDirection1 = Direction.None;
    private Direction _lastDirection2 = Direction.None;
    private Vector2 _removedTail1;
    private Vector2 _removedTail2;

    // Animation constants
    private const float SHAKE_DURATION = 0.16f;
    private const float GOING_BACK_DURATION1 = 0.18f;
    private const float GOING_BACK_DURATION2 = 0.06f;
    private const float WAITING_MENU_DURATION = 1.1f;

    // Animation properties
    public Vector2 HeadOffset { get; private set; } = Vector2.Zero;
    public Vector2 HeadRotation { get; private set; }
    public Vector2 TailOffset { get; private set; } = Vector2.Zero;
    public Vector2 ShakeOffset => _shakeOffset;

    public Vector2 Head => _segments[0];
    public Vector2 Tail => _segments[^1];
    public IReadOnlyList<Vector2> Segments => _segments;
    public Direction CurrentDirection => _currentDirection;
    public float MoveProgress => _moveProgress;
    public bool IsDead { get; private set; }
    public bool IsInDeathAnimation => _isDying;

    public virtual Color Color => Color.FromArgb(78, 124, 246); // Default blue color

    public Snake(Vector2 startPosition)
    {
        Initialize(startPosition);
    }

    public void Initialize(Vector2 startPosition)
    {
        _segments.Clear();
        _segments.Add(startPosition);
        _segments.Add(startPosition + new Vector2(-1, 0));
        _segments.Add(startPosition + new Vector2(-2, 0));

        _waitingForFirstInput = true;
        _moveProgress = 0;
        _currentDirection = Direction.Right;
        _nextDirections.Clear();

        HeadOffset = Vector2.Zero;
        HeadRotation = Vector2.Zero;
        TailOffset = Vector2.Zero;

        _isDying = false;
        IsDead = false;
        ResetDeathAnimation();
    }

    private void ResetDeathAnimation()
    {
        _shakeDurationRemaining = SHAKE_DURATION;
        _goingBackRemaining1 = GOING_BACK_DURATION1;
        _goingBackRemaining2 = GOING_BACK_DURATION2;
        _waitingMenuRemaining = WAITING_MENU_DURATION;
        _shakeOffset = Vector2.Zero;
        HeadOffset = Vector2.Zero;
        TailOffset = Vector2.Zero;
    }

    public virtual void Update(float elapsedSeconds, Direction inputDirection, float speed)
    {
        if (_isDying)
        {
            UpdateDeathAnimation(elapsedSeconds);
            return;
        }

        UpdateMovement(elapsedSeconds, inputDirection, speed);
    }

    private void UpdateMovement(float elapsedSeconds, Direction inputDirection, float speed)
    {
        if (_waitingForFirstInput
            && inputDirection != Direction.None
            && _currentDirection.Inverse() != inputDirection)
        {
            _waitingForFirstInput = false;
            _nextDirections.Add(inputDirection);
        }

        if (_waitingForFirstInput)
            return;

        if (inputDirection != Direction.None &&
            !_nextDirections.Contains(inputDirection) &&
            _nextDirections.LastOrDefault(_currentDirection) != inputDirection.Inverse())
        {
            _nextDirections.Add(inputDirection);
        }

        var oldProgress = _moveProgress;
        _moveProgress = Math.Min(1f, _moveProgress + elapsedSeconds / (1F / speed));
        
        // If we just completed a move, process it immediately
        if (oldProgress < 1f && _moveProgress >= 1f)
        {
            Move();
        }
        
        UpdateAnimation();
    }

    private void UpdateDeathAnimation(float elapsedSeconds)
    {
        if (_shakeDurationRemaining > 0)
        {
            _shakeDurationRemaining -= elapsedSeconds;
            var xOffset = Random.Shared.NextSingle(-.1f, .1f);
            var yOffset = Random.Shared.NextSingle(-.1f, .1f);
            _shakeOffset = new Vector2(xOffset, yOffset);
        }
        else if (_goingBackRemaining1 > 0)
        {
            _goingBackRemaining1 -= elapsedSeconds;
            UpdateGoingBackAnimation1();
        }
        else if (_goingBackRemaining2 > 0)
        {
            _goingBackRemaining2 -= elapsedSeconds;
            UpdateGoingBackAnimation2();
        }
        else if (_waitingMenuRemaining > 0)
        {
            _waitingMenuRemaining -= elapsedSeconds;
            if (_waitingMenuRemaining <= 0)
            {
                _isDying = false;
                IsDead = true;
            }
        }
    }

    private void UpdateGoingBackAnimation1()
    {
        var currDirection = _lastDirection1.ToVector2();
        var p0 = currDirection / 2;
        var p1 = currDirection;
        var tailDirection = _segments[^1] - _segments[^2];

        HeadOffset = Vector2.Lerp(p0, p1, _goingBackRemaining1 / GOING_BACK_DURATION1);
        TailOffset = -Vector2.Lerp(Vector2.Zero, tailDirection, _goingBackRemaining1 / GOING_BACK_DURATION1);

        if (_goingBackRemaining1 <= 0)
        {
            _segments.Add(_removedTail1);
            HeadRotation = _lastDirection1.ToVector2();
            var tailDirection1 = _segments[^1] - _segments[^2];
            TailOffset = -Vector2.Lerp(tailDirection1 / 2, tailDirection1, _goingBackRemaining2 / GOING_BACK_DURATION2);
        }
    }

    private void UpdateGoingBackAnimation2()
    {
        var tailDirection = _segments[^1] - _segments[^2];
        TailOffset = -Vector2.Lerp(tailDirection / 2, tailDirection, _goingBackRemaining2 / GOING_BACK_DURATION2);
    }

    private void UpdateAnimation()
    {
        // Simple linear interpolation for head movement
        var direction = _currentDirection.ToVector2();
        HeadOffset = direction * _moveProgress;
        HeadRotation = (_nextDirections.Count > 0 ? _nextDirections[0] : _currentDirection).ToVector2();

        // Simple linear interpolation for tail movement
        if (_segments.Count > 1)
        {
            TailOffset = Vector2.Lerp(Vector2.Zero, _segments[^2] - _segments[^1], _moveProgress);
        }
    }

    public bool Move()
    {
        if (_moveProgress < 1)
            return false;

        var newHead = _segments[0] + _currentDirection.ToVector2();

        _lastDirection2 = _lastDirection1;
        _lastDirection1 = _currentDirection;

        var previousRemovedTail = _removedTail2;
        _removedTail2 = _removedTail1;
        _removedTail1 = _segments[^1];

        if (_nextDirections.Count > 0)
        {
            _currentDirection = _nextDirections[0];
            _nextDirections.RemoveAt(0);
        }

        _moveProgress = 0;
        HeadOffset = Vector2.Zero;
        TailOffset = Vector2.Zero;

        _segments.Insert(0, newHead);
        _segments.RemoveAt(_segments.Count - 1);

        return true;
    }

    public void StartDeathAnimation()
    {
        _isDying = true;
        ResetDeathAnimation();

        // Store the current direction and head offset for death animation
        HeadOffset = Vector2.Zero;
        HeadRotation = _currentDirection.ToVector2();

        // Restore the snake to its position before the fatal move
        _segments.RemoveAt(0);
        _segments.Add(_removedTail1);

        _removedTail1 = _removedTail2;
        _removedTail2 = default;

        // Reset movement state
        _moveProgress = 0;
        _nextDirections.Clear();
    }

    public bool IsColliding(Vector2 position)
    {
        var nextHead = Head + _currentDirection.ToVector2();

        // Check self collision
        for (var i = 1; i < _segments.Count; i++)
        {
            if (_segments[i] == nextHead)
                return true;
        }

        return false;
    }
}
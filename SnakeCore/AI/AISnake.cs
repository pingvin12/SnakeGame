using System.Numerics;
using System.Drawing;
using SnakeCore.Entities.Snake;

namespace SnakeCore.AI;

public class AISnake : Snake
{
    private readonly Playground _playground;
    private Direction _lastDirection = Direction.None;
    private float _thinkTime;
    private const float THINK_INTERVAL = 0.1f;

    private static readonly Direction[] PossibleDirections = 
    {
        Direction.Up,
        Direction.Right,
        Direction.Down,
        Direction.Left
    };

    public override Color Color => Color.FromArgb(246, 78, 78); // Red tint for AI snake

    public AISnake(Vector2 startPosition, Playground playground) : base(startPosition)
    {
        _playground = playground;
    }

    public override void Update(float elapsedSeconds, Direction direction, float speed)
    {
        _thinkTime += elapsedSeconds;
        
        if (_thinkTime >= THINK_INTERVAL)
        {
            var nextMove = DecideNextMove();
            base.Update(elapsedSeconds, nextMove, speed);
            _thinkTime = 0;
        }
        else
        {
            base.Update(elapsedSeconds, Direction.None, speed);
        }
    }

    private Direction DecideNextMove()
    {
        if (CurrentDirection == Direction.None)
        {
            return IsSafeMove(Direction.Right) ? Direction.Right : Direction.Down;
        }

        var possibleMoves = GetPossibleMoves();
        
        if (!possibleMoves.Any())
        {
            return CurrentDirection;
        }

        var bestMove = FindBestMove(possibleMoves);
        _lastDirection = bestMove;
        return bestMove;
    }

    private List<Direction> GetPossibleMoves()
    {
        var moves = new List<Direction>();
        var inverse = CurrentDirection.Inverse();

        foreach (var dir in PossibleDirections)
        {
            if (dir != Direction.None && dir != inverse && IsSafeMove(dir))
            {
                moves.Add(dir);
            }
        }

        return moves;
    }

    private bool IsSafeMove(Direction direction)
    {
        if (direction == Direction.None)
            return false;

        var nextPos = Head + direction.ToVector2();

        // Check playground boundaries
        if (_playground.IsColliding(nextPos))
            return false;

        // Check self-collision
        foreach (var segment in Segments)
        {
            if (segment == nextPos)
                return false;
        }

        return true;
    }

    private Direction FindBestMove(List<Direction> possibleMoves)
    {
        var bestScore = float.MinValue;
        var bestMove = possibleMoves[0];

        foreach (var move in possibleMoves)
        {
            var score = ScoreMove(move);
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        return bestMove;
    }

    private float ScoreMove(Direction direction)
    {
        var nextPos = Head + direction.ToVector2();
        var score = 0f;

        // Prefer moves that don't get too close to walls
        var distanceToWall = GetDistanceToWall(nextPos);
        score += distanceToWall * 2;

        // Prefer moves that maximize open space
        var openSpace = CountOpenSpaces(nextPos);
        score += openSpace * 1.5f;

        // Prefer moves that don't make sharp turns
        if (_lastDirection != Direction.None && direction != _lastDirection)
        {
            score -= 0.5f;
        }

        return score;
    }

    private float GetDistanceToWall(Vector2 pos)
    {
        var distX = Math.Min(pos.X, _playground.Width - 1 - pos.X);
        var distY = Math.Min(pos.Y, _playground.Height - 1 - pos.Y);
        return Math.Min(distX, distY);
    }

    private int CountOpenSpaces(Vector2 pos)
    {
        var count = 0;
        var visited = new HashSet<Vector2>();
        var queue = new Queue<Vector2>();
        
        queue.Enqueue(pos);
        visited.Add(pos);

        // Flood fill to count accessible spaces
        while (queue.Count > 0 && count < 20) // Limit search to nearby spaces
        {
            var current = queue.Dequeue();
            count++;

            foreach (var dir in PossibleDirections)
            {
                if (dir == Direction.None) continue;
                
                var next = current + dir.ToVector2();
                if (!visited.Contains(next) && IsSafeMove(dir))
                {
                    queue.Enqueue(next);
                    visited.Add(next);
                }
            }
        }

        return count;
    }
} 
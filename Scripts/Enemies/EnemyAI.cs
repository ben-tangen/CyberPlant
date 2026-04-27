#nullable enable
using Godot;
using CyberPlant.Combat;
using PlayerCharacter = CyberPlant.Player.Player;

namespace CyberPlant.Enemies;

public partial class EnemyAI : Node
{
    [Export] public float PatrolSpeed { get; set; } = 100.0f;
    [Export] public float PatrolDistance { get; set; } = 140.0f;
    [Export] public float DetectionRange { get; set; } = 200.0f;
    [Export] public float AttackRange { get; set; } = 60.0f;
    [Export] public float AttackCooldown { get; set; } = 1.5f;
    [Export] public int AttackDamage { get; set; } = 15;
    [Export] public float ChaseSpeedMultiplier { get; set; } = 1.5f;
    [Export] public float GroundCheckForwardDistance { get; set; } = 24.0f;
    [Export] public float GroundCheckDepth { get; set; } = 80.0f;
    [Export] public float MaxFallSpeed { get; set; } = 1200.0f;

    private Enemy? _enemy;
    private CharacterBody2D? _enemyBody;
    private Node2D? _player;
    private float _timeSinceLastAttack = 0.0f;
    private int _patrolDirection = 1;
    private float _patrolOriginX;
    private float _gravity;

    public override void _Ready()
    {
        _enemy = GetParent<Enemy>();
        _enemyBody = GetParent<CharacterBody2D>();
        _gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");
        if (_enemyBody != null)
        {
            _patrolOriginX = _enemyBody.GlobalPosition.X;
        }

        var gameManager = GetNodeOrNull<Core.GameManager>("/root/GameManager");
        if (gameManager != null)
        {
            _player = gameManager.CurrentPlayer;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_enemyBody == null || _enemy == null || _enemy.CurrentHealth <= 0)
        {
            return;
        }

        if (_player == null)
        {
            return;
        }

        if (_enemy.IsInHitStun)
        {
            ApplyGravity(delta);
            _enemyBody.MoveAndSlide();
            _enemy.UpdateVisualState(_enemyBody.Velocity.X);
            _timeSinceLastAttack += (float)delta;
            return;
        }

        ApplyGravity(delta);
        float distanceToPlayer = _enemyBody.GlobalPosition.DistanceTo(_player.GlobalPosition);

        if (distanceToPlayer <= AttackRange)
        {
            Attack();
            StopMoving();
        }
        else if (distanceToPlayer <= DetectionRange)
        {
            ChasePlayer();
        }
        else
        {
            Patrol();
        }

        _enemyBody.MoveAndSlide();
        if (distanceToPlayer > DetectionRange && _enemyBody.IsOnWall())
        {
            _patrolDirection *= -1;
        }

        _enemy.UpdateVisualState(_enemyBody.Velocity.X);
        _timeSinceLastAttack += (float)delta;
    }

    private void Patrol()
    {
        if (_enemyBody == null)
        {
            return;
        }

        float patrolOffset = _enemyBody.GlobalPosition.X - _patrolOriginX;
        if (patrolOffset >= PatrolDistance)
        {
            _patrolDirection = -1;
        }
        else if (patrolOffset <= -PatrolDistance)
        {
            _patrolDirection = 1;
        }

        Vector2 velocity = _enemyBody.Velocity;
        velocity.X = _patrolDirection * PatrolSpeed;

        if (_enemyBody.IsOnFloor() && !HasGroundAhead(_patrolDirection))
        {
            _patrolDirection *= -1;
            velocity.X = _patrolDirection * PatrolSpeed;
        }

        _enemyBody.Velocity = velocity;
    }

    private void ChasePlayer()
    {
        if (_enemyBody == null || _player == null)
        {
            return;
        }

        Vector2 direction = (_player.GlobalPosition - _enemyBody.GlobalPosition).Normalized();
        Vector2 velocity = _enemyBody.Velocity;
        float desiredDirection = Mathf.Sign(direction.X);
        if (!Mathf.IsZeroApprox(desiredDirection) && _enemyBody.IsOnFloor() && !HasGroundAhead((int)desiredDirection))
        {
            velocity.X = 0.0f;
        }
        else
        {
            velocity.X = direction.X * PatrolSpeed * ChaseSpeedMultiplier;
        }

        _enemyBody.Velocity = velocity;
    }

    private void Attack()
    {
        if (_timeSinceLastAttack < AttackCooldown || _player == null || _enemyBody == null)
        {
            return;
        }

        if (_player is PlayerCharacter player)
        {
            player.TakeDamage(AttackDamage);
            player.ApplyEnemyHitFeedback(_enemyBody.GlobalPosition);
        }
        else if (_player is IDamageable damageable)
        {
            damageable.TakeDamage(AttackDamage);
        }

        _timeSinceLastAttack = 0.0f;
    }

    private void StopMoving()
    {
        if (_enemyBody == null)
        {
            return;
        }

        Vector2 velocity = _enemyBody.Velocity;
        velocity.X = 0.0f;
        _enemyBody.Velocity = velocity;
    }

    private void ApplyGravity(double delta)
    {
        if (_enemyBody == null)
        {
            return;
        }

        Vector2 velocity = _enemyBody.Velocity;
        if (!_enemyBody.IsOnFloor())
        {
            velocity.Y += _gravity * (float)delta;
            velocity.Y = Mathf.Min(velocity.Y, MaxFallSpeed);
        }
        else if (velocity.Y > 0.0f)
        {
            velocity.Y = 0.0f;
        }

        _enemyBody.Velocity = velocity;
    }

    private bool HasGroundAhead(int direction)
    {
        if (_enemyBody == null)
        {
            return true;
        }

        var query = PhysicsRayQueryParameters2D.Create(
            _enemyBody.GlobalPosition + new Vector2(direction * GroundCheckForwardDistance, 0.0f),
            _enemyBody.GlobalPosition + new Vector2(direction * GroundCheckForwardDistance, GroundCheckDepth));
        query.CollideWithAreas = false;
        query.Exclude = new Godot.Collections.Array<Rid> { _enemyBody.GetRid() };

        return _enemyBody.GetWorld2D().DirectSpaceState.IntersectRay(query).Count > 0;
    }
}

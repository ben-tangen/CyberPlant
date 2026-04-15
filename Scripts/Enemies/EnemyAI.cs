#nullable enable
using Godot;
using CyberPlant.Combat;

namespace CyberPlant.Enemies;

public partial class EnemyAI : Node
{
    [Export] public float PatrolSpeed { get; set; } = 100.0f;
    [Export] public float DetectionRange { get; set; } = 200.0f;
    [Export] public float AttackRange { get; set; } = 60.0f;
    [Export] public float AttackCooldown { get; set; } = 1.5f;
    [Export] public int AttackDamage { get; set; } = 15;

    private Enemy? _enemy;
    private CharacterBody2D? _enemyBody;
    private Node2D? _player;
    private float _timeSinceLastAttack = 0.0f;
    private int _patrolDirection = 1;

    public override void _Ready()
    {
        _enemy = GetParent<Enemy>();
        _enemyBody = GetParent<CharacterBody2D>();
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

        float distanceToPlayer = _enemyBody.GlobalPosition.DistanceTo(_player.GlobalPosition);

        if (distanceToPlayer <= AttackRange)
        {
            Attack();
        }
        else if (distanceToPlayer <= DetectionRange)
        {
            ChasePlayer();
        }
        else
        {
            Patrol();
        }

        _timeSinceLastAttack += (float)delta;
    }

    private void Patrol()
    {
        if (_enemyBody == null)
        {
            return;
        }

        Vector2 velocity = _enemyBody.Velocity;
        velocity.X = _patrolDirection * PatrolSpeed;

        if (_enemyBody.IsOnWall() || (_enemyBody.IsOnFloor() && Mathf.Abs(velocity.X) > 0))
        {
            _patrolDirection *= -1;
        }

        _enemyBody.Velocity = velocity;
        _enemyBody.MoveAndSlide();
    }

    private void ChasePlayer()
    {
        if (_enemyBody == null || _player == null)
        {
            return;
        }

        Vector2 direction = (_player.GlobalPosition - _enemyBody.GlobalPosition).Normalized();
        Vector2 velocity = _enemyBody.Velocity;
        velocity.X = direction.X * PatrolSpeed * 1.5f;

        _enemyBody.Velocity = velocity;
        _enemyBody.MoveAndSlide();
    }

    private void Attack()
    {
        if (_timeSinceLastAttack < AttackCooldown || _player == null)
        {
            return;
        }

        if (_player is IDamageable damageable)
        {
            damageable.TakeDamage(AttackDamage);
        }

        _timeSinceLastAttack = 0.0f;
    }
}

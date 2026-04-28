#nullable enable
using Godot;
using CyberPlant.Combat;
using PlayerCharacter = CyberPlant.Player.Player;

namespace CyberPlant.Enemies;

/// <summary>
/// Boss AI with 2-phase behavior. Phase 1 is cautious patrol/chase. Phase 2 is aggressive chasing and frequent attacks.
/// </summary>
public partial class BossAI : Node
{
    [Export] public float Phase1PatrolSpeed { get; set; } = 80.0f;
    [Export] public float Phase2PatrolSpeed { get; set; } = 120.0f;
    [Export] public float PatrolDistance { get; set; } = 200.0f;
    [Export] public float Phase1DetectionRange { get; set; } = 250.0f;
    [Export] public float Phase2DetectionRange { get; set; } = 350.0f;
    [Export] public float Phase1AttackRange { get; set; } = 70.0f;
    [Export] public float Phase2AttackRange { get; set; } = 80.0f;
    [Export] public float Phase1AttackCooldown { get; set; } = 2.0f;
    [Export] public float Phase2AttackCooldown { get; set; } = 1.2f;
    [Export] public int AttackDamage { get; set; } = 25;
    [Export] public float ChaseSpeedMultiplier { get; set; } = 1.6f;
    [Export] public float GroundCheckForwardDistance { get; set; } = 30.0f;
    [Export] public float GroundCheckDepth { get; set; } = 100.0f;
    [Export] public float MaxFallSpeed { get; set; } = 1200.0f;

    private Boss? _boss;
    private CharacterBody2D? _bossBody;
    private Node2D? _player;
    private float _timeSinceLastAttack = 0.0f;
    private int _patrolDirection = 1;
    private float _patrolOriginX;
    private float _gravity;

    public override void _Ready()
    {
        _boss = GetParent<Boss>();
        _bossBody = GetParent<CharacterBody2D>();
        _gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");
        if (_bossBody != null)
        {
            _patrolOriginX = _bossBody.GlobalPosition.X;
        }

        var gameManager = GetNodeOrNull<Core.GameManager>("/root/GameManager");
        if (gameManager != null)
        {
            _player = gameManager.CurrentPlayer;
        }

        if (_boss != null)
        {
            _boss.PhaseChanged += OnPhaseChanged;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_bossBody == null || _boss == null || _boss.CurrentHealth <= 0)
        {
            return;
        }

        if (_player == null)
        {
            return;
        }

        if (_boss.IsInHitStun)
        {
            ApplyGravity(delta);
            _bossBody.MoveAndSlide();
            _boss.UpdateVisualState(_bossBody.Velocity.X);
            _timeSinceLastAttack += (float)delta;
            return;
        }

        ApplyGravity(delta);
        float distanceToPlayer = _bossBody.GlobalPosition.DistanceTo(_player.GlobalPosition);
        float detectionRange = _boss.CurrentPhase == 1 ? Phase1DetectionRange : Phase2DetectionRange;
        float attackRange = _boss.CurrentPhase == 1 ? Phase1AttackRange : Phase2AttackRange;

        if (distanceToPlayer <= attackRange)
        {
            Attack();
            StopMoving();
        }
        else if (distanceToPlayer <= detectionRange)
        {
            ChasePlayer();
        }
        else
        {
            Patrol();
        }

        _bossBody.MoveAndSlide();
        if (distanceToPlayer > detectionRange && _bossBody.IsOnWall())
        {
            _patrolDirection *= -1;
        }

        _boss.UpdateVisualState(_bossBody.Velocity.X);
        _timeSinceLastAttack += (float)delta;
    }

    private void Patrol()
    {
        if (_bossBody == null)
        {
            return;
        }

        float patrolSpeed = _boss?.CurrentPhase == 2 ? Phase2PatrolSpeed : Phase1PatrolSpeed;
        float patrolOffset = _bossBody.GlobalPosition.X - _patrolOriginX;
        if (patrolOffset >= PatrolDistance)
        {
            _patrolDirection = -1;
        }
        else if (patrolOffset <= -PatrolDistance)
        {
            _patrolDirection = 1;
        }

        Vector2 velocity = _bossBody.Velocity;
        velocity.X = _patrolDirection * patrolSpeed;

        if (_bossBody.IsOnFloor() && !HasGroundAhead(_patrolDirection))
        {
            _patrolDirection *= -1;
            velocity.X = _patrolDirection * patrolSpeed;
        }

        _bossBody.Velocity = velocity;
    }

    private void ChasePlayer()
    {
        if (_bossBody == null || _player == null || _boss == null)
        {
            return;
        }

        Vector2 direction = (_player.GlobalPosition - _bossBody.GlobalPosition).Normalized();
        Vector2 velocity = _bossBody.Velocity;
        float desiredDirection = Mathf.Sign(direction.X);
        float patrolSpeed = _boss.CurrentPhase == 2 ? Phase2PatrolSpeed : Phase1PatrolSpeed;
        if (!Mathf.IsZeroApprox(desiredDirection) && _bossBody.IsOnFloor() && !HasGroundAhead((int)desiredDirection))
        {
            velocity.X = 0.0f;
        }
        else
        {
            velocity.X = direction.X * patrolSpeed * ChaseSpeedMultiplier;
        }

        _bossBody.Velocity = velocity;
    }

    private void Attack()
    {
        if (_boss == null || _player == null || _bossBody == null)
        {
            return;
        }

        float attackCooldown = _boss.CurrentPhase == 1 ? Phase1AttackCooldown : Phase2AttackCooldown;
        if (_timeSinceLastAttack < attackCooldown)
        {
            return;
        }

        Vector2 direction = (_player.GlobalPosition - _bossBody.GlobalPosition).Normalized();
        Vector2 hitPosition = _bossBody.GlobalPosition + direction * 50.0f;
        var spaceState = _bossBody.GetWorld2D().DirectSpaceState;

        var query = new PhysicsShapeQueryParameters2D
        {
            Shape = new CircleShape2D { Radius = 40.0f },
            Transform = new Transform2D(0.0f, hitPosition)
        };

        var results = spaceState.IntersectShape(query);
        foreach (var result in results)
        {
            var collider = (Node)result["collider"];
            if (collider is PlayerCharacter playerChar && playerChar == _player)
            {
                playerChar.ApplyEnemyHitFeedback(_bossBody.GlobalPosition);
            }
        }

        _timeSinceLastAttack = 0.0f;
    }

    private void StopMoving()
    {
        if (_bossBody == null)
        {
            return;
        }

        Vector2 velocity = _bossBody.Velocity;
        velocity.X = 0.0f;
        _bossBody.Velocity = velocity;
    }

    private void ApplyGravity(double delta)
    {
        if (_bossBody == null)
        {
            return;
        }

        Vector2 velocity = _bossBody.Velocity;
        velocity.Y = Mathf.Min(velocity.Y + _gravity * (float)delta, MaxFallSpeed);
        _bossBody.Velocity = velocity;
    }

    private bool HasGroundAhead(int direction)
    {
        if (_bossBody == null)
        {
            return false;
        }

        Vector2 rayStart = _bossBody.GlobalPosition + Vector2.Right * direction * GroundCheckForwardDistance;
        Vector2 rayEnd = rayStart + Vector2.Down * GroundCheckDepth;

        var spaceState = _bossBody.GetWorld2D().DirectSpaceState;
        var query = PhysicsRayQueryParameters2D.Create(rayStart, rayEnd);
        var result = spaceState.IntersectRay(query);
        return result.Count > 0;
    }

    private void OnPhaseChanged(int newPhase)
    {
        GD.Print($"BossAI detected phase change to {newPhase}");
    }
}

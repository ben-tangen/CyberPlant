#nullable enable
using Godot;
using System.Collections.Generic;
using CyberPlant.Combat;
using PlayerCharacter = CyberPlant.Player.Player;

namespace CyberPlant.Enemies;

/// <summary>
/// Boss AI with 2-phase behavior. Phase 1 is cautious and readable. Phase 2 is faster and more aggressive.
/// </summary>
public partial class BossAI : Node
{
    [Export] public float Phase1PatrolSpeed { get; set; } = 84.0f;
    [Export] public float Phase2PatrolSpeed { get; set; } = 132.0f;
    [Export] public float PatrolDistance { get; set; } = 220.0f;
    [Export] public float Phase1DetectionRange { get; set; } = 270.0f;
    [Export] public float Phase2DetectionRange { get; set; } = 380.0f;
    [Export] public float Phase1AttackRange { get; set; } = 138.0f;
    [Export] public float Phase2AttackRange { get; set; } = 184.0f;
    [Export] public float AttackHeightTolerance { get; set; } = 150.0f;
    [Export] public float Phase1AttackCooldown { get; set; } = 1.8f;
    [Export] public float Phase2AttackCooldown { get; set; } = 1.05f;
    [Export] public float Phase1TelegraphDuration { get; set; } = 0.55f;
    [Export] public float Phase2TelegraphDuration { get; set; } = 0.34f;
    [Export] public float Phase1AttackActiveDuration { get; set; } = 0.18f;
    [Export] public float Phase2AttackActiveDuration { get; set; } = 0.22f;
    [Export] public float Phase1RecoveryDuration { get; set; } = 0.72f;
    [Export] public float Phase2RecoveryDuration { get; set; } = 0.52f;
    [Export] public int AttackDamage { get; set; } = 18;
    [Export] public int Phase2AttackDamage { get; set; } = 24;
    [Export] public float Phase2LungeSpeed { get; set; } = 220.0f;
    [Export] public float ChaseSpeedMultiplier { get; set; } = 1.75f;
    [Export] public float GroundCheckForwardDistance { get; set; } = 30.0f;
    [Export] public float GroundCheckDepth { get; set; } = 100.0f;
    [Export] public float MaxFallSpeed { get; set; } = 1200.0f;

    private enum AttackState
    {
        Idle,
        Telegraph,
        Active,
        Recovery,
    }

    private Boss? _boss;
    private CharacterBody2D? _bossBody;
    private Node2D? _player;
    private Area2D? _attackHitbox;
    private AttackState _attackState = AttackState.Idle;
    private float _attackStateTimer = 0.0f;
    private float _timeSinceLastAttack = 0.0f;
    private int _patrolDirection = 1;
    private float _patrolOriginX;
    private float _gravity;
    private float _attackDirection = 1.0f;
    private readonly HashSet<ulong> _hitTargetsThisAttack = new();

    public override void _Ready()
    {
        _boss = GetParent<Boss>();
        _bossBody = GetParent<CharacterBody2D>();
        _attackHitbox = GetParent().GetNodeOrNull<Area2D>("AttackHitbox");
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
        if (_bossBody == null || _boss == null || _boss.CurrentHealth <= 0 || _player == null)
        {
            return;
        }

        float dt = (float)delta;

        if (_boss.IsInHitStun)
        {
            CancelAttackSequence();
            ApplyGravity(dt);
            _bossBody.MoveAndSlide();
            _boss.UpdateVisualState(_bossBody.Velocity.X);
            _timeSinceLastAttack += dt;
            return;
        }

        ApplyGravity(dt);

        float distanceToPlayer = _bossBody.GlobalPosition.DistanceTo(_player.GlobalPosition);
        float detectionRange = _boss.CurrentPhase == 1 ? Phase1DetectionRange : Phase2DetectionRange;
        float attackRange = _boss.CurrentPhase == 1 ? Phase1AttackRange : Phase2AttackRange;

        if (_attackState != AttackState.Idle)
        {
            UpdateAttackSequence(dt);
        }
        else if (IsPlayerInAttackRange(attackRange) && _timeSinceLastAttack >= GetAttackCooldown())
        {
            BeginAttackSequence();
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

        if (_attackState == AttackState.Idle)
        {
            _timeSinceLastAttack += dt;
        }
    }

    private void Patrol()
    {
        if (_bossBody == null || _boss == null)
        {
            return;
        }

        float patrolSpeed = _boss.CurrentPhase == 2 ? Phase2PatrolSpeed : Phase1PatrolSpeed;
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

    private void BeginAttackSequence()
    {
        if (_boss == null || _player == null || _bossBody == null)
        {
            return;
        }

        _attackDirection = Mathf.Sign(_player.GlobalPosition.X - _bossBody.GlobalPosition.X);
        if (Mathf.IsZeroApprox(_attackDirection))
        {
            _attackDirection = _patrolDirection;
        }

        _attackState = AttackState.Telegraph;
        _attackStateTimer = _boss.CurrentPhase == 1 ? Phase1TelegraphDuration : Phase2TelegraphDuration;
        _timeSinceLastAttack = 0.0f;
        _hitTargetsThisAttack.Clear();

        _boss.SetTelegraphState(true, _boss.CurrentPhase == 2, 0.0f);
        StopMoving();
        _boss.UpdateVisualState(0.0f);
    }

    private void UpdateAttackSequence(float dt)
    {
        if (_boss == null || _bossBody == null)
        {
            return;
        }

        switch (_attackState)
        {
            case AttackState.Telegraph:
            {
                _attackStateTimer -= dt;
                StopMoving();

                float telegraphDuration = _boss.CurrentPhase == 1 ? Phase1TelegraphDuration : Phase2TelegraphDuration;
                float progress = 1.0f - Mathf.Clamp(_attackStateTimer / telegraphDuration, 0.0f, 1.0f);
                _boss.SetTelegraphState(true, _boss.CurrentPhase == 2, progress);

                if (_attackStateTimer <= 0.0f)
                {
                    _attackState = AttackState.Active;
                    _attackStateTimer = _boss.CurrentPhase == 1 ? Phase1AttackActiveDuration : Phase2AttackActiveDuration;
                    _boss.SetTelegraphState(false, _boss.CurrentPhase == 2, 1.0f);
                    ResolveAttackHits();
                }

                break;
            }
            case AttackState.Active:
            {
                _attackStateTimer -= dt;
                ApplyAttackMotion();
                ResolveAttackHits();

                if (_attackStateTimer <= 0.0f)
                {
                    _attackState = AttackState.Recovery;
                    _attackStateTimer = _boss.CurrentPhase == 1 ? Phase1RecoveryDuration : Phase2RecoveryDuration;
                    StopMoving();
                }

                break;
            }
            case AttackState.Recovery:
            {
                _attackStateTimer -= dt;
                StopMoving();

                if (_attackStateTimer <= 0.0f)
                {
                    _attackState = AttackState.Idle;
                    _timeSinceLastAttack = 0.0f;
                    _boss.ClearTelegraphState();
                }

                break;
            }
        }
    }

    private void ApplyAttackMotion()
    {
        if (_bossBody == null || _boss == null)
        {
            return;
        }

        Vector2 velocity = _bossBody.Velocity;
        if (_boss.CurrentPhase == 2)
        {
            velocity.X = _attackDirection * Phase2LungeSpeed;
        }
        else
        {
            velocity.X = _attackDirection * Phase2LungeSpeed * 0.4f;
        }

        _bossBody.Velocity = velocity;
    }

    private void ResolveAttackHits()
    {
        if (_boss == null || _bossBody == null || _attackHitbox == null)
        {
            return;
        }

        foreach (var body in _attackHitbox.GetOverlappingBodies())
        {
            TryDamageTarget(body);
        }

        foreach (var area in _attackHitbox.GetOverlappingAreas())
        {
            TryDamageTarget(area);
        }
    }

    private void TryDamageTarget(Node node)
    {
        if (_boss == null || _bossBody == null)
        {
            return;
        }

        var damageable = ResolveDamageable(node);
        if (damageable == null)
        {
            return;
        }

        var targetNode = damageable as Node;
        if (targetNode == null || !targetNode.IsInGroup("player"))
        {
            return;
        }

        if (_hitTargetsThisAttack.Contains(targetNode.GetInstanceId()))
        {
            return;
        }

        _hitTargetsThisAttack.Add(targetNode.GetInstanceId());

        int damage = _boss.CurrentPhase == 2 ? Phase2AttackDamage : AttackDamage;
        damageable.TakeDamage(damage);

        if (targetNode is PlayerCharacter player)
        {
            player.ApplyEnemyHitFeedback(_bossBody.GlobalPosition);
        }
    }

    private bool IsPlayerInAttackRange(float attackRange)
    {
        if (_bossBody == null || _player == null)
        {
            return false;
        }

        Vector2 offsetToPlayer = _player.GlobalPosition - _bossBody.GlobalPosition;
        return Mathf.Abs(offsetToPlayer.X) <= attackRange
            && Mathf.Abs(offsetToPlayer.Y) <= AttackHeightTolerance;
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

    private void CancelAttackSequence()
    {
        _attackState = AttackState.Idle;
        _attackStateTimer = 0.0f;
        _timeSinceLastAttack = 0.0f;
        _hitTargetsThisAttack.Clear();

        if (_boss != null)
        {
            _boss.ClearTelegraphState();
        }
    }

    private float GetAttackCooldown()
    {
        if (_boss == null)
        {
            return Phase1AttackCooldown;
        }

        return _boss.CurrentPhase == 2 ? Phase2AttackCooldown : Phase1AttackCooldown;
    }

    private void ApplyGravity(double delta)
    {
        if (_bossBody == null)
        {
            return;
        }

        Vector2 velocity = _bossBody.Velocity;
        velocity.Y = Mathf.Min(velocity.Y + _gravity * (float)delta, MaxFallSpeed);
        if (_bossBody.IsOnFloor() && velocity.Y > 0.0f)
        {
            velocity.Y = 0.0f;
        }

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

    private static IDamageable? ResolveDamageable(Node startNode)
    {
        Node? current = startNode;
        while (current != null)
        {
            if (current is IDamageable damageable)
            {
                return damageable;
            }

            current = current.GetParent();
        }

        return null;
    }

    private void OnPhaseChanged(int newPhase)
    {
        if (newPhase == 2)
        {
            _boss?.ClearTelegraphState();
        }

        GD.Print($"BossAI detected phase change to {newPhase}");
    }
}

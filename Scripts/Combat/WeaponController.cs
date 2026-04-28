#nullable enable
using Godot;
using CyberPlant.Combat;
using CyberPlant.Core;
using CyberPlant.Enemies;
using System.Collections.Generic;

namespace CyberPlant.Player;

public partial class WeaponController : Area2D
{
    [Export] public float AttackCooldown { get; set; } = 0.3f;
    [Export] public string TargetDamageGroup { get; set; } = "enemy";

    private Weapon? _currentWeapon;
    private CollisionShape2D? _hitCollisionShape;
    private CircleShape2D? _hitShape;
    private double _timeSinceLastAttack = 0.0;
    private bool _isAttacking = false;
    private float _baseAttackCooldown;
    private float _baseHitRadius;
    private Vector2 _baseHitOffset;
    private readonly HashSet<ulong> _hitTargetsThisAttack = new();

    public override void _Ready()
    {
        _baseAttackCooldown = AttackCooldown;
        _hitCollisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        _hitShape = _hitCollisionShape?.Shape as CircleShape2D;
        if (_hitCollisionShape != null)
        {
            _baseHitOffset = _hitCollisionShape.Position;
        }

        if (_hitShape != null)
        {
            _baseHitRadius = _hitShape.Radius;
        }

        AreaEntered += OnAreaEntered;
        BodyEntered += OnBodyEntered;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isAttacking)
        {
            _timeSinceLastAttack += delta;
            if (_timeSinceLastAttack >= AttackCooldown)
            {
                _isAttacking = false;
                _timeSinceLastAttack = 0.0;
            }
        }
    }

    public void SetWeapon(Weapon? weapon)
    {
        _currentWeapon = weapon;
        ApplyWeaponSettings();
    }

    public void Attack()
    {
        if (_isAttacking || _currentWeapon == null)
        {
            return;
        }

        _isAttacking = true;
        _timeSinceLastAttack = 0.0;
        _hitTargetsThisAttack.Clear();

        var overlappingAreas = GetOverlappingAreas();
        foreach (var area in overlappingAreas)
        {
            TryDamageTarget(area);
        }

        var overlappingBodies = GetOverlappingBodies();
        foreach (var body in overlappingBodies)
        {
            TryDamageTarget(body);
        }
    }

    private void OnAreaEntered(Area2D area)
    {
        if (_isAttacking && _currentWeapon != null)
        {
            TryDamageTarget(area);
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (_isAttacking && _currentWeapon != null)
        {
            TryDamageTarget(body);
        }
    }

    private void TryDamageTarget(Node node)
    {
        if (_currentWeapon == null)
        {
            return;
        }

        var damageable = ResolveDamageable(node);
        if (damageable == null)
        {
            return;
        }

        var targetNode = damageable as Node;
        if (targetNode == null)
        {
            return;
        }

        if (!targetNode.IsInGroup(TargetDamageGroup))
        {
            return;
        }

        if (_hitTargetsThisAttack.Contains(targetNode.GetInstanceId()))
        {
            return;
        }

        _hitTargetsThisAttack.Add(targetNode.GetInstanceId());
        damageable.TakeDamage(_currentWeapon.Damage);

        if (targetNode is Enemy enemy)
        {
            enemy.ApplyWeaponHitFeedback(GlobalPosition);
        }
        else if (targetNode is Boss boss)
        {
            boss.ApplyWeaponHitFeedback(GlobalPosition);
        }
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

    private void ApplyWeaponSettings()
    {
        AttackCooldown = _currentWeapon?.AttackCooldown ?? _baseAttackCooldown;

        if (_hitCollisionShape == null || _hitShape == null)
        {
            return;
        }

        _hitShape.Radius = _currentWeapon?.HitRadius ?? _baseHitRadius;
        Vector2 hitOffset = _baseHitOffset;
        hitOffset.X = _currentWeapon?.HitOffsetX ?? _baseHitOffset.X;
        _hitCollisionShape.Position = hitOffset;
    }
}

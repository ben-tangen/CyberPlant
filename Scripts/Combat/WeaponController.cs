#nullable enable
using Godot;
using CyberPlant.Combat;
using CyberPlant.Core;
using System.Collections.Generic;

namespace CyberPlant.Player;

public partial class WeaponController : Area2D
{
    [Export] public float AttackCooldown { get; set; } = 0.3f;
    [Export] public string TargetDamageGroup { get; set; } = "enemy";

    private Weapon? _currentWeapon;
    private double _timeSinceLastAttack = 0.0;
    private bool _isAttacking = false;
    private readonly HashSet<ulong> _hitTargetsThisAttack = new();

    public override void _Ready()
    {
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
}

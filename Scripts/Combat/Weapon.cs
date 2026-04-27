using Godot;

namespace CyberPlant.Combat;

public sealed class Weapon
{
    public Weapon(
        string id,
        string displayName,
        int damage,
        float attackCooldown,
        float hitRadius,
        float hitOffsetX)
    {
        Id = id;
        DisplayName = displayName;
        Damage = damage;
        AttackCooldown = attackCooldown;
        HitRadius = hitRadius;
        HitOffsetX = hitOffsetX;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public int Damage { get; }

    public float AttackCooldown { get; }

    public float HitRadius { get; }

    public float HitOffsetX { get; }
}

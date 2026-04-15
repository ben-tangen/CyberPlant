using Godot;

namespace CyberPlant.Combat;

public sealed class Weapon
{
    public Weapon(string id, string displayName, int damage, float attackDuration = 0.3f)
    {
        Id = id;
        DisplayName = displayName;
        Damage = damage;
        AttackDuration = attackDuration;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public int Damage { get; }

    public float AttackDuration { get; }
}

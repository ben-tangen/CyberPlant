using Godot;

namespace CyberPlant.Combat;

public interface IDamageable
{
    void TakeDamage(int amount);
}

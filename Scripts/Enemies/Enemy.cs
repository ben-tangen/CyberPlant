#nullable enable
using Godot;
using CyberPlant.Combat;
using CyberPlant.Core;

namespace CyberPlant.Enemies;

public partial class Enemy : CharacterBody2D, IDamageable
{
    [Signal]
    public delegate void HealthChangedEventHandler(int currentHealth, int maxHealth);

    [Signal]
    public delegate void DiedEventHandler();

    [Export] public int MaxHealth { get; set; } = 30;
    [Export] public int WaterDropAmount { get; set; } = 5;

    public int CurrentHealth { get; private set; }

    private ProgressBar? _healthBar;

    public override void _Ready()
    {
        AddToGroup("enemy");

        _healthBar = GetNodeOrNull<ProgressBar>("HealthBar");
        CurrentHealth = MaxHealth;
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
        UpdateHealthBarDisplay();
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || CurrentHealth <= 0)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
        UpdateHealthBarDisplay();

        if (CurrentHealth == 0)
        {
            Die();
        }
    }

    private void UpdateHealthBarDisplay()
    {
        if (_healthBar == null)
        {
            return;
        }

        _healthBar.MaxValue = MaxHealth;
        _healthBar.Value = CurrentHealth;
    }

    private void Die()
    {
        var gameManager = GetNodeOrNull<GameManager>("/root/GameManager");
        if (gameManager != null)
        {
            gameManager.AddWater(WaterDropAmount);
        }

        EmitSignal(SignalName.Died);
        QueueFree();
    }
}

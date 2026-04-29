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

    [Export] public int MaxHealth { get; set; } = 32;
    [Export] public int WaterDropAmount { get; set; } = 5;
    [Export] public float HitStunDuration { get; set; } = 0.10f;
    [Export] public float HitFlashDuration { get; set; } = 0.07f;
    [Export] public float KnockbackForce { get; set; } = 240.0f;
    [Export] public float KnockbackLift { get; set; } = 150.0f;
    [Export] public float KnockbackDamping { get; set; } = 980.0f;

    public int CurrentHealth { get; private set; }
    public bool IsInHitStun => _hitStunRemaining > 0.0f;

    private AnimatedSprite2D? _animatedSprite;
    private ProgressBar? _healthBar;
    private float _hitStunRemaining;
    private float _hitFlashRemaining;

    public override void _Ready()
    {
        AddToGroup("enemy");

        _animatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        _healthBar = GetNodeOrNull<ProgressBar>("HealthBar");
        if (_healthBar != null)
        {
            _healthBar.ShowPercentage = false;
        }

        CurrentHealth = MaxHealth;
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
        _animatedSprite?.Play("idle");
        UpdateHealthBarDisplay();
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        if (_hitStunRemaining > 0.0f)
        {
            _hitStunRemaining = Mathf.Max(0.0f, _hitStunRemaining - dt);
            Vector2 velocity = Velocity;
            velocity.X = Mathf.MoveToward(velocity.X, 0.0f, KnockbackDamping * dt);
            Velocity = velocity;
        }

        if (_hitFlashRemaining > 0.0f)
        {
            _hitFlashRemaining = Mathf.Max(0.0f, _hitFlashRemaining - dt);
            Modulate = new Color(1.22f, 0.7f, 0.68f, 1.0f);
        }
        else if (Modulate != Colors.White)
        {
            Modulate = Colors.White;
        }
    }

    public void UpdateVisualState(float horizontalVelocity)
    {
        if (_animatedSprite == null)
        {
            return;
        }

        if (Mathf.IsZeroApprox(horizontalVelocity))
        {
            if (_animatedSprite.Animation != "idle")
            {
                _animatedSprite.Play("idle");
            }

            _animatedSprite.FlipH = false;
            return;
        }

        if (_animatedSprite.Animation != "walk")
        {
            _animatedSprite.Play("walk");
        }

        _animatedSprite.FlipH = horizontalVelocity > 0.0f;
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

    public void ApplyWeaponHitFeedback(Vector2 attackerPosition)
    {
        float horizontalDirection = Mathf.Sign(GlobalPosition.X - attackerPosition.X);
        if (Mathf.IsZeroApprox(horizontalDirection))
        {
            horizontalDirection = 1.0f;
        }

        Vector2 velocity = Velocity;
        velocity.X = horizontalDirection * KnockbackForce;

        if (IsOnFloor())
        {
            velocity.Y = -KnockbackLift;
        }

        Velocity = velocity;
        _hitStunRemaining = Mathf.Max(_hitStunRemaining, HitStunDuration);
        _hitFlashRemaining = Mathf.Max(_hitFlashRemaining, HitFlashDuration);
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

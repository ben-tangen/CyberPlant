#nullable enable
using Godot;
using CyberPlant.Combat;
using CyberPlant.Core;

namespace CyberPlant.Enemies;

/// <summary>
/// Boss enemy with 2-phase behavior: Phase 1 (aggressive patrol/chase), Phase 2 (harder attacks, faster).
/// </summary>
public partial class Boss : CharacterBody2D, IDamageable
{
    [Signal]
    public delegate void HealthChangedEventHandler(int currentHealth, int maxHealth);

    [Signal]
    public delegate void DiedEventHandler();

    [Signal]
    public delegate void PhaseChangedEventHandler(int phase);

    [Export] public int MaxHealth { get; set; } = 150;
    [Export] public int WaterDropAmount { get; set; } = 50;
    [Export] public float HitStunDuration { get; set; } = 0.15f;
    [Export] public float HitFlashDuration { get; set; } = 0.1f;
    [Export] public float KnockbackForce { get; set; } = 200.0f;
    [Export] public float KnockbackLift { get; set; } = 120.0f;
    [Export] public float KnockbackDamping { get; set; } = 800.0f;

    public int CurrentHealth { get; private set; }
    public bool IsInHitStun => _hitStunRemaining > 0.0f;
    public int CurrentPhase { get; private set; } = 1;

    private AnimatedSprite2D? _animatedSprite;
    private ProgressBar? _healthBar;
    private float _hitStunRemaining;
    private float _hitFlashRemaining;
    private int _damageThresholdForPhase2;

    public override void _Ready()
    {
        AddToGroup("boss");

        _animatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        _healthBar = GetNodeOrNull<ProgressBar>("HealthBar");
        CurrentHealth = MaxHealth;
        _damageThresholdForPhase2 = MaxHealth / 2; // Phase 2 at 50% health
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
            Modulate = new Color(1.5f, 0.5f, 0.5f, 1.0f);
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

        // Check for phase transition
        if (CurrentPhase == 1 && CurrentHealth <= _damageThresholdForPhase2)
        {
            CurrentPhase = 2;
            EmitSignal(SignalName.PhaseChanged, CurrentPhase);
            GD.Print($"Boss entered Phase 2!");
        }

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    public void ApplyWeaponHitFeedback(Vector2 knockbackDirection, int knockbackForce, int knockbackLift)
    {
        _hitStunRemaining = HitStunDuration;
        _hitFlashRemaining = HitFlashDuration;

        Vector2 velocity = Velocity;
        velocity.X = knockbackDirection.X * knockbackForce;
        velocity.Y = -knockbackLift;
        Velocity = velocity;
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
        var gameManager = GetNodeOrNull<Core.GameManager>("/root/GameManager");
        if (gameManager != null)
        {
            gameManager.AddWater(WaterDropAmount);
        }

        EmitSignal(SignalName.Died);
        QueueFree();
    }
}

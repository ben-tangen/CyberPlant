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

    [Export] public int MaxHealth { get; set; } = 200;
    [Export] public int WaterDropAmount { get; set; } = 75;
    [Export] public float HitStunDuration { get; set; } = 0.20f;
    [Export] public float HitFlashDuration { get; set; } = 0.12f;
    [Export] public float KnockbackForce { get; set; } = 190.0f;
    [Export] public float KnockbackLift { get; set; } = 140.0f;
    [Export] public float KnockbackDamping { get; set; } = 920.0f;

    public int CurrentHealth { get; private set; }
    public bool IsInHitStun => _hitStunRemaining > 0.0f;
    public int CurrentPhase { get; private set; } = 1;

    private AnimatedSprite2D? _animatedSprite;
    private ProgressBar? _healthBar;
    private float _hitStunRemaining;
    private float _hitFlashRemaining;
    private int _damageThresholdForPhase2;
    private Vector2 _baseSpriteScale = Vector2.One;
    private bool _isFacingRight = true;
    private bool _isTelegraphing;
    private bool _isTelegraphPhaseTwo;
    private float _telegraphProgress;
    private float _telegraphPulseTime;

    private readonly Color _hitFlashColor = new(1.45f, 0.55f, 0.55f, 1.0f);
    private readonly Color _phaseOneTelegraphColor = new(1.0f, 0.66f, 0.42f, 1.0f);
    private readonly Color _phaseTwoTelegraphColor = new(1.0f, 0.82f, 0.36f, 1.0f);

    public override void _Ready()
    {
        AddToGroup("enemy");
        AddToGroup("boss");

        _animatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        _healthBar = GetNodeOrNull<ProgressBar>("HealthBar");
        if (_animatedSprite != null)
        {
            _baseSpriteScale = _animatedSprite.Scale;
        }

        CurrentHealth = MaxHealth;
        _damageThresholdForPhase2 = MaxHealth / 2; // Phase 2 at 50% health
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
        _animatedSprite?.Play("idle");
        UpdateHealthBarDisplay();
        RefreshCombatVisualState(0.0f);
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
        }

        RefreshCombatVisualState(dt);
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

            RefreshFacing();
            return;
        }

        _isFacingRight = horizontalVelocity > 0.0f;

        if (_animatedSprite.Animation != "walk")
        {
            _animatedSprite.Play("walk");
        }

        RefreshFacing();
    }

    public void SetTelegraphState(bool isTelegraphing, bool isPhaseTwo, float progress)
    {
        _isTelegraphing = isTelegraphing;
        _isTelegraphPhaseTwo = isPhaseTwo;
        _telegraphProgress = Mathf.Clamp(progress, 0.0f, 1.0f);

        if (!isTelegraphing)
        {
            _telegraphPulseTime = 0.0f;
        }

        RefreshCombatVisualState(0.0f);
    }

    public void ClearTelegraphState()
    {
        SetTelegraphState(false, _isTelegraphPhaseTwo, 0.0f);
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
        ClearTelegraphState();
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

    private void RefreshCombatVisualState(float dt)
    {
        if (_isTelegraphing)
        {
            _telegraphPulseTime += dt;
        }

        Color tint = Colors.White;
        if (_hitFlashRemaining > 0.0f)
        {
            tint = _hitFlashColor;
        }
        else if (_isTelegraphing)
        {
            Color telegraphColor = _isTelegraphPhaseTwo ? _phaseTwoTelegraphColor : _phaseOneTelegraphColor;
            float pulse = 0.55f + 0.45f * Mathf.Sin(_telegraphPulseTime * (_isTelegraphPhaseTwo ? 14.0f : 10.0f));
            float blend = Mathf.Clamp(0.12f + (1.0f - _telegraphProgress) * 0.20f + pulse * 0.10f, 0.0f, 0.65f);
            tint = telegraphColor.Lerp(Colors.White, blend);
        }

        Modulate = tint;

        if (_animatedSprite != null)
        {
            if (_isTelegraphing)
            {
                float pulse = 1.0f + 0.05f * Mathf.Sin(_telegraphPulseTime * (_isTelegraphPhaseTwo ? 16.0f : 11.0f));
                _animatedSprite.Scale = _baseSpriteScale * pulse;
            }
            else
            {
                _animatedSprite.Scale = _baseSpriteScale;
            }

            _animatedSprite.FlipH = !_isFacingRight;
        }
    }

    private void RefreshFacing()
    {
        if (_animatedSprite != null)
        {
            _animatedSprite.FlipH = !_isFacingRight;
        }
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

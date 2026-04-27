#nullable enable
using Godot;
using CyberPlant.Core;
using CyberPlant.Combat;

namespace CyberPlant.Player;

public partial class Player : CharacterBody2D, IDamageable
{
    private const string IdleAnimation = "idle";
    private const string IdleArmedAnimation = "idle_armed";
    private const string WalkAnimation = "walk";
    private const string WalkArmedAnimation = "walk_armed";
    private const string AttackAnimation = "attack";
    private const string AttackArmedAnimation = "attack_armed";

    [Signal]
    public delegate void HealthChangedEventHandler(int currentHealth, int maxHealth);

    [Signal]
    public delegate void DiedEventHandler();

    [Export] public float MoveSpeed { get; set; } = 300.0f;
    [Export] public float JumpVelocity { get; set; } = -700.0f;
    [Export] public float FallGravityMultiplier { get; set; } = 1.8f;
    [Export] public float JumpReleaseGravityMultiplier { get; set; } = 2.4f;
    [Export] public float MaxFallSpeed { get; set; } = 1100.0f;
    [Export] public float DamageKnockbackForce { get; set; } = 230.0f;
    [Export] public float DamageKnockbackDecay { get; set; } = 1250.0f;
    [Export] public float DamageKnockbackLift { get; set; } = 120.0f;
    [Export] public float DamageFlashDuration { get; set; } = 0.10f;
    [Export] public float FallDeathY { get; set; } = 3200.0f;
    [Export] public Vector2 RespawnPosition { get; set; } = Vector2.Zero;

    [Export] public int MaxHealth { get; set; } = 100;
    [Export] public int StartingHealth { get; set; } = 100;

    public int CurrentHealth { get; private set; }

    private float _gravity;
    private AnimatedSprite2D? _animatedSprite;
    private WeaponController? _weaponController;
    private GameManager? _gameManager;
    private Sprite2D? _weaponVisual;
    private double _weaponAnimationTime = 0.0;
    private bool _isWeaponAnimating = false;
    private bool _isFacingRight = true;
    private bool _hasActiveWeapon = false;
    private float _damageFlashRemaining;
    private float _damageKnockbackVelocityX;
    private Vector2 _levelStartPosition;
    private bool _isRespawning;

    public override void _Ready()
    {
        AddToGroup("player");
        _levelStartPosition = RespawnPosition == Vector2.Zero ? GlobalPosition : RespawnPosition;

        _gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");
        _animatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        CurrentHealth = Mathf.Clamp(StartingHealth, 0, MaxHealth);
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);

        _gameManager = GetNodeOrNull<GameManager>("/root/GameManager");
        _gameManager?.RegisterPlayer(this);

        _weaponController = GetNodeOrNull<WeaponController>("WeaponController");
        _weaponVisual = GetNodeOrNull<Sprite2D>("WeaponVisual");
        if (_gameManager != null)
        {
            UpdateActiveWeapon();
            _gameManager.Connect(GameManager.SignalName.ActiveInventorySlotChanged, new Callable(this, nameof(OnActiveInventorySlotChanged)));
        }

        UpdateVisualState(0.0f);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (GlobalPosition.Y >= FallDeathY)
        {
            RespawnAtLevelStart();
            return;
        }

        float dt = (float)delta;
        Vector2 velocity = Velocity;

        float moveInput = Input.GetAxis("move_left", "move_right");
        velocity.X = moveInput * MoveSpeed;

        if (!Mathf.IsZeroApprox(_damageKnockbackVelocityX))
        {
            velocity.X += _damageKnockbackVelocityX;
            _damageKnockbackVelocityX = Mathf.MoveToward(_damageKnockbackVelocityX, 0.0f, DamageKnockbackDecay * dt);
        }

        if (!Mathf.IsZeroApprox(moveInput))
        {
            _isFacingRight = moveInput > 0.0f;
        }

        if (!IsOnFloor())
        {
            float gravityMultiplier = 1.0f;
            if (velocity.Y > 0.0f)
            {
                gravityMultiplier = FallGravityMultiplier;
            }
            else if (velocity.Y < 0.0f && !Input.IsActionPressed("jump"))
            {
                gravityMultiplier = JumpReleaseGravityMultiplier;
            }

            velocity.Y += _gravity * gravityMultiplier * (float)delta;
            velocity.Y = Mathf.Min(velocity.Y, MaxFallSpeed);
        }

        if (Input.IsActionJustPressed("jump") && IsOnFloor())
        {
            velocity.Y = JumpVelocity;
        }

        if (Input.IsActionJustPressed("use_weapon"))
        {
            _weaponController?.Attack();
            PlayWeaponAnimation();
        }

        // Update weapon animation
        if (_isWeaponAnimating)
        {
            _weaponAnimationTime += delta;
            if (_weaponAnimationTime < 0.15)
            {
                if (_weaponVisual != null)
                {
                    float progress = (float)(_weaponAnimationTime / 0.15);
                    _weaponVisual.Rotation = Mathf.Lerp(0f, Mathf.Pi / 4, progress);
                }
            }
            else
            {
                if (_weaponVisual != null)
                {
                    _weaponVisual.Rotation = 0f;
                }
                _isWeaponAnimating = false;
            }
        }

        if (_damageFlashRemaining > 0.0f)
        {
            _damageFlashRemaining = Mathf.Max(0.0f, _damageFlashRemaining - dt);
            ApplyDamageTint(new Color(1.25f, 0.7f, 0.7f, 1.0f));
        }
        else
        {
            ApplyDamageTint(Colors.White);
        }

        UpdateVisualState(moveInput);
        Velocity = velocity;
        MoveAndSlide();
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || CurrentHealth <= 0)
        {
            return;
        }

        TriggerDamageFeedback(null);
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);

        if (CurrentHealth == 0)
        {
            EmitSignal(SignalName.Died);
            RespawnAtLevelStart();
        }
    }

    public void ApplyEnemyHitFeedback(Vector2 attackerPosition)
    {
        TriggerDamageFeedback(attackerPosition);
    }

    public void Heal(int amount)
    {
        // TODO (Ben): Hook healing pickups/weapon effects here.
        if (amount <= 0 || CurrentHealth <= 0)
        {
            return;
        }

        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
    }

    private void OnActiveInventorySlotChanged(int slotIndex)
    {
        UpdateActiveWeapon();
    }

    private void PlayWeaponAnimation()
    {
        _isWeaponAnimating = true;
        _weaponAnimationTime = 0.0;
        UpdateVisualState(Velocity.X);
    }

    private void UpdateActiveWeapon()
    {
        if (_gameManager == null || _weaponController == null)
        {
            return;
        }

        var item = _gameManager.GetActiveInventoryItem();
        var weapon = WeaponCatalog.GetWeaponForItem(item?.Id);

        _hasActiveWeapon = weapon != null;
        _weaponController.SetWeapon(weapon);
        UpdateVisualState(Velocity.X);
    }

    public void SetSpawnPosition(Vector2 spawnPosition)
    {
        GlobalPosition = spawnPosition;
        _levelStartPosition = spawnPosition;
    }

    private void RespawnAtLevelStart()
    {
        if (_isRespawning)
        {
            return;
        }

        _isRespawning = true;
        GlobalPosition = _levelStartPosition;
        Velocity = Vector2.Zero;
        _damageKnockbackVelocityX = 0.0f;
        _damageFlashRemaining = 0.0f;
        _isWeaponAnimating = false;
        _weaponAnimationTime = 0.0;
        CurrentHealth = MaxHealth;
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
        ApplyDamageTint(Colors.White);
        UpdateVisualState(0.0f);
        _isRespawning = false;
    }

    private void TriggerDamageFeedback(Vector2? attackerPosition)
    {
        float horizontalDirection;
        if (attackerPosition.HasValue)
        {
            horizontalDirection = Mathf.Sign(GlobalPosition.X - attackerPosition.Value.X);
            if (Mathf.IsZeroApprox(horizontalDirection))
            {
                horizontalDirection = _isFacingRight ? -1.0f : 1.0f;
            }
        }
        else
        {
            horizontalDirection = _isFacingRight ? -1.0f : 1.0f;
        }

        _damageKnockbackVelocityX = horizontalDirection * DamageKnockbackForce;
        _damageFlashRemaining = DamageFlashDuration;

        if (IsOnFloor())
        {
            Vector2 velocity = Velocity;
            velocity.Y = -DamageKnockbackLift;
            Velocity = velocity;
        }
    }

    private void ApplyDamageTint(Color color)
    {
        if (_animatedSprite != null)
        {
            _animatedSprite.Modulate = color;
        }

        if (_weaponVisual != null)
        {
            _weaponVisual.Modulate = color;
        }
    }

    private void UpdateVisualState(float moveInput)
    {
        if (_animatedSprite == null)
        {
            return;
        }

        string animationName;
        if (_isWeaponAnimating)
        {
            animationName = _hasActiveWeapon ? AttackArmedAnimation : AttackAnimation;
        }
        else if (Mathf.IsZeroApprox(moveInput))
        {
            animationName = _hasActiveWeapon ? IdleArmedAnimation : IdleAnimation;
        }
        else
        {
            animationName = _hasActiveWeapon ? WalkArmedAnimation : WalkAnimation;
        }

        if (_animatedSprite.Animation != animationName)
        {
            _animatedSprite.Play(animationName);
        }

        _animatedSprite.FlipH = !_isFacingRight;

        if (_weaponVisual != null)
        {
            _weaponVisual.Visible = false;
            _weaponVisual.FlipH = !_isFacingRight;
        }

        if (_weaponController != null)
        {
            _weaponController.Scale = new Vector2(_isFacingRight ? 1.0f : -1.0f, 1.0f);
        }
    }
}
